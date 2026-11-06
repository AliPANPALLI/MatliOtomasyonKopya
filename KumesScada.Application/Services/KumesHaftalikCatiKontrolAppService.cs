using MapsterMapper;
using KumesScada.Application.Interfaces;
using KumesScada.Application.ViewModels;
using KumesScada.Domain.Interfaces;
using KumesScada.Domain.Models;
using PanoKimliklendirme.Application.Services.Core;

namespace KumesScada.Application.Services
{
    public class KumesHaftalikCatiKontrolAppService : AppService<KumesHaftalikCatiKontrol, KumesHaftalikCatiKontrolViewModel>, IKumesHaftalikCatiKontrolAppService
    {
        private readonly IKumesHaftalikCatiKontrolRepository _kumesHaftalikCatiKontrolRepository;
        private readonly IDonemRepository _donemRepository;

        public KumesHaftalikCatiKontrolAppService(IMapper mapper, IKumesHaftalikCatiKontrolRepository kumesHaftalikCatiKontrolRepository, IDonemRepository donemRepository) : base(mapper, kumesHaftalikCatiKontrolRepository)
        {
            _kumesHaftalikCatiKontrolRepository = kumesHaftalikCatiKontrolRepository;
            _donemRepository = donemRepository;
        }
        public async Task<List<KumesHaftalikCatiKontrolViewModel>> GetKumesCatiKontrolHaftalari(int kumesId)
        {
            var result = new List<KumesHaftalikCatiKontrolViewModel>();

            var bugun = DateTime.Now.Date;

            var donem = await _donemRepository.GetAsyncByPredicate(x =>
                x.KumesId == kumesId &&
                x.GirisTarihi.HasValue &&
                x.GirisTarihi.Value.Date <= bugun &&
                (!x.CikisTarihi.HasValue || x.CikisTarihi.Value.Date >= bugun) &&
                x.Silindi != true);

            if (donem == null || !donem.GirisTarihi.HasValue || !donem.Yas.HasValue)
                return result;

            var girisTarihi = donem.GirisTarihi.Value.Date;

            /*
             * Haftalık rapor mantığı:
             * Hafta başlangıcı Pazartesi kabul edilecek.
             * Giriş tarihi hangi güne denk gelirse gelsin,
             * o haftanın Pazartesi günü referans alınacak.
             */
            var girisHaftasiPazartesi = GetPazartesiBaslangici(girisTarihi);
            var bugununHaftasiPazartesi = GetPazartesiBaslangici(bugun);

            var catiKontroller = (await _kumesHaftalikCatiKontrolRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumesId && x.DonemId == donem.Id && x.Silindi != true)).ToList();

            int baslangicHaftaSayisi = donem.Yas.Value;

            int gecenHaftaSayisi = (int)((bugununHaftasiPazartesi - girisHaftasiPazartesi).TotalDays / 7d);

            if (gecenHaftaSayisi < 0)
                gecenHaftaSayisi = 0;

            int sonHaftaSayisi = baslangicHaftaSayisi + gecenHaftaSayisi;

            for (int hafta = baslangicHaftaSayisi; hafta <= sonHaftaSayisi; hafta++)
            {
                int haftaIndex = hafta - baslangicHaftaSayisi;

                var haftaBaslangicTarihi = girisHaftasiPazartesi.AddDays(haftaIndex * 7);
                var haftaBitisTarihi = haftaBaslangicTarihi.AddDays(6);

                var mevcutKontrol = catiKontroller.FirstOrDefault(x => x.HaftaSayisi == hafta);

                result.Add(new KumesHaftalikCatiKontrolViewModel
                {
                    Id = mevcutKontrol?.Id ?? 0,
                    KumesId = kumesId,
                    DonemId = donem.Id,
                    HaftaSayisi = hafta,

                    // ViewModel içinde bu alanlar varsa açabilirsin.
                    // HaftaBaslangicTarihi = haftaBaslangicTarihi,
                    // HaftaBitisTarihi = haftaBitisTarihi,

                    Deger = mevcutKontrol?.Deger
                });
            }

            return result.OrderByDescending(x => x.HaftaSayisi).ToList();
        }
        private static DateTime GetPazartesiBaslangici(DateTime tarih)
        {
            int diff = (7 + (tarih.DayOfWeek - DayOfWeek.Monday)) % 7;
            return tarih.Date.AddDays(-diff);
        }

