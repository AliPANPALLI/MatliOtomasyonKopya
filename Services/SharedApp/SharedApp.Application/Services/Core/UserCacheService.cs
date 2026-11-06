using Authentication.Domain.DbViewModels;
using PoultryHouseScada.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PanelIdentification.Domain.Models;
using WaterLineScada.Domain.Models;
using System.Text.Json;
using MatliOtomasyon.Shared.Infrastructure.Configuration;

namespace SharedApp.Application.Services.Core
{
    /// <summary>
    /// Yetki ekranlarının ihtiyaç duyduğu, başka servislere ait listeleri (kullanıcı, makine, bölüm,
    /// kümes, su kaynağı) ilgili API'lerden çekip kullanıcı bazında 10 dakika bellekte tutar.
    /// Kullanıcı listesi Authentication API'den iç servis anahtarıyla çekilir.
    /// </summary>
    public class UserCacheService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly IMemoryCache _memoryCache;
        private readonly InternalServiceOptions _internalServiceOptions;

        /// <summary>
        /// Servisin kullandığı bellek cache'ini ve iç servis anahtarı ayarlarını alır.
        /// </summary>
        public UserCacheService(IMemoryCache memoryCache, IOptions<InternalServiceOptions> internalServiceOptions)
        {
            _memoryCache = memoryCache;
            _internalServiceOptions = internalServiceOptions.Value;
        }

        /// <summary>Kullanıcı listesinin cache anahtarı.</summary>
        private string GetCacheKeyForUser(string userId) => $"UserCache_{userId}";

        /// <summary>Makine listesinin cache anahtarı.</summary>
        private string GetCacheKeyForMakine(string userId) => $"MakineCache_{userId}";

        /// <summary>Bölüm listesinin cache anahtarı.</summary>
        private string GetCacheKeyForBolum(string userId) => $"BolumCache_{userId}";

        /// <summary>Kümes listesinin cache anahtarı.</summary>
        private string GetCacheKeyForKumes(string userId) => $"KumesCache_{userId}";

        /// <summary>Su kaynağı listesinin cache anahtarı.</summary>
        private string GetCacheKeyForSuKaynak(string userId) => $"SuKaynakCache_{userId}";

        /// <summary>Cache'teki kullanıcı listesini döner; yoksa null.</summary>
        public List<vwUser> GetUserFromUserCache(string userId)
        {
            string cacheKey = GetCacheKeyForUser(userId);
            _memoryCache.TryGetValue(cacheKey, out List<vwUser> user);
            return user;
        }

        /// <summary>Kullanıcı listesini 10 dakikalığına cache'e yazar.</summary>
        public void SetUserToUserCache(string userId, List<vwUser> users)
        {
            string cacheKey = GetCacheKeyForUser(userId);
            _memoryCache.Set(cacheKey, users, TimeSpan.FromMinutes(10));
        }

        /// <summary>
        /// Kullanıcı listesini önce cache'ten, yoksa Authentication API'den getirir ve cache'e yazar.
        /// </summary>
        public async Task<List<vwUser>> GetUsersAsync(string userId)
        {
            var users = GetUserFromUserCache(userId);
            if (users != null)
            {
                return users;
            }

            users = await GetAllUsersWithApi();
            if (users != null)
            {
                SetUserToUserCache(userId, users);
            }

            return users;
        }

        /// <summary>Cache'teki makine listesini döner; yoksa null.</summary>
        public List<Makine> GetUserFromMakineCache(string userId)
        {
            string cacheKey = GetCacheKeyForMakine(userId);
            _memoryCache.TryGetValue(cacheKey, out List<Makine> user);
            return user;
        }

        /// <summary>Makine listesini 10 dakikalığına cache'e yazar.</summary>
        public void SetUserToMakineCache(string userId, List<Makine> makines)
        {
            string cacheKey = GetCacheKeyForMakine(userId);
            _memoryCache.Set(cacheKey, makines, TimeSpan.FromMinutes(10));
        }

        /// <summary>
        /// Makine listesini önce cache'ten, yoksa PanoKimliklendirme API'den getirir ve cache'e yazar.
        /// </summary>
        public async Task<List<Makine>> GetMakineler(string userId)
        {
            var makineler = GetUserFromMakineCache(userId);
            if (makineler != null)
            {
                return makineler;
            }

            makineler = await GetAllMakineWithApi();
            if (makineler != null)
            {
                SetUserToMakineCache(userId, makineler);
            }

            return makineler;
        }

        /// <summary>Cache'teki bölüm listesini döner; yoksa null.</summary>
        public List<Bolum> GetUserFromBolumCache(string userId)
        {
            string cacheKey = GetCacheKeyForBolum(userId);
            _memoryCache.TryGetValue(cacheKey, out List<Bolum> user);
            return user;
        }

        /// <summary>Bölüm listesini 10 dakikalığına cache'e yazar.</summary>
        public void SetUserToBolumCache(string userId, List<Bolum> bolums)
        {
            string cacheKey = GetCacheKeyForBolum(userId);
            _memoryCache.Set(cacheKey, bolums, TimeSpan.FromMinutes(10));
        }

