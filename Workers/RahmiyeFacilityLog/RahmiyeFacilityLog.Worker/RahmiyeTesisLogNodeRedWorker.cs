using FastMember;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using System.Data;
using System.Text.Json;
using RahmiyeFacilityLog.Application.Models;
using RahmiyeFacilityLog.Infrastructure.Caching;
using RahmiyeFacilityLog.Infrastructure.Sap;

namespace RahmiyeFacilityLog.Worker
{
    public class RahmiyeTesisLogNodeRedWorker : BackgroundService
    {
        private readonly ILogger<RahmiyeTesisLogNodeRedWorker> _logger;
        private readonly SqlConnection _sqlConnection;
        private readonly IServiceProvider _serviceProvider;
        private readonly CacheService _cacheService;
        private readonly SapKantarService _sapKantarService;
        private List<Kumes> kumesListesi = new List<Kumes>();
        private Dictionary<int, EggActualDbModel?> yumurtaBilgileriDict = new Dictionary<int, EggActualDbModel?>();
        private Dictionary<int, decimal> sapGunlukYemGirisiDict = new Dictionary<int, decimal>();
        private readonly string _vbapConnStr;

        /// <summary>
        /// SAP kantar servisindeki LGORT (depo) kodu -> KumesId eslesmesi. Yeni kumes eklenirse buraya eklenmelidir.
        /// </summary>
        private static readonly Dictionary<string, int> LgortKumesEslesme = new(StringComparer.OrdinalIgnoreCase)
        {
            ["TR01"] = 1,
            ["TR02"] = 2,
            ["TR03"] = 3,
            ["TR04"] = 4,
            ["TR05"] = 5,
            ["TR06"] = 6,
            ["TR07"] = 7,
            ["TR08"] = 8,
            ["TR09"] = 9,
            ["TR10"] = 10,
            ["TR11"] = 11,
        };

        /*
         * KumesIzlemeNodeRed'de "bugüne ait son satır" bulunması PLC'nin şu an
         * okunduğu anlamına gelmez; satır eski bir Node-RED yazımından kalmış
         * olabilir (ör. tablo artık her turda truncate edilmiyorsa). Bu yüzden
         * Durum == 1 kontrolüne ek olarak kaydın KayitZamani'si de bu pencere
         * içinde mi diye bakılır (Kayislar1KumesNodeRedWorker.PlcOkuyorMu ile aynı desen).
         */
        private static readonly TimeSpan PlcVeriZamanAsimi = TimeSpan.FromMinutes(3);

        private static bool PlcVerisiGuncelMi(KumesIzlemeNodeRed? model, DateTime kontrolZamani)
        {
            return model?.Durum == 1 &&
                   model.KayitZamani.HasValue &&
                   model.KayitZamani.Value >= kontrolZamani.Subtract(PlcVeriZamanAsimi);
        }

        public RahmiyeTesisLogNodeRedWorker(ILogger<RahmiyeTesisLogNodeRedWorker> logger, SqlConnection sqlConnection, IServiceProvider serviceProvider, CacheService cacheService, IConfiguration configuration, SapKantarService sapKantarService)
        {
            _vbapConnStr = configuration.GetConnectionString("SqlServerVbap");
            _logger = logger;
            _sqlConnection = sqlConnection;
            _cacheService = cacheService;
            _serviceProvider = serviceProvider;
            _sapKantarService = sapKantarService;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("RahmiyeTesisLogWorker working: {time}", DateTimeOffset.Now);
                await UpdateServisDurumAsync("RahmiyeTesisLogWorker", 2, "Servis ba�lat�l�yor");
                kumesListesi = await _cacheService.GetKumeslerAsync(12);
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                await UpdateServisDurumAsync("RahmiyeTesisLogWorker", 0, ex.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    yumurtaBilgileriDict.Clear();
                    sapGunlukYemGirisiDict = await GetSapGunlukYemGirisiByKumesIdAsync(stoppingToken);
                    for (int i = 0; i < kumesListesi.Count(); i++)
                    {
                        var yumurtaBilgileri = await GetVbapYumurtaBilgileri(kumesListesi[i].Id, DateTime.Now, stoppingToken);
                        yumurtaBilgileriDict[kumesListesi[i].Id] = yumurtaBilgileri;
                        var dataEtiketLogYeni = await GetEtiketLogYeniDataForBulkInsert(kumesListesi[i].Id, stoppingToken);
                        await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeni);
                        var dataKumesLogYeni = await GetKumesLogYeniDataForBulkInsertAsync(kumesListesi[i].Id, stoppingToken);
                        if (dataKumesLogYeni.Count() > 0) await BulkInsertKumesLogYeniDataAsync(_sqlConnection, dataKumesLogYeni);
                    }
                    await UpdateServisDurumAsync("RahmiyeTesisLogWorker", 1, "Servis �al���yor", true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    await UpdateServisDurumAsync("RahmiyeTesisLogWorker", 0, ex.Message);
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
                }
            }
        }

        //private async Task<IEnumerable<KumesLogYeni>> GetKumesLogFromEtiketLog(int kumesId, CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        var kayitlar = await _cacheService.GetEtiketLogYeniByKumesId(kumesId);
        //        var kumesLogList = kayitlar
        //                             .GroupBy(x => x.KayitZamani)
        //                             .Select(g =>
        //                             {
        //                                 var log = new KumesLogYeni
        //                                 {
        //                                     KumesId = 1,
        //                                     KayitZamani = g.Key
        //                                 };

        //                                 foreach (var item in g)
        //                                 {
        //                                     var value = Convert.ToInt32(item.Deger ?? 0);

        //                                     switch (item.EtiketId)
        //                                     {
        //                                         case 106: log.YumurtaAdet = value; break;
        //                                         case 107: log.SAdet = value; break;
        //                                         case 108: log.MAdet = value; break;
        //                                         case 109: log.LAdet = value; break;
        //                                         case 110: log.XLAdet = value; break;
        //                                         case 111: log.AsiriKucukAdet = value; break;
        //                                         case 112: log.KirikYumurtaAdet = value; break;
        //                                         case 113: log.HicKirliYumurtaAdet = value; break;
        //                                         case 114: log.AzKirliYumurtaAdet = value; break;
        //                                         case 115: log.OrtaKirliYumurtaAdet = value; break;
        //                                         case 116: log.CokKirliYumurtaAdet = value; break;
        //                                         case 117: log.HicKanliYumurtaAdet = value; break;
        //                                         case 118: log.AzKanliYumurtaAdet = value; break;
        //                                         case 119: log.OrtaKanliYumurtaAdet = value; break;
        //                                         case 120: log.CokKanliYumurtaAdet = value; break;
        //                                         case 121: log.HicDiskiliYumurtaAdet = value; break;
        //                                         case 122: log.AzDiskiliYumurtaAdet = value; break;
        //                                         case 123: log.OrtaDiskiliYumurtaAdet = value; break;
        //                                         case 124: log.CokDiskiliYumurtaAdet = value; break;
        //                                     }
        //                                 }
        //                                 return log;
        //                             }).ToList();
        //        return kumesLogList;

        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        private async Task<IEnumerable<KumesLogYeni>> GetKumesLogYeniDataForBulkInsertAsync(int kumesId, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;
                KumesIzlemeNodeRed? model;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    model = await kumesScadaDbContext.KumesIzlemeNodeRed.AsNoTracking().AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani >= now.Date && x.KayitZamani < now.Date.AddDays(1)).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                    var gunlukHayvanSayisi = await _cacheService.GetGunlukCanliHayvanSayisiByKumesId(kumesId);
                    var yumurtaBilgileri = GetYumurtaBilgiHelper(kumesId);

