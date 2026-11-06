using Authentication.Application.Interfaces;
using Authentication.API.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Configurations;
using Authentication.Application.ViewModels;
using Authentication.Domain.Models;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Models.ReportModels;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SharedApp.Infrastructure.Sender;
using SharedKernel.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Authentication.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly EmailSender _emailSender;
        private readonly SmsSender _smsSender;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserExtension _userExtension;
        private readonly IMapper _mapper;
        private readonly AuthChallenges _challenges;
        private readonly IUserRepository _authUsers;
        private readonly CustomTokenOption _tokenOptions;

        public UserController(IUserService userService,
            ITokenService tokenService,
            ITwoFactorService twoFactorService,
            EmailSender emailSender,
            SmsSender smsSender,
            IHttpContextAccessor httpContextAccessor,
            IUserExtension userExtension,
            IMapper mapper, AuthChallenges challenges, IOptions<CustomTokenOption> tokenOptions, IUserRepository authUsers
            )
        {
            _userService = userService;
            _tokenService = tokenService;
            _twoFactorService = twoFactorService;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _httpContextAccessor = httpContextAccessor;
            _userExtension = userExtension;
            _mapper = mapper;
            _challenges = challenges;
            _authUsers = authUsers;
            _tokenOptions = tokenOptions.Value;
        }

        private string GetClientIpAddress()
        {
            // Only trusted-proxy middleware may interpret forwarded headers.
            // An unconsumed header means the proxy was not trusted or the value was malformed.
            // Do not mistake the proxy's private address for an internal end user in that case.
            if (Request.Headers.ContainsKey("X-Forwarded-For")) return "unknown";
            var address = HttpContext.Connection.RemoteIpAddress;
            return (address?.IsIPv4MappedToIPv6 == true ? address.MapToIPv4() : address)?.ToString() ?? "unknown";
        }

        // Gerçek tarayıcı istekleri her zaman bir User-Agent header'ı taşır; script/otomasyon/AI
        // araçları genelde farklı veya boş bir User-Agent gönderir. Login isteklerini bu bilgiyle
        // birlikte loglayarak isteğin gerçekten bir tarayıcıdan mı yoksa başka bir istemciden mi
        // geldiğini ayırt edebiliyoruz.
        private string GetClientUserAgent()
        {
            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
            return string.IsNullOrEmpty(userAgent) ? "Bilinmiyor" : userAgent;
        }

        // ConnectSiteLog.Aciklama kolonu DB'de varchar(500); mesaj + UA birleşimini yine de kolona
        // sığacak şekilde kırpıyoruz (aksi halde SaveChanges "String or binary data would be
        // truncated" hatasıyla patlıyor) — pathological derecede uzun bir User-Agent header'ına
        // karşı güvenlik payı olarak.
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

        // ConnectSiteLog.Browser / BrowserVersion kolonları da varchar(100). Gerçek frontend kısa
        // değerler gönderir (ör. "Chrome" / "120.0.0.0"), ama browserInfo'yu kendi gönderen bir
        // script/AI istemcisi buraya uzun/rastgele bir değer koyarsa aynı truncation hatası bu
        // kolonlarda da oluşabilir — o yüzden burada da güvenli şekilde kırpıyoruz.
        private string TruncateForColumn(string value, int maxLength = 100)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }

        [HttpPost("PostSms")]
        public async Task<IActionResult> PostSms()
        {
            var result = await _userService.PostSms();
            return Ok(result);
        }

        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser(UserViewModel userViewModel)
        {
            var result = await _userService.CreateUser(userViewModel);
            return Ok(result);
        }

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

        [HttpPost("EditUser")]
        public async Task<IActionResult> EditUser(UserViewModel userViewModel)
        {
            await _userService.EditUser(userViewModel);
            return Ok();
        }

        [HttpGet("GetUser")]
        public async Task<IActionResult> GetUser(string userName)
        {
            var user = await _userService.GetUserByName(userName);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            return Ok(user);
        }

        [HttpGet("GetAllUserConnectSiteLogs")]
        public async Task<IActionResult> GetAllUserConnectSiteLogs(DateTime baslangicZamani, DateTime bitisZamani)
        {
            var users = await _userService.GetAllUserConnectSiteLogs(baslangicZamani, bitisZamani);
            return Ok(users);
        }

        [HttpGet("GetAllvwPageVisits")]
        public async Task<IActionResult> GetAllvwPageVisits()
        {
            var users = await _userService.GetAllvwPageVisits();
            return Ok(users);
        }

        [HttpGet("GetAllUsersForTicket")]
        public async Task<IActionResult> GetAllUsersForTicket()
        {
            var users = (await _userService.GetAllUsers()).Where(x => x.UserName != "onder.matli" && x.UserName != "ozer.matli" && x.Tedarikci != true && x.Silindi != true).ToList();
            var json = System.Text.Json.JsonSerializer.Serialize(users);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsers();
            return Ok(users);
        }

        [HttpGet("GetAllUsersForCombobox")]
        public async Task<IActionResult> GetAllUsersForCombobox()
        {
            var users = await _userService.GetAllUsersForCombobox();
            return Ok(users);
        }

        [HttpPut("DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromQuery] string userId)
        {
            await _userService.DeleteUser(userId);
            return Ok();
        }

        [Authorize]
        [HttpGet("GetUserForOtp")]
        public IActionResult GetUserForOtp() => NotFound();

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
            //todo donus modelini duzgun yaz
            //if (!result)
            //    return BadRequest(new { message = result.message });
            //todo: kullaniciya ait lokasyonid yok ise forbid burada yapilabilir
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
                //_emailSender.Send(otpCode, user.Email);
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
                // Token oluşturma ve geri dönme
                TokenViewModel tokenViewModel = await _tokenService.CreateToken(user, false);
                await _userService.AddConnectSiteLog(ipAddress?.ToString(), user.Id, browserName, browserVersion, isMobile, BuildAciklama("Başarılı Giriş", userAgent));
                await _userService.IncorrectLoginNumberUpdate(user.Id, true);
                return Ok(tokenViewModel);
            }
        }

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

        [AllowAnonymous]
        [EnableRateLimiting("authentication")]
        [HttpPost("NewToken")]
        public async Task<IActionResult> NewToken(NewTokenViewModel request)
        {
            if (string.IsNullOrEmpty(request.JwtToken) || request.JwtToken.Length > 32768 ||
                string.IsNullOrEmpty(request.RefreshToken) || request.RefreshToken.Length > 256)
                return Unauthorized(new { ErrorMessage = "Geçersiz oturum." });
            ClaimsPrincipal principal;
            try
            {
                principal = new JwtSecurityTokenHandler().ValidateToken(request.JwtToken, new TokenValidationParameters
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
            }
            catch (Exception ex) when (ex is SecurityTokenException || ex is ArgumentException)
            {
                return Unauthorized(new { ErrorMessage = "Geçersiz oturum." });
            }
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

        [Authorize]
        [HttpPost("SignOut")]
        public async Task<IActionResult> SignOut([FromBody] SignOutViewModel request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            await _tokenService.RemoveToken(userId);
            await _tokenService.RemoveUserHeartBeat(userId);
            await _userService.AddConnectSiteLog(GetClientIpAddress(), userId,
                TruncateForColumn(request.BrowserInfo?.BrowserName ?? "Bilinmiyor"),
                TruncateForColumn(request.BrowserInfo?.BrowserVersion ?? "Bilinmiyor"),
                request.BrowserInfo?.IsMobile ?? false, "Başarılı Çıkış");
            return Ok();
        }

        [HttpPut("ChangePassword")]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel changePasswordViewModel)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }

            var user = await _userService.GetUserById(userId);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            //var eskiSifreHash = _userManager.PasswordHasher.HashPassword(_user, changePasswordViewModel.OldPassword);
            //if (eskiSifreHash != _user.PasswordHash)
            //    return BadRequest(new { message = "Eski şifre hatalı!" });
            //if (changePasswordViewModel.NewPassword != changePasswordViewModel.NewPasswordAgain)
            //    return BadRequest(new { message = "Yeni şifreler uyumsuz!" });
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

        [HttpPost("ChangeProfilePhoto")]
        public async Task<IActionResult> ChangeProfilePhoto([FromForm] IFormFile file)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }

            var user = await _userService.GetUserById(userId);
            var ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress;

            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            var result = await _userService.ChangeProfilePhoto(user, file);
            return Ok(result);
        }

        [HttpGet("GetProfilePhoto")]
        public async Task<IActionResult> GetProfilePhoto()
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }

            var user = await _userService.GetUserById(userId);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            return Ok(user.ProfilePhoto);
        }

        [HttpGet("DeleteProfilePhoto")]
        public async Task<IActionResult> DeleteProfilePhoto()
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }

            var user = await _userService.GetUserById(userId);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            var result = await _userService.DeleteProfilePhoto(user);
            return Ok(result);
        }

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

        [HttpPost("heartbeat")]
        public async Task<IActionResult> LogHeartbeat([FromBody] HeartbeatDto dto)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
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

        [HttpPost("page-visit")]
        public async Task<IActionResult> LogPageVisit([FromBody] PageVisitDto dto)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
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
            //_context.PageVisits.Add(new PageVisit
            //{
            //    UserId = userId,
            //    Url = dto.Url,
            //    VisitTime = dto.Timestamp
            //});
            //await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("tab-close")]
        public async Task<IActionResult> TabClose(/*[FromBody] TabCloseDto dto*/)
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            var dto = JsonConvert.DeserializeObject<TabCloseDto>(body);
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(dto.Token))
            {
                //return Unauthorized();
                return Ok();
            }

            var jwtToken = handler.ReadJwtToken(dto.Token);
            var userId = jwtToken.Claims.FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            if (userId == null)
            {
                return Ok();
                //throw new Exception("Kullanıcı bulunamadı!");
            }
            if (string.IsNullOrEmpty(dto.CurrentPage) || string.IsNullOrEmpty(userId))
            {
                return Ok();
                //return BadRequest("Invalid data.");
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

            //// Burada tabId ile birlikte kullanıcının sekme bazlı durumunu güncelle
            //// Örneğin, o tabId’yi aktif sekmeler listesinden çıkar

            //// Örnek:
            //var tabSession = await _context.UserTabSessions
            //                   .FirstOrDefaultAsync(t => t.UserId == userId && t.TabId == dto.TabId);

            //if (tabSession != null)
            //{
            //    _context.UserTabSessions.Remove(tabSession);
            //    await _context.SaveChangesAsync();
            //}

            return Ok();
        }
    }
}