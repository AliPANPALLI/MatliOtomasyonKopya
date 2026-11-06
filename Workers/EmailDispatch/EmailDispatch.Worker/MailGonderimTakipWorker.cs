using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using SharedApp.Infrastructure.Sender;

namespace EmailDispatch.Worker
{
    public class MailGonderimTakipWorker : BackgroundService
    {
        private readonly ILogger<MailGonderimTakipWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly EmailSender _emailSender;

        public MailGonderimTakipWorker(ILogger<MailGonderimTakipWorker> logger, IServiceProvider serviceProvider, EmailSender emailSender)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _emailSender = emailSender;
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("MailGonderimTakipWorker starting: {time}", DateTimeOffset.Now);
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await UpdateServisDurumAsync("MailGonderimTakipWorker", 0, ex.Message);
            }

        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MailKontrolEtVeGonder(stoppingToken);
                    await UpdateServisDurumAsync("MailGonderimTakipWorker", 1, "Servis çalýþýyor", true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.ToString());
                    await UpdateServisDurumAsync("MailGonderimTakipWorker", 0, ex.Message);
                }
                finally
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        public async Task MailKontrolEtVeGonder(CancellationToken cancellationToken = default)
        {
            //todo basarisiz olani surekli denememek icin dakikada bir tekrar deneyecek sekilda kalan miktar eklenebilir.
            using (var scope = _serviceProvider.CreateScope())

            {
                var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();
                var gonderilecekler = await sharedDbContext.MailGonderimTakip.Where(p => p.GonderimBasarili != true && p.KalanDenemeSayisi.GetValueOrDefault(0) > 0).ToListAsync(cancellationToken);

                foreach (var gonderilecek in gonderilecekler)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (gonderilecek.SonDenemeZamani != null)
                    {
                        var fark = DateTime.Now - gonderilecek.SonDenemeZamani.GetValueOrDefault();
                        if (fark.TotalSeconds < 65)
                        {
                            //ayný dk içinde mail gönderiminde hata olursa
                            continue;
                        }
                    }

                    try
                    {
                        await _emailSender.SendEmailAsync(gonderilecek, cancellationToken);
                        gonderilecek.GonderimBasarili = true;
                        gonderilecek.KalanDenemeSayisi = 0;
                    }
                    catch (Exception ex)
                    {
                        gonderilecek.HataMesaji = ex.Message;
                        gonderilecek.GonderimBasarili = false;
                        gonderilecek.KalanDenemeSayisi -= 1;
                    }
                    finally
                    {
                        gonderilecek.SonDenemeZamani = DateTime.Now;
                    }
                    await sharedDbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("MailGonderimTakipWorker stop: {time}", DateTimeOffset.Now);
                //StopAsync metodu, veritabaný baðlantýlarýný düzgün bir þekilde kapatmak,
                //açýlmýþ baðlantýlarý serbest býrakmak, dýþ hizmetlerle olan baðlantýlarý kapatmak veya
                //iþlem tamamlanmadan önce yapýlmasý gereken bitiþ iþlemleri için kullanýlýr.
                await UpdateServisDurumAsync("MailGonderimTakipWorker", 3, "Servis durduruldu");
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await UpdateServisDurumAsync("MailGonderimTakipWorker", 0, ex.Message);
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