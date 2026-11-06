using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using SharedApp.Infrastructure.Sender;

namespace VoiceCallDispatch.Worker
{
    public class MesajGonderimTakipWorker : BackgroundService
    {
        private readonly ILogger<MesajGonderimTakipWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly SesliMesajSender _sesliMesajSender;
        public MesajGonderimTakipWorker(ILogger<MesajGonderimTakipWorker> logger, IServiceProvider serviceProvider, SesliMesajSender sesliMesajSender)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _sesliMesajSender = sesliMesajSender;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("SesliMesajGonderimWorker starting at: {time}", DateTimeOffset.Now);
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                await UpdateServisDurumAsync("SesliMesajGonderimWorker", 0, ex.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SesliMesajKontrolEtVeGonderAsync();
                    //await SesliMesajRaporlariniKontrolEtAsync();
                    await UpdateServisDurumAsync("SesliMesajGonderimWorker", 1, "Servis çalýþýyor", true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Sesli mesaj gönderim hatasý: {error}", ex.Message);
                    await UpdateServisDurumAsync("SesliMesajGonderimWorker", 0, ex.Message);
                }
                finally
                {
                    await Task.Delay(1000, stoppingToken); // 1 saniye bekle
                }
            }
        }

        private async Task SesliMesajKontrolEtVeGonderAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();
                var gonderilecekler = await sharedDbContext.MesajGonderimTakip.Where(p => p.GonderimBasarili != true && p.KalanDenemeSayisi.GetValueOrDefault(0) > 0).ToListAsync();

                foreach (var mesaj in gonderilecekler)
                {
                    //if (mesaj.SonDenemeZamani != null)
                    //{
                    //    var fark = DateTime.Now - mesaj.SonDenemeZamani.Value;
                    //    if (fark.TotalSeconds < 65)
                    //    {
                    //        continue; // çok erken tekrar deneme
                    //    }
                    //}

                    try
                    {
                        // Sesli mesaj gönderimi (arama yapma)
                        var callId = await _sesliMesajSender.SendVoiceCallAsync(mesaj);

                        mesaj.CallId = callId;
                        mesaj.GonderimBasarili = true;
                        mesaj.KalanDenemeSayisi = 0;
                    }
                    catch (Exception ex)
                    {
                        mesaj.HataMesaji = ex.Message;
                        mesaj.GonderimBasarili = false;
                        //mesaj.KalanDenemeSayisi -= 1;
                        mesaj.KalanDenemeSayisi = 0;
                    }
                    finally
                    {
                        mesaj.SonDenemeZamani = DateTime.Now;
                        await sharedDbContext.SaveChangesAsync();
                    }
                }
            }
        }

        //private async Task SesliMesajRaporlariniKontrolEtAsync()
        //{
        //    using var scope = _serviceProvider.CreateScope();
        //    var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();

        //    var raporsuzlar = await sharedDbContext.MesajGonderimTakip
        //        .Where(p => p.CallId != null && p.DurumKodu == null)
        //        .ToListAsync();

        //    foreach (var mesaj in raporsuzlar)
        //    {
        //        var rapor = await _sesliMesajSender.GetReportAsync(mesaj.CallId!);
        //        if (rapor != null)
        //        {
        //            mesaj.DurumKodu = rapor.StatusCode;
        //            mesaj.DurumAciklama = rapor.StatusDesc;
        //            mesaj.CevaplandiMi = rapor.Answered;
        //            mesaj.AramaSure = rapor.Duration;
        //            mesaj.DinlemeSuresi = rapor.Duration;
        //            await sharedDbContext.SaveChangesAsync();
        //        }
        //    }
        //}

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("SesliMesajGonderimWorker stop: {time}", DateTimeOffset.Now);
                //StopAsync metodu, veritabaný baðlantýlarýný düzgün bir þekilde kapatmak,
                //açýlmýþ baðlantýlarý serbest býrakmak, dýþ hizmetlerle olan baðlantýlarý kapatmak veya
                //iþlem tamamlanmadan önce yapýlmasý gereken bitiþ iþlemleri için kullanýlýr.
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await UpdateServisDurumAsync("SesliMesajGonderimWorker", 0, ex.Message);
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

