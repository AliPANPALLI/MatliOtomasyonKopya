using Authentication.Application.Interfaces;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Models;
using SharedApp.Domain.Models;
using System.Text.Json;
using static MatliOtomasyon.Shared.Application.Constants.ApplicationConstants;
using static MatliOtomasyon.Shared.Infrastructure.Configuration.ApiUrls;

namespace Authentication.Application.Services
{
    public class TwoFactorService : ITwoFactorService
    {
        private readonly ITwoFactorRepository _twoFactorRepository;
        private readonly HttpClient _httpClient;

        public TwoFactorService(ITwoFactorRepository twoFactorRepository, HttpClient httpClient)
        {
            _twoFactorRepository = twoFactorRepository;
            _httpClient = httpClient;
        }
        public async Task<bool> CheckOtpRequired(User user, string ipAdres)
        {
            var otpDomainDisiParam = await GetGenelParametrelerDegerByNameWithApi(OtpParametreler.OtpDomainDisi);
            var otpDomainDisi = otpDomainDisiParam?.Deger != "0";

            var otpDomainIciParam = await GetGenelParametrelerDegerByNameWithApi(OtpParametreler.OtpDomainIci);
            var otpDomainIci = otpDomainIciParam?.Deger != "0";

            var otpZorunluParam = await GetGenelParametrelerDegerByNameWithApi(OtpParametreler.OtpZorunlu);
            var otpZorunlu = otpZorunluParam?.Deger != "0";

            var userOtpZorunlu = user.RequiresOtp.GetValueOrDefault(false);
            //OTP Kontrol başlangıç;    
            // IP boş veya null olmasın
            if (string.IsNullOrWhiteSpace(ipAdres))
                ipAdres = "";

            // Dahili IP kontrolü (10.x.x.x, 172.16.x.x, 192.168.x.x)
            bool isInternalIp = ipAdres.StartsWith("10.")
                       || ipAdres.StartsWith("172.16.")
                       || ipAdres.StartsWith("192.168.")
                       || ipAdres == "::1"  // localhost IPv6
                       || ipAdres == "127.0.0.1"; // localhost IPv4
            // Eğer dış IP'den giriş varsa ve domain dışı OTP aktifse
            bool otpDomainDisiGerekiyor = otpDomainDisi && !isInternalIp;

            if (user.NeverSendOTP.GetValueOrDefault(false))
                return false;
            // Kullanıcıya özel OTP zorunluluğu
            if (otpZorunlu || userOtpZorunlu || otpDomainDisiGerekiyor || (otpDomainIci && isInternalIp))
                return true;
            return false;

            //var userOTPEnabled =
            //     otpDomainIci
            //     || ipAdres.Substring(0, 2) != "10"
            //     || ipAdres == "10.1.6.20" || ipAdres == "172.16.37.1";

            //if ((otpDomainDisi && userOTPEnabled && user.RequiresOtp.GetValueOrDefault(false)) || otpZorunlu)
            //    return true;
            //else
            //    return false;
        }

        //public async Task<bool> CheckOtpRequired(User user, string ipAdres)
        //{
        //    var otpDomainDisiParam = await GetGenelParametrelerDegerByNameWithApi(OtpParametreler.OtpDomainDisi);
        //    var otpDomainDisi = otpDomainDisiParam == null ? true : otpDomainDisiParam.Deger != null ? otpDomainDisiParam.Deger == "1" ? true : false : true;

        //    var otpDomainIciParam = await GetGenelParametrelerDegerByNameWithApi(OtpParametreler.OtpDomainIci);
        //    var otpDomainIci = otpDomainIciParam == null ? true : otpDomainIciParam.Deger != null ? otpDomainIciParam.Deger == "1" ? true : false : true;

        //    var otpZorunluParam = await GetGenelParametrelerDegerByNameWithApi(OtpParametreler.OtpZorunlu);
        //    var otpZorunlu = otpDomainDisiParam == null ? true : otpZorunluParam.Deger != null ? otpZorunluParam.Deger == "1" ? true : false : true;
        //    var userOtpZorunlu = user.RequiresOtp.GetValueOrDefault(false);

        //    var userOTPEnabled = otpDomainIci || ipAdres.Substring(0, 2) != "10" || ipAdres == "10.1.6.20" || ipAdres == "172.16.37.1";

        //    if ((otpDomainDisi && userOTPEnabled && user.RequiresOtp.GetValueOrDefault(false)) || otpZorunlu || userOtpZorunlu)
        //        return true;
        //    else
        //        return false;
        //}

        public async Task<string> GetCodeVerification(string userId, string ipAdres)
        {
            return await _twoFactorRepository.GetCodeVerification(userId, ipAdres);
        }

        public async Task<bool> VerifyOtpCode(string userId, string userOtpCode)
        {
            return await _twoFactorRepository.VerifyOtpCode(userId, userOtpCode);
        }

        public async Task<GenelParametreler?> GetGenelParametrelerDegerByNameWithApi(string parametreName)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(SharedAppBaseUrl + SharedAppUrls.GetGenelParametrelerDegerByName)
                    };

                    // Parametreleri Query String'e eklemek için
                    var uriBuilder = new UriBuilder(request.RequestUri);
                    var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                    query["genelParametreName"] = parametreName;
                    uriBuilder.Query = query.ToString();
                    request.RequestUri = uriBuilder.Uri;

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var genelParametre = JsonSerializer.Deserialize<GenelParametreler?>(result, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return genelParametre;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //TODO:silinecek
            //var response = await _httpClient.GetAsync($"http://localhost:44339/api/GenelParametreler/GetGenelParametrelerDegerByName/{parametreName}");
            //response.EnsureSuccessStatusCode();
            //var jsonData = JsonConvert.SerializeObject(parametreName);
            //var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            //// Diğer API'ye POST isteği gönderiyoruz.
            //var response = await _httpClient.get("http://otherapi.com/api/data", content);

            //var responseBody = await response.Content.ReadAsStringAsync();
            //var permissions = JsonSerializer.Deserialize<GenelParametreler>(responseBody);
            //return permissions;
        }
    }
}