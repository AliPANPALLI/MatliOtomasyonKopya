using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class UserKumesYetkiAppService : AppService<UserKumesYetki, UserKumesYetkiViewModel>, IUserKumesYetkiAppService
    {
        private readonly UserCacheService _userCacheService;
        private readonly IUserKumesYetkiRepository _userKumesYetkiRepository;
        private readonly ILokasyonRepository _lokasyonRepository;
        private readonly ISirketRepository _sirketRepository;

        public UserKumesYetkiAppService(IMapper mapper, UserCacheService userCacheService, IUserKumesYetkiRepository userKumesYetkiRepository, ILokasyonRepository lokasyonRepository, ISirketRepository sirketRepository) : base(mapper, userKumesYetkiRepository)
        {
            _userKumesYetkiRepository = userKumesYetkiRepository;
            _userCacheService = userCacheService;
            _lokasyonRepository = lokasyonRepository;
            _sirketRepository = sirketRepository;
        }

        public async Task AddUserKumesYetki(List<string> secilenKullanicilar, List<int> secilenKumesler, List<int> secilenLokasyonlar, List<int> secilenSirketler, bool okumaYetkisi, bool yazmaYetkisi, bool sorumluMu, bool silindi, bool aktif, bool yemRasyonYetki,bool raporRehberYetki)
        {
            var kumesler = await _userCacheService.GetKumesler("a");
            var lokasyonlar = await _lokasyonRepository.GetAllAsyncByPredicate(x => x.Silindi != true).ToListAsync();

            var userKumesYetkiler = await _userKumesYetkiRepository.GetAllListAsync();
            var yetkiKayitlari = new List<UserKumesYetki>();
            foreach (var userId in secilenKullanicilar)
            {
                var userKumesYetkiKayitlar = userKumesYetkiler.Where(x => x.UserId == userId).ToList();
                // Şirket bazında yetkilendirme
                foreach (var sirketId in secilenSirketler)
                {
                    //eger kayit varsa kaydetmemeli
                    var kayitVarMi = userKumesYetkiKayitlar.Where(x => x.SirketId == sirketId).FirstOrDefault();
                    if (kayitVarMi == null)
                    {
                        yetkiKayitlari.Add(new UserKumesYetki
                        {
                            UserId = userId,
                            SirketId = sirketId,
                            LokasyonId = 0,
                            KumesId = 0,
                            Read = okumaYetkisi,
                            Write = yazmaYetkisi,
                            SorumluMu = sorumluMu,
                            Silindi = false,
                            Aktif = true,
                            SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null,
                            YemRasyonYetki = yemRasyonYetki,
                            RaporRehberYetki = raporRehberYetki
                        });
                    }
                    else
                    {
                        kayitVarMi.Read = okumaYetkisi;
                        kayitVarMi.Write = yazmaYetkisi;
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
                            var kayitVarMi = userKumesYetkiKayitlar.Where(x => x.LokasyonId == lokasyonId).FirstOrDefault();
                            if (kayitVarMi == null)
                            {
                                kayitVarMi = userKumesYetkiKayitlar.Where(x => x.SirketId == sirketeAitLokasyonMu.SirketId && x.Silindi != true && x.Aktif != false).FirstOrDefault();
                                if (kayitVarMi == null)
                                {
                                    yetkiKayitlari.Add(new UserKumesYetki
                                    {
                                        UserId = userId,
                                        SirketId = 0,
                                        LokasyonId = lokasyonId,
                                        KumesId = 0,
                                        Read = okumaYetkisi,
                                        Write = yazmaYetkisi,
                                        SorumluMu = sorumluMu,
                                        Silindi = false,
                                        Aktif = true,
                                        SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null,
                                        YemRasyonYetki = yemRasyonYetki,
                                        RaporRehberYetki = raporRehberYetki
                                    });
                                }
                            }
                            else
                            {
                                kayitVarMi.Read = okumaYetkisi;
                                kayitVarMi.Write = yazmaYetkisi;
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

                // Kümes bazında yetkilendirme
                foreach (var kumesId in secilenKumesler)
                {
                    var lokasyonaAitKumesMi = kumesler.Where(x => x.Id == kumesId).FirstOrDefault();
                    if (lokasyonaAitKumesMi != null)
                    {
                        if (!secilenLokasyonlar.Contains(lokasyonaAitKumesMi.LokasyonId))
                        {
                            //eger kayit varsa veya ust kaydi varsa kaydetmemeli
                            var kayitVarMi = userKumesYetkiKayitlar.Where(x => x.KumesId == kumesId).FirstOrDefault();
                            if (kayitVarMi == null)
                            {
                                kayitVarMi = userKumesYetkiKayitlar.Where(x => x.LokasyonId == lokasyonaAitKumesMi.LokasyonId && x.Silindi != true && x.Aktif != false).FirstOrDefault();
                                if (kayitVarMi == null)
                                {
                                    var lokasyon = lokasyonlar.Where(x => x.Id == lokasyonaAitKumesMi.LokasyonId).FirstOrDefault();//todo silindi kontrolu eklenebilir.
                                    kayitVarMi = userKumesYetkiKayitlar.Where(x => x.SirketId == lokasyon.SirketId && x.Silindi != true && x.Aktif != false).FirstOrDefault();
                                    if (kayitVarMi == null)
                                    {
                                        yetkiKayitlari.Add(new UserKumesYetki
                                        {
                                            UserId = userId,
                                            SirketId = 0,
                                            LokasyonId = 0,
                                            KumesId = kumesId,
                                            Read = okumaYetkisi,
                                            Write = yazmaYetkisi,
                                            SorumluMu = sorumluMu,
                                            Silindi = false,
                                            Aktif = true,
                                            SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null,
                                            YemRasyonYetki = yemRasyonYetki,
                                            RaporRehberYetki = raporRehberYetki
                                        });
                                    }
                                }
                            }
                            else
                            {
                                kayitVarMi.Read = okumaYetkisi;
                                kayitVarMi.Write = yazmaYetkisi;
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
            await _userKumesYetkiRepository.AddRangeAsync(yetkiKayitlari);
            await _userKumesYetkiRepository.SaveChangesAsync();
        }

        public async Task<List<UserKumesYetki>> GetAllUserKumesYetkiByUserId(string userId)
        {
            return await _userKumesYetkiRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.UserId == userId);
        }

        public async Task<List<UserKumesYetkiViewModel>> GetAllUserKumesYetkis()
        {
            var userKumesYetkiler = await _userKumesYetkiRepository.GetAllListAsync();

            var kullanicilar = await _userCacheService.GetUsersAsync("a");//TODO
            var kumesler = await _userCacheService.GetKumesler("a");//TODO          
            var lokasyonlar = await _lokasyonRepository.GetAllListAsync();
            var sirketler = await _sirketRepository.GetAllListAsync();

            // Kullanıcı bilgilerini eşleştir ve döndür
            return userKumesYetkiler.Select(uky => new UserKumesYetkiViewModel
            {
                Id = uky.Id,
                UserId = kullanicilar.FirstOrDefault(u => u.Id == uky.UserId)?.Id,
                UserName = kullanicilar.FirstOrDefault(u => u.Id == uky.UserId)?.UserName,
                FullName = kullanicilar.FirstOrDefault(u => u.Id == uky.UserId)?.Name + " " + kullanicilar.FirstOrDefault(u => u.Id == uky.UserId)?.Surname,
                KumesId = uky.KumesId,
                KumesName = kumesler.FirstOrDefault(k => k.Id == uky.KumesId)?.Name,
                LokasyonId = uky.LokasyonId,
                LokasyonName = lokasyonlar.FirstOrDefault(l => l.Id == uky.LokasyonId)?.Name,
                SirketId = uky.SirketId,
                SirketName = sirketler.FirstOrDefault(s => s.Id == uky.SirketId)?.Name,
                Read = uky.Read,
                Write = uky.Write,
                SorumluMu = uky.SorumluMu,
                Silindi = uky.Silindi,
                SilinmeZamani = uky.SilinmeZamani,
                Aktif = uky.Aktif,
                YemRasyonYetki = uky.YemRasyonYetki,
                RaporRehberYetki = uky.RaporRehberYetki
            }).ToList();
        }

        public async Task SetSilindi(int id)
        {
            var userMakineYetki = await _userKumesYetkiRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
            userMakineYetki.Silindi = true;
            userMakineYetki.SilinmeZamani = DateTime.Now;
            await _userKumesYetkiRepository.SaveChangesAsync();
        }
    }
}