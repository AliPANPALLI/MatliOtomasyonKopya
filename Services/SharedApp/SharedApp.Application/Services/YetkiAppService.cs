using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using System.Net.Http.Json;
using static MatliOtomasyon.Shared.Infrastructure.Configuration.ApiUrls;
using static MatliOtomasyon.Shared.Application.Constants.ApplicationConstants;
using static MatliOtomasyon.Shared.Application.ViewModels.UserPermissionClaimViewModel;

namespace SharedApp.Application.Services
{
    public class YetkiAppService : IYetkiAppService
    {
        private readonly IYetkiRepository _yetkiRepository;
        private readonly ISirketRepository _sirketRepository;
        private readonly ILokasyonRepository _lokasyonRepository;
        private readonly IFormRepository _formRepository;
        private readonly IFormLokasyonEslemeRepository _formLokasyonEslemeRepository;
        private readonly IModulRepository _modulRepository;

        private readonly HttpClient _httpClient;
        public YetkiAppService(IYetkiRepository yetkiRepository, ISirketRepository sirketRepository, ILokasyonRepository lokasyonRepository,
            IFormRepository formRepository, IFormLokasyonEslemeRepository formLokasyonEslemeRepository, IModulRepository modulRepository, HttpClient httpClient)
        {
            _yetkiRepository = yetkiRepository;
            _sirketRepository = sirketRepository;
            _lokasyonRepository = lokasyonRepository;
            _formRepository = formRepository;
            _formLokasyonEslemeRepository = formLokasyonEslemeRepository;
            _modulRepository = modulRepository;
            _httpClient = httpClient;
        }

        public async Task<List<vwUserFormYetki>> GetUserFormYetkiListByUserId(string userId)
        {
            return await _yetkiRepository.GetUserFormYetkiListByUserId(userId);
        }

        public async Task<vwUserFormYetki> GetUserFormYetkiByUserIdAndFormLink(string userId, string formLink)
        {
            return await _yetkiRepository.GetUserFormYetkiByUserIdAndFormLink(userId, formLink);
        }

