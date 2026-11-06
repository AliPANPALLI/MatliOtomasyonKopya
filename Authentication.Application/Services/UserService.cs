using Authentication.Application.Interfaces;
using Authentication.Application.ViewModels;
using Authentication.Domain.DbViewModels;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Models;
using Authentication.Domain.Models.ReportModels;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text;

namespace Authentication.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UserService(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<string> PostSms()
        {
            try
            {
                string ss = "";
                ss += "<?xml version='1.0' encoding='UTF-8'?>";
                ss += "<mainbody>";
                ss += "<header>";
                ss += "<usercode>2243341246</usercode>";
                ss += "<password>E9.wd9KF</password>";
                ss += "<startdate>27082025</startdate>";
                ss += "<starttime>1657</starttime>";
                ss += "<stopdate>27082025</stopdate>";
                ss += "<stoptime>1659</stoptime>";
                ss += "<key>0</key>";
                ss += "<appkey>E9.wd9KF</appkey>";
                ss += "</header>";
                ss += "<body>";
                //ss += "<audioid>sesid</audioid>";
                ss += "<text>Kümes 1 plc bağlantısı koptu. Sıcaklık düşük. Nem fazla.</text>\"";
                ss += "<no>905532087258</no>";
                ss += "<no>905379555164</no>";
                //ss += "<no>905551234568</no>";
                //ss += "<keys>";
                //ss += "   <keydetail>";
                //ss += "     <keyinfo>1</keyinfo>";
                //ss += "     <text>1i tuslayanlara okutulacak metin</text>";
                //ss += " </keydetail>";
                //ss += " <keydetail>";
                //ss += "     <keyinfo>2</keyinfo>";
                //ss += "   <audioid>sesid2</audioid>";
                //ss += " </keydetail>";
                //ss += "   </keys>";
                ss += "</body>";
                ss += "</mainbody>";
                var postAddress = "https://api.netgsm.com.tr/voicesms/send";
                WebClient wUpload = new WebClient();
                HttpWebRequest request = WebRequest.Create(postAddress) as HttpWebRequest;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                Byte[] bPostArray = Encoding.UTF8.GetBytes(ss);
                Byte[] bResponse = wUpload.UploadData(postAddress, "POST", bPostArray);
                Char[] sReturnChars = Encoding.UTF8.GetChars(bResponse);
                string sWebPage = new string(sReturnChars);
                return sWebPage;
            }
            catch
            {
                return "-1";
            }
        }

        public async Task<UserViewModel> CreateUser(UserViewModel userViewModel)
        {
            var isUserExist = await GetUserByName(userViewModel.UserName);
            if (isUserExist != null)
            {
                throw new Exception("Kullanıcı adı mevcut olduğundan kayıt yapılamaz");
            }
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = userViewModel.Email,
                UserName = userViewModel.UserName,
                Name = userViewModel.Name,
                Surname = userViewModel.Surname,
                PhoneNumber = userViewModel.PhoneNumber,
                Position = userViewModel.Position,
                LokasyonId = userViewModel.LokasyonId,
                ProfilePhoto = userViewModel.ProfilePhoto,
                IsLocked = userViewModel.IsLocked,
                RequiresOtp = userViewModel.RequiresOtp,
                TokenExpirationTime = (userViewModel.TokenExpirationTime == null || userViewModel.TokenExpirationTime <= 0) ? 180 : userViewModel.TokenExpirationTime,
                Tedarikci = userViewModel.Tedarikci,
                Silindi = userViewModel.Silindi,
            };
            await _userRepository.CreateUser(user, "VZ3!_g73");

            return _mapper.Map<UserViewModel>(user);
        }
        public async Task EditUser(UserViewModel userViewModel)
        {
            var user = await _userRepository.GetUserById(userViewModel.Id);
            if (user == null)
            {
                throw new Exception("Username not found");
            }
            var userModel = _mapper.Map<User>(userViewModel);

            await _userRepository.EditUser(user, userModel);
        }
        public async Task DeleteUser(string userId)
        {
            await _userRepository.DeleteUser(userId);
        }

        public async Task<User> GetUserByName(string userName)
        {
            var user = await _userRepository.GetUserByName(userName);
            return user;
            //return _mapper.Map<UserViewModel>(user);
        }

        public async Task<User> GetUserBySicilNo(string userName)
        {
            var user = await _userRepository.GetUserBySicilNo(userName);
            return user;
            //return _mapper.Map<UserViewModel>(user);
        }

        public async Task<User> GetUserById(string userId)
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }
            return user;
            //return _mapper.Map<UserViewModel>(user);
        }

        public async Task<List<vwUser>> GetAllUsers()
        {
            return await _userRepository.GetAllUsers();
        }

        public async Task<List<vwUser>> GetAllUsersForCombobox()
        {
            return await _userRepository.GetAllUsersForCombobox();
        }

        public async Task<bool> CheckPasswordAsync(User user, string password)
        {
            return await _userRepository.CheckPasswordAsync(user, password);
        }

        public async Task<string> ResetPassword(User user)
        {
            return await _userRepository.ResetPassword(user);
        }

        public async Task<bool> ChangeProfilePhoto(User user, IFormFile? formFile)
        {
            return await _userRepository.ChangeProfilePhoto(user, formFile);
        }

        public async Task<bool> DeleteProfilePhoto(User user)
        {
            return await _userRepository.DeleteProfilePhoto(user);
        }

        public async Task ChangePassword(User user, string password)
        {
            await _userRepository.ChangePassword(user, password);
        }

        public async Task AddConnectSiteLog(string netIp, string userId, string browser, string browserversion, bool isMobile, string aciklama = "")
        {
            await _userRepository.AddConnectSiteLog(netIp, userId, browser, browserversion, isMobile, aciklama);
        }

        public async Task IncorrectLoginNumberUpdate(string userId, bool isLoginSuccess)
        {
            await _userRepository.IncorrectLoginNumberUpdate(userId, isLoginSuccess);
        }

        public async Task<List<ConnectSiteLogReportModel>> GetAllUserConnectSiteLogs(DateTime baslangicZamani, DateTime bitisZamani)
        {
            var records = await _userRepository.GetAllConnectSiteLogs(baslangicZamani, bitisZamani);
            var sessions = new List<ConnectSiteLogReportModel>();

            foreach (var group in records
                        .Where(r => !string.IsNullOrEmpty(r.UserId))
                        .GroupBy(r => r.UserId))
            {
                var userLogs = group.OrderBy(r => r.Tarih).ToList();
                var openSessions = new List<ConnectSiteLogReportModel>();

                foreach (var log in userLogs)
                {
                    if (log.Aciklama == "Başarılı Giriş")
                    {
                        // Yeni bir oturum aç
                        openSessions.Add(new ConnectSiteLogReportModel
                        {
                            UserFullName = log.UserFullName,
                            Email = log.Email,
                            PhoneNumber = log.PhoneNumber,
                            LokasyonName = log.LokasyonName,
                            SirketName = log.SirketName,
                            Position = log.Position,
                            UserName = log.UserName,
                            UserId = log.UserId,
                            LoginTime = log.Tarih,
                            Browser = log.Browser,
                            NetIp = log.NetIp,
                            IsMobile = log.IsMobile,
                            ProfilePhoto = log.ProfilePhoto != null ? "data:image/png;base64," + Convert.ToBase64String(log.ProfilePhoto) : null
                        });
                    }
                    else if (log.Aciklama == "Başarılı Çıkış" || log.Aciklama == "Otomatik Çıkış (Token Süresi Doldu)")
                    {
                        // Çıkışı zaman bazlı eşle
                        var match = openSessions
                            .Where(s => s.LoginTime <= log.Tarih) // login zamanı çıkıştan önce olmalı
                            .OrderByDescending(s => s.LoginTime)  // en yakın login'i al
                            .FirstOrDefault();

                        if (match != null)
                        {
                            match.LogoutTime = log.Tarih;
                            openSessions.Remove(match);
                            sessions.Add(match);
                        }
                    }
                }

                // Açık kalan oturumlar (çıkış yapılmamış)
                sessions.AddRange(openSessions);
            }

            return sessions.OrderByDescending(x => x.LoginTime).ToList();
        }

        public async Task<List<PageVisitReportModel>> GetAllvwPageVisits()
        {
            var records = await _userRepository.GetAllvwPageVisits();

            return _mapper.Map<List<PageVisitReportModel>>(records);
        }
        public async Task AddPageVisitMain(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId)
        {
            await _userRepository.AddPageVisitMain(userId, urlLink, moduleLink, formLink, urlId, tabId);
        }

        public async Task AddPageExitMain(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId)
        {
            await _userRepository.AddPageExitMain(userId, urlLink, moduleLink, formLink, urlId, tabId);
        }   

        public async Task AddUserHeartBeat(string userId, string urlLink, string moduleLink, string? formLink, string? urlId, string? tabId)
        {
            await _userRepository.AddUserHeartBeat(userId, urlLink, moduleLink, formLink, urlId, tabId);
        }
    }
}