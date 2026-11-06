using HegCoolingScada.Domain.DbViewModels;
using HegCoolingScada.Domain.Interfaces;
using HegCoolingScada.Domain.Models;
using HegCoolingScada.Domain.Models.ReportModels;
using Microsoft.EntityFrameworkCore;

namespace HegCoolingScada.Infrastructure.Repositories
{
    public class HegSogutmaRepository : IHegSogutmaRepository
    {
        // Günlük/aylık continuous aggregate'ler Europe/Istanbul takvim sınırlarına
        // göre bucketlanıyor (00:00 İstanbul, UTC'de 03:00 değil). Filtrede baslangic
        // bu yüzden UTC gün/ay başı ile değil, İstanbul gün/ay başının UTC karşılığı
        // ile karşılaştırılmalı.
        private static readonly TimeZoneInfo IstanbulTimeZone = ResolveIstanbulTimeZone();

        private readonly HegSogutmaScadaDbContext _context;

        public HegSogutmaRepository(HegSogutmaScadaDbContext context)
        {
            _context = context;
        }

        private static TimeZoneInfo ResolveIstanbulTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            }
        }

        private static DateTime IstanbulGunBasiUtc(DateTime baslangicUtc)
        {
            var istanbulZamani = TimeZoneInfo.ConvertTimeFromUtc(baslangicUtc, IstanbulTimeZone);
            var gunBasiIstanbul = DateTime.SpecifyKind(istanbulZamani.Date, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(gunBasiIstanbul, IstanbulTimeZone);
        }

        private static DateTime IstanbulAyBasiUtc(DateTime baslangicUtc)
        {
            var istanbulZamani = TimeZoneInfo.ConvertTimeFromUtc(baslangicUtc, IstanbulTimeZone);
            var ayBasiIstanbul = DateTime.SpecifyKind(new DateTime(istanbulZamani.Year, istanbulZamani.Month, 1), DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(ayBasiIstanbul, IstanbulTimeZone);
        }

        public async Task<List<Oda>> GetAllOdalarAsync()
            => await _context.Odalar.AsNoTracking().OrderBy(x => x.Kat).ThenBy(x => x.Ad).ToListAsync();

        public async Task<List<OdaVanasi>> GetAllVanalarAsync()
            => await _context.OdaVanalari.AsNoTracking().ToListAsync();

        public async Task<List<Kompresor>> GetAllKompresorlerAsync()
            => await _context.Kompresorler.AsNoTracking().OrderBy(x => x.TagNo).ToListAsync();

        public async Task<List<VanaAnlik>> GetAllVanaAnlikAsync()
            => await _context.VanaAnlik.AsNoTracking().ToListAsync();

        public async Task<List<VanaViewModel>> GetVanalarWithDurumAsync()
            => await (
                from v in _context.OdaVanalari.AsNoTracking()
                join o in _context.Odalar.AsNoTracking() on v.OdaId equals o.Id into odaGroup
                from o in odaGroup.DefaultIfEmpty()
                join a in _context.VanaAnlik.AsNoTracking() on v.Id equals a.VanaId into anlikGroup
                from a in anlikGroup.DefaultIfEmpty()
                orderby o.Kat, o.Ad, v.EkipmanKodu
                select new VanaViewModel
                {
                    VanaId = v.Id,
                    OdaId = v.OdaId,
                    OdaAdi = o.Ad,
                    OdaKat = o.Kat,
                    EkipmanKodu = v.EkipmanKodu,
                    Aciklama = v.Aciklama,
                    GuncelDurumAcik = a.Durum,
                    GuncelDurumBaslangici = a.DurumBaslangici,
                    GuncellemeZamani = a.GuncellemeZamani,
                }
            ).ToListAsync();

        public async Task<List<KompresorAnlik>> GetAllKompresorAnlikAsync()
            => await _context.KompresorAnlik.AsNoTracking().ToListAsync();

        public async Task<decimal?> GetVanaAcikSaniyeAsync(short vanaId, DateTime baslangic, DateTime bitis)
        {
            // EF Core'un skaler SqlQuery<T> desteği, döndürülen kolonun "Value"
            // adında olmasını gerektiriyor. Fonksiyon NULL dönebildiği için sonuç nullable.
            return await _context.Database
                .SqlQuery<decimal?>($"SELECT public.vana_acik_saniye({vanaId}, {baslangic}, {bitis}) AS \"Value\"")
                .SingleAsync();
        }

        public async Task<List<KompresorOkumaSaatlik>> GetKompresorOkumaSaatlikAsync(DateTime baslangic, DateTime bitis, IReadOnlyCollection<short>? kompresorIds = null)
            => await _context.KompresorOkumaSaatlik
                .Where(x => x.Bucket >= baslangic && x.Bucket < bitis)
                .Where(x => kompresorIds == null || kompresorIds.Count == 0 || kompresorIds.Contains(x.KompresorId))
                .OrderBy(x => x.KompresorId).ThenBy(x => x.Bucket)
                .ToListAsync();

        public async Task<List<KompresorOkumaGunluk>> GetKompresorOkumaGunlukAsync(DateTime baslangic, DateTime bitis, IReadOnlyCollection<short>? kompresorIds = null)
        {
            // baslangic gün içi bir saat olabildiğinden (örn. 16:00), bucket'ı
            // dışarıda bırakmamak için filtrede İstanbul takvim gününün başı kullanılıyor.
            var baslangicGunBasi = IstanbulGunBasiUtc(baslangic);
            return await _context.KompresorOkumaGunluk
                .Where(x => x.Bucket >= baslangicGunBasi && x.Bucket < bitis)
                .Where(x => kompresorIds == null || kompresorIds.Count == 0 || kompresorIds.Contains(x.KompresorId))
                .OrderBy(x => x.KompresorId).ThenBy(x => x.Bucket)
                .ToListAsync();
        }

        public async Task<List<KompresorOkumaAylik>> GetKompresorOkumaAylikAsync(DateTime baslangic, DateTime bitis, IReadOnlyCollection<short>? kompresorIds = null)
        {
            // baslangic ay içi bir gün/saat olabildiğinden, bucket'ı dışarıda
            // bırakmamak için filtrede İstanbul takvim ayının başı kullanılıyor.
            var baslangicAyBasi = IstanbulAyBasiUtc(baslangic);
            return await _context.KompresorOkumaAylik
                .Where(x => x.Bucket >= baslangicAyBasi && x.Bucket < bitis)
                .Where(x => kompresorIds == null || kompresorIds.Count == 0 || kompresorIds.Contains(x.KompresorId))
                .OrderBy(x => x.KompresorId).ThenBy(x => x.Bucket)
                .ToListAsync();
        }
    }
}
