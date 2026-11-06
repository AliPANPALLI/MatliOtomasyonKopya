using Authentication.Domain.DbViewModels;
using KumesScada.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using PanoKimliklendirme.Domain.Models;
using SharedKernel;
using SuHatScada.Domain.Models;
using System.Text.Json;

namespace SharedApp.Application.Services.Core
{
    public class UserCacheService
    {
        private readonly IMemoryCache _memoryCache;

        public UserCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        private string GetCacheKeyForUser(string userId) => $"UserCache_{userId}";
        private string GetCacheKeyForMakine(string userId) => $"MakineCache_{userId}";
        private string GetCacheKeyForBolum(string userId) => $"BolumCache_{userId}";
        private string GetCacheKeyForKumes(string userId) => $"KumesCache_{userId}";
        private string GetCacheKeyForSuKaynak(string userId) => $"SuKaynakCache_{userId}";

        public List<vwUser> GetUserFromUserCache(string userId)
        {
            string cacheKey = GetCacheKeyForUser(userId);
            _memoryCache.TryGetValue(cacheKey, out List<vwUser> user);
            return user;
        }

        public void SetUserToUserCache(string userId, List<vwUser> users)
        {
            string cacheKey = GetCacheKeyForUser(userId);
            _memoryCache.Set(cacheKey, users, TimeSpan.FromMinutes(10));
        }

        public async Task<List<vwUser>> GetUsersAsync(string userId)
        {
            var cachedUsers = new List<vwUser>();
            string cacheKey = GetCacheKeyForUser(userId);

            // Cache'ten kontrol et
            var users = GetUserFromUserCache(userId);
            if (users != null)
            {
                return users;
            }

            // Cache'te yoksa API'den çek ve ekle
            users = await GetAllUsersWithApi();
            if (users != null)
            {
                SetUserToUserCache(userId, users);
            }

            return users;
        }

        public List<Makine> GetUserFromMakineCache(string userId)
        {
            string cacheKey = GetCacheKeyForMakine(userId);
            _memoryCache.TryGetValue(cacheKey, out List<Makine> user);
            return user;
        }

        public void SetUserToMakineCache(string userId, List<Makine> makines)
        {
            string cacheKey = GetCacheKeyForUser(userId);
            _memoryCache.Set(cacheKey, makines, TimeSpan.FromMinutes(10));
        }

        public async Task<List<Makine>> GetMakineler(string userId)
        {
            var cachedUsers = new List<Makine>();
            string cacheKey = GetCacheKeyForMakine(userId);

            // Cache'ten kontrol et
            var makineler = GetUserFromMakineCache(userId);
            if (makineler != null)
            {
                return makineler;
            }

            // Cache'te yoksa API'den çek ve ekle
            makineler = await GetAllMakineWithApi();
            if (makineler != null)
            {
                SetUserToMakineCache(userId, makineler);
            }

            return makineler;
        }

        public List<Bolum> GetUserFromBolumCache(string userId)
        {
            string cacheKey = GetCacheKeyForBolum(userId);
            _memoryCache.TryGetValue(cacheKey, out List<Bolum> user);
            return user;
        }

        public void SetUserToBolumCache(string userId, List<Bolum> bolums)
        {
            string cacheKey = GetCacheKeyForBolum(userId);
            _memoryCache.Set(cacheKey, bolums, TimeSpan.FromMinutes(10));
        }

        public async Task<List<Bolum>> GetBolumler(string userId)
        {
            var cachedUsers = new List<Bolum>();
            string cacheKey = GetCacheKeyForBolum(userId);

            // Cache'ten kontrol et
            var bolumler = GetUserFromBolumCache(userId);
            if (bolumler != null)
            {
                return bolumler;
            }

            // Cache'te yoksa API'den çek ve ekle
            bolumler = await GetAllBolumWithApi();
            if (bolumler != null)
            {
                SetUserToBolumCache(userId, bolumler);
            }

            return bolumler;
        }

        public List<Kumes> GetUserFromKumesCache(string userId)
        {
            string cacheKey = GetCacheKeyForKumes(userId);
            _memoryCache.TryGetValue(cacheKey, out List<Kumes> user);
            return user;
        }