        public async Task SaveKumesHaftalikCatiKontrol(KumesHaftalikCatiKontrolViewModel model, string? userId)
        {
            if (model.KumesId <= 0)
                throw new Exception("Kümes bilgisi zorunludur.");

            if (model.HaftaSayisi <= 0)
                throw new Exception("Hafta bilgisi zorunludur.");

            var bugun = DateTime.Now.Date;

            var donem = model.DonemId.HasValue
                ? await _donemRepository.GetAsyncByPredicate(x =>
                    x.Id == model.DonemId.Value &&
                    x.KumesId == model.KumesId &&
                    x.Silindi != true)
                : await _donemRepository.GetAsyncByPredicate(x =>
                    x.KumesId == model.KumesId &&
                    x.GirisTarihi.HasValue &&
                    x.GirisTarihi.Value.Date <= bugun &&
                    (!x.CikisTarihi.HasValue || x.CikisTarihi.Value.Date >= bugun) &&
                    x.Silindi != true);

            if (donem == null)
                throw new Exception("Aktif dönem bulunamadı.");

            var mevcutKayit = await _kumesHaftalikCatiKontrolRepository.GetAsyncByPredicate(x =>
                x.KumesId == model.KumesId &&
                x.DonemId == donem.Id &&
                x.HaftaSayisi == model.HaftaSayisi &&
                x.Silindi != true);

            if (mevcutKayit == null)
            {
                var entity = new KumesHaftalikCatiKontrol
                {
                    KumesId = model.KumesId,
                    DonemId = donem.Id,
                    HaftaSayisi = model.HaftaSayisi,
                    Deger = model.Deger,
                    KayitZamani = DateTime.Now,
                    KayitYapanUserId = userId,
                    Silindi = false
                };

                await _kumesHaftalikCatiKontrolRepository.AddAsync(entity);
            }
            else
            {
                mevcutKayit.Deger = model.Deger;
                mevcutKayit.GuncellemeZamani = DateTime.Now;
                mevcutKayit.GuncelleyenUserId = userId;

                await _kumesHaftalikCatiKontrolRepository.UpdateAsync(mevcutKayit);
            }

            await _kumesHaftalikCatiKontrolRepository.SaveChangesAsync();
        }
        //public async Task<KumesHaftalikCatiKontrolViewModel?> GetKumesHaftalikCatiKontrol(int kumesId, int? donemId, int haftaSayisi)
        //{
        //    var entity = await _kumesHaftalikCatiKontrolRepository.GetAsyncByPredicate(x => x.KumesId == kumesId && x.DonemId == donemId && x.HaftaSayisi == haftaSayisi && x.Silindi != true);

        //    if (entity == null)
        //        return null;

        //    return new KumesHaftalikCatiKontrolViewModel
        //    {
        //        Id = entity.Id,
        //        KumesId = entity.KumesId,
        //        DonemId = entity.DonemId,
        //        HaftaSayisi = entity.HaftaSayisi,
        //        Deger = entity.Deger
        //    };
        //}

        //public async Task SaveKumesHaftalikCatiKontrol(KumesHaftalikCatiKontrolViewModel model, string userId)
        //{
        //    var mevcutKayit = await _kumesHaftalikCatiKontrolRepository.GetAsyncByPredicate(x => x.KumesId == model.KumesId && x.DonemId == model.DonemId && x.HaftaSayisi == model.HaftaSayisi && x.Silindi != true);

        //    if (mevcutKayit == null)
        //    {
        //        var entity = new KumesHaftalikCatiKontrol
        //        {
        //            KumesId = model.KumesId,
        //            DonemId = model.DonemId,
        //            HaftaSayisi = model.HaftaSayisi,
        //            Deger = model.Deger,
        //            KayitZamani = DateTime.Now,
        //            KayitYapanUserId = userId,
        //            Silindi = false
        //        };

        //        await _kumesHaftalikCatiKontrolRepository.AddAsync(entity);
        //    }
        //    else
        //    {
        //        mevcutKayit.Deger = model.Deger;
        //        mevcutKayit.GuncellemeZamani = DateTime.Now;
        //        mevcutKayit.GuncelleyenUserId = userId;

        //        await _kumesHaftalikCatiKontrolRepository.UpdateAsync(mevcutKayit);
        //    }

        //    await _kumesHaftalikCatiKontrolRepository.SaveChangesAsync();
        //}
    }
}