using Authentication.Application.Interfaces;
using Authentication.API.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Authentication.Application.ViewModels;
using Authentication.Domain.Models;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Models.ReportModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SharedApp.Infrastructure.Sender;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MatliOtomasyon.Shared.Infrastructure.Configuration;

namespace Authentication.API.Controllers
{
    /// <summary>
    /// Kimlik doğrulama ve kullanıcı yönetimi uçlarını sunar: giriş, OTP, token yenileme, çıkış,
    /// şifre işlemleri, profil fotoğrafı, kullanıcı CRUD'u ve oturum/sayfa ziyaret kayıtları.
    /// Yetki işaretlenmemiş her uç kimlik doğrulaması ister (Program.cs'teki fallback policy).
    /// Anonim uçlar <see cref="AllowAnonymousAttribute"/> ile, kullanıcı yönetimi uçları
    /// <see cref="UserManagementPolicies"/> politikalarıyla açıkça işaretlenir.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private const int MaxJwtLength = 32768;

        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly EmailSender _emailSender;
        private readonly SmsSender _smsSender;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuthChallenges _challenges;
        private readonly IUserRepository _authUsers;
        private readonly CustomTokenOption _tokenOptions;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Controller'ın ihtiyaç duyduğu servisleri alır.
        /// </summary>
        public UserController(IUserService userService,
            ITokenService tokenService,
            ITwoFactorService twoFactorService,
            EmailSender emailSender,
            SmsSender smsSender,
            IHttpContextAccessor httpContextAccessor,
            AuthChallenges challenges,
            IOptions<CustomTokenOption> tokenOptions,
            IUserRepository authUsers,
            IAuthorizationService authorizationService
            )
        {
            _userService = userService;
            _tokenService = tokenService;
            _twoFactorService = twoFactorService;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _httpContextAccessor = httpContextAccessor;
            _challenges = challenges;
            _authUsers = authUsers;
            _tokenOptions = tokenOptions.Value;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// İsteği yapan istemcinin IP adresini döner. Forwarded header'ları sadece güvenilir proxy
        /// middleware'i yorumlayabilir; tüketilmemiş bir X-Forwarded-For header'ı, proxy'nin güvenilir
        /// olmadığını veya değerin bozuk olduğunu gösterir. Bu durumda "unknown" döner.
        /// </summary>
        private string GetClientIpAddress()
        {
            // Only trusted-proxy middleware may interpret forwarded headers.
            // An unconsumed header means the proxy was not trusted or the value was malformed.
            // Do not mistake the proxy's private address for an internal end user in that case.
            if (Request.Headers.ContainsKey("X-Forwarded-For")) return "unknown";
            var address = HttpContext.Connection.RemoteIpAddress;
            return (address?.IsIPv4MappedToIPv6 == true ? address.MapToIPv4() : address)?.ToString() ?? "unknown";
        }

        /// <summary>
        /// İstemcinin User-Agent bilgisini döner. Login kayıtlarında isteğin gerçek bir tarayıcıdan mı
        /// yoksa script/otomasyon aracından mı geldiğini ayırt etmek için kullanılır.
        /// </summary>
        private string GetClientUserAgent()
        {
            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
            return string.IsNullOrEmpty(userAgent) ? "Bilinmiyor" : userAgent;
        }

        /// <summary>
        /// ConnectSiteLog açıklamasını mesaj ve User-Agent birleşimi olarak oluşturur. Kolon DB'de
        /// varchar(500) olduğu için, uzun bir User-Agent'ın "String or binary data would be truncated"
        /// hatasına yol açmaması amacıyla sonucu 500 karaktere sığacak şekilde kırpar.
        /// </summary>
        private string BuildAciklama(string baseMessage, string userAgent)
        {
            const int maxLength = 500;
            const string separator = " | UA: ";

            if (baseMessage.Length >= maxLength)
            {
                return baseMessage.Substring(0, maxLength);
            }

            int availableForUa = maxLength - baseMessage.Length - separator.Length;
            if (availableForUa <= 0)
            {
                return baseMessage;
            }

            string truncatedUa = userAgent.Length > availableForUa
                ? userAgent.Substring(0, availableForUa)
                : userAgent;

            return baseMessage + separator + truncatedUa;
        }

        /// <summary>
        /// Değeri verilen kolon uzunluğuna sığacak şekilde kırpar. ConnectSiteLog.Browser ve
        /// BrowserVersion kolonları varchar(100) olduğu için, istemcinin gönderdiği uzun veya
        /// rastgele değerlerin kayıt hatasına yol açmasını önler.
        /// </summary>
        private string TruncateForColumn(string value, int maxLength = 100)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }

