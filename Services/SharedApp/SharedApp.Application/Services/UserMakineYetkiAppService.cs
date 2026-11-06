using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using PanelIdentification.Domain.Models;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using System.Net.Http.Json;
using static MatliOtomasyon.Shared.Infrastructure.Configuration.ApiUrls;
using Bolum = PanelIdentification.Domain.Models.Bolum;

namespace SharedApp.Application.Services
{
    public class UserMakineYetkiAppService : AppService<UserMakineYetki, UserMakineYetkiViewModel>, IUserMakineYetkiAppService
    {
        private readonly UserCacheService _userCacheService;
        private readonly IUserMakineYetkiRepository _userMakineYetkiRepository;
        private readonly ILokasyonRepository _lokasyonRepository;
        private readonly ISirketRepository _sirketRepository;
        private readonly HttpClient _httpClient;

        public UserMakineYetkiAppService(IMapper mapper, UserCacheService userCacheService, IUserMakineYetkiRepository userMakineYetkiRepository, ILokasyonRepository lokasyonRepository, ISirketRepository sirketRepository, HttpClient httpClient) : base(mapper, userMakineYetkiRepository)
        {
            _userMakineYetkiRepository = userMakineYetkiRepository;
            _userCacheService = userCacheService;
            _lokasyonRepository = lokasyonRepository;
            _sirketRepository = sirketRepository;
            _httpClient = httpClient;
        }