        public async Task<List<PermissionDetail>> GetPermissionDetailForPanoKimliklendirme(string userId)
        {
            List<PermissionDetail> permissionDetails = new List<PermissionDetail>();
            //var formIds = (await _formRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.ModulId == 1 /*&& (x.TopbarNesnesi ?? false)*/ && x.Silindi != true));
            var formIds = await _formRepository.GetAllvwFormsForYetkiByModulId(1);
            var userMakineYetkiModel = await _yetkiRepository.GetPanoKimlikYetkiModelForLokasyonIds(userId);
            var formLokasyonEslemeler = await _formLokasyonEslemeRepository.GetAllAsyncByPredicateWithAsNoTracking(x => formIds.Select(x => x.FormId).ToList().Contains(x.FormId) && x.Silindi != true);

            var lokasyonIds = new List<int>();
            foreach (var model in userMakineYetkiModel)
            {
                if (model.Item2 == TreeViewNodeTypes.Makine)
                {
                    var asd = await GetLokasyonIdsByMakineIdsWithApi(model.Item1);
                    lokasyonIds.AddRange(asd);
                }
                else if (model.Item2 == TreeViewNodeTypes.Bolum)
                {
                    lokasyonIds.AddRange(await GetLokasyonIdsByBolumIdsWithApi(model.Item1));
                }
                else if (model.Item2 == TreeViewNodeTypes.Lokasyon)
                {
                    lokasyonIds.AddRange(model.Item1);
                }
                else if (model.Item2 == TreeViewNodeTypes.Sirket)
                {
                    var sirketLokasyonlar = await _sirketRepository.GetLokasyonIdsBySirketIds(model.Item1);
                    lokasyonIds.AddRange(sirketLokasyonlar);
                }
            }
            //lokasyonIds = lokasyonIds.Distinct().ToList();

            foreach (var formId in formIds)
            {
                var newLokasyonIds2 = formLokasyonEslemeler.Where(y => y.FormId == formId.FormId).Select(y => y.LokasyonId).ToList();
                var newLokasyonIds = lokasyonIds.Where(x => newLokasyonIds2.Contains(x)).ToList();
                permissionDetails.Add(new PermissionDetail { ModuleLink = formId.ModulLink, FormId = formId.FormId, FormLink = formId.FormLink, LokasyonIds = newLokasyonIds });
            }
            return permissionDetails;
        }
        public async Task<List<PermissionDetail>> GetPermissionDetailForKumesScada(string userId)
        {
            List<PermissionDetail> permissionDetails = new List<PermissionDetail>();
            //var formIds = (await _formRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.ModulId == 3 /*&& (x.TopbarNesnesi ?? false)*/ && x.Silindi != true));
            var formIds = await _formRepository.GetAllvwFormsForYetkiByModulId(3);
            var userKumesYetkiModel = await _yetkiRepository.GetKumesScadaYetkiModelForLokasyonIds(userId);
            var formLokasyonEslemeler = await _formLokasyonEslemeRepository.GetAllAsyncByPredicateWithAsNoTracking(x => formIds.Select(x => x.FormId).ToList().Contains(x.FormId) && x.Silindi != true);

            var lokasyonIds = new List<int>();
            foreach (var model in userKumesYetkiModel)
            {
                if (model.Item2 == KumesHiyerarsi.Kumes)
                {
                    lokasyonIds.AddRange(await GetLokasyonIdsByKumesIdsWithApi(model.Item1));
                }
                else if (model.Item2 == KumesHiyerarsi.Lokasyon)
                {
                    lokasyonIds.AddRange(model.Item1);
                }
                else if (model.Item2 == KumesHiyerarsi.Sirket)
                {
                    var sirketLokasyonlar = await _sirketRepository.GetLokasyonIdsBySirketIds(model.Item1);
                    lokasyonIds.AddRange(sirketLokasyonlar);
                }
            }
            //lokasyonIds = lokasyonIds.Distinct().ToList();
            var kumesIds = new List<int>();
            foreach (var model in userKumesYetkiModel)
            {
                if (model.Item2 == KumesHiyerarsi.Kumes)
                {
                    kumesIds.AddRange(model.Item1);
                }
                else if (model.Item2 == KumesHiyerarsi.Lokasyon)
                {
                    kumesIds.AddRange(await GetKumesIdsByLokasyonIdsWithApi(model.Item1));
                }
                else if (model.Item2 == KumesHiyerarsi.Sirket)
                {
                    var sirketLokasyonlar = await _sirketRepository.GetLokasyonIdsBySirketIds(model.Item1);
                    kumesIds.AddRange(await GetKumesIdsByLokasyonIdsWithApi(sirketLokasyonlar));
                }
            }
            foreach (var formId in formIds)
            {
                List<int> newLokasyonIds = new();
                List<int> newKumesIds = new();
                // Formun izin verdiği lokasyonId listesini al
                var formLokasyonIds = formLokasyonEslemeler.Where(x => x.FormId == formId.FormId && x.LokasyonId > 0).Select(x => x.LokasyonId).ToList();
                newLokasyonIds = lokasyonIds.Where(x => formLokasyonIds.Contains(x)).ToList();

                // Formun izin verdiği kümesId listesini al
                var formKumesIds = formLokasyonEslemeler.Where(x => x.FormId == formId.FormId && x.KumesId > 0).Select(x => x.KumesId).ToList();
                newKumesIds = kumesIds.Where(x => formKumesIds.Contains(x)).ToList();
                if (newKumesIds.Count() > 0)
                    permissionDetails.Add(new PermissionDetail { ModuleLink = formId.ModulLink, FormId = formId.FormId, FormLink = formId.FormLink, KumesIds = newKumesIds });
                else if (newLokasyonIds.Count() > 0)
                    permissionDetails.Add(new PermissionDetail { ModuleLink = formId.ModulLink, FormId = formId.FormId, FormLink = formId.FormLink, LokasyonIds = newLokasyonIds });
            }
            return permissionDetails;
        }

