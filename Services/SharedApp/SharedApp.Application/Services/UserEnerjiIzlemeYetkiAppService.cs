using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class UserEnerjiIzlemeYetkiAppService : AppService<UserEnerjiIzlemeYetki, UserEnerjiIzlemeYetkiViewModel>, IUserEnerjiIzlemeYetkiAppService
    {
        private readonly UserCacheService _userCacheService;
        private readonly IUserEnerjiIzlemeYetkiRepository _userEnerjiIzlemeYetkiRepository;
        private readonly ILokasyonRepository _lokasyonRepository;
        private readonly ISirketRepository _sirketRepository;

        public UserEnerjiIzlemeYetkiAppService(IMapper mapper, UserCacheService userCacheService, IUserEnerjiIzlemeYetkiRepository userEnerjiIzlemeYetkiRepository, ILokasyonRepository lokasyonRepository, ISirketRepository sirketRepository) : base(mapper, userEnerjiIzlemeYetkiRepository)
        {
            _userEnerjiIzlemeYetkiRepository = userEnerjiIzlemeYetkiRepository;
            _userCacheService = userCacheService;
            _lokasyonRepository = lokasyonRepository;
            _sirketRepository = sirketRepository;
        }

        public async Task AddUserEnerjiIzlemeYetki(List<string> secilenKullanicilar, List<int> secilenLokasyonlar, List<int> secilenSirketler, bool okumaYetkisi, bool yazmaYetkisi, bool sorumluMu, bool silindi, bool aktif)
        {
            var kumesler = await _userCacheService.GetKumesler("a");
            var lokasyonlar = await _lokasyonRepository.GetAllAsyncByPredicate(x => x.Silindi != true).ToListAsync();

            var UserEnerjiIzlemeYetkiler = await _userEnerjiIzlemeYetkiRepository.GetAllListAsync();
            var yetkiKayitlari = new List<UserEnerjiIzlemeYetki>();
            foreach (var userId in secilenKullanicilar)
            {
                var UserEnerjiIzlemeYetkiKayitlar = UserEnerjiIzlemeYetkiler.Where(x => x.UserId == userId).ToList();
                // Şirket bazında yetkilendirme
                foreach (var sirketId in secilenSirketler)
                {
                    //eger kayit varsa kaydetmemeli
                    var kayitVarMi = UserEnerjiIzlemeYetkiKayitlar.Where(x => x.SirketId == sirketId).FirstOrDefault();
                    if (kayitVarMi == null)
                    {
                        yetkiKayitlari.Add(new UserEnerjiIzlemeYetki
                        {
                            UserId = userId,
                            SirketId = sirketId,
                            LokasyonId = 0,
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
                            var kayitVarMi = UserEnerjiIzlemeYetkiKayitlar.Where(x => x.LokasyonId == lokasyonId).FirstOrDefault();
                            if (kayitVarMi == null)
                            {
                                kayitVarMi = UserEnerjiIzlemeYetkiKayitlar.Where(x => x.SirketId == sirketeAitLokasyonMu.SirketId && x.Silindi != true && x.Aktif != false).FirstOrDefault();
                                if (kayitVarMi == null)
                                {
                                    yetkiKayitlari.Add(new UserEnerjiIzlemeYetki
                                    {
                                        UserId = userId,
                                        SirketId = 0,
                                        LokasyonId = lokasyonId,
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
            }
            await _userEnerjiIzlemeYetkiRepository.AddRangeAsync(yetkiKayitlari);
            await _userEnerjiIzlemeYetkiRepository.SaveChangesAsync();
        }

        public async Task<List<UserEnerjiIzlemeYetki>> GetAllUserEnerjiIzlemeYetkiByUserId(string userId)
        {
            return await _userEnerjiIzlemeYetkiRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.UserId == userId);
        }

        public async Task<List<UserEnerjiIzlemeYetkiViewModel>> GetAllUserEnerjiIzlemeYetkis()
        {
            var UserEnerjiIzlemeYetkiler = await _userEnerjiIzlemeYetkiRepository.GetAllListAsync();

            var kullanicilar = await _userCacheService.GetUsersAsync("a");//TODO
            var lokasyonlar = await _lokasyonRepository.GetAllListAsync();
            var sirketler = await _sirketRepository.GetAllListAsync();

            // Kullanıcı bilgilerini eşleştir ve döndür
            return UserEnerjiIzlemeYetkiler.Select(uky => new UserEnerjiIzlemeYetkiViewModel
            {
                Id = uky.Id,
                UserId = kullanicilar.FirstOrDefault(u => u.Id == uky.UserId)?.Id,
                UserName = kullanicilar.FirstOrDefault(u => u.Id == uky.UserId)?.UserName,
                FullName = kullanicilar.FirstOrDefault(u => u.Id == uky.UserId)?.Name + " " + kullanicilar.FirstOrDefault(u => u.Id == uky.UserId)?.Surname,
                LokasyonId = uky.LokasyonId,
                LokasyonName = lokasyonlar.FirstOrDefault(l => l.Id == uky.LokasyonId)?.Name,
                SirketId = uky.SirketId,
                SirketName = sirketler.FirstOrDefault(s => s.Id == uky.SirketId)?.Name,
                Read = uky.Read,
                Write = uky.Write,
                Silindi = uky.Silindi,
                SilinmeZamani = uky.SilinmeZamani,
                Aktif = uky.Aktif,
            }).ToList();
        }

        public async Task SetSilindi(int id)
        {
            var userMakineYetki = await _userEnerjiIzlemeYetkiRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
            userMakineYetki.Silindi = true;
            userMakineYetki.SilinmeZamani = DateTime.Now;
            await _userEnerjiIzlemeYetkiRepository.SaveChangesAsync();
        }
    }
}
