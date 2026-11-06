using FastMember;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using System.Data;
using System.Text.Json;
using VerimGidaYtk.Worker.Core;
using VerimGidaYtk.Worker.Models;
using static SharedKernel.ApplicationConstants;
using AlarmLog = PoultryHouseScada.Domain.Models.AlarmLog;

namespace VerimGidaYtk.Worker
{
    public class VerimGidaYtkForRaporWorker : BackgroundService
    {
        private readonly ILogger<VerimGidaYtkForRaporWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceProvider _serviceProvider;
        //private readonly AlarmCacheService _alarmCacheService;
        private readonly SqlConnection _sqlConnection;
        private List<Alarmlar> alarmlar = new List<Alarmlar> { };
        private readonly Dictionary<int, BekleyenDurumDegisimi> bekleyenDurumDegisimleri = new();
        private readonly TimeSpan minimumDurusSuresi = TimeSpan.FromMinutes(2);
        private class BekleyenDurumDegisimi
        {
            public string DurumKey { get; set; }
            public DateTime IlkGorulmeZamani { get; set; }
        }
        public VerimGidaYtkForRaporWorker(ILogger<VerimGidaYtkForRaporWorker> logger, IServiceScopeFactory scopeFactory, IServiceProvider serviceProvider, /*AlarmCacheService alarmCacheService,*/ SqlConnection sqlConnection)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _serviceProvider = serviceProvider;
            //_alarmCacheService = alarmCacheService;
            _sqlConnection = sqlConnection;
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("VerimGidaYtkWorker working: {time}", DateTimeOffset.Now);
                await UpdateServisDurumAsync("VerimGidaYtkWorker", 2, "Servis baþlatýlýyor");
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await UpdateServisDurumAsync("VerimGidaYtkWorker", 0, ex.Message);
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    var sharedDb = scope.ServiceProvider.GetRequiredService<SharedDbContext>();
                    var alarmCacheService = scope.ServiceProvider.GetRequiredService<AlarmCacheService>();

                    #region Alarmlar
                    //Alarmlar
                    alarmlar = await sharedDb.Alarmlar.Where(x => x.KonveyorId == 19).ToListAsync();
                    var dataAlarm = await GetAlarmLogDataVerimGidaHat12ForBulkInsert(19, alarmCacheService, stoppingToken);
                    await BulkInsertAlarmLogDataAsync(_sqlConnection, dataAlarm);
                    var dataAlarm34 = await GetAlarmLogDataVerimGidaHat34ForBulkInsert(19, alarmCacheService, stoppingToken);
                    await BulkInsertAlarmLogDataAsync(_sqlConnection, dataAlarm34);
                    #endregion
                    var dataEtiket12 = await GetEtiketLogDataVerimGidaHat12ForBulkInsert(19, alarmCacheService, stoppingToken);
                    await BulkInsertEtiketLogDataAsync(_sqlConnection, dataEtiket12);
                    var dataEtiket34 = await GetEtiketLogDataVerimGidaHat34ForBulkInsert(19, alarmCacheService, stoppingToken);
                    await BulkInsertEtiketLogDataAsync(_sqlConnection, dataEtiket34);