        public async Task AddUserMakineYetki(List<string> secilenKullanicilar, List<int> secilenMakineler, List<int> secilenBolumler, List<int> secilenLokasyonlar, List<int> secilenSirketler, bool okumaYetkisi, bool yazmaYetkisi, bool sorumluMu, bool silindi, bool aktif)
        {
            var makineler = await GetMakinesWithApi();
            var bolumler = await GetBolumsWithApi();
            var lokasyonlar = await _lokasyonRepository.GetAllAsyncByPredicate(x => x.Silindi != true).ToListAsync();

            var userMakineYetkiler = await _userMakineYetkiRepository.GetAllListAsync();
            var yetkiKayitlari = new List<UserMakineYetki>();
            foreach (var userId in secilenKullanicilar)
            {
                var userMakineYetkiKayitlar = userMakineYetkiler.Where(x => x.UserId == userId).ToList();
                // Şirket bazında yetkilendirme
                foreach (var sirketId in secilenSirketler)
                {
                    //eger kayit varsa kaydetmemeli
                    var kayitVarMi = userMakineYetkiKayitlar.Where(x => x.SirketId == sirketId).FirstOrDefault();
                    if (kayitVarMi == null)
                    {
                        yetkiKayitlari.Add(new UserMakineYetki
                        {
                            UserId = userId,
                            SirketId = sirketId,
                            LokasyonId = 0,
                            BolumId = 0,
                            MakineId = 0,
                            Read = okumaYetkisi,
                            Write = yazmaYetkisi,
                            SorumluMu = sorumluMu,
                            Silindi = false,
                            Aktif = true,
                            SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null
                        });
                    }
                    else
                    {
                        kayitVarMi.Read = okumaYetkisi;
                        kayitVarMi.Write = yazmaYetkisi;
                        kayitVarMi.SorumluMu = sorumluMu;
                        if (!kayitVarMi.Silindi.GetValueOrDefault(true))
                        {
                            kayitVarMi.Silindi = false;
                            kayitVarMi.SilinmeZamani = null;
                        }
                        if (!kayitVarMi.Aktif.GetValueOrDefault(true))
                        {
                            kayitVarMi.Aktif = false;
                        }
                    }
                }

                // Lokasyon bazında yetkilendirme
                foreach (var lokasyonId in secilenLokasyonlar)
                {
                    var sirketeAitLokasyonMu = lokasyonlar.Where(x => x.Id == lokasyonId).FirstOrDefault();
                    if (sirketeAitLokasyonMu != null)
                    {
                        if (!secilenSirketler.Contains(sirketeAitLokasyonMu.SirketId))// Eğer üst seviyede şirket yetkilendirilmemişse
                        {
                            //eger kayit varsa veya ust kaydi varsa kaydetmemeli
                            var kayitVarMi = userMakineYetkiKayitlar.Where(x => x.LokasyonId == lokasyonId).FirstOrDefault();
                            if (kayitVarMi == null)
                            {
                                kayitVarMi = userMakineYetkiKayitlar.Where(x => x.SirketId == sirketeAitLokasyonMu.SirketId && x.Silindi != true && x.Aktif != false).FirstOrDefault();
                                if (kayitVarMi == null)
                                {
                                    yetkiKayitlari.Add(new UserMakineYetki
                                    {
                                        UserId = userId,
                                        SirketId = 0,
                                        LokasyonId = lokasyonId,
                                        BolumId = 0,
                                        MakineId = 0,
                                        Read = okumaYetkisi,
                                        Write = yazmaYetkisi,
                                        SorumluMu = sorumluMu,
                                        Silindi = false,
                                        Aktif = true,
                                        SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null
                                    });
                                }
                            }
                            else
                            {
                                kayitVarMi.Read = okumaYetkisi;
                                kayitVarMi.Write = yazmaYetkisi;
                                sorumluMu = sorumluMu;
                                if (!kayitVarMi.Silindi.GetValueOrDefault(true))
                                {
                                    kayitVarMi.Silindi = false;
                                    kayitVarMi.SilinmeZamani = null;
                                }
                                if (!kayitVarMi.Aktif.GetValueOrDefault(true))
                                {
                                    kayitVarMi.Aktif = false;
                                }
                            }
                        }
                    }
                }

                // Bölüm bazında yetkilendirme
                foreach (var bolumId in secilenBolumler)
                {
                    var lokasyonaAitBolumMu = bolumler.Where(x => x.Id == bolumId).FirstOrDefault();
                    if (lokasyonaAitBolumMu != null)
                    {
                        if (!secilenLokasyonlar.Contains(lokasyonaAitBolumMu.LokasyonId))
                        { //eger kayit varsa veya ust kaydi varsa kaydetmemeli
                            var kayitVarMi = userMakineYetkiKayitlar.Where(x => x.BolumId == bolumId).FirstOrDefault();
                            if (kayitVarMi == null)
                            {
                                kayitVarMi = userMakineYetkiKayitlar.Where(x => x.LokasyonId == lokasyonaAitBolumMu.LokasyonId && x.Silindi != true && x.Aktif != false).FirstOrDefault();
                                if (kayitVarMi == null)
                                {
                                    var lokasyon = lokasyonlar.Where(x => x.Id == lokasyonaAitBolumMu.LokasyonId).FirstOrDefault();//todo silindi kontrolu eklenebilir.
                                    kayitVarMi = userMakineYetkiKayitlar.Where(x => x.SirketId == lokasyon.SirketId && x.Silindi != true && x.Aktif != false).FirstOrDefault();
                                    if (kayitVarMi == null)
                                    {
                                        yetkiKayitlari.Add(new UserMakineYetki
                                        {
                                            UserId = userId,
                                            SirketId = 0,
                                            LokasyonId = 0,
                                            BolumId = bolumId,
                                            MakineId = 0,
                                            Read = okumaYetkisi,
                                            Write = yazmaYetkisi,
                                            SorumluMu = sorumluMu,
                                            Silindi = false,
                                            Aktif = true,
                                            SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null
                                        });
                                    }
                                }
                            }
                            else
                            {
                                kayitVarMi.Read = okumaYetkisi;
                                kayitVarMi.Write = yazmaYetkisi;
                                kayitVarMi.SorumluMu = sorumluMu;
                                if (!kayitVarMi.Silindi.GetValueOrDefault(true))
                                {
                                    kayitVarMi.Silindi = false;
                                    kayitVarMi.SilinmeZamani = null;
                                }
                                if (!kayitVarMi.Aktif.GetValueOrDefault(true))
                                {
                                    kayitVarMi.Aktif = false;
                                }
                            }
                        }
                    }
                }

                // Makine bazında yetkilendirme
                foreach (var makineId in secilenMakineler)
                {
                    var bolumeAitMakineMi = makineler.Where(x => x.Id == makineId).FirstOrDefault();
                    if (bolumeAitMakineMi != null)
                    {
                        if (!secilenBolumler.Contains(bolumeAitMakineMi.BolumId))
                        { //eger kayit varsa veya ust kaydi varsa kaydetmemeli
                            var kayitVarMi = userMakineYetkiKayitlar.Where(x => x.MakineId == makineId).FirstOrDefault();
                            if (kayitVarMi == null)
                            {
                                kayitVarMi = userMakineYetkiKayitlar.Where(x => x.BolumId == bolumeAitMakineMi.BolumId && x.Silindi != true && x.Aktif != false).FirstOrDefault();
                                if (kayitVarMi == null)
                                {
                                    var bolum = bolumler.Where(x => x.Id == bolumeAitMakineMi.BolumId).FirstOrDefault();//todo silindi kontrolu eklenebilir.
                                    kayitVarMi = userMakineYetkiKayitlar.Where(x => x.LokasyonId == bolum.LokasyonId && x.Silindi != true && x.Aktif != false).FirstOrDefault();
                                    if (kayitVarMi == null)
                                    {
                                        var lokasyon = lokasyonlar.Where(x => x.Id == bolum.LokasyonId).FirstOrDefault();//todo silindi kontrolu eklenebilir.
                                        kayitVarMi = userMakineYetkiKayitlar.Where(x => x.SirketId == lokasyon.SirketId && x.Silindi != true && x.Aktif != false).FirstOrDefault();
                                        if (kayitVarMi == null)
                                        {
                                            yetkiKayitlari.Add(new UserMakineYetki
                                            {
                                                UserId = userId,
                                                SirketId = 0,
                                                LokasyonId = 0,
                                                BolumId = 0,
                                                MakineId = makineId,
                                                Read = okumaYetkisi,
                                                Write = yazmaYetkisi,
                                                SorumluMu = sorumluMu,
                                                Silindi = false,
                                                Aktif = true,
                                                SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null
                                            });
                                        }
                                    }
                                }
                            }
                            else
                            {
                                kayitVarMi.Read = okumaYetkisi;
                                kayitVarMi.Write = yazmaYetkisi;
                                kayitVarMi.SorumluMu = sorumluMu;
                                if (!kayitVarMi.Silindi.GetValueOrDefault(true))
                                {
                                    kayitVarMi.Silindi = false;
                                    kayitVarMi.SilinmeZamani = null;
                                }
                                if (!kayitVarMi.Aktif.GetValueOrDefault(true))
                                {
                                    kayitVarMi.Aktif = false;
                                }
                            }
                        }
                    }
                }
            }
            await _userMakineYetkiRepository.AddRangeAsync(yetkiKayitlari);
            await _userMakineYetkiRepository.SaveChangesAsync();
        }