        /// <summary>
        /// Bölüm listesini önce cache'ten, yoksa PanoKimliklendirme API'den getirir ve cache'e yazar.
        /// </summary>
        public async Task<List<Bolum>> GetBolumler(string userId)
        {
            var bolumler = GetUserFromBolumCache(userId);
            if (bolumler != null)
            {
                return bolumler;
            }

            bolumler = await GetAllBolumWithApi();
            if (bolumler != null)
            {
                SetUserToBolumCache(userId, bolumler);
            }

            return bolumler;
        }

        /// <summary>Cache'teki kümes listesini döner; yoksa null.</summary>
        public List<Kumes> GetUserFromKumesCache(string userId)
        {
            string cacheKey = GetCacheKeyForKumes(userId);
            _memoryCache.TryGetValue(cacheKey, out List<Kumes> user);
            return user;
        }

        /// <summary>Kümes listesini 10 dakikalığına cache'e yazar.</summary>
        public void SetUserToKumesCache(string userId, List<Kumes> makines)
        {
            string cacheKey = GetCacheKeyForKumes(userId);
            _memoryCache.Set(cacheKey, makines, TimeSpan.FromMinutes(10));
        }

        /// <summary>
        /// Kümes listesini önce cache'ten, yoksa KumesScada API'den getirir ve cache'e yazar.
        /// </summary>
        public async Task<List<Kumes>> GetKumesler(string userId)
        {
            var kumesler = GetUserFromKumesCache(userId);
            if (kumesler != null)
            {
                return kumesler;
            }

            kumesler = await GetAllKumesWithApi();
            if (kumesler != null)
            {
                SetUserToKumesCache(userId, kumesler);
            }

            return kumesler;
        }

        /// <summary>Cache'teki su kaynağı listesini döner; yoksa null.</summary>
        public List<SuKaynak> GetUserFromSuKaynakCache(string userId)
        {
            string cacheKey = GetCacheKeyForSuKaynak(userId);
            _memoryCache.TryGetValue(cacheKey, out List<SuKaynak> user);
            return user;
        }

        /// <summary>Su kaynağı listesini 10 dakikalığına cache'e yazar.</summary>
        public void SetUserToSuKaynakCache(string userId, List<SuKaynak> sukaynaks)
        {
            string cacheKey = GetCacheKeyForSuKaynak(userId);
            _memoryCache.Set(cacheKey, sukaynaks, TimeSpan.FromMinutes(10));
        }

        /// <summary>
        /// Su kaynağı listesini önce cache'ten, yoksa SuHatScada API'den getirir ve cache'e yazar.
        /// </summary>
        public async Task<List<SuKaynak>> GetSuKaynaklar(string userId)
        {
            var suKaynaklar = GetUserFromSuKaynakCache(userId);
            if (suKaynaklar != null)
            {
                return suKaynaklar;
            }

            suKaynaklar = await GetAllSuKaynakWithApi();
            if (suKaynaklar != null)
            {
                SetUserToSuKaynakCache(userId, suKaynaklar);
            }

            return suKaynaklar;
        }

        /// <summary>
        /// Authentication API'den tüm kullanıcıları çeker. Uç kullanıcı yönetimi yetkisi istediği için
        /// istek iç servis anahtarıyla gönderilir; anahtar ayarlı değilse API isteği reddeder.
        /// </summary>
        /// <returns>Kullanıcı listesi; API başarısız cevap verirse null.</returns>
        public async Task<List<vwUser?>> GetAllUsersWithApi()
        {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrls.AuthenticationBaseUrl + "/User/GetAllUsers");
            if (!string.IsNullOrWhiteSpace(_internalServiceOptions.AuthenticationApiKey))
            {
                request.Headers.Add(InternalServiceOptions.ApiKeyHeaderName, _internalServiceOptions.AuthenticationApiKey);
            }

            using var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<vwUser?>>(result, JsonOptions);
        }

        /// <summary>
        /// PanoKimliklendirme API'den tüm bölümleri çeker.
        /// </summary>
        /// <returns>Bölüm listesi; API başarısız cevap verirse null.</returns>
        public async Task<List<Bolum?>> GetAllBolumWithApi()
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(ApiUrls.PanoKimliklendirmeBaseUrl + "/Bolum/GetAllList");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Bolum?>>(result, JsonOptions);
        }

        /// <summary>
        /// PanoKimliklendirme API'den tüm makineleri çeker.
        /// </summary>
        /// <returns>Makine listesi; API başarısız cevap verirse null.</returns>
        public async Task<List<Makine?>> GetAllMakineWithApi()
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(ApiUrls.PanoKimliklendirmeBaseUrl + "/Makine/GetAllList");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Makine?>>(result, JsonOptions);
        }

        /// <summary>
        /// KumesScada API'den tüm kümesleri çeker.
        /// </summary>
        /// <returns>Kümes listesi; API başarısız cevap verirse null.</returns>
        public async Task<List<Kumes?>> GetAllKumesWithApi()
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(ApiUrls.KumesScadaBaseUrl + "/Kumes/GetAllList");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Kumes?>>(result, JsonOptions);
        }

        /// <summary>
        /// SuHatScada API'den tüm su kaynaklarını çeker.
        /// </summary>
        /// <returns>Su kaynağı listesi; API başarısız cevap verirse null.</returns>
        public async Task<List<SuKaynak?>> GetAllSuKaynakWithApi()
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(ApiUrls.SuHatScadaBaseUrl + "/SuKaynak/GetAllList");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<SuKaynak?>>(result, JsonOptions);
        }
    }
}
