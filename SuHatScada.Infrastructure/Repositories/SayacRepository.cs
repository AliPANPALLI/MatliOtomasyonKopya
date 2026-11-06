using Microsoft.EntityFrameworkCore;
using SuHatScada.Application.Helpers;
using SuHatScada.Domain.Interfaces;
using SuHatScada.Domain.Models;
using SuHatScada.Domain.Models.ReportModels;
using SuHatScada.Infrastructure.Repositories.Core;
using System.Runtime.CompilerServices;

namespace SuHatScada.Infrastructure.Repositories
{
    public class SayacRepository : Repository<Sayac>, ISayacRepository
    {
        private readonly SuHatScadaDbContext _context;
        public SayacRepository(SuHatScadaDbContext context) : base(context)
        {
            _context = context;
        }

        //public async Task<List<EtiketLog>> GetEtiketLogVerilerFromSuKaynakId(List<int> suKaynakIds, DateTime baslangicZamani, DateTime bitisZamani)//todo baslangic bitis datetime eklenecek
        //{
        //    var veriler = await _context.EtiketLog.AsNoTracking().Where(x => suKaynakIds.Contains(x.SuKaynakId.GetValueOrDefault(0)) && x.KayitZamani.Value.Date >= baslangicZamani.Date && x.KayitZamani.Value.Date <= bitisZamani.Date).OrderByDescending(x => x.Id).ToListAsync();
        //    return veriler;
        //}

        //public async Task<List<SuKaynakLog>> GetSuKaynakLogVerilerFromSayacId(List<int> sayacIds, DateTime baslangicZamani, DateTime bitisZamani)
        //{
        //    var veriler = await _context.SuKaynakLog.Where(x => sayacIds.Contains(x.SayacId.GetValueOrDefault(0)) && x.KayitZamani.Date >= baslangicZamani.Date && x.KayitZamani.Date <= bitisZamani.Date).OrderByDescending(x => x.Id).AsNoTracking().ToListAsync();
        //    return veriler;
        //}
        public async Task<List<EtiketLogYeni>> GetEtiketLogVerilerFromSuKaynakId(List<int> suKaynakIds, DateTime baslangicZamani, DateTime bitisZamani)//todo baslangic bitis datetime eklenecek
        {
            //var veriler = await _context.EtiketLogYeni.Where(x => suKaynakIds.Contains(x.SuKaynakId.GetValueOrDefault(0)) && x.KayitZamani.Value.Date >= baslangicZamani.Date && x.KayitZamani.Value.Date <= bitisZamani.Date).AsNoTracking().OrderByDescending(x => x.Id).ToListAsync();
            if (suKaynakIds == null || suKaynakIds.Count == 0)
                return new List<EtiketLogYeni>();

            var baslangic = baslangicZamani.Date;

            // Bitiş gününün tamamını dahil etmek için sonraki günün başlangıcı
            var bitisExclusive = bitisZamani.Date.AddDays(1);

            var veriler = await _context.EtiketLogYeni.AsNoTracking().Where(x => x.SuKaynakId.HasValue && suKaynakIds.Contains(x.SuKaynakId.GetValueOrDefault(0)) && x.KayitZamani.HasValue && x.KayitZamani.Value >= baslangic && x.KayitZamani.Value < bitisExclusive).OrderByDescending(x => x.Id).ToListAsync();
            return veriler;
        }

