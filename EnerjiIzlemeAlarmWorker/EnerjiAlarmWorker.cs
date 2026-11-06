using EnerjiIzleme.Domain.Models;
using EnerjiIzleme.Infrastructure;
using EnerjiIzleme.Infrastructure.Mongo;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using static SharedKernel.ApplicationConstants;
using AlarmLog = EnerjiIzleme.Domain.Models.AlarmLog;

namespace EnerjiIzlemeAlarmWorker
{
    public class EnerjiAlarmWorker : BackgroundService
    {
        private readonly ILogger<EnerjiAlarmWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMongoClient _mongoClient;
        private readonly MongoDbSettings _mongoSettings;
        public EnerjiAlarmWorker(ILogger<EnerjiAlarmWorker> logger, IServiceProvider serviceProvider, IMongoClient mongoClient, MongoDbSettings mongoSettings)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _mongoClient = mongoClient;
            _mongoSettings = mongoSettings;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("EnerjiIzlemeAlarmWorker working: {time}", DateTimeOffset.Now);
                await UpdateServisDurumAsync("EnerjiIzlemeAlarmWorker", 2, "Servis baþlatýlýyor");
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                await UpdateServisDurumAsync("EnerjiIzlemeAlarmWorker", 0, ex.Message);
            }

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var database = _mongoClient.GetDatabase(_mongoSettings.DatabaseName);
            var collection = database.GetCollection<Logs>("Logs");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();
                        var enerjiIzlemeDbContext = scope.ServiceProvider.GetRequiredService<EnerjiIzlemeDbContext>();

                        var cihazlar = await enerjiIzlemeDbContext.vwCihaz.AsNoTracking().Where(x => x.Aktif != false && x.Silindi != true).ToListAsync(stoppingToken);

                        ////Yeni alarm ekleme
                        //var alarmlar = await sharedDbContext.Alarmlar.Where(x => x.CihazId > 0 && x.Silindi != true).ToListAsync();
                        //foreach (var cihaz in cihazlar)
                        //{
                        //    var alarmvarmi = alarmlar.Where(x => x.CihazId == cihaz.Id).FirstOrDefault();
                        //    if (alarmvarmi == null)
                        //    {
                        //        Alarmlar alarmlar1 = new Alarmlar()
                        //        {
                        //            AlarmAd = cihaz.MakineName + " / " + cihaz.Name + " veri okumuyor",
                        //            AlarmMetni = cihaz.MakineName + " / " + cihaz.Name + " veri okumuyor",
                        //            Aciklama = cihaz.MakineName + " / " + cihaz.Name + " veri okumuyor",
                        //            Silindi = false,
                        //            KonveyorId = null,
                        //            KumesId = null,
                        //            LokasyonId = null,
                        //            Tip = "0",
                        //            AlarmScadaEkrandaGosterilsinMi = true,
                        //            CihazId = cihaz.Id,
                        //            SesliMesajMetni = cihaz.MakineName + " / " + cihaz.Name + " veri okumuyor",
                        //        };
                        //        await sharedDbContext.Alarmlar.AddAsync(alarmlar1);
                        //    }
                        //}
                        //await sharedDbContext.SaveChangesAsync();
                        var now = DateTime.Now;

                        List<AlarmLog> yeniAlarmlar = new();
                        List<AlarmLog> pasifEdilecekler = new();

                        var recentLogs = await collection.Find(x => x.payload.okunma_zamani >= DateTime.Now.AddHours(3).AddMinutes(-5)).SortByDescending(x => x.payload.okunma_zamani).ToListAsync(stoppingToken);

                        var alarmlarList = await sharedDbContext.Alarmlar.Where(x => x.Silindi != true && x.CihazId != null).ToListAsync(stoppingToken);
                        var aktifAlarmlar = await enerjiIzlemeDbContext.AlarmLog.Where(x => x.Aktif != false && x.Silindi == false).ToListAsync(stoppingToken);
                        foreach (var alarm in aktifAlarmlar)
                        {
                            var cihaz = cihazlar.FirstOrDefault(c => c.Id == alarm.CihazId);

                            if (cihaz == null || cihaz.Aktif == false || cihaz.Silindi == true)
                            {
                                alarm.Silindi = true;
                                alarm.Aktif = false;
                                alarm.PasifZamani = now;
                                pasifEdilecekler.Add(alarm);
                            }
                        }

                        foreach (var cihaz in cihazlar)
                        {
                            var cihazLogs = recentLogs.Where(x => x.payload.okunan_analizor_id == cihaz.Id).OrderByDescending(x => x.payload.okunma_zamani).Take(5).ToList();

                            var aktifAlarm = aktifAlarmlar.FirstOrDefault(x => x.CihazId == cihaz.Id);
                            var alarmTanimi = alarmlarList.FirstOrDefault(x => x.CihazId == cihaz.Id);

                            if (cihazLogs != null)
                            {
                                if (aktifAlarm == null && cihazLogs.All(x => x.payload.state != EnerjiIzlemeDurum.Normal))
                                {
                                    yeniAlarmlar.Add(new AlarmLog
                                    {
                                        KayitZamani = now,
                                        Mesaj = cihaz.BolumName + "/" + cihaz.MakineName + "/" + cihaz.Name + " için veri okunamýyor",
                                        AlarmId = alarmTanimi?.Id ?? 0,
                                        AlarmSeviye = AlarmSeviye.Normal,
                                        Aktif = true,
                                        Silindi = false,
                                        CihazId = cihaz.Id
                                    });
                                }
                                //  Veri düzenli geliyorsa, aktif alarm varsa pasifleþtir
                                if (aktifAlarm != null && cihazLogs.Any(x => x.payload.state == EnerjiIzlemeDurum.Normal))
                                {
                                    aktifAlarm.Aktif = false;
                                    aktifAlarm.PasifZamani = now;
                                    //aktifAlarm.Mesaj += " (veri tekrar geldi)";
                                    pasifEdilecekler.Add(aktifAlarm);
                                }
                            }
                        }

                        if (yeniAlarmlar.Any())
                            await enerjiIzlemeDbContext.AlarmLog.AddRangeAsync(yeniAlarmlar, stoppingToken);

                        if (pasifEdilecekler.Any())
                            enerjiIzlemeDbContext.AlarmLog.UpdateRange(pasifEdilecekler);

                        await enerjiIzlemeDbContext.SaveChangesAsync(stoppingToken);
                        await UpdateServisDurumAsync("EnerjiIzlemeAlarmWorker", 1, "Servis çalýþýyor", true);
                    }
                }
                catch (Exception ex)
                {
                    await UpdateServisDurumAsync("EnerjiIzlemeAlarmWorker", 0, ex.Message);
                }
                finally
                {
                    await Task.Delay(20000, stoppingToken);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker stop: {time}", DateTimeOffset.Now);
            //StopAsync metodu, veritabaný baðlantýlarýný düzgün bir þekilde kapatmak,
            //açýlmýþ baðlantýlarý serbest býrakmak, dýþ hizmetlerle olan baðlantýlarý kapatmak veya
            //iþlem tamamlanmadan önce yapýlmasý gereken bitiþ iþlemleri için kullanýlýr.
            await UpdateServisDurumAsync("EnerjiIzlemeAlarmWorker", 3, "Servis durduruldu");
            await base.StopAsync(cancellationToken);
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