        public async Task<List<PermissionDetail>> GetPermissionDetailForSuHatScada(string userId)
        {
            List<PermissionDetail> permissionDetails = new List<PermissionDetail>();
            //var formIds = (await _formRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.ModulId == 5 /*&& (x.TopbarNesnesi ?? false)*/ && x.Silindi != true));
            var formIds = await _formRepository.GetAllvwFormsForYetkiByModulId(5);
            var userSuKaynakYetkiModel = await _yetkiRepository.GetSuHatScadaYetkiModelForLokasyonIds(userId);
            var formLokasyonEslemeler = await _formLokasyonEslemeRepository.GetAllAsyncByPredicateWithAsNoTracking(x => formIds.Select(x => x.FormId).ToList().Contains(x.FormId) && x.Silindi != true);

            var cizimIds = new List<int>();
            foreach (var model in userSuKaynakYetkiModel)
            {
                if (model.Item2 == SuKaynakHiyerarsi.SuKaynak)
                {
                    cizimIds.AddRange(await GetLokasyonIdsBySuKaynakIdsWithApi(model.Item1));
                }
                else if (model.Item2 == SuKaynakHiyerarsi.Cizim)
                {
                    cizimIds.AddRange(model.Item1);
                }
                else if (model.Item2 == SuKaynakHiyerarsi.Sirket)
                {
                    var sirketLokasyonlar = await _sirketRepository.GetLokasyonIdsBySirketIds(model.Item1);
                    cizimIds.AddRange(sirketLokasyonlar);
                }
            }

            foreach (var formId in formIds)
            {
                var newLokasyonIds = cizimIds.Where(x => formLokasyonEslemeler.Where(y => y.FormId == formId.FormId).Select(y => y.CizimId).ToList().Contains(x)).ToList();
                permissionDetails.Add(new PermissionDetail { ModuleLink = formId.ModulLink, FormId = formId.FormId, FormLink = formId.FormLink, CizimIds = newLokasyonIds });
            }
            return permissionDetails;
        }

        public async Task<List<PermissionDetail>> GetPermissionDetailForEnerjiIzleme(string userId)
        {
            List<PermissionDetail> permissionDetails = new List<PermissionDetail>();
            //var formIds = (await _formRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.ModulId == 6 /*&& (x.TopbarNesnesi ?? false)*/ && x.Silindi != true));
            var formIds = await _formRepository.GetAllvwFormsForYetkiByModulId(6);
            var userEnerjiIzlemeYetkiModel = await _yetkiRepository.GetEnerjiIzlemeYetkiModelForLokasyonIds(userId);
            var formLokasyonEslemeler = await _formLokasyonEslemeRepository.GetAllAsyncByPredicateWithAsNoTracking(x => formIds.Select(x => x.FormId).ToList().Contains(x.FormId) && x.Silindi != true);

            var lokasyonIds = new List<int>();
            foreach (var model in userEnerjiIzlemeYetkiModel)
            {
                if (model.Item2 == EnerjiIzlemeHiyerarsi.Lokasyon)
                {
                    lokasyonIds.AddRange(model.Item1);
                }
                else if (model.Item2 == EnerjiIzlemeHiyerarsi.Sirket)
                {
                    var sirketLokasyonlar = await _sirketRepository.GetLokasyonIdsBySirketIds(model.Item1);
                    lokasyonIds.AddRange(sirketLokasyonlar);
                }
            }
            lokasyonIds = lokasyonIds.Distinct().ToList();

            foreach (var formId in formIds)
            {
                var newLokasyonIds = lokasyonIds.Where(x => formLokasyonEslemeler.Where(y => y.FormId == formId.FormId).Select(y => y.LokasyonId).ToList().Contains(x)).Distinct().ToList();
                permissionDetails.Add(new PermissionDetail { ModuleLink = formId.ModulLink, FormId = formId.FormId, FormLink = formId.FormLink, LokasyonIds = newLokasyonIds });
            }
            return permissionDetails;
        }
        public async Task<List<PermissionDetail>> GetPermissionDetailForSunucuScada(string userId)
        {
            List<PermissionDetail> permissionDetails = new List<PermissionDetail>();
            var formIds = await _formRepository.GetAllvwFormsForYetkiByModulId(10);
            var userLinkYetkiler = await _yetkiRepository.GetUserLinkYetkiByUserId(userId);
            var links = userLinkYetkiler.Where(x => formIds.Select(y => y.FormId).ToList().Contains(x.FormId)).Select(x => new { x.FormId, x.Link }).ToList();
            var map = links.GroupBy(x => x.FormId).ToDictionary(g => g.Key, g => g.Select(x => NormalizePath(x.Link)).Distinct().ToList());

            foreach (var f in formIds)
            {
                permissionDetails.Add(new PermissionDetail
                {
                    ModuleLink = f.ModulLink,
                    FormId = f.FormId,
                    FormLink = f.FormLink,
                    AllowedPaths = map.TryGetValue(f.FormId, out var paths) ? paths : new List<string>(),
                });
            }
            return permissionDetails;
        }

