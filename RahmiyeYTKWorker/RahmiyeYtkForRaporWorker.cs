using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RahmiyeYTKWorker.Core;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using System.Data;
using static SharedKernel.ApplicationConstants;

namespace RahmiyeYTKWorker
{
    public class RahmiyeYtkForRaporWorker : BackgroundService
    {
        private readonly ILogger<RahmiyeYtkForRaporWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        //private readonly AlarmCacheService _alarmCacheService;
        private readonly Dictionary<int, string> oncekiDurumlar = new();

        public RahmiyeYtkForRaporWorker(ILogger<RahmiyeYtkForRaporWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            //_alarmCacheService = alarmCacheService;
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("RahmiyeYtkWorker working: {time}", DateTimeOffset.Now);
                await UpdateServisDurumAsync("RahmiyeYtkWorker", 2, "Servis baþlatýlýyor");
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await UpdateServisDurumAsync("RahmiyeYtkWorker", 0, ex.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

                    var now = DateTime.Now;
                    var eklenecekKonveyorRaporlar = new List<KonveyorRapor>();
                    var konveyorEtiketler = await db.Etiket.AsNoTracking().ToListAsync();
                    var tumkonveyorler = await db.Konveyor.AsNoTracking().Where(x => x.LokasyonId == 12).ToListAsync();
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
                                if ((sonRapor.Durum == konveyorRapor.Durum && !konveyorRapor.VeriYok.GetValueOrDefault(false) && !sonRapor.VeriYok.GetValueOrDefault(false)) || (sonRapor?.VeriYok == true && konveyorRapor.VeriYok == true))
                                {
                                    continue;
                                }
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
                                if ((sonRapor.Durum == konveyorRapor.Durum && !konveyorRapor.VeriYok.GetValueOrDefault(false) && !sonRapor.VeriYok.GetValueOrDefault(false)) || (sonRapor?.VeriYok == true && konveyorRapor.VeriYok == true))
                                {
                                    continue;
                                }
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
                                if ((sonRapor.Durum == konveyorRapor.Durum && !konveyorRapor.VeriYok.GetValueOrDefault(false) && !sonRapor.VeriYok.GetValueOrDefault(false)) || (sonRapor?.VeriYok == true && konveyorRapor.VeriYok == true))
                                {
                                    continue;
                                }
                            }
                            eklenecekKonveyorRaporlar.Add(konveyorRapor);
                        }

                        await db.KonveyorRapor.AddRangeAsync(eklenecekKonveyorRaporlar);
                        await db.SaveChangesAsync();
                    }
                    await UpdateServisDurumAsync("RahmiyeYtkWorker", 1, "Servis çalýþýyor", true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hata oluþtu:");
                    await UpdateServisDurumAsync("RahmiyeYtkWorker", 0, ex.Message);
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("RahmiyeYtkWorker stop: {time}", DateTimeOffset.Now);
                //StopAsync metodu, veritabaný baðlantýlarýný düzgün bir þekilde kapatmak,
                //açýlmýþ baðlantýlarý serbest býrakmak, dýþ hizmetlerle olan baðlantýlarý kapatmak veya
                //iþlem tamamlanmadan önce yapýlmasý gereken bitiþ iþlemleri için kullanýlýr.
                await UpdateServisDurumAsync("RahmiyeYtkWorker", 3, "Servis durduruldu");
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await UpdateServisDurumAsync("RahmiyeYtkWorker", 0, ex.Message);
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