        public async Task EditUserMakineYetki(UserMakineYetkiViewModel userMakineYetkiViewModel)
        {
            var userMakineYetki = await _userMakineYetkiRepository.GetAllListAsync();

            //if (userMakineYetki == null)
            //{
            //    var addModel = new UserMakineYetki
            //    {
            //        UserId = userMakineYetkiViewModel.UserId,
            //        MakineId = userMakineYetkiViewModel.FormId,
            //        Okuma = userMakineYetkiViewModel.Okuma,
            //        Yazma = userMakineYetkiViewModel.Yazma,
            //        Degistirme = userMakineYetkiViewModel.Degistirme,
            //        Silme = userMakineYetkiViewModel.Silme,
            //        Silindi = false
            //    };
            //    await _userFormYetkiRepository.AddAsync(addModel);
            //}
            //else
            //{
            //    _userFormYetkiRepository.Attach(userMakineYetki);
            //    userMakineYetki.Okuma = userMakineYetki.Okuma;
            //    userMakineYetki.Yazma = userMakineYetkiViewModel.Yazma;
            //    userMakineYetki.Degistirme = userMakineYetkiViewModel.Degistirme;
            //    userMakineYetki.Silme = userMakineYetkiViewModel.Silme;
            //    userMakineYetki.Silindi = userMakineYetkiViewModel.Silindi;
            //    userMakineYetki.SilinmeZamani = userMakineYetkiViewModel.Silindi.GetValueOrDefault(false) ? DateTime.Now : null;
            //}
            //await _userFormYetkiRepository.SaveChangesAsync();
        }