        public async Task<List<PermissionDetail>> GetPermissionDetailForTesisScada(string userId)
        {
            List<PermissionDetail> permissionDetails = new List<PermissionDetail>();
            var formIds = await _formRepository.GetAllvwFormsForYetkiByModulId(11);
            var userLinkYetkiler = await _yetkiRepository.GetUserLinkYetkiByUserId(userId);
            var links = userLinkYetkiler.Where(x => formIds.Select(y => y.FormId).ToList().Contains(x.FormId)).Select(x => new { x.FormId, x.Link }).ToList();
            var map = links.GroupBy(x => x.FormId).ToDictionary(g => g.Key, g => g.Select(x => NormalizePath(x.Link)).Distinct().ToList());

            foreach (var f in formIds)
            {
                permissionDetails.Add(new PermissionDetail
                {
                    ModuleLink = f.ModulLink,
                    FormId = f.FormId,
                    FormLink = f.FormLink,
                    AllowedPaths = map.TryGetValue(f.FormId, out var paths) ? paths : new List<string>(),
                });
            }
            return permissionDetails;
        }

        public async Task<List<PermissionDetail>> GetPermissionDetailForTopoloji(string userId)
        {
            List<PermissionDetail> permissionDetails = new List<PermissionDetail>();
            var formIds = new List<vwForm>();

            var forms10 = await _formRepository.GetAllvwFormsForYetkiByModulId(10);
            var forms11 = await _formRepository.GetAllvwFormsForYetkiByModulId(11);
            var forms13 = await _formRepository.GetAllvwFormsForYetkiByModulId(13);
            var forms14 = await _formRepository.GetAllvwFormsForYetkiByModulId(14);
            var forms15 = await _formRepository.GetAllvwFormsForYetkiByModulId(15);
            var forms16 = await _formRepository.GetAllvwFormsForYetkiByModulId(16);
            var forms17 = await _formRepository.GetAllvwFormsForYetkiByModulId(17);
            formIds.AddRange(forms10);
            formIds.AddRange(forms11);
            formIds.AddRange(forms13);
            formIds.AddRange(forms14);
            formIds.AddRange(forms15);
            formIds.AddRange(forms16);
            formIds.AddRange(forms17);
            var userLinkYetkiler = await _yetkiRepository.GetUserLinkYetkiByUserId(userId);
            var links = userLinkYetkiler.Where(x => formIds.Select(y => y.FormId).ToList().Contains(x.FormId)).Select(x => new { x.FormId, x.Link }).ToList();
            var map = links.GroupBy(x => x.FormId).ToDictionary(g => g.Key, g => g.Select(x => NormalizePath(x.Link)).Distinct().ToList());

            foreach (var f in formIds)
            {
                permissionDetails.Add(new PermissionDetail
                {
                    ModuleLink = f.ModulLink,
                    FormId = f.FormId,
                    FormLink = f.FormLink,
                    AllowedPaths = map.TryGetValue(f.FormId, out var paths) ? paths : new List<string>(),
                });
            }
            return permissionDetails;
        }
        private static string NormalizePath(string u)
        {
            if (string.IsNullOrWhiteSpace(u)) return "";
            var clean = u.Split('?')[0].Split('#')[0].Trim();
            while (clean.EndsWith("/")) clean = clean[..^1];
            if (!clean.StartsWith("/")) clean = "/" + clean;
            return clean;
        }

        public async Task<List<PermissionDetail>> GetPermissionDetailForTicketAndYonetim(string userId)
        {
            List<PermissionDetail> permissionDetails = new List<PermissionDetail>();
            //var formIds = (await _formRepository.GetAllAsyncByPredicateWithAsNoTracking(x => (x.ModulId == 4 || x.ModulId == 8) /*&& (x.TopbarNesnesi ?? false)*/ && x.Silindi != true));
            var formIds = await _formRepository.GetAllvwFormsForYetkiByModulIds(new List<int>() { 4, 8 });

            foreach (var formId in formIds)
            {
                permissionDetails.Add(new PermissionDetail { ModuleLink = formId.ModulLink, FormId = formId.FormId, FormLink = formId.FormLink, LokasyonIds = new List<int>() });
            }
            return permissionDetails;
        }

