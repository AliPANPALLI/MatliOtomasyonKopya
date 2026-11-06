using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using SharedApp.Infrastructure.Sender;

namespace SmsGonderimWorker
{
    public class SmsGonderimTakipWorker : BackgroundService
    {
        private readonly ILogger<SmsGonderimTakipWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly SmsSender _smsSender;

        public SmsGonderimTakipWorker(ILogger<SmsGonderimTakipWorker> logger, IServiceProvider serviceProvider, SmsSender smsSender)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _smsSender = smsSender;
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UpdateServisDurumAsync("SmsGonderimTakipWorker", 2, "Servis baþlatýlýyor");
                _logger.LogInformation("SmsGonderimTakipWorker starting: {time}", DateTimeOffset.Now);
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await UpdateServisDurumAsync("SmsGonderimTakipWorker", 0, ex.Message);
            }

        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SmsKontrolEtVeGonder();
                    await UpdateServisDurumAsync("SmsGonderimTakipWorker", 1, "Servis çalýþýyor", true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.ToString());
                    await UpdateServisDurumAsync("SmsGonderimTakipWorker", 0, ex.Message);
                }
                finally
                {
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        public async Task SmsKontrolEtVeGonder()
        {
            //todo basarisiz olani surekli denememek icin dakikada bir tekrar deneyecek sekilda kalan miktar eklenebilir.
            using (var scope = _serviceProvider.CreateScope())

            {
                var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();
                var gonderilecekler = await sharedDbContext.GonderilenSms.Where(p => p.BasariliMi == null /*&& p.KalanDenemeSayisi.GetValueOrDefault(0) > 0*/).ToListAsync();

                foreach (var gonderilecek in gonderilecekler)
                {
                    //if (gonderilecek.SonDenemeZamani != null)
                    //{
                    //    var fark = DateTime.Now - gonderilecek.SonDenemeZamani.GetValueOrDefault();
                    //    if (fark.TotalSeconds < 65)
                    //    {
                    //        //ayný dk içinde mail gönderiminde hata olursa
                    //        continue;
                    //    }
                    //}

                    try
                    {
                        var result = await _smsSender.SendSmsForAlarm(gonderilecek.CepTelefonu, gonderilecek.Mesaj, "");
                        gonderilecek.BasariliMi = result.Item1;
                        gonderilecek.DonusMesaj = result.Item2;
                        gonderilecek.SonDenemeZamani = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        gonderilecek.DonusMesaj = ex.Message;
                        gonderilecek.BasariliMi = false;
                        gonderilecek.SonDenemeZamani = DateTime.Now;
                        //gonderilecek.KalanDenemeSayisi -= 1;
                    }
                    finally
                    {
                        //gonderilecek.SonDenemeZamani = DateTime.Now;
                    }
                    await sharedDbContext.SaveChangesAsync();
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("SmsGonderimTakipWorker stop: {time}", DateTimeOffset.Now);
                //StopAsync metodu, veritabaný baðlantýlarýný düzgün bir þekilde kapatmak,
                //açýlmýþ baðlantýlarý serbest býrakmak, dýþ hizmetlerle olan baðlantýlarý kapatmak veya
                //iþlem tamamlanmadan önce yapýlmasý gereken bitiþ iþlemleri için kullanýlýr.
                await UpdateServisDurumAsync("SmsGonderimTakipWorker", 3, "Servis durduruldu");
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await UpdateServisDurumAsync("SmsGonderimTakipWorker", 0, ex.Message);
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