        public async Task<List<UserMakineYetki>> GetAllUserMakineYetkiByUserId(string userId)
        {
            return await _userMakineYetkiRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.UserId == userId);
        }

        public async Task<List<UserMakineYetkiViewModel>> GetAllUserMakineYetkis(string userId)
        {
            var userMakineYetkiler = await _userMakineYetkiRepository.GetAllListAsync();

            var kullanicilar = await _userCacheService.GetUsersAsync(userId);
            var makineler = await _userCacheService.GetMakineler(userId);
            var bolumler = await _userCacheService.GetBolumler(userId);
            var lokasyonlar = await _lokasyonRepository.GetAllListAsync();
            var sirketler = await _sirketRepository.GetAllListAsync();

            // Kullanıcı bilgilerini eşleştir ve döndür
            return userMakineYetkiler.Select(umy => new UserMakineYetkiViewModel
            {
                Id = umy.Id,
                UserId = kullanicilar.FirstOrDefault(u => u.Id == umy.UserId)?.Id,
                UserName = kullanicilar.FirstOrDefault(u => u.Id == umy.UserId)?.UserName,
                FullName = kullanicilar.FirstOrDefault(u => u.Id == umy.UserId)?.Name + " " + kullanicilar.FirstOrDefault(u => u.Id == umy.UserId)?.Surname,
                MakineId = umy.MakineId,
                MakineName = makineler.FirstOrDefault(m => m.Id == umy.MakineId)?.Name,
                BolumId = umy.BolumId,
                BolumName = bolumler.FirstOrDefault(f => f.Id == umy.BolumId)?.Name,
                LokasyonId = umy.LokasyonId,
                LokasyonName = lokasyonlar.FirstOrDefault(m => m.Id == umy.LokasyonId)?.Name,
                SirketId = umy.SirketId,
                SirketName = sirketler.FirstOrDefault(f => f.Id == umy.SirketId)?.Name,
                Read = umy.Read,
                Write = umy.Write,
                Silindi = umy.Silindi,
                SilinmeZamani = umy.SilinmeZamani,
                Aktif = umy.Aktif
            }).ToList();
        }

        public async Task SetSilindi(int id)
        {
            var userMakineYetki = await _userMakineYetkiRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
            userMakineYetki.Silindi = true;
            userMakineYetki.SilinmeZamani = DateTime.Now;
            await _userMakineYetkiRepository.SaveChangesAsync();
        }

        public async Task<List<Makine>> GetMakinesWithApi()
        {
            string url = PanoKimliklendirmeBaseUrl + PanoKimliklendirmeUrls.GetMakines;

            // Parametreleri query string olarak hazırlayın

            // GET isteği yapın
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<Makine>>();
                return result;
            }
            else
            {
                // Hata durumunu ele alın
                throw new Exception("Mikroservisten veri alınırken bir hata oluştu.");
            }
        }

        public async Task<List<Bolum>> GetBolumsWithApi()
        {
            string url = PanoKimliklendirmeBaseUrl + PanoKimliklendirmeUrls.GetBolums;

            // GET isteği yapın
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<Bolum>>();
                return result;
            }
            else
            {
                // Hata durumunu ele alın
                throw new Exception("Mikroservisten veri alınırken bir hata oluştu.");
            }
        }
    }
}