        public async Task<PermissionDetail> GetPermissionDetailForKumesScada2(string userId)
        {
            var userKumesYetkiModel = await _yetkiRepository.GetKumesScadaYetkiModelForLokasyonIds(userId);
            PermissionDetail yetkiDetay = new PermissionDetail();
            yetkiDetay.LokasyonIds = new List<int>();
            foreach (var model in userKumesYetkiModel)
            {
                if (model.Item2 == KumesHiyerarsi.Kumes)
                {
                    yetkiDetay.LokasyonIds.AddRange(await GetLokasyonIdsByKumesIdsWithApi(model.Item1));
                }
                else if (model.Item2 == KumesHiyerarsi.Lokasyon)
                {
                    yetkiDetay.LokasyonIds.AddRange(model.Item1);
                }
                else if (model.Item2 == KumesHiyerarsi.Sirket)
                {
                    var sirketLokasyonlar = await _sirketRepository.GetLokasyonIdsBySirketIds(model.Item1);
                    yetkiDetay.LokasyonIds.AddRange(sirketLokasyonlar);
                }
            }
            yetkiDetay.LokasyonIds = yetkiDetay.LokasyonIds.Distinct().ToList();
            return yetkiDetay;
        }
        public async Task<PermissionDetail> GetPermissionDetailForPanoKimliklendirme2(string userId)
        {
            var userMakineYetkiModel = await _yetkiRepository.GetPanoKimlikYetkiModelForLokasyonIds(userId);
            PermissionDetail yetkiDetay = new PermissionDetail();
            yetkiDetay.LokasyonIds = new List<int>();
            foreach (var model in userMakineYetkiModel)
            {
                if (model.Item2 == TreeViewNodeTypes.Makine)
                {
                    var asd = await GetLokasyonIdsByMakineIdsWithApi(model.Item1);
                    yetkiDetay.LokasyonIds.AddRange(asd);
                    //var bolumIdList = await _context.Makine.Where(x => makineler.Contains(x.Id) && x.Silindi != true).Select(x => x.BolumId).ToListAsync();
                    //var lokasyonList = await _context.Bolum.Where(x => bolumIdList.Contains(x.Id) && x.Silindi != true).Select(x => x.LokasyonId).ToListAsync();
                    //lokasyonIdList.AddRange(lokasyonList);
                    //yetkiDetay.LokasyonIds.AddRange(lokasyonList);
                }
                else if (model.Item2 == TreeViewNodeTypes.Bolum)
                {
                    yetkiDetay.LokasyonIds.AddRange(await GetLokasyonIdsByBolumIdsWithApi(model.Item1));
                    //lokasyonList = await _context.Bolum.Where(x => bolumler.Contains(x.Id) && x.Silindi != true).Select(x => x.LokasyonId).ToListAsync();
                    //lokasyonIdList.AddRange(lokasyonList);
                    //yetkiDetay.LokasyonIds.AddRange(lokasyonList);
                }
                else if (model.Item2 == TreeViewNodeTypes.Lokasyon)
                {
                    //lokasyonIdList.AddRange(lokasyonlar);
                    yetkiDetay.LokasyonIds.AddRange(model.Item1);
                }
                else if (model.Item2 == TreeViewNodeTypes.Sirket)
                {
                    var sirketLokasyonlar = await _sirketRepository.GetLokasyonIdsBySirketIds(model.Item1);
                    yetkiDetay.LokasyonIds.AddRange(sirketLokasyonlar);
                    //var sirketLokasyonlar = await _context.Lokasyon.Where(x => sirketler.Contains(x.SirketId) && x.Silindi != true).Select(x => x.Id).ToListAsync();
                    //lokasyonIdList.AddRange(sirketLokasyonlar);
                    //yetkiDetay.LokasyonIds.AddRange(sirketLokasyonlar);
                }
            }
            yetkiDetay.LokasyonIds = yetkiDetay.LokasyonIds.Distinct().ToList();
            return yetkiDetay;
        }

