using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using System.ComponentModel;

namespace ServisKontrolWorker
{
    public class ServisKontrolEtWorker : BackgroundService
    {
        private readonly ILogger<ServisKontrolEtWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ServisKontrolEtWorker(ILogger<ServisKontrolEtWorker> logger, IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ServisKontrolEtWorker starting: {time}", DateTimeOffset.Now);
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<SharedDbContext>();
                    var servisler = await db.ServisKontrol.ToListAsync(stoppingToken);

                    foreach (var servis in servisler)
                    {
                        if (!ServiceAlarmMap.TryGetValue(servis.ServisName, out var alarmId))
                            continue;

                        if (!servis.SonAktifZaman.HasValue)
                            continue;

                        var timeoutOldu = DateTime.Now > servis.SonAktifZaman.Value.AddMinutes(Convert.ToDouble(servis.ZamanAsimiDakika ?? 0));
                        if (!timeoutOldu)
                        {
                            // Varsa aktif alarmý kapat
                            var aktifAlarm = await db.AlarmLog.FirstOrDefaultAsync(x => x.AlarmId == (int)alarmId && x.Aktif == true && x.Silindi != true, stoppingToken);

                            if (aktifAlarm != null)
                            {
                                aktifAlarm.Aktif = false;
                                aktifAlarm.PasifZamani = DateTime.Now;
                            }

                            continue;
                        }

                        // Aktif alarm var mý?
                        var mevcutAlarm = await db.AlarmLog.FirstOrDefaultAsync(x => x.AlarmId == (int)alarmId && x.Aktif == true && x.Silindi != true, stoppingToken);

                        if (mevcutAlarm != null)
                            continue;

                        db.AlarmLog.Add(new AlarmLog
                        {
                            KayitZamani = DateTime.Now,
                            AlarmId = (int)alarmId,
                            AlarmSeviye = 2,
                            Aktif = true,
                            Silindi = false,
                            Mesaj =
                            $"{servis.ServisName} servisinden {servis.ZamanAsimiDakika} dakikadýr heartbeat alýnamadý."
                        });

                        _logger.LogWarning("{Servis} timeout oldu", servis.ServisName);
                    }
                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ServisKontrolEtWorker çalýþýrken hata oluþtu: {message}", ex.Message);
                }
                finally
                {
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ServisKontrolEtWorker stop: {time}", DateTimeOffset.Now);
            //StopAsync metodu, veritabaný baðlantýlarýný düzgün bir þekilde kapatmak,
            //açýlmýþ baðlantýlarý serbest býrakmak, dýþ hizmetlerle olan baðlantýlarý kapatmak veya
            //iþlem tamamlanmadan önce yapýlmasý gereken bitiþ iþlemleri için kullanýlýr.
            await base.StopAsync(cancellationToken);
        }
        private static readonly Dictionary<string, AlarmIds> ServiceAlarmMap = new()
        {
            { "RahmiyeTesisLogWorker", AlarmIds.RahmiyeTesisLogWorker_Calismiyor },
            { "RahmiyeTesisWorker", AlarmIds.RahmiyeTesisWorker_Calismiyor },
            { "AlarmBilgilendirmeService", AlarmIds.AlarmWorker_Calismiyor },
            { "EnerjiIzlemeAlarmWorker", AlarmIds.EnerjiIzlemeAlarmWorker_Calismiyor },
            { "EnerjiIzlemeKumesKayitWorker", AlarmIds.KumesEnerjiLogKayitWorker_Calismiyor },
            { "MailGonderimTakipWorker", AlarmIds.MailGonderimWorker_Calismiyor },
            { "SesliMesajGonderimWorker", AlarmIds.MesajGonderimWorker_Calismiyor },
            { "RahmiyeYtkWorker", AlarmIds.RahmiyeYTKWorker_Calismiyor },
            { "SmsGonderimTakipWorker", AlarmIds.SmsGonderimWorker_Calismiyor },
            { "SuIzlemeLogNodeRedWorker", AlarmIds.SuIzlemeLogWorker_Calismiyor },
            { "SuIzlemeWorker", AlarmIds.SuIzlemeWorker_Calismiyor },
            { "VerimGidaYtkWorker", AlarmIds.VerimGidaYTKWorker_Calismiyor }
        };
    }
}

public enum AlarmIds : int
{
    [Description("RahmiyeTesisLogWorker çalýþmýyor")]
    RahmiyeTesisLogWorker_Calismiyor = 1049,
    [Description("RahmiyeTesisWorker çalýþmýyor")]
    RahmiyeTesisWorker_Calismiyor = 1050,
    [Description("AlarmWorker çalýþmýyor")]
    AlarmWorker_Calismiyor = 1051,
    [Description("EnerjiIzlemeAlarmWorker çalýþmýyor")]
    EnerjiIzlemeAlarmWorker_Calismiyor = 1052,
    [Description("KumesEnerjiLogKayitWorker çalýþmýyor")]
    KumesEnerjiLogKayitWorker_Calismiyor = 1053,
    [Description("MailGonderimWorker çalýþmýyor")]
    MailGonderimWorker_Calismiyor = 1054,
    [Description("MesajGonderimWorker çalýþmýyor")]
    MesajGonderimWorker_Calismiyor = 1055,
    [Description("RahmiyeYTKWorker çalýþmýyor")]
    RahmiyeYTKWorker_Calismiyor = 1056,
    [Description("SmsGonderimWorker çalýþmýyor")]
    SmsGonderimWorker_Calismiyor = 1057,
    [Description("SuIzlemeLogWorker çalýþmýyor")]
    SuIzlemeLogWorker_Calismiyor = 1058,
    [Description("SuIzlemeWorker çalýþmýyor")]
    SuIzlemeWorker_Calismiyor = 1059,
    [Description("VerimGidaYTKWorker çalýþmýyor")]
    VerimGidaYTKWorker_Calismiyor = 1060,
}