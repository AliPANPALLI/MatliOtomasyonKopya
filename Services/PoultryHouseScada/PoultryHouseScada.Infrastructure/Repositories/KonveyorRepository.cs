using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Domain.Models.ReportModels;
using PoultryHouseScada.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using static MatliOtomasyon.Shared.Application.Constants.ApplicationConstants;

namespace PoultryHouseScada.Infrastructure.Repositories
{
    public class KonveyorRepository : Repository<Konveyor>, IKonveyorRepository
    {
        private readonly KumesScadaDbContext _context;
        public KonveyorRepository(KumesScadaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<int?> GetKonveyorCizimIdByLokasyonId(int lokasyonId)
        {
            var cizimEsleme = await _context.CizimLokasyonEsleme.Where(x => x.LokasyonId == lokasyonId && x.Silindi != true).FirstOrDefaultAsync();
            return cizimEsleme != null ? cizimEsleme.CizimId : null;
        }

        public async Task<List<AlarmLog>> GetAlarmLogForKonveyor(int konveyorId)
        {
            var alarmLogs = await _context.AlarmLog.Where(x => x.KonveyorId == konveyorId && x.KayitZamani.Value.Date == DateTime.Now.Date && x.Silindi != true).OrderByDescending(x => x.KayitZamani).ToListAsync();
            return alarmLogs;
        }

        public async Task<List<AlarmLog>> GetAktifAlarmLogForKonveyor(int konveyorId)
        {
            var alarmLogs = await _context.AlarmLog.Where(x => x.KonveyorId == konveyorId && x.Aktif != false /*&& x.KayitZamani.Value.Date == DateTime.Now.Date*/ && x.Silindi != true).OrderByDescending(x => x.KayitZamani).ToListAsync();
            return alarmLogs;
        }

        public async Task<List<AlarmLog>> GetPasifAlarmLogForKonveyor(int konveyorId)
        {
            var alarmLogs = await _context.AlarmLog.Where(x => x.KonveyorId == konveyorId && x.KayitZamani.Value.Date >= DateTime.Now.Date.AddDays(-10) && x.Silindi != true && x.Aktif == false).OrderByDescending(x => x.KayitZamani).ToListAsync();
            return alarmLogs;
        }

        private string GetDurumAciklama(KonveyorRapor rapor, bool? veriYok = false)
        {
            if (veriYok.GetValueOrDefault(false))
            {
                return "Eksik Veri";
            }
            return rapor.Durum switch
            {
                KonveyorLogDurum.Ariza => rapor.Aciklama,
                KonveyorLogDurum.Bosta => "Duruşta",
                KonveyorLogDurum.Calisiyor => "Çalışıyor",
                _ => "Bilinmiyor"
            };
        }

        private string GetDurum(KonveyorRapor rapor, bool? veriYok = false)
        {
            if (veriYok.GetValueOrDefault(false))
            {
                return "Eksik Veri";
            }
            return rapor.Durum switch
            {
                KonveyorLogDurum.Ariza => "Arıza",
                KonveyorLogDurum.Bosta => "Duruşta",
                KonveyorLogDurum.Calisiyor => "Çalışıyor",
                _ => "Bilinmiyor"
            };
        }

        public async Task<List<KonveyorZamanCizelgesi>> GetZamanCizelgesiFromSecilenAsync(List<int> secilenKonveyorIdler, DateTime baslangicZamani, DateTime bitisZamani)
        {
            List<KonveyorRapor> tumRaporlar = new List<KonveyorRapor>();
            // Eğer baslangicZamani Now'dan büyükse
            if (baslangicZamani > DateTime.Now)
            {
                return new List<KonveyorZamanCizelgesi>();
            }
            else if (baslangicZamani >= bitisZamani)
            {
                return new List<KonveyorZamanCizelgesi>();
            }

            foreach (var secilenKonveyorId in secilenKonveyorIdler)
            {
                //başlangıç zamanından küçük ilk kaydı al
                var baslangicZamanindanKucukIlkKayit = await _context.KonveyorRapor.AsNoTracking()
                    .Where(x => secilenKonveyorId == x.KonveyorId.GetValueOrDefault(0) && x.KayitZamani.Value <= baslangicZamani).OrderByDescending(x => x.KayitZamani)
                    .FirstOrDefaultAsync();

                //bitis zamanından büyük ilk kaydı al
                var bitisZamanindanBuyukIlkKayit = await _context.KonveyorRapor.AsNoTracking()
                    .Where(x => secilenKonveyorId == x.KonveyorId.GetValueOrDefault(0) && x.KayitZamani.Value >= bitisZamani)
                    .FirstOrDefaultAsync();

                if (baslangicZamanindanKucukIlkKayit != null && bitisZamanindanBuyukIlkKayit != null)
                {
                    var asd = await _context.KonveyorRapor.AsNoTracking().Where(x => secilenKonveyorId == x.KonveyorId.GetValueOrDefault(0) && x.Id >= baslangicZamanindanKucukIlkKayit.Id && x.Id <= bitisZamanindanBuyukIlkKayit.Id).ToListAsync();
                    tumRaporlar.AddRange(asd);
                }
                else if (baslangicZamanindanKucukIlkKayit == null && bitisZamanindanBuyukIlkKayit != null)
                {
                    //  //01/07/2025 09:11-01/07/2025 10:40  2025-07-01 09:48   2025-07-01 10:40 kayıt gelmeli
                    var bitisZamanindanKucukIlkKayit = await _context.KonveyorRapor.AsNoTracking().Where(x => secilenKonveyorId == x.KonveyorId.GetValueOrDefault(0) && x.KayitZamani.Value <= bitisZamani).FirstOrDefaultAsync();
                    var asd = await _context.KonveyorRapor.AsNoTracking().Where(x => secilenKonveyorId == x.KonveyorId.GetValueOrDefault(0) && x.Id >= bitisZamanindanKucukIlkKayit.Id && x.Id <= bitisZamanindanBuyukIlkKayit.Id).ToListAsync();
                    tumRaporlar.AddRange(asd);
                }
                else if (baslangicZamanindanKucukIlkKayit != null && bitisZamanindanBuyukIlkKayit == null)
                {
                    //  //03/07/2025 14:43-03/07/2025 09:47  2025-07-01 14:48:13.310   2025-07-03 14:30:25.163 kayıt gelmeli
                    var asd = await _context.KonveyorRapor.AsNoTracking().Where(x => secilenKonveyorId == x.KonveyorId.GetValueOrDefault(0) && x.Id >= baslangicZamanindanKucukIlkKayit.Id).ToListAsync();
                    tumRaporlar.AddRange(asd);
                }
            }
            if (tumRaporlar.Count() == 0)
            {
                return new List<KonveyorZamanCizelgesi>();
            }
            tumRaporlar.OrderBy(x => x.KonveyorId).ThenBy(x => x.KayitZamani).ToList();

            var konveyorAdlari = await _context.Konveyor.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);

            var result = new List<KonveyorZamanCizelgesi>();

            var gruplar = tumRaporlar.GroupBy(x => x.KonveyorId);

            foreach (var grup in gruplar)
            {
                var raporlarSirali = grup.OrderBy(x => x.KayitZamani).ToList();
                KonveyorRapor? onceki = null;

                foreach (var simdiki in raporlarSirali)
                {


                    // Eğer kaydın KayitZamani bitisZamani'ndan büyükse, bu kaydı atla
                    if (simdiki.KayitZamani > bitisZamani)
                    {
                        continue; // Bu kaydı dışarıda bırak
                    }

                    if (onceki == null)
                    {
                        onceki = simdiki;
                        continue;
                    }

                    // Eğer durum değişmişse zaman çizelgesi oluştur
                    if (onceki.Durum != simdiki.Durum)
                    {
                        var zamanCizelgesi = new KonveyorZamanCizelgesi
                        {
                            KonveyorId = onceki.KonveyorId.GetValueOrDefault(0),
                            Name = konveyorAdlari.GetValueOrDefault(onceki.KonveyorId.GetValueOrDefault(0)) ?? $"Konveyor {onceki.KonveyorId}",
                            BaslamaZamani = onceki.KayitZamani >= baslangicZamani ? onceki.KayitZamani : baslangicZamani, // Baslama zamanını kontrol et
                            BitisZamani = simdiki.KayitZamani <= bitisZamani ? simdiki.KayitZamani : bitisZamani, // Bitis zamanını kontrol et
                            Durum = GetDurum(onceki, onceki.VeriYok),
                            DurumAciklama = GetDurumAciklama(onceki, onceki.VeriYok)
                        };

                        result.Add(zamanCizelgesi);
                        onceki = simdiki;
                    }
                }

                // Son kaydı ekle (bitmemişse ve bitisZamani'na uyuyorsa)
                if (onceki != null)
                {
                    // Eğer son kaydın KayitZamani bitisZamani'ndan büyükse, kaydı ekleme
                    if (onceki.KayitZamani <= bitisZamani)
                    {
                        result.Add(new KonveyorZamanCizelgesi
                        {
                            KonveyorId = onceki.KonveyorId.GetValueOrDefault(0),
                            Name = konveyorAdlari.GetValueOrDefault(onceki.KonveyorId.GetValueOrDefault(0)) ?? $"Konveyor {onceki.KonveyorId}",
                            BaslamaZamani = onceki.KayitZamani >= baslangicZamani ? onceki.KayitZamani : baslangicZamani,
                            BitisZamani = DateTime.Now <= bitisZamani ? DateTime.Now : bitisZamani,
                            Durum = GetDurum(onceki, onceki.VeriYok),
                            DurumAciklama = GetDurumAciklama(onceki, onceki.VeriYok)
                        });
                    }
                }
            }
            return result;
        }

