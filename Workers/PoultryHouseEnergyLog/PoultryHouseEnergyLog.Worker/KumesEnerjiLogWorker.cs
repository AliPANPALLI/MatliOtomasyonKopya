using EnergyMonitoring.Domain.Interfaces;
using EnergyMonitoring.Domain.Models;
using EnergyMonitoring.Domain.Models.ReportModels;
using EnergyMonitoring.Infrastructure;
using EnergyMonitoring.Infrastructure.Mongo;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using System.Net.Http.Json;

namespace PoultryHouseEnergyLog.Worker
{
    public class KumesEnerjiLogWorker : BackgroundService
    {
        private readonly ILogger<KumesEnerjiLogWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMongoClient _mongoClient;
        private readonly MongoDbSettings _mongoSettings;
        public KumesEnerjiLogWorker(ILogger<KumesEnerjiLogWorker> logger, IServiceProvider serviceProvider, IMongoClient mongoClient, MongoDbSettings mongoSettings)
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
                _logger.LogInformation("EnerjiIzlemeKumesKayitWorker working: {time}", DateTimeOffset.Now);
                await UpdateServisDurumAsync("EnerjiIzlemeKumesKayitWorker", 2, "Servis baþlatýlýyor");
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                await UpdateServisDurumAsync("EnerjiIzlemeKumesKayitWorker", 0, ex.Message);
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Her lokasyon için hangi cihaz ID'lerinin toplanacaðýný buradan yönetirsiniz
            var lokasyonAnalizorMap = new Dictionary<int, List<int>> { { 12, new List<int> { 219/*, 218*/ } }/*, { 2, new List<int> { 201, 202, 203 } }*/, };

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 10 dakika hizalamasý
                    var utcNow = DateTime.UtcNow;
                    var minutes = (utcNow.Minute / 10) * 10;
                    var aligned = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, minutes, 0, DateTimeKind.Utc).AddMinutes(10);
                    var delay = aligned - utcNow;
                    if (delay > TimeSpan.Zero)
                    {
                        await UpdateServisDurumAsync("EnerjiIzlemeKumesKayitWorker", 1, "Servis çalýþýyor", true);
                        await Task.Delay(delay, stoppingToken);
                    }

                    var runTime = DateTime.Now;
                    var todayStart = runTime.Date;

                    using var scope = _serviceProvider.CreateScope();
                    var logsRepository = scope.ServiceProvider.GetRequiredService<ILogsRepository>();
                    var treeRepository = scope.ServiceProvider.GetRequiredService<ITreeRepository>();
                    var dbContext = scope.ServiceProvider.GetRequiredService<EnerjiIzlemeDbContext>();
                    var kumesDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

                    var kumesLoglari = new List<KumesEnerjiLog>();

                    foreach (var (lokasyonId, cihazIdList) in lokasyonAnalizorMap)
                    {
                        try
                        {
                            decimal toplamImport = 0m;
                            long toplamMaliyet = 0;

                            foreach (var cihazId in cihazIdList)
                            {
                                try
                                {
                                    var doc = await logsRepository
                                        .GetTuketimEnerjiVerileriByCihazId(
                                            cihazId, todayStart, runTime, stoppingToken);

                                    if (doc == null || !doc.Contains("import_fark"))
                                        continue;

                                    toplamImport += doc.GetValue("import_fark", 0).ToDecimal();

                                    toplamMaliyet += await GetTuketimBedeliByCihazList(
                                        new List<int> { cihazId },
                                        todayStart, runTime,
                                        lokasyonId,
                                        logsRepository, treeRepository,
                                        stoppingToken);
                                }
                                catch
                                {
                                    // tek cihaz hatasý lokasyonu durdurmasýn
                                }
                            }

                            // En az bir cihazdan veri geldiyse kaydet
                            if (toplamImport > 0 || toplamMaliyet > 0)
                            {
                                kumesLoglari.Add(new KumesEnerjiLog
                                {
                                    LokasyonId = lokasyonId,
                                    KayitZamani = DateTime.Now,
                                    ImportAktifEnerji = toplamImport / 1000,
                                    TuketimBedeli = toplamMaliyet,
                                    Silindi = false
                                });
                            }
                        }
                        catch
                        {
                            // lokasyon hatasý diðerlerini durdurmasýn
                        }
                    }

                    if (kumesLoglari.Any())
                    {
                        await kumesDbContext.KumesEnerjiLog.AddRangeAsync(kumesLoglari, stoppingToken);
                        await kumesDbContext.SaveChangesAsync(stoppingToken);
                    }