                    await ExecutionForKonveyorRapor(19, stoppingToken);
                    await UpdateServisDurumAsync("VerimGidaYtkWorker", 1, "Servis çalýþýyor", true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    await UpdateServisDurumAsync("VerimGidaYtkWorker", 0, ex.Message);
                }
                finally
                {
                    await Task.Delay(10000, stoppingToken);
                }
            }
        }
        private async Task ExecutionForKonveyorRapor(int lokasyonId, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

                var now = DateTime.Now;
                var eklenecekKonveyorRaporlar = new List<KonveyorRapor>();
                var konveyorEtiketler = await db.Etiket.AsNoTracking().ToListAsync();
                var tumkonveyorler = await db.Konveyor.AsNoTracking().Where(x => x.LokasyonId == 19).ToListAsync();
                var konveyorler = tumkonveyorler.Where(x => x.Tip == KonveyorTip.Konveyor).ToList();
                var degerler = await db.EtiketLogKonveyor.AsNoTracking().Where(x => x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).ToListAsync();
                var sonRaporlar = await db.KonveyorRapor.AsNoTracking()
                    .Where(x => tumkonveyorler.Select(y => y.Id).Contains(x.KonveyorId.GetValueOrDefault()) && x.KayitZamani.Value.Date == now.Date)
                    .OrderByDescending(x => x.Id).ToListAsync();

                var kontrolSuresi = TimeSpan.FromMinutes(6);
                var sabahBeklemeSuresi = TimeSpan.FromMinutes(5);
                var gununIlkSaatleri = now.TimeOfDay < sabahBeklemeSuresi;

                // Veri tamamen yoksa ve sabah deðilse VeriYok = true kaydý atýlýr
                if (degerler.Count == 0)
                {
                    if (!gununIlkSaatleri && sonRaporlar.Count == 0)
                    {
                        foreach (var konveyor in tumkonveyorler)
                        {
                            eklenecekKonveyorRaporlar.Add(new KonveyorRapor
                            {
                                KonveyorId = konveyor.Id,
                                KayitZamani = now,
                                VeriYok = true
                            });
                        }
                        await db.KonveyorRapor.AddRangeAsync(eklenecekKonveyorRaporlar);
                        await db.SaveChangesAsync();
                    }
                }
                else
                {
                    foreach (var konveyor in konveyorler)
                    {
                        var konveyorEtiket = konveyorEtiketler.FirstOrDefault(x => x.Id == konveyor.EtiketIdForKonveyorRapor);
                        if (konveyorEtiket == null)
                        {
                            _logger.LogWarning($"Konveyor {konveyor.Id} için etiket bulunamadý.");
                            continue;
                        }

                        var deger = degerler.FirstOrDefault(x => x.EtiketId == konveyorEtiket.Id);
                        var konveyorRapor = new KonveyorRapor
                        {
                            KonveyorId = konveyor.Id,
                            KayitZamani = now
                        };

                        // Veri yoksa veya veri çok eskiyse
                        if (deger == null)
                        {
                            konveyorRapor.VeriYok = true;
                        }
                        else if ((now - deger.KayitZamani.GetValueOrDefault()) > kontrolSuresi)
                        {
                            konveyorRapor.VeriYok = true;
                        }
                        else
                        {
                            switch (deger.Deger)
                            {
                                case KonveyorLogDurum.Bosta:
                                    konveyorRapor.Bosta = true;
                                    konveyorRapor.Durum = KonveyorLogDurum.Bosta;
                                    break;
                                case KonveyorLogDurum.Calisiyor:
                                    konveyorRapor.Calisiyor = true;
                                    konveyorRapor.Durum = KonveyorLogDurum.Calisiyor;
                                    break;
                                case KonveyorLogDurum.Ariza:
                                    konveyorRapor.Ariza = true;
                                    konveyorRapor.Durum = KonveyorLogDurum.Ariza;
                                    konveyorRapor.Aciklama = konveyor.Name + " koptu";
                                    break;
                            }
                        }

                        var sonRapor = sonRaporlar.Where(x => x.KonveyorId == konveyor.Id).OrderByDescending(x => x.KayitZamani).FirstOrDefault();

                        if (sonRapor != null)
                        {
                            if ((sonRapor.Durum == konveyorRapor.Durum
                                 && !konveyorRapor.VeriYok.GetValueOrDefault(false)
                                 && !sonRapor.VeriYok.GetValueOrDefault(false))
                                || (sonRapor.VeriYok == true && konveyorRapor.VeriYok == true))
                            {
                                continue;
                            }
                        }

                        // Yeni eklenecek kontrol:
                        if (!RaporEklenebilirMi(konveyorRapor, sonRapor, now))
                        {
                            continue;
                        }

                        eklenecekKonveyorRaporlar.Add(konveyorRapor);
                    }

                    // Bölüm hesaplamalarý
                    var bolumler = tumkonveyorler.Where(x => x.Tip == KonveyorTip.Bolum).ToList();
                    foreach (var bolum in bolumler)
                    {
                        var konveyorlerBolumdeki = konveyorler.Where(x => x.ParentId == bolum.Id).ToList();
                        var sonDegerler = degerler.Where(x => konveyorlerBolumdeki.Select(y => y.EtiketIdForKonveyorRapor).Contains(x.EtiketId)).OrderByDescending(x => x.Id).ToList();
                        var bolumDurum = KonveyorLogDurum.Bosta;

                        if (sonDegerler.Any(x => x.Deger == KonveyorLogDurum.Ariza))
                            bolumDurum = KonveyorLogDurum.Ariza;
                        else if (sonDegerler.Any(x => x.Deger == KonveyorLogDurum.Calisiyor))
                            bolumDurum = KonveyorLogDurum.Calisiyor;

                        var konveyorRapor = new KonveyorRapor
                        {
                            KonveyorId = bolum.Id,
                            KayitZamani = now,
                            Durum = bolumDurum
                        };
                        // Veri yoksa veya veri çok eskiyse
                        if (sonDegerler.Count() == 0)
                        {
                            konveyorRapor.VeriYok = true;
                        }
                        else if ((now - sonDegerler.First().KayitZamani.GetValueOrDefault()) > kontrolSuresi)
                        {
                            konveyorRapor.VeriYok = true;
                        }
                        else
                        {
                            switch (bolumDurum)
                            {
                                case KonveyorLogDurum.Bosta:
                                    konveyorRapor.Bosta = true;
                                    break;
                                case KonveyorLogDurum.Calisiyor:
                                    konveyorRapor.Calisiyor = true;
                                    break;
                                case KonveyorLogDurum.Ariza:
                                    konveyorRapor.Ariza = true;
                                    var arizaEtiketId = sonDegerler.FirstOrDefault(x => x.Deger == KonveyorLogDurum.Ariza)?.EtiketId;
                                    var arizaliKonveyor = konveyorlerBolumdeki.FirstOrDefault(x => x.EtiketIdForKonveyorRapor == arizaEtiketId)?.Name;
                                    konveyorRapor.Aciklama = arizaliKonveyor != null ? arizaliKonveyor + " koptu" : bolum.Name + " koptu";
                                    break;
                            }
                        }
                        var sonRapor = sonRaporlar.Where(x => x.KonveyorId == bolum.Id).OrderByDescending(x => x.KayitZamani).FirstOrDefault();

                        if (sonRapor != null)
                        {
                            if ((sonRapor.Durum == konveyorRapor.Durum
                                 && !konveyorRapor.VeriYok.GetValueOrDefault(false)
                                 && !sonRapor.VeriYok.GetValueOrDefault(false))
                                || (sonRapor.VeriYok == true && konveyorRapor.VeriYok == true))
                            {
                                continue;
                            }
                        }

                        // Yeni eklenecek kontrol:
                        if (!RaporEklenebilirMi(konveyorRapor, sonRapor, now))
                        {
                            continue;
                        }

                        eklenecekKonveyorRaporlar.Add(konveyorRapor);
                    }

                    // Hat hesaplamalarý
                    var hatlar = tumkonveyorler.Where(x => x.Tip == KonveyorTip.Hat).ToList();
                    foreach (var hat in hatlar)
                    {
                        var bolumIds = bolumler.Where(x => x.ParentId == hat.Id).Select(x => x.Id).ToList();
                        var konveyorlerHatta = konveyorler.Where(x => bolumIds.Contains(x.ParentId ?? 0)).ToList();
                        var sonDegerler = degerler.Where(x => konveyorlerHatta.Select(y => y.EtiketIdForKonveyorRapor).Contains(x.EtiketId)).OrderByDescending(x => x.Id).ToList();

                        var hatDurum = KonveyorLogDurum.Bosta;
                        if (sonDegerler.Any(x => x.Deger == KonveyorLogDurum.Ariza))
                            hatDurum = KonveyorLogDurum.Ariza;
                        else if (sonDegerler.Any(x => x.Deger == KonveyorLogDurum.Calisiyor))
                            hatDurum = KonveyorLogDurum.Calisiyor;

                        var konveyorRapor = new KonveyorRapor
                        {
                            KonveyorId = hat.Id,
                            KayitZamani = now,
                            Durum = hatDurum
                        };

                        // Veri yoksa veya veri çok eskiyse
                        if (sonDegerler.Count() == 0)
                        {
                            konveyorRapor.VeriYok = true;
                        }
                        else if ((now - sonDegerler.First().KayitZamani.GetValueOrDefault()) > kontrolSuresi)
                        {
                            konveyorRapor.VeriYok = true;
                        }
                        else
                        {
                            switch (hatDurum)
                            {
                                case KonveyorLogDurum.Bosta:
                                    konveyorRapor.Bosta = true;
                                    break;
                                case KonveyorLogDurum.Calisiyor:
                                    konveyorRapor.Calisiyor = true;
                                    break;
                                case KonveyorLogDurum.Ariza:
                                    konveyorRapor.Ariza = true;
                                    var arizaEtiketId = sonDegerler.FirstOrDefault(x => x.Deger == KonveyorLogDurum.Ariza)?.EtiketId;
                                    var arizaliKonveyor = konveyorlerHatta.FirstOrDefault(x => x.EtiketIdForKonveyorRapor == arizaEtiketId)?.Name;
                                    konveyorRapor.Aciklama = arizaliKonveyor != null ? arizaliKonveyor + " koptu" : hat.Name + " koptu";
                                    break;
                            }
                        }
                        var sonRapor = sonRaporlar.Where(x => x.KonveyorId == hat.Id).OrderByDescending(x => x.KayitZamani).FirstOrDefault();

                        if (sonRapor != null)
                        {
                            if ((sonRapor.Durum == konveyorRapor.Durum
                                 && !konveyorRapor.VeriYok.GetValueOrDefault(false)
                                 && !sonRapor.VeriYok.GetValueOrDefault(false))
                                || (sonRapor.VeriYok == true && konveyorRapor.VeriYok == true))
                            {
                                continue;
                            }
                        }

                        // Yeni eklenecek kontrol:
                        if (!RaporEklenebilirMi(konveyorRapor, sonRapor, now))
                        {
                            continue;
                        }

                        eklenecekKonveyorRaporlar.Add(konveyorRapor);
                    }

                    await db.KonveyorRapor.AddRangeAsync(eklenecekKonveyorRaporlar);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hata oluþtu:");
            }
        }

        private string GetDurumKey(KonveyorRapor rapor)
        {
            if (rapor.VeriYok == true)
                return "VERI_YOK";

            if (rapor.Ariza == true)
                return "ARIZA";

            if (rapor.Calisiyor == true)
                return "CALISIYOR";

            if (rapor.Bosta == true)
                return "BOSTA";

            return rapor.Durum?.ToString() ?? "BILINMIYOR";
        }

        private bool GeciciDurumMu(KonveyorRapor rapor)
        {
            // Arýza önemliyse anýnda yazýlsýn.
            // Sadece Boþta ve VeriYok kýsa süreliyse bekletelim.
            return rapor.Bosta == true || rapor.VeriYok == true;
        }

        private bool RaporEklenebilirMi(KonveyorRapor yeniRapor, KonveyorRapor sonRapor, DateTime now)
        {
            if (sonRapor == null)
                return true;

            var yeniDurumKey = GetDurumKey(yeniRapor);
            var sonDurumKey = GetDurumKey(sonRapor);

            // Durum aynýysa zaten yeni kayýt gerekmez.
            if (yeniDurumKey == sonDurumKey)
            {
                bekleyenDurumDegisimleri.Remove(yeniRapor.KonveyorId.GetValueOrDefault());
                return false;
            }

            // Çalýþýyor veya Arýza gibi durumlarý bekletmeden yaz.
            if (!GeciciDurumMu(yeniRapor))
            {
                bekleyenDurumDegisimleri.Remove(yeniRapor.KonveyorId.GetValueOrDefault());
                return true;
            }

            var konveyorId = yeniRapor.KonveyorId.GetValueOrDefault();

            if (!bekleyenDurumDegisimleri.TryGetValue(konveyorId, out var bekleyenDurum))
            {
                bekleyenDurumDegisimleri[konveyorId] = new BekleyenDurumDegisimi
                {
                    DurumKey = yeniDurumKey,
                    IlkGorulmeZamani = now
                };

                return false;
            }

            // Beklenen durum deðiþmiþse süreyi baþtan baþlat.
            if (bekleyenDurum.DurumKey != yeniDurumKey)
            {
                bekleyenDurumDegisimleri[konveyorId] = new BekleyenDurumDegisimi
                {
                    DurumKey = yeniDurumKey,
                    IlkGorulmeZamani = now
                };

                return false;
            }

            // Ayný geçici durum 5 dakikadýr devam ediyorsa artýk gerçek duruþ say.
            if (now - bekleyenDurum.IlkGorulmeZamani >= minimumDurusSuresi)
            {
                bekleyenDurumDegisimleri.Remove(konveyorId);
                return true;
            }

            return false;
        }
        private async Task<IEnumerable<AlarmLog>> GetAlarmLogDataVerimGidaHat12ForBulkInsert(int lokasyonId, AlarmCacheService _alarmCacheService, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;
                List<AlarmLog> alarmLogs = new List<AlarmLog>();
                YTKNodeRed? model;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    model = await kumesScadaDbContext.YTKNodeRed.AsNoTracking().AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.PlcAdi == "HAT12" && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                    if (model != null)
                    {
                        if (model.Durum == 1)
                        {
                            if (model.Veriler != null)
                            {
                                VerimGidaHat12NodeRedModel ytkNodeRedModel = JsonSerializer.Deserialize<VerimGidaHat12NodeRedModel>(model.Veriler);

                                var activeAlarms = await _alarmCacheService.GetAlarmLogsAsync(lokasyonId);

                                bool hat1konveyor1TermikNok = ytkNodeRedModel.Hat_1_Konv_1_TermikNok.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 837))
                                {
                                    if (hat1konveyor1TermikNok)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 837, AlarmEnum = 837, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 837)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!hat1konveyor1TermikNok)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 837);
                                    }
                                }
                                bool hat1konveyor2TermikNok = ytkNodeRedModel.Hat_1_Konv_2_TermikNok.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 838))
                                {
                                    if (hat1konveyor2TermikNok)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 838, AlarmEnum = 838, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 838)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!hat1konveyor2TermikNok)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 838);
                                    }
                                }
                                bool hat2konveyor1TermikNok = ytkNodeRedModel.Hat_2_Konv_1_TermikNok.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 839))
                                {
                                    if (hat2konveyor1TermikNok)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 839, AlarmEnum = 839, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 839)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!hat2konveyor1TermikNok)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 839);
                                    }
                                }
                                bool hat2konveyor2TermikNok = ytkNodeRedModel.Hat_2_Konv_2_TermikNok.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 840))
                                {
                                    if (hat2konveyor2TermikNok)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 840, AlarmEnum = 840, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 840)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!hat2konveyor2TermikNok)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 840);
                                    }
                                }
                                ///////////////////////////////////////////////////////
                                bool sensor111asKoptu = ytkNodeRedModel.Hat_1_Konv_1_Koptu_KopmaNoktasi_111AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 741))
                                {
                                    if (sensor111asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 741, AlarmEnum = 741, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 741)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor111asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 741);
                                    }
                                }
                                bool sensor111usKoptu = ytkNodeRedModel.Hat_1_Konv_1_Koptu_KopmaNoktasi_111US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 742))
                                {
                                    if (sensor111usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 742, AlarmEnum = 742, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 742)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor111usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 742);
                                    }
                                }
                                bool sensor112asKoptu = ytkNodeRedModel.Hat_1_Konv_1_Koptu_KopmaNoktasi_112AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 743))
                                {
                                    if (sensor112asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 743, AlarmEnum = 743, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 743)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor112asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 743);
                                    }
                                }
                                bool sensor112usKoptu = ytkNodeRedModel.Hat_1_Konv_1_Koptu_KopmaNoktasi_112US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 744))
                                {
                                    if (sensor112usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 744, AlarmEnum = 744, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 744)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor112usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 744);
                                    }
                                }
                                bool sensor113asKoptu = ytkNodeRedModel.Hat_1_Konv_1_Koptu_KopmaNoktasi_113AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 745))
                                {
                                    if (sensor113asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 745, AlarmEnum = 745, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 745)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor113asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 745);
                                    }
                                }
                                bool sensor113usKoptu = ytkNodeRedModel.Hat_1_Konv_1_Koptu_KopmaNoktasi_113US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 746))
                                {
                                    if (sensor113usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 746, AlarmEnum = 746, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 746)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor113usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 746);
                                    }
                                }
                                bool sensor114asKoptu = ytkNodeRedModel.Hat_1_Konv_1_Koptu_KopmaNoktasi_114AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 747))
                                {
                                    if (sensor114asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 747, AlarmEnum = 747, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 747)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor114asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 747);
                                    }
                                }
                                bool sensor114usKoptu = ytkNodeRedModel.Hat_1_Konv_1_Koptu_KopmaNoktasi_114US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 748))
                                {
                                    if (sensor114usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 748, AlarmEnum = 748, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 748)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor114usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 748);
                                    }
                                }
                                bool sensor115asKoptu = ytkNodeRedModel.Hat_1_Konv_1_Koptu_KopmaNoktasi_115AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 749))
                                {
                                    if (sensor115asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 749, AlarmEnum = 749, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 749)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor115asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 749);
                                    }
                                }
                                bool sensor115usKoptu = ytkNodeRedModel.Hat_1_Konv_1_Koptu_KopmaNoktasi_115US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 750))
                                {
                                    if (sensor115usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 750, AlarmEnum = 750, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 750)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor115usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 750);
                                    }
                                }
                                bool sensor116asKoptu = ytkNodeRedModel.Hat_1_Konv_1_Koptu_KopmaNoktasi_116AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 751))
                                {
                                    if (sensor116asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 751, AlarmEnum = 751, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 751)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor116asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 751);
                                    }
                                }
                                bool sensor116usKoptu = ytkNodeRedModel.Hat_1_Konv_1_Koptu_KopmaNoktasi_116US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 752))
                                {
                                    if (sensor116usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 752, AlarmEnum = 752, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 752)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor116usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 752);
                                    }
                                }
                                /////////
                                bool sensor120asKoptu = ytkNodeRedModel.Hat_1_Konv_2_Koptu_KopmaNoktasi_120AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 753))
                                {
                                    if (sensor120asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 753, AlarmEnum = 753, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 753)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor120asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 753);
                                    }
                                }
                                bool sensor120usKoptu = ytkNodeRedModel.Hat_1_Konv_2_Koptu_KopmaNoktasi_120US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 754))
                                {
                                    if (sensor120usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 754, AlarmEnum = 754, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 754)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor120usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 754);
                                    }
                                }
                                bool sensor121asKoptu = ytkNodeRedModel.Hat_1_Konv_2_Koptu_KopmaNoktasi_121AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 755))
                                {
                                    if (sensor121asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 755, AlarmEnum = 755, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 755)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor121asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 755);
                                    }
                                }
                                bool sensor121usKoptu = ytkNodeRedModel.Hat_1_Konv_2_Koptu_KopmaNoktasi_121US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 756))
                                {
                                    if (sensor121usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 756, AlarmEnum = 756, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 756)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor121usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 756);
                                    }
                                }
                                bool sensor122asKoptu = ytkNodeRedModel.Hat_1_Konv_2_Koptu_KopmaNoktasi_122AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 757))
                                {
                                    if (sensor122asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 757, AlarmEnum = 757, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 757)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor122asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 757);
                                    }
                                }
                                bool sensor122usKoptu = ytkNodeRedModel.Hat_1_Konv_2_Koptu_KopmaNoktasi_122US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 758))
                                {
                                    if (sensor122usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 758, AlarmEnum = 758, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 758)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor122usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 758);
                                    }
                                }
                                bool sensor123asKoptu = ytkNodeRedModel.Hat_1_Konv_2_Koptu_KopmaNoktasi_123AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 759))
                                {
                                    if (sensor123asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 759, AlarmEnum = 759, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 759)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor123asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 759);
                                    }
                                }
                                bool sensor123usKoptu = ytkNodeRedModel.Hat_1_Konv_2_Koptu_KopmaNoktasi_123US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 760))
                                {
                                    if (sensor123usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 760, AlarmEnum = 760, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 760)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor123usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 760);
                                    }
                                }
                                bool sensor124asKoptu = ytkNodeRedModel.Hat_1_Konv_2_Koptu_KopmaNoktasi_124AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 761))
                                {
                                    if (sensor124asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 761, AlarmEnum = 761, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 761)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor124asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 761);
                                    }
                                }
                                bool sensor124usKoptu = ytkNodeRedModel.Hat_1_Konv_2_Koptu_KopmaNoktasi_124US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 762))
                                {
                                    if (sensor124usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 762, AlarmEnum = 762, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 762)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor124usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 762);
                                    }
                                }
                                bool sensor125asKoptu = ytkNodeRedModel.Hat_1_Konv_2_Koptu_KopmaNoktasi_125AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 763))
                                {
                                    if (sensor125asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 763, AlarmEnum = 763, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 763)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor125asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 763);
                                    }
                                }
                                bool sensor125usKoptu = ytkNodeRedModel.Hat_1_Konv_2_Koptu_KopmaNoktasi_125US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 764))
                                {
                                    if (sensor125usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 764, AlarmEnum = 764, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 764)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor125usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 764);
                                    }
                                }
                                bool sensor126asKoptu = ytkNodeRedModel.Hat_1_Konv_2_Koptu_KopmaNoktasi_126AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 765))
                                {
                                    if (sensor126asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 765, AlarmEnum = 765, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 765)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor126asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 765);
                                    }
                                }
                                bool sensor126usKoptu = ytkNodeRedModel.Hat_1_Konv_2_Koptu_KopmaNoktasi_126US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 766))
                                {
                                    if (sensor126usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 766, AlarmEnum = 766, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 766)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor126usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 766);
                                    }
                                }
                                /////////////////////
                                bool sensor211asKoptu = ytkNodeRedModel.Hat_2_Konv_1_Koptu_KopmaNoktasi_211AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 767))
                                {
                                    if (sensor211asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 767, AlarmEnum = 767, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 767)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor211asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 767);
                                    }
                                }
                                bool sensor211usKoptu = ytkNodeRedModel.Hat_2_Konv_1_Koptu_KopmaNoktasi_211US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 768))
                                {
                                    if (sensor211usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 768, AlarmEnum = 768, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 768)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor211usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 768);
                                    }
                                }
                                bool sensor212asKoptu = ytkNodeRedModel.Hat_2_Konv_1_Koptu_KopmaNoktasi_212AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 769))
                                {
                                    if (sensor212asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 769, AlarmEnum = 769, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 769)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor212asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 769);
                                    }
                                }
                                bool sensor212usKoptu = ytkNodeRedModel.Hat_2_Konv_1_Koptu_KopmaNoktasi_212US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 770))
                                {
                                    if (sensor212usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 770, AlarmEnum = 770, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 770)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor212usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 770);
                                    }
                                }
                                bool sensor213asKoptu = ytkNodeRedModel.Hat_2_Konv_1_Koptu_KopmaNoktasi_213AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 771))
                                {
                                    if (sensor213asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 771, AlarmEnum = 771, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 771)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor213asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 771);
                                    }
                                }
                                bool sensor213usKoptu = ytkNodeRedModel.Hat_2_Konv_1_Koptu_KopmaNoktasi_213US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 772))
                                {
                                    if (sensor213usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 772, AlarmEnum = 772, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 772)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor213usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 772);
                                    }
                                }
                                bool sensor214asKoptu = ytkNodeRedModel.Hat_2_Konv_1_Koptu_KopmaNoktasi_214AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 773))
                                {
                                    if (sensor214asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 773, AlarmEnum = 773, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 773)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor214asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 773);
                                    }
                                }
                                bool sensor214usKoptu = ytkNodeRedModel.Hat_2_Konv_1_Koptu_KopmaNoktasi_214US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 774))
                                {
                                    if (sensor214usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 774, AlarmEnum = 774, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 774)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor214usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 774);
                                    }
                                }
                                bool sensor215asKoptu = ytkNodeRedModel.Hat_2_Konv_1_Koptu_KopmaNoktasi_215AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 775))
                                {
                                    if (sensor215asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 775, AlarmEnum = 775, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 775)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor215asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 775);
                                    }
                                }
                                bool sensor215usKoptu = ytkNodeRedModel.Hat_2_Konv_1_Koptu_KopmaNoktasi_215US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 776))
                                {
                                    if (sensor215usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 776, AlarmEnum = 776, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 776)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor215usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 776);
                                    }
                                }
                                bool sensor216asKoptu = ytkNodeRedModel.Hat_2_Konv_1_Koptu_KopmaNoktasi_216AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 777))
                                {
                                    if (sensor216asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 777, AlarmEnum = 777, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 777)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor216asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 777);
                                    }
                                }
                                bool sensor216usKoptu = ytkNodeRedModel.Hat_2_Konv_1_Koptu_KopmaNoktasi_216US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 778))
                                {
                                    if (sensor216usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 778, AlarmEnum = 778, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 778)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor216usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 778);
                                    }
                                }
                                /////////////////////
                                bool sensor220asKoptu = ytkNodeRedModel.Hat_2_Konv_2_Koptu_KopmaNoktasi_220AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 779))
                                {
                                    if (sensor220asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 779, AlarmEnum = 779, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 779)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor220asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 779);
                                    }
                                }
                                bool sensor220usKoptu = ytkNodeRedModel.Hat_2_Konv_2_Koptu_KopmaNoktasi_220US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 780))
                                {
                                    if (sensor220usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 780, AlarmEnum = 780, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 780)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor220usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 780);
                                    }
                                }
                                bool sensor221asKoptu = ytkNodeRedModel.Hat_2_Konv_2_Koptu_KopmaNoktasi_221AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 781))
                                {
                                    if (sensor221asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 781, AlarmEnum = 781, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 781)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor221asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 781);
                                    }
                                }
                                bool sensor221usKoptu = ytkNodeRedModel.Hat_2_Konv_2_Koptu_KopmaNoktasi_221US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 782))
                                {
                                    if (sensor221usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 782, AlarmEnum = 782, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 782)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor221usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 782);
                                    }
                                }
                                bool sensor222asKoptu = ytkNodeRedModel.Hat_2_Konv_2_Koptu_KopmaNoktasi_222AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 783))
                                {
                                    if (sensor222asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 783, AlarmEnum = 783, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 783)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor222asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 783);
                                    }
                                }
                                bool sensor222usKoptu = ytkNodeRedModel.Hat_2_Konv_2_Koptu_KopmaNoktasi_222US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 784))
                                {
                                    if (sensor222usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 784, AlarmEnum = 784, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 784)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor222usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 784);
                                    }
                                }
                                bool sensor223asKoptu = ytkNodeRedModel.Hat_2_Konv_2_Koptu_KopmaNoktasi_223AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 785))
                                {
                                    if (sensor223asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 785, AlarmEnum = 785, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 785)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor223asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 785);
                                    }
                                }
                                bool sensor223usKoptu = ytkNodeRedModel.Hat_2_Konv_2_Koptu_KopmaNoktasi_223US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 786))
                                {
                                    if (sensor223usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 786, AlarmEnum = 786, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 786)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor223usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 786);
                                    }
                                }
                                bool sensor224asKoptu = ytkNodeRedModel.Hat_2_Konv_2_Koptu_KopmaNoktasi_224AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 787))
                                {
                                    if (sensor224asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 787, AlarmEnum = 787, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 787)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor224asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 787);
                                    }
                                }
                                bool sensor224usKoptu = ytkNodeRedModel.Hat_2_Konv_2_Koptu_KopmaNoktasi_224US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 788))
                                {
                                    if (sensor224usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 788, AlarmEnum = 788, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 788)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor224usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 788);
                                    }
                                }
                                bool sensor225asKoptu = ytkNodeRedModel.Hat_2_Konv_2_Koptu_KopmaNoktasi_225AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 789))
                                {
                                    if (sensor225asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 789, AlarmEnum = 789, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 789)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor225asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 789);
                                    }
                                }
                                bool sensor225usKoptu = ytkNodeRedModel.Hat_2_Konv_2_Koptu_KopmaNoktasi_225US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 790))
                                {
                                    if (sensor225usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 790, AlarmEnum = 790, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 790)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor225usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 790);
                                    }
                                }
                                bool sensor226asKoptu = ytkNodeRedModel.Hat_2_Konv_2_Koptu_KopmaNoktasi_226AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 791))
                                {
                                    if (sensor226asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 791, AlarmEnum = 791, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 791)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor226asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 791);
                                    }
                                }
                                bool sensor226usKoptu = ytkNodeRedModel.Hat_2_Konv_2_Koptu_KopmaNoktasi_226US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 792))
                                {
                                    if (sensor226usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 792, AlarmEnum = 792, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 792)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor226usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 792);
                                    }
                                }
                            }
                        }
                    }
                    return alarmLogs;
                }
            }
            catch (Exception)
            {
                return new List<AlarmLog>();
            }
        }
        private async Task<IEnumerable<AlarmLog>> GetAlarmLogDataVerimGidaHat34ForBulkInsert(int lokasyonId, AlarmCacheService _alarmCacheService, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;
                List<AlarmLog> alarmLogs = new List<AlarmLog>();
                YTKNodeRed? model;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    model = await kumesScadaDbContext.YTKNodeRed.AsNoTracking().AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.PlcAdi == "HAT34" && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                    if (model != null)
                    {
                        if (model.Durum == 1)
                        {
                            if (model.Veriler != null)
                            {
                                VerimGidaHat34NodeRedModel ytkNodeRedModel = JsonSerializer.Deserialize<VerimGidaHat34NodeRedModel>(model.Veriler);

                                var activeAlarms = await _alarmCacheService.GetAlarmLogsAsync(lokasyonId);

                                bool hat3konveyor1TermikNok = ytkNodeRedModel.Hat_3_Konv_1_TermikNok.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 841))
                                {
                                    if (hat3konveyor1TermikNok)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 841, AlarmEnum = 841, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 841)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!hat3konveyor1TermikNok)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 841);
                                    }
                                }
                                bool hat3konveyor2TermikNok = ytkNodeRedModel.Hat_3_Konv_2_TermikNok.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 842))
                                {
                                    if (hat3konveyor2TermikNok)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 842, AlarmEnum = 842, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 842)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!hat3konveyor2TermikNok)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 842);
                                    }
                                }
                                bool hat4konveyor1TermikNok = ytkNodeRedModel.Hat_4_Konv_1_TermikNok.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 843))
                                {
                                    if (hat4konveyor1TermikNok)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 843, AlarmEnum = 843, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 843)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!hat4konveyor1TermikNok)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 843);
                                    }
                                }
                                bool hat4konveyor2TermikNok = ytkNodeRedModel.Hat_4_Konv_2_TemikNok.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 844))
                                {
                                    if (hat4konveyor2TermikNok)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 844, AlarmEnum = 844, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 844)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!hat4konveyor2TermikNok)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 844);
                                    }
                                }
                                //////////////////////////////////////////////////////////////////////
                                bool sensor311asKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_311AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 793))
                                {
                                    if (sensor311asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 793, AlarmEnum = 793, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 793)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor311asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 793);
                                    }
                                }
                                bool sensor311usKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_311US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 794))
                                {
                                    if (sensor311usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 794, AlarmEnum = 794, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 794)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor311usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 794);
                                    }
                                }
                                bool sensor312asKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_312AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 795))
                                {
                                    if (sensor312asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 795, AlarmEnum = 795, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 795)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor312asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 795);
                                    }
                                }
                                bool sensor312usKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_312US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 796))
                                {
                                    if (sensor312usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 796, AlarmEnum = 796, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 796)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor312usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 796);
                                    }
                                }
                                bool sensor313asKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_313AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 797))
                                {
                                    if (sensor313asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 797, AlarmEnum = 797, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 797)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor313asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 797);
                                    }
                                }
                                bool sensor313usKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_313US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 798))
                                {
                                    if (sensor313usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 798, AlarmEnum = 798, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 798)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor313usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 798);
                                    }
                                }
                                bool sensor314asKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_314AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 799))
                                {
                                    if (sensor314asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 799, AlarmEnum = 799, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 799)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor314asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 799);
                                    }
                                }
                                bool sensor314usKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_314US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 800))
                                {
                                    if (sensor314usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 800, AlarmEnum = 800, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 800)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor314usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 800);
                                    }
                                }
                                bool sensor315asKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_315AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 801))
                                {
                                    if (sensor315asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 801, AlarmEnum = 801, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 801)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor315asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 801);
                                    }
                                }
                                bool sensor315usKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_315US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 802))
                                {
                                    if (sensor315usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 802, AlarmEnum = 802, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 802)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor315usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 802);
                                    }
                                }
                                bool sensor316asKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_316AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 803))
                                {
                                    if (sensor316asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 803, AlarmEnum = 803, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 803)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor316asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 803);
                                    }
                                }
                                bool sensor316usKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_316US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 804))
                                {
                                    if (sensor316usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 804, AlarmEnum = 804, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 804)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor316usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 804);
                                    }
                                }
                                bool sensor317asKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_317AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 805))
                                {
                                    if (sensor317asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 805, AlarmEnum = 805, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 805)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor317asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 805);
                                    }
                                }
                                bool sensor317usKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_317US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 806))
                                {
                                    if (sensor317usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 806, AlarmEnum = 806, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 806)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor317usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 806);
                                    }
                                }
                                bool sensor318asKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_318AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 807))
                                {
                                    if (sensor318asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 807, AlarmEnum = 807, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 807)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor318asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 807);
                                    }
                                }
                                bool sensor318usKoptu = ytkNodeRedModel.Hat_3_Konv_1_Koptu_KopmaNoktasi_318US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 808))
                                {
                                    if (sensor318usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 808, AlarmEnum = 808, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 808)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor318usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 808);
                                    }
                                }
                                ///////////////////////////////////////
                                bool sensor321asKoptu = ytkNodeRedModel.Hat_3_Konv_2_Koptu_KopmaNoktasi_321AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 809))
                                {
                                    if (sensor321asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 809, AlarmEnum = 809, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 809)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor321asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 809);
                                    }
                                }
                                bool sensor321usKoptu = ytkNodeRedModel.Hat_3_Konv_2_Koptu_KopmaNoktasi_321US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 810))
                                {
                                    if (sensor321usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 810, AlarmEnum = 810, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 810)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor321usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 810);
                                    }
                                }
                                bool sensor322asKoptu = ytkNodeRedModel.Hat_3_Konv_2_Koptu_KopmaNoktasi_322AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 811))
                                {
                                    if (sensor322asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 811, AlarmEnum = 811, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 811)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor322asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 811);
                                    }
                                }
                                bool sensor322usKoptu = ytkNodeRedModel.Hat_3_Konv_2_Koptu_KopmaNoktasi_322US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 812))
                                {
                                    if (sensor322usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 812, AlarmEnum = 812, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 812)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor322usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 812);
                                    }
                                }
                                bool sensor323asKoptu = ytkNodeRedModel.Hat_3_Konv_2_Koptu_KopmaNoktasi_323AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 813))
                                {
                                    if (sensor323asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 813, AlarmEnum = 813, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 813)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor323asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 813);
                                    }
                                }
                                bool sensor323usKoptu = ytkNodeRedModel.Hat_3_Konv_2_Koptu_KopmaNoktasi_323US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 814))
                                {
                                    if (sensor323usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 814, AlarmEnum = 814, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 814)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor323usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 814);
                                    }
                                }
                                ////////////////////////////////////////
                                bool sensor411asKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_411AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 815))
                                {
                                    if (sensor411asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 815, AlarmEnum = 815, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 815)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor411asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 815);
                                    }
                                }
                                bool sensor411usKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_411US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 816))
                                {
                                    if (sensor411usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 816, AlarmEnum = 816, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 816)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor411usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 816);
                                    }
                                }
                                bool sensor412asKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_412AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 817))
                                {
                                    if (sensor412asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 817, AlarmEnum = 817, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 817)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor412asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 817);
                                    }
                                }
                                bool sensor412usKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_412US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 818))
                                {
                                    if (sensor412usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 818, AlarmEnum = 818, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 818)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor412usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 818);
                                    }
                                }
                                bool sensor413asKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_413AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 819))
                                {
                                    if (sensor413asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 819, AlarmEnum = 819, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 819)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor413asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 819);
                                    }
                                }
                                bool sensor413usKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_413US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 820))
                                {
                                    if (sensor413usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 820, AlarmEnum = 820, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 820)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor413usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 820);
                                    }
                                }
                                bool sensor414asKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_414AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 821))
                                {
                                    if (sensor414asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 821, AlarmEnum = 821, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 821)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor414asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 821);
                                    }
                                }
                                bool sensor414usKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_414US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 822))
                                {
                                    if (sensor414usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 822, AlarmEnum = 822, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 822)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor414usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 822);
                                    }
                                }
                                bool sensor415asKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_415AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 823))
                                {
                                    if (sensor415asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 823, AlarmEnum = 823, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 823)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor415asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 823);
                                    }
                                }
                                bool sensor415usKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_415US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 824))
                                {
                                    if (sensor415usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 824, AlarmEnum = 824, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 824)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor415usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 824);
                                    }
                                }
                                bool sensor416asKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_416AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 825))
                                {
                                    if (sensor416asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 825, AlarmEnum = 825, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 825)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor416asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 825);
                                    }
                                }
                                bool sensor416usKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_416US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 826))
                                {
                                    if (sensor416usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 826, AlarmEnum = 826, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 826)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor416usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 826);
                                    }
                                }
                                bool sensor417asKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_417AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 827))
                                {
                                    if (sensor417asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 827, AlarmEnum = 827, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 827)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor417asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 827);
                                    }
                                }
                                bool sensor417usKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_417US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 828))
                                {
                                    if (sensor417usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 828, AlarmEnum = 828, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 828)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor417usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 828);
                                    }
                                }
                                bool sensor418asKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_418AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 829))
                                {
                                    if (sensor418asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 829, AlarmEnum = 829, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 829)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor418asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 829);
                                    }
                                }
                                bool sensor418usKoptu = ytkNodeRedModel.Hat_4_Konv_1_Koptu_KopmaNoktasi_418US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 830))
                                {
                                    if (sensor418usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 830, AlarmEnum = 830, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 830)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor418usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 830);
                                    }
                                }
                                /////////////////////////////////////////////////
                                bool sensor421asKoptu = ytkNodeRedModel.Hat_4_Konv_2_Koptu_KopmaNoktasi_421AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 831))
                                {
                                    if (sensor421asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 831, AlarmEnum = 831, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 831)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor421asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 831);
                                    }
                                }
                                bool sensor421usKoptu = ytkNodeRedModel.Hat_4_Konv_2_Koptu_KopmaNoktasi_421US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 832))
                                {
                                    if (sensor421usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 832, AlarmEnum = 832, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 832)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor421usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 832);
                                    }
                                }
                                bool sensor422asKoptu = ytkNodeRedModel.Hat_4_Konv_2_Koptu_KopmaNoktasi_422AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 833))
                                {
                                    if (sensor422asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 833, AlarmEnum = 833, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 833)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor422asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 833);
                                    }
                                }
                                bool sensor422usKoptu = ytkNodeRedModel.Hat_4_Konv_2_Koptu_KopmaNoktasi_422US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 834))
                                {
                                    if (sensor422usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 834, AlarmEnum = 834, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 834)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor422usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 834);
                                    }
                                }
                                bool sensor423asKoptu = ytkNodeRedModel.Hat_4_Konv_2_Koptu_KopmaNoktasi_423AS.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 835))
                                {
                                    if (sensor423asKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 835, AlarmEnum = 835, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 835)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor423asKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 835);
                                    }
                                }
                                bool sensor423usKoptu = ytkNodeRedModel.Hat_4_Konv_2_Koptu_KopmaNoktasi_423US.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 836))
                                {
                                    if (sensor423usKoptu)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 836, AlarmEnum = 836, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 836)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(lokasyonId);
                                    }
                                }
                                else
                                {
                                    if (!sensor423usKoptu)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(lokasyonId, 836);
                                    }
                                }
                            }
                        }
                    }
                    return alarmLogs;
                }
            }
            catch (Exception)
            {
                return new List<AlarmLog>();
            }
        }
        public async Task BulkInsertAlarmLogDataAsync(SqlConnection connection, IEnumerable<AlarmLog> data)
        {
            try
            {
                var dataTable = new DataTable();
                var accessor = TypeAccessor.Create(typeof(AlarmLog)); // FastMember ile eriþim

                // DataTable'a sütunlarý ekleyelim
                var members = accessor.GetMembers();
                foreach (var member in members)
                {
                    dataTable.Columns.Add(member.Name);
                }

                // Verileri DataTable'a ekleyelim
                foreach (var item in data)
                {
                    var row = dataTable.NewRow();
                    foreach (var member in members)
                    {
                        row[member.Name] = accessor[item, member.Name];
                    }
                    dataTable.Rows.Add(row);
                }

                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                // SqlBulkCopy ile veriyi MSSQL'e ekleyelim
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "AlarmLog"; // Verinin ekleneceði tablo adý
                    // Dinamik kolon eþleþtirmesi
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }
                    await bulkCopy.WriteToServerAsync(dataTable); // Veriyi yazma
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                {
                    await connection.CloseAsync();
                }
            }
        }
        public async Task BulkInsertEtiketLogDataAsync(SqlConnection connection, IEnumerable<EtiketLogKonveyor> data)
        {
            try
            {
                var dataTable = new DataTable();
                var accessor = TypeAccessor.Create(typeof(EtiketLogKonveyor)); // FastMember ile eriþim

                // DataTable'a sütunlarý ekleyelim
                var members = accessor.GetMembers();
                foreach (var member in members)
                {
                    dataTable.Columns.Add(member.Name);
                }

                // Verileri DataTable'a ekleyelim
                foreach (var item in data)
                {
                    var row = dataTable.NewRow();
                    foreach (var member in members)
                    {
                        row[member.Name] = accessor[item, member.Name];
                    }
                    dataTable.Rows.Add(row);
                }

                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                // SqlBulkCopy ile veriyi MSSQL'e ekleyelim
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "EtiketLogKonveyor"; // Verinin ekleneceði tablo adý
                    bulkCopy.BulkCopyTimeout = 600;
                    // Dinamik kolon eþleþtirmesi
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }
                    await bulkCopy.WriteToServerAsync(dataTable); // Veriyi yazma
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                {
                    await connection.CloseAsync();
                }
            }
        }
        public void PlcLogYaz(string alarmMesaj)
        {
            try
            {
                if (_sqlConnection.State != ConnectionState.Open)
                {
                    _sqlConnection.Open();
                }
                if (_sqlConnection.State == ConnectionState.Open)
                {
                    const string queryString = "INSERT INTO Logs (KayitZamani, Mesaj) " +
                                               "VALUES (@KayitZamani, @Mesaj);";
                    SqlCommand command = new(queryString, _sqlConnection);

                    command.Parameters.AddWithValue("@KayitZamani", System.DateTime.Now);
                    command.Parameters.AddWithValue("@Mesaj", alarmMesaj);

                    command.ExecuteNonQuery();
                }

                if (_sqlConnection.State != ConnectionState.Closed)
                {
                    _sqlConnection.Close();
                }
            }
            catch (Exception)
            {
            }
        }
        private async Task<IEnumerable<EtiketLogKonveyor>> GetEtiketLogDataVerimGidaHat12ForBulkInsert(int lokasyonId, AlarmCacheService _alarmCacheService, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;
                List<EtiketLogKonveyor> etiketLogs = new List<EtiketLogKonveyor>();
                YTKNodeRed? model;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    model = await kumesScadaDbContext.YTKNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.PlcAdi == "HAT12" && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                    if (model == null)
                    {
                        var plcConnected = 0;
                        etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 567, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                        return etiketLogs;
                    }
                    if (model != null)
                    {
                        var plcConnected = model.Durum;
                        var activeAlarms = await _alarmCacheService.GetAlarmLogsAsync(lokasyonId);
                        List<AlarmLog> alarmLogs = new List<AlarmLog>();
                        if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 845))
                        {
                            if (plcConnected == 0)
                            {
                                PlcLogYaz("Verim Gýda Hat 1-2 plc baðlantýsý koptu");
                                alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 845, AlarmEnum = 845, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 845)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                _alarmCacheService.ClearMemoryCache(lokasyonId);
                            }
                        }
                        else
                        {
                            if (plcConnected == 1)
                            {
                                PlcLogYaz("Verim Gýda Hat 1-2 plc numaralý kümeste plc baðlantýsý saðlandý");
                                await _alarmCacheService.SetAlarmPassive(lokasyonId, 845);
                            }
                        }
                        await BulkInsertAlarmLogDataAsync(_sqlConnection, alarmLogs);
                        if (plcConnected == 0) return etiketLogs;

                        if (model.Durum == 1)
                        {
                            if (model.Veriler != null)
                            {
                                VerimGidaHat12NodeRedModel ytkNodeRedModel = JsonSerializer.Deserialize<VerimGidaHat12NodeRedModel>(model.Veriler);
                                var b111 = -1;
                                var b112 = -1;
                                var b113 = -1;
                                var b114 = -1;
                                var b115 = -1;
                                var b116 = -1;
                                if (ytkNodeRedModel.Hat_1_Konv_1_Calisiyor.GetValueOrDefault(false) == true)
                                {
                                    b111 = KonveyorLogDurum.Calisiyor;
                                    b112 = KonveyorLogDurum.Calisiyor;
                                    b113 = KonveyorLogDurum.Calisiyor;
                                    b114 = KonveyorLogDurum.Calisiyor;
                                    b115 = KonveyorLogDurum.Calisiyor;
                                    b116 = KonveyorLogDurum.Calisiyor;
                                }
                                else
                                {
                                    if (!ytkNodeRedModel.Hat_1_Konv_1_Koptu.GetValueOrDefault(false))
                                    {
                                        b111 = KonveyorLogDurum.Bosta;
                                        b112 = KonveyorLogDurum.Bosta;
                                        b113 = KonveyorLogDurum.Bosta;
                                        b114 = KonveyorLogDurum.Bosta;
                                        b115 = KonveyorLogDurum.Bosta;
                                        b116 = KonveyorLogDurum.Bosta;
                                    }
                                    else
                                    {
                                        b111 = ytkNodeRedModel.Hat_1_Konv_1_Koptu_Konv1_Blm1.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b112 = ytkNodeRedModel.Hat_1_Konv_1_Koptu_Konv1_Blm2.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b113 = ytkNodeRedModel.Hat_1_Konv_1_Koptu_Konv1_Blm3.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b114 = ytkNodeRedModel.Hat_1_Konv_1_Koptu_Konv1_Blm4.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b115 = ytkNodeRedModel.Hat_1_Konv_1_Koptu_Konv1_Blm5.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b116 = ytkNodeRedModel.Hat_1_Konv_1_Koptu_Konv1_Blm6.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                    }
                                }
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 519, KayitZamani = DateTime.Now, Deger = (decimal?)b111 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 520, KayitZamani = DateTime.Now, Deger = (decimal?)b112 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 521, KayitZamani = DateTime.Now, Deger = (decimal?)b113 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 522, KayitZamani = DateTime.Now, Deger = (decimal?)b114 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 523, KayitZamani = DateTime.Now, Deger = (decimal?)b115 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 524, KayitZamani = DateTime.Now, Deger = (decimal?)b116 });

                                var b120 = -1;
                                var b121 = -1;
                                var b122 = -1;
                                var b123 = -1;
                                var b124 = -1;
                                var b125 = -1;
                                var b126 = -1;
                                if (ytkNodeRedModel.Hat_1_Konv_2_Calisiyor.GetValueOrDefault(false) == true)
                                {
                                    b120 = KonveyorLogDurum.Calisiyor;
                                    b121 = KonveyorLogDurum.Calisiyor;
                                    b122 = KonveyorLogDurum.Calisiyor;
                                    b123 = KonveyorLogDurum.Calisiyor;
                                    b124 = KonveyorLogDurum.Calisiyor;
                                    b125 = KonveyorLogDurum.Calisiyor;
                                    b126 = KonveyorLogDurum.Calisiyor;
                                }
                                else
                                {
                                    if (!ytkNodeRedModel.Hat_1_Konv_2_Koptu.GetValueOrDefault(false))
                                    {
                                        b120 = KonveyorLogDurum.Bosta;
                                        b121 = KonveyorLogDurum.Bosta;
                                        b122 = KonveyorLogDurum.Bosta;
                                        b123 = KonveyorLogDurum.Bosta;
                                        b124 = KonveyorLogDurum.Bosta;
                                        b125 = KonveyorLogDurum.Bosta;
                                        b126 = KonveyorLogDurum.Bosta;
                                    }
                                    else
                                    {
                                        b120 = ytkNodeRedModel.Hat_1_Konv_2_Koptu_Konv1_Blm0.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b121 = ytkNodeRedModel.Hat_1_Konv_2_Koptu_Konv1_Blm1.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b122 = ytkNodeRedModel.Hat_1_Konv_2_Koptu_Konv1_Blm2.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b123 = ytkNodeRedModel.Hat_1_Konv_2_Koptu_Konv1_Blm3.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b124 = ytkNodeRedModel.Hat_1_Konv_2_Koptu_Konv1_Blm4.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b125 = ytkNodeRedModel.Hat_1_Konv_2_Koptu_Konv1_Blm5.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b126 = ytkNodeRedModel.Hat_1_Konv_2_Koptu_Konv1_Blm6.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                    }
                                }
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 525, KayitZamani = DateTime.Now, Deger = (decimal?)b120 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 526, KayitZamani = DateTime.Now, Deger = (decimal?)b121 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 527, KayitZamani = DateTime.Now, Deger = (decimal?)b122 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 528, KayitZamani = DateTime.Now, Deger = (decimal?)b123 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 529, KayitZamani = DateTime.Now, Deger = (decimal?)b124 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 530, KayitZamani = DateTime.Now, Deger = (decimal?)b125 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 531, KayitZamani = DateTime.Now, Deger = (decimal?)b126 });

                                var b211 = -1;
                                var b212 = -1;
                                var b213 = -1;
                                var b214 = -1;
                                var b215 = -1;
                                var b216 = -1;
                                if (ytkNodeRedModel.Hat_2_Konv_1_Calisiyor.GetValueOrDefault(false) == true)
                                {
                                    b211 = KonveyorLogDurum.Calisiyor;
                                    b212 = KonveyorLogDurum.Calisiyor;
                                    b213 = KonveyorLogDurum.Calisiyor;
                                    b214 = KonveyorLogDurum.Calisiyor;
                                    b215 = KonveyorLogDurum.Calisiyor;
                                    b216 = KonveyorLogDurum.Calisiyor;
                                }
                                else
                                {
                                    if (!ytkNodeRedModel.Hat_1_Konv_1_Koptu.GetValueOrDefault(false))
                                    {
                                        b211 = KonveyorLogDurum.Bosta;
                                        b212 = KonveyorLogDurum.Bosta;
                                        b213 = KonveyorLogDurum.Bosta;
                                        b214 = KonveyorLogDurum.Bosta;
                                        b215 = KonveyorLogDurum.Bosta;
                                        b216 = KonveyorLogDurum.Bosta;
                                    }
                                    else
                                    {
                                        b211 = ytkNodeRedModel.Hat_2_Konv_1_Koptu_Konv1_Blm1.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b212 = ytkNodeRedModel.Hat_2_Konv_1_Koptu_Konv1_Blm2.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b213 = ytkNodeRedModel.Hat_2_Konv_1_Koptu_Konv1_Blm3.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b214 = ytkNodeRedModel.Hat_2_Konv_1_Koptu_Konv1_Blm4.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b215 = ytkNodeRedModel.Hat_2_Konv_1_Koptu_Konv1_Blm5.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b216 = ytkNodeRedModel.Hat_2_Konv_1_Koptu_Konv1_Blm6.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                    }
                                }
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 532, KayitZamani = DateTime.Now, Deger = (decimal?)b211 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 533, KayitZamani = DateTime.Now, Deger = (decimal?)b212 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 534, KayitZamani = DateTime.Now, Deger = (decimal?)b213 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 535, KayitZamani = DateTime.Now, Deger = (decimal?)b214 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 536, KayitZamani = DateTime.Now, Deger = (decimal?)b215 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 537, KayitZamani = DateTime.Now, Deger = (decimal?)b216 });

                                var b220 = -1;
                                var b221 = -1;
                                var b222 = -1;
                                var b223 = -1;
                                var b224 = -1;
                                var b225 = -1;
                                var b226 = -1;
                                if (ytkNodeRedModel.Hat_2_Konv_2_Calisiyor.GetValueOrDefault(false) == true)
                                {
                                    b220 = KonveyorLogDurum.Calisiyor;
                                    b221 = KonveyorLogDurum.Calisiyor;
                                    b222 = KonveyorLogDurum.Calisiyor;
                                    b223 = KonveyorLogDurum.Calisiyor;
                                    b224 = KonveyorLogDurum.Calisiyor;
                                    b225 = KonveyorLogDurum.Calisiyor;
                                    b226 = KonveyorLogDurum.Calisiyor;
                                }
                                else
                                {
                                    if (!ytkNodeRedModel.Hat_2_Konv_2_Koptu.GetValueOrDefault(false))
                                    {
                                        b220 = KonveyorLogDurum.Bosta;
                                        b221 = KonveyorLogDurum.Bosta;
                                        b222 = KonveyorLogDurum.Bosta;
                                        b223 = KonveyorLogDurum.Bosta;
                                        b224 = KonveyorLogDurum.Bosta;
                                        b225 = KonveyorLogDurum.Bosta;
                                        b226 = KonveyorLogDurum.Bosta;
                                    }
                                    else
                                    {
                                        b220 = ytkNodeRedModel.Hat_2_Konv_2_Koptu_Konv1_Blm0.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b221 = ytkNodeRedModel.Hat_2_Konv_2_Koptu_Konv1_Blm1.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b222 = ytkNodeRedModel.Hat_2_Konv_2_Koptu_Konv1_Blm2.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b223 = ytkNodeRedModel.Hat_2_Konv_2_Koptu_Konv1_Blm3.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b224 = ytkNodeRedModel.Hat_2_Konv_2_Koptu_Konv1_Blm4.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b225 = ytkNodeRedModel.Hat_2_Konv_2_Koptu_Konv1_Blm5.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b226 = ytkNodeRedModel.Hat_2_Konv_2_Koptu_Konv1_Blm6.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                    }
                                }
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 538, KayitZamani = DateTime.Now, Deger = (decimal?)b220 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 539, KayitZamani = DateTime.Now, Deger = (decimal?)b221 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 540, KayitZamani = DateTime.Now, Deger = (decimal?)b222 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 541, KayitZamani = DateTime.Now, Deger = (decimal?)b223 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 542, KayitZamani = DateTime.Now, Deger = (decimal?)b224 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 543, KayitZamani = DateTime.Now, Deger = (decimal?)b225 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 544, KayitZamani = DateTime.Now, Deger = (decimal?)b226 });
                            }
                        }
                    }
                    return etiketLogs;
                }
            }
            catch (Exception)
            {
                return new List<EtiketLogKonveyor>();
            }
        }
        private async Task<IEnumerable<EtiketLogKonveyor>> GetEtiketLogDataVerimGidaHat34ForBulkInsert(int lokasyonId, AlarmCacheService _alarmCacheService, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;
                List<EtiketLogKonveyor> etiketLogs = new List<EtiketLogKonveyor>();
                YTKNodeRed? model;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    model = await kumesScadaDbContext.YTKNodeRed.AsNoTracking().AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.PlcAdi == "HAT34" && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                    if (model == null)
                    {
                        var plcConnected = 0;
                        etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 568, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                        return etiketLogs;
                    }
                    if (model != null)
                    {
                        var plcRead = model.Durum;
                        var plcConnected = model.Durum;
                        var activeAlarms = await _alarmCacheService.GetAlarmLogsAsync(lokasyonId);
                        List<AlarmLog> alarmLogs = new List<AlarmLog>();
                        if (!activeAlarms.Any(x => x.KonveyorId == lokasyonId && x.AlarmId == 846))
                        {
                            if (plcConnected == 0)
                            {
                                PlcLogYaz("Verim Gýda Hat 3-4 plc baðlantýsý koptu");
                                alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KonveyorId = lokasyonId, AlarmId = 846, AlarmEnum = 846, Mesaj = alarmlar.FirstOrDefault(x => x.Id == 846)?.AlarmMetni, KumesId = null, Aktif = true, Silindi = false });
                                _alarmCacheService.ClearMemoryCache(lokasyonId);
                            }
                        }
                        else
                        {
                            if (plcConnected == 1)
                            {
                                PlcLogYaz("Verim Gýda Hat 3-4 plc numaralý kümeste plc baðlantýsý saðlandý");
                                await _alarmCacheService.SetAlarmPassive(lokasyonId, 846);
                            }
                        }
                        await BulkInsertAlarmLogDataAsync(_sqlConnection, alarmLogs);
                        if (plcConnected == 0) return etiketLogs;

                        if (model.Durum == 1)
                        {
                            if (model.Veriler != null)
                            {
                                VerimGidaHat34NodeRedModel ytkNodeRedModel = JsonSerializer.Deserialize<VerimGidaHat34NodeRedModel>(model.Veriler);
                                var b311 = -1;
                                var b312 = -1;
                                var b313 = -1;
                                var b314 = -1;
                                var b315 = -1;
                                var b316 = -1;
                                var b317 = -1;
                                var b318 = -1;
                                if (ytkNodeRedModel.Hat_3_Konv_1_Calisiyor.GetValueOrDefault(false) == true)
                                {
                                    b311 = KonveyorLogDurum.Calisiyor;
                                    b312 = KonveyorLogDurum.Calisiyor;
                                    b313 = KonveyorLogDurum.Calisiyor;
                                    b314 = KonveyorLogDurum.Calisiyor;
                                    b315 = KonveyorLogDurum.Calisiyor;
                                    b316 = KonveyorLogDurum.Calisiyor;
                                    b317 = KonveyorLogDurum.Calisiyor;
                                    b318 = KonveyorLogDurum.Calisiyor;
                                }
                                else
                                {
                                    if (!ytkNodeRedModel.Hat_3_Konv_1_Koptu.GetValueOrDefault(false))
                                    {
                                        b311 = KonveyorLogDurum.Bosta;
                                        b312 = KonveyorLogDurum.Bosta;
                                        b313 = KonveyorLogDurum.Bosta;
                                        b314 = KonveyorLogDurum.Bosta;
                                        b315 = KonveyorLogDurum.Bosta;
                                        b316 = KonveyorLogDurum.Bosta;
                                        b317 = KonveyorLogDurum.Bosta;
                                        b318 = KonveyorLogDurum.Bosta;
                                    }
                                    else
                                    {
                                        b311 = ytkNodeRedModel.Hat_3_Konv_1_Koptu_Konv1_Blm1.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b312 = ytkNodeRedModel.Hat_3_Konv_1_Koptu_Konv1_Blm2.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b313 = ytkNodeRedModel.Hat_3_Konv_1_Koptu_Konv1_Blm3.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b314 = ytkNodeRedModel.Hat_3_Konv_1_Koptu_Konv1_Blm4.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b315 = ytkNodeRedModel.Hat_3_Konv_1_Koptu_Konv1_Blm5.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b316 = ytkNodeRedModel.Hat_3_Konv_1_Koptu_Konv1_Blm6.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b317 = ytkNodeRedModel.Hat_3_Konv_1_Koptu_Konv1_Blm7.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b318 = ytkNodeRedModel.Hat_3_Konv_1_Koptu_Konv1_Blm8.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                    }
                                }
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 545, KayitZamani = DateTime.Now, Deger = (decimal?)b311 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 546, KayitZamani = DateTime.Now, Deger = (decimal?)b312 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 547, KayitZamani = DateTime.Now, Deger = (decimal?)b313 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 548, KayitZamani = DateTime.Now, Deger = (decimal?)b314 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 549, KayitZamani = DateTime.Now, Deger = (decimal?)b315 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 550, KayitZamani = DateTime.Now, Deger = (decimal?)b316 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 551, KayitZamani = DateTime.Now, Deger = (decimal?)b317 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 552, KayitZamani = DateTime.Now, Deger = (decimal?)b318 });
                                var b321 = -1;
                                var b322 = -1;
                                var b323 = -1;
                                if (ytkNodeRedModel.Hat_3_Konv_2_Calisiyor.GetValueOrDefault(false) == true)
                                {
                                    b321 = KonveyorLogDurum.Calisiyor;
                                    b322 = KonveyorLogDurum.Calisiyor;
                                    b323 = KonveyorLogDurum.Calisiyor;
                                }
                                else
                                {
                                    if (!ytkNodeRedModel.Hat_3_Konv_2_Koptu.GetValueOrDefault(false))
                                    {
                                        b321 = KonveyorLogDurum.Bosta;
                                        b322 = KonveyorLogDurum.Bosta;
                                        b323 = KonveyorLogDurum.Bosta;
                                    }
                                    else
                                    {
                                        b321 = ytkNodeRedModel.Hat_3_Konv_2_Koptu_Konv1_Blm1.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b322 = ytkNodeRedModel.Hat_3_Konv_2_Koptu_Konv1_Blm2.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b323 = ytkNodeRedModel.Hat_3_Konv_2_Koptu_Konv1_Blm3.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                    }
                                }
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 553, KayitZamani = DateTime.Now, Deger = (decimal?)b321 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 554, KayitZamani = DateTime.Now, Deger = (decimal?)b322 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 555, KayitZamani = DateTime.Now, Deger = (decimal?)b323 });
                                var b411 = -1;
                                var b412 = -1;
                                var b413 = -1;
                                var b414 = -1;
                                var b415 = -1;
                                var b416 = -1;
                                var b417 = -1;
                                var b418 = -1;
                                if (ytkNodeRedModel.Hat_4_Konv_1_Calisiyor.GetValueOrDefault(false) == true)
                                {
                                    b411 = KonveyorLogDurum.Calisiyor;
                                    b412 = KonveyorLogDurum.Calisiyor;
                                    b413 = KonveyorLogDurum.Calisiyor;
                                    b414 = KonveyorLogDurum.Calisiyor;
                                    b415 = KonveyorLogDurum.Calisiyor;
                                    b416 = KonveyorLogDurum.Calisiyor;
                                    b417 = KonveyorLogDurum.Calisiyor;
                                    b418 = KonveyorLogDurum.Calisiyor;
                                }
                                else
                                {
                                    if (!ytkNodeRedModel.Hat_4_Konv_1_Koptu.GetValueOrDefault(false))
                                    {
                                        b411 = KonveyorLogDurum.Bosta;
                                        b412 = KonveyorLogDurum.Bosta;
                                        b413 = KonveyorLogDurum.Bosta;
                                        b414 = KonveyorLogDurum.Bosta;
                                        b415 = KonveyorLogDurum.Bosta;
                                        b416 = KonveyorLogDurum.Bosta;
                                        b417 = KonveyorLogDurum.Bosta;
                                        b418 = KonveyorLogDurum.Bosta;
                                    }
                                    else
                                    {
                                        b411 = ytkNodeRedModel.Hat_4_Konv_1_Koptu_Konv1_Blm1.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b412 = ytkNodeRedModel.Hat_4_Konv_1_Koptu_Konv1_Blm2.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b413 = ytkNodeRedModel.Hat_4_Konv_1_Koptu_Konv1_Blm3.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b414 = ytkNodeRedModel.Hat_4_Konv_1_Koptu_Konv1_Blm4.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b415 = ytkNodeRedModel.Hat_4_Konv_1_Koptu_Konv1_Blm5.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b416 = ytkNodeRedModel.Hat_4_Konv_1_Koptu_Konv1_Blm6.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b417 = ytkNodeRedModel.Hat_4_Konv_1_Koptu_Konv1_Blm7.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b418 = ytkNodeRedModel.Hat_4_Konv_1_Koptu_Konv1_Blm8.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                    }
                                }
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 556, KayitZamani = DateTime.Now, Deger = (decimal?)b411 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 557, KayitZamani = DateTime.Now, Deger = (decimal?)b412 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 558, KayitZamani = DateTime.Now, Deger = (decimal?)b413 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 559, KayitZamani = DateTime.Now, Deger = (decimal?)b414 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 560, KayitZamani = DateTime.Now, Deger = (decimal?)b415 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 561, KayitZamani = DateTime.Now, Deger = (decimal?)b416 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 562, KayitZamani = DateTime.Now, Deger = (decimal?)b417 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 563, KayitZamani = DateTime.Now, Deger = (decimal?)b418 });
                                var b421 = -1;
                                var b422 = -1;
                                var b423 = -1;
                                if (ytkNodeRedModel.Hat_4_Konv_2_Calisiyor.GetValueOrDefault(false) == true)
                                {
                                    b421 = KonveyorLogDurum.Calisiyor;
                                    b422 = KonveyorLogDurum.Calisiyor;
                                    b423 = KonveyorLogDurum.Calisiyor;
                                }
                                else
                                {
                                    if (!ytkNodeRedModel.Hat_4_Konv_2_Koptu.GetValueOrDefault(false))
                                    {
                                        b421 = KonveyorLogDurum.Bosta;
                                        b422 = KonveyorLogDurum.Bosta;
                                        b423 = KonveyorLogDurum.Bosta;
                                    }
                                    else
                                    {
                                        b421 = ytkNodeRedModel.Hat_4_Konv_2_Koptu_Konv2_Blm1.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b422 = ytkNodeRedModel.Hat_4_Konv_2_Koptu_Konv2_Blm2.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                        b423 = ytkNodeRedModel.Hat_4_Konv_2_Koptu_Konv2_Blm3.GetValueOrDefault(false) == true ? KonveyorLogDurum.Ariza : KonveyorLogDurum.Bosta;
                                    }
                                }
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 564, KayitZamani = DateTime.Now, Deger = (decimal?)b421 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 565, KayitZamani = DateTime.Now, Deger = (decimal?)b422 });
                                etiketLogs.Add(new EtiketLogKonveyor { LokasyonId = lokasyonId, EtiketId = 566, KayitZamani = DateTime.Now, Deger = (decimal?)b423 });
                            }
                        }
                    }
                    return etiketLogs;
                }
            }
            catch (Exception)
            {
                return new List<EtiketLogKonveyor>();
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("VerimGidaYtkWorker stop: {time}", DateTimeOffset.Now);
                //StopAsync metodu, veritabaný baðlantýlarýný düzgün bir þekilde kapatmak,
                //açýlmýþ baðlantýlarý serbest býrakmak, dýþ hizmetlerle olan baðlantýlarý kapatmak veya
                //iþlem tamamlanmadan önce yapýlmasý gereken bitiþ iþlemleri için kullanýlýr.
                await UpdateServisDurumAsync("VerimGidaYtkWorker", 3, "Servis durduruldu");
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await UpdateServisDurumAsync("VerimGidaYtkWorker", 0, ex.Message);
            }
        }
        public async Task UpdateServisDurumAsync(string servisName, byte durum, string mesaj, bool basariliMi = false)
        {
            try
            {
                mesaj = mesaj.Length > 500 ? mesaj.Substring(0, 500) : mesaj;
                using var scope = _serviceProvider.CreateScope();
                var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();
                var servis = await sharedDbContext.ServisKontrol.FirstOrDefaultAsync(x => x.ServisName == servisName);

                if (servis == null)
                {
                    servis = new ServisKontrol
                    {
                        ServisName = servisName
                    };

                    sharedDbContext.ServisKontrol.Add(servis);
                }

                servis.Durum = durum;
                servis.Mesaj = mesaj;
                servis.SonAktifZaman = DateTime.Now;

                if (basariliMi)
                {
                    servis.SonBasariliZaman = DateTime.Now;
                }

                await sharedDbContext.SaveChangesAsync();
            }
            catch (Exception) { }
        }
    }
}