        public void SetUserToKumesCache(string userId, List<Kumes> makines)
        {
            string cacheKey = GetCacheKeyForKumes(userId);
            _memoryCache.Set(cacheKey, makines, TimeSpan.FromMinutes(10));
        }

        public async Task<List<Kumes>> GetKumesler(string userId)
        {
            var cachedUsers = new List<Kumes>();
            string cacheKey = GetCacheKeyForKumes(userId);

            // Cache'ten kontrol et
            var kumesler = GetUserFromKumesCache(userId);
            if (kumesler != null)
            {
                return kumesler;
            }

            // Cache'te yoksa API'den çek ve ekle
            kumesler = await GetAllKumesWithApi();
            if (kumesler != null)
            {
                SetUserToKumesCache(userId, kumesler);
            }

            return kumesler;
        }

        public List<SuKaynak> GetUserFromSuKaynakCache(string userId)
        {
            string cacheKey = GetCacheKeyForSuKaynak(userId);
            _memoryCache.TryGetValue(cacheKey, out List<SuKaynak> user);
            return user;
        }

        public void SetUserToSuKaynakCache(string userId, List<SuKaynak> sukaynaks)
        {
            string cacheKey = GetCacheKeyForSuKaynak(userId);
            _memoryCache.Set(cacheKey, sukaynaks, TimeSpan.FromMinutes(10));
        }

        public async Task<List<SuKaynak>> GetSuKaynaklar(string userId)
        {
            var cachedUsers = new List<SuKaynak>();
            string cacheKey = GetCacheKeyForSuKaynak(userId);

            // Cache'ten kontrol et
            var suKaynaklar = GetUserFromSuKaynakCache(userId);
            if (suKaynaklar != null)
            {
                return suKaynaklar;
            }

            // Cache'te yoksa API'den çek ve ekle
            suKaynaklar = await GetAllSuKaynakWithApi();
            if (suKaynaklar != null)
            {
                SetUserToSuKaynakCache(userId, suKaynaklar);
            }

            return suKaynaklar;
        }
        public async Task<List<vwUser?>> GetAllUsersWithApi()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(ApiUrls.AuthenticationBaseUrl + "/User/GetAllUsers")
                    };

                    // Parametreleri Query String'e eklemek için
                    var uriBuilder = new UriBuilder(request.RequestUri);
                    request.RequestUri = uriBuilder.Uri;

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var users = JsonSerializer.Deserialize<List<vwUser?>>(result, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return users;
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
        }

        public async Task<List<Bolum?>> GetAllBolumWithApi()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(ApiUrls.PanoKimliklendirmeBaseUrl + "/Bolum/GetAllList")
                    };

                    // Parametreleri Query String'e eklemek için
                    var uriBuilder = new UriBuilder(request.RequestUri);
                    request.RequestUri = uriBuilder.Uri;

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var bolumler = JsonSerializer.Deserialize<List<Bolum?>>(result, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return bolumler;
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
        }

        public async Task<List<Makine?>> GetAllMakineWithApi()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(ApiUrls.PanoKimliklendirmeBaseUrl + "/Makine/GetAllList")
                    };

                    // Parametreleri Query String'e eklemek için
                    var uriBuilder = new UriBuilder(request.RequestUri);
                    request.RequestUri = uriBuilder.Uri;

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var makineler = JsonSerializer.Deserialize<List<Makine?>>(result, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return makineler;
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
        }

        public async Task<List<Kumes?>> GetAllKumesWithApi()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(ApiUrls.KumesScadaBaseUrl + "/Kumes/GetAllList")
                    };

                    // Parametreleri Query String'e eklemek için
                    var uriBuilder = new UriBuilder(request.RequestUri);
                    request.RequestUri = uriBuilder.Uri;

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var kumesler = JsonSerializer.Deserialize<List<Kumes?>>(result, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return kumesler;
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
        }

        public async Task<List<SuKaynak?>> GetAllSuKaynakWithApi()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(ApiUrls.SuHatScadaBaseUrl + "/SuKaynak/GetAllList")
                    };

                    // Parametreleri Query String'e eklemek için
                    var uriBuilder = new UriBuilder(request.RequestUri);
                    request.RequestUri = uriBuilder.Uri;

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var suKaynaklar = JsonSerializer.Deserialize<List<SuKaynak?>>(result, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return suKaynaklar;
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
        }
    }
}