        /// <summary>
        /// Oturum açmış kullanıcının Id'sini token'daki NameIdentifier claim'inden döner.
        /// </summary>
        private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>
        /// Bir JWT'nin imzasını, issuer'ını ve audience'ını doğrular. Süresi dolmuş token'lar kabul edilir,
        /// çünkü bu metot sadece token yenileme ve sekme kapanışı kaydı gibi süre dolduktan sonra da
        /// gelebilen isteklerde kullanılır; asıl oturum süresi refresh token tarafında kontrol edilir.
        /// </summary>
        /// <param name="jwtToken">Doğrulanacak token.</param>
        /// <param name="principal">Token geçerliyse içindeki kullanıcı bilgileri.</param>
        /// <returns>Token geçerliyse true.</returns>
        private bool TryValidateSignedToken(string? jwtToken, out ClaimsPrincipal principal)
        {
            principal = new ClaimsPrincipal();
            if (string.IsNullOrEmpty(jwtToken) || jwtToken.Length > MaxJwtLength)
                return false;

            try
            {
                principal = new JwtSecurityTokenHandler().ValidateToken(jwtToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenOptions.SecurityKey)),
                    ValidateIssuer = true,
                    ValidIssuer = _tokenOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudiences = _tokenOptions.Audience,
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256, SecurityAlgorithms.HmacSha256Signature },
                    // Expired access tokens are allowed only here; refresh lifetime is checked separately.
                    ValidateLifetime = false
                }, out _);
                return true;
            }
            catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Yeni bir kullanıcı oluşturur. Kullanıcı yönetimi formunda Yazma yetkisi gerekir.
        /// </summary>
        [Authorize(Policy = UserManagementPolicies.Write)]
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser(UserViewModel userViewModel)
        {
            var result = await _userService.CreateUser(userViewModel);
            return Ok(result);
        }

        /// <summary>
        /// Kullanıcının şifresini sıfırlar ve yeni şifreyi e-posta ile gönderir. Yeni kullanıcıya ilk
        /// giriş bilgisini iletmek için kullanılır. Kullanıcı yönetimi formunda Yazma yetkisi gerekir.
        /// </summary>
        [Authorize(Policy = UserManagementPolicies.Write)]
        [HttpPut("SendEmailForNewUser")]
        public async Task<IActionResult> SendEmailForNewUser(string userId)
        {
            User user = await _userService.GetUserById(userId);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            var newPassword = await _userService.ResetPassword(user);
            _emailSender.SendNewUserEmail(newPassword, user.Email, user.Name + " " + user.Surname, user.SicilNo);
            return Ok();
        }

        /// <summary>
        /// Kullanıcının şifresini sıfırlar ve yeni şifreyi SMS ile gönderir. Yeni kullanıcıya ilk
        /// giriş bilgisini iletmek için kullanılır. Kullanıcı yönetimi formunda Yazma yetkisi gerekir.
        /// </summary>
        [Authorize(Policy = UserManagementPolicies.Write)]
        [HttpPut("SendSmsForNewUser")]
        public async Task<IActionResult> SendSmsForNewUser(string userId)
        {
            User user = await _userService.GetUserById(userId);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            var newPassword = await _userService.ResetPassword(user);
            _smsSender.SendNewUserSms(newPassword, user.PhoneNumber, user.Name + " " + user.Surname, user.SicilNo);
            return Ok();
        }

        /// <summary>
        /// Mevcut bir kullanıcının bilgilerini günceller. Kullanıcı yönetimi formunda Yazma yetkisi gerekir.
        /// </summary>
        [Authorize(Policy = UserManagementPolicies.Write)]
        [HttpPost("EditUser")]
        public async Task<IActionResult> EditUser(UserViewModel userViewModel)
        {
            await _userService.EditUser(userViewModel);
            return Ok();
        }

        /// <summary>
        /// Kullanıcı adına göre kullanıcı kaydını döner. Kullanıcı kendi kaydını her zaman görebilir;
        /// başka bir kullanıcının kaydını görmek için kullanıcı yönetimi formunda Okuma yetkisi gerekir.
        /// </summary>
        [HttpGet("GetUser")]
        public async Task<IActionResult> GetUser(string userName)
        {
            var callerUserName = User.FindFirstValue(ClaimTypes.GivenName);
            var isOwnAccount = !string.IsNullOrEmpty(callerUserName) &&
                string.Equals(callerUserName, userName, StringComparison.OrdinalIgnoreCase);
            if (!isOwnAccount)
            {
                var authorization = await _authorizationService.AuthorizeAsync(User, UserManagementPolicies.Read);
                if (!authorization.Succeeded)
                    return Forbid();
            }

            var user = await _userService.GetUserByName(userName);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            return Ok(user);
        }

        /// <summary>
        /// Verilen tarih aralığındaki kullanıcı giriş/çıkış oturumlarını döner.
        /// Kullanıcı yönetimi formunda Okuma yetkisi gerekir.
        /// </summary>
        [Authorize(Policy = UserManagementPolicies.Read)]
        [HttpGet("GetAllUserConnectSiteLogs")]
        public async Task<IActionResult> GetAllUserConnectSiteLogs(DateTime baslangicZamani, DateTime bitisZamani)
        {
            var users = await _userService.GetAllUserConnectSiteLogs(baslangicZamani, bitisZamani);
            return Ok(users);
        }

        /// <summary>
        /// Kullanıcıların sayfa ziyaret raporunu döner. Kullanıcı yönetimi formunda Okuma yetkisi gerekir.
        /// </summary>
        [Authorize(Policy = UserManagementPolicies.Read)]
        [HttpGet("GetAllvwPageVisits")]
        public async Task<IActionResult> GetAllvwPageVisits()
        {
            var users = await _userService.GetAllvwPageVisits();
            return Ok(users);
        }

        /// <summary>
        /// Ticket ekranlarında atanabilecek kullanıcıların şifrelenmiş listesini döner.
        /// Tedarikçiler, silinmiş kullanıcılar ve sistem yöneticisi hesapları listeye dahil edilmez.
        /// Oturum açmış her kullanıcı çağırabilir.
        /// </summary>
        [HttpGet("GetAllUsersForTicket")]
        public async Task<IActionResult> GetAllUsersForTicket()
        {
            var users = (await _userService.GetAllUsers()).Where(x => x.UserName != "onder.matli" && x.UserName != "ozer.matli" && x.Tedarikci != true && x.Silindi != true).ToList();
            var json = System.Text.Json.JsonSerializer.Serialize(users);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        /// <summary>
        /// Tüm kullanıcıların listesini döner. Kullanıcı yönetimi formunda Okuma yetkisi ya da
        /// geçerli bir iç servis anahtarı gerekir; SharedApp kullanıcı listesini bu uçtan anahtarla çeker.
        /// </summary>
        [Authorize(Policy = UserManagementPolicies.ReadOrInternalService)]
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsers();
            return Ok(users);
        }

        /// <summary>
        /// Seçim kutularında kullanılmak üzere kullanıcı listesini döner. Oturum açmış her kullanıcı çağırabilir.
        /// </summary>
        [HttpGet("GetAllUsersForCombobox")]
        public async Task<IActionResult> GetAllUsersForCombobox()
        {
            var users = await _userService.GetAllUsersForCombobox();
            return Ok(users);
        }

        /// <summary>
        /// Kullanıcıyı siler. Kullanıcı yönetimi formunda Yazma yetkisi gerekir.
        /// </summary>
        [Authorize(Policy = UserManagementPolicies.Write)]
        [HttpPut("DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromQuery] string userId)
        {
            await _userService.DeleteUser(userId);
            return Ok();
        }

        /// <summary>
        /// Kullanıcı adı ve şifreyle giriş yapar. OTP gerekiyorsa SMS gönderip challenge döner,
        /// gerekmiyorsa doğrudan access ve refresh token üretir. Başarılı ve başarısız tüm denemeler
        /// ConnectSiteLog'a yazılır, başarısız denemeler sayılır.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("authentication")]
        [HttpPost("SignIn")]
        public async Task<IActionResult> SignIn(LoginViewModel loginViewModel)
        {
            if (string.IsNullOrWhiteSpace(loginViewModel.Name) || loginViewModel.Name.Length > 256 ||
                string.IsNullOrEmpty(loginViewModel.Password) || loginViewModel.Password.Length > 1024)
                return BadRequest(new { ErrorMessage = "Kullanıcı adı veya şifre geçersiz." });
            if (!_challenges.Allow("signin", loginViewModel.Name, 10, TimeSpan.FromMinutes(5)))
                return StatusCode(429, new { ErrorMessage = "Çok fazla deneme. Lütfen daha sonra tekrar deneyin." });
            var user = await _userService.GetUserBySicilNo(loginViewModel.Name);
            string ipAddress = GetClientIpAddress();
            string userAgent = GetClientUserAgent();

            // BrowserInfo, gerçek frontend her zaman gönderdiği için doluyor; ama bunu null-safe
            // (?. ve fallback) okumuyorsak, browserInfo göndermeyen bir istemci (script/otomasyon/
            // başka bir araç) burada NullReferenceException'a sebep olup AddConnectSiteLog hiç
            // çalışmadan isteği patlatıyordu — yani token DB'de güncelleniyor ama giriş HİÇ
            // loglanmıyordu. Artık her durumda (BrowserInfo eksik olsa bile) log satırı yazılıyor.
            string browserName = TruncateForColumn(loginViewModel.BrowserInfo?.BrowserName ?? "Bilinmiyor");
            string browserVersion = TruncateForColumn(loginViewModel.BrowserInfo?.BrowserVersion ?? "Bilinmiyor");
            bool isMobile = loginViewModel.BrowserInfo?.IsMobile ?? false;

            if (user == null)
            {
                await _userService.AddConnectSiteLog(ipAddress?.ToString(), "", browserName, browserVersion, isMobile, BuildAciklama($"{loginViewModel.Name} - Kullanıcı bulunamadı", userAgent));
                return Unauthorized(new { ErrorMessage = "Kullanıcı adı veya şifre doğru değil." });
            }
            var result = await _userService.CheckPasswordAsync(user, loginViewModel.Password);
            if (user.IsLocked == true || user.Silindi == true || user.IncorrectLoginNumber.GetValueOrDefault(0) >= 5)
            {
                return Unauthorized(new { ErrorMessage = "Giriş yapılamadı. Bilgilerinizi kontrol edin veya şifrenizi sıfırlayın." });
            }
            if (!result)
            {
                await _userService.AddConnectSiteLog(ipAddress?.ToString(), user.Id, browserName, browserVersion, isMobile, BuildAciklama("Kullanıcı adı veya şifre doğru değil", userAgent));
                await _userService.IncorrectLoginNumberUpdate(user.Id, false);
                return Unauthorized(new { ErrorMessage = "Kullanıcı adı veya şifre doğru değil." });
            }

            var isOtpRequired = await _twoFactorService.CheckOtpRequired(user, ipAddress == null ? "" : ipAddress.ToString());
            if (isOtpRequired)
            {
                var otpCode = await _twoFactorService.GetCodeVerification(user.Id, ipAddress == null ? "" : ipAddress.ToString());
                _smsSender.SendOtpSms(user.PhoneNumber, otpCode, user.Name + " " + user.Surname);
                await _userService.AddConnectSiteLog(ipAddress?.ToString(), user.Id, browserName, browserVersion, isMobile, BuildAciklama("OTP Gönderildi", userAgent));
                var challengeId = _challenges.Issue("login", user.Id, user.PasswordHash, otpCode, TimeSpan.FromMinutes(2));
                var phone = user.PhoneNumber ?? "";
                return Ok(new
                {
                    RequiresOtp = true,
                    ChallengeId = challengeId,
                    ExpiresIn = 120,
                    MaskedPhone = phone.Length >= 4 ? "******" + phone[^4..] : "******"
                });
            }
            else
            {
                TokenViewModel tokenViewModel = await _tokenService.CreateToken(user, false);
                await _userService.AddConnectSiteLog(ipAddress?.ToString(), user.Id, browserName, browserVersion, isMobile, BuildAciklama("Başarılı Giriş", userAgent));
                await _userService.IncorrectLoginNumberUpdate(user.Id, true);
                return Ok(tokenViewModel);
            }
        }

        /// <summary>
        /// Girişin ikinci adımı: SignIn'in verdiği challenge ile SMS'le gelen OTP kodunu doğrular ve
        /// başarılıysa token üretir. Şifre challenge verildikten sonra değiştiyse doğrulama reddedilir.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("authentication")]
        [HttpPut("VerifyOtp")]
        public async Task<IActionResult> CheckTwoFactorAuth([FromBody] OtpViewModel request)
        {
            if (!_challenges.TryConsume("login", request.ChallengeId, request.OtpCode, out var userId, out var stamp))
                return Unauthorized(new { ErrorMessage = "Doğrulama başarısız veya kodun süresi dolmuş." });
            var user = await _authUsers.GetUserById(userId);
            if (user == null || user.Silindi == true || user.IsLocked == true ||
                user.IncorrectLoginNumber.GetValueOrDefault() >= 5 || AuthChallenges.Fingerprint(user.PasswordHash) != stamp)
                return Unauthorized(new { ErrorMessage = "Doğrulama başarısız. Tekrar giriş yapın." });
            if (!await _twoFactorService.VerifyOtpCode(user.Id, request.OtpCode))
                return Unauthorized(new { ErrorMessage = "Doğrulama başarısız. Tekrar giriş yapın." });
            var token = await _tokenService.CreateToken(user, false);
            await _userService.AddConnectSiteLog(GetClientIpAddress(), user.Id,
                TruncateForColumn(request.BrowserInfo?.BrowserName ?? "Bilinmiyor"),
                TruncateForColumn(request.BrowserInfo?.BrowserVersion ?? "Bilinmiyor"),
                request.BrowserInfo?.IsMobile ?? false, BuildAciklama("Başarılı Giriş", GetClientUserAgent()));
            await _userService.IncorrectLoginNumberUpdate(user.Id, true);
            return Ok(token);
        }

        /// <summary>
        /// Süresi dolmuş (ama imzası geçerli) access token ve refresh token ile yeni token çifti üretir.
        /// Refresh token tek kullanımlıktır; kilitli veya silinmiş hesaplar için yenileme reddedilir.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("authentication")]
        [HttpPost("NewToken")]
        public async Task<IActionResult> NewToken(NewTokenViewModel request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken) || request.RefreshToken.Length > 256)
                return Unauthorized(new { ErrorMessage = "Geçersiz oturum." });
            if (!TryValidateSignedToken(request.JwtToken, out var principal))
                return Unauthorized(new { ErrorMessage = "Geçersiz oturum." });
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { ErrorMessage = "Geçersiz oturum." });
            var user = await _authUsers.GetUserById(userId);
            if (user == null || user.Silindi == true || user.IsLocked == true || user.IncorrectLoginNumber.GetValueOrDefault() >= 5)
                return Unauthorized(new { ErrorMessage = "Geçersiz oturum." });
            try { return Ok(await _tokenService.CreateNewRefreshToken(user, request.RefreshToken)); }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { ErrorMessage = "Oturum yenilenemedi. Tekrar giriş yapın." });
            }
        }

        /// <summary>
        /// Oturumu kapatır: kullanıcının refresh token'ını ve heartbeat kaydını siler, çıkışı loglar.
        /// Kullanıcı kimliği istek gövdesinden değil, doğrulanmış token'dan alınır.
        /// </summary>
        [Authorize]
        [HttpPost("SignOut")]
        public async Task<IActionResult> SignOut([FromBody] SignOutViewModel request)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            await _tokenService.RemoveToken(userId);
            await _tokenService.RemoveUserHeartBeat(userId);
            await _userService.AddConnectSiteLog(GetClientIpAddress(), userId,
                TruncateForColumn(request.BrowserInfo?.BrowserName ?? "Bilinmiyor"),
                TruncateForColumn(request.BrowserInfo?.BrowserVersion ?? "Bilinmiyor"),
                request.BrowserInfo?.IsMobile ?? false, "Başarılı Çıkış");
            return Ok();
        }

        /// <summary>
        /// Oturum açmış kullanıcının şifresini değiştirir. Eski şifre doğrulanır ve yeni şifrenin
        /// iki kez aynı girilmiş olması gerekir.
        /// </summary>
        [HttpPut("ChangePassword")]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel changePasswordViewModel)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userService.GetUserById(userId);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            var result = await _userService.CheckPasswordAsync(user, changePasswordViewModel.OldPassword);

            if (!result)
                return BadRequest(new { message = "Eski şifre hatalı!" });

            if (changePasswordViewModel.NewPassword != changePasswordViewModel.NewPasswordAgain)
            {
                return BadRequest(new { message = "Yeni Şifreler aynı olmalıdır!" });
            }

            await _userService.ChangePassword(user, changePasswordViewModel.NewPassword);

            return Ok();
        }

        /// <summary>
        /// Oturum açmış kullanıcının profil fotoğrafını yükler veya değiştirir.
        /// </summary>
        [HttpPost("ChangeProfilePhoto")]
        public async Task<IActionResult> ChangeProfilePhoto([FromForm] IFormFile file)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userService.GetUserById(userId);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            var result = await _userService.ChangeProfilePhoto(user, file);
            return Ok(result);
        }

        /// <summary>
        /// Oturum açmış kullanıcının profil fotoğrafını döner.
        /// </summary>
        [HttpGet("GetProfilePhoto")]
        public async Task<IActionResult> GetProfilePhoto()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userService.GetUserById(userId);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            return Ok(user.ProfilePhoto);
        }

        /// <summary>
        /// Oturum açmış kullanıcının profil fotoğrafını siler.
        /// </summary>
        [HttpGet("DeleteProfilePhoto")]
        public async Task<IActionResult> DeleteProfilePhoto()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _userService.GetUserById(userId);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            var result = await _userService.DeleteProfilePhoto(user);
            return Ok(result);
        }

        /// <summary>
        /// Şifre sıfırlamanın ilk adımı: kullanıcı adı ve iletişim bilgisi eşleşirse e-posta veya SMS ile
        /// doğrulama kodu gönderir. Hesabın var olup olmadığını sızdırmamak için her durumda aynı cevabı döner.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("authentication")]
        [HttpPut("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel request)
        {
            if (string.IsNullOrWhiteSpace(request.UserName) || request.UserName.Length > 256 ||
                string.IsNullOrWhiteSpace(request.Detail) || request.Detail.Length > 256 ||
                (request.Method != "email" && request.Method != "sms"))
                return BadRequest(new { ErrorMessage = "Kullanıcı adı, iletişim bilgisi ve yöntem gerekli." });
            var challengeId = AuthChallenges.NewId();
            if (_challenges.Allow("reset", request.UserName, 3, TimeSpan.FromMinutes(15)))
            {
                var user = await _userService.GetUserBySicilNo(request.UserName);
                var matches = user != null && user.Silindi != true && user.IsLocked != true &&
                    (request.Method == "email"
                        ? string.Equals(user.Email?.Trim(), request.Detail.Trim(), StringComparison.OrdinalIgnoreCase)
                        : string.Equals(user.PhoneNumber, request.Detail.Trim(), StringComparison.Ordinal));
                if (matches)
                {
                    var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                    challengeId = _challenges.Issue("reset", user!.Id, user.PasswordHash, code, TimeSpan.FromMinutes(10));
                    if (request.Method == "email") await _emailSender.SendOtp(code, user.Email, isPasswordReset: true);
                    else _smsSender.SendOtpSms(user.PhoneNumber, code, user.Name + " " + user.Surname, isPasswordReset: true);
                }
            }
            return Ok(new
            {
                ChallengeId = challengeId,
                ExpiresIn = 600,
                Message = "Bilgiler eşleşiyorsa doğrulama kodu gönderildi."
            });
        }

        /// <summary>
        /// Şifre sıfırlamanın ikinci adımı: doğrulama kodunu kontrol edip yeni şifreyi kaydeder ve
        /// kullanıcının açık oturumunu sonlandırır. Yeni şifre 6-24 karakter olmalı; büyük/küçük harf,
        /// rakam ve özel karakter içermelidir.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("authentication")]
        [HttpPut("CompletePasswordReset")]
        public async Task<IActionResult> CompletePasswordReset(CompletePasswordResetViewModel request)
        {
            if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6 || request.NewPassword.Length > 24 ||
                !request.NewPassword.Any(char.IsUpper) || !request.NewPassword.Any(char.IsLower) ||
                !request.NewPassword.Any(char.IsDigit) || !request.NewPassword.Any(c => !char.IsLetterOrDigit(c)) ||
                request.NewPassword != request.NewPasswordAgain)
                return BadRequest(new { ErrorMessage = "Şifreler eşleşmeli; şifre 6–24 karakter, büyük/küçük harf, rakam ve özel karakter içermeli." });
            if (!_challenges.TryConsume("reset", request.ChallengeId, request.Code, out var userId, out var stamp))
                return BadRequest(new { ErrorMessage = "Doğrulama başarısız veya kodun süresi dolmuş." });
            var user = await _authUsers.GetUserById(userId);
            if (user == null || user.Silindi == true || user.IsLocked == true || AuthChallenges.Fingerprint(user.PasswordHash) != stamp)
                return BadRequest(new { ErrorMessage = "Doğrulama başarısız. Yeni bir kod talep edin." });
            if (!await _authUsers.TryResetPassword(user.Id, user.PasswordHash, request.NewPassword))
                return BadRequest(new { ErrorMessage = "Doğrulama başarısız. Yeni bir kod talep edin." });
            await _tokenService.RemoveUserHeartBeat(user.Id);
            return Ok(new { Message = "Şifreniz değiştirildi. Yeni şifrenizle giriş yapabilirsiniz." });
        }

        /// <summary>
        /// Metni AES ile şifreleyip Base64 olarak döner. GetAllUsersForTicket cevabını şifrelemek için kullanılır.
        /// </summary>
        private static string AesEncrypt(string plainText, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[16]; // 16 byte boş IV

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (var sw = new StreamWriter(cs)) sw.Write(plainText);

            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Frontend'in açık sekmeler için periyodik gönderdiği heartbeat'i kaydeder; aktif kullanıcı ve
        /// sayfa raporları bu kayıtlardan üretilir. Token'ı süresi dolmuş istemciler de heartbeat gönderebildiği
        /// için anonim isteklere açıktır; kimliği doğrulanmamış istekler sessizce yok sayılır.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("heartbeat")]
        public async Task<IActionResult> LogHeartbeat([FromBody] HeartbeatDto dto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) { return Ok(); }
            var parts = dto.CurrentPage.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            string moduleLink = "";
            string formLink = "";
            string urlId = null;

            if (parts.Length >= 1)
            {
                moduleLink = "/" + parts[0];
            }

            if (parts.Length >= 2)
            {
                formLink = "/" + parts[1];
            }

            if (parts.Length == 3 && int.TryParse(parts[2], out int numericId))
            {
                urlId = numericId.ToString();
            }
            await _userService.AddUserHeartBeat(userId, dto.CurrentPage, moduleLink, formLink, urlId, dto.TabId);
            return Ok();
        }

        /// <summary>
        /// Kullanıcının bir sayfayı ziyaret ettiğini kaydeder; sayfa ziyaret raporu bu kayıtlardan üretilir.
        /// Heartbeat ile aynı sebeple anonim isteklere açıktır; kimliği doğrulanmamış istekler sessizce yok sayılır.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("page-visit")]
        public async Task<IActionResult> LogPageVisit([FromBody] PageVisitDto dto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) { return Ok(); }
            var parts = dto.Url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            string moduleLink = "";
            string formLink = "";
            string urlId = null;

            if (parts.Length >= 1)
            {
                moduleLink = "/" + parts[0];
            }

            if (parts.Length >= 2)
            {
                formLink = "/" + parts[1];
            }

            if (parts.Length == 3 && int.TryParse(parts[2], out int numericId))
            {
                urlId = numericId.ToString();
            }
            await _userService.AddPageVisitMain(userId, dto.Url, moduleLink, formLink, urlId, dto.TabId);
            return Ok();
        }

        /// <summary>
        /// Kullanıcı bir sekmeyi kapattığında sayfadan çıkışı kaydeder. Tarayıcı sekme kapanırken header
        /// gönderemediği (sendBeacon) için token istek gövdesinde gelir ve bu yüzden uç anonime açıktır.
        /// Token'ın imzası doğrulanır; geçersiz token'la başka bir kullanıcı adına kayıt atılamaz.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("tab-close")]
        public async Task<IActionResult> TabClose()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            var dto = JsonConvert.DeserializeObject<TabCloseDto>(body);
            if (dto == null || !TryValidateSignedToken(dto.Token, out var principal))
            {
                return Ok();
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(dto.CurrentPage) || string.IsNullOrEmpty(userId))
            {
                return Ok();
            }
            var parts = dto.CurrentPage.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            string moduleLink = "";
            string formLink = "";
            string urlId = null;

            if (parts.Length >= 1)
            {
                moduleLink = "/" + parts[0];
            }

            if (parts.Length >= 2)
            {
                formLink = "/" + parts[1];
            }

            if (parts.Length == 3 && int.TryParse(parts[2], out int numericId))
            {
                urlId = numericId.ToString();
            }
            await _userService.AddPageExitMain(userId, dto.CurrentPage, moduleLink, formLink, urlId, dto.TabId);

            return Ok();
        }
    }
}