        public async Task<List<SuKaynakLogYeni>> GetSuKaynakLogVerilerFromSayacId(List<int> sayacIds, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var veriler = await _context.SuKaynakLogYeni.Where(x => sayacIds.Contains(x.SayacId.GetValueOrDefault(0)) && x.KayitZamani.Date >= baslangicZamani.Date && x.KayitZamani.Date <= bitisZamani.Date).OrderByDescending(x => x.Id).AsNoTracking().ToListAsync();
            return veriler;
        }
        public async Task<List<SayacPeriyotData2>> GetToplamSuDataFromViewToplu2(
            string periyot,
            List<int> sayacIds,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            if (sayacIds == null || sayacIds.Count == 0)
                return new List<SayacPeriyotData2>();

            var ids = sayacIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new List<SayacPeriyotData2>();

            var range = SuHatRaporTarihHelper2.GetReportRange2(
                startDate,
                endDate,
                periyot);

            var sonuc = new List<SayacPeriyotData2>();

            // Tek sorguda çok fazla sayaç göndermemek için.
            const int batchSize = 5;

            // Sadece bu işlem için geçici timeout.
            var eskiTimeout = _context.Database.GetCommandTimeout();

            try
            {
                _context.Database.SetCommandTimeout(180);

                foreach (var idPaketi in ids.Chunk(batchSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batchIds = idPaketi.ToList();

                    IQueryable<SayacPeriyotData2> query;

                    switch (periyot)
                    {
                        case "Saatlik":
                            query = _context.vwSuKaynakLog_Saatlik
                                .AsNoTracking()
                                .Where(x =>
                                    x.SayacId.HasValue &&
                                    batchIds.Contains(x.SayacId.Value) &&
                                    x.IlkZaman.HasValue &&
                                    x.IlkZaman.Value >= range.Start &&
                                    x.IlkZaman.Value < range.EndExclusive)
                                .Select(x => new SayacPeriyotData2
                                {
                                    SayacId = x.SayacId!.Value,
                                    Periyot = x.IlkZaman!.Value,
                                    Toplam = x.DebiFark ?? 0
                                });
                            break;

                        case "Günlük":
                            query = _context.vwSuKaynakLog_Gunluk
                                .AsNoTracking()
                                .Where(x =>
                                    x.SayacId.HasValue &&
                                    batchIds.Contains(x.SayacId.Value) &&
                                    x.IlkZaman.HasValue &&
                                    x.IlkZaman.Value >= range.Start &&
                                    x.IlkZaman.Value < range.EndExclusive)
                                .Select(x => new SayacPeriyotData2
                                {
                                    SayacId = x.SayacId!.Value,
                                    Periyot = x.IlkZaman!.Value,
                                    Toplam = x.DebiFark ?? 0
                                });
                            break;

                        case "Haftalık":
                            query = _context.vwSuKaynakLog_Haftalik
                                .AsNoTracking()
                                .Where(x =>
                                    x.SayacId.HasValue &&
                                    batchIds.Contains(x.SayacId.Value) &&
                                    x.IlkZaman.HasValue &&
                                    x.IlkZaman.Value >= range.Start &&
                                    x.IlkZaman.Value < range.EndExclusive)
                                .Select(x => new SayacPeriyotData2
                                {
                                    SayacId = x.SayacId!.Value,
                                    Periyot = x.IlkZaman!.Value,
                                    Toplam = x.DebiFark ?? 0
                                });
                            break;

                        case "Aylık":
                            query = _context.vwSuKaynakLog_Aylik
                                .AsNoTracking()
                                .Where(x =>
                                    x.SayacId.HasValue &&
                                    batchIds.Contains(x.SayacId.Value) &&
                                    x.IlkZaman.HasValue &&
                                    x.IlkZaman.Value >= range.Start &&
                                    x.IlkZaman.Value < range.EndExclusive)
                                .Select(x => new SayacPeriyotData2
                                {
                                    SayacId = x.SayacId!.Value,
                                    Periyot = x.IlkZaman!.Value,
                                    Toplam = x.DebiFark ?? 0
                                });
                            break;

                        case "Yıllık":
                            query = _context.vwSuKaynakLog_Yillik
                                .AsNoTracking()
                                .Where(x =>
                                    x.SayacId.HasValue &&
                                    batchIds.Contains(x.SayacId.Value) &&
                                    x.IlkZaman.HasValue &&
                                    x.IlkZaman.Value >= range.Start &&
                                    x.IlkZaman.Value < range.EndExclusive)
                                .Select(x => new SayacPeriyotData2
                                {
                                    SayacId = x.SayacId!.Value,
                                    Periyot = x.IlkZaman!.Value,
                                    Toplam = x.DebiFark ?? 0
                                });
                            break;

                        default:
                            throw new ArgumentException(
                                "Geçersiz periyot",
                                nameof(periyot));
                    }

                    // Burada OrderBy YOK.
                    var paketSonucu = await query
                        .ToListAsync(cancellationToken);

                    sonuc.AddRange(paketSonucu);
                }
            }
            finally
            {
                _context.Database.SetCommandTimeout(eskiTimeout);
            }

            // SQL Server yerine uygulama belleğinde sıralanıyor.
            return sonuc
                .OrderBy(x => x.SayacId)
                .ThenBy(x => x.Periyot)
                .ToList();
        }
        public async Task<List<TankPeriyotData2>>
    GetTankSuDataFromViewToplu2(
        string periyot,
        List<int> suKaynakIds,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
        {
            if (suKaynakIds == null || suKaynakIds.Count == 0)
                return new List<TankPeriyotData2>();

            suKaynakIds = suKaynakIds
                .Distinct()
                .ToList();

            var range =
                SuHatRaporTarihHelper2.GetReportRange2(
                    startDate,
                    endDate,
                    periyot);

            var hamVeriler = await _context.vwTankLog_Saatlik
                .AsNoTracking()
                .Where(x =>
                    x.SuKaynakId.HasValue &&
                    suKaynakIds.Contains(x.SuKaynakId.Value) &&
                    x.IlkZaman.HasValue &&
                    x.IlkZaman.Value >= range.Start &&
                    x.IlkZaman.Value < range.EndExclusive)
                .Select(x => new TankSaatlikHamData2
                {
                    SuKaynakId = x.SuKaynakId!.Value,
                    IlkZaman = x.IlkZaman!.Value,
                    MinHacim = x.MinHacim ?? 0,
                    OrtalamaHacim = x.OrtalamaHacim ?? 0,
                    MaxHacim = x.MaxHacim ?? 0
                })
                .ToListAsync(cancellationToken);

            return hamVeriler
                .GroupBy(x => new
                {
                    x.SuKaynakId,

                    Periyot = SuHatRaporTarihHelper2
                        .GetPeriodStart2(
                            x.IlkZaman,
                            periyot)
                })
                .Select(group => new TankPeriyotData2
                {
                    SuKaynakId = group.Key.SuKaynakId,
                    Periyot = group.Key.Periyot,

                    MinHacim = group.Min(x => x.MinHacim),

                    OrtalamaHacim =
                        group.Average(x => x.OrtalamaHacim),

                    MaxHacim = group.Max(x => x.MaxHacim)
                })
                .OrderBy(x => x.SuKaynakId)
                .ThenBy(x => x.Periyot)
                .ToList();
        }
        public async Task<List<EtiketLogYeni>> GetEtiketLogVeriler2(List<int> suKaynakIds, List<int> etiketIds, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken = default)
        {
            if (suKaynakIds == null ||
                suKaynakIds.Count == 0 ||
                etiketIds == null ||
                etiketIds.Count == 0)
            {
                return new List<EtiketLogYeni>();
            }

            suKaynakIds = suKaynakIds
                .Distinct()
                .ToList();

            etiketIds = etiketIds
                .Distinct()
                .ToList();

            var baslangic = baslangicZamani.Date;

            var bitisExclusive =
                bitisZamani.Date.AddDays(1);

            return await _context.EtiketLogYeni
                .AsNoTracking()
                .Where(x =>
                    x.SuKaynakId.HasValue &&
                    suKaynakIds.Contains(x.SuKaynakId.Value) &&

                    x.EtiketId.HasValue &&
                    etiketIds.Contains(x.EtiketId.Value) &&

                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value >= baslangic &&
                    x.KayitZamani.Value < bitisExclusive)
                .ToListAsync(cancellationToken);
        }
        private sealed class TankSaatlikHamData2
        {
            public int SuKaynakId { get; set; }

            public DateTime IlkZaman { get; set; }

            public decimal MinHacim { get; set; }

            public decimal OrtalamaHacim { get; set; }

            public decimal MaxHacim { get; set; }
        }
    }
}