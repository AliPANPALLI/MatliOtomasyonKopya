using Authentication.Application.ViewModels;
using Authentication.Domain.DbViewModels;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace Authentication.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private AuthDbContext _context;

        public UserRepository(AuthDbContext context, PasswordHash passwordHash)
        {
            _context = context;
        }

        public async Task<bool> TryResetPassword(string userId, string expectedPasswordHash, string newPassword)
        {
            var hash = PasswordHash.CreateHash(newPassword);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var changedAt = DateTimeOffset.UtcNow;
            var updated = await _context.User.Where(x => x.Id == userId && x.PasswordHash == expectedPasswordHash && x.Silindi != true && x.IsLocked != true)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.PasswordHash, hash)
                    .SetProperty(x => x.IncorrectLoginNumber, (byte?)0).SetProperty(x => x.PasswordChangedAt, changedAt));
            if (updated != 1) return false;
            await _context.UserRefreshTokens.Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await _context.OneTimePassword.Where(x => x.EkleyenUserId == userId).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Aktif, (bool?)false));
            await transaction.CommitAsync();
            return true;
        }
        public async Task ChangePassword(User user, string password)
        {
            string hash = PasswordHash.CreateHash(password);

            user.PasswordHash = hash;
            user.IncorrectLoginNumber = 0;
            await _context.SaveChangesAsync();
        }

        public async Task SignIn(HttpContext httpContext, User user, bool isPersistent = false)
        {
            ClaimsIdentity identity = new ClaimsIdentity(GetUserClaims(user), CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(
              CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties() { IsPersistent = isPersistent }
            );
        }

        public async Task SignOut(HttpContext httpContext)
        { }

        public int GetCurrentUserId(HttpContext httpContext)
        {
            if (!httpContext.User.Identity.IsAuthenticated)
                return -1;

            Claim claim = httpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (claim == null)
                return -1;

            int currentUserId;

            if (!int.TryParse(claim.Value, out currentUserId))
                return -1;

            return currentUserId;
        }

        public User GetCurrentUser(HttpContext httpContext)
        {
            int currentUserId = GetCurrentUserId(httpContext);

            if (currentUserId == -1)
                return null;

            return _context.User.Find(currentUserId);
        }

        private IEnumerable<Claim> GetUserClaims(User user)
        {
            List<Claim> claims = new List<Claim>();

            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Name, user.Name));
            //claims.AddRange(this.GetUserRoleClaims(user));
            return claims;
        }

        public async Task<bool> ChangeProfilePhoto(User user, IFormFile? formFile)
        {
            try
            {
                if (formFile == null)
                {
                    user.ProfilePhoto = null;
                    await _context.SaveChangesAsync();
                }
                using (var memoryStream = new MemoryStream())
                {
                    await formFile.CopyToAsync(memoryStream);
                    using (var img = Image.FromStream(memoryStream))
                    {
                        using (var ms = new MemoryStream())
                        {
                            Image resized = ResizeImage(img, new Size(600, 480), false);
                            resized.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            user.ProfilePhoto = ms.ToArray();
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<bool> DeleteProfilePhoto(User user)
        {
            try
            {
                user.ProfilePhoto = null;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static Image ResizeImage(Image image, Size size, bool preserveAspectRatio = true)
        {
            int newWidth;
            int newHeight;
            if (preserveAspectRatio)
            {
                int originalWidth = image.Width;
                int originalHeight = image.Height;
                float percentWidth = (float)size.Width / (float)originalWidth;
                float percentHeight = (float)size.Height / (float)originalHeight;
                float percent = percentHeight < percentWidth ? percentHeight : percentWidth;
                newWidth = (int)(originalWidth * percent);
                newHeight = (int)(originalHeight * percent);
            }
            else
            {
                newWidth = size.Width;
                newHeight = size.Height;
            }
            Image newImage = new Bitmap(image, newWidth, newHeight);
            using (Graphics graphicsHandle = Graphics.FromImage(image))
            {
                graphicsHandle.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphicsHandle.DrawImage(newImage, 0, 0, newWidth, newHeight);
            }
            return newImage;
        }
        //private IEnumerable<Claim> GetUserRoleClaims(User user)
        //{
        //    List<Claim> claims = new List<Claim>();
        //    IEnumerable<int> roleIds = _context.UserRoles.Where(ur => ur.UserId == user.Id).Select(ur => ur.RoleId).ToList();

        //    if (roleIds != null)
        //    {
        //        foreach (int roleId in roleIds)
        //        {
        //            Role role = _context.Roles.Find(roleId);

        //            claims.Add(new Claim(ClaimTypes.Role, role.Code));
        //            claims.AddRange(this.GetUserPermissionClaims(role));
        //        }
        //    }

        //    return claims;
        //}

        //private IEnumerable<Claim> GetUserPermissionClaims(Role role)
        //{
        //    List<Claim> claims = new List<Claim>();
        //    IEnumerable<int> permissionIds = _context.RolePermissions.Where(rp => rp.RoleId == role.Id).Select(rp => rp.PermissionId).ToList();

        //    if (permissionIds != null)
        //    {
        //        foreach (int permissionId in permissionIds)
        //        {
        //            Permission permission = _context.Permissions.Find(permissionId);

        //            claims.Add(new Claim("Permission", permission.Code));
        //        }
        //    }

        //    return claims;
        //}

        public async Task CreateUser(User user, string password)
        {
            string hash = "";

            if (!string.IsNullOrEmpty(password))
            {
                hash = PasswordHash.CreateHash(password);
            }
            else
            {
                throw new Exception("Kullanıcı Adı veya Şifre boş bırakılamaz!");
            }

            user.PasswordHash = hash;
            await _context.User.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User> GetUserByName(string name)
        {
            var user = await _context.User.Where(x => x.UserName == name && x.Silindi != true).FirstOrDefaultAsync();
            return user;
        }

        public async Task<User> GetUserBySicilNo(string sicilNo)
        {
            var user = await _context.User.Where(x => x.SicilNo == sicilNo && x.Silindi != true).FirstOrDefaultAsync();
            return user;
        }

        public async Task<User> GetUserById(string id)
        {
            var user = await _context.User.Where(x => x.Id == id).FirstOrDefaultAsync();
            return user;
        }

        public async Task<bool> CheckPasswordAsync(User user, string password)
        {
            bool checkPass = false;

            if (!string.IsNullOrEmpty(password))
            {
                checkPass = PasswordHash.ValidatePassword(password, user.PasswordHash);
            }
            else
            {
                return false;
            }
            return checkPass;
        }

        public async Task<string> ResetPassword(User user)
        {
            string newPassword = GenerateNewPassword(8);
            await ChangePassword(user, newPassword);

            return newPassword;
        }
        public static string GenerateNewPassword(int length)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_-+=?";
            var result = new StringBuilder(length);
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] buffer = new byte[sizeof(uint)];

                for (int i = 0; i < length; i++)
                {
                    rng.GetBytes(buffer);
                    uint randomNumber = BitConverter.ToUInt32(buffer, 0);
                    result.Append(validChars[(int)(randomNumber % (uint)validChars.Length)]);
                }
            }

            return result.ToString();
        }

        public async Task<List<vwConnectSiteLog>> GetAllConnectSiteLogs(DateTime baslangicZamani, DateTime bitisZamani)
        {
            return await _context.vwConnectSiteLog.AsNoTracking().Where(x => x.Tarih >= baslangicZamani && x.Tarih <= bitisZamani).ToListAsync();
        }

        public async Task<List<vwPageVisitMain>> GetAllvwPageVisits()
        {
            return await _context.vwPageVisitMain.AsNoTracking().OrderByDescending(x => x.ZiyaretZamani).ToListAsync();
        }

        public async Task<List<vwUser>> GetAllUsers()
        {
            return await _context.vwUser.ToListAsync();
        }

        public async Task<List<vwUser>> GetAllUsersForCombobox()
        {
            return await _context.vwUser.Where(x => x.Silindi != true && x.UserName != "onder.matli" && x.UserName != "ozer.matli" && x.Tedarikci != true).ToListAsync();
        }

        public async Task DeleteUser(string userId)
        {
            var user = await GetUserById(userId);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            user.Silindi = true;
            await _context.SaveChangesAsync();
        }

        public async Task EditUser(User oldUser, User newUser)
        {
            oldUser.Email = newUser.Email;
            oldUser.UserName = newUser.UserName;
            oldUser.Name = newUser.Name;
            oldUser.Surname = newUser.Surname;
            oldUser.PhoneNumber = newUser.PhoneNumber;
            oldUser.Position = newUser.Position;
            oldUser.LokasyonId = newUser.LokasyonId;
            oldUser.ProfilePhoto = newUser.ProfilePhoto;
            oldUser.IsLocked = newUser.IsLocked;
            oldUser.IncorrectLoginNumber = newUser.IncorrectLoginNumber;
            oldUser.RequiresOtp = newUser.RequiresOtp;
            oldUser.Silindi = newUser.Silindi;
            oldUser.TokenExpirationTime = (newUser.TokenExpirationTime == null || newUser.TokenExpirationTime <= 0) ? 180 : newUser.TokenExpirationTime;
            oldUser.Tedarikci = newUser.Tedarikci;
            await _context.SaveChangesAsync();
        }

        public async Task IncorrectLoginNumberUpdate(string userId, bool isLoginSuccess)
        {
            var user = await _context.User.Where(x => x.Id == userId && x.Silindi != true).FirstOrDefaultAsync();
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            if (isLoginSuccess)
            {
                user.IncorrectLoginNumber = 0;
            }
            else
            {
                user.IncorrectLoginNumber = (byte)(user.IncorrectLoginNumber.GetValueOrDefault(0) + 1);
            }
            await _context.SaveChangesAsync();
        }
        public async Task AddConnectSiteLog(string netIp, string userId, string browser, string browserversion, bool isMobile, string aciklama = "")
        {
            try
            {
                var item = new ConnectSiteLog();
                item.LocalName = "";
                item.HostName = "";
                item.LocalIp = "";
                item.NetIp = netIp;
                item.Browser = browser;
                item.BrowserVersion = browserversion;
                item.IsMobile = isMobile;
                item.Tarih = DateTime.Now;
                //item.Page = pageurl.Replace("/", "");
                item.UserId = userId;
                item.Aciklama = aciklama;
                await _context.ConnectSiteLog.AddAsync(item);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task AddPageVisitMain(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId)
        {
            var pageVisit = new PageVisitMain
            {
                UserId = userId,
                PageName = urlLink,
                ModulLink = moduleLink,
                FormLink = formLink,
                UrlId = urlId,
                TabId = tabId,
                ZiyaretZamani = DateTime.Now
            };

            _context.PageVisitMain.Add(pageVisit);
            await _context.SaveChangesAsync();
        }

        public async Task AddPageExitMain(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId)
        {
            var visits = await _context.PageVisitMain.Where(x => x.UserId == userId && x.PageName == urlLink && x.CikisZamani == null && x.TabId == tabId).ToListAsync();

            if (!visits.Any()) return;

            var now = DateTime.Now;
            foreach (var visit in visits)
            {
                visit.CikisZamani = now;
            }
            await _context.SaveChangesAsync();
        }

        public async Task AddUserHeartBeat(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId)
        {
            try
            {
                var userHeartBeat = await _context.UserHeartBeatMain.Where(x => x.UserId == userId && x.PageName == urlLink && x.TabId == tabId).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
                if (userHeartBeat == null)
                {
                    userHeartBeat = new UserHeartBeatMain
                    {
                        UserId = userId,
                        PageName = urlLink,
                        ModulLink = moduleLink,
                        FormLink = formLink,
                        UrlId = urlId,
                        TabId = tabId,
                        LastSeen = DateTime.Now
                    };
                    await _context.UserHeartBeatMain.AddAsync(userHeartBeat);
                }
                else
                {
                    userHeartBeat.LastSeen = DateTime.Now;
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
            }
        }

        //public static string ComputeHash(string password, byte[] salt)
        //{
        //    return Convert.ToBase64String(
        //      KeyDerivation.Pbkdf2(
        //        password: password,
        //        salt: salt,
        //        prf: KeyDerivationPrf.HMACSHA1,
        //        iterationCount: 10000,
        //        numBytesRequested: 256 / 8
        //      )
        //    );
        //}

        //public static byte[] GenerateRandomSalt()
        //{
        //    byte[] salt = new byte[128 / 8];

        //    using (var rng = RandomNumberGenerator.Create())
        //        rng.GetBytes(salt);

        //    return salt;
        //}
    }
}