                    await UpdateServisDurumAsync("EnerjiIzlemeKumesKayitWorker", 1, "Servis çalýþýyor", true);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await UpdateServisDurumAsync("EnerjiIzlemeKumesKayitWorker", 0, ex.Message);
                    // loglama
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        public async Task<long> GetTuketimBedeliByCihazList(List<int> cihazNodeIds, DateTime baslangicZamani, DateTime bitisZamani, int lokasyonId, ILogsRepository logsRepository, ITreeRepository treeRepository, CancellationToken cancellationToken)
        {
            try
            {
                var result = new List<EnerjiMaliyetSonuc>();

                if (cihazNodeIds == null || cihazNodeIds.Count == 0) return 0;

                // 1) TGT + PTF
                var tgt = await GetTGTFromEPIAS(cancellationToken);
                var ptfItems = await GetPtfValues(baslangicZamani, bitisZamani, tgt, cancellationToken);
                if (ptfItems == null)
                {
                    throw new Exception("EPIAS veri çekilirken hata oluþtu!");
                }

                // PTF map’le
                // Key: (Date.Date, Date.Hour)
                var ptfMap = ptfItems.GroupBy(x => (x.Date.Date, x.Date.Hour)).ToDictionary(g => g.Key, g => g.First().Price);

                // 2) Parametre ay bazlý
                var paramCache = new Dictionary<(int yil, int ay), EnerjiMaliyetParametreler>();
                var enerjiParametreler = await treeRepository.GetAllParametrelerByLokasyonId(23);
                foreach (var cihazId in cihazNodeIds)
                {
                    // 3) Cihaz için Mongo’dan parçalý tüketimler
                    var segmentler = await logsRepository.GetSaatlikTuketimFromMongoAsync(
                        cihazId, baslangicZamani, bitisZamani, cancellationToken);

                    foreach (var seg in segmentler)
                    {
                        var baslangic = seg["baslangic"].ToUniversalTime().AddHours(3);
                        var bitis = seg["bitis"].ToUniversalTime().AddHours(3);

                        var dakika = seg["dakika"].ToDouble();
                        var importFarkWh = seg["import_fark"].ToDecimal();
                        var exportFarkWh = seg["export_fark"].ToDecimal();
                        var netTuketimWh = seg.Contains("net_tuketim_wh") ? seg["net_tuketim_wh"].ToDecimal() : (importFarkWh - exportFarkWh);

                        if (dakika <= 0) continue;

                        // 4) Bu segmentin ayýna uygun parametreleri bul/cache’le
                        var key = (baslangic.Year, baslangic.Month);
                        if (!paramCache.TryGetValue(key, out var param))
                        {
                            param = enerjiParametreler.FirstOrDefault(x => x.Yil == baslangic.Year && x.Ay == baslangic.Month);
                            if (param == null)
                            {
                                continue;
                            }
                            paramCache[key] = param;
                        }

                        var yekdemTlMWh = param.Yekdem;
                        var dagitimTlMWh = param.DagitimBedeli;
                        var anlasmaOrYuzde = param.AnlasmaOrani;
                        var anlasmaOr = anlasmaOrYuzde / 100m;

                        // 5) Ýlgili saat için PTF
                        ptfMap.TryGetValue((baslangic.Date, baslangic.Hour), out var ptfTlMWh);

                        // 6) Maliyet hesabý                   
                        var enerjiBirimTlKWh = ((ptfTlMWh + yekdemTlMWh) * (1 + anlasmaOr)) / 1000m;
                        var dagitimBirimTlKWh = dagitimTlMWh / 1000m;
                        var birimFiyatTlKWh = enerjiBirimTlKWh + dagitimBirimTlKWh;

                        var tuketimWh = importFarkWh;

                        var tuketimKWh = tuketimWh / 1000m;
                        var maliyetTl = tuketimKWh * birimFiyatTlKWh;

                        result.Add(new EnerjiMaliyetSonuc
                        {
                            MaliyetTl = maliyetTl
                        });
                    }
                }
                return (long)result.Sum(x => x.MaliyetTl);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<string> GetTGTFromEPIAS(CancellationToken cancellationToken)
        {
            using (HttpClient client = new HttpClient())
            {
                // Ýstek baþlýklarý
                client.DefaultRequestHeaders.Add("Accept", "text/plain");
                string url = "https://giris.epias.com.tr/cas/v1/tickets";

                // Form verisi (Body)
                var formData = new Dictionary<string, string> { { "username", "beray.bayazit@matli.com.tr" }, { "password", "Bb.300521" } };
                var content = new FormUrlEncodedContent(formData);
                try
                {
                    HttpResponseMessage response = await client.PostAsync(url, content, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        // Gelen cevabý string olarak al
                        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        //Console.WriteLine("Gelen Veri: " + responseBody);
                        return responseBody;
                    }
                    else
                    {
                        string errorMessage = $"Hata: {response.StatusCode} - {response.ReasonPhrase}";
                        //Console.WriteLine(errorMessage);
                        return null;
                    }
                }
                catch (Exception)
                {
                    //Console.WriteLine("Bir hata oluþtu: " + ex.Message);
                    return null;
                }
            }
        }

        public async Task<List<PtfItem>> GetPtfValues(DateTime startDate, DateTime endDate, string tgtToken, CancellationToken cancellationToken)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("TGT", tgtToken);

                    var body = new
                    {
                        startDate = startDate.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                        endDate = endDate.ToString("yyyy-MM-ddTHH:mm:sszzz")
                    };

                    var response = await client.PostAsJsonAsync(
                        "https://seffaflik.epias.com.tr/electricity-service/v1/markets/dam/data/mcp",
                        body, cancellationToken);

                    response.EnsureSuccessStatusCode();

                    var result = await response.Content.ReadFromJsonAsync<PtfResponse>(cancellationToken);

                    return result?.Items ?? new List<PtfItem>();
                }
            }
            catch (Exception)
            {
                return null;
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

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("EnerjiIzlemeKumesKayitWorker stop: {time}", DateTimeOffset.Now);
                //StopAsync metodu, veritabaný baðlantýlarýný düzgün bir þekilde kapatmak,
                //açýlmýþ baðlantýlarý serbest býrakmak, dýþ hizmetlerle olan baðlantýlarý kapatmak veya
                //iþlem tamamlanmadan önce yapýlmasý gereken bitiþ iþlemleri için kullanýlýr.
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await UpdateServisDurumAsync("EnerjiIzlemeKumesKayitWorker", 0, ex.Message);
            }
        }
    }
}