        public async Task<List<int>> GetLokasyonIdsByMakineIdsWithApi(List<int> makineIds)
        {
            // İstek yapılacak URL. Burada mikroservisin base adresini eklemeniz gerekiyor.
            string url = PanoKimliklendirmeBaseUrl + PanoKimliklendirmeUrls.GetLokasyonIdsByMakineIds;

            // Parametreleri query string olarak hazırlayın
            var query = string.Join("&", makineIds.Select(id => $"makineIds={id}"));
            var fullUrl = $"{url}?{query}";

            // GET isteği yapın
            var response = await _httpClient.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<int>>();
                return result;
            }
            else
            {
                // Hata durumunu ele alın
                throw new Exception("Mikroservisten veri alınırken bir hata oluştu.");
            }
        }

        public async Task<List<int>> GetLokasyonIdsByBolumIdsWithApi(List<int> bolumIds)
        {
            // İstek yapılacak URL. Burada mikroservisin base adresini eklemeniz gerekiyor.
            string url = PanoKimliklendirmeBaseUrl + PanoKimliklendirmeUrls.GetLokasyonIdsByBolumIds;

            // Parametreleri query string olarak hazırlayın
            var query = string.Join("&", bolumIds.Select(id => $"bolumIds={id}"));
            var fullUrl = $"{url}?{query}";

            // GET isteği yapın
            var response = await _httpClient.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<int>>();
                return result;
            }
            else
            {
                // Hata durumunu ele alın
                throw new Exception("Mikroservisten veri alınırken bir hata oluştu.");
            }
        }

        public async Task<List<int>> GetLokasyonIdsByKumesIdsWithApi(List<int> kumesIds)
        {
            // İstek yapılacak URL. Burada mikroservisin base adresini eklemeniz gerekiyor.
            string url = KumesScadaBaseUrl + KumesScadaUrls.GetLokasyonIdsByKumesIds;

            // Parametreleri query string olarak hazırlayın
            var query = string.Join("&", kumesIds.Select(id => $"kumesIds={id}"));
            var fullUrl = $"{url}?{query}";

            // GET isteği yapın
            var response = await _httpClient.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<int>>();
                return result;
            }
            else
            {
                // Hata durumunu ele alın
                throw new Exception("Mikroservisten veri alınırken bir hata oluştu.");
            }
        }

        public async Task<List<int>> GetKumesIdsByLokasyonIdsWithApi(List<int> lokasyonIds)
        {
            // İstek yapılacak URL. Burada mikroservisin base adresini eklemeniz gerekiyor.
            string url = KumesScadaBaseUrl + KumesScadaUrls.GetKumesIdsByLokasyonIds;

            // Parametreleri query string olarak hazırlayın
            var query = string.Join("&", lokasyonIds.Select(id => $"lokasyonIds={id}"));
            var fullUrl = $"{url}?{query}";

            // GET isteği yapın
            var response = await _httpClient.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<int>>();
                return result;
            }
            else
            {
                // Hata durumunu ele alın
                throw new Exception("Mikroservisten veri alınırken bir hata oluştu.");
            }
        }

        public async Task<List<int>> GetLokasyonIdsBySuKaynakIdsWithApi(List<int> suKaynakIds)
        {
            // İstek yapılacak URL. Burada mikroservisin base adresini eklemeniz gerekiyor.
            string url = SuHatScadaBaseUrl + SuHatScadaUrls.GetLokasyonIdsBySuKaynakIds;

            // Parametreleri query string olarak hazırlayın
            var query = string.Join("&", suKaynakIds.Select(id => $"suKaynakIds={id}"));
            var fullUrl = $"{url}?{query}";

            // GET isteği yapın
            var response = await _httpClient.GetAsync(fullUrl);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<int>>();
                return result;
            }
            else
            {
                // Hata durumunu ele alın
                throw new Exception("Mikroservisten veri alınırken bir hata oluştu.");
            }
        }

        public async Task<List<MenuItem>> GetAllSideBar()
        {
            List<MenuItem> sidebarMenu = new List<MenuItem>();

            var sirketler = await _sirketRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Silindi != true);

            foreach (var sirket in sirketler)
            {
                var menuItem = new MenuItem();
                menuItem.Id = sirket.Id;
                menuItem.Title = sirket.Name;
                menuItem.Active = false;
                menuItem.Icon = sirket.Name;
                menuItem.MenuIdType = MenuIdType.SirketId;

                var sirketLokasyonlar = await _lokasyonRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Silindi != true && x.SirketId == sirket.Id);
                if (sirketLokasyonlar.Count() > 0)
                {
                    menuItem.Type = "dropdown";
                    menuItem.SubItems = new List<MenuItem>();
                    foreach (var lokasyon in sirketLokasyonlar)
                    {
                        menuItem.SubItems.Add(new MenuItem { Id = lokasyon.Id, MenuIdType = MenuIdType.LokasyonId, Title = lokasyon.Name });
                    }
                }

                sidebarMenu.Add(menuItem);
            }

            var suHatCizimler2 = await _yetkiRepository.GetvwSuHatCizimSirketForYetki();
            var suHatCizimler = suHatCizimler2.GroupBy(c => new { c.SirketId, c.SirketName })
             .Select(g => new
             {
                 SirketId = g.Key.SirketId,
                 SirketName = g.Key.SirketName,
                 Locations = g.ToList()
             });
            foreach (var suHatCizim in suHatCizimler)
            {
                var menuItem = new MenuItem();
                menuItem.Id = suHatCizim.SirketId.GetValueOrDefault(0);
                menuItem.Title = suHatCizim.SirketName;
                menuItem.Active = false;
                menuItem.Icon = suHatCizim.SirketName;
                menuItem.MenuIdType = MenuIdType.SirketId;

                menuItem.Type = "dropdown";
                menuItem.SubItems = new List<MenuItem>();
                foreach (var lokasyon in suHatCizim.Locations.DistinctBy(x => x.CizimId).ToList())
                {
                    menuItem.SubItems.Add(new MenuItem { Id = lokasyon.CizimId.GetValueOrDefault(0), MenuIdType = MenuIdType.CizimId, Title = lokasyon.CizimName });
                }

                sidebarMenu.Add(menuItem);
            }
            return sidebarMenu;
        }
        public async Task<List<MenuItem>> GetAllTopBar()
        {
            var topbarMenu = new List<MenuItem>();

            var moduls = (await _modulRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Silindi != true)).OrderBy(x => x.Sira).ToList();
            var forms = (await _formRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Silindi != true)).ToList();

            foreach (var modul in moduls)
            {
                var rootForms = forms.Where(f => f.ModulId == modul.Id && f.ParentId == null).OrderBy(f => f.Sira).ToList();
                var masterForm = rootForms.FirstOrDefault(f => f.Sira == 0 || f.Name.Contains("Master"));

                if (masterForm != null && rootForms.Count == 1/*modul.TopbarNesnesi.GetValueOrDefault(false) == true*/)
                {
                    var singleMenuItem = new MenuItem
                    {
                        Id = masterForm.Id,//modulId degil formId yazildi
                        Title = modul.Name,
                        Link = modul.Link + masterForm.Link,
                        Type = "single", // veya null
                        TopbarNesnesi = true,
                        SubItems = new List<MenuItem>() // Boş liste
                    };
                    topbarMenu.Add(singleMenuItem);
                }
                else
                {
                    var menuItem = new MenuItem
                    {
                        Id = modul.Id,
                        Title = modul.Name,
                        Link = modul.Link,
                        Type = rootForms.Any() ? "dropdown" : null,
                        SubItems = rootForms.Select(root => MapFormToMenuItem(root, forms, modul.Link)).ToList(),
                    };
                    topbarMenu.Add(menuItem);
                }
            }
            return topbarMenu;
        }

        private MenuItem MapFormToMenuItem(Form form, List<Form> allForms, string modulBaseLink)
        {
            var children = allForms
                .Where(f => f.ParentId == form.Id)
                .OrderBy(f => f.Sira)
                .ToList();

            return new MenuItem
            {
                Id = form.Id,
                Title = form.Name,
                Link = modulBaseLink + form.Link,
                TopbarNesnesi = form.TopbarNesnesi.GetValueOrDefault(false),
                Type = children.Any() ? "dropdown" : null,
                SubItems = children.Select(child => MapFormToMenuItem(child, allForms, modulBaseLink)).ToList()
            };
        }
        //public async Task<List<MenuItem>> GetAllTopBar()
        //{
        //    List<MenuItem> topbarMenu = new List<MenuItem>();

        //    var moduls = (await _modulRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Silindi != true)).OrderBy(x => x.Sira);
        //    var forms = await _formRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Silindi != true);

        //    var menuItems = moduls.Select(m => new MenuItem
        //    {
        //        Id = m.Id,
        //        Title = m.Name,
        //        Link = m.Link,
        //        Type = forms.Any(f => f.ModulId == m.Id) ? "dropdown" : null,
        //        SubItems = forms
        //            .Where(f => f.ModulId == m.Id && f.Silindi != true /*&& f.TopbarNesnesi.GetValueOrDefault(false)*/).OrderBy(x => x.Sira)
        //            .Select(f => new MenuItem
        //            {
        //                Id = f.Id,
        //                Title = f.Name,
        //                Link = m.Link + f.Link,
        //                TopbarNesnesi = f.TopbarNesnesi.GetValueOrDefault(false)
        //            }).ToList()
        //    }).ToList();
        //    topbarMenu.AddRange(menuItems);

        //    return topbarMenu;
        //}

        public async Task<List<string>> SendEmailForScheduledMaintenance(bool panoKimlikKullanicisiMi, bool kumesScadaKullanicisiMi, bool suHatScadaKullanicisiMi)
        {
            List<string> toList = new List<string>();

            try
            {
                if (panoKimlikKullanicisiMi)
                {
                    var userMakineYetkililer = await _yetkiRepository.GetUserMakineYetkiForSendingMail();
                    toList.AddRange(userMakineYetkililer.Select(x => x.Email).ToList());
                }
                if (kumesScadaKullanicisiMi)
                {
                    var userKumesYetkililer = await _yetkiRepository.GetUserKumesYetkiForSendingMail();
                    toList.AddRange(userKumesYetkililer.Select(x => x.Email).ToList());
                }
                if (suHatScadaKullanicisiMi)
                {
                    var userSuKaynakYetkililer = await _yetkiRepository.GetUserSuKaynakYetkiForSendingMail();
                    toList.AddRange(userSuKaynakYetkililer.Select(x => x.Email).ToList());
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return toList.Distinct().ToList();
        }

        public async Task<bool> GetvwUserEnerjiIzlemeYetkiByUserIdAndLokasyonId(string userId, int lokasyonId)
        {
            var userEnerjiIzlemeYetkiModel = await _yetkiRepository.GetEnerjiIzlemeYetkiModelOnaylayabilirForLokasyonIds(userId);
            var lokasyonIds = new List<int>();
            foreach (var model in userEnerjiIzlemeYetkiModel)
            {
                if (model.Item2 == EnerjiIzlemeHiyerarsi.Lokasyon)
                {
                    lokasyonIds.AddRange(model.Item1);
                }
                else if (model.Item2 == EnerjiIzlemeHiyerarsi.Sirket)
                {
                    var sirketLokasyonlar = await _sirketRepository.GetLokasyonIdsBySirketIds(model.Item1);
                    lokasyonIds.AddRange(sirketLokasyonlar);
                }
            }
            //lokasyonIds = lokasyonIds.Distinct().ToList();
            if (lokasyonIds.Contains(lokasyonId))
            {
                return true;
            }
            return false;
        }

        public async Task<UserLinkYetki?> GetUserLinkYetkiByUserIdAndLink(string userId, string link)
        {
            var userLinkYetki = await _yetkiRepository.GetUserLinkYetkiByUserIdAndLink(userId, link);
            return userLinkYetki;
        }
    }
    //public async Task<List<Makine>> GetMakinesWithApi()
    //  {
    //      string url = PanoKimliklendirmeBaseUrl + PanoKimliklendirmeUrls.GetMakines;

    //      // Parametreleri query string olarak hazırlayın

    //      // GET isteği yapın
    //      var response = await _httpClient.GetAsync(url);

    //      if (response.IsSuccessStatusCode)
    //      {
    //          var result = await response.Content.ReadFromJsonAsync<List<Makine>>();
    //          return result;
    //      }
    //      else
    //      {
    //          // Hata durumunu ele alın
    //          throw new Exception("Mikroservisten veri alınırken bir hata oluştu.");
    //      }
    //  }

}