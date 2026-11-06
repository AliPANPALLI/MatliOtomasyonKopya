using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class UserSuKaynakYetkiAppService : AppService<UserSuKaynakYetki, UserSuKaynakYetkiViewModel>, IUserSuKaynakYetkiAppService
    {
        private readonly UserCacheService _userCacheService;
        private readonly IUserSuKaynakYetkiRepository _userSuKaynakYetkiRepository;
        private readonly ILokasyonRepository _lokasyonRepository;
        private readonly ISirketRepository _sirketRepository;

        public UserSuKaynakYetkiAppService(IMapper mapper, UserCacheService userCacheService, IUserSuKaynakYetkiRepository userSuKaynakYetkiRepository, ILokasyonRepository lokasyonRepository, ISirketRepository sirketRepository) : base(mapper, userSuKaynakYetkiRepository)
        {
            _userSuKaynakYetkiRepository = userSuKaynakYetkiRepository;
            _userCacheService = userCacheService;
            _lokasyonRepository = lokasyonRepository;
            _sirketRepository = sirketRepository;
        }
        public async Task AddUserSuKaynakYetki(List<string> secilenKullanicilar, List<int> secilenCizimler, List<int> secilenLokasyonlar, List<int> secilenSirketler, bool okumaYetkisi, bool yazmaYetkisi, bool silindi, bool aktif)
        {
            var userSuKaynakYetkiler = await _userSuKaynakYetkiRepository.GetAllListAsync();

            var yetkiKayitlari = new List<UserSuKaynakYetki>();

            foreach (var userId in secilenKullanicilar)
            {
                var userYetkileri = userSuKaynakYetkiler.Where(x => x.UserId == userId).ToList();

                foreach (var cizimId in secilenCizimler)
                {
                    var mevcut = userYetkileri.FirstOrDefault(x => x.CizimId == cizimId);
                    if (mevcut == null)
                    {
                        yetkiKayitlari.Add(new UserSuKaynakYetki
                        {
                            UserId = userId,
                            SirketId = 0,
                            LokasyonId = 0,
                            CizimId = cizimId,
                            SuKaynakId = 0,
                            Read = okumaYetkisi,
                            Write = yazmaYetkisi,
                            Silindi = false,
                            Aktif = true,
                            SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null
                        });
                    }
                    else
                    {
                        mevcut.Read = okumaYetkisi;
                        mevcut.Write = yazmaYetkisi;
                        mevcut.Silindi = false;
                        mevcut.SilinmeZamani = null;
                        mevcut.Aktif = aktif;
                    }
                }
            }
            await _userSuKaynakYetkiRepository.AddRangeAsync(yetkiKayitlari);
            await _userSuKaynakYetkiRepository.SaveChangesAsync();
        }
        public async Task AddUserSuKaynakYetki2(List<string> secilenKullanicilar, List<int> secilenCizimler, List<int> secilenLokasyonlar, List<int> secilenSirketler, bool okumaYetkisi, bool yazmaYetkisi, bool silindi, bool aktif)
        {
            //var cizimler = await _userSuKaynakYetkiRepository.GetvwSuHatCizimSirketForYetkiByCizimIds(secilenCizimler);
            var cizimler = await _userSuKaynakYetkiRepository.GetAllvwSuHatCizimSirket();
            //var lokasyonlar = await _lokasyonRepository.GetAllAsyncByPredicate(x => x.Silindi != true).ToListAsync();
            var userSuKaynakYetkiler = await _userSuKaynakYetkiRepository.GetAllListAsync();

            var secilenSirketSet = secilenSirketler.ToHashSet();
            //var secilenLokasyonSet = secilenLokasyonlar.ToHashSet();
            var secilenCizimSet = secilenCizimler.ToHashSet();

            var yetkiKayitlari = new List<UserSuKaynakYetki>();
            var secilenSirketCizimleri = cizimler.Where(x => x.SirketId.HasValue && secilenSirketSet.Contains(x.SirketId.Value)).GroupBy(x => x.CizimId).Select(g => g.First()).ToList();

            foreach (var userId in secilenKullanicilar)
            {
                var userYetkileri = userSuKaynakYetkiler.Where(x => x.UserId == userId).ToList();

                foreach (var cizim in secilenSirketCizimleri)
                {
                    var mevcut = userYetkileri.FirstOrDefault(x => x.CizimId == cizim.CizimId);
                    if (mevcut == null)
                    {
                        yetkiKayitlari.Add(new UserSuKaynakYetki
                        {
                            UserId = userId,
                            SirketId = 0,
                            LokasyonId = 0,
                            CizimId = cizim.CizimId.GetValueOrDefault(0),
                            SuKaynakId = 0,
                            Read = okumaYetkisi,
                            Write = yazmaYetkisi,
                            Silindi = false,
                            Aktif = true,
                            SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null
                        });
                    }
                    else
                    {
                        mevcut.Read = okumaYetkisi;
                        mevcut.Write = yazmaYetkisi;
                        mevcut.Silindi = false;
                        mevcut.SilinmeZamani = null;
                        mevcut.Aktif = aktif;
                    }
                }

                // Çizim bazlı yetki
                foreach (var cizim in cizimler.Where(x => secilenCizimSet.Contains(x.CizimId.GetValueOrDefault(0))))
                {
                    //if (secilenLokasyonSet.Contains(cizim.LokasyonId ?? -1)) continue;
                    //if (secilenSirketSet.Contains(cizim.SirketId.GetValueOrDefault(0))) continue;

                    var mevcut = userYetkileri.FirstOrDefault(x => x.CizimId == cizim.CizimId);
                    if (mevcut == null)
                    {
                        yetkiKayitlari.Add(new UserSuKaynakYetki
                        {
                            UserId = userId,
                            SirketId = 0,
                            LokasyonId = 0,
                            CizimId = cizim.CizimId.GetValueOrDefault(0),
                            SuKaynakId = 0,
                            Read = okumaYetkisi,
                            Write = yazmaYetkisi,
                            Silindi = false,
                            Aktif = true,
                            SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null
                        });
                    }
                    else
                    {
                        mevcut.Read = okumaYetkisi;
                        mevcut.Write = yazmaYetkisi;
                        mevcut.Silindi = false;
                        mevcut.SilinmeZamani = null;
                        mevcut.Aktif = aktif;
                    }
                }

                // Kullanıcının sahip olmaması gereken tüm eski kayıtları iptal et
                var secilenSirkettenGelenCizimIdSet = secilenSirketCizimleri.Where(x => x.CizimId.HasValue).Select(x => x.CizimId.Value).ToHashSet();

                foreach (var eskiYetki in userYetkileri)
                {
                    bool seciliMi =
                        (eskiYetki.CizimId != 0 && (
                            secilenCizimSet.Contains(eskiYetki.CizimId) ||
                            secilenSirkettenGelenCizimIdSet.Contains(eskiYetki.CizimId)
                        ));

                    if (!seciliMi && !eskiYetki.Silindi.GetValueOrDefault(false))
                    {
                        eskiYetki.Silindi = true;
                        eskiYetki.SilinmeZamani = DateTime.Now;
                        eskiYetki.Aktif = false;
                    }
                }
            }

            await _userSuKaynakYetkiRepository.AddRangeAsync(yetkiKayitlari);
            await _userSuKaynakYetkiRepository.SaveChangesAsync();
        }

        public async Task AddUserSuKaynakYetki3(List<string> secilenKullanicilar, List<int> secilenSuKaynaklar, List<int> secilenLokasyonlar, List<int> secilenSirketler, bool okumaYetkisi, bool yazmaYetkisi, bool silindi, bool aktif)
        {
            var suKaynaklar = await _userCacheService.GetSuKaynaklar("a");
            var lokasyonlar = await _lokasyonRepository.GetAllAsyncByPredicate(x => x.Silindi != true).ToListAsync();

            var userSuKaynakYetkiler = await _userSuKaynakYetkiRepository.GetAllListAsync();
            var yetkiKayitlari = new List<UserSuKaynakYetki>();
            foreach (var userId in secilenKullanicilar)
            {
                var userSuKaynakYetkiKayitlar = userSuKaynakYetkiler.Where(x => x.UserId == userId).ToList();
                // Şirket bazında yetkilendirme
                foreach (var sirketId in secilenSirketler)
                {
                    //eger kayit varsa kaydetmemeli
                    var kayitVarMi = userSuKaynakYetkiKayitlar.Where(x => x.SirketId == sirketId).FirstOrDefault();
                    if (kayitVarMi == null)
                    {
                        yetkiKayitlari.Add(new UserSuKaynakYetki
                        {
                            UserId = userId,
                            SirketId = sirketId,
                            LokasyonId = 0,
                            SuKaynakId = 0,
                            Read = okumaYetkisi,
                            Write = yazmaYetkisi,
                            Silindi = false,
                            Aktif = true,
                            SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null
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
                            var kayitVarMi = userSuKaynakYetkiKayitlar.Where(x => x.LokasyonId == lokasyonId).FirstOrDefault();
                            if (kayitVarMi == null)
                            {
                                kayitVarMi = userSuKaynakYetkiKayitlar.Where(x => x.SirketId == sirketeAitLokasyonMu.SirketId && x.Silindi != true && x.Aktif != false).FirstOrDefault();
                                if (kayitVarMi == null)
                                {
                                    yetkiKayitlari.Add(new UserSuKaynakYetki
                                    {
                                        UserId = userId,
                                        SirketId = 0,
                                        LokasyonId = lokasyonId,
                                        SuKaynakId = 0,
                                        Read = okumaYetkisi,
                                        Write = yazmaYetkisi,
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

                // Su kaynağı bazında yetkilendirme
                foreach (var suKaynakId in secilenSuKaynaklar)
                {
                    var lokasyonaAitSuKaynakMi = suKaynaklar.Where(x => x.Id == suKaynakId).FirstOrDefault();
                    if (lokasyonaAitSuKaynakMi != null)
                    {
                        if (!secilenLokasyonlar.Contains(lokasyonaAitSuKaynakMi.LokasyonId))
                        {
                            //eger kayit varsa veya ust kaydi varsa kaydetmemeli
                            var kayitVarMi = userSuKaynakYetkiKayitlar.Where(x => x.SuKaynakId == suKaynakId).FirstOrDefault();
                            if (kayitVarMi == null)
                            {
                                kayitVarMi = userSuKaynakYetkiKayitlar.Where(x => x.LokasyonId == lokasyonaAitSuKaynakMi.LokasyonId && x.Silindi != true && x.Aktif != false).FirstOrDefault();
                                if (kayitVarMi == null)
                                {
                                    var lokasyon = lokasyonlar.Where(x => x.Id == lokasyonaAitSuKaynakMi.LokasyonId).FirstOrDefault();//todo silindi kontrolu eklenebilir.
                                    //var cizimIds=await 
                                    kayitVarMi = userSuKaynakYetkiKayitlar.Where(x => x.SirketId == lokasyon.SirketId && x.Silindi != true && x.Aktif != false).FirstOrDefault();
                                    if (kayitVarMi == null)
                                    {
                                        yetkiKayitlari.Add(new UserSuKaynakYetki
                                        {
                                            UserId = userId,
                                            SirketId = 0,
                                            LokasyonId = 0,
                                            SuKaynakId = suKaynakId,
                                            Read = okumaYetkisi,
                                            Write = yazmaYetkisi,
                                            Silindi = silindi,
                                            Aktif = aktif,
                                            SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null
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
            await _userSuKaynakYetkiRepository.AddRangeAsync(yetkiKayitlari);
            await _userSuKaynakYetkiRepository.SaveChangesAsync();
        }

        public async Task<List<UserSuKaynakYetki>> GetAllUserSuKaynakYetkiByUserId(string userId)
        {
            return await _userSuKaynakYetkiRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.UserId == userId);
        }

        public async Task<List<UserSuKaynakYetkiViewModel>> GetAllUserSuKaynakYetkis()
        {
            var userSuKaynakYetkiler = await _userSuKaynakYetkiRepository.GetAllvwUserSuKaynakYetki();

            var filteredUserSuKaynakYetkiler = userSuKaynakYetkiler.Where(x => x.CizimId > 0).ToList();
            return filteredUserSuKaynakYetkiler.Select(usy => new UserSuKaynakYetkiViewModel
            {
                Id = usy.Id,
                UserId = usy.UserId,
                UserName = usy.UserName,
                FullName = usy.Name + " " + usy.Surname,
                SuKaynakId = usy.SuKaynakId.GetValueOrDefault(0),
                LokasyonId = usy.LokasyonId.GetValueOrDefault(0),
                SirketId = usy.SirketId.GetValueOrDefault(0),
                CizimName = usy.CizimName,
                Read = usy.Read,
                Write = usy.Write,
                Silindi = usy.Silindi,
                SilinmeZamani = usy.SilinmeZamani,
                Aktif = usy.Aktif
            }).ToList();
        }

        public async Task SetSilindi(int id)
        {
            var userSuKaynakYetki = await _userSuKaynakYetkiRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
            userSuKaynakYetki.Silindi = true;
            userSuKaynakYetki.SilinmeZamani = DateTime.Now;
            await _userSuKaynakYetkiRepository.SaveChangesAsync();
        }
    }
}
