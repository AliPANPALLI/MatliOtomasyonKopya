using KumesScada.Domain.DbViewModels;
using KumesScada.Domain.Interfaces;
using KumesScada.Domain.Models;
using KumesScada.Domain.Models.ReportModels;
using KumesScada.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;

namespace KumesScada.Infrastructure.Repositories
{
    public class KumesRepository : Repository<Kumes>, IKumesRepository
    {
        private readonly KumesScadaDbContext _context;
        public KumesRepository(KumesScadaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<AlarmLog>> GetAlarmLogForKumes(int kumesId)
        {
            var alarmLogs = await _context.AlarmLog.Where(x => x.KumesId == kumesId /*&& x.KayitZamani.Value.Date == DateTime.Now.Date*/ && x.Silindi != true).OrderByDescending(x => x.KayitZamani).ToListAsync();
            return alarmLogs;
        }

        public async Task<List<AlarmLog>> GetAktifAlarmLogForKumes(int kumesId)
        {
            var alarmLogs = await _context.AlarmLog.Where(x => x.KumesId == kumesId && x.Aktif != false /*&& x.KayitZamani.Value.Date == DateTime.Now.Date*/ && x.Silindi != true).OrderByDescending(x => x.KayitZamani).ToListAsync();
            return alarmLogs;
        }

        public async Task<List<AlarmLog>> GetPasifAlarmLogForKumes(int kumesId)
        {
            var alarmLogs = await _context.AlarmLog.Where(x => x.KumesId == kumesId && x.KayitZamani.Value.Date >= DateTime.Now.Date.AddDays(-10) && x.Silindi != true && x.Aktif == false).OrderByDescending(x => x.KayitZamani).ToListAsync();
            return alarmLogs;
        }

        public async Task<List<HayvanCins>> GetAllHayvanCins()
        {
            return await _context.HayvanCins.ToListAsync();
        }

        public async Task<bool> SaveTesisYemRasyonAyarlariByLokasyonId(TesisYemRasyonAyarModel tesisYemRasyonAyarModel, CancellationToken cancellationToken)
        {
            try
            {
                var bugun = DateTime.Today;
                int diff = (7 + (bugun.DayOfWeek - DayOfWeek.Thursday)) % 7;
                var periyotBaslangic = bugun.AddDays(-diff);
                var periyotBitis = periyotBaslangic.AddDays(7);

                // ── 1. Mevcut periyottaki aktif YemRasyon kayıtlarını çek ──────────
                var mevcutYemRasyonlar = await _context.YemRasyon
                    .Where(x => x.LokasyonId == tesisYemRasyonAyarModel.LokasyonId
                             && x.Silindi == false
                             && x.GecerlilikBaslangicZamani <= periyotBitis
                             && x.GecerlilikBitisZamani > periyotBaslangic)
                    .ToListAsync(cancellationToken);

                // ── 2. Artık gönderilmeyen YemRasyon → soft delete ─────────────────
                var gonderilen = tesisYemRasyonAyarModel.FeedRows;

                var silinecekYemRasyonlar = mevcutYemRasyonlar
                    .Where(m => !gonderilen.Any(g =>
                        g.YemAdi == m.YemAdi &&
                        g.Rasyon == m.Rasyon &&
                        g.BirimFiyat == m.BirimFiyat))
                    .ToList();

                foreach (var silinecek in silinecekYemRasyonlar)
                {
                    silinecek.Silindi = true;

                    // Bu YemRasyon'a bağlı eslemeleri de soft delete yap
                    var bagliEslesmeler = await _context.KumesYemRasyonEsleme
                        .Where(x => x.YemRasyonId == silinecek.Id
                                 && x.Silindi == false
                                 && x.GecerlilikBaslangicZamani < periyotBitis
                                 && x.GecerlilikBitisZamani >= periyotBaslangic)
                        .ToListAsync(cancellationToken);

                    foreach (var esleme in bagliEslesmeler)
                    {
                        esleme.Silindi = true;
                    }
                }

                // ── 3. Gönderilen FeedRows'u işle (ekle veya mevcut bırak) ─────────
                foreach (var yemRasyon in gonderilen)
                {
                    var existingYemRasyon = mevcutYemRasyonlar
                        .FirstOrDefault(x =>
                            x.YemAdi == yemRasyon.YemAdi &&
                            x.Rasyon == yemRasyon.Rasyon &&
                            x.BirimFiyat == yemRasyon.BirimFiyat);

                    YemRasyon hedefYemRasyon;

                    if (existingYemRasyon == null)
                    {
                        hedefYemRasyon = new YemRasyon
                        {
                            LokasyonId = tesisYemRasyonAyarModel.LokasyonId,
                            YemAdi = yemRasyon.YemAdi,
                            Rasyon = yemRasyon.Rasyon,
                            BirimFiyat = yemRasyon.BirimFiyat,
                            GecerlilikBaslangicZamani = periyotBaslangic,
                            GecerlilikBitisZamani = periyotBitis,
                            Silindi = false
                        };
                        await _context.YemRasyon.AddAsync(hedefYemRasyon, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken); // ID için
                    }
                    else
                    {
                        hedefYemRasyon = existingYemRasyon;
                    }

                    // ── 4. Mevcut periyottaki aktif eslemeleri çek ─────────────────
                    var mevcutEslesmeler = await _context.KumesYemRasyonEsleme
                        .Where(x => x.LokasyonId == tesisYemRasyonAyarModel.LokasyonId
                                 && x.YemRasyonId == hedefYemRasyon.Id
                                 && x.Silindi == false
                                 && x.GecerlilikBaslangicZamani <= periyotBitis
                                 && x.GecerlilikBitisZamani > periyotBaslangic)
                        .ToListAsync(cancellationToken);

                    var ilgiliKumesSecimler = tesisYemRasyonAyarModel.KumesRasyonSelections
                        .Where(k => k.Rasyon == yemRasyon.Rasyon)
                        .ToList();

                    // ── 5. Artık gönderilmeyen esleme → soft delete ────────────────
                    var silinecekEslesmeler = mevcutEslesmeler
                        .Where(m => !ilgiliKumesSecimler.Any(k => k.KumesNo == m.KumesId))
                        .ToList();

                    foreach (var silinecek in silinecekEslesmeler)
                    {
                        silinecek.Silindi = true;
                    }

                    // ── 6. Yeni esleme ekle ────────────────────────────────────────
                    foreach (var kumesSecim in ilgiliKumesSecimler)
                    {
                        var existingEsleme = mevcutEslesmeler
                            .FirstOrDefault(x => x.KumesId == kumesSecim.KumesNo);

                        if (existingEsleme == null)
                        {
                            var yeniEsleme = new KumesYemRasyonEsleme
                            {
                                LokasyonId = tesisYemRasyonAyarModel.LokasyonId,
                                KumesId = kumesSecim.KumesNo,
                                YemRasyonId = hedefYemRasyon.Id,
                                KayitZamani = DateTime.Now,
                                GecerlilikBaslangicZamani = periyotBaslangic,
                                GecerlilikBitisZamani = periyotBitis,
                                Silindi = false
                            };
                            await _context.KumesYemRasyonEsleme.AddAsync(yeniEsleme, cancellationToken);
                        }
                        // Mevcut esleme varsa dokunmaya gerek yok
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<TesisYemRasyonAyarModel?> GetTesisYemRasyonAyarlariByLokasyonId(int lokasyonId, CancellationToken cancellationToken)
        {
            try
            {
                // Bugünün tarihine göre mevcut periyodun perşembe aralığını hesapla
                var bugun = DateTime.Today;
                int diff = (7 + (bugun.DayOfWeek - DayOfWeek.Thursday)) % 7;
                var periyotBaslangic = bugun.AddDays(-diff);           // Bu periyodun perşembesi
                var periyotBitis = periyotBaslangic.AddDays(7);                  // Sonraki perşembe

                // LokasyonId'ye ait tüm esleme kayitlarini YemRasyon ile birlikte cek
                //var eslemeListesi = await _context.KumesYemRasyonEsleme.Where(x => x.LokasyonId == lokasyonId && x.GecerlilikBaslangicZamani < periyotBitis && x.GecerlilikBitisZamani >= periyotBaslangic)
                //    .Join(
                //        _context.YemRasyon,
                //        esleme => esleme.YemRasyonId,
                //        yemRasyon => yemRasyon.Id,
                //        (esleme, yemRasyon) => new
                //        {
                //            esleme.KumesId,
                //            esleme.LokasyonId,
                //            yemRasyon.YemAdi,
                //            yemRasyon.Rasyon,
                //            yemRasyon.BirimFiyat
                //        })
                //    .ToListAsync(cancellationToken);
                var yemRasyonListesi = await _context.YemRasyon.Where(x => x.GecerlilikBaslangicZamani <= periyotBitis && (x.GecerlilikBitisZamani == null || x.GecerlilikBitisZamani > periyotBaslangic) && x.Silindi != true).ToListAsync(cancellationToken);

                if (!yemRasyonListesi.Any())
                    return null;

                // İlgili lokasyona ait kumes-rasyon eslemelerini ayrıca cek (LEFT JOIN yerine)
                var yemRasyonIdler = yemRasyonListesi.Select(x => x.Id).ToList();

                var eslemeListesi = await _context.KumesYemRasyonEsleme
                    .Where(x => x.LokasyonId == lokasyonId
                             && yemRasyonIdler.Contains(x.YemRasyonId)
                             && x.GecerlilikBaslangicZamani <= periyotBitis
                             && (x.GecerlilikBitisZamani == null || x.GecerlilikBitisZamani > periyotBaslangic))
                    .ToListAsync(cancellationToken);

                // Tekrar eden YemRasyon satirlarini distinct olarak topla
                var feedRows = yemRasyonListesi
            .Select(x => new TesisYemRasyonSatirModel
            {
                YemAdi = x.YemAdi,
                Rasyon = x.Rasyon,
                BirimFiyat = x.BirimFiyat
            })
            .ToList();

                // Sadece atama yapılmış kumes-rasyon eşleşmeleri
                var kumesRasyonSelections = eslemeListesi
                    .GroupBy(x => new { x.KumesId, YemRasyon = yemRasyonListesi.First(y => y.Id == x.YemRasyonId).Rasyon })
                    .Select(g => new TesisKumesRasyonModel
                    {
                        KumesNo = g.Key.KumesId,
                        Rasyon = g.Key.YemRasyon
                    })
                    .ToList();

                return new TesisYemRasyonAyarModel
                {
                    LokasyonId = lokasyonId,
                    FeedRows = feedRows,
                    KumesRasyonSelections = kumesRasyonSelections
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<vwLokasyon> GetLokasyonById(int lokasyonId)
        {
            var lokasyon = await _context.vwLokasyon.Where(x => x.Id == lokasyonId && x.Silindi != true).FirstOrDefaultAsync();
            return lokasyon;
        }

        public async Task<List<vwUserKumesYetkis>> GetAllUserKumesYetkiByUserId(string userId)
        {
            var userKumesYetkiler = await _context.vwUserKumesYetkis.Where(x => x.UserId == userId && x.Silindi != true).ToListAsync();
            return userKumesYetkiler;
        }

        public async Task<List<vwLokasyon>> GetAllLokasyonBySirketId(int sirketId)
        {
            var lokasyonlar = await _context.vwLokasyon.Where(x => x.SirketId == sirketId && x.Silindi != true).ToListAsync();
            return lokasyonlar;
        }

        // Kesinti anı olarak öncelikle KopmaZamani, o boşsa KayitZamani, o da boşsa BaglanmaZamani baz alınır.
        // (Bazı eski kayıtlarda KopmaZamani boş kalabiliyor.)
        // bitisZamani DIŞLAYICIDIR (< kullanılır); çağıran taraf seçilen bitiş gününün SONRAKİ günün
        // başlangıcını göndermelidir ki o gün de rapora dahil olsun.
        public async Task<List<PlcBaglantiKesintiGunlukRaporModel>> GetPlcBaglantiKesintiGunlukRaporuByKumesIds(List<int> kumesIds, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var kayitlar = await _context.PlcBaglantiKesintiLog
                .Where(x => kumesIds.Contains(x.KumesId)
                         && (x.KopmaZamani ?? x.KayitZamani ?? x.BaglanmaZamani) >= baslangicZamani
                         && (x.KopmaZamani ?? x.KayitZamani ?? x.BaglanmaZamani) < bitisZamani)
                .Select(x => new
                {
                    x.KumesId,
                    OlayZamani = (x.KopmaZamani ?? x.KayitZamani ?? x.BaglanmaZamani)!.Value,
                    x.KesintiSuresiSaniye,
                })
                .ToListAsync();

            return kayitlar
                .GroupBy(x => new { x.KumesId, Tarih = x.OlayZamani.Date })
                .Select(g => new PlcBaglantiKesintiGunlukRaporModel
                {
                    KumesId = g.Key.KumesId,
                    Tarih = g.Key.Tarih,
                    KopmaSayisi = g.Count(),
                    ToplamKesintiSaniye = g.Sum(x => x.KesintiSuresiSaniye ?? 0),
                    ToplamKesintiDakika = Math.Round(g.Sum(x => x.KesintiSuresiSaniye ?? 0) / 60m, 2),
                    EnUzunKesintiSaniye = g.Max(x => x.KesintiSuresiSaniye ?? 0),
                })
                .OrderByDescending(x => x.Tarih)
                .ToList();
        }

        public async Task<List<PlcBaglantiKesintiLog>> GetPlcBaglantiKesintiDetayByKumesId(int kumesId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var detaylar = await _context.PlcBaglantiKesintiLog
                .Where(x => x.KumesId == kumesId
                         && (x.KopmaZamani ?? x.KayitZamani ?? x.BaglanmaZamani) >= baslangicZamani
                         && (x.KopmaZamani ?? x.KayitZamani ?? x.BaglanmaZamani) < bitisZamani)
                .OrderByDescending(x => x.KopmaZamani ?? x.KayitZamani ?? x.BaglanmaZamani)
                .ToListAsync();
            return detaylar;
        }
    }
}