        public async Task<List<KonveyorZamanCizelgesi>> GetKonveyorCalismaRaporuTablo(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            if (baslangicZamani >= bitisZamani)
                return new List<KonveyorZamanCizelgesi>();

            var konveyorler = await _context.Konveyor.Where(x => x.LokasyonId == konveyorLokasyonId && x.Silindi != true).ToListAsync();

            // 🔹 Sadece Hat (Tip=1)
            var hatKonveyorler = konveyorler.Where(x => x.Tip == 1).ToList();

            var hardEnd = (DateTime.Now <= bitisZamani) ? DateTime.Now : bitisZamani;

            // 🔹 Başlangıç öncesi son durumları yakalayabilmek için biraz öncesinden veri çekelim
            var prefetchStart = baslangicZamani.AddDays(-1);

            // 🔹 Tüm raporlar
            var tumRaporlar = await _context.KonveyorRapor
                .Where(x => x.KayitZamani >= prefetchStart && x.KayitZamani <= hardEnd.AddHours(1))
                .AsNoTracking()
                .ToListAsync();

            var result = new List<KonveyorZamanCizelgesi>();

            foreach (var hat in hatKonveyorler)
            {
                // 🧩 Hat altındaki sadece Tip=3 konveyörleri al
                var tip3KonveyorIdler = konveyorler
                    .Where(x => x.Tip == 3)
                    .Where(x =>
                    {
                        // Tip=3 konveyörlerin üst zincirinde bu hat var mı?
                        int? parentId = x.ParentId;
                        while (parentId.HasValue)
                        {
                            var parent = konveyorler.FirstOrDefault(k => k.Id == parentId.Value);
                            if (parent == null) break;
                            if (parent.Id == hat.Id) return true;
                            parentId = parent.ParentId;
                        }
                        return false;
                    })
                    .Select(x => x.Id)
                    .Distinct()
                    .ToList();

                if (!tip3KonveyorIdler.Any())
                    continue; // Altında Tip=3 yoksa bu hattı atla

                // 🔹 Bu hatta ait Tip=3 kayıtları
                var raporlar = tumRaporlar
                    .Where(x => x.KonveyorId.HasValue && tip3KonveyorIdler.Contains(x.KonveyorId.Value))
                    .OrderBy(x => x.KayitZamani)
                    .ToList();

                if (!raporlar.Any())
                {
                    // veri yoksa tek satır
                    result.Add(new KonveyorZamanCizelgesi
                    {
                        KonveyorId = hat.Id,
                        Name = hat.Name,
                        BaslamaZamani = baslangicZamani,
                        BitisZamani = hardEnd,
                        Fark = (hardEnd - baslangicZamani),
                        Durum = "Veri Yok",
                        DurumAciklama = "Veri Yok"
                    });
                    continue;
                }

                // 🔹 Zaman noktaları
                var timePoints = new SortedSet<DateTime>();
                timePoints.Add(baslangicZamani);
                foreach (var r in raporlar)
                {
                    if (r.KayitZamani >= baslangicZamani && r.KayitZamani <= hardEnd)
                        timePoints.Add(r.KayitZamani.Value);
                }
                timePoints.Add(hardEnd);

                // 🔹 Başlangıçtaki son bilinen durumları bul
                var childStates = new Dictionary<int, (bool? Calisiyor, bool? Bosta, bool? Ariza, bool? VeriYok)>();
                foreach (var cid in tip3KonveyorIdler)
                {
                    var lastBeforeStart = raporlar
                        .Where(x => x.KonveyorId == cid && x.KayitZamani <= baslangicZamani)
                        .OrderByDescending(x => x.KayitZamani)
                        .FirstOrDefault();

                    if (lastBeforeStart != null)
                        childStates[cid] = (lastBeforeStart.Calisiyor, lastBeforeStart.Bosta, lastBeforeStart.Ariza, lastBeforeStart.VeriYok);
                    else
                        childStates[cid] = (null, null, null, true); // hiç veri yoksa
                }

                // 🔹 Zaman bazlı lookup
                var byTime = raporlar
                    .Where(x => x.KayitZamani.HasValue)
                    .GroupBy(x => x.KayitZamani!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                KonveyorZamanCizelgesi? currentRow = null;
                string? lastDurum = null;
                string? lastArizaDurum = null;
                DateTime prev = timePoints.Min;

                foreach (var current in timePoints.Skip(1))
                {
                    var (hatDurum, hatAriza) = ComputeHatState(childStates);

                    // 🔸 Sadece durum değiştiyse yeni satır aç
                    if (currentRow == null || hatDurum != lastDurum || hatAriza != lastArizaDurum)
                    {
                        if (currentRow != null)
                        {
                            currentRow.BitisZamani = prev;
                            currentRow.Fark = (currentRow.BitisZamani.Value - currentRow.BaslamaZamani.Value);
                            if ((currentRow.BitisZamani - currentRow.BaslamaZamani)?.TotalSeconds > 0)
                                result.Add(currentRow);
                        }

                        currentRow = new KonveyorZamanCizelgesi
                        {
                            KonveyorId = hat.Id,
                            Name = hat.Name,
                            BaslamaZamani = prev,
                            Durum = hatDurum,
                            DurumAciklama = hatAriza
                        };

                        lastDurum = hatDurum;
                        lastArizaDurum = hatAriza;
                    }

                    // 🔹 Bu anda gelen log varsa alt konveyörlerin durumlarını güncelle
                    if (byTime.TryGetValue(current, out var reportsAtCurrent))
                    {
                        foreach (var rep in reportsAtCurrent)
                        {
                            childStates[rep.KonveyorId!.Value] = (rep.Calisiyor, rep.Bosta, rep.Ariza, rep.VeriYok);
                        }
                    }

                    prev = current;
                }

                // 🔹 Son satır (halen açık)
                if (currentRow != null)
                {
                    currentRow.BitisZamani = hardEnd;
                    currentRow.Fark = (currentRow.BitisZamani.Value - currentRow.BaslamaZamani.Value);
                    if ((currentRow.BitisZamani - currentRow.BaslamaZamani)?.TotalSeconds > 0)
                        result.Add(currentRow);
                }
            }

            return result
                .OrderBy(x => x.KonveyorId)
                .ThenBy(x => x.BaslamaZamani)
                .ToList();

            // 🔸 Alt fonksiyon: Hat durumu hesaplama
            (string Durum, string Ariza) ComputeHatState(Dictionary<int, (bool? Calisiyor, bool? Bosta, bool? Ariza, bool? VeriYok)> states)
            {
                bool tumuVeriYok = states.Values.All(s =>
                    (s.VeriYok == true) ||
                    (s.Calisiyor == null && s.Bosta == null && s.Ariza == null && s.VeriYok == null));

                if (tumuVeriYok)
                    return ("Veri Yok", "Veri Yok");

                bool calisiyorVarmi = states.Values.Any(s => s.Calisiyor == true);
                bool arizaVarmi = states.Values.Any(s => s.Ariza == true);

                string durum = calisiyorVarmi ? "Çalışıyor" : "Duruyor";
                string ariza = arizaVarmi ? "NOK" : "OK";

                return (durum, ariza);
            }
        }

        public async Task<List<KonveyorZamanCizelgesi>> GetKonveyorCalismaDurumuRaporu(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var rapor = await GetKonveyorCalismaRaporuTablo(konveyorLokasyonId, baslangicZamani, bitisZamani);

            // 🔹 Sadece çalışma durumlarını al (Çalışıyor / Duruyor / Veri Yok)
            var data = rapor.Select(x => new KonveyorZamanCizelgesi
            {
                KonveyorId = x.KonveyorId,
                Name = x.Name,
                BaslamaZamani = x.BaslamaZamani,
                BitisZamani = x.BitisZamani,
                Fark = x.Fark,
                Durum = x.Durum
            }).ToList();

            // 🔹 Ardışık aynı "Durum" kayıtlarını birleştir
            return MergeSameCalismaRows(data);
        }

        public async Task<List<KonveyorZamanCizelgesi>> GetKonveyorArizaDurumuRaporu(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var rapor = await GetKonveyorCalismaRaporuTablo(konveyorLokasyonId, baslangicZamani, bitisZamani);

            // 🔹 Sadece arıza durumlarını al (OK / NOK / Veri Yok)
            var data = rapor.Select(x => new KonveyorZamanCizelgesi
            {
                KonveyorId = x.KonveyorId,
                Name = x.Name,
                BaslamaZamani = x.BaslamaZamani,
                BitisZamani = x.BitisZamani,
                Fark = x.Fark,
                Durum = x.DurumAciklama
            }).ToList();

            // 🔹 Ardışık aynı "OK/NOK" kayıtlarını birleştir
            return MergeSameArizaRows(data);
        }
        public async Task<List<KonveyorZamanCizelgesi>> GetKonveyorKopmaRaporu(
    int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            if (baslangicZamani >= bitisZamani)
                return new List<KonveyorZamanCizelgesi>();

            var now = DateTime.Now;
            var hardEnd = (now <= bitisZamani) ? now : bitisZamani;

            // Başlangıç öncesinden al ki başlangıç anındaki state'i bilelim
            var prefetchStart = baslangicZamani.AddDays(-1);

            var konveyorler = await _context.Konveyor
                .Where(x => x.Silindi != true)
                .ToListAsync();

            // Lokasyondaki Hat (Tip=1)
            var hatKonveyorler = konveyorler
                .Where(x => x.Tip == 1 && x.LokasyonId == konveyorLokasyonId)
                .ToList();

            // İlgili rapor havuzu
            var tumRaporlar = await _context.KonveyorRapor
                .Where(x => x.KayitZamani >= prefetchStart && x.KayitZamani <= hardEnd.AddHours(1))
                .AsNoTracking()
                .ToListAsync();

            var result = new List<KonveyorZamanCizelgesi>();

            foreach (var hat in hatKonveyorler)
            {
                // Hat altındaki Tip=3 konveyörler
                var tip3KonveyorIdler = konveyorler
                    .Where(x => x.Tip == 3)
                    .Where(x =>
                    {
                        int? parentId = x.ParentId;
                        while (parentId.HasValue)
                        {
                            var parent = konveyorler.FirstOrDefault(k => k.Id == parentId.Value);
                            if (parent == null) break;
                            if (parent.Id == hat.Id) return true;
                            parentId = parent.ParentId;
                        }
                        return false;
                    })
                    .Select(x => x.Id)
                    .Distinct()
                    .ToList();

                if (!tip3KonveyorIdler.Any())
                    continue;

                // Hat altı raporlar (zaman sıralı)
                var raporlar = tumRaporlar
                    .Where(x => x.KonveyorId.HasValue && tip3KonveyorIdler.Contains(x.KonveyorId.Value))
                    .OrderBy(x => x.KayitZamani)
                    .ToList();

                if (!raporlar.Any())
                {
                    // Hiç veri yok
                    result.Add(new KonveyorZamanCizelgesi
                    {
                        KonveyorId = hat.Id,
                        Name = hat.Name,
                        HatName = hat.Name,
                        BaslamaZamani = baslangicZamani,
                        BitisZamani = hardEnd,
                        Durum = "Veri Yok",
                        DurumAciklama = "Veri Yok",
                        Fark = (hardEnd - baslangicZamani)
                    });
                    continue;
                }

                // === timePoints: sadece pencere içine düşen olay anları + pencere uçları
                var timePoints = new SortedSet<DateTime>();
                timePoints.Add(baslangicZamani);
                foreach (var r in raporlar)
                {
                    if (r.KayitZamani >= baslangicZamani && r.KayitZamani <= hardEnd)
                        timePoints.Add(r.KayitZamani!.Value);
                }
                timePoints.Add(hardEnd);

                // === Zaman -> rapor map'i (aynı anda birden fazla log olabilir)
                var byTime = raporlar
                    .Where(x => x.KayitZamani.HasValue)
                    .GroupBy(x => x.KayitZamani!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var hatDetayAraliklar = new List<(DateTime Start, DateTime End, int Cid, string CName, bool DevamEden)>();

                // === Tip-3 bazında sadece "geçiş" anlarında aralık üret
                foreach (var cid in tip3KonveyorIdler)
                {
                    var cName = konveyorler.FirstOrDefault(k => k.Id == cid)?.Name ?? $"Konveyor-{cid}";
                    var logsForCid = raporlar.Where(r => r.KonveyorId == cid).OrderBy(r => r.KayitZamani).ToList();
                    if (!logsForCid.Any())
                        continue;

                    // Başlangıç anındaki son bilinen durum
                    var lastBefore = logsForCid
                        .Where(l => l.KayitZamani <= baslangicZamani)
                        .OrderByDescending(l => l.KayitZamani)
                        .FirstOrDefault();

                    bool arizaAktif = lastBefore?.Ariza == true;
                    DateTime? aktifBaslangic = arizaAktif ? baslangicZamani : (DateTime?)null;

                    // SADECE geçiş anlarında işlem: timePoints boyunca ilerle
                    foreach (var t in timePoints)
                    {
                        if (byTime.TryGetValue(t, out var reportsAtT))
                        {
                            foreach (var rep in reportsAtT.Where(r => r.KonveyorId == cid))
                            {
                                bool yeni = rep.Ariza == true;
                                bool eski = arizaAktif;

                                if (!eski && yeni)
                                {
                                    // false → true: sadece başlangıcı işaretle
                                    arizaAktif = true;
                                    aktifBaslangic = t < baslangicZamani ? baslangicZamani : t;
                                }
                                else if (eski && !yeni)
                                {
                                    // true → false: TEK aralık yaz
                                    if (aktifBaslangic.HasValue)
                                    {
                                        var start = aktifBaslangic.Value;
                                        var end = t > hardEnd ? hardEnd : t;

                                        // pencere ile kesişiyorsa ekle
                                        if (end > start && end > baslangicZamani)
                                        {
                                            if (start < baslangicZamani) start = baslangicZamani;
                                            hatDetayAraliklar.Add((start, end, cid, cName, false));
                                        }
                                    }
                                    arizaAktif = false;
                                    aktifBaslangic = null;
                                }
                                // aynı statü devam ediyorsa satır üretme
                            }
                        }
                    }

                    // Pencere sonunda hâlâ arızadaysa (true) → tek aralık yaz
                    //if (arizaAktif && aktifBaslangic.HasValue)
                    //{
                    //    var start = aktifBaslangic.Value < baslangicZamani ? baslangicZamani : aktifBaslangic.Value;
                    //    var end = hardEnd;

                    //    // Son log gerçekten güncel mi? (devam ediyor etiketi için)
                    //    var sonLog = logsForCid.LastOrDefault();
                    //    bool devam = false;
                    //    if (sonLog?.Ariza == true)
                    //    {
                    //        var farkSn = (hardEnd - sonLog.KayitZamani!.Value).TotalSeconds;
                    //        devam = farkSn <= 120; // son 2 dk içindeyse "devam ediyor"
                    //    }

                    //    if (end > start)
                    //        hatDetayAraliklar.Add((start, end, cid, cName, devam));
                    //}
                    // Pencere sonunda hâlâ arızadaysa (true) → "Kopma Devam Ediyor"
                    if (arizaAktif && aktifBaslangic.HasValue)
                    {
                        var start = aktifBaslangic.Value < baslangicZamani ? baslangicZamani : aktifBaslangic.Value;
                        var end = hardEnd;

                        // OK’a dönüş yok → kesin devam ediyor
                        bool devam = true;

                        if (end > start)
                            hatDetayAraliklar.Add((start, end, cid, cName, devam));
                    }

                }

                // === Detay satırları ekle
                foreach (var a in hatDetayAraliklar.OrderBy(x => x.Start))
                {
                    result.Add(new KonveyorZamanCizelgesi
                    {
                        KonveyorId = a.Cid,
                        Name = a.CName,         // Kopma Noktası (Tip-3)
                        HatName = hat.Name,     // Hat adı
                        BaslamaZamani = a.Start,
                        BitisZamani = a.End,
                        Durum = "Arıza",
                        DurumAciklama = a.DevamEden ? "Kopma Devam Ediyor" : "Kopma",
                        Fark = a.End - a.Start
                    });
                }

                // === Hat bazında çakışmasız toplam süre (union/merge)
                var intervals = hatDetayAraliklar
                    .Select(x => (x.Start, x.End))
                    .OrderBy(t => t.Start)
                    .ToList();

                TimeSpan hatToplamKopma = TimeSpan.Zero;
                if (intervals.Count > 0)
                {
                    var merged = new List<(DateTime S, DateTime E)>();
                    var curS = intervals[0].Start;
                    var curE = intervals[0].End;

                    for (int i = 1; i < intervals.Count; i++)
                    {
                        var (s, e) = intervals[i];
                        if (s <= curE) // çakışan veya bitişik aralıklar birleştirilir
                        {
                            if (e > curE) curE = e;
                        }
                        else
                        {
                            merged.Add((curS, curE));
                            curS = s; curE = e;
                        }
                    }
                    merged.Add((curS, curE));

                    foreach (var m in merged)
                        hatToplamKopma += (m.E - m.S);
                }

                // Veri var ama hiç NOK aralığı yoksa → Kopma Yok
                if (raporlar.Any() && hatDetayAraliklar.Count == 0)
                {
                    result.Add(new KonveyorZamanCizelgesi
                    {
                        KonveyorId = hat.Id,
                        Name = hat.Name,
                        HatName = hat.Name,
                        BaslamaZamani = baslangicZamani,
                        BitisZamani = hardEnd,
                        Durum = "Kopma Yok",
                        DurumAciklama = "Bu hat altında kopma bulunamadı",
                        Fark = hardEnd - baslangicZamani
                    });
                }

                // İsteğe bağlı: Hat özet satırı
                result.Add(new KonveyorZamanCizelgesi
                {
                    KonveyorId = hat.Id,
                    Name = hat.Name,
                    HatName = hat.Name,
                    BaslamaZamani = baslangicZamani,
                    BitisZamani = hardEnd,
                    Durum = "Özet",
                    DurumAciklama = $"Toplam Kopma Süresi (çakışmasız): {hatToplamKopma:hh\\:mm\\:ss}",
                    Fark = hatToplamKopma
                });
            }

            return result
                .OrderBy(x => x.KonveyorId)
                .ThenBy(x => x.BaslamaZamani)
                .ToList();
        }




        public async Task<List<KonveyorZamanCizelgesi>> GetKonveyorKopmaRaporu2(int konveyorLokasyonId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            if (baslangicZamani >= bitisZamani)
                return new List<KonveyorZamanCizelgesi>();

            var konveyorler = await _context.Konveyor
                .Where(x => x.Silindi != true)
                .ToListAsync();

            // Sadece Hat (Tip=1)
            var hatKonveyorler = konveyorler.Where(x => x.Tip == 1 && x.LokasyonId == konveyorLokasyonId).ToList();

            var hardEnd = (DateTime.Now <= bitisZamani) ? DateTime.Now : bitisZamani;

            // Öncesinden veri al (durum geçişlerini yakalamak için)
            var prefetchStart = baslangicZamani.AddDays(-1);

            var tumRaporlar = await _context.KonveyorRapor
                .Where(x => x.KayitZamani >= prefetchStart && x.KayitZamani <= hardEnd.AddHours(1))
                .AsNoTracking()
                .ToListAsync();

            var result = new List<KonveyorZamanCizelgesi>();

            // Her hat için çalış
            foreach (var hat in hatKonveyorler)
            {
                // Hat altındaki Tip=3 konveyörleri bul
                var tip3KonveyorIdler = konveyorler.Where(x => x.Tip == 3)
                    .Where(x =>
                    {
                        int? parentId = x.ParentId;
                        while (parentId.HasValue)
                        {
                            var parent = konveyorler.FirstOrDefault(k => k.Id == parentId.Value);
                            if (parent == null) break;
                            if (parent.Id == hat.Id) return true;
                            parentId = parent.ParentId;
                        }
                        return false;
                    }).Select(x => x.Id).ToList();

                if (!tip3KonveyorIdler.Any())
                    continue;

                // Hat altındaki loglar
                var raporlar = tumRaporlar.Where(x => x.KonveyorId.HasValue && tip3KonveyorIdler.Contains(x.KonveyorId.Value)).OrderBy(x => x.KayitZamani).ToList();

                if (!raporlar.Any())
                {
                    // veri yoksa "Veri Yok" kaydı ekle
                    result.Add(new KonveyorZamanCizelgesi
                    {
                        KonveyorId = hat.Id,
                        Name = hat.Name,
                        HatName = hat.Name,
                        BaslamaZamani = baslangicZamani,
                        BitisZamani = hardEnd,
                        Durum = "Veri Yok",
                        DurumAciklama = "Veri Yok",
                        Fark = hardEnd - baslangicZamani
                    });
                    continue;
                }
                bool kopmaVarMi = false;
                // Her Tip=3 konveyör için NOK -> OK aralıklarını bul
                foreach (var cid in tip3KonveyorIdler)
                {
                    var konveyorAdi = konveyorler.FirstOrDefault(k => k.Id == cid)?.Name ?? $"Konveyor-{cid}";

                    var logs = raporlar.Where(x => x.KonveyorId == cid).OrderBy(x => x.KayitZamani).ToList();
                    if (!logs.Any()) continue;

                    KonveyorRapor? aktifKopma = null;

                    foreach (var log in logs)
                    {
                        // NOK başladıysa (Arıza)
                        if (log.Ariza == true && aktifKopma == null)
                        {
                            aktifKopma = log;
                        }
                        // OK olduğunda arıza bitti
                        else if (log.Ariza == false && aktifKopma != null)
                        {
                            var fark = log.KayitZamani!.Value - aktifKopma.KayitZamani!.Value;

                            result.Add(new KonveyorZamanCizelgesi
                            {
                                KonveyorId = cid,
                                Name = konveyorAdi, // Kopma Noktası (Tip=3 adı)
                                HatName = hat.Name,
                                BaslamaZamani = aktifKopma.KayitZamani,
                                BitisZamani = log.KayitZamani,
                                Durum = "Arıza",
                                DurumAciklama = "Kopma",
                                Fark = fark
                            });
                            aktifKopma = null;
                            kopmaVarMi = true;
                        }
                    }

                    // Eğer arıza halen devam ediyorsa (OK gelmemiş)
                    if (aktifKopma != null)
                    {
                        var fark = hardEnd - aktifKopma.KayitZamani!.Value;
                        result.Add(new KonveyorZamanCizelgesi
                        {
                            KonveyorId = cid,
                            Name = konveyorAdi,
                            HatName = hat.Name,
                            BaslamaZamani = aktifKopma.KayitZamani,
                            BitisZamani = hardEnd,
                            Durum = "Arıza",
                            DurumAciklama = "Kopma Devam Ediyor",
                            Fark = fark
                        });
                        kopmaVarMi = true;
                    }
                }
                if (!kopmaVarMi)
                {
                    result.Add(new KonveyorZamanCizelgesi
                    {
                        KonveyorId = hat.Id,
                        Name = hat.Name,
                        HatName = hat.Name,
                        BaslamaZamani = baslangicZamani,
                        BitisZamani = hardEnd,
                        Durum = "Kopma Yok",
                        DurumAciklama = "Bu hat altında kopma bulunamadı",
                        Fark = hardEnd - baslangicZamani
                    });
                }
            }

            // 🔹 Zaman ve hat sırasına göre sırala
            return result
                .OrderBy(x => x.KonveyorId)
                .ThenBy(x => x.BaslamaZamani)
                .ToList();
        }

        private List<KonveyorZamanCizelgesi> MergeSameCalismaRows(List<KonveyorZamanCizelgesi> data)
        {
            var merged = new List<KonveyorZamanCizelgesi>();
            KonveyorZamanCizelgesi? last = null;

            foreach (var row in data.OrderBy(x => x.KonveyorId).ThenBy(x => x.BaslamaZamani))
            {
                if (last == null)
                {
                    last = row;
                    continue;
                }

                bool same = last.KonveyorId == row.KonveyorId &&
                            last.Durum == row.Durum &&
                            last.BitisZamani.HasValue &&
                            row.BaslamaZamani.HasValue &&
                            Math.Abs((row.BaslamaZamani.Value - last.BitisZamani.Value).TotalSeconds) < 2;

                if (same)
                {
                    // Birleştir
                    last.BitisZamani = row.BitisZamani;
                    last.Fark = (last.BitisZamani.Value - last.BaslamaZamani.Value);
                }
                else
                {
                    merged.Add(last);
                    last = row;
                }
            }

            if (last != null)
                merged.Add(last);

            return merged;
        }

        // 🔸 Arıza durumu için birleştirici
        private List<KonveyorZamanCizelgesi> MergeSameArizaRows(List<KonveyorZamanCizelgesi> data)
        {
            var merged = new List<KonveyorZamanCizelgesi>();
            KonveyorZamanCizelgesi? last = null;

            foreach (var row in data.OrderBy(x => x.KonveyorId).ThenBy(x => x.BaslamaZamani))
            {
                if (last == null)
                {
                    last = row;
                    continue;
                }

                bool same = last.KonveyorId == row.KonveyorId &&
                            last.Durum == row.Durum &&
                            last.BitisZamani.HasValue &&
                            row.BaslamaZamani.HasValue &&
                            Math.Abs((row.BaslamaZamani.Value - last.BitisZamani.Value).TotalSeconds) < 2;

                if (same)
                {
                    last.BitisZamani = row.BitisZamani;
                    last.Fark = (last.BitisZamani.Value - last.BaslamaZamani.Value);
                }
                else
                {
                    merged.Add(last);
                    last = row;
                }
            }

            if (last != null)
                merged.Add(last);

            return merged;
        }


        public async Task<List<KonveyorZamanListesi>> GetZamanListesiFromSecilenAsync(List<int> secilenKonveyorIdler, DateTime baslangicZamani, DateTime bitisZamani)
        {
            List<KonveyorRapor> tumRaporlar = new List<KonveyorRapor>();
            // Eğer baslangicZamani Now'dan büyükse
            if (baslangicZamani > DateTime.Now)
            {
                return new List<KonveyorZamanListesi>();
            }
            else if (baslangicZamani >= bitisZamani)
            {
                return new List<KonveyorZamanListesi>();
            }

            foreach (var secilenKonveyorId in secilenKonveyorIdler)
            {
                //başlangıç zamanından küçük ilk kaydı al
                var baslangicZamanindanKucukIlkKayit = await _context.KonveyorRapor.AsNoTracking()
                    .Where(x => secilenKonveyorId == x.KonveyorId.GetValueOrDefault(0) && x.KayitZamani.Value <= baslangicZamani).OrderByDescending(x => x.KayitZamani)
                    .FirstOrDefaultAsync();

                //bitis zamanından büyük ilk kaydı al
                var bitisZamanindanBuyukIlkKayit = await _context.KonveyorRapor.AsNoTracking()
                    .Where(x => secilenKonveyorId == x.KonveyorId.GetValueOrDefault(0) && x.KayitZamani.Value >= bitisZamani)
                    .FirstOrDefaultAsync();

                if (baslangicZamanindanKucukIlkKayit != null && bitisZamanindanBuyukIlkKayit != null)
                {
                    var asd = await _context.KonveyorRapor.AsNoTracking().Where(x => secilenKonveyorId == x.KonveyorId.GetValueOrDefault(0) && x.Id >= baslangicZamanindanKucukIlkKayit.Id && x.Id <= bitisZamanindanBuyukIlkKayit.Id).ToListAsync();
                    tumRaporlar.AddRange(asd);
                }
                else if (baslangicZamanindanKucukIlkKayit == null && bitisZamanindanBuyukIlkKayit != null)
                {
                    //  //01/07/2025 09:11-01/07/2025 10:40  2025-07-01 09:48   2025-07-01 10:40 kayıt gelmeli
                    var bitisZamanindanKucukIlkKayit = await _context.KonveyorRapor.AsNoTracking().Where(x => secilenKonveyorId == x.KonveyorId.GetValueOrDefault(0) && x.KayitZamani.Value <= bitisZamani).FirstOrDefaultAsync();
                    var asd = await _context.KonveyorRapor.AsNoTracking().Where(x => secilenKonveyorId == x.KonveyorId.GetValueOrDefault(0) && x.Id >= bitisZamanindanKucukIlkKayit.Id && x.Id <= bitisZamanindanBuyukIlkKayit.Id).ToListAsync();
                    tumRaporlar.AddRange(asd);
                }
                else if (baslangicZamanindanKucukIlkKayit != null && bitisZamanindanBuyukIlkKayit == null)
                {
                    //  //03/07/2025 14:43-03/07/2025 09:47  2025-07-01 14:48:13.310   2025-07-03 14:30:25.163 kayıt gelmeli
                    var asd = await _context.KonveyorRapor.AsNoTracking().Where(x => secilenKonveyorId == x.KonveyorId.GetValueOrDefault(0) && x.Id >= baslangicZamanindanKucukIlkKayit.Id).ToListAsync();
                    tumRaporlar.AddRange(asd);
                }
            }
            if (tumRaporlar.Count() == 0)
            {
                return new List<KonveyorZamanListesi>();
            }
            tumRaporlar.OrderBy(x => x.KonveyorId).ThenBy(x => x.KayitZamani).ToList();

            var konveyorAdlari = await _context.Konveyor.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);

            var result = new List<KonveyorZamanCizelgesi>();

            var gruplar = tumRaporlar.GroupBy(x => x.KonveyorId);

            foreach (var grup in gruplar)
            {
                var raporlarSirali = grup.OrderBy(x => x.KayitZamani).ToList();
                KonveyorRapor? onceki = null;

                foreach (var simdiki in raporlarSirali)
                {


                    // Eğer kaydın KayitZamani bitisZamani'ndan büyükse, bu kaydı atla
                    if (simdiki.KayitZamani > bitisZamani)
                    {
                        continue; // Bu kaydı dışarıda bırak
                    }

                    if (onceki == null)
                    {
                        onceki = simdiki;
                        continue;
                    }

                    // Eğer durum değişmişse zaman çizelgesi oluştur
                    if (onceki.Durum != simdiki.Durum)
                    {
                        var zamanCizelgesi = new KonveyorZamanCizelgesi
                        {
                            KonveyorId = onceki.KonveyorId.GetValueOrDefault(0),
                            Name = konveyorAdlari.GetValueOrDefault(onceki.KonveyorId.GetValueOrDefault(0)) ?? $"Konveyor {onceki.KonveyorId}",
                            BaslamaZamani = onceki.KayitZamani >= baslangicZamani ? onceki.KayitZamani : baslangicZamani, // Baslama zamanını kontrol et
                            BitisZamani = simdiki.KayitZamani <= bitisZamani ? simdiki.KayitZamani : bitisZamani, // Bitis zamanını kontrol et
                            Durum = GetDurum(onceki, onceki.VeriYok),
                            DurumAciklama = GetDurumAciklama(onceki, onceki.VeriYok)
                        };

                        result.Add(zamanCizelgesi);
                        onceki = simdiki;
                    }
                }

                // Son kaydı ekle (bitmemişse ve bitisZamani'na uyuyorsa)
                if (onceki != null)
                {
                    // Eğer son kaydın KayitZamani bitisZamani'ndan büyükse, kaydı ekleme
                    if (onceki.KayitZamani <= bitisZamani)
                    {
                        result.Add(new KonveyorZamanCizelgesi
                        {
                            KonveyorId = onceki.KonveyorId.GetValueOrDefault(0),
                            Name = konveyorAdlari.GetValueOrDefault(onceki.KonveyorId.GetValueOrDefault(0)) ?? $"Konveyor {onceki.KonveyorId}",
                            BaslamaZamani = onceki.KayitZamani >= baslangicZamani ? onceki.KayitZamani : baslangicZamani,
                            BitisZamani = DateTime.Now <= bitisZamani ? DateTime.Now : bitisZamani,
                            Durum = GetDurum(onceki, onceki.VeriYok),
                            DurumAciklama = GetDurumAciklama(onceki, onceki.VeriYok)
                        });
                    }
                }
            }

            var summaryList = result
    .GroupBy(x => new { x.KonveyorId, x.Name })
    .Select(grup => new
    {
        KonveyorId = grup.Key.KonveyorId,
        Name = grup.Key.Name,
        CalisiyorSuresi = grup
            .Where(x => x.Durum == "Çalışıyor")
            .Sum(x => (x.BitisZamani - x.BaslamaZamani).Value.TotalHours),

        BostaSuresi = grup
            .Where(x => x.Durum == "Duruşta")
            .Sum(x => (x.BitisZamani - x.BaslamaZamani).Value.TotalHours),

        ArizaSuresi = grup
            .Where(x => x.Durum == "Arıza")
            .Sum(x => (x.BitisZamani - x.BaslamaZamani).Value.TotalHours),

        EksikVeriSuresi = grup
            .Where(x => x.Durum == "Eksik Veri")
            .Sum(x => (x.BitisZamani - x.BaslamaZamani).Value.TotalHours)
    })
    .ToList();

            return summaryList.Select(x => new KonveyorZamanListesi
            {
                KonveyorId = x.KonveyorId,
                Name = x.Name,
                CalisiyorSuresi = x.CalisiyorSuresi,
                BostaSuresi = x.BostaSuresi,
                ArizaSuresi = x.ArizaSuresi,
            }).ToList();
        }
    }
}