                    if (model != null)
                    {
                        if (PlcVerisiGuncelMi(model, now))
                        {
                            if (model.Veriler != null)
                            {
                                KumesNodeRedModel kumesNodeRedModel = JsonSerializer.Deserialize<KumesNodeRedModel>(model.Veriler);

                                var sicaklik_Kumes = kumesNodeRedModel.Sicaklik_Kumes_Value;
                                var sicaklik_Dis = kumesNodeRedModel.Sicaklik_Dis_Value;
                                var sicaklik_Sensor1 = kumesNodeRedModel.Sicaklik_Sensor_1_Value;
                                var sicaklik_Sensor2 = kumesNodeRedModel.Sicaklik_Sensor_2_Value;
                                var sicaklik_Sensor3 = kumesNodeRedModel.Sicaklik_Sensor_3_Value;
                                var sicaklik_Sensor4 = kumesNodeRedModel.Sicaklik_Sensor_4_Value;
                                var sicaklik_Sensor5 = kumesNodeRedModel.Sicaklik_Sensor_5_Value;
                                var sicaklik_Sensor6 = kumesNodeRedModel.Sicaklik_Sensor_6_Value;
                                var sicaklik_Sensor7 = 0;
                                var sicaklik_Sensor8 = 0;
                                var nem_Kumes = kumesNodeRedModel.Nem_Kumes_Value;
                                var nem_Dis = kumesNodeRedModel.Nem_Dis_Value;
                                var nem_Sensor1 = kumesNodeRedModel.Nem_Sensor_1_Value;
                                var nem_Sensor2 = kumesNodeRedModel.Nem_Sensor_2_Value;
                                var nem_Sensor3 = kumesNodeRedModel.Nem_Sensor_3_Value;
                                var nem_Sensor4 = kumesNodeRedModel.Nem_Sensor_4_Value;
                                var nem_Sensor5 = kumesNodeRedModel.Nem_Sensor_5_Value;
                                var nem_Sensor6 = kumesNodeRedModel.Nem_Sensor_6_Value;
                                var silo1 = kumesNodeRedModel.Yem_Silo_1_Value;
                                var silo2 = kumesNodeRedModel.Yem_Silo_2_Value;
                                var basinc = kumesNodeRedModel.Basinc_Sens_Value;
                                var co2 = kumesNodeRedModel.CO2_Sens_Value;
                                var icmeSuyuTuketim = kumesNodeRedModel.IcmeSuyu_TuketimBugun;
                                var yemTuketim = kumesNodeRedModel.Yem_TuketimBugun;

                                if (icmeSuyuTuketim.GetValueOrDefault(0) > 50)
                                {
                                    var lastCounter = await _cacheService.GetDunIcmeSuyuSayacByKumesId(kumesId, kumesNodeRedModel.IcmeSuyu_Sayac.GetValueOrDefault(0));
                                    var currentCounter = kumesNodeRedModel.IcmeSuyu_Sayac.GetValueOrDefault(0);

                                    decimal fark = 0;
                                    fark = currentCounter >= lastCounter ? currentCounter - lastCounter : currentCounter;
                                    icmeSuyuTuketim = fark;
                                }

                                var sapGunlukYemGirisi = sapGunlukYemGirisiDict.TryGetValue(kumesId, out var sapDeger) ? sapDeger : 0m;
                                var dunGunSonuSiloToplam = await GetDunGunSonuSiloToplamAsync(kumesScadaDbContext, kumesId, now, stoppingToken);
                                var bugunSiloToplam = silo1.GetValueOrDefault(0) + silo2.GetValueOrDefault(0);
                                var hesaplananYemTuketimi = dunGunSonuSiloToplam + sapGunlukYemGirisi - bugunSiloToplam;

                                // PLC'den al�nan verileri burada d�nd�r�yoruz
                                return new List<KumesLogYeni>
                                 {
                                       new KumesLogYeni { KumesId = kumesId,
                                           DonemId=1,
                                           KayitZamani=DateTime.Now,
                                           SapGunlukYemGirisi=(decimal?)sapGunlukYemGirisi,
                                           HesaplananYemTuketimi=(decimal?)hesaplananYemTuketimi,
                                           SicaklikKumes=(decimal?)sicaklik_Kumes,
                                           SicaklikDis=(decimal?)sicaklik_Dis,
                                           Sicaklik1=(decimal?)sicaklik_Sensor1,
                                           Sicaklik2=(decimal?)sicaklik_Sensor2,
                                           Sicaklik3=(decimal?)sicaklik_Sensor3,
                                           Sicaklik4=(decimal?)sicaklik_Sensor4,
                                           Sicaklik5=(decimal?)sicaklik_Sensor5,
                                           Sicaklik6=(decimal?)sicaklik_Sensor6,
                                           Sicaklik7=(decimal?)sicaklik_Sensor7,
                                           Sicaklik8=(decimal?)sicaklik_Sensor8,
                                           NemKumes=(decimal?)nem_Kumes,
                                           NemDis=(decimal?)nem_Dis,
                                           Nem1=(decimal?)nem_Sensor1,
                                           Nem2=(decimal?)nem_Sensor2,
                                           Nem3=(decimal?)nem_Sensor3,
                                           Nem4=(decimal?)nem_Sensor4,
                                           Nem5=(decimal?)nem_Sensor5,
                                           Nem6=(decimal?)nem_Sensor6,

                                           Silo1=(decimal?)silo1,
                                           Silo2=(decimal?)silo2,
                                           Basinc1=(decimal?)basinc,
                                           Co2_1=(decimal?)co2,
                                           YemTuketimBugun=(decimal?)yemTuketim,
                                           SuTuketimBugun=(decimal?)icmeSuyuTuketim,
                                           YumurtaAdet=yumurtaBilgileri==null ? null : yumurtaBilgileri.KumesYumurtaAdet,
                                           SAdet=yumurtaBilgileri==null ? null :yumurtaBilgileri.SAdet,
                                           MAdet = yumurtaBilgileri==null ? null :yumurtaBilgileri.MAdet,
                                           LAdet = yumurtaBilgileri==null ? null :yumurtaBilgileri.LAdet,
                                           XLAdet = yumurtaBilgileri==null ? null :yumurtaBilgileri.XLAdet,
                                           AsiriKucukAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.AsiriKucukAdet,
                                           KirikYumurtaAdet = yumurtaBilgileri==null ? null :yumurtaBilgileri.KirikYumurtaAdet,
                                           HicKirliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.HicKirliYumurtaAdet,
                                           AzKirliYumurtaAdet=yumurtaBilgileri==null ? null : yumurtaBilgileri.AzKirliYumurtaAdet,
                                           OrtaKirliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.OrtaKirliYumurtaAdet,
                                           CokKirliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.CokKirliYumurtaAdet,
                                           HicKanliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.HicKanliYumurtaAdet,
                                           AzKanliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.AzKanliYumurtaAdet,
                                           OrtaKanliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.OrtaKanliYumurtaAdet,
                                           CokKanliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.CokKanliYumurtaAdet,
                                           HicDiskiliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.HicDiskiliYumurtaAdet,
                                           AzDiskiliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.AzDiskiliYumurtaAdet,
                                           OrtaDiskiliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.OrtaDiskiliYumurtaAdet,
                                           CokDiskiliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.CokDiskiliYumurtaAdet,
                                       },
                                 };
                            }
                            else
                            {
                                return new List<KumesLogYeni>
                                {
                                    //new KumesLog_TempYeni { KumesId = kumesId,
                                    //    DonemId=1,
                                    //    KayitZamani=DateTime.Now,
                                    //    YumurtaAdet=yumurtaBilgileri==null ? null :yumurtaBilgileri.HicKirliYumurtaAdet,
                                    //    SAdet=yumurtaBilgileri==null ? null :yumurtaBilgileri.SAdet,
                                    //    MAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.MAdet,
                                    //    LAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.LAdet,
                                    //    XLAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.XLAdet,
                                    //    AsiriKucukAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.AsiriKucukAdet,
                                    //    KirikYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.KirikYumurtaAdet,
                                    //    HicKirliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.HicKirliYumurtaAdet,
                                    //    AzKirliYumurtaAdet=yumurtaBilgileri==null ? null : yumurtaBilgileri.AzKirliYumurtaAdet,
                                    //    OrtaKirliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.OrtaKirliYumurtaAdet,
                                    //    CokKirliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.CokKirliYumurtaAdet,
                                    //    HicKanliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.HicKanliYumurtaAdet,
                                    //    AzKanliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.AzKanliYumurtaAdet,
                                    //    OrtaKanliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.OrtaKanliYumurtaAdet,
                                    //    CokKanliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.CokKanliYumurtaAdet,
                                    //    HicDiskiliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.HicDiskiliYumurtaAdet,
                                    //    AzDiskiliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.AzDiskiliYumurtaAdet,
                                    //    OrtaDiskiliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.OrtaDiskiliYumurtaAdet,
                                    //    CokDiskiliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.CokDiskiliYumurtaAdet,
                                    //},
                                };
                            }
                        }
                        else
                        {
                            return new List<KumesLogYeni>
                            {
                                new KumesLogYeni { KumesId = kumesId,
                                    DonemId=1,
                                    KayitZamani=DateTime.Now,
                                YumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.HicKirliYumurtaAdet,
                                SAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.SAdet,
                                MAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.MAdet,
                                LAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.LAdet,
                                XLAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.XLAdet,
                                AsiriKucukAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.AsiriKucukAdet,
                                KirikYumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.KirikYumurtaAdet,
                                HicKirliYumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.HicKirliYumurtaAdet,
                                AzKirliYumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.AzKirliYumurtaAdet,
                                OrtaKirliYumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.OrtaKirliYumurtaAdet,
                                CokKirliYumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.CokKirliYumurtaAdet,
                                HicKanliYumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.HicKanliYumurtaAdet,
                                AzKanliYumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.AzKanliYumurtaAdet,
                                OrtaKanliYumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.OrtaKanliYumurtaAdet,
                                CokKanliYumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.CokKanliYumurtaAdet,
                                HicDiskiliYumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.HicDiskiliYumurtaAdet,
                                AzDiskiliYumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.AzDiskiliYumurtaAdet,
                                OrtaDiskiliYumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.OrtaDiskiliYumurtaAdet,
                                CokDiskiliYumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.CokDiskiliYumurtaAdet,
                                },
                            };
                        }
                    }
                    else
                    {
                        return new List<KumesLogYeni>
                        {
                            new KumesLogYeni { KumesId = kumesId,
                                DonemId=1,
                                KayitZamani=DateTime.Now,
                                YumurtaAdet=yumurtaBilgileri==null ? null :yumurtaBilgileri.KumesYumurtaAdet,
                                SAdet=yumurtaBilgileri==null ? null :yumurtaBilgileri.SAdet,
                                MAdet = yumurtaBilgileri==null ? null :yumurtaBilgileri.MAdet,
                                LAdet = yumurtaBilgileri==null ? null :yumurtaBilgileri.LAdet,
                                XLAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.XLAdet,
                                AsiriKucukAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.AsiriKucukAdet,
                                KirikYumurtaAdet = yumurtaBilgileri==null ? null :yumurtaBilgileri.KirikYumurtaAdet,
                                HicKirliYumurtaAdet = yumurtaBilgileri==null ? null :yumurtaBilgileri.HicKirliYumurtaAdet,
                                AzKirliYumurtaAdet=yumurtaBilgileri==null ? null : yumurtaBilgileri.AzKirliYumurtaAdet,
                                OrtaKirliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.OrtaKirliYumurtaAdet,
                                CokKirliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.CokKirliYumurtaAdet,
                                HicKanliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.HicKanliYumurtaAdet,
                                AzKanliYumurtaAdet = yumurtaBilgileri==null ? null :yumurtaBilgileri.AzKanliYumurtaAdet,
                                OrtaKanliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.OrtaKanliYumurtaAdet,
                                CokKanliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.CokKanliYumurtaAdet,
                                HicDiskiliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.HicDiskiliYumurtaAdet,
                                AzDiskiliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.AzDiskiliYumurtaAdet,
                                OrtaDiskiliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.OrtaDiskiliYumurtaAdet,
                                CokDiskiliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.CokDiskiliYumurtaAdet,
                            },
                        };
                    }
                }
            }
            catch (Exception)
            {
                return new List<KumesLogYeni>();
            }
        }

        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsert(int kumesId, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;
                List<EtiketLogYeni> etiketLogs = new List<EtiketLogYeni>();
                KumesIzlemeNodeRed? model;
                var gunlukHayvanSayisi = await _cacheService.GetGunlukCanliHayvanSayisiByKumesId(kumesId);
                var yumurtaBilgileri = GetYumurtaBilgiHelper(kumesId);
                if (yumurtaBilgileri != null)
                {
                    var yumurtaAdet = yumurtaBilgileri.KumesYumurtaAdet;
                    var sAdet = yumurtaBilgileri.SAdet;
                    var mAdet = yumurtaBilgileri.MAdet;
                    var lAdet = yumurtaBilgileri.LAdet;
                    var xlAdet = yumurtaBilgileri.XLAdet;
                    var asiriKucukAdet = yumurtaBilgileri.AsiriKucukAdet;
                    var kirik = yumurtaBilgileri.KirikYumurtaAdet;
                    var hicKirli = yumurtaBilgileri.HicKirliYumurtaAdet;
                    var azKirli = yumurtaBilgileri.AzKirliYumurtaAdet;
                    var ortaKirli = yumurtaBilgileri.OrtaKirliYumurtaAdet;
                    var cokKirli = yumurtaBilgileri.CokKirliYumurtaAdet;
                    var hicKanli = yumurtaBilgileri.HicKanliYumurtaAdet;
                    var azKanli = yumurtaBilgileri.AzKanliYumurtaAdet;
                    var ortaKanli = yumurtaBilgileri.OrtaKanliYumurtaAdet;
                    var cokKanli = yumurtaBilgileri.CokKanliYumurtaAdet;
                    var hicDiskili = yumurtaBilgileri.HicDiskiliYumurtaAdet;
                    var azDiskili = yumurtaBilgileri.AzDiskiliYumurtaAdet;
                    var ortaDiskili = yumurtaBilgileri.OrtaDiskiliYumurtaAdet;
                    var cokDiskili = yumurtaBilgileri.CokDiskiliYumurtaAdet;
                    var hayvanBasiYumurtaAdet = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.KumesYumurtaAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiSAdet = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.SAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiMAdet = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.MAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiLAdet = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.LAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiXlAdet = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.XLAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiAsiriKucukAdet = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.AsiriKucukAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiKirik = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.KirikYumurtaAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiHicKirli = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.HicKirliYumurtaAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiAzKirli = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.AzKirliYumurtaAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiOrtaKirli = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.OrtaKirliYumurtaAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiCokKirli = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.CokKirliYumurtaAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiHicKanli = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.HicKanliYumurtaAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiAzKanli = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.AzKanliYumurtaAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiOrtaKanli = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.OrtaKanliYumurtaAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiCokKanli = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.CokKanliYumurtaAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiHicDiskili = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.HicDiskiliYumurtaAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiAzDiskili = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.AzDiskiliYumurtaAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiOrtaDiskili = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.OrtaDiskiliYumurtaAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;
                    var hayvanBasiCokDiskili = gunlukHayvanSayisi == 0 ? 0 : (decimal?)yumurtaBilgileri.CokDiskiliYumurtaAdet.GetValueOrDefault(0) / gunlukHayvanSayisi;

                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 106, KayitZamani = now, Deger = (decimal?)yumurtaAdet });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 107, KayitZamani = now, Deger = (decimal?)sAdet });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 108, KayitZamani = now, Deger = (decimal?)mAdet });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 109, KayitZamani = now, Deger = (decimal?)lAdet });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 110, KayitZamani = now, Deger = (decimal?)xlAdet });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 111, KayitZamani = now, Deger = (decimal?)asiriKucukAdet });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 112, KayitZamani = now, Deger = (decimal?)kirik });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 113, KayitZamani = now, Deger = (decimal?)hicKirli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 114, KayitZamani = now, Deger = (decimal?)azKirli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 115, KayitZamani = now, Deger = (decimal?)ortaKirli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 116, KayitZamani = now, Deger = (decimal?)cokKirli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 117, KayitZamani = now, Deger = (decimal?)hicKanli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 118, KayitZamani = now, Deger = (decimal?)azKanli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 119, KayitZamani = now, Deger = (decimal?)ortaKanli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 120, KayitZamani = now, Deger = (decimal?)cokKanli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 121, KayitZamani = now, Deger = (decimal?)hicDiskili });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 122, KayitZamani = now, Deger = (decimal?)azDiskili });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 123, KayitZamani = now, Deger = (decimal?)ortaDiskili });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 124, KayitZamani = now, Deger = (decimal?)cokDiskili });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 125, KayitZamani = now, Deger = (decimal?)hayvanBasiYumurtaAdet });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 126, KayitZamani = now, Deger = (decimal?)hayvanBasiSAdet });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 127, KayitZamani = now, Deger = (decimal?)hayvanBasiMAdet });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 128, KayitZamani = now, Deger = (decimal?)hayvanBasiLAdet });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 129, KayitZamani = now, Deger = (decimal?)hayvanBasiXlAdet });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 130, KayitZamani = now, Deger = (decimal?)hayvanBasiAsiriKucukAdet });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 131, KayitZamani = now, Deger = (decimal?)hayvanBasiKirik });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 132, KayitZamani = now, Deger = (decimal?)hayvanBasiHicKirli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 133, KayitZamani = now, Deger = (decimal?)hayvanBasiAzKirli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 134, KayitZamani = now, Deger = (decimal?)hayvanBasiOrtaKirli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 135, KayitZamani = now, Deger = (decimal?)hayvanBasiCokKirli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 136, KayitZamani = now, Deger = (decimal?)hayvanBasiHicKanli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 137, KayitZamani = now, Deger = (decimal?)hayvanBasiAzKanli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 138, KayitZamani = now, Deger = (decimal?)hayvanBasiOrtaKanli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 139, KayitZamani = now, Deger = (decimal?)hayvanBasiCokKanli });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 140, KayitZamani = now, Deger = (decimal?)hayvanBasiHicDiskili });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 141, KayitZamani = now, Deger = (decimal?)hayvanBasiAzDiskili });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 142, KayitZamani = now, Deger = (decimal?)hayvanBasiOrtaDiskili });
                    etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 143, KayitZamani = now, Deger = (decimal?)hayvanBasiCokDiskili });
                }
                using (var scope = _serviceProvider.CreateScope())
                {
                    var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    model = await kumesScadaDbContext.KumesIzlemeNodeRed.AsNoTracking().AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani >= now.Date && x.KayitZamani < now.Date.AddDays(1)).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                    if (model == null)
                    {
                        var plcRead = 0;
                        var plcConnected = 0;
                        etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                        etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                        return etiketLogs;
                        //var activeAlarms = await _alarmCacheService.GetAlarmLogsAsync(lokasyonId, DateTime.Now);
                        //List<AlarmLog> alarmLogs = new List<AlarmLog>();
                        //if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Plc_Koptu))
                        //{
                        //    if (plcConnected == 0)
                        //    {
                        //        PlcLogYaz((int)mainClassRahmiyeKumes.KumesTanim + " numaral� k�meste plc ba�lant�s� koptu");
                        //        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Plc_Koptu + kumesId, AlarmEnum = (int?)AlarmLogTipler.Plc_Koptu, Mesaj = AlarmLogTipler.Plc_Koptu.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                        //        _alarmCacheService.ClearMemoryCache(kumesId);
                        //    }
                        //}
                        //else
                        //{
                        //    if (plcConnected == 1)
                        //    {
                        //        PlcLogYaz((int)mainClassRahmiyeKumes.KumesTanim + " numaral� k�meste plc ba�lant�s� sa�land�");
                        //        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Plc_Koptu/*, now*/);
                        //    }
                        //}
                        //await BulkInsertAlarmLogDataAsync(_sqlConnection, alarmLogs);
                    }
                    if (model != null)
                    {
                        var plcRead = model.Durum;
                        var plcConnected = model.Durum;

                        if (PlcVerisiGuncelMi(model, now))
                        {
                            if (model.Veriler != null)
                            {
                                KumesNodeRedModel kumesNodeRedModel = JsonSerializer.Deserialize<KumesNodeRedModel>(model.Veriler);

                                var byteDensity = 0;
                                var pingAverage = 0;
                                var lastPackageLength = 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 71, KayitZamani = DateTime.Now, Deger = (decimal?)byteDensity });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 72, KayitZamani = DateTime.Now, Deger = (decimal?)lastPackageLength });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 73, KayitZamani = DateTime.Now, Deger = (decimal?)pingAverage });

                                if (plcConnected == 0) return etiketLogs;
                                //if (plcRead == 0) return etiketLogs;


                                if (plcConnected == 0) return etiketLogs;

                                var sicaklik_Kumes_Ort = kumesNodeRedModel.Sicaklik_Kumes_Value;
                                var sicaklik_Kumes_Ort_Aktif = kumesNodeRedModel.Sicaklik_Kumes_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 4, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Kumes_Ort });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 12, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Kumes_Ort_Aktif });
                                var sicaklik_Dis = kumesNodeRedModel.Sicaklik_Dis_Value;
                                var sicaklik_Dis_Aktif = kumesNodeRedModel.Sicaklik_Dis_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 5, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Dis });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 13, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Dis_Aktif });
                                var sicaklik_Sensor1 = kumesNodeRedModel.Sicaklik_Sensor_1_Value;
                                var sicaklik_Sensor1_Aktif = kumesNodeRedModel.Sicaklik_Sensor_1_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 6, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor1 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 36, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor1_Aktif });
                                var sicaklik_Sensor2 = kumesNodeRedModel.Sicaklik_Sensor_2_Value;
                                var sicaklik_Sensor2_Aktif = kumesNodeRedModel.Sicaklik_Sensor_2_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 7, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor2 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 37, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor2_Aktif });
                                var sicaklik_Sensor3 = kumesNodeRedModel.Sicaklik_Sensor_3_Value;
                                var sicaklik_Sensor3_Aktif = kumesNodeRedModel.Sicaklik_Sensor_3_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 8, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor3 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 38, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor3_Aktif });
                                var sicaklik_Sensor4 = kumesNodeRedModel.Sicaklik_Sensor_4_Value;
                                var sicaklik_Sensor4_Aktif = kumesNodeRedModel.Sicaklik_Sensor_4_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 9, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor4 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 39, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor4_Aktif });
                                var sicaklik_Sensor5 = kumesNodeRedModel.Sicaklik_Sensor_5_Value;
                                var sicaklik_Sensor5_Aktif = kumesNodeRedModel.Sicaklik_Sensor_5_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 10, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor5 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 40, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor5_Aktif });
                                var sicaklik_Sensor6 = kumesNodeRedModel.Sicaklik_Sensor_6_Value;
                                var sicaklik_Sensor6_Aktif = kumesNodeRedModel.Sicaklik_Sensor_6_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 11, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor6 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 41, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor6_Aktif });
                                var silo1_aktif = kumesNodeRedModel.Yem_Silo_1_Aktif == true ? 1 : 0;
                                var silo1_value = kumesNodeRedModel.Yem_Silo_1_Value;
                                var silo1_varlikoran = kumesNodeRedModel.Yem_Silo_1_VarlikOran;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 27, KayitZamani = DateTime.Now, Deger = (decimal?)silo1_aktif });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 28, KayitZamani = DateTime.Now, Deger = (decimal?)silo1_value });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 29, KayitZamani = DateTime.Now, Deger = (decimal?)silo1_varlikoran });
                                var silo2_aktif = kumesNodeRedModel.Yem_Silo_2_Aktif == true ? 1 : 0; ;
                                var silo2_value = kumesNodeRedModel.Yem_Silo_2_Value;
                                var silo2_varlikoran = kumesNodeRedModel.Yem_Silo_2_VarlikOran;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 30, KayitZamani = DateTime.Now, Deger = (decimal?)silo2_aktif });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 31, KayitZamani = DateTime.Now, Deger = (decimal?)silo2_value });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 32, KayitZamani = DateTime.Now, Deger = (decimal?)silo2_varlikoran });
                                var nem_Dis = kumesNodeRedModel.Nem_Dis_Value;
                                var nem_Dis_Aktif = kumesNodeRedModel.Nem_Dis_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 15, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Dis_Aktif });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 14, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Dis });
                                var nem_Kumes = kumesNodeRedModel.Nem_Kumes_Value;
                                var nem_Kumes_Aktif = kumesNodeRedModel.Nem_Kumes_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 80, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Kumes_Aktif });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 81, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Kumes });
                                var nem_Sensor1 = kumesNodeRedModel.Nem_Sensor_1_Value;
                                var nem_Sensor1_Aktif = kumesNodeRedModel.Nem_Sensor_1_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 46, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor1 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 52, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor1_Aktif });
                                var nem_Sensor2 = kumesNodeRedModel.Nem_Sensor_2_Value;
                                var nem_Sensor2_Aktif = kumesNodeRedModel.Nem_Sensor_2_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 47, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor2 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 53, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor2_Aktif });
                                var nem_Sensor3 = kumesNodeRedModel.Nem_Sensor_3_Value;
                                var nem_Sensor3_Aktif = kumesNodeRedModel.Nem_Sensor_3_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 48, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor3 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 54, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor3_Aktif });
                                var nem_Sensor4 = kumesNodeRedModel.Nem_Sensor_4_Value;
                                var nem_Sensor4_Aktif = kumesNodeRedModel.Nem_Sensor_4_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 49, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor4 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 55, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor4_Aktif });
                                var nem_Sensor5 = kumesNodeRedModel.Nem_Sensor_5_Value;
                                var nem_Sensor5_Aktif = kumesNodeRedModel.Nem_Sensor_5_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 50, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor5 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 56, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor5_Aktif });
                                var nem_Sensor6 = kumesNodeRedModel.Nem_Sensor_6_Value;
                                var nem_Sensor6_Aktif = kumesNodeRedModel.Nem_Sensor_6_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 51, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor6 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 57, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor6_Aktif });
                                var basinc = kumesNodeRedModel.Basinc_Sens_Value;
                                var basinc_aktif = kumesNodeRedModel.Basinc_Sens_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 43, KayitZamani = DateTime.Now, Deger = (decimal?)basinc_aktif });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 42, KayitZamani = DateTime.Now, Deger = (decimal?)basinc });
                                var co2 = kumesNodeRedModel.CO2_Sens_Value;
                                var co2_aktif = kumesNodeRedModel.CO2_Sens_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 45, KayitZamani = DateTime.Now, Deger = (decimal?)co2_aktif });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 44, KayitZamani = DateTime.Now, Deger = (decimal?)co2 });

                                var suVanaAc = kumesNodeRedModel.IcmeSuyu_VanaAc == true ? 1 : 0;
                                var suSayacTuketim = kumesNodeRedModel.IcmeSuyu_Sayac;
                                var suTuketimDun = kumesNodeRedModel.IcmeSuyu_TuketimDun;
                                var suTuketimBugun = kumesNodeRedModel.IcmeSuyu_TuketimBugun;
                                var vanaState = kumesNodeRedModel.IcmeSuyu_VanaState;
                                var vana_Aktif = kumesNodeRedModel.IcmeSuyu_SayacAktif == true ? 1 : 0;
                                var vana_Mode = kumesNodeRedModel.IcmeSuyu_ModeVana;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 74, KayitZamani = DateTime.Now, Deger = (decimal?)suVanaAc });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 75, KayitZamani = DateTime.Now, Deger = (decimal?)suTuketimDun });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 76, KayitZamani = DateTime.Now, Deger = (decimal?)suSayacTuketim });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 16, KayitZamani = DateTime.Now, Deger = (decimal?)suTuketimBugun });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 24, KayitZamani = DateTime.Now, Deger = (decimal?)vana_Mode });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 25, KayitZamani = DateTime.Now, Deger = (decimal?)vana_Aktif });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 26, KayitZamani = DateTime.Now, Deger = (decimal?)vanaState });
                                var yemTuketim = kumesNodeRedModel.Yem_TuketimBugun;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 17, KayitZamani = DateTime.Now, Deger = (decimal?)yemTuketim });
                                var pedPompaRun = kumesNodeRedModel.Ped_PompaRun == true ? 1 : 0;
                                var pedPompaDurum = kumesNodeRedModel.Ped_PompaDurum;
                                var pedPompaMode = kumesNodeRedModel.Ped_ModePompa;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 33, KayitZamani = DateTime.Now, Deger = (decimal?)pedPompaRun });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 34, KayitZamani = DateTime.Now, Deger = (decimal?)pedPompaDurum });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 60, KayitZamani = DateTime.Now, Deger = (decimal?)pedPompaMode });
                                var pedKapakAc = kumesNodeRedModel.Ped_PedKapakAc == true ? 1 : 0;
                                var pedKapakDurum = kumesNodeRedModel.Ped_PedKapakDurum;
                                var pedKapakMode = kumesNodeRedModel.Ped_ModeKapak;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 79, KayitZamani = DateTime.Now, Deger = (decimal?)pedKapakAc });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 35, KayitZamani = DateTime.Now, Deger = (decimal?)pedKapakDurum });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 61, KayitZamani = DateTime.Now, Deger = (decimal?)pedKapakMode });
                                var aydinlatma_Ac = kumesNodeRedModel.Aydinlatma_Ac == true ? 1 : 0;
                                var aydinlatma_Oran = kumesNodeRedModel.Aydinlatma_Oran;
                                var aydinlatma_Durum = kumesNodeRedModel.Aydinlatma_Durum;
                                var aydinlatma_Mode = kumesNodeRedModel.Aydinlatma_Mode;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 77, KayitZamani = DateTime.Now, Deger = (decimal?)aydinlatma_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 78, KayitZamani = DateTime.Now, Deger = (decimal?)aydinlatma_Oran });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 59, KayitZamani = DateTime.Now, Deger = (decimal?)aydinlatma_Durum });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 58, KayitZamani = DateTime.Now, Deger = (decimal?)aydinlatma_Mode });
                                var fanMode = kumesNodeRedModel.Fan_Mode;
                                var fanKademeMin = kumesNodeRedModel.Fan_MinHavalandirma == true ? 1 : 0;
                                var fanKademe1 = kumesNodeRedModel.Fan_Kademe_1 == true ? 1 : 0;
                                var fanKademe2 = kumesNodeRedModel.Fan_Kademe_2 == true ? 1 : 0;
                                var fanKademe3 = kumesNodeRedModel.Fan_Kademe_3 == true ? 1 : 0;
                                var fanKademe4 = kumesNodeRedModel.Fan_Kademe_4 == true ? 1 : 0;
                                var fanKademe5 = kumesNodeRedModel.Fan_Kademe_5 == true ? 1 : 0;
                                var fanKademe6 = kumesNodeRedModel.Fan_Kademe_6 == true ? 1 : 0;
                                var fanKademe7 = kumesNodeRedModel.Fan_Kademe_7 == true ? 1 : 0;
                                var fanKademe8 = kumesNodeRedModel.Fan_Kademe_8 == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 18, KayitZamani = DateTime.Now, Deger = (decimal?)fanMode });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 62, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademeMin });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 63, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe1 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 64, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe2 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 65, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe3 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 66, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe4 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 67, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe5 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 68, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe6 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 69, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe7 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 70, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe8 });
                                var yemlemeState1 = kumesNodeRedModel.Yemleme_1_Durum;
                                var yemlemeState2 = kumesNodeRedModel.Yemleme_2_Durum;
                                var yemlemeState3 = kumesNodeRedModel.Yemleme_3_Durum;
                                var yemlemeState4 = kumesNodeRedModel.Yemleme_4_Durum;
                                var yemlemeState5 = kumesNodeRedModel.Yemleme_5_Durum;
                                var yemlemeState6 = kumesNodeRedModel.Yemleme_6_Durum;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 82, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState1 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 83, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState2 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 84, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState3 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 85, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState4 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 86, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState5 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 87, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState6 });
                                var yemlemeBolumState1 = kumesNodeRedModel.Yemleme_Bolum_1_Durum;
                                var yemlemeBolumState2 = kumesNodeRedModel.Yemleme_Bolum_2_Durum;
                                var yemlemeBolumState3 = kumesNodeRedModel.Yemleme_Bolum_3_Durum;
                                var yemlemeBolumState4 = kumesNodeRedModel.Yemleme_Bolum_4_Durum;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 88, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumState1 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 89, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumState2 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 90, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumState3 });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 91, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumState4 });
                                var yemlemeBaslamaZamani = TimeSpan.FromMilliseconds((double)kumesNodeRedModel.Yemleme_BaslamaZamani).TotalSeconds;
                                var yemlemeBolumAktuelSure = TimeSpan.FromMilliseconds((double)kumesNodeRedModel.Yemleme_BolumAktuelSure).TotalSeconds /*mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Yemleme.BolumAktuelSure.TotalSeconds*/;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 92, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBaslamaZamani });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 93, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumAktuelSure });
                                var buffelSol_CmdBit = kumesNodeRedModel.Buffel_Sol_Cmd_CmdBit;
                                var buffelSol_RprMotorState = kumesNodeRedModel.Buffel_Sol_Rpr_MotorState;
                                var buffelSol_RprDurum = kumesNodeRedModel.Buffel_Sol_Rpr_Durum;
                                var buffelSol_RprSwitchKapali = kumesNodeRedModel.Buffel_Sol_Rpr_Switch_Kapali == true ? 1 : 0;
                                var buffelSol_RprSwitchAcik = kumesNodeRedModel.Buffel_Sol_Rpr_Switch_Acik == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 94, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_CmdBit });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 95, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_RprMotorState });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 96, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_RprDurum });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 97, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_RprSwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 98, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_RprSwitchAcik });
                                var buffelSag_CmdBit = kumesNodeRedModel.Buffel_Sag_Cmd_CmdBit;
                                var buffelSag_RprMotorState = kumesNodeRedModel.Buffel_Sag_Rpr_MotorState;
                                var buffelSag_RprDurum = kumesNodeRedModel.Buffel_Sag_Rpr_Durum;
                                var buffelSag_RprSwitchKapali = kumesNodeRedModel.Buffel_Sag_Rpr_Switch_Kapali == true ? 1 : 0;
                                var buffelSag_RprSwitchAcik = kumesNodeRedModel.Buffel_Sag_Rpr_Switch_Acik == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 99, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_CmdBit });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 100, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_RprMotorState });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 101, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_RprDurum });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 102, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_RprSwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 103, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_RprSwitchAcik });

                                var Buffel_2_Sol_State_Auto = kumesNodeRedModel.Buffel_2_Sol_State_Auto == true ? 1 : 0; ;
                                var Buffel_2_Sol_State_Ac_Kontaktor = kumesNodeRedModel.Buffel_2_Sol_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var Buffel_2_Sol_State_Kapat_Kontaktor = kumesNodeRedModel.Buffel_2_Sol_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var Buffel_2_Sol_State_SwitchAcik = kumesNodeRedModel.Buffel_2_Sol_State_SwitchAcik == true ? 1 : 0;
                                var Buffel_2_Sol_State_SwitchKapali = kumesNodeRedModel.Buffel_2_Sol_State_SwitchKapali == true ? 1 : 0;
                                var Buffel_2_Sol_State_Durum = kumesNodeRedModel.Buffel_2_Sol_State_Durum;
                                var Buffel_2_Sol_Cmd_Ac = kumesNodeRedModel.Buffel_2_Sol_Cmd_Ac == true ? 1 : 0;
                                var Buffel_2_Sol_Cmd_Kapat = kumesNodeRedModel.Buffel_2_Sol_Cmd_Kapat == true ? 1 : 0;
                                var Buffel_2_Sol_Cmd_CmdBit = kumesNodeRedModel.Buffel_2_Sol_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 569, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_State_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 570, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 571, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 572, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 573, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 574, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 575, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 576, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 577, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_Cmd_CmdBit.GetValueOrDefault(0) });

                                var Buffel_2_Sag_State_Auto = kumesNodeRedModel.Buffel_2_Sag_State_Auto == true ? 1 : 0; ;
                                var Buffel_2_Sag_State_Ac_Kontaktor = kumesNodeRedModel.Buffel_2_Sag_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var Buffel_2_Sag_State_Kapat_Kontaktor = kumesNodeRedModel.Buffel_2_Sag_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var Buffel_2_Sag_State_SwitchAcik = kumesNodeRedModel.Buffel_2_Sag_State_SwitchAcik == true ? 1 : 0;
                                var Buffel_2_Sag_State_SwitchKapali = kumesNodeRedModel.Buffel_2_Sag_State_SwitchKapali == true ? 1 : 0;
                                var Buffel_2_Sag_State_Durum = kumesNodeRedModel.Buffel_2_Sag_State_Durum;
                                var Buffel_2_Sag_Cmd_Ac = kumesNodeRedModel.Buffel_2_Sag_Cmd_Ac == true ? 1 : 0;
                                var Buffel_2_Sag_Cmd_Kapat = kumesNodeRedModel.Buffel_2_Sag_Cmd_Kapat == true ? 1 : 0;
                                var Buffel_2_Sag_Cmd_CmdBit = kumesNodeRedModel.Buffel_2_Sag_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 578, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_State_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 579, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 580, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 581, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 582, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 583, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 584, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 585, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 586, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_1_State_Auto = kumesNodeRedModel.PedKapak_On_1_State_Auto == true ? 1 : 0; ;
                                var PedKapak_On_1_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_On_1_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_1_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_On_1_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_1_State_SwitchAcik = kumesNodeRedModel.PedKapak_On_1_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_On_1_State_SwitchKapali = kumesNodeRedModel.PedKapak_On_1_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_On_1_State_Durum = kumesNodeRedModel.PedKapak_On_1_State_Durum;
                                var PedKapak_On_1_Cmd_Ac = kumesNodeRedModel.PedKapak_On_1_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_On_1_Cmd_Kapat = kumesNodeRedModel.PedKapak_On_1_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_On_1_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_1_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 587, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_State_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 588, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 589, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 590, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 591, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 592, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 593, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 594, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 595, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_2_State_Auto = kumesNodeRedModel.PedKapak_On_2_State_Auto == true ? 1 : 0; ;
                                var PedKapak_On_2_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_On_2_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_2_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_On_2_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_2_State_SwitchAcik = kumesNodeRedModel.PedKapak_On_2_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_On_2_State_SwitchKapali = kumesNodeRedModel.PedKapak_On_2_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_On_2_State_Durum = kumesNodeRedModel.PedKapak_On_2_State_Durum;
                                var PedKapak_On_2_Cmd_Ac = kumesNodeRedModel.PedKapak_On_2_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_On_2_Cmd_Kapat = kumesNodeRedModel.PedKapak_On_2_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_On_2_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_2_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 596, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_State_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 597, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 598, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 599, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 600, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 601, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 602, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 603, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 604, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_3_State_Auto = kumesNodeRedModel.PedKapak_On_3_State_Auto == true ? 1 : 0; ;
                                var PedKapak_On_3_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_On_3_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_3_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_On_3_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_3_State_SwitchAcik = kumesNodeRedModel.PedKapak_On_3_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_On_3_State_SwitchKapali = kumesNodeRedModel.PedKapak_On_3_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_On_3_State_Durum = kumesNodeRedModel.PedKapak_On_3_State_Durum;
                                var PedKapak_On_3_Cmd_Ac = kumesNodeRedModel.PedKapak_On_3_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_On_3_Cmd_Kapat = kumesNodeRedModel.PedKapak_On_3_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_On_3_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_3_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 605, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_State_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 606, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 607, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 608, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 609, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 610, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 611, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 612, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 613, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_4_State_Auto = kumesNodeRedModel.PedKapak_On_4_State_Auto == true ? 1 : 0; ;
                                var PedKapak_On_4_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_On_4_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_4_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_On_4_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_4_State_SwitchAcik = kumesNodeRedModel.PedKapak_On_4_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_On_4_State_SwitchKapali = kumesNodeRedModel.PedKapak_On_4_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_On_4_State_Durum = kumesNodeRedModel.PedKapak_On_4_State_Durum;
                                var PedKapak_On_4_Cmd_Ac = kumesNodeRedModel.PedKapak_On_4_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_On_4_Cmd_Kapat = kumesNodeRedModel.PedKapak_On_4_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_On_4_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_4_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 614, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_State_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 615, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 616, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 617, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 618, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 619, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 620, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 621, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 622, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_1_State_Auto = kumesNodeRedModel.PedKapak_Arka_1_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Arka_1_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Arka_1_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_1_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Arka_1_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_1_State_SwitchAcik = kumesNodeRedModel.PedKapak_Arka_1_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Arka_1_State_SwitchKapali = kumesNodeRedModel.PedKapak_Arka_1_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Arka_1_State_Durum = kumesNodeRedModel.PedKapak_Arka_1_State_Durum;
                                var PedKapak_Arka_1_Cmd_Ac = kumesNodeRedModel.PedKapak_Arka_1_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Arka_1_Cmd_Kapat = kumesNodeRedModel.PedKapak_Arka_1_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Arka_1_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_1_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 623, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_State_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 624, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 625, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 626, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 627, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 628, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 629, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 630, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 631, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_2_State_Auto = kumesNodeRedModel.PedKapak_Arka_2_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Arka_2_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Arka_2_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_2_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Arka_2_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_2_State_SwitchAcik = kumesNodeRedModel.PedKapak_Arka_2_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Arka_2_State_SwitchKapali = kumesNodeRedModel.PedKapak_Arka_2_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Arka_2_State_Durum = kumesNodeRedModel.PedKapak_Arka_2_State_Durum;
                                var PedKapak_Arka_2_Cmd_Ac = kumesNodeRedModel.PedKapak_Arka_2_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Arka_2_Cmd_Kapat = kumesNodeRedModel.PedKapak_Arka_2_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Arka_2_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_2_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 632, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_State_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 633, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 634, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 635, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 636, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 637, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 638, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 639, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 640, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_3_State_Auto = kumesNodeRedModel.PedKapak_Arka_3_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Arka_3_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Arka_3_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_3_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Arka_3_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_3_State_SwitchAcik = kumesNodeRedModel.PedKapak_Arka_3_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Arka_3_State_SwitchKapali = kumesNodeRedModel.PedKapak_Arka_3_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Arka_3_State_Durum = kumesNodeRedModel.PedKapak_Arka_3_State_Durum;
                                var PedKapak_Arka_3_Cmd_Ac = kumesNodeRedModel.PedKapak_Arka_3_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Arka_3_Cmd_Kapat = kumesNodeRedModel.PedKapak_Arka_3_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Arka_3_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_3_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 641, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_State_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 642, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 643, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 644, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 645, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 646, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 647, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 648, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 649, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_4_State_Auto = kumesNodeRedModel.PedKapak_Arka_4_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Arka_4_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Arka_4_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_4_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Arka_4_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_4_State_SwitchAcik = kumesNodeRedModel.PedKapak_Arka_4_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Arka_4_State_SwitchKapali = kumesNodeRedModel.PedKapak_Arka_4_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Arka_4_State_Durum = kumesNodeRedModel.PedKapak_Arka_4_State_Durum;
                                var PedKapak_Arka_4_Cmd_Ac = kumesNodeRedModel.PedKapak_Arka_4_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Arka_4_Cmd_Kapat = kumesNodeRedModel.PedKapak_Arka_4_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Arka_4_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_4_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 650, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_State_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 651, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 652, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 653, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 654, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 655, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 656, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 657, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 658, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Sol_1_State_Auto = kumesNodeRedModel.PedKapak_Sol_1_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Sol_1_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Sol_1_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sol_1_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Sol_1_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sol_1_State_SwitchAcik = kumesNodeRedModel.PedKapak_Sol_1_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Sol_1_State_SwitchKapali = kumesNodeRedModel.PedKapak_Sol_1_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Sol_1_State_Durum = kumesNodeRedModel.PedKapak_Sol_1_State_Durum;
                                var PedKapak_Sol_1_Cmd_Ac = kumesNodeRedModel.PedKapak_Sol_1_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Sol_1_Cmd_Kapat = kumesNodeRedModel.PedKapak_Sol_1_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Sol_1_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Sol_1_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 659, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_State_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 660, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 661, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 662, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 663, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 664, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 665, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 666, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 667, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Sol_2_State_Auto = kumesNodeRedModel.PedKapak_Sol_2_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Sol_2_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Sol_2_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sol_2_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Sol_2_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sol_2_State_SwitchAcik = kumesNodeRedModel.PedKapak_Sol_2_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Sol_2_State_SwitchKapali = kumesNodeRedModel.PedKapak_Sol_2_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Sol_2_State_Durum = kumesNodeRedModel.PedKapak_Sol_2_State_Durum;
                                var PedKapak_Sol_2_Cmd_Ac = kumesNodeRedModel.PedKapak_Sol_2_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Sol_2_Cmd_Kapat = kumesNodeRedModel.PedKapak_Sol_2_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Sol_2_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Sol_2_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 668, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_State_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 669, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 670, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 671, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 672, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 673, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 674, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 675, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 676, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Sag_1_State_Auto = kumesNodeRedModel.PedKapak_Sag_1_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Sag_1_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Sag_1_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sag_1_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Sag_1_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sag_1_State_SwitchAcik = kumesNodeRedModel.PedKapak_Sag_1_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Sag_1_State_SwitchKapali = kumesNodeRedModel.PedKapak_Sag_1_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Sag_1_State_Durum = kumesNodeRedModel.PedKapak_Sag_1_State_Durum;
                                var PedKapak_Sag_1_Cmd_Ac = kumesNodeRedModel.PedKapak_Sag_1_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Sag_1_Cmd_Kapat = kumesNodeRedModel.PedKapak_Sag_1_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Sag_1_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Sag_1_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 677, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_State_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 678, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 679, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 680, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 681, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 682, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 683, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 684, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 685, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Sag_2_State_Auto = kumesNodeRedModel.PedKapak_Sag_2_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Sag_2_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Sag_2_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sag_2_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Sag_2_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sag_2_State_SwitchAcik = kumesNodeRedModel.PedKapak_Sag_2_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Sag_2_State_SwitchKapali = kumesNodeRedModel.PedKapak_Sag_2_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Sag_2_State_Durum = kumesNodeRedModel.PedKapak_Sag_2_State_Durum;
                                var PedKapak_Sag_2_Cmd_Ac = kumesNodeRedModel.PedKapak_Sag_2_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Sag_2_Cmd_Kapat = kumesNodeRedModel.PedKapak_Sag_2_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Sag_2_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Sag_2_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 686, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_State_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 687, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 688, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 689, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 690, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 691, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 692, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 693, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 694, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_Cmd_CmdBit.GetValueOrDefault(0) });

                                var BacaFani_1_Auto = kumesNodeRedModel.BacaFani_1_Auto == true ? 1 : 0; ;
                                var BacaFani_1_Calisiyor = kumesNodeRedModel.BacaFani_1_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_1_Cmd_Ac = kumesNodeRedModel.BacaFani_1_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_1_Cmd_Kapat = kumesNodeRedModel.BacaFani_1_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_1_Cmd_CmdBit = kumesNodeRedModel.BacaFani_1_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 695, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_1_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 696, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_1_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 697, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_1_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 698, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_1_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 699, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_1_Cmd_CmdBit });

                                var BacaFani_2_Auto = kumesNodeRedModel.BacaFani_2_Auto == true ? 1 : 0; ;
                                var BacaFani_2_Calisiyor = kumesNodeRedModel.BacaFani_2_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_2_Cmd_Ac = kumesNodeRedModel.BacaFani_2_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_2_Cmd_Kapat = kumesNodeRedModel.BacaFani_2_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_2_Cmd_CmdBit = kumesNodeRedModel.BacaFani_2_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 700, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_2_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 701, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_2_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 702, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_2_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 703, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_2_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 704, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_2_Cmd_CmdBit });

                                var BacaFani_3_Auto = kumesNodeRedModel.BacaFani_3_Auto == true ? 1 : 0; ;
                                var BacaFani_3_Calisiyor = kumesNodeRedModel.BacaFani_3_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_3_Cmd_Ac = kumesNodeRedModel.BacaFani_3_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_3_Cmd_Kapat = kumesNodeRedModel.BacaFani_3_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_3_Cmd_CmdBit = kumesNodeRedModel.BacaFani_3_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 705, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_3_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 706, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_3_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 707, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_3_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 708, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_3_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 709, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_3_Cmd_CmdBit });

                                var BacaFani_4_Auto = kumesNodeRedModel.BacaFani_4_Auto == true ? 1 : 0; ;
                                var BacaFani_4_Calisiyor = kumesNodeRedModel.BacaFani_4_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_4_Cmd_Ac = kumesNodeRedModel.BacaFani_4_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_4_Cmd_Kapat = kumesNodeRedModel.BacaFani_4_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_4_Cmd_CmdBit = kumesNodeRedModel.BacaFani_4_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 710, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_4_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 711, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_4_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 712, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_4_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 713, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_4_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 714, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_4_Cmd_CmdBit });

                                var BacaFani_5_Auto = kumesNodeRedModel.BacaFani_5_Auto == true ? 1 : 0; ;
                                var BacaFani_5_Calisiyor = kumesNodeRedModel.BacaFani_5_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_5_Cmd_Ac = kumesNodeRedModel.BacaFani_5_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_5_Cmd_Kapat = kumesNodeRedModel.BacaFani_5_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_5_Cmd_CmdBit = kumesNodeRedModel.BacaFani_5_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 715, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_5_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 716, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_5_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 717, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_5_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 718, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_5_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 719, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_5_Cmd_CmdBit });

                                var BacaFani_6_Auto = kumesNodeRedModel.BacaFani_6_Auto == true ? 1 : 0; ;
                                var BacaFani_6_Calisiyor = kumesNodeRedModel.BacaFani_6_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_6_Cmd_Ac = kumesNodeRedModel.BacaFani_6_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_6_Cmd_Kapat = kumesNodeRedModel.BacaFani_6_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_6_Cmd_CmdBit = kumesNodeRedModel.BacaFani_6_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 720, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_6_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 721, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_6_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 722, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_6_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 723, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_6_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 724, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_6_Cmd_CmdBit });

                                var BacaFani_7_Auto = kumesNodeRedModel.BacaFani_7_Auto == true ? 1 : 0; ;
                                var BacaFani_7_Calisiyor = kumesNodeRedModel.BacaFani_7_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_7_Cmd_Ac = kumesNodeRedModel.BacaFani_7_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_7_Cmd_Kapat = kumesNodeRedModel.BacaFani_7_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_7_Cmd_CmdBit = kumesNodeRedModel.BacaFani_7_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 725, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_7_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 726, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_7_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 727, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_7_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 728, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_7_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 729, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_7_Cmd_CmdBit });

                                var BacaFani_8_Auto = kumesNodeRedModel.BacaFani_8_Auto == true ? 1 : 0; ;
                                var BacaFani_8_Calisiyor = kumesNodeRedModel.BacaFani_8_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_8_Cmd_Ac = kumesNodeRedModel.BacaFani_8_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_8_Cmd_Kapat = kumesNodeRedModel.BacaFani_8_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_8_Cmd_CmdBit = kumesNodeRedModel.BacaFani_8_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 730, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_8_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 731, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_8_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 732, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_8_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 733, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_8_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 734, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_8_Cmd_CmdBit });

                                var BacaFani_9_Auto = kumesNodeRedModel.BacaFani_9_Auto == true ? 1 : 0; ;
                                var BacaFani_9_Calisiyor = kumesNodeRedModel.BacaFani_9_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_9_Cmd_Ac = kumesNodeRedModel.BacaFani_9_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_9_Cmd_Kapat = kumesNodeRedModel.BacaFani_9_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_9_Cmd_CmdBit = kumesNodeRedModel.BacaFani_9_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 735, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_9_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 736, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_9_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 737, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_9_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 738, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_9_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 739, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_9_Cmd_CmdBit });

                                var BacaFani_10_Auto = kumesNodeRedModel.BacaFani_10_Auto == true ? 1 : 0; ;
                                var BacaFani_10_Calisiyor = kumesNodeRedModel.BacaFani_10_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_10_Cmd_Ac = kumesNodeRedModel.BacaFani_10_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_10_Cmd_Kapat = kumesNodeRedModel.BacaFani_10_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_10_Cmd_CmdBit = kumesNodeRedModel.BacaFani_10_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 740, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_10_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 741, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_10_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 742, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_10_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 743, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_10_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 744, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_10_Cmd_CmdBit });

                                var BacaFani_11_Auto = kumesNodeRedModel.BacaFani_11_Auto == true ? 1 : 0; ;
                                var BacaFani_11_Calisiyor = kumesNodeRedModel.BacaFani_11_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_11_Cmd_Ac = kumesNodeRedModel.BacaFani_11_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_11_Cmd_Kapat = kumesNodeRedModel.BacaFani_11_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_11_Cmd_CmdBit = kumesNodeRedModel.BacaFani_11_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 745, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_11_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 746, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_11_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 747, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_11_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 748, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_11_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 749, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_11_Cmd_CmdBit });

                                var BacaFani_12_Auto = kumesNodeRedModel.BacaFani_12_Auto == true ? 1 : 0; ;
                                var BacaFani_12_Calisiyor = kumesNodeRedModel.BacaFani_12_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_12_Cmd_Ac = kumesNodeRedModel.BacaFani_12_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_12_Cmd_Kapat = kumesNodeRedModel.BacaFani_12_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_12_Cmd_CmdBit = kumesNodeRedModel.BacaFani_12_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 750, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_12_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 751, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_12_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 752, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_12_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 753, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_12_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 754, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_12_Cmd_CmdBit });

                                var BacaFani_13_Auto = kumesNodeRedModel.BacaFani_13_Auto == true ? 1 : 0; ;
                                var BacaFani_13_Calisiyor = kumesNodeRedModel.BacaFani_13_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_13_Cmd_Ac = kumesNodeRedModel.BacaFani_13_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_13_Cmd_Kapat = kumesNodeRedModel.BacaFani_13_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_13_Cmd_CmdBit = kumesNodeRedModel.BacaFani_13_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 755, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_13_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 756, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_13_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 757, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_13_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 758, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_13_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 759, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_13_Cmd_CmdBit });

                                var BacaFani_14_Auto = kumesNodeRedModel.BacaFani_14_Auto == true ? 1 : 0; ;
                                var BacaFani_14_Calisiyor = kumesNodeRedModel.BacaFani_14_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_14_Cmd_Ac = kumesNodeRedModel.BacaFani_14_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_14_Cmd_Kapat = kumesNodeRedModel.BacaFani_14_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_14_Cmd_CmdBit = kumesNodeRedModel.BacaFani_14_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 760, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_14_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 761, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_14_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 762, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_14_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 763, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_14_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 764, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_14_Cmd_CmdBit });

                                var BacaFani_15_Auto = kumesNodeRedModel.BacaFani_15_Auto == true ? 1 : 0; ;
                                var BacaFani_15_Calisiyor = kumesNodeRedModel.BacaFani_15_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_15_Cmd_Ac = kumesNodeRedModel.BacaFani_15_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_15_Cmd_Kapat = kumesNodeRedModel.BacaFani_15_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_15_Cmd_CmdBit = kumesNodeRedModel.BacaFani_15_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 765, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_15_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 766, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_15_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 767, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_15_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 768, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_15_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 769, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_15_Cmd_CmdBit });

                                var BacaFani_16_Auto = kumesNodeRedModel.BacaFani_16_Auto == true ? 1 : 0; ;
                                var BacaFani_16_Calisiyor = kumesNodeRedModel.BacaFani_16_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_16_Cmd_Ac = kumesNodeRedModel.BacaFani_16_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_16_Cmd_Kapat = kumesNodeRedModel.BacaFani_16_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_16_Cmd_CmdBit = kumesNodeRedModel.BacaFani_16_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 770, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_16_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 771, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_16_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 772, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_16_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 773, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_16_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 774, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_16_Cmd_CmdBit });

                                var BacaFani_17_Auto = kumesNodeRedModel.BacaFani_17_Auto == true ? 1 : 0; ;
                                var BacaFani_17_Calisiyor = kumesNodeRedModel.BacaFani_17_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_17_Cmd_Ac = kumesNodeRedModel.BacaFani_17_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_17_Cmd_Kapat = kumesNodeRedModel.BacaFani_17_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_17_Cmd_CmdBit = kumesNodeRedModel.BacaFani_17_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 775, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_17_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 776, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_17_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 777, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_17_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 778, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_17_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 779, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_17_Cmd_CmdBit });

                                var BacaFani_18_Auto = kumesNodeRedModel.BacaFani_18_Auto == true ? 1 : 0; ;
                                var BacaFani_18_Calisiyor = kumesNodeRedModel.BacaFani_18_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_18_Cmd_Ac = kumesNodeRedModel.BacaFani_18_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_18_Cmd_Kapat = kumesNodeRedModel.BacaFani_18_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_18_Cmd_CmdBit = kumesNodeRedModel.BacaFani_18_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 780, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_18_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 781, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_18_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 782, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_18_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 783, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_18_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 784, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_18_Cmd_CmdBit });

                                var BacaFani_19_Auto = kumesNodeRedModel.BacaFani_19_Auto == true ? 1 : 0; ;
                                var BacaFani_19_Calisiyor = kumesNodeRedModel.BacaFani_19_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_19_Cmd_Ac = kumesNodeRedModel.BacaFani_19_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_19_Cmd_Kapat = kumesNodeRedModel.BacaFani_19_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_19_Cmd_CmdBit = kumesNodeRedModel.BacaFani_19_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 785, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_19_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 786, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_19_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 787, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_19_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 788, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_19_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 789, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_19_Cmd_CmdBit });

                                var BacaFani_20_Auto = kumesNodeRedModel.BacaFani_20_Auto == true ? 1 : 0; ;
                                var BacaFani_20_Calisiyor = kumesNodeRedModel.BacaFani_20_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_20_Cmd_Ac = kumesNodeRedModel.BacaFani_20_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_20_Cmd_Kapat = kumesNodeRedModel.BacaFani_20_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_20_Cmd_CmdBit = kumesNodeRedModel.BacaFani_20_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 790, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_20_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 791, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_20_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 792, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_20_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 793, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_20_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 794, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_20_Cmd_CmdBit });

                                var BacaFani_21_Auto = kumesNodeRedModel.BacaFani_21_Auto == true ? 1 : 0; ;
                                var BacaFani_21_Calisiyor = kumesNodeRedModel.BacaFani_21_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_21_Cmd_Ac = kumesNodeRedModel.BacaFani_21_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_21_Cmd_Kapat = kumesNodeRedModel.BacaFani_21_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_21_Cmd_CmdBit = kumesNodeRedModel.BacaFani_21_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 795, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_21_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 796, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_21_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 797, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_21_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 798, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_21_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 799, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_21_Cmd_CmdBit });

                                var BacaFani_22_Auto = kumesNodeRedModel.BacaFani_22_Auto == true ? 1 : 0; ;
                                var BacaFani_22_Calisiyor = kumesNodeRedModel.BacaFani_22_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_22_Cmd_Ac = kumesNodeRedModel.BacaFani_22_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_22_Cmd_Kapat = kumesNodeRedModel.BacaFani_22_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_22_Cmd_CmdBit = kumesNodeRedModel.BacaFani_22_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 800, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_22_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 801, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_22_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 802, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_22_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 803, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_22_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 804, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_22_Cmd_CmdBit });

                                var BacaFani_23_Auto = kumesNodeRedModel.BacaFani_23_Auto == true ? 1 : 0; ;
                                var BacaFani_23_Calisiyor = kumesNodeRedModel.BacaFani_23_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_23_Cmd_Ac = kumesNodeRedModel.BacaFani_23_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_23_Cmd_Kapat = kumesNodeRedModel.BacaFani_23_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_23_Cmd_CmdBit = kumesNodeRedModel.BacaFani_23_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 805, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_23_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 806, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_23_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 807, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_23_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 808, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_23_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 809, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_23_Cmd_CmdBit });

                                var BacaFani_24_Auto = kumesNodeRedModel.BacaFani_24_Auto == true ? 1 : 0; ;
                                var BacaFani_24_Calisiyor = kumesNodeRedModel.BacaFani_24_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_24_Cmd_Ac = kumesNodeRedModel.BacaFani_24_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_24_Cmd_Kapat = kumesNodeRedModel.BacaFani_24_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_24_Cmd_CmdBit = kumesNodeRedModel.BacaFani_24_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 810, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_24_Auto });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 811, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_24_Calisiyor });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 812, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_24_Cmd_Ac });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 813, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_24_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 814, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_24_Cmd_CmdBit });

                                var hayvanBasinaSuTuketim = gunlukHayvanSayisi == 0 ? 0 : (kumesNodeRedModel.IcmeSuyu_TuketimBugun * 1000000) / gunlukHayvanSayisi;
                                var hayvanBasinaYemTuketim = gunlukHayvanSayisi == 0 ? 0 : (kumesNodeRedModel.Yem_TuketimBugun * 1000000) / gunlukHayvanSayisi;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 104, KayitZamani = DateTime.Now, Deger = (decimal?)hayvanBasinaSuTuketim });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 105, KayitZamani = DateTime.Now, Deger = (decimal?)hayvanBasinaYemTuketim });
                            }
                            else
                            {
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            }
                        }
                    }
                    return etiketLogs;
                }
            }
            catch (Exception)
            {
                return new List<EtiketLogYeni>();
            }
        }

        private EggActualDbModel? GetYumurtaBilgiHelper(int kumesId)
        {
            return yumurtaBilgileriDict.TryGetValue(kumesId, out var dto) ? dto : null;
        }

        /// <summary>
        /// SAP ZWS_F0017 kantar servisinden bugune ait, SUREC=H ve FINISHED=X olan kayitlarin
        /// NET_VALUE toplamini LGORT -> KumesId eslesmesi uzerinden KumesId bazinda gruplar.
        /// </summary>
        private async Task<Dictionary<int, decimal>> GetSapGunlukYemGirisiByKumesIdAsync(CancellationToken cancellationToken)
        {
            var sonuc = new Dictionary<int, decimal>();
            try
            {
                var bugun = DateTime.Now.Date;
                var kayitlar = await _sapKantarService.GetKantarKayitlariAsync(bugun, bugun, cancellationToken);

                foreach (var kayit in kayitlar)
                {
                    if (!string.Equals(kayit.Surec?.Trim(), "H", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(kayit.Finished?.Trim(), "X", StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.IsNullOrWhiteSpace(kayit.Lgort)) continue;
                    if (!LgortKumesEslesme.TryGetValue(kayit.Lgort.Trim(), out var kumesId)) continue;

                    sonuc.TryGetValue(kumesId, out var mevcutToplam);
                    sonuc[kumesId] = mevcutToplam + kayit.NetValue.GetValueOrDefault(0)/1000;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAP kantar verisi alinamadi");
            }
            return sonuc;
        }

        /// <summary>
        /// Bir onceki gunun son KumesLogYeni kaydindaki Silo1+Silo2 toplamini doner (gun sonu silo seviyesi).
        /// </summary>
        private async Task<decimal> GetDunGunSonuSiloToplamAsync(KumesScadaDbContext kumesScadaDbContext, int kumesId, DateTime bugun, CancellationToken cancellationToken)
        {
            var dunBaslangic = bugun.Date.AddDays(-1);
            var dunSonKayit = await kumesScadaDbContext.KumesLogYeni.AsNoTracking()
                .Where(x => x.KumesId == kumesId && x.KayitZamani >= dunBaslangic && x.KayitZamani < bugun.Date)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (dunSonKayit == null) return 0;
            return dunSonKayit.Silo1.GetValueOrDefault(0) + dunSonKayit.Silo2.GetValueOrDefault(0);
        }
        private static int? ReadIntNullable(SqlDataReader r, string col)
        {
            int i = r.GetOrdinal(col);
            return r.IsDBNull(i) ? (int?)null : r.GetInt32(i);
        }
        private async Task<EggActualDbModel?> GetVbapYumurtaBilgileri(int kumesId, DateTime day, CancellationToken ct)
        {
            //const string sql = @"SELECT TOP (1) SizeS, SizeM, SizeL, SizeXL, SizeNone, BrokenEgg, DirtEggClean, DirtEggLow, DirtEggMedium, DirtEggHigh, DirtBloodClean, DirtBloodLow, DirtBloodMedium, DirtBloodHigh, DirtFecesClean, DirtFecesLow,
            //                    DirtFecesMedium, DirtFecesHigh FROM dbo.EggActual WITH (NOLOCK) WHERE LocationId = @kumesId AND CAST(LastModificationTime as date) = CAST(@day as date) ORDER BY LastModificationTime DESC;";

            const string sql = @"SELECT TOP (1) Kumes, SizeSKumes, SizeMKumes, SizeLKumes, SizeXLKumes, SizeNoneKumes, BrokenEgg, DirtEggCleanKumes, DirtEggLowKumes, DirtEggMediumKumes, DirtEggHighKumes, DirtBloodCleanKumes, 
                                DirtBloodLowKumes, DirtBloodMediumKumes, DirtBloodHighKumes, DirtFecesCleanKumes, DirtFecesLowKumes, DirtFecesMediumKumes,DirtFecesHighKumes 
                                FROM dbo.EggKumes WITH (NOLOCK) WHERE KumesId = @kumesId AND CAST(CreationTime as date) = CAST(@day as date) ORDER BY CreationTime DESC;";

            await using var conn = new SqlConnection(_vbapConnStr);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@kumesId", SqlDbType.Int).Value = kumesId;
            cmd.Parameters.Add("@day", SqlDbType.DateTime2).Value = day;

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct);
            if (!await reader.ReadAsync(ct)) return null;

            return new EggActualDbModel
            {
                SAdet = ReadIntNullable(reader, "SizeSKumes"),
                MAdet = ReadIntNullable(reader, "SizeMKumes"),
                LAdet = ReadIntNullable(reader, "SizeLKumes"),
                XLAdet = ReadIntNullable(reader, "SizeXLKumes"),
                AsiriKucukAdet = ReadIntNullable(reader, "SizeNoneKumes"),
                KirikYumurtaAdet = ReadIntNullable(reader, "BrokenEgg"),
                HicKirliYumurtaAdet = ReadIntNullable(reader, "DirtEggCleanKumes"),
                AzKirliYumurtaAdet = ReadIntNullable(reader, "DirtEggLowKumes"),
                OrtaKirliYumurtaAdet = ReadIntNullable(reader, "DirtEggMediumKumes"),
                CokKirliYumurtaAdet = ReadIntNullable(reader, "DirtEggHighKumes"),
                HicKanliYumurtaAdet = ReadIntNullable(reader, "DirtBloodCleanKumes"),
                AzKanliYumurtaAdet = ReadIntNullable(reader, "DirtBloodLowKumes"),
                OrtaKanliYumurtaAdet = ReadIntNullable(reader, "DirtBloodMediumKumes"),
                CokKanliYumurtaAdet = ReadIntNullable(reader, "DirtBloodHighKumes"),
                HicDiskiliYumurtaAdet = ReadIntNullable(reader, "DirtFecesCleanKumes"),
                AzDiskiliYumurtaAdet = ReadIntNullable(reader, "DirtFecesLowKumes"),
                OrtaDiskiliYumurtaAdet = ReadIntNullable(reader, "DirtFecesMediumKumes"),
                CokDiskiliYumurtaAdet = ReadIntNullable(reader, "DirtFecesHighKumes"),
                KumesYumurtaAdet = ReadIntNullable(reader, "Kumes"),
            };
        }
        public async Task BulkInsertEtiketLogYeniDataAsync(SqlConnection connection, IEnumerable<EtiketLogYeni> data)
        {
            try
            {
                var dataTable = new DataTable();
                var accessor = TypeAccessor.Create(typeof(EtiketLogYeni)); // FastMember ile eri�im

                // DataTable'a s�tunlar� ekleyelim
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
                    bulkCopy.DestinationTableName = "EtiketLogYeni"; // Verinin eklenece�i tablo ad�
                    bulkCopy.BulkCopyTimeout = 600;
                    // Dinamik kolon e�le�tirmesi
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
        public async Task BulkInsertKumesLogYeniDataAsync(SqlConnection connection, IEnumerable<KumesLogYeni> data)
        {
            try
            {
                var dataTable = new DataTable();
                var accessor = TypeAccessor.Create(typeof(KumesLogYeni)); // FastMember ile eri�im

                // DataTable'a s�tunlar� ekleyelim
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
                    bulkCopy.DestinationTableName = "KumesLogYeni"; // Verinin eklenece�i tablo ad�
                    // Dinamik kolon e�le�tirmesi
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }
                    await bulkCopy.WriteToServerAsync(dataTable); // Veriyi yazma
                }
            }
            catch (Exception) { }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                {
                    await connection.CloseAsync();
                }
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
                //_logger.LogInformation("AlarmBilgilendirmeServis stop: {time}", DateTimeOffset.Now);
                await UpdateServisDurumAsync("RahmiyeTesisLogWorker", 3, "Servis durduruldu");
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await UpdateServisDurumAsync("RahmiyeTesisLogWorker", 0, ex.Message);
            }
        }
    }
}