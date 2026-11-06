using FastMember;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using WaterLineScada.Domain.Models;
using WaterLineScada.Infrastructure;
using SuIzlemeLogWorker.Models;
using System.Data;
using System.Text.Json;
using static SharedKernel.ApplicationConstants;

namespace SuIzlemeLogWorker
{
    public class SuIzlemeLogNodeRedWorker : BackgroundService
    {
        private readonly ILogger<SuIzlemeLogNodeRedWorker> _logger;
        private readonly SqlConnection _sqlConnection;
        private readonly IServiceProvider _serviceProvider;
        public SuIzlemeLogNodeRedWorker(ILogger<SuIzlemeLogNodeRedWorker> logger, SqlConnection sqlConnection, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _sqlConnection = sqlConnection;
            _serviceProvider = serviceProvider;
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("SuIzlemeLogNodeRedWorker working: {time}", DateTimeOffset.Now);
                await UpdateServisDurumAsync("SuIzlemeLogNodeRedWorker", 2, "Servis baþlatýlýyor");
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                await UpdateServisDurumAsync("SuIzlemeLogNodeRedWorker", 0, ex.Message);
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var dataEtiketLogYeniKulucka = await GetEtiketLogYeniDataForBulkInsertKuluckaAsync(stoppingToken);
                    await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeniKulucka);
                    var dataEtiketLogYeniRahmiye = await GetEtiketLogYeniDataForBulkInsertRahmiyeAsync(stoppingToken);
                    await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeniRahmiye);
                    var dataEtiketLogYeniKesimhane = await GetEtiketLogYeniDataForBulkInsertKesimhaneAsync(stoppingToken);
                    await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeniKesimhane);
                    var dataEtiketLogYeniCayKarsisi = await GetEtiketLogYeniDataForBulkInsertCayKarsisiAsync(stoppingToken);
                    await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeniCayKarsisi);
                    var dataEtiketLogYeniOSBYem = await GetEtiketLogYeniDataForBulkInsertOSBYemAsync(stoppingToken);
                    await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeniOSBYem);
                    var dataEtiketLogYeniViol = await GetEtiketLogYeniDataForBulkInsertViolAsync(stoppingToken);
                    await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeniViol);
                    var dataEtiketLogYeniKayislar = await GetEtiketLogYeniDataForBulkInsertKayislarAsync(stoppingToken);
                    await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeniKayislar);
                    var dataEtiketLogYeniYumusakSu = await GetEtiketLogYeniDataForBulkInsertYumusakSuAsync(stoppingToken);
                    await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeniYumusakSu);
                    var dataEtiketLogYeniRendering = await GetEtiketLogYeniDataForBulkInsertRenderingAsync(stoppingToken);
                    await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeniRendering);
                    var dataEtiketLogYeniSuIsleme = await GetEtiketLogYeniDataForBulkInsertSuIslemeAsync(stoppingToken);
                    await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeniSuIsleme);
                    var dataEtiketLogYeniIleriIslem = await GetEtiketLogYeniDataForBulkInsertIleriIslemAsync(stoppingToken);
                    await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeniIleriIslem);
                    var dataEtiketLogYeniDogalgazliKazan = await GetEtiketLogYeniDataForBulkInsertDogalgazliKazanAsync(stoppingToken);
                    await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeniDogalgazliKazan);
                    var dataEtiketLogYeniPastorize = await GetEtiketLogYeniDataForBulkInsertPastorizeAsync(stoppingToken);
                    await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeniPastorize);

                    var dataSuKaynakLogYeniKulucka = await GetSuKaynakLogYeniDataForBulkInsertKuluckaAsync(stoppingToken);
                    await BulkInsertSuKaynakLogYeniDataAsync(_sqlConnection, dataSuKaynakLogYeniKulucka);
                    var dataSuKaynakLogYeniRahmiye = await GetSuKaynakLogYeniDataForBulkInsertRahmiyeAsync(stoppingToken);
                    await BulkInsertSuKaynakLogYeniDataAsync(_sqlConnection, dataSuKaynakLogYeniRahmiye);
                    var dataSuKaynakLogYeniKesimhane = await GetSuKaynakLogYeniDataForBulkInsertKesimhaneAsync(stoppingToken);
                    await BulkInsertSuKaynakLogYeniDataAsync(_sqlConnection, dataSuKaynakLogYeniKesimhane);
                    var dataSuKaynakLogYeniCayKarsisi = await GetSuKaynakLogYeniDataForBulkInsertCayKarsisiAsync(stoppingToken);
                    await BulkInsertSuKaynakLogYeniDataAsync(_sqlConnection, dataSuKaynakLogYeniCayKarsisi);
                    var dataSuKaynakLogYeniOSBYem = await GetSuKaynakLogYeniDataForBulkInsertOSBYemAsync(stoppingToken);
                    await BulkInsertSuKaynakLogYeniDataAsync(_sqlConnection, dataSuKaynakLogYeniOSBYem);
                    var dataSuKaynakLogYeniViol = await GetSuKaynakLogYeniDataForBulkInsertViolAsync(stoppingToken);
                    await BulkInsertSuKaynakLogYeniDataAsync(_sqlConnection, dataSuKaynakLogYeniViol);
                    var dataSuKaynakLogYeniKayislar = await GetSuKaynakLogYeniDataForBulkInsertKayislarAsync(stoppingToken);
                    await BulkInsertSuKaynakLogYeniDataAsync(_sqlConnection, dataSuKaynakLogYeniKayislar);
                    var dataSuKaynakLogYeniYumusakSu = await GetSuKaynakLogYeniDataForBulkInsertYumusakSuAsync(stoppingToken);
                    await BulkInsertSuKaynakLogYeniDataAsync(_sqlConnection, dataSuKaynakLogYeniYumusakSu);
                    var dataSuKaynakLogYeniRendering = await GetSuKaynakLogYeniDataForBulkInsertRenderingAsync(stoppingToken);
                    await BulkInsertSuKaynakLogYeniDataAsync(_sqlConnection, dataSuKaynakLogYeniRendering);
                    var dataSuKaynakLogYeniSuIsleme = await GetSuKaynakLogYeniDataForBulkInsertSuIslemeAsync(stoppingToken);
                    await BulkInsertSuKaynakLogYeniDataAsync(_sqlConnection, dataSuKaynakLogYeniSuIsleme);
                    var dataSuKaynakLogYeniIleriIslem = await GetSuKaynakLogYeniDataForBulkInsertIleriIslemAsync(stoppingToken);
                    await BulkInsertSuKaynakLogYeniDataAsync(_sqlConnection, dataSuKaynakLogYeniIleriIslem);
                    var dataSuKaynakLogYeniDogalgazliKazan = await GetSuKaynakLogYeniDataForBulkInsertDogalgazliKazanAsync(stoppingToken);
                    await BulkInsertSuKaynakLogYeniDataAsync(_sqlConnection, dataSuKaynakLogYeniDogalgazliKazan);
                    var dataSuKaynakLogYeniPastorize = await GetSuKaynakLogYeniDataForBulkInsertPastorizeAsync(stoppingToken);
                    await BulkInsertSuKaynakLogYeniDataAsync(_sqlConnection, dataSuKaynakLogYeniPastorize);

                    await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
                    await UpdateServisDurumAsync("SuIzlemeLogNodeRedWorker", 1, "Servis çalýþýyor", true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    await UpdateServisDurumAsync("SuIzlemeLogNodeRedWorker", 0, ex.Message);
                }
            }
        }

        #region EtiketLogData       
        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsertKuluckaAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 17;
            var now = DateTime.Now;
            List<EtiketLogYeni> etiketLogs = new List<EtiketLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model == null)
                {
                    var plcRead = 0;
                    var plcConnected = 1;
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 0, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 0, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    return etiketLogs;
                }
                if (model != null)
                {
                    var plcRead = model.Durum;
                    var plcConnected = model.Durum;

                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            KuluckaNodeRedModel kuluckaNodeRedModel = JsonSerializer.Deserialize<KuluckaNodeRedModel>(model.Veriler);

                            var byteDensity = 0;
                            var pingAverage = 0;
                            var lastPackageLength = 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 0, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 0, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 0, EtiketId = 4, KayitZamani = DateTime.Now, Deger = (decimal?)byteDensity });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 0, EtiketId = 5, KayitZamani = DateTime.Now, Deger = (decimal?)pingAverage });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 0, EtiketId = 6, KayitZamani = DateTime.Now, Deger = (decimal?)lastPackageLength });

                            if (plcConnected == 0) return etiketLogs;
                            //if (plcRead == 0) return etiketLogs;

                            var Kuyu_MezarlýkYaný_1_Pompa_Cmd_Run = kuluckaNodeRedModel.Kuyu_MezarlikYani_1_Pompa_Cmd_Run == true ? 1 : 0;
                            var Kuyu_MezarlýkYaný_1_Pompa_Rpr_Kontaktor = kuluckaNodeRedModel.Kuyu_MezarlikYani_1_Pompa_Rpr_Kontktr == true ? 1 : 0;
                            var Kuyu_MezarlýkYaný_1_Pompa_Rpr_TermikOK = kuluckaNodeRedModel.Kuyu_MezarlikYani_1_Pompa_Rpr_TermikOk == true ? 1 : 0;
                            var Kuyu_MezarlýkYaný_1_Pompa_Rpr_State = kuluckaNodeRedModel.Kuyu_MezarlikYani_1_Pompa_Rpr_State;
                            var Kuyu_MezarlýkYaný_1_SuSeviye = kuluckaNodeRedModel.Kuyu_MezarlikYani_1_SuSeviye == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 1, SayacId = 0, EtiketId = 7, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_MezarlýkYaný_1_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 1, SayacId = 0, EtiketId = 8, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_MezarlýkYaný_1_Pompa_Rpr_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 1, SayacId = 0, EtiketId = 9, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_MezarlýkYaný_1_Pompa_Rpr_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 1, SayacId = 0, EtiketId = 10, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_MezarlýkYaný_1_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 1, SayacId = 0, EtiketId = 430, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_MezarlýkYaný_1_SuSeviye });

                            var Kuyu_MezarlýkYaný_2_Pompa_Cmd_Run = kuluckaNodeRedModel.Kuyu_MezarlikYani_2_Pompa_Cmd_Run == true ? 1 : 0;
                            var Kuyu_MezarlýkYaný_2_Pompa_Rpr_Kontaktor = kuluckaNodeRedModel.Kuyu_MezarlikYani_2_Pompa_Rpr_Kontktr == true ? 1 : 0;
                            var Kuyu_MezarlýkYaný_2_Pompa_Rpr_TermikOK = kuluckaNodeRedModel.Kuyu_MezarlikYani_2_Pompa_Rpr_TermikOk == true ? 1 : 0;
                            var Kuyu_MezarlýkYaný_2_Pompa_Rpr_State = kuluckaNodeRedModel.Kuyu_MezarlikYani_2_Pompa_Rpr_State;
                            var Kuyu_MezarlýkYaný_2_SuSeviye = kuluckaNodeRedModel.Kuyu_MezarlikYani_2_SuSeviye == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 2, SayacId = 0, EtiketId = 11, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_MezarlýkYaný_2_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 2, SayacId = 0, EtiketId = 12, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_MezarlýkYaný_2_Pompa_Rpr_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 2, SayacId = 0, EtiketId = 13, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_MezarlýkYaný_2_Pompa_Rpr_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 2, SayacId = 0, EtiketId = 14, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_MezarlýkYaný_2_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 2, SayacId = 0, EtiketId = 431, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_MezarlýkYaný_2_SuSeviye });

                            var Kuyu_YanTarla_Pompa_Cmd_Run = kuluckaNodeRedModel.Kuyu_YanTarla_Pompa_Cmd_Run == true ? 1 : 0;
                            var Kuyu_YanTarla_Pompa_Rpr_Kontaktor = kuluckaNodeRedModel.Kuyu_YanTarla_Pompa_Rpr_Kontktr == true ? 1 : 0;
                            var Kuyu_YanTarla_Pompa_Rpr_TermikOK = kuluckaNodeRedModel.Kuyu_YanTarla_Pompa_Rpr_TermikOk == true ? 1 : 0;
                            var Kuyu_YanTarla_Pompa_Rpr_State = kuluckaNodeRedModel.Kuyu_YanTarla_Pompa_Rpr_State;
                            var Kuyu_YanTarla_SuSeviye = kuluckaNodeRedModel.Kuyu_YanTarla_SuSeviye == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 3, SayacId = 0, EtiketId = 15, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_YanTarla_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 3, SayacId = 0, EtiketId = 16, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_YanTarla_Pompa_Rpr_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 3, SayacId = 0, EtiketId = 17, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_YanTarla_Pompa_Rpr_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 3, SayacId = 0, EtiketId = 18, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_YanTarla_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 3, SayacId = 0, EtiketId = 432, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_YanTarla_SuSeviye });

                            var Tank_Sens_Min = kuluckaNodeRedModel.Tank_Sens_Min == true ? 1 : 0;
                            var Tank_Sens_Max = kuluckaNodeRedModel.Tank_Sens_Max == true ? 1 : 0;
                            var Tank_Sens_Basinc = kuluckaNodeRedModel.Tank_SensBasinc;
                            var Tank_SuSeviye = kuluckaNodeRedModel.Tank_SuSeviye;
                            var Tank_SuSeviye_Yuzdesi = kuluckaNodeRedModel.Tank_SuSeviye_Yuzde;
                            var Tank_SuHacim = kuluckaNodeRedModel.Tank_SuHacim;
                            var Tank_SuHacim_Yuzdesi = kuluckaNodeRedModel.Tank_SuHacim_Yuzde;
                            var Tank_T_TankHacim = kuluckaNodeRedModel.Tank_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 4, SayacId = 0, EtiketId = 19, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 4, SayacId = 0, EtiketId = 20, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 4, SayacId = 0, EtiketId = 21, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 4, SayacId = 0, EtiketId = 22, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 4, SayacId = 0, EtiketId = 23, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 4, SayacId = 0, EtiketId = 24, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 4, SayacId = 0, EtiketId = 25, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 4, SayacId = 0, EtiketId = 26, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_T_TankHacim });

                            var DM_MezarlýkYaný_1_Connected = kuluckaNodeRedModel.DM_MezarlikYani_1_Connected == true ? 1 : 0;
                            var DM_MezarlýkYaný_1_Total = kuluckaNodeRedModel.DM_MezarlikYani_1_Total;
                            var DM_MezarlýkYaný_1_Debi = kuluckaNodeRedModel.DM_MezarlikYani_1_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 1, EtiketId = 27, KayitZamani = DateTime.Now, Deger = (decimal?)DM_MezarlýkYaný_1_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 1, EtiketId = 28, KayitZamani = DateTime.Now, Deger = (decimal?)DM_MezarlýkYaný_1_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 1, EtiketId = 29, KayitZamani = DateTime.Now, Deger = (decimal?)DM_MezarlýkYaný_1_Debi });

                            var DM_MezarlýkYaný_2_Connected = kuluckaNodeRedModel.DM_MezarlikYani_2_Connected == true ? 1 : 0;
                            var DM_MezarlýkYaný_2_Total = kuluckaNodeRedModel.DM_MezarlikYani_2_Total;
                            var DM_MezarlýkYaný_2_Debi = kuluckaNodeRedModel.DM_MezarlikYani_2_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 2, EtiketId = 30, KayitZamani = DateTime.Now, Deger = (decimal?)DM_MezarlýkYaný_2_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 2, EtiketId = 31, KayitZamani = DateTime.Now, Deger = (decimal?)DM_MezarlýkYaný_2_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 2, EtiketId = 32, KayitZamani = DateTime.Now, Deger = (decimal?)DM_MezarlýkYaný_2_Debi });

                            var DM_YanTarla_Connected = kuluckaNodeRedModel.DM_YanTarla_Connected == true ? 1 : 0;
                            var DM_YanTarla_Total = kuluckaNodeRedModel.DM_YanTarla_Total;
                            var DM_YanTarla_Debi = kuluckaNodeRedModel.DM_YanTarla_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 3, EtiketId = 33, KayitZamani = DateTime.Now, Deger = (decimal?)DM_YanTarla_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 3, EtiketId = 34, KayitZamani = DateTime.Now, Deger = (decimal?)DM_YanTarla_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 3, EtiketId = 35, KayitZamani = DateTime.Now, Deger = (decimal?)DM_YanTarla_Debi });

                            var DM_Uretim_Connected = kuluckaNodeRedModel.DM_Uretim_Connected == true ? 1 : 0;
                            var DM_Uretim_Total = kuluckaNodeRedModel.DM_Uretim_Total;
                            var DM_Uretim_Debi = kuluckaNodeRedModel.DM_Uretim_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 6, EtiketId = 36, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 6, EtiketId = 37, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 6, EtiketId = 38, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim_Debi });

                            var DM_Kayislar_Connected = kuluckaNodeRedModel.DM_Kayislar_Connected == true ? 1 : 0;
                            var DM_Kayislar_Total = kuluckaNodeRedModel.DM_Kayislar_Total;
                            var DM_Kayislar_Debi = kuluckaNodeRedModel.DM_Kayislar_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 5, EtiketId = 39, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kayislar_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 5, EtiketId = 40, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kayislar_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 5, EtiketId = 41, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kayislar_Debi });

                            var Hidrofor_Cmd_Calis = kuluckaNodeRedModel.Hidrofor_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Rpr_State = kuluckaNodeRedModel.Hidrofor_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 5, SayacId = 0, EtiketId = 42, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 5, SayacId = 0, EtiketId = 43, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Rpr_State });

                            var Hidrofor_Motor_1_Kontaktor = kuluckaNodeRedModel.Hidrofor_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_1_TermikOK = kuluckaNodeRedModel.Hidrofor_Motor1_Alarm == true ? 1 : 0;
                            var Hidrofor_Motor_1_State = kuluckaNodeRedModel.Hidrofor_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 6, SayacId = 0, EtiketId = 44, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 6, SayacId = 0, EtiketId = 45, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 6, SayacId = 0, EtiketId = 46, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_State });

                            var Hidrofor_Motor_2_Kontaktor = kuluckaNodeRedModel.Hidrofor_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_2_TermikOK = kuluckaNodeRedModel.Hidrofor_Motor2_Alarm == true ? 1 : 0;
                            var Hidrofor_Motor_2_State = kuluckaNodeRedModel.Hidrofor_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 7, SayacId = 0, EtiketId = 47, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 7, SayacId = 0, EtiketId = 48, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 7, SayacId = 0, EtiketId = 49, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_State });

                            var Hidrofor_Motor_3_Kontaktor = kuluckaNodeRedModel.Hidrofor_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_3_TermikOK = kuluckaNodeRedModel.Hidrofor_Motor3_Alarm == true ? 1 : 0;
                            var Hidrofor_Motor_3_State = kuluckaNodeRedModel.Hidrofor_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 8, SayacId = 0, EtiketId = 50, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 8, SayacId = 0, EtiketId = 51, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 8, SayacId = 0, EtiketId = 52, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_State });

                            var Rio_Kuyu_MezarlýkYaný_1_Connected = kuluckaNodeRedModel.Rio_1_MezarlikYani_1_Connected == true ? 1 : 0;
                            var Rio_Kuyu_MezarlýkYaný_1_RSSI = kuluckaNodeRedModel.Rio_1_MezarlikYani_1_RSSI;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 1, SayacId = 0, EtiketId = 404, KayitZamani = DateTime.Now, Deger = (decimal?)Rio_Kuyu_MezarlýkYaný_1_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 1, SayacId = 0, EtiketId = 405, KayitZamani = DateTime.Now, Deger = (decimal?)Rio_Kuyu_MezarlýkYaný_1_RSSI });

                            var Rio_Kuyu_YanTarla_Connected = kuluckaNodeRedModel.Rio_2_YanTarla_Connected == true ? 1 : 0;
                            var Rio_Kuyu_YanTarla_RSSI = kuluckaNodeRedModel.Rio_2_YanTarla_RSSI;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 3, SayacId = 0, EtiketId = 406, KayitZamani = DateTime.Now, Deger = (decimal?)Rio_Kuyu_YanTarla_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 3, SayacId = 0, EtiketId = 407, KayitZamani = DateTime.Now, Deger = (decimal?)Rio_Kuyu_YanTarla_RSSI });

                            var KollektorBasinci_Connected = kuluckaNodeRedModel.KollektorBasinci_Connected == true ? 1 : 0;
                            var KollektorBasinci_Value = kuluckaNodeRedModel.KollektorBasinci_Value;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 5, SayacId = 0, EtiketId = 408, KayitZamani = DateTime.Now, Deger = (decimal?)KollektorBasinci_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 5, SayacId = 0, EtiketId = 409, KayitZamani = DateTime.Now, Deger = (decimal?)KollektorBasinci_Value });
                        }
                    }
                    else
                    {
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 0, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = 17, SuKaynakId = 0, SayacId = 0, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    }
                }
            }
            return etiketLogs;
        }
        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsertRahmiyeAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 13;
            var now = DateTime.Now;
            List<EtiketLogYeni> etiketLogs = new List<EtiketLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model == null)
                {
                    var plcRead = 0;
                    var plcConnected = 1;
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    return etiketLogs;
                }
                if (model != null)
                {
                    var plcRead = model.Durum;
                    var plcConnected = model.Durum;

                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            RahmiyeNodeRedModel rahmiyeNodeRedModel = JsonSerializer.Deserialize<RahmiyeNodeRedModel>(model.Veriler);

                            var byteDensity = 0;
                            var pingAverage = 0;
                            var lastPackageLength = 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 0, EtiketId = 54, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 0, EtiketId = 55, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 0, EtiketId = 56, KayitZamani = DateTime.Now, Deger = (decimal?)byteDensity });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 0, EtiketId = 57, KayitZamani = DateTime.Now, Deger = (decimal?)pingAverage });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 0, EtiketId = 58, KayitZamani = DateTime.Now, Deger = (decimal?)lastPackageLength });

                            if (plcConnected == 0) return etiketLogs;
                            //if (plcRead == 0) return etiketLogs;

                            var Rahmiye_Kuyu_1_Mode = rahmiyeNodeRedModel.Kuyu_1_Mode;
                            var Rahmiye_Kuyu_1_Pompa_Cmd_Run = rahmiyeNodeRedModel.Kuyu_1_Pompa_Cmd_Run == true ? 1 : 0;
                            var Rahmiye_Kuyu_1_Pompa_Rpr_Kontaktor = rahmiyeNodeRedModel.Kuyu_1_Pompa_Rpr_Kontkr == true ? 1 : 0;
                            var Rahmiye_Kuyu_1_Pompa_Rpr_TermikOK = rahmiyeNodeRedModel.Kuyu_1_Pompa_Rpr_TermikOk == true ? 1 : 0;
                            var Rahmiye_Kuyu_1_Pompa_Rpr_State = rahmiyeNodeRedModel.Kuyu_1_Pompa_Rpr_State;
                            var Rahmiye_Kuyu_1_SuSeviye = rahmiyeNodeRedModel.Kuyu_1_SeviyeSensor == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 12, SayacId = 0, EtiketId = 355, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_1_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 12, SayacId = 0, EtiketId = 59, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_1_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 12, SayacId = 0, EtiketId = 60, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_1_Pompa_Rpr_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 12, SayacId = 0, EtiketId = 61, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_1_Pompa_Rpr_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 12, SayacId = 0, EtiketId = 62, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_1_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 12, SayacId = 0, EtiketId = 389, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_1_SuSeviye });

                            var Rahmiye_Kuyu_2_Mode = rahmiyeNodeRedModel.Kuyu_2_Mode;
                            var Rahmiye_Kuyu_2_Pompa_Cmd_Run = rahmiyeNodeRedModel.Kuyu_2_Pompa_Cmd_Run == true ? 1 : 0;
                            var Rahmiye_Kuyu_2_Pompa_Rpr_Kontaktor = rahmiyeNodeRedModel.Kuyu_2_Pompa_Rpr_Kontkr == true ? 1 : 0;
                            var Rahmiye_Kuyu_2_Pompa_Rpr_TermikOK = rahmiyeNodeRedModel.Kuyu_2_Pompa_Rpr_TermikOk == true ? 1 : 0;
                            var Rahmiye_Kuyu_2_Pompa_Rpr_State = rahmiyeNodeRedModel.Kuyu_2_Pompa_Rpr_State;
                            var Rahmiye_Kuyu_2_SuSeviye = rahmiyeNodeRedModel.Kuyu_2_SeviyeSensor == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 12, SayacId = 0, EtiketId = 356, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_2_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 13, SayacId = 0, EtiketId = 63, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_2_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 13, SayacId = 0, EtiketId = 64, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_2_Pompa_Rpr_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 13, SayacId = 0, EtiketId = 65, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_2_Pompa_Rpr_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 13, SayacId = 0, EtiketId = 66, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_2_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 13, SayacId = 0, EtiketId = 390, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_2_SuSeviye });

                            var Rahmiye_Kuyu_3_Mode = rahmiyeNodeRedModel.Kuyu_3_Mode;
                            var Rahmiye_Kuyu_3_Pompa_Cmd_Run = rahmiyeNodeRedModel.Kuyu_3_Pompa_Cmd_Run == true ? 1 : 0;
                            var Rahmiye_Kuyu_3_Pompa_Rpr_Kontaktor = rahmiyeNodeRedModel.Kuyu_3_Pompa_Rpr_Kontkr == true ? 1 : 0;
                            var Rahmiye_Kuyu_3_Pompa_Rpr_TermikOK = rahmiyeNodeRedModel.Kuyu_3_Pompa_Rpr_TermikOk == true ? 1 : 0;
                            var Rahmiye_Kuyu_3_Pompa_Rpr_State = rahmiyeNodeRedModel.Kuyu_3_Pompa_Rpr_State;
                            var Rahmiye_Kuyu_3_SuSeviye = rahmiyeNodeRedModel.Kuyu_3_SeviyeSensor == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 12, SayacId = 0, EtiketId = 357, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_3_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 14, SayacId = 0, EtiketId = 67, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_3_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 14, SayacId = 0, EtiketId = 68, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_3_Pompa_Rpr_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 14, SayacId = 0, EtiketId = 69, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_3_Pompa_Rpr_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 14, SayacId = 0, EtiketId = 70, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_3_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 14, SayacId = 0, EtiketId = 391, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_3_SuSeviye });

                            var Rahmiye_Kuyu_4_Mode = rahmiyeNodeRedModel.Kuyu_4_Mode;
                            var Rahmiye_Kuyu_4_Pompa_Cmd_Run = rahmiyeNodeRedModel.Kuyu_4_Pompa_Cmd_Run == true ? 1 : 0;
                            var Rahmiye_Kuyu_4_Pompa_Rpr_Kontaktor = rahmiyeNodeRedModel.Kuyu_4_Pompa_Rpr_Kontkr == true ? 1 : 0;
                            var Rahmiye_Kuyu_4_Pompa_Rpr_TermikOK = rahmiyeNodeRedModel.Kuyu_4_Pompa_Rpr_TermikOk == true ? 1 : 0;
                            var Rahmiye_Kuyu_4_Pompa_Rpr_State = rahmiyeNodeRedModel.Kuyu_4_Pompa_Rpr_State;
                            var Rahmiye_Kuyu_4_SuSeviye = rahmiyeNodeRedModel.Kuyu_4_SeviyeSensor == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 12, SayacId = 0, EtiketId = 358, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_4_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 15, SayacId = 0, EtiketId = 71, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_4_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 15, SayacId = 0, EtiketId = 72, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_4_Pompa_Rpr_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 15, SayacId = 0, EtiketId = 73, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_4_Pompa_Rpr_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 15, SayacId = 0, EtiketId = 74, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_4_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 15, SayacId = 0, EtiketId = 392, KayitZamani = DateTime.Now, Deger = (decimal?)Rahmiye_Kuyu_4_SuSeviye });

                            var Tank_Sens_Min = rahmiyeNodeRedModel.Tank_Sens_Min == true ? 1 : 0;
                            var Tank_Sens_Max = rahmiyeNodeRedModel.Tank_Sens_Max == true ? 1 : 0;
                            var Tank_Sens_Basinc = rahmiyeNodeRedModel.Tank_SensBasinc;
                            var Tank_SuSeviye = rahmiyeNodeRedModel.Tank_SuSeviye;
                            var Tank_SuSeviye_Yuzdesi = rahmiyeNodeRedModel.Tank_SuSeviye_Yuzde;
                            var Tank_SuHacim = rahmiyeNodeRedModel.Tank_SuHacim;
                            var Tank_SuHacim_Yuzdesi = rahmiyeNodeRedModel.Tank_SuHacimYuzde;
                            var Tank_T_TankHacim = rahmiyeNodeRedModel.Tank_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 16, SayacId = 0, EtiketId = 75, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 16, SayacId = 0, EtiketId = 76, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 16, SayacId = 0, EtiketId = 77, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 16, SayacId = 0, EtiketId = 78, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 16, SayacId = 0, EtiketId = 79, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 16, SayacId = 0, EtiketId = 80, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 16, SayacId = 0, EtiketId = 81, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 16, SayacId = 0, EtiketId = 82, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_T_TankHacim });

                            var DM_Kuyu_1_Connected = rahmiyeNodeRedModel.DM_Kuyu_1_Connected == true ? 1 : 0;
                            var DM_Kuyu_1_Total = rahmiyeNodeRedModel.DM_Kuyu_1_Total;
                            var DM_Kuyu_1_Debi = rahmiyeNodeRedModel.DM_Kuyu_1_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 10, EtiketId = 83, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_1_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 10, EtiketId = 84, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_1_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 10, EtiketId = 85, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_1_Debi });

                            var DM_Kuyu_2_Connected = rahmiyeNodeRedModel.DM_Kuyu_2_Connected == true ? 1 : 0;
                            var DM_Kuyu_2_Total = rahmiyeNodeRedModel.DM_Kuyu_2_Total;
                            var DM_Kuyu_2_Debi = rahmiyeNodeRedModel.DM_Kuyu_2_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 11, EtiketId = 86, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_2_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 11, EtiketId = 87, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_2_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 11, EtiketId = 88, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_2_Debi });

                            var DM_Kuyu_3_Connected = rahmiyeNodeRedModel.DM_Kuyu_3_Connected == true ? 1 : 0;
                            var DM_Kuyu_3_Total = rahmiyeNodeRedModel.DM_Kuyu_3_Total;
                            var DM_Kuyu_3_Debi = rahmiyeNodeRedModel.DM_Kuyu_3_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 12, EtiketId = 89, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_3_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 12, EtiketId = 90, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_3_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 12, EtiketId = 91, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_3_Debi });

                            var DM_Kuyu_4_Connected = rahmiyeNodeRedModel.DM_Kuyu_4_Connected == true ? 1 : 0;
                            var DM_Kuyu_4_Total = rahmiyeNodeRedModel.DM_Kuyu_4_Total;
                            var DM_Kuyu_4_Debi = rahmiyeNodeRedModel.DM_Kuyu_4_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 13, EtiketId = 92, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_4_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 13, EtiketId = 93, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_4_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 13, EtiketId = 94, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_4_Debi });

                            var Hidrofor_Cmd_Calis = rahmiyeNodeRedModel.Hidrofor_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Rpr_Mode = rahmiyeNodeRedModel.Hidrofor_Rpr_Mode;
                            var Hidrofor_Rpr_State = rahmiyeNodeRedModel.Hidrofor_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 17, SayacId = 0, EtiketId = 95, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 17, SayacId = 0, EtiketId = 359, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 17, SayacId = 0, EtiketId = 96, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Rpr_State });

                            var Hidrofor_Motor_1_Kontaktor = rahmiyeNodeRedModel.Hidrofor_Motor_1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_1_TermikOK = rahmiyeNodeRedModel.Hidrofor_Motor_1_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_1_State = rahmiyeNodeRedModel.Hidrofor_Motor_1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 18, SayacId = 0, EtiketId = 97, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 18, SayacId = 0, EtiketId = 98, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 18, SayacId = 0, EtiketId = 99, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_State });

                            var Hidrofor_Motor_2_Kontaktor = rahmiyeNodeRedModel.Hidrofor_Motor_2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_2_TermikOK = rahmiyeNodeRedModel.Hidrofor_Motor_2_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_2_State = rahmiyeNodeRedModel.Hidrofor_Motor_2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 19, SayacId = 0, EtiketId = 100, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 19, SayacId = 0, EtiketId = 101, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 19, SayacId = 0, EtiketId = 102, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_State });

                            var Hidrofor_Motor_3_Kontaktor = rahmiyeNodeRedModel.Hidrofor_Motor_3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_3_TermikOK = rahmiyeNodeRedModel.Hidrofor_Motor_3_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_3_State = rahmiyeNodeRedModel.Hidrofor_Motor_3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 20, SayacId = 0, EtiketId = 103, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 20, SayacId = 0, EtiketId = 104, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 20, SayacId = 0, EtiketId = 105, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_State });

                            var Jenerator_Calisiyor = rahmiyeNodeRedModel.Jenerator_Calisiyor == true ? 1 : 0;
                            var Jenerator_Ariza = rahmiyeNodeRedModel.Jenerator_Ariza == true ? 1 : 0;
                            var Jenerator_SebekeElektrik = rahmiyeNodeRedModel.Jenerator_SebekeElektrik == true ? 1 : 0;
                            var Jenerator_HidroforKollektorBasinc = rahmiyeNodeRedModel.HidroforKollektorBasinc;
                            var Jenerator_EnerjiKaynagi = rahmiyeNodeRedModel.EnerjiKaynagi;
                            var JeneratorKacakAkimOK = rahmiyeNodeRedModel.KacakAkimOk == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 0, EtiketId = 349, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 0, EtiketId = 350, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 0, EtiketId = 351, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_SebekeElektrik });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 0, EtiketId = 352, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_HidroforKollektorBasinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 0, EtiketId = 353, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_EnerjiKaynagi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 0, EtiketId = 354, KayitZamani = DateTime.Now, Deger = (decimal?)JeneratorKacakAkimOK });
                        }
                    }
                    else
                    {
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 0, EtiketId = 54, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = 13, SuKaynakId = 0, SayacId = 0, EtiketId = 55, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    }
                }
            }
            return etiketLogs;
        }
        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsertKesimhaneAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 10;
            var now = DateTime.Now;
            List<EtiketLogYeni> etiketLogs = new List<EtiketLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model == null)
                {
                    var plcRead = 0;
                    var plcConnected = 1;
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 0, SayacId = 0, EtiketId = 107, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 0, SayacId = 0, EtiketId = 108, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    return etiketLogs;
                }
                if (model != null)
                {
                    var plcRead = model.Durum;
                    var plcConnected = model.Durum;

                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            KesimhaneNodeRedModel kesimhaneNodeRedModel = JsonSerializer.Deserialize<KesimhaneNodeRedModel>(model.Veriler);

                            var byteDensity = 0;
                            var pingAverage = 0;
                            var lastPackageLength = 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 0, SayacId = 0, EtiketId = 107, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 0, SayacId = 0, EtiketId = 108, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 0, SayacId = 0, EtiketId = 109, KayitZamani = DateTime.Now, Deger = (decimal?)byteDensity });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 0, SayacId = 0, EtiketId = 110, KayitZamani = DateTime.Now, Deger = (decimal?)pingAverage });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 0, SayacId = 0, EtiketId = 111, KayitZamani = DateTime.Now, Deger = (decimal?)lastPackageLength });

                            if (plcConnected == 0) return etiketLogs;
                            //if (plcRead == 0) return etiketLogs;

                            var TankButun_Sens_Min = kesimhaneNodeRedModel.TankButun_Sens_Min == true ? 1 : 0;
                            var TankButun_Sens_Orta = kesimhaneNodeRedModel.TankButun_Sens_Orta == true ? 1 : 0;
                            var TankButun_Sens_Max = kesimhaneNodeRedModel.TankButun_Sens_Max == true ? 1 : 0;
                            var TankButun_Sens_Basinc = kesimhaneNodeRedModel.TankButun_SensBasinc;
                            var TankButun_SuSeviye = kesimhaneNodeRedModel.TankButun_SuSeviye;
                            var TankButun_SuHacim = kesimhaneNodeRedModel.TankButun_SuHacim;
                            var TankButun_SuHacim_Yuzdesi = kesimhaneNodeRedModel.TankButun_SuHacim_Yuzde;
                            var TankButun_T_TankHacim = kesimhaneNodeRedModel.TankButun_Hacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 40, SayacId = 0, EtiketId = 112, KayitZamani = DateTime.Now, Deger = (decimal?)TankButun_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 40, SayacId = 0, EtiketId = 113, KayitZamani = DateTime.Now, Deger = (decimal?)TankButun_Sens_Orta });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 40, SayacId = 0, EtiketId = 114, KayitZamani = DateTime.Now, Deger = (decimal?)TankButun_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 40, SayacId = 0, EtiketId = 115, KayitZamani = DateTime.Now, Deger = (decimal?)TankButun_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 40, SayacId = 0, EtiketId = 116, KayitZamani = DateTime.Now, Deger = (decimal?)TankButun_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 40, SayacId = 0, EtiketId = 117, KayitZamani = DateTime.Now, Deger = (decimal?)TankButun_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 40, SayacId = 0, EtiketId = 118, KayitZamani = DateTime.Now, Deger = (decimal?)TankButun_SuHacim_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 40, SayacId = 0, EtiketId = 119, KayitZamani = DateTime.Now, Deger = (decimal?)TankButun_T_TankHacim });

                            var Tank1_SuSeviye = kesimhaneNodeRedModel.Tank_1_SuSeviye;
                            var Tank1_SuSeviye_Yuzdesi = kesimhaneNodeRedModel.Tank_1_SuSeviye_Yuzde;
                            var Tank1_SuHacim = kesimhaneNodeRedModel.Tank_1_SuSeviye_Hacim;
                            var Tank1_SuHacim_Yuzdesi = kesimhaneNodeRedModel.Tank_1_SuSeviye_Hacim_Yuzde;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 41, SayacId = 0, EtiketId = 120, KayitZamani = DateTime.Now, Deger = (decimal?)Tank1_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 41, SayacId = 0, EtiketId = 121, KayitZamani = DateTime.Now, Deger = (decimal?)Tank1_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 41, SayacId = 0, EtiketId = 122, KayitZamani = DateTime.Now, Deger = (decimal?)Tank1_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 41, SayacId = 0, EtiketId = 123, KayitZamani = DateTime.Now, Deger = (decimal?)Tank1_SuHacim_Yuzdesi });

                            var Tank2_SuSeviye = kesimhaneNodeRedModel.Tank_2_SuSeviye;
                            var Tank2_SuSeviye_Yuzdesi = kesimhaneNodeRedModel.Tank_2_SuSeviye_Yuzde;
                            var Tank2_SuHacim = kesimhaneNodeRedModel.Tank_2_SuSeviye_Hacim;
                            var Tank2_SuHacim_Yuzdesi = kesimhaneNodeRedModel.Tank_2_SuSeviye_Hacim_Yuzde;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 42, SayacId = 0, EtiketId = 124, KayitZamani = DateTime.Now, Deger = (decimal?)Tank2_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 42, SayacId = 0, EtiketId = 125, KayitZamani = DateTime.Now, Deger = (decimal?)Tank2_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 42, SayacId = 0, EtiketId = 126, KayitZamani = DateTime.Now, Deger = (decimal?)Tank2_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 42, SayacId = 0, EtiketId = 127, KayitZamani = DateTime.Now, Deger = (decimal?)Tank2_SuHacim_Yuzdesi });

                            var DM_Tank_Connected = kesimhaneNodeRedModel.DM_Tank_Connected == true ? 1 : 0;
                            var DM_Tank_Total = kesimhaneNodeRedModel.DM_Tank_Total;
                            var DM_Tank_Debi = kesimhaneNodeRedModel.DM_Tank_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 0, SayacId = 23, EtiketId = 128, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Tank_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 0, SayacId = 23, EtiketId = 129, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Tank_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 0, SayacId = 23, EtiketId = 130, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Tank_Debi });

                            var RahmiyeBaglantiKoptu = kesimhaneNodeRedModel.Alarm_3.GetValueOrDefault(false) == true ? 1 : 0;
                            var CayKarsisiBaglantiKoptu = kesimhaneNodeRedModel.Alarm_4.GetValueOrDefault(false) == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 0, SayacId = 0, EtiketId = 244, KayitZamani = DateTime.Now, Deger = (decimal?)RahmiyeBaglantiKoptu });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 0, SayacId = 0, EtiketId = 245, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisiBaglantiKoptu });
                        }
                    }
                    else
                    {
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 0, SayacId = 0, EtiketId = 107, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = 10, SuKaynakId = 0, SayacId = 0, EtiketId = 108, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    }
                }
            }
            return etiketLogs;
        }
        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsertCayKarsisiAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 14;
            var now = DateTime.Now;
            List<EtiketLogYeni> etiketLogs = new List<EtiketLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model == null)
                {
                    var plcRead = 0;
                    var plcConnected = 1;
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 0, EtiketId = 132, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 0, EtiketId = 133, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    return etiketLogs;
                }
                if (model != null)
                {
                    var plcRead = model.Durum;
                    var plcConnected = model.Durum;

                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            CayKarsisiNodeRedModel cayKarsisiNodeRedModel = JsonSerializer.Deserialize<CayKarsisiNodeRedModel>(model.Veriler);

                            var byteDensity = 0;
                            var pingAverage = 0;
                            var lastPackageLength = 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 0, EtiketId = 132, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 0, EtiketId = 133, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 0, EtiketId = 134, KayitZamani = DateTime.Now, Deger = (decimal?)byteDensity });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 0, EtiketId = 135, KayitZamani = DateTime.Now, Deger = (decimal?)pingAverage });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 0, EtiketId = 136, KayitZamani = DateTime.Now, Deger = (decimal?)lastPackageLength });

                            if (plcConnected == 0) return etiketLogs;
                            //if (plcRead == 0) return etiketLogs;

                            var CayKarsisi_Kuyu_1_Mode = cayKarsisiNodeRedModel.Kuyu_1_Mode;
                            var CayKarsisi_Kuyu_1_Pompa_Cmd_Run = cayKarsisiNodeRedModel.Kuyu_1_Pompa_Cmd_Run == true ? 1 : 0;
                            var CayKarsisi_Kuyu_1_Pompa_Rpr_Calisiyor = cayKarsisiNodeRedModel.Kuyu_1_Pompa_Rpr_Calisiyor == true ? 1 : 0;
                            var CayKarsisi_Kuyu_1_Pompa_Rpr_Ariza = cayKarsisiNodeRedModel.Kuyu_1_Pompa_Rpr_Ariza == true ? 1 : 0;
                            var CayKarsisi_Kuyu_1_Pompa_Rpr_State = cayKarsisiNodeRedModel.Kuyu_1_Pompa_Rpr_State;
                            var CayKarsisi_Kuyu_1_SuSeviye = cayKarsisiNodeRedModel.Kuyu_1_Seviye_Sensor == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 31, SayacId = 0, EtiketId = 340, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_1_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 31, SayacId = 0, EtiketId = 137, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_1_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 31, SayacId = 0, EtiketId = 138, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_1_Pompa_Rpr_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 31, SayacId = 0, EtiketId = 139, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_1_Pompa_Rpr_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 31, SayacId = 0, EtiketId = 140, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_1_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 31, SayacId = 0, EtiketId = 393, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_1_SuSeviye });

                            var CayKarsisi_Kuyu_2_Mode = cayKarsisiNodeRedModel.Kuyu_2_Mode;
                            var CayKarsisi_Kuyu_2_Pompa_Cmd_Run = cayKarsisiNodeRedModel.Kuyu_2_Pompa_Cmd_Run == true ? 1 : 0;
                            var CayKarsisi_Kuyu_2_Pompa_Rpr_Calisiyor = cayKarsisiNodeRedModel.Kuyu_2_Pompa_Rpr_Calisiyor == true ? 1 : 0;
                            var CayKarsisi_Kuyu_2_Pompa_Rpr_Ariza = cayKarsisiNodeRedModel.Kuyu_2_Pompa_Rpr_Ariza == true ? 1 : 0;
                            var CayKarsisi_Kuyu_2_Pompa_Rpr_State = cayKarsisiNodeRedModel.Kuyu_2_Pompa_Rpr_State;
                            var CayKarsisi_Kuyu_2_SuSeviye = cayKarsisiNodeRedModel.Kuyu_2_Seviye_Sensor == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 32, SayacId = 0, EtiketId = 341, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_2_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 32, SayacId = 0, EtiketId = 141, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_2_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 32, SayacId = 0, EtiketId = 142, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_2_Pompa_Rpr_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 32, SayacId = 0, EtiketId = 143, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_2_Pompa_Rpr_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 32, SayacId = 0, EtiketId = 144, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_2_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 32, SayacId = 0, EtiketId = 394, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_2_SuSeviye });

                            var CayKarsisi_Kuyu_3_Mode = cayKarsisiNodeRedModel.Kuyu_3_Mode;
                            var CayKarsisi_Kuyu_3_Pompa_Cmd_Run = cayKarsisiNodeRedModel.Kuyu_3_Pompa_Cmd_Run == true ? 1 : 0;
                            var CayKarsisi_Kuyu_3_Pompa_Rpr_Calisiyor = cayKarsisiNodeRedModel.Kuyu_3_Pompa_Rpr_Calisiyor == true ? 1 : 0;
                            var CayKarsisi_Kuyu_3_Pompa_Rpr_Ariza = cayKarsisiNodeRedModel.Kuyu_3_Pompa_Rpr_Ariza == true ? 1 : 0;
                            var CayKarsisi_Kuyu_3_Pompa_Rpr_State = cayKarsisiNodeRedModel.Kuyu_3_Pompa_Rpr_State;
                            var CayKarsisi_Kuyu_3_SuSeviye = cayKarsisiNodeRedModel.Kuyu_3_Seviye_Sensor == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 33, SayacId = 0, EtiketId = 342, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_3_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 33, SayacId = 0, EtiketId = 145, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_3_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 33, SayacId = 0, EtiketId = 146, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_3_Pompa_Rpr_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 33, SayacId = 0, EtiketId = 147, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_3_Pompa_Rpr_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 33, SayacId = 0, EtiketId = 148, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_3_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 33, SayacId = 0, EtiketId = 395, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_3_SuSeviye });

                            var CayKarsisi_Kuyu_4_Mode = cayKarsisiNodeRedModel.Kuyu_4_Mode;
                            var CayKarsisi_Kuyu_4_Pompa_Cmd_Run = cayKarsisiNodeRedModel.Kuyu_4_Pompa_Cmd_Run == true ? 1 : 0;
                            var CayKarsisi_Kuyu_4_Pompa_Rpr_Calisiyor = cayKarsisiNodeRedModel.Kuyu_4_Pompa_Rpr_Calisiyor == true ? 1 : 0;
                            var CayKarsisi_Kuyu_4_Pompa_Rpr_Ariza = cayKarsisiNodeRedModel.Kuyu_4_Pompa_Rpr_Ariza == true ? 1 : 0;
                            var CayKarsisi_Kuyu_4_Pompa_Rpr_State = cayKarsisiNodeRedModel.Kuyu_4_Pompa_Rpr_State;
                            var CayKarsisi_Kuyu_4_SuSeviye = cayKarsisiNodeRedModel.Kuyu_4_Seviye_Sensor == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 34, SayacId = 0, EtiketId = 343, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_4_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 34, SayacId = 0, EtiketId = 149, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_4_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 34, SayacId = 0, EtiketId = 150, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_4_Pompa_Rpr_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 34, SayacId = 0, EtiketId = 151, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_4_Pompa_Rpr_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 34, SayacId = 0, EtiketId = 152, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_4_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 34, SayacId = 0, EtiketId = 396, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_4_SuSeviye });

                            var CayKarsisi_Kuyu_5_Mode = cayKarsisiNodeRedModel.Kuyu_5_Mode;
                            var CayKarsisi_Kuyu_5_Pompa_Cmd_Run = cayKarsisiNodeRedModel.Kuyu_5_Pompa_Cmd_Run == true ? 1 : 0;
                            var CayKarsisi_Kuyu_5_Pompa_Rpr_Calisiyor = cayKarsisiNodeRedModel.Kuyu_5_Pompa_Rpr_Calisiyor == true ? 1 : 0;
                            var CayKarsisi_Kuyu_5_Pompa_Rpr_Ariza = cayKarsisiNodeRedModel.Kuyu_5_Pompa_Rpr_Ariza == true ? 1 : 0;
                            var CayKarsisi_Kuyu_5_Pompa_Rpr_State = cayKarsisiNodeRedModel.Kuyu_5_Pompa_Rpr_State;
                            var CayKarsisi_Kuyu_5_SuSeviye = cayKarsisiNodeRedModel.Kuyu_5_Seviye_Sensor == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 35, SayacId = 0, EtiketId = 344, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_5_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 35, SayacId = 0, EtiketId = 153, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_5_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 35, SayacId = 0, EtiketId = 154, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_5_Pompa_Rpr_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 35, SayacId = 0, EtiketId = 155, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_5_Pompa_Rpr_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 35, SayacId = 0, EtiketId = 156, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_5_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 35, SayacId = 0, EtiketId = 397, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_5_SuSeviye });

                            var CayKarsisi_Kuyu_6_Mode = cayKarsisiNodeRedModel.Kuyu_6_Mode;
                            var CayKarsisi_Kuyu_6_Pompa_Cmd_Run = cayKarsisiNodeRedModel.Kuyu_6_Pompa_Cmd_Run == true ? 1 : 0;
                            var CayKarsisi_Kuyu_6_Pompa_Rpr_Calisiyor = cayKarsisiNodeRedModel.Kuyu_6_Pompa_Rpr_Calisiyor == true ? 1 : 0;
                            var CayKarsisi_Kuyu_6_Pompa_Rpr_Ariza = cayKarsisiNodeRedModel.Kuyu_6_Pompa_Rpr_Ariza == true ? 1 : 0;
                            var CayKarsisi_Kuyu_6_Pompa_Rpr_State = cayKarsisiNodeRedModel.Kuyu_6_Pompa_Rpr_State;
                            var CayKarsisi_Kuyu_6_SuSeviye = cayKarsisiNodeRedModel.Kuyu_6_Seviye_Sensor == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 36, SayacId = 0, EtiketId = 345, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_6_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 36, SayacId = 0, EtiketId = 157, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_6_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 36, SayacId = 0, EtiketId = 158, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_6_Pompa_Rpr_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 36, SayacId = 0, EtiketId = 159, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_6_Pompa_Rpr_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 36, SayacId = 0, EtiketId = 160, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_6_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 36, SayacId = 0, EtiketId = 398, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_6_SuSeviye });

                            var CayKarsisi_Kuyu_7_Mode = cayKarsisiNodeRedModel.Kuyu_7_Mode;
                            var CayKarsisi_Kuyu_7_Pompa_Cmd_Run = cayKarsisiNodeRedModel.Kuyu_7_Pompa_Cmd_Run == true ? 1 : 0;
                            var CayKarsisi_Kuyu_7_Pompa_Rpr_Calisiyor = cayKarsisiNodeRedModel.Kuyu_7_Pompa_Rpr_Calisiyor == true ? 1 : 0;
                            var CayKarsisi_Kuyu_7_Pompa_Rpr_Ariza = cayKarsisiNodeRedModel.Kuyu_7_Pompa_Rpr_Ariza == true ? 1 : 0;
                            var CayKarsisi_Kuyu_7_Pompa_Rpr_State = cayKarsisiNodeRedModel.Kuyu_7_Pompa_Rpr_State;
                            var CayKarsisi_Kuyu_7_SuSeviye = cayKarsisiNodeRedModel.Kuyu_7_Seviye_Sensor == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 37, SayacId = 0, EtiketId = 346, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_7_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 37, SayacId = 0, EtiketId = 161, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_7_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 37, SayacId = 0, EtiketId = 162, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_7_Pompa_Rpr_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 37, SayacId = 0, EtiketId = 163, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_7_Pompa_Rpr_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 37, SayacId = 0, EtiketId = 164, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_7_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 37, SayacId = 0, EtiketId = 399, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_7_SuSeviye });

                            var CayKarsisi_Kuyu_8_Mode = cayKarsisiNodeRedModel.Kuyu_8_Mode;
                            var CayKarsisi_Kuyu_8_Pompa_Cmd_Run = cayKarsisiNodeRedModel.Kuyu_8_Pompa_Cmd_Run == true ? 1 : 0;
                            var CayKarsisi_Kuyu_8_Pompa_Rpr_Calisiyor = cayKarsisiNodeRedModel.Kuyu_8_Pompa_Rpr_Calisiyor == true ? 1 : 0;
                            var CayKarsisi_Kuyu_8_Pompa_Rpr_Ariza = cayKarsisiNodeRedModel.Kuyu_8_Pompa_Rpr_Ariza == true ? 1 : 0;
                            var CayKarsisi_Kuyu_8_Pompa_Rpr_State = cayKarsisiNodeRedModel.Kuyu_8_Pompa_Rpr_State;
                            var CayKarsisi_Kuyu_8_SuSeviye = cayKarsisiNodeRedModel.Kuyu_8_Seviye_Sensor == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 38, SayacId = 0, EtiketId = 347, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_8_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 38, SayacId = 0, EtiketId = 165, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_8_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 38, SayacId = 0, EtiketId = 166, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_8_Pompa_Rpr_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 38, SayacId = 0, EtiketId = 167, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_8_Pompa_Rpr_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 38, SayacId = 0, EtiketId = 168, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_8_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 38, SayacId = 0, EtiketId = 400, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_8_SuSeviye });

                            var CayKarsisi_Kuyu_9_Mode = cayKarsisiNodeRedModel.Kuyu_9_Mode;
                            var CayKarsisi_Kuyu_9_Pompa_Cmd_Run = cayKarsisiNodeRedModel.Kuyu_9_Pompa_Cmd_Run == true ? 1 : 0;
                            var CayKarsisi_Kuyu_9_Pompa_Rpr_Calisiyor = cayKarsisiNodeRedModel.Kuyu_9_Pompa_Rpr_Calisiyor == true ? 1 : 0;
                            var CayKarsisi_Kuyu_9_Pompa_Rpr_Ariza = cayKarsisiNodeRedModel.Kuyu_9_Pompa_Rpr_Ariza == true ? 1 : 0;
                            var CayKarsisi_Kuyu_9_Pompa_Rpr_State = cayKarsisiNodeRedModel.Kuyu_9_Pompa_Rpr_State;
                            var CayKarsisi_Kuyu_9_SuSeviye = cayKarsisiNodeRedModel.Kuyu_9_Seviye_Sensor == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 39, SayacId = 0, EtiketId = 348, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_9_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 39, SayacId = 0, EtiketId = 169, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_9_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 39, SayacId = 0, EtiketId = 170, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_9_Pompa_Rpr_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 39, SayacId = 0, EtiketId = 171, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_9_Pompa_Rpr_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 39, SayacId = 0, EtiketId = 172, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_9_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 39, SayacId = 0, EtiketId = 401, KayitZamani = DateTime.Now, Deger = (decimal?)CayKarsisi_Kuyu_9_SuSeviye });

                            var Tank_1_Sens_Min = cayKarsisiNodeRedModel.Tank_1_SensMin == true ? 1 : 0;
                            var Tank_1_Sens_Max = cayKarsisiNodeRedModel.Tank_1_SensMax == true ? 1 : 0;
                            var Tank_1_Sens_Basinc = cayKarsisiNodeRedModel.Tank_1_SensBasinc;
                            var Tank_1_SuSeviye = cayKarsisiNodeRedModel.Tank_1_SuSeviye;
                            var Tank_1_SuSeviye_Yuzdesi = cayKarsisiNodeRedModel.Tank_1_SuSeviye_Yuzde;
                            var Tank_1_SuHacim = cayKarsisiNodeRedModel.Tank_1_SuHacim;
                            var Tank_1_SuHacim_Yuzdesi = cayKarsisiNodeRedModel.Tank_1_SuHacim_Yuzde;
                            var Tank_1_T_TankHacim = cayKarsisiNodeRedModel.Tank_1_T_Tank_Hacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 21, SayacId = 0, EtiketId = 173, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 21, SayacId = 0, EtiketId = 174, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 21, SayacId = 0, EtiketId = 175, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 21, SayacId = 0, EtiketId = 176, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 21, SayacId = 0, EtiketId = 177, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 21, SayacId = 0, EtiketId = 178, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 21, SayacId = 0, EtiketId = 179, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_SuHacim_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 21, SayacId = 0, EtiketId = 180, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_T_TankHacim });

                            var Tank_2_Sens_Min = cayKarsisiNodeRedModel.Tank_2_SensMin == true ? 1 : 0;
                            var Tank_2_Sens_Max = cayKarsisiNodeRedModel.Tank_2_SensMax == true ? 1 : 0;
                            var Tank_2_Sens_Basinc = cayKarsisiNodeRedModel.Tank_2_SensBasinc;
                            var Tank_2_SuSeviye = cayKarsisiNodeRedModel.Tank_2_SuSeviye;
                            var Tank_2_SuSeviye_Yuzdesi = cayKarsisiNodeRedModel.Tank_2_SuSeviye_Yuzde;
                            var Tank_2_SuHacim = cayKarsisiNodeRedModel.Tank_2_SuHacim;
                            var Tank_2_SuHacim_Yuzdesi = cayKarsisiNodeRedModel.Tank_2_SuHacim_Yuzde;
                            var Tank_2_T_TankHacim = cayKarsisiNodeRedModel.Tank_2_T_Tank_Hacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 22, SayacId = 0, EtiketId = 181, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 22, SayacId = 0, EtiketId = 182, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 22, SayacId = 0, EtiketId = 183, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 22, SayacId = 0, EtiketId = 184, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 22, SayacId = 0, EtiketId = 185, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 22, SayacId = 0, EtiketId = 186, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 22, SayacId = 0, EtiketId = 187, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_SuHacim_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 22, SayacId = 0, EtiketId = 188, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_T_TankHacim });

                            var Tank_3_Sens_Min = cayKarsisiNodeRedModel.Tank_3_SensMin == true ? 1 : 0;
                            var Tank_3_Sens_Max = cayKarsisiNodeRedModel.Tank_3_SensMax == true ? 1 : 0;
                            var Tank_3_Sens_Basinc = cayKarsisiNodeRedModel.Tank_3_SensBasinc;
                            var Tank_3_SuSeviye = cayKarsisiNodeRedModel.Tank_3_SuSeviye;
                            var Tank_3_SuSeviye_Yuzdesi = cayKarsisiNodeRedModel.Tank_3_SuSeviye_Yuzde;
                            var Tank_3_SuHacim = cayKarsisiNodeRedModel.Tank_3_SuHacim;
                            var Tank_3_SuHacim_Yuzdesi = cayKarsisiNodeRedModel.Tank_3_SuHacim_Yuzde;
                            var Tank_3_T_TankHacim = cayKarsisiNodeRedModel.Tank_3_T_Tank_Hacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 70, SayacId = 0, EtiketId = 412, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 70, SayacId = 0, EtiketId = 413, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 70, SayacId = 0, EtiketId = 414, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 70, SayacId = 0, EtiketId = 415, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 70, SayacId = 0, EtiketId = 416, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 70, SayacId = 0, EtiketId = 417, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 70, SayacId = 0, EtiketId = 418, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_SuHacim_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 70, SayacId = 0, EtiketId = 419, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_T_TankHacim });

                            var DM_Kuyu_1_Connected = cayKarsisiNodeRedModel.DM_Kuyu_1_Connected == true ? 1 : 0;
                            var DM_Kuyu_1_Total = cayKarsisiNodeRedModel.DM_Kuyu_1_Total;
                            var DM_Kuyu_1_Debi = cayKarsisiNodeRedModel.DM_Kuyu_1_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 16, EtiketId = 189, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_1_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 16, EtiketId = 190, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_1_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 16, EtiketId = 191, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_1_Debi });

                            var DM_Kuyu_2_Connected = cayKarsisiNodeRedModel.DM_Kuyu_2_Connected == true ? 1 : 0;
                            var DM_Kuyu_2_Total = cayKarsisiNodeRedModel.DM_Kuyu_2_Total;
                            var DM_Kuyu_2_Debi = cayKarsisiNodeRedModel.DM_Kuyu_2_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 17, EtiketId = 192, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_2_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 17, EtiketId = 193, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_2_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 17, EtiketId = 194, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_2_Debi });

                            var DM_Kuyu_3_Connected = cayKarsisiNodeRedModel.DM_Kuyu_3_Connected == true ? 1 : 0;
                            var DM_Kuyu_3_Total = cayKarsisiNodeRedModel.DM_Kuyu_3_Total;
                            var DM_Kuyu_3_Debi = cayKarsisiNodeRedModel.DM_Kuyu_3_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 18, EtiketId = 195, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_3_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 18, EtiketId = 196, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_3_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 18, EtiketId = 197, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_3_Debi });

                            var DM_Kuyu_4_Connected = cayKarsisiNodeRedModel.DM_Kuyu_4_Connected == true ? 1 : 0;
                            var DM_Kuyu_4_Total = cayKarsisiNodeRedModel.DM_Kuyu_4_Total;
                            var DM_Kuyu_4_Debi = cayKarsisiNodeRedModel.DM_Kuyu_4_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 19, EtiketId = 198, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_4_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 19, EtiketId = 199, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_4_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 19, EtiketId = 200, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_4_Debi });

                            var DM_Kuyu_5_Connected = cayKarsisiNodeRedModel.DM_Kuyu_5_Connected == true ? 1 : 0;
                            var DM_Kuyu_5_Total = cayKarsisiNodeRedModel.DM_Kuyu_5_Total;
                            var DM_Kuyu_5_Debi = cayKarsisiNodeRedModel.DM_Kuyu_5_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 20, EtiketId = 201, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_5_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 20, EtiketId = 202, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_5_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 20, EtiketId = 203, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_5_Debi });

                            var DM_Kuyu_6_Connected = cayKarsisiNodeRedModel.DM_Kuyu_10_Connected == true ? 1 : 0;
                            var DM_Kuyu_6_Total = cayKarsisiNodeRedModel.DM_Kuyu_10_Total;
                            var DM_Kuyu_6_Debi = cayKarsisiNodeRedModel.DM_Kuyu_10_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 58, EtiketId = 437, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_6_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 58, EtiketId = 438, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_6_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 58, EtiketId = 439, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_6_Debi });

                            var DM_Kuyu_7_Connected = cayKarsisiNodeRedModel.DM_Kuyu_6_7_Connected == true ? 1 : 0;
                            var DM_Kuyu_7_Total = cayKarsisiNodeRedModel.DM_Kuyu_6_7_Total;
                            var DM_Kuyu_7_Debi = cayKarsisiNodeRedModel.DM_Kuyu_6_7_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 59, EtiketId = 440, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_7_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 59, EtiketId = 441, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_7_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 59, EtiketId = 442, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_7_Debi });

                            var DM_Kuyu_8_9_Connected = cayKarsisiNodeRedModel.DM_Kuyu_8_9_Connected == true ? 1 : 0;
                            var DM_Kuyu_8_9_Total = cayKarsisiNodeRedModel.DM_Kuyu_8_9_Total;
                            var DM_Kuyu_8_9_Debi = cayKarsisiNodeRedModel.DM_Kuyu_8_9_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 22, EtiketId = 207, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_8_9_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 22, EtiketId = 208, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_8_9_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 22, EtiketId = 209, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_8_9_Debi });

                            var DM_Rahmiye_Connected = cayKarsisiNodeRedModel.DM_Rahmiye_Connected == true ? 1 : 0;
                            var DM_Rahmiye_Total = cayKarsisiNodeRedModel.DM_Rahmiye_Total;
                            var DM_Rahmiye_Debi = cayKarsisiNodeRedModel.DM_Rahmiye_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 15, EtiketId = 210, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Rahmiye_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 15, EtiketId = 211, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Rahmiye_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 15, EtiketId = 212, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Rahmiye_Debi });

                            var DM_Pastorize_Connected = cayKarsisiNodeRedModel.DM_Pastorize_Connected == true ? 1 : 0;
                            var DM_Pastorize_Total = cayKarsisiNodeRedModel.DM_Pastorize_Total;
                            var DM_Pastorize_Debi = cayKarsisiNodeRedModel.DM_Pastorize_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 24, EtiketId = 213, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Pastorize_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 24, EtiketId = 214, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Pastorize_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 24, EtiketId = 215, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Pastorize_Debi });

                            var DM_IleriIslem_Connected = cayKarsisiNodeRedModel.DM_IleriIslem_Connected == true ? 1 : 0;
                            var DM_IleriIslem_Total = cayKarsisiNodeRedModel.DM_IleriIslem_Total;
                            var DM_IleriIslem_Debi = cayKarsisiNodeRedModel.DM_IleriIslem_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 25, EtiketId = 216, KayitZamani = DateTime.Now, Deger = (decimal?)DM_IleriIslem_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 25, EtiketId = 217, KayitZamani = DateTime.Now, Deger = (decimal?)DM_IleriIslem_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 25, EtiketId = 218, KayitZamani = DateTime.Now, Deger = (decimal?)DM_IleriIslem_Debi });

                            var Hidrofor_Kesimhane_Cmd_Calis = cayKarsisiNodeRedModel.Hidrofor_Kesimhane_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Kesimhane_Rpr_Mode = cayKarsisiNodeRedModel.Hidrofor_Kesimhane_Rpr_Mode;
                            var Hidrofor_Kesimhane_Rpr_State = cayKarsisiNodeRedModel.Hidrofor_Kesimhane_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 27, SayacId = 0, EtiketId = 219, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Kesimhane_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 27, SayacId = 0, EtiketId = 241, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Kesimhane_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 27, SayacId = 0, EtiketId = 220, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Kesimhane_Rpr_State });

                            var Hidrofor_Kesimhane_Motor_1_Calisiyor = cayKarsisiNodeRedModel.Hidrofor_Kesimhane_Motor_1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Kesimhane_Motor_1_Ariza = cayKarsisiNodeRedModel.Hidrofor_Kesimhane_Motor_1_Ariza == true ? 1 : 0;
                            var Hidrofor_Kesimhane_Motor_1_State = cayKarsisiNodeRedModel.Hidrofor_Kesimhane_Motor_1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 28, SayacId = 0, EtiketId = 221, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Kesimhane_Motor_1_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 28, SayacId = 0, EtiketId = 222, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Kesimhane_Motor_1_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 28, SayacId = 0, EtiketId = 223, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Kesimhane_Motor_1_State });

                            var Hidrofor_Kesimhane_Motor_2_Calisiyor = cayKarsisiNodeRedModel.Hidrofor_Kesimhane_Motor_2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Kesimhane_Motor_2_Ariza = cayKarsisiNodeRedModel.Hidrofor_Kesimhane_Motor_2_Ariza == true ? 1 : 0;
                            var Hidrofor_Kesimhane_Motor_2_State = cayKarsisiNodeRedModel.Hidrofor_Kesimhane_Motor_2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 29, SayacId = 0, EtiketId = 224, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Kesimhane_Motor_2_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 29, SayacId = 0, EtiketId = 225, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Kesimhane_Motor_2_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 29, SayacId = 0, EtiketId = 226, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Kesimhane_Motor_2_State });

                            var Hidrofor_Kesimhane_Motor_3_Calisiyor = cayKarsisiNodeRedModel.Hidrofor_Kesimhane_Motor_3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Kesimhane_Motor_3_Ariza = cayKarsisiNodeRedModel.Hidrofor_Kesimhane_Motor_3_Ariza == true ? 1 : 0;
                            var Hidrofor_Kesimhane_Motor_3_State = cayKarsisiNodeRedModel.Hidrofor_Kesimhane_Motor_3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 30, SayacId = 0, EtiketId = 227, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Kesimhane_Motor_3_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 30, SayacId = 0, EtiketId = 228, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Kesimhane_Motor_3_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 30, SayacId = 0, EtiketId = 229, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Kesimhane_Motor_3_State });

                            var Hidrofor_Pastorize_Cmd_Calis = cayKarsisiNodeRedModel.Hidrofor_2_Kesimhane_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Pastorize_Rpr_Mode = cayKarsisiNodeRedModel.Hidrofor_2_Kesimhane_Rpr_Mode;
                            var Hidrofor_Pastorize_Rpr_State = cayKarsisiNodeRedModel.Hidrofor_2_Kesimhane_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 23, SayacId = 0, EtiketId = 230, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Pastorize_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 23, SayacId = 0, EtiketId = 242, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Pastorize_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 23, SayacId = 0, EtiketId = 231, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Pastorize_Rpr_State });

                            var Hidrofor_Pastorize_Motor_1_Calisiyor = cayKarsisiNodeRedModel.Hidrofor_2_Kesimhane_Motor_1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Pastorize_Motor_1_Ariza = cayKarsisiNodeRedModel.Hidrofor_2_Kesimhane_Motor_1_Ariza == true ? 1 : 0;
                            var Hidrofor_Pastorize_Motor_1_State = cayKarsisiNodeRedModel.Hidrofor_2_Kesimhane_Motor_1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 24, SayacId = 0, EtiketId = 232, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Pastorize_Motor_1_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 24, SayacId = 0, EtiketId = 233, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Pastorize_Motor_1_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 24, SayacId = 0, EtiketId = 234, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Pastorize_Motor_1_State });

                            var Hidrofor_Pastorize_Motor_2_Calisiyor = cayKarsisiNodeRedModel.Hidrofor_2_Kesimhane_Motor_2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Pastorize_Motor_2_Ariza = cayKarsisiNodeRedModel.Hidrofor_2_Kesimhane_Motor_2_Ariza == true ? 1 : 0;
                            var Hidrofor_Pastorize_Motor_2_State = cayKarsisiNodeRedModel.Hidrofor_2_Kesimhane_Motor_2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 25, SayacId = 0, EtiketId = 235, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Pastorize_Motor_2_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 25, SayacId = 0, EtiketId = 236, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Pastorize_Motor_2_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 25, SayacId = 0, EtiketId = 237, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Pastorize_Motor_2_State });

                            var Hidrofor_Pastorize_Motor_3_Calisiyor = cayKarsisiNodeRedModel.Hidrofor_2_Kesimhane_Motor_3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Pastorize_Motor_3_Ariza = cayKarsisiNodeRedModel.Hidrofor_2_Kesimhane_Motor_3_Ariza == true ? 1 : 0;
                            var Hidrofor_Pastorize_Motor_3_State = cayKarsisiNodeRedModel.Hidrofor_2_Kesimhane_Motor_3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 26, SayacId = 0, EtiketId = 238, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Pastorize_Motor_3_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 26, SayacId = 0, EtiketId = 239, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Pastorize_Motor_3_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 26, SayacId = 0, EtiketId = 240, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Pastorize_Motor_3_State });

                            var SebekeElektrikVar = cayKarsisiNodeRedModel.SebekeElektrikVar == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 0, EtiketId = 243, KayitZamani = DateTime.Now, Deger = (decimal?)SebekeElektrikVar });
                        }
                    }
                    else
                    {
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 0, EtiketId = 132, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = 14, SuKaynakId = 0, SayacId = 0, EtiketId = 133, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    }
                }
            }
            return etiketLogs;
        }
        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsertOSBYemAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 11;
            var now = DateTime.Now;
            List<EtiketLogYeni> etiketLogs = new List<EtiketLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model == null)
                {
                    var plcRead = 0;
                    var plcConnected = 1;
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 0, EtiketId = 247, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 0, EtiketId = 248, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    return etiketLogs;
                }
                if (model != null)
                {
                    var plcRead = model.Durum;
                    var plcConnected = model.Durum;

                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            OSBYemNodeRedModel osbYemNodeRedModel = JsonSerializer.Deserialize<OSBYemNodeRedModel>(model.Veriler);

                            var byteDensity = 0;
                            var pingAverage = 0;
                            var lastPackageLength = 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 0, EtiketId = 247, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 0, EtiketId = 248, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 0, EtiketId = 249, KayitZamani = DateTime.Now, Deger = (decimal?)byteDensity });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 0, EtiketId = 250, KayitZamani = DateTime.Now, Deger = (decimal?)pingAverage });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 0, EtiketId = 251, KayitZamani = DateTime.Now, Deger = (decimal?)lastPackageLength });

                            if (plcConnected == 0) return etiketLogs;
                            //if (plcRead == 0) return etiketLogs;

                            var Kuyu_1_Pompa_Cmd_Run = osbYemNodeRedModel.Kuyu_1_Pompa_Cmd_Run == true ? 1 : 0;
                            var Kuyu_1_Pompa_Rpr_Calisiyor = osbYemNodeRedModel.Kuyu_1_Pompa_Rpr_Calisiyor == true ? 1 : 0;
                            var Kuyu_1_Pompa_Rpr_Ariza = osbYemNodeRedModel.Kuyu_1_Pompa_Rpr_Ariza == true ? 1 : 0;
                            var Kuyu_1_Pompa_Rpr_State = osbYemNodeRedModel.Kuyu_1_Pompa_Rpr_State;
                            var Kuyu_1_SuSeviye = osbYemNodeRedModel.Kuyu_1_SuSeviye == true ? 1 : 0;
                            var Kuyu_1_Mode = osbYemNodeRedModel.Kuyu_1_Mode;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 45, SayacId = 0, EtiketId = 252, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_1_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 45, SayacId = 0, EtiketId = 253, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_1_Pompa_Rpr_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 45, SayacId = 0, EtiketId = 254, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_1_Pompa_Rpr_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 45, SayacId = 0, EtiketId = 255, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_1_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 45, SayacId = 0, EtiketId = 291, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_1_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 45, SayacId = 0, EtiketId = 410, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_1_Mode });

                            var Kuyu_2_Pompa_Cmd_Run = osbYemNodeRedModel.Kuyu_2_Pompa_Cmd_Run == true ? 1 : 0;
                            var Kuyu_2_Pompa_Rpr_Kontaktor = osbYemNodeRedModel.Kuyu_2_Pompa_Rpr_Calisiyor == true ? 1 : 0;
                            var Kuyu_2_Pompa_Rpr_TermikOK = osbYemNodeRedModel.Kuyu_2_Pompa_Rpr_Ariza == true ? 1 : 0;
                            var Kuyu_2_Pompa_Rpr_State = osbYemNodeRedModel.Kuyu_2_Pompa_Rpr_State;
                            var Kuyu_2_SuSeviye = osbYemNodeRedModel.Kuyu_2_SuSeviye == true ? 1 : 0;
                            var Kuyu_2_Mode = osbYemNodeRedModel.Kuyu_2_Mode;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 46, SayacId = 0, EtiketId = 256, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_2_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 46, SayacId = 0, EtiketId = 257, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_2_Pompa_Rpr_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 46, SayacId = 0, EtiketId = 258, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_2_Pompa_Rpr_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 46, SayacId = 0, EtiketId = 259, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_2_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 46, SayacId = 0, EtiketId = 292, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_2_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 46, SayacId = 0, EtiketId = 411, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_2_Mode });

                            var Tank_Sens_Min = osbYemNodeRedModel.Tank_Sens_Min == true ? 1 : 0;
                            var Tank_Sens_Max = osbYemNodeRedModel.Tank_Sens_Max == true ? 1 : 0;
                            var Tank_Sens_Basinc = osbYemNodeRedModel.Tank_SensBasinc;
                            var Tank_SuSeviye = osbYemNodeRedModel.Tank_SuSeviye;
                            var Tank_SuSeviye_Yuzdesi = osbYemNodeRedModel.Tank_SuSeviye_Yuzde;
                            var Tank_SuHacim = osbYemNodeRedModel.Tank_SuHacim;
                            var Tank_SuHacim_Yuzdesi = osbYemNodeRedModel.Tank_SuHacim_Yuzde;
                            var Tank_T_TankHacim = osbYemNodeRedModel.Tank_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 47, SayacId = 0, EtiketId = 260, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 47, SayacId = 0, EtiketId = 261, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 47, SayacId = 0, EtiketId = 262, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 47, SayacId = 0, EtiketId = 263, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 47, SayacId = 0, EtiketId = 264, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 47, SayacId = 0, EtiketId = 265, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 47, SayacId = 0, EtiketId = 266, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 47, SayacId = 0, EtiketId = 267, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_T_TankHacim });

                            var YanginTank_Sens_Min = osbYemNodeRedModel.YanginTank_Sens_Min == true ? 1 : 0;
                            var YanginTank_Sens_Max = osbYemNodeRedModel.YanginTank_Sens_Max == true ? 1 : 0;
                            var YanginTank_Sens_Basinc = osbYemNodeRedModel.YanginTank_SensBasinc;
                            var YanginTank_SuSeviye = osbYemNodeRedModel.YanginTank_SuSeviye;
                            var YanginTank_SuSeviye_Yuzdesi = osbYemNodeRedModel.YanginTank_SuSeviye_Yuzde;
                            var YanginTank_SuHacim = osbYemNodeRedModel.YanginTank_SuHacim;
                            var YanginTank_SuHacim_Yuzdesi = osbYemNodeRedModel.YanginTank_SuHacim_Yuzde;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 68, SayacId = 0, EtiketId = 420, KayitZamani = DateTime.Now, Deger = (decimal?)YanginTank_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 68, SayacId = 0, EtiketId = 421, KayitZamani = DateTime.Now, Deger = (decimal?)YanginTank_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 68, SayacId = 0, EtiketId = 422, KayitZamani = DateTime.Now, Deger = (decimal?)YanginTank_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 68, SayacId = 0, EtiketId = 423, KayitZamani = DateTime.Now, Deger = (decimal?)YanginTank_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 68, SayacId = 0, EtiketId = 424, KayitZamani = DateTime.Now, Deger = (decimal?)YanginTank_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 68, SayacId = 0, EtiketId = 425, KayitZamani = DateTime.Now, Deger = (decimal?)YanginTank_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 68, SayacId = 0, EtiketId = 426, KayitZamani = DateTime.Now, Deger = (decimal?)YanginTank_SuHacim_Yuzdesi });

                            var DM_Kuyu_1_Connected = osbYemNodeRedModel.DM_Kuyu_1_Connected == true ? 1 : 0;
                            var DM_Kuyu_1_Total = osbYemNodeRedModel.DM_Kuyu_1_Total;
                            var DM_Kuyu_1_Debi = osbYemNodeRedModel.DM_Kuyu_1_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 26, EtiketId = 268, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_1_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 26, EtiketId = 269, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_1_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 26, EtiketId = 270, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_1_Debi });

                            var DM_Kuyu_2_Connected = osbYemNodeRedModel.DM_Kuyu_2_Connected == true ? 1 : 0;
                            var DM_Kuyu_2_Total = osbYemNodeRedModel.DM_Kuyu_2_Total;
                            var DM_Kuyu_2_Debi = osbYemNodeRedModel.DM_Kuyu_2_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 27, EtiketId = 271, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_2_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 27, EtiketId = 272, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_2_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 27, EtiketId = 273, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_2_Debi });

                            var DM_Uretim_Connected = osbYemNodeRedModel.DM_Kuyu_Uretim_Connected == true ? 1 : 0;
                            var DM_Uretim_Total = osbYemNodeRedModel.DM_Kuyu_Uretim_Total;
                            var DM_Uretim_Debi = osbYemNodeRedModel.DM_Kuyu_Uretim_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 28, EtiketId = 274, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 28, EtiketId = 275, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 28, EtiketId = 276, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim_Debi });

                            var DM_Kazan_Connected = osbYemNodeRedModel.DM_Kuyu_Kazan_Connected == true ? 1 : 0;
                            var DM_Kazan_Total = osbYemNodeRedModel.DM_Kuyu_Kazan_Total;
                            var DM_Kazan_Debi = osbYemNodeRedModel.DM_Kuyu_Kazan_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 29, EtiketId = 277, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kazan_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 29, EtiketId = 278, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kazan_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 29, EtiketId = 279, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kazan_Debi });

                            var Hidrofor_Cmd_Calis = osbYemNodeRedModel.Hidrofor_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Rpr_State = osbYemNodeRedModel.Hidrofor_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 48, SayacId = 0, EtiketId = 280, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 48, SayacId = 0, EtiketId = 281, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Rpr_State });

                            var Hidrofor_Motor_1_Calisiyor = osbYemNodeRedModel.Hidrofor_Motor_1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_1_Ariza = osbYemNodeRedModel.Hidrofor_Motor_1_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_1_State = osbYemNodeRedModel.Hidrofor_Motor_1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 49, SayacId = 0, EtiketId = 282, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 49, SayacId = 0, EtiketId = 283, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 49, SayacId = 0, EtiketId = 284, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_State });

                            var Hidrofor_Motor_2_Calisiyor = osbYemNodeRedModel.Hidrofor_Motor_2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_2_Ariza = osbYemNodeRedModel.Hidrofor_Motor_2_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_2_State = osbYemNodeRedModel.Hidrofor_Motor_2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 50, SayacId = 0, EtiketId = 285, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 50, SayacId = 0, EtiketId = 286, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 50, SayacId = 0, EtiketId = 287, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_State });

                            var Hidrofor_Motor_3_Calisiyor = osbYemNodeRedModel.Hidrofor_Motor_3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_3_Ariza = osbYemNodeRedModel.Hidrofor_Motor_3_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_3_State = osbYemNodeRedModel.Hidrofor_Motor_3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 51, SayacId = 0, EtiketId = 288, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 51, SayacId = 0, EtiketId = 289, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 51, SayacId = 0, EtiketId = 290, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_State });

                            var Rio_Kuyu_1_Connected = osbYemNodeRedModel.Rio_1_Connected == true ? 1 : 0;
                            var Rio_Kuyu_1_RSSI = osbYemNodeRedModel.Rio_1_RSSI;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 45, SayacId = 0, EtiketId = 293, KayitZamani = DateTime.Now, Deger = (decimal?)Rio_Kuyu_1_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 45, SayacId = 0, EtiketId = 294, KayitZamani = DateTime.Now, Deger = (decimal?)Rio_Kuyu_1_RSSI });

                            var YanginTank_PompaSensBasinc = osbYemNodeRedModel.YanginTank_PompaSensBasinc;
                            var YanginTank_MotorAriza = osbYemNodeRedModel.YanginTank_MotorAriza == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 69, SayacId = 0, EtiketId = 428, KayitZamani = DateTime.Now, Deger = (decimal?)YanginTank_PompaSensBasinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 69, SayacId = 0, EtiketId = 429, KayitZamani = DateTime.Now, Deger = (decimal?)YanginTank_MotorAriza });
                        }
                    }
                    else
                    {
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 0, EtiketId = 247, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = 11, SuKaynakId = 0, SayacId = 0, EtiketId = 248, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    }
                }
            }
            return etiketLogs;
        }
        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsertViolAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 15;
            var now = DateTime.Now;
            List<EtiketLogYeni> etiketLogs = new List<EtiketLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model == null)
                {
                    var plcRead = 0;
                    var plcConnected = 1;
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 0, EtiketId = 296, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 0, EtiketId = 297, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    return etiketLogs;
                }
                if (model != null)
                {
                    var plcRead = model.Durum;
                    var plcConnected = model.Durum;

                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            ViolNodeRedModel violNodeRedModel = JsonSerializer.Deserialize<ViolNodeRedModel>(model.Veriler);

                            var byteDensity = 0;
                            var pingAverage = 0;
                            var lastPackageLength = 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 0, EtiketId = 296, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 0, EtiketId = 297, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 0, EtiketId = 298, KayitZamani = DateTime.Now, Deger = (decimal?)byteDensity });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 0, EtiketId = 299, KayitZamani = DateTime.Now, Deger = (decimal?)pingAverage });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 0, EtiketId = 300, KayitZamani = DateTime.Now, Deger = (decimal?)lastPackageLength });

                            if (plcConnected == 0) return etiketLogs;
                            //if (plcRead == 0) return etiketLogs;

                            var Kuyu_1_Mode = violNodeRedModel.Kuyu_1_Mode;
                            var Kuyu_1_Pompa_Cmd_Run = violNodeRedModel.Kuyu_1_Pompa_Cmd_Run == true ? 1 : 0;
                            var Kuyu_1_Pompa_Rpr_Calisiyor = violNodeRedModel.Kuyu_1_Pompa_Rpr_Calisiyor == true ? 1 : 0;
                            var Kuyu_1_Pompa_Rpr_Ariza = violNodeRedModel.Kuyu_1_Pompa_Rpr_Ariza == true ? 1 : 0;
                            var Kuyu_1_Pompa_Rpr_State = violNodeRedModel.Kuyu_1_Pompa_Rpr_State;
                            var Kuyu_1_SuSeviye = violNodeRedModel.Kuyu_1_SuSeviye == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 54, SayacId = 0, EtiketId = 301, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_1_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 54, SayacId = 0, EtiketId = 302, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_1_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 54, SayacId = 0, EtiketId = 303, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_1_Pompa_Rpr_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 54, SayacId = 0, EtiketId = 304, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_1_Pompa_Rpr_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 54, SayacId = 0, EtiketId = 305, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_1_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 54, SayacId = 0, EtiketId = 402, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_1_SuSeviye });

                            var Kuyu_2_Mode = violNodeRedModel.Kuyu_2_Mode;
                            var Kuyu_2_Pompa_Cmd_Run = violNodeRedModel.Kuyu_2_Pompa_Cmd_Run == true ? 1 : 0;
                            var Kuyu_2_Pompa_Rpr_Calisiyor = violNodeRedModel.Kuyu_2_Pompa_Rpr_Calisiyor == true ? 1 : 0;
                            var Kuyu_2_Pompa_Rpr_Ariza = violNodeRedModel.Kuyu_2_Pompa_Rpr_Ariza == true ? 1 : 0;
                            var Kuyu_2_Pompa_Rpr_State = violNodeRedModel.Kuyu_2_Pompa_Rpr_State;
                            var Kuyu_2_SuSeviye = violNodeRedModel.Kuyu_2_SuSeviye == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 55, SayacId = 0, EtiketId = 306, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_2_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 55, SayacId = 0, EtiketId = 307, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_2_Pompa_Cmd_Run });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 55, SayacId = 0, EtiketId = 308, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_2_Pompa_Rpr_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 55, SayacId = 0, EtiketId = 309, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_2_Pompa_Rpr_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 55, SayacId = 0, EtiketId = 310, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_2_Pompa_Rpr_State });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 55, SayacId = 0, EtiketId = 403, KayitZamani = DateTime.Now, Deger = (decimal?)Kuyu_2_SuSeviye });

                            var Tank_Sens_Min = violNodeRedModel.Tank_Sens_Min == true ? 1 : 0;
                            var Tank_Sens_Max = violNodeRedModel.Tank_Sens_Max == true ? 1 : 0;
                            var Tank_Sens_Basinc = violNodeRedModel.Tank_SensBasinc;
                            var Tank_SuSeviye = violNodeRedModel.Tank_SuSeviye;
                            var Tank_SuSeviye_Yuzdesi = violNodeRedModel.Tank_SuSeviye_Yuzde;
                            var Tank_SuHacim = violNodeRedModel.Tank_SuHacim;
                            var Tank_SuHacim_Yuzdesi = violNodeRedModel.Tank_SuHacim_Yuzde;
                            var Tank_T_TankHacim = violNodeRedModel.Tank_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 56, SayacId = 0, EtiketId = 311, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 56, SayacId = 0, EtiketId = 312, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 56, SayacId = 0, EtiketId = 313, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 56, SayacId = 0, EtiketId = 314, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 56, SayacId = 0, EtiketId = 315, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 56, SayacId = 0, EtiketId = 316, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 56, SayacId = 0, EtiketId = 317, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 56, SayacId = 0, EtiketId = 318, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_T_TankHacim });

                            var DM_Kuyu_1_Connected = violNodeRedModel.DM_Kuyu_1_Connected == true ? 1 : 0;
                            var DM_Kuyu_1_Total = violNodeRedModel.DM_Kuyu_1_Total;
                            var DM_Kuyu_1_Debi = violNodeRedModel.DM_Kuyu_1_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 31, EtiketId = 319, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_1_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 31, EtiketId = 320, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_1_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 31, EtiketId = 321, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_1_Debi });

                            var DM_Kuyu_2_Connected = violNodeRedModel.DM_Kuyu_2_Connected == true ? 1 : 0;
                            var DM_Kuyu_2_Total = violNodeRedModel.DM_Kuyu_2_Total;
                            var DM_Kuyu_2_Debi = violNodeRedModel.DM_Kuyu_2_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 32, EtiketId = 322, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_2_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 32, EtiketId = 323, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_2_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 32, EtiketId = 324, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu_2_Debi });

                            var DM_Uretim_Connected = violNodeRedModel.DM_Uretim_Connected == true ? 1 : 0;
                            var DM_Uretim_Total = violNodeRedModel.DM_Uretim_Total;
                            var DM_Uretim_Debi = violNodeRedModel.DM_Uretim_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 33, EtiketId = 325, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 33, EtiketId = 326, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 33, EtiketId = 327, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim_Debi });

                            var Hidrofor_Cmd_Calis = violNodeRedModel.Hidrofor_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Rpr_Mode = violNodeRedModel.Hidrofor_Rpr_Mode;
                            var Hidrofor_Rpr_State = violNodeRedModel.Hidrofor_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 57, SayacId = 0, EtiketId = 328, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 57, SayacId = 0, EtiketId = 329, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 57, SayacId = 0, EtiketId = 330, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Rpr_State });

                            var Hidrofor_Motor_1_Calisiyor = violNodeRedModel.Hidrofor_Motor_1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_1_Ariza = violNodeRedModel.Hidrofor_Motor_1_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_1_State = violNodeRedModel.Hidrofor_Motor_1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 58, SayacId = 0, EtiketId = 331, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 58, SayacId = 0, EtiketId = 332, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 58, SayacId = 0, EtiketId = 333, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_State });

                            var Hidrofor_Motor_2_Calisiyor = violNodeRedModel.Hidrofor_Motor_2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_2_Ariza = violNodeRedModel.Hidrofor_Motor_2_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_2_State = violNodeRedModel.Hidrofor_Motor_2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 59, SayacId = 0, EtiketId = 334, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 59, SayacId = 0, EtiketId = 335, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 59, SayacId = 0, EtiketId = 336, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_State });

                            var Hidrofor_Motor_3_Calisiyor = violNodeRedModel.Hidrofor_Motor_3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_3_Ariza = violNodeRedModel.Hidrofor_Motor_3_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_3_State = violNodeRedModel.Hidrofor_Motor_3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 60, SayacId = 0, EtiketId = 337, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 60, SayacId = 0, EtiketId = 338, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 60, SayacId = 0, EtiketId = 339, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_State });
                        }
                    }
                    else
                    {
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 0, EtiketId = 296, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = 15, SuKaynakId = 0, SayacId = 0, EtiketId = 297, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    }
                }
            }
            return etiketLogs;
        }
        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsertKayislarAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 22;
            var now = DateTime.Now;
            List<EtiketLogYeni> etiketLogs = new List<EtiketLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model == null)
                {
                    var plcRead = 0;
                    var plcConnected = 0;
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 361, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 362, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    return etiketLogs;
                }
                if (model != null)
                {
                    var plcRead = model.Durum;
                    var plcConnected = model.Durum;

                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            KayislarNodeRedModel kayislarNodeRedModel = JsonSerializer.Deserialize<KayislarNodeRedModel>(model.Veriler);

                            var byteDensity = 0;
                            var pingAverage = 0;
                            var lastPackageLength = 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 361, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 362, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 363, KayitZamani = DateTime.Now, Deger = (decimal?)byteDensity });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 364, KayitZamani = DateTime.Now, Deger = (decimal?)pingAverage });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 365, KayitZamani = DateTime.Now, Deger = (decimal?)lastPackageLength });

                            if (plcConnected == 0) return etiketLogs;
                            //if (plcRead == 0) return etiketLogs;

                            var Tank_Sens_Min = kayislarNodeRedModel.TankSens_Min == true ? 1 : 0;
                            var Tank_Sens_Max = kayislarNodeRedModel.TankSens_Max == true ? 1 : 0;
                            var Tank_Sens_Basinc = kayislarNodeRedModel.TankSensBasinc;
                            var Tank_SuSeviye = kayislarNodeRedModel.TankSuSeviye;
                            var Tank_SuSeviye_Yuzdesi = kayislarNodeRedModel.TankSuSeviye_Yuzde;
                            var Tank_SuHacim = kayislarNodeRedModel.TankSuHacim;
                            var Tank_SuHacim_Yuzdesi = kayislarNodeRedModel.TankSuHacim_Yuzde;
                            var Tank_T_TankHacim = kayislarNodeRedModel.T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 10, SayacId = 0, EtiketId = 366, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 10, SayacId = 0, EtiketId = 367, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 10, SayacId = 0, EtiketId = 368, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 10, SayacId = 0, EtiketId = 369, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 10, SayacId = 0, EtiketId = 370, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 10, SayacId = 0, EtiketId = 371, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 10, SayacId = 0, EtiketId = 372, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 10, SayacId = 0, EtiketId = 373, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_T_TankHacim });

                            var DM_Kayislar_Connected = kayislarNodeRedModel.DM_Kayislar_Connected == true ? 1 : 0;
                            var DM_Kayislar_Total = kayislarNodeRedModel.DM_Kayislar_Total;
                            var DM_Kayislar_Debi = kayislarNodeRedModel.DM_Kayislar_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 7, EtiketId = 374, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kayislar_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 7, EtiketId = 375, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kayislar_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 7, EtiketId = 376, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kayislar_Debi });

                            var DM_Yarkahane_Connected = kayislarNodeRedModel.DM_Yarkahane_Connected == true ? 1 : 0;
                            var DM_Yarkahane_Total = kayislarNodeRedModel.DM_Yarkahane_Total;
                            var DM_Yarkahane_Debi = kayislarNodeRedModel.DM_Yarkahane_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 8, EtiketId = 377, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yarkahane_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 8, EtiketId = 378, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yarkahane_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 8, EtiketId = 379, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yarkahane_Debi });

                            var Vana_Acik = kayislarNodeRedModel.Vana_Acik == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 380, KayitZamani = DateTime.Now, Deger = (decimal?)Vana_Acik });

                            var Hidrofor_Cmd_Calis = kayislarNodeRedModel.Hidrofor_Kumesler_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Rpr_State = kayislarNodeRedModel.Hidrofor_Kumesler_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 64, SayacId = 0, EtiketId = 381, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 64, SayacId = 0, EtiketId = 382, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Rpr_State });

                            var Hidrofor_Motor_1_Calisiyor = kayislarNodeRedModel.Hidrofor_Kumesler_Motor_1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_1_Ariza = kayislarNodeRedModel.Hidrofor_Kumesler_Motor_1_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_1_State = kayislarNodeRedModel.Hidrofor_Kumesler_Motor_1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 65, SayacId = 0, EtiketId = 383, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 65, SayacId = 0, EtiketId = 384, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 65, SayacId = 0, EtiketId = 385, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_State });

                            var Hidrofor_Motor_2_Calisiyor = kayislarNodeRedModel.Hidrofor_Kumesler_Motor_2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_2_Ariza = kayislarNodeRedModel.Hidrofor_Kumesler_Motor_2_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_2_State = kayislarNodeRedModel.Hidrofor_Kumesler_Motor_2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 66, SayacId = 0, EtiketId = 386, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 66, SayacId = 0, EtiketId = 387, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 66, SayacId = 0, EtiketId = 388, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_State });
                        }
                    }
                    else
                    {
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 361, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 362, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    }
                }
            }
            return etiketLogs;
        }
        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsertYumusakSuAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 62;
            var now = DateTime.Now;
            List<EtiketLogYeni> etiketLogs = new List<EtiketLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model == null)
                {
                    var plcRead = 0;
                    var plcConnected = 1;
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 473, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 472, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    return etiketLogs;
                }
                if (model != null)
                {
                    var plcRead = model.Durum;
                    var plcConnected = model.Durum;

                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            YumusakSuNodeRedModel yumusakSuNodeRedModel = JsonSerializer.Deserialize<YumusakSuNodeRedModel>(model.Veriler);

                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 473, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 472, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });

                            if (plcConnected == 0) return etiketLogs;
                            //if (plcRead == 0) return etiketLogs;

                            var Tank_Sens_Min = yumusakSuNodeRedModel.Tank_1_Sens_Min == true ? 1 : 0;
                            var Tank_Sens_Max = yumusakSuNodeRedModel.Tank_1_Sens_Max == true ? 1 : 0;
                            var Tank_Sens_Basinc = yumusakSuNodeRedModel.Tank_1_SensBasinc;
                            var Tank_SuSeviye = yumusakSuNodeRedModel.Tank_1_SuSeviye;
                            var Tank_SuSeviye_Yuzdesi = yumusakSuNodeRedModel.Tank_1_SuSeviye_Yuzde;
                            var Tank_SuHacim = yumusakSuNodeRedModel.Tank_1_SuHacim;
                            var Tank_SuHacim_Yuzdesi = yumusakSuNodeRedModel.Tank_1_SuHacim_Yuzde;
                            var Tank_T_TankHacim = yumusakSuNodeRedModel.Tank_1_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 72, SayacId = 0, EtiketId = 443, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 72, SayacId = 0, EtiketId = 444, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 72, SayacId = 0, EtiketId = 445, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 72, SayacId = 0, EtiketId = 446, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 72, SayacId = 0, EtiketId = 447, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 72, SayacId = 0, EtiketId = 448, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 72, SayacId = 0, EtiketId = 449, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 72, SayacId = 0, EtiketId = 450, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_T_TankHacim });

                            var DM_Kesimhane12_Connected = yumusakSuNodeRedModel.DM_Uretim_12_Connected == true ? 1 : 0;
                            var DM_Kesimhane12_Total = yumusakSuNodeRedModel.DM_Uretim_12_Total;
                            var DM_Kesimhane12_Debi = yumusakSuNodeRedModel.DM_Uretim_12_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 61, EtiketId = 451, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kesimhane12_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 61, EtiketId = 452, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kesimhane12_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 61, EtiketId = 453, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kesimhane12_Debi });

                            var DM_Kesimhane3_Connected = yumusakSuNodeRedModel.DM_Uretim_3_Connected == true ? 1 : 0;
                            var DM_Kesimhane3_Total = yumusakSuNodeRedModel.DM_Uretim_3_Total;
                            var DM_Kesimhane3_Debi = yumusakSuNodeRedModel.DM_Uretim_3_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 62, EtiketId = 454, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kesimhane3_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 62, EtiketId = 455, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kesimhane3_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 62, EtiketId = 456, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kesimhane3_Debi });

                            var Hidrofor_Cmd_Calis = yumusakSuNodeRedModel.Hidrofor_1_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Rpr_Mode = yumusakSuNodeRedModel.Hidrofor_1_Rpr_Mode;
                            var Hidrofor_Rpr_State = yumusakSuNodeRedModel.Hidrofor_1_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 73, SayacId = 0, EtiketId = 457, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 73, SayacId = 0, EtiketId = 458, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 73, SayacId = 0, EtiketId = 459, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Rpr_State });

                            var Hidrofor_Motor_1_Kontaktor = yumusakSuNodeRedModel.Hidrofor_1_Motor_1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_1_TermikOK = yumusakSuNodeRedModel.Hidrofor_1_Motor_1_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_1_State = yumusakSuNodeRedModel.Hidrofor_1_Motor_1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 74, SayacId = 0, EtiketId = 460, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 74, SayacId = 0, EtiketId = 461, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 74, SayacId = 0, EtiketId = 462, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_State });

                            var Hidrofor_Motor_2_Kontaktor = yumusakSuNodeRedModel.Hidrofor_1_Motor_2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_2_TermikOK = yumusakSuNodeRedModel.Hidrofor_1_Motor_2_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_2_State = yumusakSuNodeRedModel.Hidrofor_1_Motor_2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 75, SayacId = 0, EtiketId = 463, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 75, SayacId = 0, EtiketId = 464, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 75, SayacId = 0, EtiketId = 465, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_State });

                            var Hidrofor_Motor_3_Kontaktor = yumusakSuNodeRedModel.Hidrofor_1_Motor_3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_3_TermikOK = yumusakSuNodeRedModel.Hidrofor_1_Motor_3_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_3_State = yumusakSuNodeRedModel.Hidrofor_1_Motor_3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 76, SayacId = 0, EtiketId = 466, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 76, SayacId = 0, EtiketId = 467, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 76, SayacId = 0, EtiketId = 468, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_State });

                            //var KollektorBasinci_Connected = yumusakSuNodeRedModel.Hidrofor_Basinc == true ? 1 : 0;
                            var KollektorBasinci_Value = yumusakSuNodeRedModel.Hidrofor_Basinc;
                            //etiketLogs.Add(new EtiketLog_TempYeni { LokasyonId = lokasyonId, SuKaynakId = 73, SayacId = 0, EtiketId = 408, KayitZamani = DateTime.Now, Deger = (decimal?)KollektorBasinci_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 73, SayacId = 0, EtiketId = 469, KayitZamani = DateTime.Now, Deger = (decimal?)KollektorBasinci_Value });

                            var Jenerator_SebekeElektrik = yumusakSuNodeRedModel.SebekedeElektrikVar == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 470, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_SebekeElektrik });
                        }
                    }
                    else
                    {
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 473, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 472, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    }
                }
            }
            return etiketLogs;
        }
        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsertRenderingAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 58;
            var now = DateTime.Now;
            List<EtiketLogYeni> etiketLogs = new List<EtiketLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model == null)
                {
                    var plcRead = 0;
                    var plcConnected = 1;
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 475, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 476, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    return etiketLogs;
                }
                if (model != null)
                {
                    var plcRead = model.Durum;
                    var plcConnected = model.Durum;

                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            RenderingNodeRedModel renderingNodeRedModel = JsonSerializer.Deserialize<RenderingNodeRedModel>(model.Veriler);

                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 475, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 476, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });

                            if (plcConnected == 0) return etiketLogs;
                            //if (plcRead == 0) return etiketLogs;

                            var Tank_Sens_Min = renderingNodeRedModel.Tank_1_Sens_Min == true ? 1 : 0;
                            var Tank_Sens_Max = renderingNodeRedModel.Tank_1_Sens_Max == true ? 1 : 0;
                            var Tank_Sens_Basinc = renderingNodeRedModel.Tank_1_SensBasinc;
                            var Tank_SuSeviye = renderingNodeRedModel.Tank_1_SuSeviye;
                            var Tank_SuSeviye_Yuzdesi = renderingNodeRedModel.Tank_1_SuSeviye_Yuzde;
                            var Tank_SuHacim = renderingNodeRedModel.Tank_1_SuHacim;
                            var Tank_SuHacim_Yuzdesi = renderingNodeRedModel.Tank_1_SuHacim_Yuzde;
                            var Tank_T_TankHacim = renderingNodeRedModel.Tank_1_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 79, SayacId = 0, EtiketId = 477, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 79, SayacId = 0, EtiketId = 478, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 79, SayacId = 0, EtiketId = 479, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 79, SayacId = 0, EtiketId = 480, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 79, SayacId = 0, EtiketId = 481, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 79, SayacId = 0, EtiketId = 482, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 79, SayacId = 0, EtiketId = 483, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 79, SayacId = 0, EtiketId = 484, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_T_TankHacim });

                            var DM_SicakSuRingHatti_Connected = renderingNodeRedModel.DM_SicakSuRingHatti_Connected == true ? 1 : 0;
                            var DM_SicakSuRingHatti_Total = renderingNodeRedModel.DM_SicakSuRingHatti_Total;
                            var DM_SicakSuRingHatti_Debi = renderingNodeRedModel.DM_SicakSuRingHatti_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 64, EtiketId = 485, KayitZamani = DateTime.Now, Deger = (decimal?)DM_SicakSuRingHatti_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 64, EtiketId = 486, KayitZamani = DateTime.Now, Deger = (decimal?)DM_SicakSuRingHatti_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 64, EtiketId = 487, KayitZamani = DateTime.Now, Deger = (decimal?)DM_SicakSuRingHatti_Debi });

                            var DM_SicakSuHatti_Connected = renderingNodeRedModel.DM_SicakSuHatti_Connected == true ? 1 : 0;
                            var DM_SicakSuHatti_Total = renderingNodeRedModel.DM_SicakSuHatti_Total;
                            var DM_SicakSuHatti_Debi = renderingNodeRedModel.DM_SicakSuHatti_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 65, EtiketId = 488, KayitZamani = DateTime.Now, Deger = (decimal?)DM_SicakSuHatti_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 65, EtiketId = 489, KayitZamani = DateTime.Now, Deger = (decimal?)DM_SicakSuHatti_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 65, EtiketId = 490, KayitZamani = DateTime.Now, Deger = (decimal?)DM_SicakSuHatti_Debi });

                            var Hidrofor_Sicaksu_Cmd_Calis = renderingNodeRedModel.Hidrofor_Sicaksu_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Sicaksu_Rpr_Mode = renderingNodeRedModel.Hidrofor_Sicaksu_Rpr_Mode;
                            var Hidrofor_Sicaksu_Rpr_State = renderingNodeRedModel.Hidrofor_Sicaksu_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 80, SayacId = 0, EtiketId = 491, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicaksu_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 80, SayacId = 0, EtiketId = 492, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicaksu_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 80, SayacId = 0, EtiketId = 493, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicaksu_Rpr_State });

                            var Hidrofor_Motor_1_Kontaktor = renderingNodeRedModel.Hidrofor_Sicaksu_Motor_1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_1_TermikOK = renderingNodeRedModel.Hidrofor_Sicaksu_Motor_1_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_1_State = renderingNodeRedModel.Hidrofor_Sicaksu_Motor_1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 81, SayacId = 0, EtiketId = 494, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 81, SayacId = 0, EtiketId = 495, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 81, SayacId = 0, EtiketId = 496, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_1_State });

                            var Hidrofor_Motor_2_Kontaktor = renderingNodeRedModel.Hidrofor_Sicaksu_Motor_2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_2_TermikOK = renderingNodeRedModel.Hidrofor_Sicaksu_Motor_2_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_2_State = renderingNodeRedModel.Hidrofor_Sicaksu_Motor_2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 82, SayacId = 0, EtiketId = 497, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 82, SayacId = 0, EtiketId = 498, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 82, SayacId = 0, EtiketId = 499, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_2_State });

                            var Hidrofor_Motor_3_Kontaktor = renderingNodeRedModel.Hidrofor_Sicaksu_Motor_3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_3_TermikOK = renderingNodeRedModel.Hidrofor_Sicaksu_Motor_3_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_3_State = renderingNodeRedModel.Hidrofor_Sicaksu_Motor_3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 83, SayacId = 0, EtiketId = 500, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 83, SayacId = 0, EtiketId = 501, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 83, SayacId = 0, EtiketId = 502, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_3_State });

                            var Hidrofor_Motor_4_Kontaktor = renderingNodeRedModel.Hidrofor_Sicaksu_Motor_4_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Motor_4_TermikOK = renderingNodeRedModel.Hidrofor_Sicaksu_Motor_4_Ariza == true ? 1 : 0;
                            var Hidrofor_Motor_4_State = renderingNodeRedModel.Hidrofor_Sicaksu_Motor_4_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 84, SayacId = 0, EtiketId = 503, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_4_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 84, SayacId = 0, EtiketId = 504, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_4_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 84, SayacId = 0, EtiketId = 505, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Motor_4_State });

                            var KollektorBasinci_Value = renderingNodeRedModel.Hidrofor_Sicaksu_Basinc;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 80, SayacId = 0, EtiketId = 506, KayitZamani = DateTime.Now, Deger = (decimal?)KollektorBasinci_Value });

                            var Hidrofor_Ring_Cmd_Calis = renderingNodeRedModel.Hidrofor_Ring_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Ring_Rpr_Mode = renderingNodeRedModel.Hidrofor_Ring_Rpr_Mode;
                            var Hidrofor_Ring_Rpr_State = renderingNodeRedModel.Hidrofor_Ring_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 85, SayacId = 0, EtiketId = 507, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ring_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 85, SayacId = 0, EtiketId = 508, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ring_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 85, SayacId = 0, EtiketId = 509, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ring_Rpr_State });

                            var Hidrofor_Ring_Motor_1_Kontaktor = renderingNodeRedModel.Hidrofor_Ring_Motor_1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ring_Motor_1_TermikOK = renderingNodeRedModel.Hidrofor_Ring_Motor_1_Ariza == true ? 1 : 0;
                            var Hidrofor_Ring_Motor_1_State = renderingNodeRedModel.Hidrofor_Ring_Motor_1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 86, SayacId = 0, EtiketId = 510, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ring_Motor_1_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 86, SayacId = 0, EtiketId = 511, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ring_Motor_1_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 86, SayacId = 0, EtiketId = 512, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ring_Motor_1_State });

                            var Hidrofor_Ring_Motor_2_Kontaktor = renderingNodeRedModel.Hidrofor_Ring_Motor_2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ring_Motor_2_TermikOK = renderingNodeRedModel.Hidrofor_Ring_Motor_2_Ariza == true ? 1 : 0;
                            var Hidrofor_Ring_Motor_2_State = renderingNodeRedModel.Hidrofor_Ring_Motor_2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 87, SayacId = 0, EtiketId = 513, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ring_Motor_2_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 87, SayacId = 0, EtiketId = 514, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ring_Motor_2_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 87, SayacId = 0, EtiketId = 515, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ring_Motor_2_State });

                            var Hidrofor_Ring_Motor_3_Kontaktor = renderingNodeRedModel.Hidrofor_Ring_Motor_3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ring_Motor_3_TermikOK = renderingNodeRedModel.Hidrofor_Ring_Motor_3_Ariza == true ? 1 : 0;
                            var Hidrofor_Ring_Motor_3_State = renderingNodeRedModel.Hidrofor_Ring_Motor_3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 88, SayacId = 0, EtiketId = 516, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ring_Motor_3_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 88, SayacId = 0, EtiketId = 517, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ring_Motor_3_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 88, SayacId = 0, EtiketId = 518, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ring_Motor_3_State });

                            var KollektorSicaklik_Value = renderingNodeRedModel.Hidrofor_Ring_Kollektor_Sicaklik;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 85, SayacId = 0, EtiketId = 519, KayitZamani = DateTime.Now, Deger = (decimal?)KollektorBasinci_Value });

                            var Jenerator_SebekeElektrik = renderingNodeRedModel.SebekedeElektrikVar == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 520, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_SebekeElektrik });
                        }
                    }
                    else
                    {
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 475, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 476, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    }
                }
            }
            return etiketLogs;
        }
        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsertSuIslemeAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 63;
            var now = DateTime.Now;
            List<EtiketLogYeni> etiketLogs = new List<EtiketLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model == null)
                {
                    var plcRead = 0;
                    var plcConnected = 1;
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 522, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 523, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    return etiketLogs;
                }
                if (model != null)
                {
                    var plcRead = model.Durum;
                    var plcConnected = model.Durum;

                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            SuIslemeNodeRedModel suIslemeNodeRedModel = JsonSerializer.Deserialize<SuIslemeNodeRedModel>(model.Veriler);

                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 522, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 523, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });

                            if (plcConnected == 0) return etiketLogs;
                            //if (plcRead == 0) return etiketLogs;                                                     

                            var DM_Osmoz_Besleme_Connected = suIslemeNodeRedModel.DM_OsmozBesleme_Connected == true ? 1 : 0;
                            var DM_Osmoz_Besleme_Total = suIslemeNodeRedModel.DM_OsmozBesleme_Total;
                            var DM_Osmoz_Besleme_Debi = suIslemeNodeRedModel.DM_OsmozBesleme_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 66, EtiketId = 524, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Besleme_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 66, EtiketId = 525, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Besleme_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 66, EtiketId = 526, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Besleme_Debi });

                            var DM_Kesimhane_12_Connected = suIslemeNodeRedModel.DM_Kesimhane1ve2Uretim_Connected == true ? 1 : 0;
                            var DM_Kesimhane_12_Total = suIslemeNodeRedModel.DM_Kesimhane1ve2Uretim_Total;
                            var DM_Kesimhane_12_Debi = suIslemeNodeRedModel.DM_Kesimhane1ve2Uretim_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 67, EtiketId = 527, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kesimhane_12_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 67, EtiketId = 528, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kesimhane_12_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 67, EtiketId = 529, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kesimhane_12_Debi });

                            var DM_Kesimhane_3_Connected = suIslemeNodeRedModel.DM_Kesimhane3_Connected == true ? 1 : 0;
                            var DM_Kesimhane_3_Total = suIslemeNodeRedModel.DM_Kesimhane3_Total;
                            var DM_Kesimhane_3_Debi = suIslemeNodeRedModel.DM_Kesimhane3_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 68, EtiketId = 530, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kesimhane_3_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 68, EtiketId = 531, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kesimhane_3_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 68, EtiketId = 532, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kesimhane_3_Debi });

                            var DM_Yumusatma_Besleme_Connected = suIslemeNodeRedModel.DM_YumusatmaBesleme_Connected == true ? 1 : 0;
                            var DM_Yumusatma_Besleme_Total = suIslemeNodeRedModel.DM_YumusatmaBesleme_Total;
                            var DM_Yumusatma_Besleme_Debi = suIslemeNodeRedModel.DM_YumusatmaBesleme_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 69, EtiketId = 533, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusatma_Besleme_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 69, EtiketId = 534, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusatma_Besleme_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 69, EtiketId = 535, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusatma_Besleme_Debi });

                            var DM_Yumusak_Tank_1_Connected = suIslemeNodeRedModel.DM_YumusatmaTank1_Connected == true ? 1 : 0;
                            var DM_Yumusak_Tank_1_Total = suIslemeNodeRedModel.DM_YumusatmaTank1_Total;
                            var DM_Yumusak_Tank_1_Debi = suIslemeNodeRedModel.DM_YumusatmaTank1_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 70, EtiketId = 536, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Tank_1_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 70, EtiketId = 537, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Tank_1_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 70, EtiketId = 538, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Tank_1_Debi });

                            var DM_Yumusak_Tank_2_Connected = suIslemeNodeRedModel.DM_YumusatmaTank2_Connected == true ? 1 : 0;
                            var DM_Yumusak_Tank_2_Total = suIslemeNodeRedModel.DM_YumusatmaTank2_Total;
                            var DM_Yumusak_Tank_2_Debi = suIslemeNodeRedModel.DM_YumusatmaTank2_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 71, EtiketId = 539, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Tank_2_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 71, EtiketId = 540, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Tank_2_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 71, EtiketId = 541, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Tank_2_Debi });

                            var DM_Yumusak_Tank_3_Connected = suIslemeNodeRedModel.DM_YumusatmaTank3_Connected == true ? 1 : 0;
                            var DM_Yumusak_Tank_3_Total = suIslemeNodeRedModel.DM_YumusatmaTank3_Total;
                            var DM_Yumusak_Tank_3_Debi = suIslemeNodeRedModel.DM_YumusatmaTank3_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 72, EtiketId = 542, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Tank_3_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 72, EtiketId = 543, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Tank_3_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 72, EtiketId = 544, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Tank_3_Debi });

                            var DM_Yumusak_Tank_4_Connected = suIslemeNodeRedModel.DM_YumusatmaTank4_Connected == true ? 1 : 0;
                            var DM_Yumusak_Tank_4_Total = suIslemeNodeRedModel.DM_YumusatmaTank4_Total;
                            var DM_Yumusak_Tank_4_Debi = suIslemeNodeRedModel.DM_YumusatmaTank4_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 73, EtiketId = 545, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Tank_4_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 73, EtiketId = 546, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Tank_4_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 73, EtiketId = 547, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Tank_4_Debi });

                            var DM_Yumusak_Atik_Connected = suIslemeNodeRedModel.DM_YumusakAtik_Connected == true ? 1 : 0;
                            var DM_Yumusak_Atik_Total = suIslemeNodeRedModel.DM_YumusakAtik_Total;
                            var DM_Yumusak_Atik_Debi = suIslemeNodeRedModel.DM_YumusakAtik_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 74, EtiketId = 548, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Atik_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 74, EtiketId = 549, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Atik_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 74, EtiketId = 550, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Atik_Debi });

                            var DM_Yumusak_Hatti_Connected = suIslemeNodeRedModel.DM_YumusakHatti_Connected == true ? 1 : 0;
                            var DM_Yumusak_Hatti_Total = suIslemeNodeRedModel.DM_YumusakHatti_Total;
                            var DM_Yumusak_Hatti_Debi = suIslemeNodeRedModel.DM_YumusakHatti_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 75, EtiketId = 551, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Hatti_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 75, EtiketId = 552, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Hatti_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 75, EtiketId = 553, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Hatti_Debi });

                            var DM_Rendering_Connected = suIslemeNodeRedModel.DM_Rendering_Connected == true ? 1 : 0;
                            var DM_Rendering_Total = suIslemeNodeRedModel.DM_Rendering_Total;
                            var DM_Rendering_Debi = suIslemeNodeRedModel.DM_Rendering_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 76, EtiketId = 554, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Rendering_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 76, EtiketId = 555, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Rendering_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 76, EtiketId = 556, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Rendering_Debi });

                            var DM_Yumusak_Su_Cikisi_Connected = suIslemeNodeRedModel.DM_YumusakSuCikisi_Connected == true ? 1 : 0;
                            var DM_Yumusak_Su_Cikisi_Total = suIslemeNodeRedModel.DM_YumusakSuCikisi_Total;
                            var DM_Yumusak_Su_Cikisi_Debi = suIslemeNodeRedModel.DM_YumusakSuCikisi_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 77, EtiketId = 557, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Su_Cikisi_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 77, EtiketId = 558, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Su_Cikisi_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 77, EtiketId = 559, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Su_Cikisi_Debi });



                            var Hidrofor_Ham_Su1_Cmd_Calis = suIslemeNodeRedModel.Hidrofor_Ham_Su_1_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Ham_Su1_Rpr_Mode = suIslemeNodeRedModel.Hidrofor_Ham_Su_1_Rpr_Mode;
                            var Hidrofor_Ham_Su1_Rpr_State = suIslemeNodeRedModel.Hidrofor_Ham_Su_1_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 91, SayacId = 0, EtiketId = 560, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 91, SayacId = 0, EtiketId = 561, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 91, SayacId = 0, EtiketId = 562, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Rpr_State });

                            var Hidrofor_Ham_Su1_Motor_1_Kontaktor = suIslemeNodeRedModel.Hidrofor_Ham_Su_1_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su1_Motor_1_TermikOK = suIslemeNodeRedModel.Hidrofor_Ham_Su_1_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su1_Motor_1_State = suIslemeNodeRedModel.Hidrofor_Ham_Su_1_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 92, SayacId = 0, EtiketId = 563, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_1_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 92, SayacId = 0, EtiketId = 564, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_1_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 92, SayacId = 0, EtiketId = 565, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_1_State });

                            var Hidrofor_Ham_Su1_Motor_2_Kontaktor = suIslemeNodeRedModel.Hidrofor_Ham_Su_1_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su1_Motor_2_TermikOK = suIslemeNodeRedModel.Hidrofor_Ham_Su_1_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su1_Motor_2_State = suIslemeNodeRedModel.Hidrofor_Ham_Su_1_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 93, SayacId = 0, EtiketId = 566, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_2_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 93, SayacId = 0, EtiketId = 567, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_2_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 93, SayacId = 0, EtiketId = 568, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_2_State });

                            var Hidrofor_Ham_Su1_Motor_3_Kontaktor = suIslemeNodeRedModel.Hidrofor_Ham_Su_1_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su1_Motor_3_TermikOK = suIslemeNodeRedModel.Hidrofor_Ham_Su_1_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su1_Motor_3_State = suIslemeNodeRedModel.Hidrofor_Ham_Su_1_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 94, SayacId = 0, EtiketId = 569, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_3_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 94, SayacId = 0, EtiketId = 570, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_3_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 94, SayacId = 0, EtiketId = 571, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_3_State });

                            var Hidrofor_Ham_Su1_Basinc = suIslemeNodeRedModel.Hidrofor_Ham_Su_1_CikisBasinci;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 91, SayacId = 0, EtiketId = 572, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Basinc });

                            var Hidrofor_Ham_Su2_Cmd_Calis = suIslemeNodeRedModel.Hidrofor_Ham_Su_2_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Ham_Su2_Rpr_Mode = suIslemeNodeRedModel.Hidrofor_Ham_Su_2_Rpr_Mode;
                            var Hidrofor_Ham_Su2_Rpr_State = suIslemeNodeRedModel.Hidrofor_Ham_Su_2_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 95, SayacId = 0, EtiketId = 573, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 95, SayacId = 0, EtiketId = 574, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 95, SayacId = 0, EtiketId = 575, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Rpr_State });

                            var Hidrofor_Ham_Su2_Motor_1_Kontaktor = suIslemeNodeRedModel.Hidrofor_Ham_Su_2_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su2_Motor_1_TermikOK = suIslemeNodeRedModel.Hidrofor_Ham_Su_2_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su2_Motor_1_State = suIslemeNodeRedModel.Hidrofor_Ham_Su_2_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 96, SayacId = 0, EtiketId = 576, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_1_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 96, SayacId = 0, EtiketId = 577, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_1_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 96, SayacId = 0, EtiketId = 578, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_1_State });

                            var Hidrofor_Ham_Su2_Motor_2_Kontaktor = suIslemeNodeRedModel.Hidrofor_Ham_Su_2_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su2_Motor_2_TermikOK = suIslemeNodeRedModel.Hidrofor_Ham_Su_2_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su2_Motor_2_State = suIslemeNodeRedModel.Hidrofor_Ham_Su_2_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 97, SayacId = 0, EtiketId = 579, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_2_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 97, SayacId = 0, EtiketId = 580, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_2_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 97, SayacId = 0, EtiketId = 581, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_2_State });

                            var Hidrofor_Ham_Su2_Motor_3_Kontaktor = suIslemeNodeRedModel.Hidrofor_Ham_Su_2_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su2_Motor_3_TermikOK = suIslemeNodeRedModel.Hidrofor_Ham_Su_2_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su2_Motor_3_State = suIslemeNodeRedModel.Hidrofor_Ham_Su_2_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 98, SayacId = 0, EtiketId = 582, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_3_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 98, SayacId = 0, EtiketId = 583, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_3_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 98, SayacId = 0, EtiketId = 584, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_3_State });

                            var Hidrofor_Ham_Su2_Basinc = suIslemeNodeRedModel.Hidrofor_Ham_Su_2_CikisBasinci;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 95, SayacId = 0, EtiketId = 585, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Basinc });

                            var Hidrofor_Yumusak_Su_Cmd_Calis = suIslemeNodeRedModel.Hidrofor_YumusakSu_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Rpr_Mode = suIslemeNodeRedModel.Hidrofor_YumusakSu_Rpr_Mode;
                            var Hidrofor_Yumusak_Su_Rpr_State = suIslemeNodeRedModel.Hidrofor_YumusakSu_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 99, SayacId = 0, EtiketId = 586, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 99, SayacId = 0, EtiketId = 587, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 99, SayacId = 0, EtiketId = 588, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Rpr_State });

                            var Hidrofor_Yumusak_Su_Motor_1_Calisiyor = suIslemeNodeRedModel.Hidrofor_YumusakSu_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_1_Ariza = suIslemeNodeRedModel.Hidrofor_YumusakSu_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_1_State = suIslemeNodeRedModel.Hidrofor_YumusakSu_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 100, SayacId = 0, EtiketId = 589, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_1_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 100, SayacId = 0, EtiketId = 590, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_1_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 100, SayacId = 0, EtiketId = 591, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_1_State });

                            var Hidrofor_Yumusak_Su_Motor_2_Calisiyor = suIslemeNodeRedModel.Hidrofor_YumusakSu_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_2_Ariza = suIslemeNodeRedModel.Hidrofor_YumusakSu_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_2_State = suIslemeNodeRedModel.Hidrofor_YumusakSu_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 101, SayacId = 0, EtiketId = 592, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_2_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 101, SayacId = 0, EtiketId = 593, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_2_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 101, SayacId = 0, EtiketId = 594, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_2_State });

                            var Hidrofor_Yumusak_Su_Motor_3_Calisiyor = suIslemeNodeRedModel.Hidrofor_YumusakSu_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_3_Ariza = suIslemeNodeRedModel.Hidrofor_YumusakSu_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_3_State = suIslemeNodeRedModel.Hidrofor_YumusakSu_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 102, SayacId = 0, EtiketId = 595, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_3_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 102, SayacId = 0, EtiketId = 596, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_3_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 102, SayacId = 0, EtiketId = 597, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_3_State });

                            var Hidrofor_Yumusak_Su_Motor_4_Calisiyor = suIslemeNodeRedModel.Hidrofor_YumusakSu_Motor4_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_4_Ariza = suIslemeNodeRedModel.Hidrofor_YumusakSu_Motor4_Ariza == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_4_State = suIslemeNodeRedModel.Hidrofor_YumusakSu_Motor4_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 103, SayacId = 0, EtiketId = 598, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_4_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 103, SayacId = 0, EtiketId = 599, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_4_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 103, SayacId = 0, EtiketId = 600, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_4_State });

                            var Hidrofor_Yumusak_Su_Basinc = suIslemeNodeRedModel.Hidrofor_YumusakSu_CikisBasinci;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 99, SayacId = 0, EtiketId = 601, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Basinc });

                            var Jenerator_SebekeElektrik = suIslemeNodeRedModel.SebekedeElektrikVar == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 602, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_SebekeElektrik });


                            var Yangin_Tanki_SuSeviye = suIslemeNodeRedModel.Yangin_Tanki_Seviye;
                            var Yangin_Tanki_SuSeviye_Yuzdesi = suIslemeNodeRedModel.Yangin_Tanki_Seviye_Yuzde;
                            var Yangin_Tanki_SuHacim = suIslemeNodeRedModel.Yangin_Tanki_Hacim;
                            var Yangin_Tanki_SuHacim_Yuzdesi = suIslemeNodeRedModel.Yangin_Tanki_Hacim_Yuzde;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 204, SayacId = 0, EtiketId = 1120, KayitZamani = DateTime.Now, Deger = (decimal?)Yangin_Tanki_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 204, SayacId = 0, EtiketId = 1121, KayitZamani = DateTime.Now, Deger = (decimal?)Yangin_Tanki_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 204, SayacId = 0, EtiketId = 1122, KayitZamani = DateTime.Now, Deger = (decimal?)Yangin_Tanki_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 204, SayacId = 0, EtiketId = 1123, KayitZamani = DateTime.Now, Deger = (decimal?)Yangin_Tanki_SuHacim_Yuzdesi });

                            var Yangin_Hatti_Elektrik_Pompa_Calisti = suIslemeNodeRedModel.Yangin_Hatti_Elektrik_Pompa_Calisti == true ? 1 : 0;
                            var Yangin_Hatti_Elektrik_Pompa_Talebi = suIslemeNodeRedModel.Yangin_Hatti_Elektrik_Pompa_Talebi == true ? 1 : 0;
                            var Yangin_Hatti_Dizel_Pompa_Manuel_Konum = suIslemeNodeRedModel.Yangin_Hatti_Dizel_Pompa_Manuel_Konum == true ? 1 : 0;
                            var Yangin_Hatti_Dizel_Pompa_Calisti = suIslemeNodeRedModel.Yangin_Hatti_Dizel_Pompa_Calisti == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 1124, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_SebekeElektrik });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 1125, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_SebekeElektrik });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 1126, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_SebekeElektrik });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 1127, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_SebekeElektrik });
                        }
                    }
                    else
                    {
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 522, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 523, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    }
                }
            }
            return etiketLogs;
        }
        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsertIleriIslemAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 64;
            var now = DateTime.Now;
            List<EtiketLogYeni> etiketLogs = new List<EtiketLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model == null)
                {
                    var plcRead = 0;
                    var plcConnected = 1;
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 654, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 655, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    return etiketLogs;
                }
                if (model != null)
                {
                    var plcRead = model.Durum;
                    var plcConnected = model.Durum;

                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            IleriIslemNodeRedModel ileriIslemNodeRedModel = JsonSerializer.Deserialize<IleriIslemNodeRedModel>(model.Veriler);

                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 654, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 655, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });

                            if (plcConnected == 0) return etiketLogs;
                            //if (plcRead == 0) return etiketLogs;

                            var Tank_1_Sens_Min = ileriIslemNodeRedModel.Tank_1_Sens_Min == true ? 1 : 0;
                            //var Tank_1_Sens_Orta = ileriIslemNodeRedModel.Tank_1_Sens_Orta == true ? 1 : 0;
                            var Tank_1_Sens_Max = ileriIslemNodeRedModel.Tank_1_Sens_Max == true ? 1 : 0;
                            var Tank_1_Sens_Basinc = ileriIslemNodeRedModel.Tank_1_SensBasinc;
                            var Tank_1_SuSeviye = ileriIslemNodeRedModel.Tank_1_SuSeviye;
                            var Tank_1_SuSeviye_Yuzdesi = ileriIslemNodeRedModel.Tank_1_SuSeviye_Yuzde;
                            var Tank_1_SuHacim = ileriIslemNodeRedModel.Tank_1_SuHacim;
                            var Tank_1_SuHacim_Yuzdesi = ileriIslemNodeRedModel.Tank_1_SuHacim_Yuzde;
                            var Tank_1_T_TankHacim = ileriIslemNodeRedModel.Tank_1_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 116, SayacId = 0, EtiketId = 656, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_Sens_Min });
                            //etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 116, SayacId = 0, EtiketId = 657, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_Sens_Orta });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 116, SayacId = 0, EtiketId = 658, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 116, SayacId = 0, EtiketId = 659, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 116, SayacId = 0, EtiketId = 660, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 116, SayacId = 0, EtiketId = 661, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 116, SayacId = 0, EtiketId = 662, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 116, SayacId = 0, EtiketId = 663, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_SuHacim_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 116, SayacId = 0, EtiketId = 664, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_T_TankHacim });

                            var Tank_2_Sens_Min = ileriIslemNodeRedModel.Tank_2_Sens_Min == true ? 1 : 0;
                            //var Tank_2_Sens_Orta = ileriIslemNodeRedModel.Tank_2_Sens_Orta == true ? 1 : 0;
                            var Tank_2_Sens_Max = ileriIslemNodeRedModel.Tank_2_Sens_Max == true ? 1 : 0;
                            var Tank_2_Sens_Basinc = ileriIslemNodeRedModel.Tank_2_SensBasinc;
                            var Tank_2_SuSeviye = ileriIslemNodeRedModel.Tank_2_SuSeviye;
                            var Tank_2_SuSeviye_Yuzdesi = ileriIslemNodeRedModel.Tank_2_SuSeviye_Yuzde;
                            var Tank_2_SuHacim = ileriIslemNodeRedModel.Tank_2_SuHacim;
                            var Tank_2_SuHacim_Yuzdesi = ileriIslemNodeRedModel.Tank_2_SuHacim_Yuzde;
                            var Tank_2_T_TankHacim = ileriIslemNodeRedModel.Tank_2_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 117, SayacId = 0, EtiketId = 665, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_Sens_Min });
                            //etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 117, SayacId = 0, EtiketId = 666, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_Sens_Orta });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 117, SayacId = 0, EtiketId = 667, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 117, SayacId = 0, EtiketId = 668, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 117, SayacId = 0, EtiketId = 669, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 117, SayacId = 0, EtiketId = 670, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 117, SayacId = 0, EtiketId = 671, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 117, SayacId = 0, EtiketId = 672, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_SuHacim_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 117, SayacId = 0, EtiketId = 673, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_T_TankHacim });

                            var Tank_3_Sens_Min = ileriIslemNodeRedModel.Tank_3_Sens_Min == true ? 1 : 0;
                            //var Tank_3_Sens_Orta = ileriIslemNodeRedModel.Tank_3_Sens_Orta == true ? 1 : 0;
                            var Tank_3_Sens_Max = ileriIslemNodeRedModel.Tank_3_Sens_Max == true ? 1 : 0;
                            var Tank_3_Sens_Basinc = ileriIslemNodeRedModel.Tank_3_SensBasinc;
                            var Tank_3_SuSeviye = ileriIslemNodeRedModel.Tank_3_SuSeviye;
                            var Tank_3_SuSeviye_Yuzdesi = ileriIslemNodeRedModel.Tank_3_SuSeviye_Yuzde;
                            var Tank_3_SuHacim = ileriIslemNodeRedModel.Tank_3_SuHacim;
                            var Tank_3_SuHacim_Yuzdesi = ileriIslemNodeRedModel.Tank_3_SuHacim_Yuzde;
                            var Tank_3_T_TankHacim = ileriIslemNodeRedModel.Tank_3_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 118, SayacId = 0, EtiketId = 674, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_Sens_Min });
                            //etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 118, SayacId = 0, EtiketId = 675, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_Sens_Orta });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 118, SayacId = 0, EtiketId = 676, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 118, SayacId = 0, EtiketId = 677, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 118, SayacId = 0, EtiketId = 678, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 118, SayacId = 0, EtiketId = 679, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 118, SayacId = 0, EtiketId = 680, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 118, SayacId = 0, EtiketId = 681, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_SuHacim_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 118, SayacId = 0, EtiketId = 682, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_T_TankHacim });

                            var DM_Yumusatma_Giris_Connected = ileriIslemNodeRedModel.DM_YumusatmaGiris_Connected == true ? 1 : 0;
                            var DM_Yumusatma_Giris_Total = ileriIslemNodeRedModel.DM_YumusatmaGiris_Total;
                            var DM_Yumusatma_Giris_Debi = ileriIslemNodeRedModel.DM_YumusatmaGiris_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 81, EtiketId = 683, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusatma_Giris_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 81, EtiketId = 684, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusatma_Giris_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 81, EtiketId = 685, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusatma_Giris_Debi });

                            var DM_Osmoz_Giris_Connected = ileriIslemNodeRedModel.DM_OsmozGiris_Connected == true ? 1 : 0;
                            var DM_Osmoz_Giris_Total = ileriIslemNodeRedModel.DM_OsmozGiris_Total;
                            var DM_Osmoz_Giris_Debi = ileriIslemNodeRedModel.DM_OsmozGiris_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 82, EtiketId = 686, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Giris_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 82, EtiketId = 687, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Giris_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 82, EtiketId = 688, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Giris_Debi });

                            var DM_Kazan_Dairesi_Connected = ileriIslemNodeRedModel.DM_KazanDairesi_Connected == true ? 1 : 0;
                            var DM_Kazan_Dairesi_Total = ileriIslemNodeRedModel.DM_KazanDairesi_Total;
                            var DM_Kazan_Dairesi_Debi = ileriIslemNodeRedModel.DM_KazanDairesi_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 83, EtiketId = 689, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kazan_Dairesi_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 83, EtiketId = 690, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kazan_Dairesi_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 83, EtiketId = 691, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kazan_Dairesi_Debi });

                            var DM_Kuyu4_Connected = ileriIslemNodeRedModel.DM_YumusatmaAtik_Connected == true ? 1 : 0;
                            var DM_Kuyu4_Total = ileriIslemNodeRedModel.DM_YumusatmaAtik_Total;
                            var DM_Kuyu4_Debi = ileriIslemNodeRedModel.DM_YumusatmaAtik_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 84, EtiketId = 692, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu4_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 84, EtiketId = 693, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu4_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 84, EtiketId = 694, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Kuyu4_Debi });

                            var DM_Yedek1_Connected = ileriIslemNodeRedModel.DM_Yedek1_Connected == true ? 1 : 0;
                            var DM_Yedek1_Total = ileriIslemNodeRedModel.DM_Yedek1_Total;
                            var DM_Yedek1_Debi = ileriIslemNodeRedModel.DM_Yedek1_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 85, EtiketId = 695, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yedek1_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 85, EtiketId = 696, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yedek1_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 85, EtiketId = 697, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yedek1_Debi });

                            //var DM_Yedek2_Connected = ileriIslemNodeRedModel.DM_Yedek2_Connected == true ? 1 : 0;
                            //var DM_Yedek2_Total = ileriIslemNodeRedModel.DM_Yedek2_Total;
                            //var DM_Yedek2_Debi = ileriIslemNodeRedModel.DM_Yedek2_Debi;
                            //etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 86, EtiketId = 698, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yedek2_Connected });
                            //etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 86, EtiketId = 699, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yedek2_Total });
                            //etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 86, EtiketId = 700, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yedek2_Debi });

                            var Hidrofor_Ham_Su_Cmd_Calis = ileriIslemNodeRedModel.Hidrofor_HamSu_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Ham_Su_Rpr_Mode = ileriIslemNodeRedModel.Hidrofor_HamSu_Rpr_Mode;
                            var Hidrofor_Ham_Su_Rpr_State = ileriIslemNodeRedModel.Hidrofor_HamSu_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 125, SayacId = 0, EtiketId = 701, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 125, SayacId = 0, EtiketId = 702, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 125, SayacId = 0, EtiketId = 703, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su_Rpr_State });

                            var Hidrofor_Ham_Su_Motor_1_Kontaktor = ileriIslemNodeRedModel.Hidrofor_HamSu_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su_Motor_1_TermikOK = ileriIslemNodeRedModel.Hidrofor_HamSu_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su_Motor_1_State = ileriIslemNodeRedModel.Hidrofor_HamSu_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 126, SayacId = 0, EtiketId = 704, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su_Motor_1_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 126, SayacId = 0, EtiketId = 705, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su_Motor_1_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 126, SayacId = 0, EtiketId = 706, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su_Motor_1_State });

                            var Hidrofor_Ham_Su_Motor_2_Kontaktor = ileriIslemNodeRedModel.Hidrofor_HamSu_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su_Motor_2_TermikOK = ileriIslemNodeRedModel.Hidrofor_HamSu_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su_Motor_2_State = ileriIslemNodeRedModel.Hidrofor_HamSu_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 127, SayacId = 0, EtiketId = 707, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su_Motor_2_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 127, SayacId = 0, EtiketId = 708, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su_Motor_2_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 127, SayacId = 0, EtiketId = 709, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su_Motor_2_State });

                            var Hidrofor_Ham_Su_Motor_3_Kontaktor = ileriIslemNodeRedModel.Hidrofor_HamSu_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su_Motor_3_TermikOK = ileriIslemNodeRedModel.Hidrofor_HamSu_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su_Motor_3_State = ileriIslemNodeRedModel.Hidrofor_HamSu_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 128, SayacId = 0, EtiketId = 710, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su_Motor_3_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 128, SayacId = 0, EtiketId = 711, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su_Motor_3_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 128, SayacId = 0, EtiketId = 712, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su_Motor_3_State });

                            var Hidrofor_Ham_Su_Basinc = ileriIslemNodeRedModel.Hidrofor_HamSu_Basinc;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 125, SayacId = 0, EtiketId = 713, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su_Basinc });

                            var Hidrofor_Yumusak_Su_Cmd_Calis = ileriIslemNodeRedModel.Hidrofor_YumusakSu_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Rpr_Mode = ileriIslemNodeRedModel.Hidrofor_YumusakSu_Rpr_Mode;
                            var Hidrofor_Yumusak_Su_Rpr_State = ileriIslemNodeRedModel.Hidrofor_YumusakSu_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 129, SayacId = 0, EtiketId = 714, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 129, SayacId = 0, EtiketId = 715, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 129, SayacId = 0, EtiketId = 716, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Rpr_State });

                            var Hidrofor_Yumusak_Su_Motor_1_Kontaktor = ileriIslemNodeRedModel.Hidrofor_YumusakSu_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_1_TermikOK = ileriIslemNodeRedModel.Hidrofor_YumusakSu_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_1_State = ileriIslemNodeRedModel.Hidrofor_YumusakSu_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 130, SayacId = 0, EtiketId = 717, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_1_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 130, SayacId = 0, EtiketId = 718, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_1_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 130, SayacId = 0, EtiketId = 719, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_1_State });

                            var Hidrofor_Yumusak_Su_Motor_2_Kontaktor = ileriIslemNodeRedModel.Hidrofor_YumusakSu_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_2_TermikOK = ileriIslemNodeRedModel.Hidrofor_YumusakSu_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_2_State = ileriIslemNodeRedModel.Hidrofor_YumusakSu_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 131, SayacId = 0, EtiketId = 720, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_2_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 131, SayacId = 0, EtiketId = 721, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_2_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 131, SayacId = 0, EtiketId = 722, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_2_State });

                            var Hidrofor_Yumusak_Su_Motor_3_Kontaktor = ileriIslemNodeRedModel.Hidrofor_YumusakSu_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_3_TermikOK = ileriIslemNodeRedModel.Hidrofor_YumusakSu_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_3_State = ileriIslemNodeRedModel.Hidrofor_YumusakSu_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 132, SayacId = 0, EtiketId = 723, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_3_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 132, SayacId = 0, EtiketId = 724, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_3_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 132, SayacId = 0, EtiketId = 725, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_3_State });

                            var Hidrofor_Yumusak_Su_Basinc = ileriIslemNodeRedModel.Hidrofor_YumusakSu_Basinc;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 129, SayacId = 0, EtiketId = 726, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Basinc });

                            var Hidrofor_Osmoz_Su_Cmd_Calis = ileriIslemNodeRedModel.Hidrofor_OsmozSu_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Osmoz_Su_Rpr_Mode = ileriIslemNodeRedModel.Hidrofor_OsmozSu_Rpr_Mode;
                            var Hidrofor_Osmoz_Su_Rpr_State = ileriIslemNodeRedModel.Hidrofor_OsmozSu_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 133, SayacId = 0, EtiketId = 727, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 133, SayacId = 0, EtiketId = 728, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 133, SayacId = 0, EtiketId = 729, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Rpr_State });

                            var Hidrofor_Osmoz_Su_Motor_1_Kontaktor = ileriIslemNodeRedModel.Hidrofor_OsmozSu_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Osmoz_Su_Motor_1_TermikOK = ileriIslemNodeRedModel.Hidrofor_OsmozSu_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_Osmoz_Su_Motor_1_State = ileriIslemNodeRedModel.Hidrofor_OsmozSu_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 134, SayacId = 0, EtiketId = 730, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_1_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 134, SayacId = 0, EtiketId = 731, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_1_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 134, SayacId = 0, EtiketId = 732, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_1_State });

                            var Hidrofor_Osmoz_Su_Motor_2_Kontaktor = ileriIslemNodeRedModel.Hidrofor_OsmozSu_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Osmoz_Su_Motor_2_TermikOK = ileriIslemNodeRedModel.Hidrofor_OsmozSu_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_Osmoz_Su_Motor_2_State = ileriIslemNodeRedModel.Hidrofor_OsmozSu_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 135, SayacId = 0, EtiketId = 733, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_2_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 135, SayacId = 0, EtiketId = 734, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_2_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 135, SayacId = 0, EtiketId = 735, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_2_State });

                            var Hidrofor_Osmoz_Su_Motor_3_Kontaktor = ileriIslemNodeRedModel.Hidrofor_OsmozSu_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Osmoz_Su_Motor_3_TermikOK = ileriIslemNodeRedModel.Hidrofor_OsmozSu_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_Osmoz_Su_Motor_3_State = ileriIslemNodeRedModel.Hidrofor_OsmozSu_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 136, SayacId = 0, EtiketId = 736, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_3_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 136, SayacId = 0, EtiketId = 737, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_3_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 136, SayacId = 0, EtiketId = 738, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_3_State });

                            var Hidrofor_Osmoz_Su_Basinc = ileriIslemNodeRedModel.Hidrofor_OsmozSu_Basinc;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 133, SayacId = 0, EtiketId = 739, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Basinc });

                            var Hidrofor_Yangin_Cmd_Calis = ileriIslemNodeRedModel.Hidrofor_Yangin_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Yangin_Rpr_Mode = ileriIslemNodeRedModel.Hidrofor_Yangin_Rpr_Mode;
                            var Hidrofor_Yangin_Rpr_State = ileriIslemNodeRedModel.Hidrofor_Yangin_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 137, SayacId = 0, EtiketId = 740, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 137, SayacId = 0, EtiketId = 741, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 137, SayacId = 0, EtiketId = 742, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Rpr_State });

                            var Hidrofor_Yangin_Motor_1_Kontaktor = ileriIslemNodeRedModel.Hidrofor_Yangin_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yangin_Motor_1_TermikOK = ileriIslemNodeRedModel.Hidrofor_Yangin_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_Yangin_Motor_1_State = ileriIslemNodeRedModel.Hidrofor_Yangin_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 138, SayacId = 0, EtiketId = 743, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Motor_1_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 138, SayacId = 0, EtiketId = 744, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Motor_1_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 138, SayacId = 0, EtiketId = 745, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Motor_1_State });

                            var Hidrofor_Yangin_Motor_2_Kontaktor = ileriIslemNodeRedModel.Hidrofor_Yangin_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yangin_Motor_2_TermikOK = ileriIslemNodeRedModel.Hidrofor_Yangin_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_Yangin_Motor_2_State = ileriIslemNodeRedModel.Hidrofor_Yangin_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 139, SayacId = 0, EtiketId = 746, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Motor_2_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 139, SayacId = 0, EtiketId = 747, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Motor_2_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 139, SayacId = 0, EtiketId = 748, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Motor_2_State });

                            var Hidrofor_Yangin_Motor_3_Kontaktor = ileriIslemNodeRedModel.Hidrofor_Yangin_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yangin_Motor_3_TermikOK = ileriIslemNodeRedModel.Hidrofor_Yangin_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_Yangin_Motor_3_State = ileriIslemNodeRedModel.Hidrofor_Yangin_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 140, SayacId = 0, EtiketId = 749, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Motor_3_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 140, SayacId = 0, EtiketId = 750, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Motor_3_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 140, SayacId = 0, EtiketId = 751, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Motor_3_State });

                            var Hidrofor_Yangin_Basinc = ileriIslemNodeRedModel.Hidrofor_Yangin_Basinc;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 137, SayacId = 0, EtiketId = 752, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Basinc });

                            var Hidrofor_SicakSu1_Cmd_Calis = ileriIslemNodeRedModel.Hidrofor_SicakSu1_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_SicakSu1_Rpr_Mode = ileriIslemNodeRedModel.Hidrofor_SicakSu1_Rpr_Mode;
                            var Hidrofor_SicakSu1_Rpr_State = ileriIslemNodeRedModel.Hidrofor_SicakSu1_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 196, SayacId = 0, EtiketId = 753, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu1_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 196, SayacId = 0, EtiketId = 754, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu1_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 196, SayacId = 0, EtiketId = 755, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu1_Rpr_State });

                            var Hidrofor_SicakSu1_Motor_1_Kontaktor = ileriIslemNodeRedModel.Hidrofor_SicakSu1_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_SicakSu1_Motor_1_TermikOK = ileriIslemNodeRedModel.Hidrofor_SicakSu1_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_SicakSu1_Motor_1_State = ileriIslemNodeRedModel.Hidrofor_SicakSu1_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 197, SayacId = 0, EtiketId = 756, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu1_Motor_1_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 197, SayacId = 0, EtiketId = 757, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu1_Motor_1_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 197, SayacId = 0, EtiketId = 758, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu1_Motor_1_State });

                            var Hidrofor_SicakSu1_Motor_2_Kontaktor = ileriIslemNodeRedModel.Hidrofor_SicakSu1_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_SicakSu1_Motor_2_TermikOK = ileriIslemNodeRedModel.Hidrofor_SicakSu1_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_SicakSu1_Motor_2_State = ileriIslemNodeRedModel.Hidrofor_SicakSu1_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 198, SayacId = 0, EtiketId = 759, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu1_Motor_2_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 198, SayacId = 0, EtiketId = 760, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu1_Motor_2_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 198, SayacId = 0, EtiketId = 761, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu1_Motor_2_State });

                            var Hidrofor_SicakSu1_Motor_3_Kontaktor = ileriIslemNodeRedModel.Hidrofor_SicakSu1_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_SicakSu1_Motor_3_TermikOK = ileriIslemNodeRedModel.Hidrofor_SicakSu1_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_SicakSu1_Motor_3_State = ileriIslemNodeRedModel.Hidrofor_SicakSu1_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 199, SayacId = 0, EtiketId = 762, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu1_Motor_3_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 199, SayacId = 0, EtiketId = 763, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu1_Motor_3_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 199, SayacId = 0, EtiketId = 764, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu1_Motor_3_State });

                            var Hidrofor_SicakSu1_Basinc = ileriIslemNodeRedModel.Hidrofor_SicakSu1_Basinc;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 196, SayacId = 0, EtiketId = 765, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu1_Basinc });

                            var Hidrofor_SicakSu2_Cmd_Calis = ileriIslemNodeRedModel.Hidrofor_SicakSu2_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_SicakSu2_Rpr_Mode = ileriIslemNodeRedModel.Hidrofor_SicakSu2_Rpr_Mode;
                            var Hidrofor_SicakSu2_Rpr_State = ileriIslemNodeRedModel.Hidrofor_SicakSu2_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 200, SayacId = 0, EtiketId = 766, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu2_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 200, SayacId = 0, EtiketId = 767, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu2_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 200, SayacId = 0, EtiketId = 768, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu2_Rpr_State });

                            var Hidrofor_SicakSu2_Motor_1_Kontaktor = ileriIslemNodeRedModel.Hidrofor_SicakSu2_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_SicakSu2_Motor_1_TermikOK = ileriIslemNodeRedModel.Hidrofor_SicakSu2_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_SicakSu2_Motor_1_State = ileriIslemNodeRedModel.Hidrofor_SicakSu2_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 201, SayacId = 0, EtiketId = 769, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu2_Motor_1_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 201, SayacId = 0, EtiketId = 770, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu2_Motor_1_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 201, SayacId = 0, EtiketId = 771, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu2_Motor_1_State });

                            var Hidrofor_SicakSu2_Motor_2_Kontaktor = ileriIslemNodeRedModel.Hidrofor_SicakSu2_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_SicakSu2_Motor_2_TermikOK = ileriIslemNodeRedModel.Hidrofor_SicakSu2_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_SicakSu2_Motor_2_State = ileriIslemNodeRedModel.Hidrofor_SicakSu2_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 202, SayacId = 0, EtiketId = 772, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu2_Motor_2_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 202, SayacId = 0, EtiketId = 773, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu2_Motor_2_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 202, SayacId = 0, EtiketId = 774, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu2_Motor_2_State });

                            var Hidrofor_SicakSu2_Motor_3_Kontaktor = ileriIslemNodeRedModel.Hidrofor_SicakSu2_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_SicakSu2_Motor_3_TermikOK = ileriIslemNodeRedModel.Hidrofor_SicakSu2_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_SicakSu2_Motor_3_State = ileriIslemNodeRedModel.Hidrofor_SicakSu2_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 203, SayacId = 0, EtiketId = 775, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu2_Motor_3_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 203, SayacId = 0, EtiketId = 776, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu2_Motor_3_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 203, SayacId = 0, EtiketId = 777, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu2_Motor_3_State });

                            var Hidrofor_SicakSu2_Basinc = ileriIslemNodeRedModel.Hidrofor_SicakSu2_Basinc;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 200, SayacId = 0, EtiketId = 778, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_SicakSu2_Basinc });

                            var Jenerator_SebekeElektrik = ileriIslemNodeRedModel.SebekedeElektrikVar == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 779, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_SebekeElektrik });
                        }
                    }
                    else
                    {
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 654, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 655, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    }
                }
            }
            return etiketLogs;
        }
        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsertDogalgazliKazanAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 66;
            var now = DateTime.Now;
            List<EtiketLogYeni> etiketLogs = new List<EtiketLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model == null)
                {
                    var plcRead = 0;
                    var plcConnected = 1;
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 805, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 806, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    return etiketLogs;
                }
                if (model != null)
                {
                    var plcRead = model.Durum;
                    var plcConnected = model.Durum;

                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            DogalgazliKazanNodeRedModel dogalgazliKazanNodeRedModel = JsonSerializer.Deserialize<DogalgazliKazanNodeRedModel>(model.Veriler);

                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 805, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 806, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });

                            if (plcConnected == 0) return etiketLogs;
                            //if (plcRead == 0) return etiketLogs;

                            var Tank_Sens_Min = dogalgazliKazanNodeRedModel.Tank_1_SensMin_Value == true ? 1 : 0;
                            var Tank_Sens_Orta = dogalgazliKazanNodeRedModel.Tank_1_SensOrta_Value == true ? 1 : 0;
                            var Tank_Sens_Max = dogalgazliKazanNodeRedModel.Tank_1_SensMax_Value == true ? 1 : 0;
                            var Tank_Sens_Basinc = dogalgazliKazanNodeRedModel.Tank_1_SensBasinc;
                            var Tank_SuSeviye = dogalgazliKazanNodeRedModel.Tank_1_SuSeviye;
                            var Tank_SuSeviye_Yuzdesi = dogalgazliKazanNodeRedModel.Tank_1_SuSeviye_Yuzde;
                            var Tank_SuHacim = dogalgazliKazanNodeRedModel.Tank_1_SuHacim;
                            var Tank_SuHacim_Yuzdesi = dogalgazliKazanNodeRedModel.Tank_1_SuHacim_Yuzde;
                            //var Tank_T_TankHacim = dogalgazliKazanNodeRedModel.Tank_1_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 807, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 808, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Orta });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 809, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 810, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 811, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 812, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 813, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 814, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_SuHacim_Yuzdesi });
                            //etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 815, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_T_TankHacim });

                            var DM_R_O_Atik_Su_Connected = dogalgazliKazanNodeRedModel.DM_ROAtikSu_Connected == true ? 1 : 0;
                            var DM_R_O_Atik_Su_Total = dogalgazliKazanNodeRedModel.DM_ROAtikSu_Total;
                            var DM_R_O_Atik_Su_Debi = dogalgazliKazanNodeRedModel.DM_ROAtikSu_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 87, EtiketId = 816, KayitZamani = DateTime.Now, Deger = (decimal?)DM_R_O_Atik_Su_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 87, EtiketId = 817, KayitZamani = DateTime.Now, Deger = (decimal?)DM_R_O_Atik_Su_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 87, EtiketId = 818, KayitZamani = DateTime.Now, Deger = (decimal?)DM_R_O_Atik_Su_Debi });

                            var DM_R_O_Kum_Filtresi_Connected = dogalgazliKazanNodeRedModel.DM_ROKumFiltresi_Connected == true ? 1 : 0;
                            var DM_R_O_Kum_Filtresi_Total = dogalgazliKazanNodeRedModel.DM_ROKumFiltresi_Total;
                            var DM_R_O_Kum_Filtresi_Debi = dogalgazliKazanNodeRedModel.DM_ROKumFiltresi_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 88, EtiketId = 819, KayitZamani = DateTime.Now, Deger = (decimal?)DM_R_O_Kum_Filtresi_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 88, EtiketId = 820, KayitZamani = DateTime.Now, Deger = (decimal?)DM_R_O_Kum_Filtresi_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 88, EtiketId = 821, KayitZamani = DateTime.Now, Deger = (decimal?)DM_R_O_Kum_Filtresi_Debi });

                            var DM_Osmoz_Su_Tanki_Girisi_Connected = dogalgazliKazanNodeRedModel.DM_OsmozSuTakiGirisi_Connected == true ? 1 : 0;
                            var DM_Osmoz_Su_Tanki_Girisi_Total = dogalgazliKazanNodeRedModel.DM_OsmozSuTakiGirisi_Total;
                            var DM_Osmoz_Su_Tanki_Girisi_Debi = dogalgazliKazanNodeRedModel.DM_OsmozSuTakiGirisi_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 89, EtiketId = 822, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Su_Tanki_Girisi_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 89, EtiketId = 823, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Su_Tanki_Girisi_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 89, EtiketId = 824, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Su_Tanki_Girisi_Debi });

                            var DM_Osmoz_Uretim_Hatti_Connected = dogalgazliKazanNodeRedModel.DM_OsmozUretimHatti_Connected == true ? 1 : 0;
                            var DM_Osmoz_Uretim_Hatti_Total = dogalgazliKazanNodeRedModel.DM_OsmozUretimHatti_Total;
                            var DM_Osmoz_Uretim_Hatti_Debi = dogalgazliKazanNodeRedModel.DM_OsmozUretimHatti_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 90, EtiketId = 825, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Uretim_Hatti_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 90, EtiketId = 826, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Uretim_Hatti_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 90, EtiketId = 827, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Uretim_Hatti_Debi });

                            var DM_Yedek2_Connected = dogalgazliKazanNodeRedModel.DM_Yedek2_Connected == true ? 1 : 0;
                            var DM_Yedek2_Total = dogalgazliKazanNodeRedModel.DM_Yedek2_Total;
                            var DM_Yedek2_Debi = dogalgazliKazanNodeRedModel.DM_Yedek2_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 92, EtiketId = 831, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yedek2_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 92, EtiketId = 832, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yedek2_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 92, EtiketId = 833, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yedek2_Debi });

                            var DM_Osmoz_Kazan_Dairesi_Connected = dogalgazliKazanNodeRedModel.DM_Osmoz_Kazan_Dairesi_Connected == true ? 1 : 0;
                            var DM_Osmoz_Kazan_Dairesi_Total = dogalgazliKazanNodeRedModel.DM_Osmoz_Kazan_Dairesi_Total;
                            var DM_Osmoz_Kazan_Dairesi_Debi = dogalgazliKazanNodeRedModel.DM_Osmoz_Kazan_Dairesi_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 112, EtiketId = 1168, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Kazan_Dairesi_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 112, EtiketId = 1169, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Kazan_Dairesi_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 112, EtiketId = 1170, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Kazan_Dairesi_Debi });

                            var DM_Osmoz_Makine_Dairesi_Connected = dogalgazliKazanNodeRedModel.DM_Osmoz_Makine_Dairesi_Connected == true ? 1 : 0;
                            var DM_Osmoz_Makine_Dairesi_Total = dogalgazliKazanNodeRedModel.DM_Osmoz_Makine_Dairesi_Total;
                            var DM_Osmoz_Makine_Dairesi_Debi = dogalgazliKazanNodeRedModel.DM_Osmoz_Makine_Dairesi_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 113, EtiketId = 1171, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Makine_Dairesi_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 113, EtiketId = 1172, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Makine_Dairesi_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 113, EtiketId = 1173, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Makine_Dairesi_Debi });

                            var Hidrofor_Osmoz_Cmd_Calis = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Osmoz_Rpr_Mode = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Rpr_Mode;
                            var Hidrofor_Osmoz_Rpr_State = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 148, SayacId = 0, EtiketId = 834, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 148, SayacId = 0, EtiketId = 835, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 148, SayacId = 0, EtiketId = 836, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Rpr_State });

                            var Hidrofor_Osmoz_Motor_1_Kontaktor = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Osmoz_Motor_1_TermikOK = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_Osmoz_Motor_1_State = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 149, SayacId = 0, EtiketId = 837, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Motor_1_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 149, SayacId = 0, EtiketId = 838, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Motor_1_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 149, SayacId = 0, EtiketId = 839, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Motor_1_State });

                            var Hidrofor_Osmoz_Motor_2_Kontaktor = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Osmoz_Motor_2_TermikOK = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_Osmoz_Motor_2_State = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 150, SayacId = 0, EtiketId = 840, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Motor_2_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 150, SayacId = 0, EtiketId = 841, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Motor_2_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 150, SayacId = 0, EtiketId = 842, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Motor_2_State });

                            var Hidrofor_Osmoz_Motor_3_Kontaktor = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Osmoz_Motor_3_TermikOK = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_Osmoz_Motor_3_State = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 151, SayacId = 0, EtiketId = 843, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Motor_3_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 151, SayacId = 0, EtiketId = 844, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Motor_3_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 151, SayacId = 0, EtiketId = 845, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Motor_3_State });

                            var Hidrofor_Osmoz_Motor_4_Kontaktor = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Motor4_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Osmoz_Motor_4_TermikOK = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Motor4_Ariza == true ? 1 : 0;
                            var Hidrofor_Osmoz_Motor_4_State = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Motor4_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 152, SayacId = 0, EtiketId = 846, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Motor_4_Kontaktor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 152, SayacId = 0, EtiketId = 847, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Motor_4_TermikOK });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 152, SayacId = 0, EtiketId = 848, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Motor_4_State });

                            //var Hidrofor_Osmoz_Basinc = dogalgazliKazanNodeRedModel.Hidrofor_Osmoz_Basinc;
                            //etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 148, SayacId = 0, EtiketId = 849, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Basinc });

                            var Jenerator_SebekeElektrik = dogalgazliKazanNodeRedModel.SebekedeElektrikVar == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 850, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_SebekeElektrik });

                            var Tank_1_Transmitter_Active = dogalgazliKazanNodeRedModel.Tank_1_Transmitter_Active == true ? 1 : 0;
                            var Tank_1_Transmitter_Value = dogalgazliKazanNodeRedModel.Tank_1_Transmitter_Value;
                            var Tank_1_Transmitter_Ham = dogalgazliKazanNodeRedModel.Tank_1_Transmitter_Ham;
                            var Tank_Sens_Min_Aktif = dogalgazliKazanNodeRedModel.Tank_1_SensMin_Aktif == true ? 1 : 0;
                            var Tank_Sens_Orta_Aktif = dogalgazliKazanNodeRedModel.Tank_1_SensOrta_Aktif == true ? 1 : 0;
                            var Tank_Sens_Max_Aktif = dogalgazliKazanNodeRedModel.Tank_1_SensMax_Aktif == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 851, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_Transmitter_Active });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 852, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_Transmitter_Value });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 853, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_Transmitter_Ham });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 854, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Min_Aktif });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 855, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Orta_Aktif });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 141, SayacId = 0, EtiketId = 856, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_Sens_Max_Aktif });

                            //Su Geri Kazanim
                            var DM_UretimAtikSertSu_Connected = dogalgazliKazanNodeRedModel.Uretim_Atik_Sert_Su_Sayaci_Connected == true ? 1 : 0;
                            var DM_UretimAtikSertSu_Total = dogalgazliKazanNodeRedModel.Uretim_Atik_Sert_Su_Sayaci_Total;
                            var DM_UretimAtikSertSu_Debi = dogalgazliKazanNodeRedModel.Uretim_Atik_Sert_Su_Sayaci_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 91, EtiketId = 828, KayitZamani = DateTime.Now, Deger = (decimal?)DM_UretimAtikSertSu_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 91, EtiketId = 829, KayitZamani = DateTime.Now, Deger = (decimal?)DM_UretimAtikSertSu_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 91, EtiketId = 830, KayitZamani = DateTime.Now, Deger = (decimal?)DM_UretimAtikSertSu_Debi });

                            var Sistem_Besleme_Pompasi_1_Start = dogalgazliKazanNodeRedModel.Sistem_Besleme_Pompasi_1_Start == true ? 1 : 0;
                            var Sistem_Besleme_Pompasi_2_Start = dogalgazliKazanNodeRedModel.Sistem_Besleme_Pompasi_2_Start == true ? 1 : 0;
                            var Urun_Hatti_Pompasi_1_Start = dogalgazliKazanNodeRedModel.Urun_Hatti_Pompasi_1_Start == true ? 1 : 0;
                            var Urun_Hatti_Pompasi_2_Start = dogalgazliKazanNodeRedModel.Sistem_Besleme_Pompasi_2_Arizada == true ? 1 : 0;
                            var Sistem_Besleme_Pompasi_1_Devrede = dogalgazliKazanNodeRedModel.Sistem_Besleme_Pompasi_1_Devrede == true ? 1 : 0;
                            var Sistem_Besleme_Pompasi_1_Arizada = dogalgazliKazanNodeRedModel.Sistem_Besleme_Pompasi_1_Arizada == true ? 1 : 0;
                            var Sistem_Besleme_Pompasi_2_Devrede = dogalgazliKazanNodeRedModel.Sistem_Besleme_Pompasi_2_Devrede == true ? 1 : 0;
                            var Sistem_Besleme_Pompasi_2_Arizada = dogalgazliKazanNodeRedModel.Sistem_Besleme_Pompasi_2_Arizada == true ? 1 : 0;
                            var Urun_Hatti_Pompasi_1_Devrede = dogalgazliKazanNodeRedModel.Urun_Hatti_Pompasi_1_Devrede == true ? 1 : 0;
                            var Urun_Hatti_Pompasi_1_Arizada = dogalgazliKazanNodeRedModel.Urun_Hatti_Pompasi_1_Arizada == true ? 1 : 0;
                            var Urun_Hatti_Pompasi_2_Devrede = dogalgazliKazanNodeRedModel.Urun_Hatti_Pompasi_2_Devrede == true ? 1 : 0;
                            var Urun_Hatti_Pompasi_2_Arizada = dogalgazliKazanNodeRedModel.Urun_Hatti_Pompasi_2_Arizada == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 0, EtiketId = 1128, KayitZamani = DateTime.Now, Deger = (decimal?)Sistem_Besleme_Pompasi_1_Start });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 0, EtiketId = 1129, KayitZamani = DateTime.Now, Deger = (decimal?)Sistem_Besleme_Pompasi_2_Start });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 0, EtiketId = 1130, KayitZamani = DateTime.Now, Deger = (decimal?)Urun_Hatti_Pompasi_1_Start });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 0, EtiketId = 1131, KayitZamani = DateTime.Now, Deger = (decimal?)Urun_Hatti_Pompasi_2_Start });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 0, EtiketId = 1132, KayitZamani = DateTime.Now, Deger = (decimal?)Sistem_Besleme_Pompasi_1_Devrede });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 0, EtiketId = 1133, KayitZamani = DateTime.Now, Deger = (decimal?)Sistem_Besleme_Pompasi_1_Arizada });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 0, EtiketId = 1134, KayitZamani = DateTime.Now, Deger = (decimal?)Sistem_Besleme_Pompasi_2_Devrede });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 0, EtiketId = 1135, KayitZamani = DateTime.Now, Deger = (decimal?)Sistem_Besleme_Pompasi_2_Arizada });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 0, EtiketId = 1136, KayitZamani = DateTime.Now, Deger = (decimal?)Urun_Hatti_Pompasi_1_Devrede });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 0, EtiketId = 1137, KayitZamani = DateTime.Now, Deger = (decimal?)Urun_Hatti_Pompasi_1_Arizada });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 0, EtiketId = 1138, KayitZamani = DateTime.Now, Deger = (decimal?)Urun_Hatti_Pompasi_2_Devrede });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 0, SayacId = 0, EtiketId = 1139, KayitZamani = DateTime.Now, Deger = (decimal?)Urun_Hatti_Pompasi_2_Arizada });

                            var Reject_Stok_Tanki_Transmitter_Active = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_Transmitter_Active == true ? 1 : 0;
                            var Reject_Stok_Tanki_Transmitter_Value = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_Transmitter_Value;
                            var Reject_Stok_Tanki_Transmitter_Ham = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_Transmitter_Ham;
                            var Reject_Stok_Tanki_SensMin_Aktif = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SensMin_Aktif == true ? 1 : 0;
                            var Reject_Stok_Tanki_SensOrta_Aktif = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SensOrta_Aktif == true ? 1 : 0;
                            var Reject_Stok_Tanki_SensMax_Aktif = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SensMax_Aktif == true ? 1 : 0;
                            var Reject_Stok_Tanki_SensMin_Value = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SensMin_Value == true ? 1 : 0;
                            var Reject_Stok_Tanki_SensOrta_Value = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SensOrta_Value == true ? 1 : 0;
                            var Reject_Stok_Tanki_SensMax_Value = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SensMax_Value == true ? 1 : 0;
                            var Reject_Stok_Tanki_SensBasinc = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SensBasinc;
                            var Reject_Stok_Tanki_SuSeviye = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SuSeviye;
                            var Reject_Stok_Tanki_SuSeviye_Yuzde = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SuSeviye_Yuzde;
                            var Reject_Stok_Tanki_SuHacim = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SuHacim;
                            var Reject_Stok_Tanki_SuHacim_Yuzde = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SuHacim_Yuzde;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 207, SayacId = 0, EtiketId = 1140, KayitZamani = DateTime.Now, Deger = (decimal?)Reject_Stok_Tanki_Transmitter_Active });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 207, SayacId = 0, EtiketId = 1141, KayitZamani = DateTime.Now, Deger = (decimal?)Reject_Stok_Tanki_Transmitter_Value });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 207, SayacId = 0, EtiketId = 1142, KayitZamani = DateTime.Now, Deger = (decimal?)Reject_Stok_Tanki_Transmitter_Ham });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 207, SayacId = 0, EtiketId = 1143, KayitZamani = DateTime.Now, Deger = (decimal?)Reject_Stok_Tanki_SensMin_Aktif });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 207, SayacId = 0, EtiketId = 1144, KayitZamani = DateTime.Now, Deger = (decimal?)Reject_Stok_Tanki_SensOrta_Aktif });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 207, SayacId = 0, EtiketId = 1145, KayitZamani = DateTime.Now, Deger = (decimal?)Reject_Stok_Tanki_SensMax_Aktif });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 207, SayacId = 0, EtiketId = 1146, KayitZamani = DateTime.Now, Deger = (decimal?)Reject_Stok_Tanki_SensMin_Value });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 207, SayacId = 0, EtiketId = 1147, KayitZamani = DateTime.Now, Deger = (decimal?)Reject_Stok_Tanki_SensOrta_Value });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 207, SayacId = 0, EtiketId = 1148, KayitZamani = DateTime.Now, Deger = (decimal?)Reject_Stok_Tanki_SensMax_Value });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 207, SayacId = 0, EtiketId = 1149, KayitZamani = DateTime.Now, Deger = (decimal?)Reject_Stok_Tanki_SensBasinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 207, SayacId = 0, EtiketId = 1150, KayitZamani = DateTime.Now, Deger = (decimal?)Reject_Stok_Tanki_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 207, SayacId = 0, EtiketId = 1151, KayitZamani = DateTime.Now, Deger = (decimal?)Reject_Stok_Tanki_SuSeviye_Yuzde });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 207, SayacId = 0, EtiketId = 1152, KayitZamani = DateTime.Now, Deger = (decimal?)Reject_Stok_Tanki_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 207, SayacId = 0, EtiketId = 1153, KayitZamani = DateTime.Now, Deger = (decimal?)Reject_Stok_Tanki_SuHacim_Yuzde });

                            var Proses_Suyu_Depolama_Tanki_Transmitter_Active = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_Transmitter_Active == true ? 1 : 0;
                            var Proses_Suyu_Depolama_Tanki_Transmitter_Value = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_Transmitter_Value;
                            var Proses_Suyu_Depolama_Tanki_Transmitter_Ham = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_Transmitter_Ham;
                            var Proses_Suyu_Depolama_Tanki_SensMin_Aktif = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SensMin_Aktif == true ? 1 : 0;
                            var Proses_Suyu_Depolama_Tanki_SensOrta_Aktif = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SensOrta_Aktif == true ? 1 : 0;
                            var Proses_Suyu_Depolama_Tanki_SensMax_Aktif = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SensMax_Aktif == true ? 1 : 0;
                            var Proses_Suyu_Depolama_Tanki_SensMin_Value = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SensMin_Value == true ? 1 : 0;
                            var Proses_Suyu_Depolama_Tanki_SensOrta_Value = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SensOrta_Value == true ? 1 : 0;
                            var Proses_Suyu_Depolama_Tanki_SensMax_Value = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SensMax_Value == true ? 1 : 0;
                            var Proses_Suyu_Depolama_Tanki_SensBasinc = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SensBasinc;
                            var Proses_Suyu_Depolama_Tanki_SuSeviye = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SuSeviye;
                            var Proses_Suyu_Depolama_Tanki_SuSeviye_Yuzde = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SuSeviye_Yuzde;
                            var Proses_Suyu_Depolama_Tanki_SuHacim = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SuHacim;
                            var Proses_Suyu_Depolama_Tanki_SuHacim_Yuzde = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SuHacim_Yuzde;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 208, SayacId = 0, EtiketId = 1154, KayitZamani = DateTime.Now, Deger = (decimal?)Proses_Suyu_Depolama_Tanki_Transmitter_Active });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 208, SayacId = 0, EtiketId = 1155, KayitZamani = DateTime.Now, Deger = (decimal?)Proses_Suyu_Depolama_Tanki_Transmitter_Value });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 208, SayacId = 0, EtiketId = 1156, KayitZamani = DateTime.Now, Deger = (decimal?)Proses_Suyu_Depolama_Tanki_Transmitter_Ham });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 208, SayacId = 0, EtiketId = 1157, KayitZamani = DateTime.Now, Deger = (decimal?)Proses_Suyu_Depolama_Tanki_SensMin_Aktif });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 208, SayacId = 0, EtiketId = 1158, KayitZamani = DateTime.Now, Deger = (decimal?)Proses_Suyu_Depolama_Tanki_SensOrta_Aktif });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 208, SayacId = 0, EtiketId = 1159, KayitZamani = DateTime.Now, Deger = (decimal?)Proses_Suyu_Depolama_Tanki_SensMax_Aktif });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 208, SayacId = 0, EtiketId = 1160, KayitZamani = DateTime.Now, Deger = (decimal?)Proses_Suyu_Depolama_Tanki_SensMin_Value });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 208, SayacId = 0, EtiketId = 1161, KayitZamani = DateTime.Now, Deger = (decimal?)Proses_Suyu_Depolama_Tanki_SensOrta_Value });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 208, SayacId = 0, EtiketId = 1162, KayitZamani = DateTime.Now, Deger = (decimal?)Proses_Suyu_Depolama_Tanki_SensMax_Value });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 208, SayacId = 0, EtiketId = 1163, KayitZamani = DateTime.Now, Deger = (decimal?)Proses_Suyu_Depolama_Tanki_SensBasinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 208, SayacId = 0, EtiketId = 1164, KayitZamani = DateTime.Now, Deger = (decimal?)Proses_Suyu_Depolama_Tanki_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 208, SayacId = 0, EtiketId = 1165, KayitZamani = DateTime.Now, Deger = (decimal?)Proses_Suyu_Depolama_Tanki_SuSeviye_Yuzde });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 208, SayacId = 0, EtiketId = 1166, KayitZamani = DateTime.Now, Deger = (decimal?)Proses_Suyu_Depolama_Tanki_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = 68, SuKaynakId = 208, SayacId = 0, EtiketId = 1167, KayitZamani = DateTime.Now, Deger = (decimal?)Proses_Suyu_Depolama_Tanki_SuHacim_Yuzde });


                            var Sistem_Besleme_Pompasi_1_Durum = dogalgazliKazanNodeRedModel.Sistem_Besleme_Pompasi_1_Durum;
                            var Sistem_Besleme_Pompasi_2_Durum = dogalgazliKazanNodeRedModel.Sistem_Besleme_Pompasi_2_Durum;
                            var Urun_Hatti_Pompasi_1_Durum = dogalgazliKazanNodeRedModel.Urun_Hatti_Pompasi_1_Durum;
                            var Urun_Hatti_Pompasi_2_Durum = dogalgazliKazanNodeRedModel.Urun_Hatti_Pompasi_2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 209, SayacId = 0, EtiketId = 1174, KayitZamani = DateTime.Now, Deger = (decimal?)Sistem_Besleme_Pompasi_1_Durum });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 210, SayacId = 0, EtiketId = 1175, KayitZamani = DateTime.Now, Deger = (decimal?)Sistem_Besleme_Pompasi_2_Durum });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 211, SayacId = 0, EtiketId = 1176, KayitZamani = DateTime.Now, Deger = (decimal?)Urun_Hatti_Pompasi_1_Durum });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 212, SayacId = 0, EtiketId = 1177, KayitZamani = DateTime.Now, Deger = (decimal?)Urun_Hatti_Pompasi_2_Durum });

                        }
                    }
                    else
                    {
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 805, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 806, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    }
                }
            }
            return etiketLogs;
        }
        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsertPastorizeAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 65;
            var now = DateTime.Now;
            List<EtiketLogYeni> etiketLogs = new List<EtiketLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model == null)
                {
                    var plcRead = 0;
                    var plcConnected = 1;
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 902, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                    etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 903, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    return etiketLogs;
                }
                if (model != null)
                {
                    var plcRead = model.Durum;
                    var plcConnected = model.Durum;

                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            PastorizeNodeRedModel pastorizeNodeRedModel = JsonSerializer.Deserialize<PastorizeNodeRedModel>(model.Veriler);

                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 902, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 903, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });

                            if (plcConnected == 0) return etiketLogs;
                            //if (plcRead == 0) return etiketLogs;

                            var Tank_1_HamSu_Sens_Min = pastorizeNodeRedModel.Tank_HamSu_SensMin_Value == true ? 1 : 0;
                            var Tank_1_HamSu_Sens_Orta = pastorizeNodeRedModel.Tank_HamSu_SensOrta_Value == true ? 1 : 0;
                            var Tank_1_HamSu_Sens_Max = pastorizeNodeRedModel.Tank_HamSu_SensMax_Value == true ? 1 : 0;
                            var Tank_1_HamSu_Sens_Basinc = pastorizeNodeRedModel.Tank_HamSu_SensBasinc;
                            var Tank_1_HamSu_SuSeviye = pastorizeNodeRedModel.Tank_HamSu_SuSeviye;
                            var Tank_1_HamSu_SuSeviye_Yuzdesi = pastorizeNodeRedModel.Tank_HamSu_SuSeviye_Yuzde;
                            var Tank_1_HamSu_SuHacim = pastorizeNodeRedModel.Tank_HamSu_SuHacim;
                            var Tank_1_HamSu_SuHacim_Yuzdesi = pastorizeNodeRedModel.Tank_HamSu_SuHacim_Yuzde;
                            //var Tank_1_HamSu_T_TankHacim = pastorizeNodeRedModel.Tank_1_HamSu_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 153, SayacId = 0, EtiketId = 904, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_HamSu_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 153, SayacId = 0, EtiketId = 905, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_HamSu_Sens_Orta });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 153, SayacId = 0, EtiketId = 906, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_HamSu_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 153, SayacId = 0, EtiketId = 907, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_HamSu_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 153, SayacId = 0, EtiketId = 908, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_HamSu_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 153, SayacId = 0, EtiketId = 909, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_HamSu_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 153, SayacId = 0, EtiketId = 910, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_HamSu_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 153, SayacId = 0, EtiketId = 911, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_HamSu_SuHacim_Yuzdesi });
                            //etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 153, SayacId = 0, EtiketId = 912, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_1_HamSu_T_TankHacim });

                            var Tank_2_YumusakSu_Sens_Min = pastorizeNodeRedModel.Tank_YumusakSu_SensMin_Value == true ? 1 : 0;
                            var Tank_2_YumusakSu_Sens_Orta = pastorizeNodeRedModel.Tank_YumusakSu_SensOrta_Value == true ? 1 : 0;
                            var Tank_2_YumusakSu_Sens_Max = pastorizeNodeRedModel.Tank_YumusakSu_SensMax_Value == true ? 1 : 0;
                            var Tank_2_YumusakSu_Sens_Basinc = pastorizeNodeRedModel.Tank_YumusakSu_SensBasinc;
                            var Tank_2_YumusakSu_SuSeviye = pastorizeNodeRedModel.Tank_YumusakSu_SuSeviye;
                            var Tank_2_YumusakSu_SuSeviye_Yuzdesi = pastorizeNodeRedModel.Tank_YumusakSu_SuSeviye_Yuzde;
                            var Tank_2_YumusakSu_SuHacim = pastorizeNodeRedModel.Tank_YumusakSu_SuHacim;
                            var Tank_2_YumusakSu_SuHacim_Yuzdesi = pastorizeNodeRedModel.Tank_YumusakSu_SuHacim_Yuzde;
                            //var Tank_2_YumusakSu_T_TankHacim = pastorizeNodeRedModel.Tank_2_YumusakSu_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 154, SayacId = 0, EtiketId = 913, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_YumusakSu_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 154, SayacId = 0, EtiketId = 914, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_YumusakSu_Sens_Orta });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 154, SayacId = 0, EtiketId = 915, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_YumusakSu_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 154, SayacId = 0, EtiketId = 916, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_YumusakSu_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 154, SayacId = 0, EtiketId = 917, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_YumusakSu_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 154, SayacId = 0, EtiketId = 918, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_YumusakSu_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 154, SayacId = 0, EtiketId = 919, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_YumusakSu_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 154, SayacId = 0, EtiketId = 920, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_YumusakSu_SuHacim_Yuzdesi });
                            //etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 154, SayacId = 0, EtiketId = 921, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_2_YumusakSu_T_TankHacim });

                            var Tank_3_OsmozSu_Sens_Min = pastorizeNodeRedModel.Tank_OsmozSu_SensMin_Value == true ? 1 : 0;
                            var Tank_3_OsmozSu_Sens_Orta = pastorizeNodeRedModel.Tank_OsmozSu_SensOrta_Value == true ? 1 : 0;
                            var Tank_3_OsmozSu_Sens_Max = pastorizeNodeRedModel.Tank_OsmozSu_SensMax_Value == true ? 1 : 0;
                            var Tank_3_OsmozSu_Sens_Basinc = pastorizeNodeRedModel.Tank_OsmozSu_SensBasinc;
                            var Tank_3_OsmozSu_SuSeviye = pastorizeNodeRedModel.Tank_OsmozSu_SuSeviye;
                            var Tank_3_OsmozSu_SuSeviye_Yuzdesi = pastorizeNodeRedModel.Tank_OsmozSu_SuSeviye_Yuzde;
                            var Tank_3_OsmozSu_SuHacim = pastorizeNodeRedModel.Tank_OsmozSu_SuHacim;
                            var Tank_3_OsmozSu_SuHacim_Yuzdesi = pastorizeNodeRedModel.Tank_OsmozSu_SuHacim_Yuzde;
                            //var Tank_3_OsmozSu_T_TankHacim = pastorizeNodeRedModel.Tank_3_OsmozSu_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 155, SayacId = 0, EtiketId = 922, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_OsmozSu_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 155, SayacId = 0, EtiketId = 923, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_OsmozSu_Sens_Orta });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 155, SayacId = 0, EtiketId = 924, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_OsmozSu_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 155, SayacId = 0, EtiketId = 925, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_OsmozSu_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 155, SayacId = 0, EtiketId = 926, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_OsmozSu_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 155, SayacId = 0, EtiketId = 927, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_OsmozSu_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 155, SayacId = 0, EtiketId = 928, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_OsmozSu_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 155, SayacId = 0, EtiketId = 929, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_OsmozSu_SuHacim_Yuzdesi });
                            //etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 155, SayacId = 0, EtiketId = 930, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_3_OsmozSu_T_TankHacim });

                            var Tank_4_SicakSu_Sens_Min = pastorizeNodeRedModel.Tank_SicakSu_SensMin_Value == true ? 1 : 0;
                            var Tank_4_SicakSu_Sens_Orta = pastorizeNodeRedModel.Tank_SicakSu_SensOrta_Value == true ? 1 : 0;
                            var Tank_4_SicakSu_Sens_Max = pastorizeNodeRedModel.Tank_SicakSu_SensMax_Value == true ? 1 : 0;
                            var Tank_4_SicakSu_Sens_Basinc = pastorizeNodeRedModel.Tank_SicakSu_SensBasinc;
                            var Tank_4_SicakSu_SuSeviye = pastorizeNodeRedModel.Tank_SicakSu_SuSeviye;
                            var Tank_4_SicakSu_SuSeviye_Yuzdesi = pastorizeNodeRedModel.Tank_SicakSu_SuSeviye_Yuzde;
                            var Tank_4_SicakSu_SuHacim = pastorizeNodeRedModel.Tank_SicakSu_SuHacim;
                            var Tank_4_SicakSu_SuHacim_Yuzdesi = pastorizeNodeRedModel.Tank_SicakSu_SuHacim_Yuzde;
                            //var Tank_4_SicakSu_T_TankHacim = pastorizeNodeRedModel.Tank_4_SicakSu_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 156, SayacId = 0, EtiketId = 931, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_4_SicakSu_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 156, SayacId = 0, EtiketId = 932, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_4_SicakSu_Sens_Orta });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 156, SayacId = 0, EtiketId = 933, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_4_SicakSu_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 156, SayacId = 0, EtiketId = 934, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_4_SicakSu_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 156, SayacId = 0, EtiketId = 935, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_4_SicakSu_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 156, SayacId = 0, EtiketId = 936, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_4_SicakSu_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 156, SayacId = 0, EtiketId = 937, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_4_SicakSu_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 156, SayacId = 0, EtiketId = 938, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_4_SicakSu_SuHacim_Yuzdesi });
                            //etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 156, SayacId = 0, EtiketId = 939, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_4_SicakSu_T_TankHacim });

                            var Tank_5_Yangin_Sens_Min = pastorizeNodeRedModel.Tank_Yangin_SensMin_Value == true ? 1 : 0;
                            var Tank_5_Yangin_Sens_Orta = pastorizeNodeRedModel.Tank_Yangin_SensOrta_Value == true ? 1 : 0;
                            var Tank_5_Yangin_Sens_Max = pastorizeNodeRedModel.Tank_Yangin_SensMax_Value == true ? 1 : 0;
                            var Tank_5_Yangin_Sens_Basinc = pastorizeNodeRedModel.Tank_Yangin_SensBasinc;
                            var Tank_5_Yangin_SuSeviye = pastorizeNodeRedModel.Tank_Yangin_SuSeviye;
                            var Tank_5_Yangin_SuSeviye_Yuzdesi = pastorizeNodeRedModel.Tank_Yangin_SuSeviye_Yuzde;
                            var Tank_5_Yangin_SuHacim = pastorizeNodeRedModel.Tank_Yangin_SuHacim;
                            var Tank_5_Yangin_SuHacim_Yuzdesi = pastorizeNodeRedModel.Tank_Yangin_SuHacim_Yuzde;
                            //var Tank_5_Yangin_T_TankHacim = pastorizeNodeRedModel.Tank_5_Yangin_T_TankHacim;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 157, SayacId = 0, EtiketId = 940, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_5_Yangin_Sens_Min });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 157, SayacId = 0, EtiketId = 941, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_5_Yangin_Sens_Orta });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 157, SayacId = 0, EtiketId = 942, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_5_Yangin_Sens_Max });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 157, SayacId = 0, EtiketId = 943, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_5_Yangin_Sens_Basinc });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 157, SayacId = 0, EtiketId = 944, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_5_Yangin_SuSeviye });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 157, SayacId = 0, EtiketId = 945, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_5_Yangin_SuSeviye_Yuzdesi });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 157, SayacId = 0, EtiketId = 946, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_5_Yangin_SuHacim });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 157, SayacId = 0, EtiketId = 947, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_5_Yangin_SuHacim_Yuzdesi });
                            //etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 157, SayacId = 0, EtiketId = 948, KayitZamani = DateTime.Now, Deger = (decimal?)Tank_5_Yangin_T_TankHacim });

                            var DM_Ham_Su_Hatti_Connected = pastorizeNodeRedModel.DM_HamSuHatti_Connected == true ? 1 : 0;
                            var DM_Ham_Su_Hatti_Total = pastorizeNodeRedModel.DM_HamSuHatti_Total;
                            var DM_Ham_Su_Hatti_Debi = pastorizeNodeRedModel.DM_HamSuHatti_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 98, EtiketId = 949, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Ham_Su_Hatti_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 98, EtiketId = 950, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Ham_Su_Hatti_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 98, EtiketId = 951, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Ham_Su_Hatti_Debi });

                            var DM_Yumusak_Atik_Connected = pastorizeNodeRedModel.DM_YumusakAtik_Connected == true ? 1 : 0;
                            var DM_Yumusak_Atik_Total = pastorizeNodeRedModel.DM_YumusakAtik_Total;
                            var DM_Yumusak_Atik_Debi = pastorizeNodeRedModel.DM_YumusakAtik_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 99, EtiketId = 952, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Atik_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 99, EtiketId = 953, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Atik_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 99, EtiketId = 954, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Atik_Debi });

                            var DM_Yumusak_Su_Hatti_Connected = pastorizeNodeRedModel.DM_YumusakSuHatti_Connected == true ? 1 : 0;
                            var DM_Yumusak_Su_Hatti_Total = pastorizeNodeRedModel.DM_YumusakSuHatti_Total;
                            var DM_Yumusak_Su_Hatti_Debi = pastorizeNodeRedModel.DM_YumusakSuHatti_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 100, EtiketId = 955, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Su_Hatti_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 100, EtiketId = 956, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Su_Hatti_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 100, EtiketId = 957, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Yumusak_Su_Hatti_Debi });

                            var DM_Uretim1_Connected = pastorizeNodeRedModel.DM_Uretim1_Connected == true ? 1 : 0;
                            var DM_Uretim1_Total = pastorizeNodeRedModel.DM_Uretim1_Total;
                            var DM_Uretim1_Debi = pastorizeNodeRedModel.DM_Uretim1_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 101, EtiketId = 958, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim1_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 101, EtiketId = 959, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim1_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 101, EtiketId = 960, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim1_Debi });

                            var DM_Uretim2_Connected = pastorizeNodeRedModel.DM_Uretim2_Connected == true ? 1 : 0;
                            var DM_Uretim2_Total = pastorizeNodeRedModel.DM_Uretim2_Total;
                            var DM_Uretim2_Debi = pastorizeNodeRedModel.DM_Uretim2_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 102, EtiketId = 961, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim2_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 102, EtiketId = 962, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim2_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 102, EtiketId = 963, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Uretim2_Debi });

                            var DM_Arac_Yikama_Connected = pastorizeNodeRedModel.DM_AracYikama_Connected == true ? 1 : 0;
                            var DM_Arac_Yikama_Total = pastorizeNodeRedModel.DM_AracYikama_Total;
                            var DM_Arac_Yikama_Debi = pastorizeNodeRedModel.DM_AracYikama_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 103, EtiketId = 964, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Arac_Yikama_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 103, EtiketId = 965, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Arac_Yikama_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 103, EtiketId = 966, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Arac_Yikama_Debi });

                            var DM_Lavabolar_Connected = pastorizeNodeRedModel.DM_Lavabolar_Connected == true ? 1 : 0;
                            var DM_Lavabolar_Total = pastorizeNodeRedModel.DM_Lavabolar_Total;
                            var DM_Lavabolar_Debi = pastorizeNodeRedModel.DM_Lavabolar_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 104, EtiketId = 967, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Lavabolar_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 104, EtiketId = 968, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Lavabolar_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 104, EtiketId = 969, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Lavabolar_Debi });

                            var DM_Osmoz_Giris_Hatti_Connected = pastorizeNodeRedModel.DM_OsmozGirisHatti_Connected == true ? 1 : 0;
                            var DM_Osmoz_Giris_Hatti_Total = pastorizeNodeRedModel.DM_OsmozGirisHatti_Total;
                            var DM_Osmoz_Giris_Hatti_Debi = pastorizeNodeRedModel.DM_OsmozGirisHatti_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 105, EtiketId = 970, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Giris_Hatti_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 105, EtiketId = 971, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Giris_Hatti_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 105, EtiketId = 972, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Osmoz_Giris_Hatti_Debi });

                            var DM_RO_Atik_Su_Connected = pastorizeNodeRedModel.DM_ROAtikSu_Connected == true ? 1 : 0;
                            var DM_RO_Atik_Su_Total = pastorizeNodeRedModel.DM_ROAtikSu_Total;
                            var DM_RO_Atik_Su_Debi = pastorizeNodeRedModel.DM_ROAtikSu_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 106, EtiketId = 973, KayitZamani = DateTime.Now, Deger = (decimal?)DM_RO_Atik_Su_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 106, EtiketId = 974, KayitZamani = DateTime.Now, Deger = (decimal?)DM_RO_Atik_Su_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 106, EtiketId = 975, KayitZamani = DateTime.Now, Deger = (decimal?)DM_RO_Atik_Su_Debi });

                            var DM_Membran_Atik_Su_Connected = pastorizeNodeRedModel.DM_MembranAtikSu_Connected == true ? 1 : 0;
                            var DM_Membran_Atik_Su_Total = pastorizeNodeRedModel.DM_MembranAtikSu_Total;
                            var DM_Membran_Atik_Su_Debi = pastorizeNodeRedModel.DM_MembranAtikSu_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 107, EtiketId = 976, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Membran_Atik_Su_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 107, EtiketId = 977, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Membran_Atik_Su_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 107, EtiketId = 978, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Membran_Atik_Su_Debi });

                            var DM_Peyzaj_Connected = pastorizeNodeRedModel.DM_Peyzaj_Connected == true ? 1 : 0;
                            var DM_Peyzaj_Total = pastorizeNodeRedModel.DM_Peyzaj_Total;
                            var DM_Peyzaj_Debi = pastorizeNodeRedModel.DM_Peyzaj_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 108, EtiketId = 979, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Peyzaj_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 108, EtiketId = 980, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Peyzaj_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 108, EtiketId = 981, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Peyzaj_Debi });

                            var DM_Rendering_Connected = pastorizeNodeRedModel.DM_Rendering_Connected == true ? 1 : 0;
                            var DM_Rendering_Total = pastorizeNodeRedModel.DM_Rendering_Total;
                            var DM_Rendering_Debi = pastorizeNodeRedModel.DM_Rendering_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 109, EtiketId = 982, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Rendering_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 109, EtiketId = 983, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Rendering_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 109, EtiketId = 984, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Rendering_Debi });

                            var DM_Ring_Hatti_Donus_Connected = pastorizeNodeRedModel.DM_RingHattiDonus_Connected == true ? 1 : 0;
                            var DM_Ring_Hatti_Donus_Total = pastorizeNodeRedModel.DM_RingHattiDonus_Total;
                            var DM_Ring_Hatti_Donus_Debi = pastorizeNodeRedModel.DM_RingHattiDonus_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 110, EtiketId = 985, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Ring_Hatti_Donus_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 110, EtiketId = 986, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Ring_Hatti_Donus_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 110, EtiketId = 987, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Ring_Hatti_Donus_Debi });

                            var DM_Sicak_Su_Uretim_Connected = pastorizeNodeRedModel.DM_SicakSuUretim_Connected == true ? 1 : 0;
                            var DM_Sicak_Su_Uretim_Total = pastorizeNodeRedModel.DM_SicakSuUretim_Total;
                            var DM_Sicak_Su_Uretim_Debi = pastorizeNodeRedModel.DM_SicakSuUretim_Debi;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 111, EtiketId = 988, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Sicak_Su_Uretim_Connected });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 111, EtiketId = 989, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Sicak_Su_Uretim_Total });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 111, EtiketId = 990, KayitZamani = DateTime.Now, Deger = (decimal?)DM_Sicak_Su_Uretim_Debi });



                            var Hidrofor_Ham_Su1_Cmd_Calis = pastorizeNodeRedModel.Hidrofor_HamSu1_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Ham_Su1_Rpr_Mode = pastorizeNodeRedModel.Hidrofor_HamSu1_Rpr_Mode;
                            var Hidrofor_Ham_Su1_Rpr_State = pastorizeNodeRedModel.Hidrofor_HamSu1_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 172, SayacId = 0, EtiketId = 991, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 172, SayacId = 0, EtiketId = 992, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 172, SayacId = 0, EtiketId = 993, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Rpr_State });

                            var Hidrofor_Ham_Su1_Motor_1_Calisiyor = pastorizeNodeRedModel.Hidrofor_HamSu1_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su1_Motor_1_Ariza = pastorizeNodeRedModel.Hidrofor_HamSu1_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su1_Motor_1_Durum = pastorizeNodeRedModel.Hidrofor_HamSu1_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 173, SayacId = 0, EtiketId = 994, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_1_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 173, SayacId = 0, EtiketId = 995, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_1_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 173, SayacId = 0, EtiketId = 996, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_1_Durum });

                            var Hidrofor_Ham_Su1_Motor_2_Calisiyor = pastorizeNodeRedModel.Hidrofor_HamSu1_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su1_Motor_2_Ariza = pastorizeNodeRedModel.Hidrofor_HamSu1_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su1_Motor_2_Durum = pastorizeNodeRedModel.Hidrofor_HamSu1_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 174, SayacId = 0, EtiketId = 997, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_2_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 174, SayacId = 0, EtiketId = 998, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_2_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 174, SayacId = 0, EtiketId = 999, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_2_Durum });

                            var Hidrofor_Ham_Su1_Motor_3_Calisiyor = pastorizeNodeRedModel.Hidrofor_HamSu1_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su1_Motor_3_Ariza = pastorizeNodeRedModel.Hidrofor_HamSu1_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su1_Motor_3_Durum = pastorizeNodeRedModel.Hidrofor_HamSu1_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 175, SayacId = 0, EtiketId = 1000, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_3_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 175, SayacId = 0, EtiketId = 1001, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_3_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 175, SayacId = 0, EtiketId = 1002, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Motor_3_Durum });

                            var Hidrofor_Ham_Su1_Basinc = pastorizeNodeRedModel.Hidrofor_HamSu1_Basinc;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 172, SayacId = 0, EtiketId = 1003, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su1_Basinc });


                            var Hidrofor_Ham_Su2_Cmd_Calis = pastorizeNodeRedModel.Hidrofor_HamSu2_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Ham_Su2_Rpr_Mode = pastorizeNodeRedModel.Hidrofor_HamSu2_Rpr_Mode;
                            var Hidrofor_Ham_Su2_Rpr_State = pastorizeNodeRedModel.Hidrofor_HamSu2_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 176, SayacId = 0, EtiketId = 1004, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 176, SayacId = 0, EtiketId = 1005, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 176, SayacId = 0, EtiketId = 1006, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Rpr_State });

                            var Hidrofor_Ham_Su2_Motor_1_Calisiyor = pastorizeNodeRedModel.Hidrofor_HamSu2_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su2_Motor_1_Ariza = pastorizeNodeRedModel.Hidrofor_HamSu2_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su2_Motor_1_Durum = pastorizeNodeRedModel.Hidrofor_HamSu2_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 177, SayacId = 0, EtiketId = 1007, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_1_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 177, SayacId = 0, EtiketId = 1008, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_1_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 177, SayacId = 0, EtiketId = 1009, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_1_Durum });

                            var Hidrofor_Ham_Su2_Motor_2_Calisiyor = pastorizeNodeRedModel.Hidrofor_HamSu2_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su2_Motor_2_Ariza = pastorizeNodeRedModel.Hidrofor_HamSu2_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su2_Motor_2_Durum = pastorizeNodeRedModel.Hidrofor_HamSu2_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 178, SayacId = 0, EtiketId = 1010, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_2_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 178, SayacId = 0, EtiketId = 1011, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_2_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 178, SayacId = 0, EtiketId = 1012, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_2_Durum });

                            var Hidrofor_Ham_Su2_Motor_3_Calisiyor = pastorizeNodeRedModel.Hidrofor_HamSu2_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Ham_Su2_Motor_3_Ariza = pastorizeNodeRedModel.Hidrofor_HamSu2_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_Ham_Su2_Motor_3_Durum = pastorizeNodeRedModel.Hidrofor_HamSu2_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 179, SayacId = 0, EtiketId = 1013, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_3_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 179, SayacId = 0, EtiketId = 1014, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_3_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 179, SayacId = 0, EtiketId = 1015, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Motor_3_Durum });

                            var Hidrofor_Ham_Su2_Basinc = pastorizeNodeRedModel.Hidrofor_HamSu2_Basinc;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 176, SayacId = 0, EtiketId = 1016, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Ham_Su2_Basinc });


                            var Hidrofor_Yumusak_Su_Cmd_Calis = pastorizeNodeRedModel.Hidrofor_YumusakSu_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Rpr_Mode = pastorizeNodeRedModel.Hidrofor_YumusakSu_Rpr_Mode;
                            var Hidrofor_Yumusak_Su_Rpr_State = pastorizeNodeRedModel.Hidrofor_YumusakSu_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 180, SayacId = 0, EtiketId = 1017, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 180, SayacId = 0, EtiketId = 1018, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 180, SayacId = 0, EtiketId = 1019, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Rpr_State });

                            var Hidrofor_Yumusak_Su_Motor_1_Calisiyor = pastorizeNodeRedModel.Hidrofor_YumusakSu_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_1_Ariza = pastorizeNodeRedModel.Hidrofor_YumusakSu_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_1_Durum = pastorizeNodeRedModel.Hidrofor_YumusakSu_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 181, SayacId = 0, EtiketId = 1020, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_1_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 181, SayacId = 0, EtiketId = 1021, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_1_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 181, SayacId = 0, EtiketId = 1022, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_1_Durum });

                            var Hidrofor_Yumusak_Su_Motor_2_Calisiyor = pastorizeNodeRedModel.Hidrofor_YumusakSu_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_2_Ariza = pastorizeNodeRedModel.Hidrofor_YumusakSu_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_2_Durum = pastorizeNodeRedModel.Hidrofor_YumusakSu_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 182, SayacId = 0, EtiketId = 1023, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_2_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 182, SayacId = 0, EtiketId = 1024, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_2_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 182, SayacId = 0, EtiketId = 1025, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_2_Durum });

                            var Hidrofor_Yumusak_Su_Motor_3_Calisiyor = pastorizeNodeRedModel.Hidrofor_YumusakSu_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_3_Ariza = pastorizeNodeRedModel.Hidrofor_YumusakSu_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_Yumusak_Su_Motor_3_Durum = pastorizeNodeRedModel.Hidrofor_YumusakSu_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 183, SayacId = 0, EtiketId = 1026, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_3_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 183, SayacId = 0, EtiketId = 1027, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_3_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 183, SayacId = 0, EtiketId = 1028, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Motor_3_Durum });

                            var Hidrofor_Yumusak_Su_Basinc = pastorizeNodeRedModel.Hidrofor_YumusakSu_Basinc;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 180, SayacId = 0, EtiketId = 1029, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yumusak_Su_Basinc });


                            var Hidrofor_Osmoz_Su_Cmd_Calis = pastorizeNodeRedModel.Hidrofor_OsmozSu_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Osmoz_Su_Rpr_Mode = pastorizeNodeRedModel.Hidrofor_OsmozSu_Rpr_Mode;
                            var Hidrofor_Osmoz_Su_Rpr_State = pastorizeNodeRedModel.Hidrofor_OsmozSu_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 184, SayacId = 0, EtiketId = 1030, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 184, SayacId = 0, EtiketId = 1031, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 184, SayacId = 0, EtiketId = 1032, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Rpr_State });

                            var Hidrofor_Osmoz_Su_Motor_1_Calisiyor = pastorizeNodeRedModel.Hidrofor_OsmozSu_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Osmoz_Su_Motor_1_Ariza = pastorizeNodeRedModel.Hidrofor_OsmozSu_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_Osmoz_Su_Motor_1_Durum = pastorizeNodeRedModel.Hidrofor_OsmozSu_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 185, SayacId = 0, EtiketId = 1033, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_1_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 185, SayacId = 0, EtiketId = 1034, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_1_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 185, SayacId = 0, EtiketId = 1035, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_1_Durum });

                            var Hidrofor_Osmoz_Su_Motor_2_Calisiyor = pastorizeNodeRedModel.Hidrofor_OsmozSu_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Osmoz_Su_Motor_2_Ariza = pastorizeNodeRedModel.Hidrofor_OsmozSu_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_Osmoz_Su_Motor_2_Durum = pastorizeNodeRedModel.Hidrofor_OsmozSu_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 186, SayacId = 0, EtiketId = 1036, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_2_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 186, SayacId = 0, EtiketId = 1037, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_2_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 186, SayacId = 0, EtiketId = 1038, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_2_Durum });

                            var Hidrofor_Osmoz_Su_Motor_3_Calisiyor = pastorizeNodeRedModel.Hidrofor_OsmozSu_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Osmoz_Su_Motor_3_Ariza = pastorizeNodeRedModel.Hidrofor_OsmozSu_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_Osmoz_Su_Motor_3_Durum = pastorizeNodeRedModel.Hidrofor_OsmozSu_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 187, SayacId = 0, EtiketId = 1039, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_3_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 187, SayacId = 0, EtiketId = 1040, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_3_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 187, SayacId = 0, EtiketId = 1041, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Motor_3_Durum });

                            var Hidrofor_Osmoz_Su_Basinc = pastorizeNodeRedModel.Hidrofor_OsmozSu_Basinc;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 184, SayacId = 0, EtiketId = 1042, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Osmoz_Su_Basinc });


                            var Hidrofor_Sicak_Su_Cmd_Calis = pastorizeNodeRedModel.Hidrofor_SicakSu_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Sicak_Su_Rpr_Mode = pastorizeNodeRedModel.Hidrofor_SicakSu_Rpr_Mode;
                            var Hidrofor_Sicak_Su_Rpr_State = pastorizeNodeRedModel.Hidrofor_SicakSu_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 188, SayacId = 0, EtiketId = 1043, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 188, SayacId = 0, EtiketId = 1044, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 188, SayacId = 0, EtiketId = 1045, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Rpr_State });

                            var Hidrofor_Sicak_Su_Motor_1_Calisiyor = pastorizeNodeRedModel.Hidrofor_SicakSu_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Sicak_Su_Motor_1_Ariza = pastorizeNodeRedModel.Hidrofor_SicakSu_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_Sicak_Su_Motor_1_Durum = pastorizeNodeRedModel.Hidrofor_SicakSu_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 189, SayacId = 0, EtiketId = 1046, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Motor_1_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 189, SayacId = 0, EtiketId = 1047, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Motor_1_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 189, SayacId = 0, EtiketId = 1048, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Motor_1_Durum });

                            var Hidrofor_Sicak_Su_Motor_2_Calisiyor = pastorizeNodeRedModel.Hidrofor_SicakSu_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Sicak_Su_Motor_2_Ariza = pastorizeNodeRedModel.Hidrofor_SicakSu_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_Sicak_Su_Motor_2_Durum = pastorizeNodeRedModel.Hidrofor_SicakSu_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 190, SayacId = 0, EtiketId = 1049, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Motor_2_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 190, SayacId = 0, EtiketId = 1050, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Motor_2_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 190, SayacId = 0, EtiketId = 1051, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Motor_2_Durum });

                            var Hidrofor_Sicak_Su_Motor_3_Calisiyor = pastorizeNodeRedModel.Hidrofor_SicakSu_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Sicak_Su_Motor_3_Ariza = pastorizeNodeRedModel.Hidrofor_SicakSu_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_Sicak_Su_Motor_3_Durum = pastorizeNodeRedModel.Hidrofor_SicakSu_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 191, SayacId = 0, EtiketId = 1052, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Motor_3_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 191, SayacId = 0, EtiketId = 1053, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Motor_3_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 191, SayacId = 0, EtiketId = 1054, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Motor_3_Durum });

                            var Hidrofor_Sicak_Su_Basinc = pastorizeNodeRedModel.Hidrofor_SicakSu_Basinc;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 188, SayacId = 0, EtiketId = 1055, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Basinc });


                            var Hidrofor_Yangin_Su_Cmd_Calis = pastorizeNodeRedModel.Hidrofor_YanginSu_Cmd_Calis == true ? 1 : 0;
                            var Hidrofor_Yangin_Su_Rpr_Mode = pastorizeNodeRedModel.Hidrofor_YanginSu_Rpr_Mode;
                            var Hidrofor_Yangin_Su_Rpr_State = pastorizeNodeRedModel.Hidrofor_YanginSu_Rpr_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 192, SayacId = 0, EtiketId = 1056, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Su_Cmd_Calis });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 192, SayacId = 0, EtiketId = 1057, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Su_Rpr_Mode });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 192, SayacId = 0, EtiketId = 1058, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Su_Rpr_State });

                            var Hidrofor_Yangin_Su_Motor_1_Calisiyor = pastorizeNodeRedModel.Hidrofor_YanginSu_Motor1_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yangin_Su_Motor_1_Ariza = pastorizeNodeRedModel.Hidrofor_YanginSu_Motor1_Ariza == true ? 1 : 0;
                            var Hidrofor_Yangin_Su_Motor_1_Durum = pastorizeNodeRedModel.Hidrofor_YanginSu_Motor1_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 193, SayacId = 0, EtiketId = 1059, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Su_Motor_1_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 193, SayacId = 0, EtiketId = 1060, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Su_Motor_1_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 193, SayacId = 0, EtiketId = 1061, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Su_Motor_1_Durum });

                            var Hidrofor_Yangin_Su_Motor_2_Calisiyor = pastorizeNodeRedModel.Hidrofor_YanginSu_Motor2_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yangin_Su_Motor_2_Ariza = pastorizeNodeRedModel.Hidrofor_YanginSu_Motor2_Ariza == true ? 1 : 0;
                            var Hidrofor_Yangin_Su_Motor_2_Durum = pastorizeNodeRedModel.Hidrofor_YanginSu_Motor2_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 194, SayacId = 0, EtiketId = 1062, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Motor_2_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 194, SayacId = 0, EtiketId = 1063, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Motor_2_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 194, SayacId = 0, EtiketId = 1064, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Sicak_Su_Motor_2_Durum });

                            var Hidrofor_Yangin_Su_Motor_3_Calisiyor = pastorizeNodeRedModel.Hidrofor_YanginSu_Motor3_Calisiyor == true ? 1 : 0;
                            var Hidrofor_Yangin_Su_Motor_3_Ariza = pastorizeNodeRedModel.Hidrofor_YanginSu_Motor3_Ariza == true ? 1 : 0;
                            var Hidrofor_Yangin_Su_Motor_3_Durum = pastorizeNodeRedModel.Hidrofor_YanginSu_Motor3_Durum;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 195, SayacId = 0, EtiketId = 1065, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Su_Motor_3_Calisiyor });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 195, SayacId = 0, EtiketId = 1066, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Su_Motor_3_Ariza });
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 195, SayacId = 0, EtiketId = 1067, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Su_Motor_3_Durum });

                            var Hidrofor_Yangin_Su_Basinc = pastorizeNodeRedModel.Hidrofor_YanginSu_Basinc;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 192, SayacId = 0, EtiketId = 1068, KayitZamani = DateTime.Now, Deger = (decimal?)Hidrofor_Yangin_Su_Basinc });


                            var Jenerator_SebekeElektrik = pastorizeNodeRedModel.SebekedeElektrikVar == true ? 1 : 0;
                            etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 1069, KayitZamani = DateTime.Now, Deger = (decimal?)Jenerator_SebekeElektrik });
                        }
                    }
                    else
                    {
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 902, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                        etiketLogs.Add(new EtiketLogYeni { LokasyonId = lokasyonId, SuKaynakId = 0, SayacId = 0, EtiketId = 903, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                    }
                }
            }
            return etiketLogs;
        }


        #endregion EtiketLogData

        #region SuKaynakLogData
        private async Task<IEnumerable<SuKaynakLogYeni>> GetSuKaynakLogYeniDataForBulkInsertKuluckaAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 17;
            var now = DateTime.Now;
            List<SuKaynakLogYeni> suKaynakLogs = new List<SuKaynakLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model != null)
                {
                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            KuluckaNodeRedModel kuluckaNodeRedModel = JsonSerializer.Deserialize<KuluckaNodeRedModel>(model.Veriler);

                            var Tank_Sens_Basinc = kuluckaNodeRedModel.Tank_SensBasinc;
                            var Tank_SuSeviye = kuluckaNodeRedModel.Tank_SuSeviye;
                            var Tank_SuSeviye_Yuzdesi = kuluckaNodeRedModel.Tank_SuSeviye_Yuzde;
                            var Tank_SuHacim = kuluckaNodeRedModel.Tank_SuHacim;
                            var Tank_SuHacim_Yuzdesi = kuluckaNodeRedModel.Tank_SuHacim_Yuzde;
                            var Tank_T_TankHacim = kuluckaNodeRedModel.Tank_T_TankHacim;

                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 4,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_T_TankHacim
                            });

                            var DM_MezarlýkYaný_1_Connected = kuluckaNodeRedModel.DM_MezarlikYani_1_Connected == true ? 1 : 0;
                            var DM_MezarlýkYaný_1_Total = kuluckaNodeRedModel.DM_MezarlikYani_1_Total;
                            var DM_MezarlýkYaný_1_Debi = kuluckaNodeRedModel.DM_MezarlikYani_1_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 1,
                                KayitZamani = now,
                                Debi = (decimal?)DM_MezarlýkYaný_1_Debi,
                                DebiSayac = (decimal?)DM_MezarlýkYaný_1_Total,
                            });

                            var DM_MezarlýkYaný_2_Connected = kuluckaNodeRedModel.DM_MezarlikYani_2_Connected == true ? 1 : 0;
                            var DM_MezarlýkYaný_2_Total = kuluckaNodeRedModel.DM_MezarlikYani_2_Total;
                            var DM_MezarlýkYaný_2_Debi = kuluckaNodeRedModel.DM_MezarlikYani_2_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 2,
                                KayitZamani = now,
                                Debi = (decimal?)DM_MezarlýkYaný_2_Debi,
                                DebiSayac = (decimal?)DM_MezarlýkYaný_2_Total,
                            });

                            var DM_YanTarla_Connected = kuluckaNodeRedModel.DM_YanTarla_Connected == true ? 1 : 0;
                            var DM_YanTarla_Total = kuluckaNodeRedModel.DM_YanTarla_Total;
                            var DM_YanTarla_Debi = kuluckaNodeRedModel.DM_YanTarla_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 3,
                                KayitZamani = now,
                                Debi = (decimal?)DM_YanTarla_Debi,
                                DebiSayac = (decimal?)DM_YanTarla_Total,
                            });

                            var DM_Uretim_Connected = kuluckaNodeRedModel.DM_Uretim_Connected == true ? 1 : 0;
                            var DM_Uretim_Total = kuluckaNodeRedModel.DM_Uretim_Total;
                            var DM_Uretim_Debi = kuluckaNodeRedModel.DM_Uretim_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 6,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Uretim_Debi,
                                DebiSayac = (decimal?)DM_Uretim_Total,
                            });

                            var DM_Kayislar_Connected = kuluckaNodeRedModel.DM_Kayislar_Connected == true ? 1 : 0;
                            var DM_Kayislar_Total = kuluckaNodeRedModel.DM_Kayislar_Total;
                            var DM_Kayislar_Debi = kuluckaNodeRedModel.DM_Kayislar_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 5,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kayislar_Debi,
                                DebiSayac = (decimal?)DM_Kayislar_Total,
                            });
                        }
                    }
                }
            }
            return suKaynakLogs;
        }
        private async Task<IEnumerable<SuKaynakLogYeni>> GetSuKaynakLogYeniDataForBulkInsertRahmiyeAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 13;
            var now = DateTime.Now;
            List<SuKaynakLogYeni> suKaynakLogs = new List<SuKaynakLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model != null)
                {
                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            RahmiyeNodeRedModel rahmiyeNodeRedModel = JsonSerializer.Deserialize<RahmiyeNodeRedModel>(model.Veriler);

                            var Tank_Sens_Basinc = rahmiyeNodeRedModel.Tank_SensBasinc;
                            var Tank_SuSeviye = rahmiyeNodeRedModel.Tank_SuSeviye;
                            var Tank_SuSeviye_Yuzdesi = rahmiyeNodeRedModel.Tank_SuSeviye_Yuzde;
                            var Tank_SuHacim = rahmiyeNodeRedModel.Tank_SuHacim;
                            var Tank_SuHacim_Yuzdesi = rahmiyeNodeRedModel.Tank_SuHacimYuzde;
                            var Tank_T_TankHacim = rahmiyeNodeRedModel.Tank_T_TankHacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 16,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_T_TankHacim
                            });

                            var DM_Kuyu_1_Total = rahmiyeNodeRedModel.DM_Kuyu_1_Total;
                            var DM_Kuyu_1_Debi = rahmiyeNodeRedModel.DM_Kuyu_1_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 10,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_1_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_1_Total,
                            });

                            var DM_Kuyu_2_Total = rahmiyeNodeRedModel.DM_Kuyu_2_Total;
                            var DM_Kuyu_2_Debi = rahmiyeNodeRedModel.DM_Kuyu_2_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 11,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_2_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_2_Total,
                            });

                            var DM_Kuyu_3_Total = rahmiyeNodeRedModel.DM_Kuyu_3_Total;
                            var DM_Kuyu_3_Debi = rahmiyeNodeRedModel.DM_Kuyu_3_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 12,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_3_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_3_Total,
                            });

                            var DM_Kuyu_4_Total = rahmiyeNodeRedModel.DM_Kuyu_4_Total;
                            var DM_Kuyu_4_Debi = rahmiyeNodeRedModel.DM_Kuyu_4_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 13,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_4_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_4_Total,
                            });
                        }
                    }
                }
            }
            return suKaynakLogs;
        }
        private async Task<IEnumerable<SuKaynakLogYeni>> GetSuKaynakLogYeniDataForBulkInsertKesimhaneAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 10;
            var now = DateTime.Now;
            List<SuKaynakLogYeni> suKaynakLogs = new List<SuKaynakLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model != null)
                {
                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            KesimhaneNodeRedModel kesimhaneNodeRedModel = JsonSerializer.Deserialize<KesimhaneNodeRedModel>(model.Veriler);

                            var TankButun_Sens_Basinc = kesimhaneNodeRedModel.TankButun_SensBasinc;
                            var TankButun_SuSeviye = kesimhaneNodeRedModel.TankButun_SuSeviye;
                            var TankButun_SuHacim = kesimhaneNodeRedModel.TankButun_SuHacim;
                            var TankButun_SuHacim_Yuzdesi = kesimhaneNodeRedModel.TankButun_SuHacim_Yuzde;
                            var TankButun_T_TankHacim = kesimhaneNodeRedModel.TankButun_Hacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 40,
                                KayitZamani = now,
                                Basinc = (decimal?)TankButun_Sens_Basinc,
                                SeviyeMetre = (decimal?)TankButun_SuSeviye,
                                SuHacim = (decimal?)TankButun_SuHacim,
                                TankHacim = (decimal?)TankButun_T_TankHacim
                            });

                            var Tank1_SuSeviye = kesimhaneNodeRedModel.Tank_1_SuSeviye;
                            var Tank1_SuSeviye_Yuzdesi = kesimhaneNodeRedModel.Tank_1_SuSeviye_Yuzde;
                            var Tank1_SuHacim = kesimhaneNodeRedModel.Tank_1_SuSeviye_Hacim;
                            var Tank1_SuHacim_Yuzdesi = kesimhaneNodeRedModel.Tank_1_SuSeviye_Hacim_Yuzde;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 41,
                                KayitZamani = now,
                                SeviyeMetre = (decimal?)Tank1_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank1_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank1_SuHacim,
                                SuHacimYuzde = (decimal?)Tank1_SuHacim_Yuzdesi,
                            });

                            var Tank2_SuSeviye = kesimhaneNodeRedModel.Tank_2_SuSeviye;
                            var Tank2_SuSeviye_Yuzdesi = kesimhaneNodeRedModel.Tank_2_SuSeviye_Yuzde;
                            var Tank2_SuHacim = kesimhaneNodeRedModel.Tank_2_SuSeviye_Hacim;
                            var Tank2_SuHacim_Yuzdesi = kesimhaneNodeRedModel.Tank_2_SuSeviye_Hacim_Yuzde;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 42,
                                KayitZamani = now,
                                SeviyeMetre = (decimal?)Tank2_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank2_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank2_SuHacim,
                                SuHacimYuzde = (decimal?)Tank2_SuHacim_Yuzdesi,
                            });

                            var DM_Tank_Total = kesimhaneNodeRedModel.DM_Tank_Total;
                            var DM_Tank_Debi = kesimhaneNodeRedModel.DM_Tank_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 23,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Tank_Debi,
                                DebiSayac = (decimal?)DM_Tank_Total,
                            });
                        }
                    }
                }
            }
            return suKaynakLogs;
        }
        private async Task<IEnumerable<SuKaynakLogYeni>> GetSuKaynakLogYeniDataForBulkInsertCayKarsisiAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 14;
            var now = DateTime.Now;
            List<SuKaynakLogYeni> suKaynakLogs = new List<SuKaynakLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model != null)
                {
                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            CayKarsisiNodeRedModel cayKarsisiNodeRedModel = JsonSerializer.Deserialize<CayKarsisiNodeRedModel>(model.Veriler);

                            var Tank_1_Sens_Basinc = cayKarsisiNodeRedModel.Tank_1_SensBasinc;
                            var Tank_1_SuSeviye = cayKarsisiNodeRedModel.Tank_1_SuSeviye;
                            var Tank_1_SuSeviye_Yuzdesi = cayKarsisiNodeRedModel.Tank_1_SuSeviye_Yuzde;
                            var Tank_1_SuHacim = cayKarsisiNodeRedModel.Tank_1_SuHacim;
                            var Tank_1_SuHacim_Yuzdesi = cayKarsisiNodeRedModel.Tank_1_SuHacim_Yuzde;
                            var Tank_1_T_TankHacim = cayKarsisiNodeRedModel.Tank_1_T_Tank_Hacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 21,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_1_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_1_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_1_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_1_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_1_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_1_T_TankHacim
                            });

                            var Tank_2_Sens_Basinc = cayKarsisiNodeRedModel.Tank_2_SensBasinc;
                            var Tank_2_SuSeviye = cayKarsisiNodeRedModel.Tank_2_SuSeviye;
                            var Tank_2_SuSeviye_Yuzdesi = cayKarsisiNodeRedModel.Tank_2_SuSeviye_Yuzde;
                            var Tank_2_SuHacim = cayKarsisiNodeRedModel.Tank_2_SuHacim;
                            var Tank_2_SuHacim_Yuzdesi = cayKarsisiNodeRedModel.Tank_2_SuHacim_Yuzde;
                            var Tank_2_T_TankHacim = cayKarsisiNodeRedModel.Tank_2_T_Tank_Hacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 22,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_2_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_2_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_2_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_2_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_2_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_2_T_TankHacim
                            });

                            var Tank_3_Sens_Basinc = cayKarsisiNodeRedModel.Tank_3_SensBasinc;
                            var Tank_3_SuSeviye = cayKarsisiNodeRedModel.Tank_3_SuSeviye;
                            var Tank_3_SuSeviye_Yuzdesi = cayKarsisiNodeRedModel.Tank_3_SuSeviye_Yuzde;
                            var Tank_3_SuHacim = cayKarsisiNodeRedModel.Tank_3_SuHacim;
                            var Tank_3_SuHacim_Yuzdesi = cayKarsisiNodeRedModel.Tank_3_SuHacim_Yuzde;
                            var Tank_3_T_TankHacim = cayKarsisiNodeRedModel.Tank_3_T_Tank_Hacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 70,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_3_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_3_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_3_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_3_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_3_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_3_T_TankHacim
                            });

                            var DM_Kuyu_1_Total = cayKarsisiNodeRedModel.DM_Kuyu_1_Total;
                            var DM_Kuyu_1_Debi = cayKarsisiNodeRedModel.DM_Kuyu_1_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 16,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_1_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_1_Total,
                            });

                            var DM_Kuyu_2_Total = cayKarsisiNodeRedModel.DM_Kuyu_2_Total;
                            var DM_Kuyu_2_Debi = cayKarsisiNodeRedModel.DM_Kuyu_2_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 17,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_2_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_2_Total,
                            });

                            var DM_Kuyu_3_Total = cayKarsisiNodeRedModel.DM_Kuyu_3_Total;
                            var DM_Kuyu_3_Debi = cayKarsisiNodeRedModel.DM_Kuyu_3_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 18,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_3_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_3_Total,
                            });

                            var DM_Kuyu_4_Total = cayKarsisiNodeRedModel.DM_Kuyu_4_Total;
                            var DM_Kuyu_4_Debi = cayKarsisiNodeRedModel.DM_Kuyu_4_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 19,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_4_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_4_Total,
                            });

                            var DM_Kuyu_5_Total = cayKarsisiNodeRedModel.DM_Kuyu_5_Total;
                            var DM_Kuyu_5_Debi = cayKarsisiNodeRedModel.DM_Kuyu_5_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 20,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_5_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_5_Total,
                            });

                            var DM_Kuyu_6_Total = cayKarsisiNodeRedModel.DM_Kuyu_10_Total;
                            var DM_Kuyu_6_Debi = cayKarsisiNodeRedModel.DM_Kuyu_10_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 58,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_6_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_6_Total,
                            });

                            var DM_Kuyu_7_Total = cayKarsisiNodeRedModel.DM_Kuyu_6_7_Total;
                            var DM_Kuyu_7_Debi = cayKarsisiNodeRedModel.DM_Kuyu_6_7_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 59,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_7_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_7_Total,
                            });

                            var DM_Kuyu_8_9_Total = cayKarsisiNodeRedModel.DM_Kuyu_8_9_Total;
                            var DM_Kuyu_8_9_Debi = cayKarsisiNodeRedModel.DM_Kuyu_8_9_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 22,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_8_9_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_8_9_Total,
                            });

                            var DM_Rahmiye_Total = cayKarsisiNodeRedModel.DM_Rahmiye_Total;
                            var DM_Rahmiye_Debi = cayKarsisiNodeRedModel.DM_Rahmiye_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 15,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Rahmiye_Debi,
                                DebiSayac = (decimal?)DM_Rahmiye_Total,
                            });

                            var DM_Pastorize_Total = cayKarsisiNodeRedModel.DM_Pastorize_Total;
                            var DM_Pastorize_Debi = cayKarsisiNodeRedModel.DM_Pastorize_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 24,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Pastorize_Debi,
                                DebiSayac = (decimal?)DM_Pastorize_Total,
                            });

                            var DM_IleriIslem_Total = cayKarsisiNodeRedModel.DM_IleriIslem_Total;
                            var DM_IleriIslem_Debi = cayKarsisiNodeRedModel.DM_IleriIslem_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 25,
                                KayitZamani = now,
                                Debi = (decimal?)DM_IleriIslem_Debi,
                                DebiSayac = (decimal?)DM_IleriIslem_Total,
                            });
                        }
                    }
                }
            }
            return suKaynakLogs;
        }
        private async Task<IEnumerable<SuKaynakLogYeni>> GetSuKaynakLogYeniDataForBulkInsertOSBYemAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 11;
            var now = DateTime.Now;
            List<SuKaynakLogYeni> suKaynakLogs = new List<SuKaynakLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model != null)
                {
                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            OSBYemNodeRedModel osbYemNodeRedModel = JsonSerializer.Deserialize<OSBYemNodeRedModel>(model.Veriler);

                            var Tank_Sens_Basinc = osbYemNodeRedModel.Tank_SensBasinc;
                            var Tank_SuSeviye = osbYemNodeRedModel.Tank_SuSeviye;
                            var Tank_SuSeviye_Yuzdesi = osbYemNodeRedModel.Tank_SuSeviye_Yuzde;
                            var Tank_SuHacim = osbYemNodeRedModel.Tank_SuHacim;
                            var Tank_SuHacim_Yuzdesi = osbYemNodeRedModel.Tank_SuHacim_Yuzde;
                            var Tank_T_TankHacim = osbYemNodeRedModel.Tank_T_TankHacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 47,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_T_TankHacim
                            });

                            var YanginTank_Sens_Basinc = osbYemNodeRedModel.YanginTank_SensBasinc;
                            var YanginTank_SuSeviye = osbYemNodeRedModel.YanginTank_SuSeviye;
                            var YanginTank_SuSeviye_Yuzdesi = osbYemNodeRedModel.YanginTank_SuSeviye_Yuzde;
                            var YanginTank_SuHacim = osbYemNodeRedModel.YanginTank_SuHacim;
                            var YanginTank_SuHacim_Yuzdesi = osbYemNodeRedModel.YanginTank_SuHacim_Yuzde;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 68,
                                KayitZamani = now,
                                Basinc = (decimal?)YanginTank_Sens_Basinc,
                                SeviyeMetre = (decimal?)YanginTank_SuSeviye,
                                SeviyeYuzde = (decimal?)YanginTank_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)YanginTank_SuHacim,
                                SuHacimYuzde = (decimal?)YanginTank_SuHacim_Yuzdesi,
                                TankHacim = 0
                            });

                            var DM_Kuyu_1_Total = osbYemNodeRedModel.DM_Kuyu_1_Total;
                            var DM_Kuyu_1_Debi = osbYemNodeRedModel.DM_Kuyu_1_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 26,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_1_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_1_Total,
                            });

                            var DM_Kuyu_2_Total = osbYemNodeRedModel.DM_Kuyu_2_Total;
                            var DM_Kuyu_2_Debi = osbYemNodeRedModel.DM_Kuyu_2_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 27,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_2_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_2_Total,
                            });

                            var DM_Uretim_Total = osbYemNodeRedModel.DM_Kuyu_Uretim_Total;
                            var DM_Uretim_Debi = osbYemNodeRedModel.DM_Kuyu_Uretim_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 28,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Uretim_Debi,
                                DebiSayac = (decimal?)DM_Uretim_Total,
                            });

                            var DM_Kazan_Total = osbYemNodeRedModel.DM_Kuyu_Kazan_Total;
                            var DM_Kazan_Debi = osbYemNodeRedModel.DM_Kuyu_Kazan_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 29,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kazan_Debi,
                                DebiSayac = (decimal?)DM_Kazan_Total,
                            });
                        }
                    }
                }
            }
            return suKaynakLogs;
        }
        private async Task<IEnumerable<SuKaynakLogYeni>> GetSuKaynakLogYeniDataForBulkInsertViolAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 15;
            var now = DateTime.Now;
            List<SuKaynakLogYeni> suKaynakLogs = new List<SuKaynakLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model != null)
                {
                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            ViolNodeRedModel violNodeRedModel = JsonSerializer.Deserialize<ViolNodeRedModel>(model.Veriler);

                            var Tank_Sens_Basinc = violNodeRedModel.Tank_SensBasinc;
                            var Tank_SuSeviye = violNodeRedModel.Tank_SuSeviye;
                            var Tank_SuSeviye_Yuzdesi = violNodeRedModel.Tank_SuSeviye_Yuzde;
                            var Tank_SuHacim = violNodeRedModel.Tank_SuHacim;
                            var Tank_SuHacim_Yuzdesi = violNodeRedModel.Tank_SuHacim_Yuzde;
                            var Tank_T_TankHacim = violNodeRedModel.Tank_T_TankHacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 56,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_T_TankHacim
                            });

                            var DM_Kuyu_1_Total = violNodeRedModel.DM_Kuyu_1_Total;
                            var DM_Kuyu_1_Debi = violNodeRedModel.DM_Kuyu_1_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 31,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_1_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_1_Total,
                            });

                            var DM_Kuyu_2_Total = violNodeRedModel.DM_Kuyu_2_Total;
                            var DM_Kuyu_2_Debi = violNodeRedModel.DM_Kuyu_2_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 32,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu_2_Debi,
                                DebiSayac = (decimal?)DM_Kuyu_2_Total,
                            });

                            var DM_Uretim_Total = violNodeRedModel.DM_Uretim_Total;
                            var DM_Uretim_Debi = violNodeRedModel.DM_Uretim_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 33,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Uretim_Debi,
                                DebiSayac = (decimal?)DM_Uretim_Total,
                            });
                        }
                    }
                }
            }
            return suKaynakLogs;
        }
        private async Task<IEnumerable<SuKaynakLogYeni>> GetSuKaynakLogYeniDataForBulkInsertKayislarAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 22;
            var now = DateTime.Now;
            List<SuKaynakLogYeni> suKaynakLogs = new List<SuKaynakLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model != null)
                {
                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            KayislarNodeRedModel kayislarNodeRedModel = JsonSerializer.Deserialize<KayislarNodeRedModel>(model.Veriler);
                            var Tank_Sens_Basinc = kayislarNodeRedModel.TankSensBasinc;
                            var Tank_SuSeviye = kayislarNodeRedModel.TankSuSeviye;
                            var Tank_SuSeviye_Yuzdesi = kayislarNodeRedModel.TankSuSeviye_Yuzde;
                            var Tank_SuHacim = kayislarNodeRedModel.TankSuHacim;
                            var Tank_SuHacim_Yuzdesi = kayislarNodeRedModel.TankSuHacim_Yuzde;
                            var Tank_T_TankHacim = kayislarNodeRedModel.T_TankHacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 10,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_T_TankHacim
                            });

                            var DM_Kayislar_Total = kayislarNodeRedModel.DM_Kayislar_Total;
                            var DM_Kayislar_Debi = kayislarNodeRedModel.DM_Kayislar_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 7,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kayislar_Debi,
                                DebiSayac = (decimal?)DM_Kayislar_Total,
                            });

                            var DM_Yarkahane_Total = kayislarNodeRedModel.DM_Yarkahane_Total;
                            var DM_Yarkahane_Debi = kayislarNodeRedModel.DM_Yarkahane_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 8,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yarkahane_Debi,
                                DebiSayac = (decimal?)DM_Yarkahane_Total,
                            });
                        }
                    }
                }
            }
            return suKaynakLogs;
        }
        private async Task<IEnumerable<SuKaynakLogYeni>> GetSuKaynakLogYeniDataForBulkInsertYumusakSuAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 62;
            var now = DateTime.Now;
            List<SuKaynakLogYeni> suKaynakLogs = new List<SuKaynakLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model != null)
                {
                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            YumusakSuNodeRedModel yumusakSuNodeRedModel = JsonSerializer.Deserialize<YumusakSuNodeRedModel>(model.Veriler);
                            var Tank_Sens_Basinc = yumusakSuNodeRedModel.Tank_1_SensBasinc;
                            var Tank_SuSeviye = yumusakSuNodeRedModel.Tank_1_SuSeviye;
                            var Tank_SuSeviye_Yuzdesi = yumusakSuNodeRedModel.Tank_1_SuSeviye_Yuzde;
                            var Tank_SuHacim = yumusakSuNodeRedModel.Tank_1_SuHacim;
                            var Tank_SuHacim_Yuzdesi = yumusakSuNodeRedModel.Tank_1_SuHacim_Yuzde;
                            var Tank_T_TankHacim = yumusakSuNodeRedModel.Tank_1_T_TankHacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 72,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_T_TankHacim
                            });

                            var DM_Kesimhane12_Total = yumusakSuNodeRedModel.DM_Uretim_12_Total;
                            var DM_Kesimhane12_Debi = yumusakSuNodeRedModel.DM_Uretim_12_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 61,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kesimhane12_Debi,
                                DebiSayac = (decimal?)DM_Kesimhane12_Total,
                            });

                            var DM_Kesimhane3_Total = yumusakSuNodeRedModel.DM_Uretim_3_Total;
                            var DM_Kesimhane3_Debi = yumusakSuNodeRedModel.DM_Uretim_3_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 62,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kesimhane3_Debi,
                                DebiSayac = (decimal?)DM_Kesimhane3_Total,
                            });
                        }
                    }
                }
            }
            return suKaynakLogs;
        }
        private async Task<IEnumerable<SuKaynakLogYeni>> GetSuKaynakLogYeniDataForBulkInsertRenderingAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 58;
            var now = DateTime.Now;
            List<SuKaynakLogYeni> suKaynakLogs = new List<SuKaynakLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model != null)
                {
                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            RenderingNodeRedModel renderingNodeRedModel = JsonSerializer.Deserialize<RenderingNodeRedModel>(model.Veriler);
                            var Tank_Sens_Basinc = renderingNodeRedModel.Tank_1_SensBasinc;
                            var Tank_SuSeviye = renderingNodeRedModel.Tank_1_SuSeviye;
                            var Tank_SuSeviye_Yuzdesi = renderingNodeRedModel.Tank_1_SuSeviye_Yuzde;
                            var Tank_SuHacim = renderingNodeRedModel.Tank_1_SuHacim;
                            var Tank_SuHacim_Yuzdesi = renderingNodeRedModel.Tank_1_SuHacim_Yuzde;
                            var Tank_T_TankHacim = renderingNodeRedModel.Tank_1_T_TankHacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 79,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_T_TankHacim
                            });

                            var DM_SicakSuRing_Total = renderingNodeRedModel.DM_SicakSuRingHatti_Total;
                            var DM_SicakSuRing_Debi = renderingNodeRedModel.DM_SicakSuRingHatti_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 64,
                                KayitZamani = now,
                                Debi = (decimal?)DM_SicakSuRing_Debi,
                                DebiSayac = (decimal?)DM_SicakSuRing_Total,
                            });

                            var DM_SicakSu_Total = renderingNodeRedModel.DM_SicakSuHatti_Total;
                            var DM_SicakSu_Debi = renderingNodeRedModel.DM_SicakSuHatti_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 65,
                                KayitZamani = now,
                                Debi = (decimal?)DM_SicakSu_Debi,
                                DebiSayac = (decimal?)DM_SicakSu_Total,
                            });
                        }
                    }
                }
            }
            return suKaynakLogs;
        }
        private async Task<IEnumerable<SuKaynakLogYeni>> GetSuKaynakLogYeniDataForBulkInsertSuIslemeAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 63;
            var now = DateTime.Now;
            List<SuKaynakLogYeni> suKaynakLogs = new List<SuKaynakLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model != null)
                {
                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            SuIslemeNodeRedModel suIslemeNodeRedModel = JsonSerializer.Deserialize<SuIslemeNodeRedModel>(model.Veriler);
                            var DM_Osmoz_Besleme_Total = suIslemeNodeRedModel.DM_OsmozBesleme_Total;
                            var DM_Osmoz_Besleme_Debi = suIslemeNodeRedModel.DM_OsmozBesleme_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 66,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Osmoz_Besleme_Total,
                                DebiSayac = (decimal?)DM_Osmoz_Besleme_Debi,
                            });

                            var DM_Kesimhane_12_Total = suIslemeNodeRedModel.DM_Kesimhane1ve2Uretim_Total;
                            var DM_Kesimhane_12_Debi = suIslemeNodeRedModel.DM_Kesimhane1ve2Uretim_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 67,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kesimhane_12_Debi,
                                DebiSayac = (decimal?)DM_Kesimhane_12_Total,
                            });

                            var DM_Kesimhane_3_Total = suIslemeNodeRedModel.DM_Kesimhane3_Total;
                            var DM_Kesimhane_3_Debi = suIslemeNodeRedModel.DM_Kesimhane3_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 68,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kesimhane_3_Debi,
                                DebiSayac = (decimal?)DM_Kesimhane_3_Total,
                            });

                            var DM_Yumusatma_Besleme_Total = suIslemeNodeRedModel.DM_YumusatmaBesleme_Total;
                            var DM_Yumusatma_Besleme_Debi = suIslemeNodeRedModel.DM_YumusatmaBesleme_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 69,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yumusatma_Besleme_Debi,
                                DebiSayac = (decimal?)DM_Yumusatma_Besleme_Total,
                            });

                            var DM_Yumusak_Tank_1_Total = suIslemeNodeRedModel.DM_YumusatmaTank1_Total;
                            var DM_Yumusak_Tank_1_Debi = suIslemeNodeRedModel.DM_YumusatmaTank1_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 70,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yumusak_Tank_1_Debi,
                                DebiSayac = (decimal?)DM_Yumusak_Tank_1_Total,
                            });

                            var DM_Yumusak_Tank_2_Total = suIslemeNodeRedModel.DM_YumusatmaTank2_Total;
                            var DM_Yumusak_Tank_2_Debi = suIslemeNodeRedModel.DM_YumusatmaTank2_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 71,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yumusak_Tank_2_Debi,
                                DebiSayac = (decimal?)DM_Yumusak_Tank_2_Total,
                            });

                            var DM_Yumusak_Tank_3_Total = suIslemeNodeRedModel.DM_YumusatmaTank3_Total;
                            var DM_Yumusak_Tank_3_Debi = suIslemeNodeRedModel.DM_YumusatmaTank3_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 72,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yumusak_Tank_3_Debi,
                                DebiSayac = (decimal?)DM_Yumusak_Tank_3_Total,
                            });

                            var DM_Yumusak_Tank_4_Total = suIslemeNodeRedModel.DM_YumusatmaTank4_Total;
                            var DM_Yumusak_Tank_4_Debi = suIslemeNodeRedModel.DM_YumusatmaTank4_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 73,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yumusak_Tank_4_Debi,
                                DebiSayac = (decimal?)DM_Yumusak_Tank_4_Total,
                            });

                            var DM_Yumusak_Atik_Total = suIslemeNodeRedModel.DM_YumusakAtik_Total;
                            var DM_Yumusak_Atik_Debi = suIslemeNodeRedModel.DM_YumusakAtik_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 74,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yumusak_Atik_Debi,
                                DebiSayac = (decimal?)DM_Yumusak_Atik_Total,
                            });

                            var DM_Yumusak_Hatti_Total = suIslemeNodeRedModel.DM_YumusakHatti_Total;
                            var DM_Yumusak_Hatti_Debi = suIslemeNodeRedModel.DM_YumusakHatti_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 75,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yumusak_Hatti_Debi,
                                DebiSayac = (decimal?)DM_Yumusak_Hatti_Total,
                            });

                            var DM_Rendering_Total = suIslemeNodeRedModel.DM_Rendering_Total;
                            var DM_Rendering_Debi = suIslemeNodeRedModel.DM_Rendering_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 76,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Rendering_Debi,
                                DebiSayac = (decimal?)DM_Rendering_Total,
                            });

                            var DM_Yumusak_Su_Cikisi_Total = suIslemeNodeRedModel.DM_YumusakSuCikisi_Total;
                            var DM_Yumusak_Su_Cikisi_Debi = suIslemeNodeRedModel.DM_YumusakSuCikisi_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 77,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yumusak_Su_Cikisi_Debi,
                                DebiSayac = (decimal?)DM_Yumusak_Su_Cikisi_Total,
                            });

                            var Tank_SuSeviye = suIslemeNodeRedModel.Yangin_Tanki_Seviye;
                            var Tank_SuSeviye_Yuzdesi = suIslemeNodeRedModel.Yangin_Tanki_Seviye_Yuzde;
                            var Tank_SuHacim = suIslemeNodeRedModel.Yangin_Tanki_Hacim;
                            var Tank_SuHacim_Yuzdesi = suIslemeNodeRedModel.Yangin_Tanki_Hacim_Yuzde;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 204,
                                KayitZamani = now,
                                SeviyeMetre = (decimal?)Tank_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_SuHacim_Yuzdesi,
                            });
                        }
                    }
                }
            }
            return suKaynakLogs;
        }
        private async Task<IEnumerable<SuKaynakLogYeni>> GetSuKaynakLogYeniDataForBulkInsertIleriIslemAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 64;
            var now = DateTime.Now;
            List<SuKaynakLogYeni> suKaynakLogs = new List<SuKaynakLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model != null)
                {
                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            IleriIslemNodeRedModel ileriIslemNodeRedModel = JsonSerializer.Deserialize<IleriIslemNodeRedModel>(model.Veriler);
                            var Tank_1_Sens_Basinc = ileriIslemNodeRedModel.Tank_1_SensBasinc;
                            var Tank_1_SuSeviye = ileriIslemNodeRedModel.Tank_1_SuSeviye;
                            var Tank_1_SuSeviye_Yuzdesi = ileriIslemNodeRedModel.Tank_1_SuSeviye_Yuzde;
                            var Tank_1_SuHacim = ileriIslemNodeRedModel.Tank_1_SuHacim;
                            var Tank_1_SuHacim_Yuzdesi = ileriIslemNodeRedModel.Tank_1_SuHacim_Yuzde;
                            var Tank_1_T_TankHacim = ileriIslemNodeRedModel.Tank_1_T_TankHacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 116,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_1_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_1_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_1_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_1_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_1_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_1_T_TankHacim
                            });

                            var Tank_2_Sens_Basinc = ileriIslemNodeRedModel.Tank_2_SensBasinc;
                            var Tank_2_SuSeviye = ileriIslemNodeRedModel.Tank_2_SuSeviye;
                            var Tank_2_SuSeviye_Yuzdesi = ileriIslemNodeRedModel.Tank_2_SuSeviye_Yuzde;
                            var Tank_2_SuHacim = ileriIslemNodeRedModel.Tank_2_SuHacim;
                            var Tank_2_SuHacim_Yuzdesi = ileriIslemNodeRedModel.Tank_2_SuHacim_Yuzde;
                            var Tank_2_T_TankHacim = ileriIslemNodeRedModel.Tank_2_T_TankHacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 117,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_2_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_2_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_2_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_2_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_2_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_2_T_TankHacim
                            });

                            var Tank_3_Sens_Basinc = ileriIslemNodeRedModel.Tank_3_SensBasinc;
                            var Tank_3_SuSeviye = ileriIslemNodeRedModel.Tank_3_SuSeviye;
                            var Tank_3_SuSeviye_Yuzdesi = ileriIslemNodeRedModel.Tank_3_SuSeviye_Yuzde;
                            var Tank_3_SuHacim = ileriIslemNodeRedModel.Tank_3_SuHacim;
                            var Tank_3_SuHacim_Yuzdesi = ileriIslemNodeRedModel.Tank_3_SuHacim_Yuzde;
                            var Tank_3_T_TankHacim = ileriIslemNodeRedModel.Tank_3_T_TankHacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 118,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_3_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_3_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_3_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_3_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_3_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_3_T_TankHacim
                            });

                            var DM_Yumusatma_Giris_Total = ileriIslemNodeRedModel.DM_YumusatmaGiris_Total;
                            var DM_Yumusatma_Giris_Debi = ileriIslemNodeRedModel.DM_YumusatmaGiris_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 81,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yumusatma_Giris_Debi,
                                DebiSayac = (decimal?)DM_Yumusatma_Giris_Total,
                            });

                            var DM_Osmoz_Giris_Total = ileriIslemNodeRedModel.DM_OsmozGiris_Total;
                            var DM_Osmoz_Giris_Debi = ileriIslemNodeRedModel.DM_OsmozGiris_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 82,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Osmoz_Giris_Debi,
                                DebiSayac = (decimal?)DM_Osmoz_Giris_Total,
                            });

                            var DM_Kazan_Dairesi_Total = ileriIslemNodeRedModel.DM_KazanDairesi_Total;
                            var DM_Kazan_Dairesi_Debi = ileriIslemNodeRedModel.DM_KazanDairesi_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 83,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kazan_Dairesi_Debi,
                                DebiSayac = (decimal?)DM_Kazan_Dairesi_Total,
                            });

                            var DM_Kuyu4_Total = ileriIslemNodeRedModel.DM_YumusatmaAtik_Total;
                            var DM_Kuyu4_Debi = ileriIslemNodeRedModel.DM_YumusatmaAtik_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 84,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Kuyu4_Debi,
                                DebiSayac = (decimal?)DM_Kuyu4_Total,
                            });

                            var DM_Yedek1_Total = ileriIslemNodeRedModel.DM_Yedek1_Total;
                            var DM_Yedek1_Debi = ileriIslemNodeRedModel.DM_Yedek1_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 85,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yedek1_Debi,
                                DebiSayac = (decimal?)DM_Yedek1_Total,
                            });

                            //var DM_Yedek2_Total = ileriIslemNodeRedModel.DM_Yedek2_Total;
                            //var DM_Yedek2_Debi = ileriIslemNodeRedModel.DM_Yedek2_Debi;
                            //suKaynakLogs.Add(new SuKaynakLogYeni()
                            //{
                            //    SayacId = 86,
                            //    KayitZamani = now,
                            //    Debi = (decimal?)DM_Yedek2_Debi,
                            //    DebiSayac = (decimal?)DM_Yedek2_Total,
                            //});
                        }
                    }
                }
            }
            return suKaynakLogs;
        }
        private async Task<IEnumerable<SuKaynakLogYeni>> GetSuKaynakLogYeniDataForBulkInsertDogalgazliKazanAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 66;
            var now = DateTime.Now;
            List<SuKaynakLogYeni> suKaynakLogs = new List<SuKaynakLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model != null)
                {
                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            DogalgazliKazanNodeRedModel dogalgazliKazanNodeRedModel = JsonSerializer.Deserialize<DogalgazliKazanNodeRedModel>(model.Veriler);
                            var Tank_Sens_Basinc = dogalgazliKazanNodeRedModel.Tank_1_SensBasinc;
                            var Tank_SuSeviye = dogalgazliKazanNodeRedModel.Tank_1_SuSeviye;
                            var Tank_SuSeviye_Yuzdesi = dogalgazliKazanNodeRedModel.Tank_1_SuSeviye_Yuzde;
                            var Tank_SuHacim = dogalgazliKazanNodeRedModel.Tank_1_SuHacim;
                            var Tank_SuHacim_Yuzdesi = dogalgazliKazanNodeRedModel.Tank_1_SuHacim_Yuzde;
                            //var Tank_T_TankHacim = dogalgazliKazanNodeRedModel.Tank_1_T_TankHacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 141,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)100
                            });

                            var Reject_Stok_Tanki_SensBasinc = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SensBasinc;
                            var Reject_Stok_Tanki_SuSeviye = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SuSeviye;
                            var Reject_Stok_Tanki_SuSeviye_Yuzde = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SuSeviye_Yuzde;
                            var Reject_Stok_Tanki_SuHacim = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SuHacim;
                            var Reject_Stok_Tanki_SuHacim_Yuzde = dogalgazliKazanNodeRedModel.Reject_Stok_Tanki_SuHacim_Yuzde;
                            //var Tank_T_TankHacim = dogalgazliKazanNodeRedModel.Tank_1_T_TankHacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 207,
                                KayitZamani = now,
                                Basinc = (decimal?)Reject_Stok_Tanki_SensBasinc,
                                SeviyeMetre = (decimal?)Reject_Stok_Tanki_SuSeviye,
                                SeviyeYuzde = (decimal?)Reject_Stok_Tanki_SuSeviye_Yuzde,
                                SuHacim = (decimal?)Reject_Stok_Tanki_SuHacim,
                                SuHacimYuzde = (decimal?)Reject_Stok_Tanki_SuHacim_Yuzde,
                                TankHacim = (decimal?)10
                            });

                            var Proses_Suyu_Depolama_Tanki_SensBasinc = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SensBasinc;
                            var Proses_Suyu_Depolama_Tanki_SuSeviye = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SuSeviye;
                            var Proses_Suyu_Depolama_Tanki_SuSeviye_Yuzde = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SuSeviye_Yuzde;
                            var Proses_Suyu_Depolama_Tanki_SuHacim = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SuHacim;
                            var Proses_Suyu_Depolama_Tanki_SuHacim_Yuzde = dogalgazliKazanNodeRedModel.Proses_Suyu_Depolama_Tanki_SuHacim_Yuzde;
                            //var Tank_T_TankHacim = dogalgazliKazanNodeRedModel.Tank_1_T_TankHacim;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 208,
                                KayitZamani = now,
                                Basinc = (decimal?)Proses_Suyu_Depolama_Tanki_SensBasinc,
                                SeviyeMetre = (decimal?)Proses_Suyu_Depolama_Tanki_SuSeviye,
                                SeviyeYuzde = (decimal?)Proses_Suyu_Depolama_Tanki_SuSeviye_Yuzde,
                                SuHacim = (decimal?)Proses_Suyu_Depolama_Tanki_SuHacim,
                                SuHacimYuzde = (decimal?)Proses_Suyu_Depolama_Tanki_SuHacim_Yuzde,
                                TankHacim = (decimal?)100
                            });

                            var DM_R_O_Atik_Su_Total = dogalgazliKazanNodeRedModel.DM_ROAtikSu_Total;
                            var DM_R_O_Atik_Su_Debi = dogalgazliKazanNodeRedModel.DM_ROAtikSu_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 87,
                                KayitZamani = now,
                                Debi = (decimal?)DM_R_O_Atik_Su_Debi,
                                DebiSayac = (decimal?)DM_R_O_Atik_Su_Total,
                            });

                            var DM_R_O_Kum_Filtresi_Total = dogalgazliKazanNodeRedModel.DM_ROKumFiltresi_Total;
                            var DM_R_O_Kum_Filtresi_Debi = dogalgazliKazanNodeRedModel.DM_ROKumFiltresi_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 88,
                                KayitZamani = now,
                                Debi = (decimal?)DM_R_O_Kum_Filtresi_Debi,
                                DebiSayac = (decimal?)DM_R_O_Kum_Filtresi_Total,
                            });

                            var DM_Osmoz_Su_Tanki_Girisi_Total = dogalgazliKazanNodeRedModel.DM_OsmozSuTakiGirisi_Total;
                            var DM_Osmoz_Su_Tanki_Girisi_Debi = dogalgazliKazanNodeRedModel.DM_OsmozSuTakiGirisi_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 89,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Osmoz_Su_Tanki_Girisi_Debi,
                                DebiSayac = (decimal?)DM_Osmoz_Su_Tanki_Girisi_Total,
                            });

                            var DM_Osmoz_Uretim_Hatti_Total = dogalgazliKazanNodeRedModel.DM_OsmozUretimHatti_Total;
                            var DM_Osmoz_Uretim_Hatti_Debi = dogalgazliKazanNodeRedModel.DM_OsmozUretimHatti_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 90,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Osmoz_Uretim_Hatti_Debi,
                                DebiSayac = (decimal?)DM_Osmoz_Uretim_Hatti_Total,
                            });

                            var DM_Yedek1_Total = dogalgazliKazanNodeRedModel.Uretim_Atik_Sert_Su_Sayaci_Total;
                            var DM_Yedek1_Debi = dogalgazliKazanNodeRedModel.Uretim_Atik_Sert_Su_Sayaci_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 91,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yedek1_Debi,
                                DebiSayac = (decimal?)DM_Yedek1_Total,
                            });

                            var DM_Yedek2_Total = dogalgazliKazanNodeRedModel.DM_Yedek2_Total;
                            var DM_Yedek2_Debi = dogalgazliKazanNodeRedModel.DM_Yedek2_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 92,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yedek2_Debi,
                                DebiSayac = (decimal?)DM_Yedek2_Total,
                            });

                            var DM_Osmoz_Kazan_Dairesi_Total = dogalgazliKazanNodeRedModel.DM_Osmoz_Kazan_Dairesi_Total;
                            var DM_Osmoz_Kazan_Dairesi_Debi = dogalgazliKazanNodeRedModel.DM_Osmoz_Kazan_Dairesi_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 112,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Osmoz_Kazan_Dairesi_Debi,
                                DebiSayac = (decimal?)DM_Osmoz_Kazan_Dairesi_Total,
                            });

                            var DM_Osmoz_Makine_Dairesi_Total = dogalgazliKazanNodeRedModel.DM_Osmoz_Makine_Dairesi_Total;
                            var DM_Osmoz_Makine_Dairesi_Debi = dogalgazliKazanNodeRedModel.DM_Osmoz_Makine_Dairesi_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 113,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Osmoz_Makine_Dairesi_Debi,
                                DebiSayac = (decimal?)DM_Osmoz_Makine_Dairesi_Total,
                            });
                        }
                    }
                }
            }
            return suKaynakLogs;
        }
        private async Task<IEnumerable<SuKaynakLogYeni>> GetSuKaynakLogYeniDataForBulkInsertPastorizeAsync(CancellationToken stoppingToken)
        {
            var lokasyonId = 65;
            var now = DateTime.Now;
            List<SuKaynakLogYeni> suKaynakLogs = new List<SuKaynakLogYeni>();
            SuIzlemeNodeRed? model;
            using (var scope = _serviceProvider.CreateScope())
            {
                var suHatScadaDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                model = await suHatScadaDbContext.SuIzlemeNodeRed.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                if (model != null)
                {
                    if (model.Durum == 1)
                    {
                        if (model.Veriler != null)
                        {
                            PastorizeNodeRedModel pastorizeNodeRedModel = JsonSerializer.Deserialize<PastorizeNodeRedModel>(model.Veriler);
                            var Tank_1_HamSu_Sens_Basinc = pastorizeNodeRedModel.Tank_HamSu_SensBasinc;
                            var Tank_1_HamSu_SuSeviye = pastorizeNodeRedModel.Tank_HamSu_SuSeviye;
                            var Tank_1_HamSu_SuSeviye_Yuzdesi = pastorizeNodeRedModel.Tank_HamSu_SuSeviye_Yuzde;
                            var Tank_1_HamSu_SuHacim = pastorizeNodeRedModel.Tank_HamSu_SuHacim;
                            var Tank_1_HamSu_SuHacim_Yuzdesi = pastorizeNodeRedModel.Tank_HamSu_SuHacim_Yuzde;
                            var Tank_1_HamSu_T_TankHacim = 185;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 153,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_1_HamSu_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_1_HamSu_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_1_HamSu_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_1_HamSu_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_1_HamSu_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_1_HamSu_T_TankHacim
                            });

                            var Tank_2_YumusakSu_Sens_Basinc = pastorizeNodeRedModel.Tank_YumusakSu_SensBasinc;
                            var Tank_2_YumusakSu_SuSeviye = pastorizeNodeRedModel.Tank_YumusakSu_SuSeviye;
                            var Tank_2_YumusakSu_SuSeviye_Yuzdesi = pastorizeNodeRedModel.Tank_YumusakSu_SuSeviye_Yuzde;
                            var Tank_2_YumusakSu_SuHacim = pastorizeNodeRedModel.Tank_YumusakSu_SuHacim;
                            var Tank_2_YumusakSu_SuHacim_Yuzdesi = pastorizeNodeRedModel.Tank_YumusakSu_SuHacim_Yuzde;
                            var Tank_2_YumusakSu_T_TankHacim = 100;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 154,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_2_YumusakSu_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_2_YumusakSu_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_2_YumusakSu_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_2_YumusakSu_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_2_YumusakSu_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_2_YumusakSu_T_TankHacim
                            });

                            var Tank_3_OsmozSu_Sens_Basinc = pastorizeNodeRedModel.Tank_OsmozSu_SensBasinc;
                            var Tank_3_OsmozSu_SuSeviye = pastorizeNodeRedModel.Tank_OsmozSu_SuSeviye;
                            var Tank_3_OsmozSu_SuSeviye_Yuzdesi = pastorizeNodeRedModel.Tank_OsmozSu_SuSeviye_Yuzde;
                            var Tank_3_OsmozSu_SuHacim = pastorizeNodeRedModel.Tank_OsmozSu_SuHacim;
                            var Tank_3_OsmozSu_SuHacim_Yuzdesi = pastorizeNodeRedModel.Tank_OsmozSu_SuHacim_Yuzde;
                            var Tank_3_OsmozSu_T_TankHacim = 100;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 155,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_3_OsmozSu_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_3_OsmozSu_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_3_OsmozSu_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_3_OsmozSu_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_3_OsmozSu_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_3_OsmozSu_T_TankHacim
                            });

                            var Tank_4_SicakSu_Sens_Basinc = pastorizeNodeRedModel.Tank_SicakSu_SensBasinc;
                            var Tank_4_SicakSu_SuSeviye = pastorizeNodeRedModel.Tank_SicakSu_SuSeviye;
                            var Tank_4_SicakSu_SuSeviye_Yuzdesi = pastorizeNodeRedModel.Tank_SicakSu_SuSeviye_Yuzde;
                            var Tank_4_SicakSu_SuHacim = pastorizeNodeRedModel.Tank_SicakSu_SuHacim;
                            var Tank_4_SicakSu_SuHacim_Yuzdesi = pastorizeNodeRedModel.Tank_SicakSu_SuHacim_Yuzde;
                            var Tank_4_SicakSu_T_TankHacim = 50;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 156,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_4_SicakSu_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_4_SicakSu_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_4_SicakSu_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_4_SicakSu_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_4_SicakSu_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_4_SicakSu_T_TankHacim
                            });

                            var Tank_5_Yangin_Sens_Basinc = pastorizeNodeRedModel.Tank_Yangin_SensBasinc;
                            var Tank_5_Yangin_SuSeviye = pastorizeNodeRedModel.Tank_Yangin_SuSeviye;
                            var Tank_5_Yangin_SuSeviye_Yuzdesi = pastorizeNodeRedModel.Tank_Yangin_SuSeviye_Yuzde;
                            var Tank_5_Yangin_SuHacim = pastorizeNodeRedModel.Tank_Yangin_SuHacim;
                            var Tank_5_Yangin_SuHacim_Yuzdesi = pastorizeNodeRedModel.Tank_Yangin_SuHacim_Yuzde;
                            var Tank_5_Yangin_T_TankHacim = 200;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SuKaynakId = 157,
                                KayitZamani = now,
                                Basinc = (decimal?)Tank_5_Yangin_Sens_Basinc,
                                SeviyeMetre = (decimal?)Tank_5_Yangin_SuSeviye,
                                SeviyeYuzde = (decimal?)Tank_5_Yangin_SuSeviye_Yuzdesi,
                                SuHacim = (decimal?)Tank_5_Yangin_SuHacim,
                                SuHacimYuzde = (decimal?)Tank_5_Yangin_SuHacim_Yuzdesi,
                                TankHacim = (decimal?)Tank_5_Yangin_T_TankHacim
                            });



                            var DM_Ham_Su_Hatti_Total = pastorizeNodeRedModel.DM_HamSuHatti_Total;
                            var DM_Ham_Su_Hatti_Debi = pastorizeNodeRedModel.DM_HamSuHatti_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 98,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Ham_Su_Hatti_Debi,
                                DebiSayac = (decimal?)DM_Ham_Su_Hatti_Total,
                            });

                            var DM_Yumusak_Atik_Total = pastorizeNodeRedModel.DM_YumusakAtik_Total;
                            var DM_Yumusak_Atik_Debi = pastorizeNodeRedModel.DM_YumusakAtik_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 99,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yumusak_Atik_Debi,
                                DebiSayac = (decimal?)DM_Yumusak_Atik_Total,
                            });

                            var DM_Yumusak_Su_Hatti_Total = pastorizeNodeRedModel.DM_YumusakSuHatti_Total;
                            var DM_Yumusak_Su_Hatti_Debi = pastorizeNodeRedModel.DM_YumusakSuHatti_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 100,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Yumusak_Su_Hatti_Debi,
                                DebiSayac = (decimal?)DM_Yumusak_Su_Hatti_Total,
                            });

                            var DM_Uretim1_Total = pastorizeNodeRedModel.DM_Uretim1_Total;
                            var DM_Uretim1_Debi = pastorizeNodeRedModel.DM_Uretim1_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 101,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Uretim1_Debi,
                                DebiSayac = (decimal?)DM_Uretim1_Total,
                            });

                            var DM_Uretim2_Total = pastorizeNodeRedModel.DM_Uretim2_Total;
                            var DM_Uretim2_Debi = pastorizeNodeRedModel.DM_Uretim2_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 102,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Uretim2_Debi,
                                DebiSayac = (decimal?)DM_Uretim2_Total,
                            });

                            var DM_Arac_Yikama_Total = pastorizeNodeRedModel.DM_AracYikama_Total;
                            var DM_Arac_Yikama_Debi = pastorizeNodeRedModel.DM_AracYikama_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 103,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Arac_Yikama_Debi,
                                DebiSayac = (decimal?)DM_Arac_Yikama_Total,
                            });

                            var DM_Lavabolar_Total = pastorizeNodeRedModel.DM_Lavabolar_Total;
                            var DM_Lavabolar_Debi = pastorizeNodeRedModel.DM_Lavabolar_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 104,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Lavabolar_Debi,
                                DebiSayac = (decimal?)DM_Lavabolar_Total,
                            });

                            var DM_Osmoz_Giris_Hatti_Total = pastorizeNodeRedModel.DM_OsmozGirisHatti_Total;
                            var DM_Osmoz_Giris_Hatti_Debi = pastorizeNodeRedModel.DM_OsmozGirisHatti_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 105,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Osmoz_Giris_Hatti_Debi,
                                DebiSayac = (decimal?)DM_Osmoz_Giris_Hatti_Total,
                            });

                            var DM_RO_Atik_Su_Total = pastorizeNodeRedModel.DM_ROAtikSu_Total;
                            var DM_RO_Atik_Su_Debi = pastorizeNodeRedModel.DM_ROAtikSu_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 106,
                                KayitZamani = now,
                                Debi = (decimal?)DM_RO_Atik_Su_Debi,
                                DebiSayac = (decimal?)DM_RO_Atik_Su_Total,
                            });

                            var DM_Membran_Atik_Su_Total = pastorizeNodeRedModel.DM_MembranAtikSu_Total;
                            var DM_Membran_Atik_Su_Debi = pastorizeNodeRedModel.DM_MembranAtikSu_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 107,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Membran_Atik_Su_Debi,
                                DebiSayac = (decimal?)DM_Membran_Atik_Su_Total,
                            });

                            var DM_Peyzaj_Total = pastorizeNodeRedModel.DM_Peyzaj_Total;
                            var DM_Peyzaj_Debi = pastorizeNodeRedModel.DM_Peyzaj_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 108,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Peyzaj_Debi,
                                DebiSayac = (decimal?)DM_Peyzaj_Total,
                            });

                            var DM_Rendering_Total = pastorizeNodeRedModel.DM_Rendering_Total;
                            var DM_Rendering_Debi = pastorizeNodeRedModel.DM_Rendering_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 109,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Rendering_Debi,
                                DebiSayac = (decimal?)DM_Rendering_Total,
                            });

                            var DM_Ring_Hatti_Donus_Total = pastorizeNodeRedModel.DM_RingHattiDonus_Total;
                            var DM_Ring_Hatti_Donus_Debi = pastorizeNodeRedModel.DM_RingHattiDonus_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 110,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Ring_Hatti_Donus_Debi,
                                DebiSayac = (decimal?)DM_Ring_Hatti_Donus_Total,
                            });

                            var DM_Sicak_Su_Uretim_Total = pastorizeNodeRedModel.DM_SicakSuUretim_Total;
                            var DM_Sicak_Su_Uretim_Debi = pastorizeNodeRedModel.DM_SicakSuUretim_Debi;
                            suKaynakLogs.Add(new SuKaynakLogYeni()
                            {
                                SayacId = 111,
                                KayitZamani = now,
                                Debi = (decimal?)DM_Sicak_Su_Uretim_Debi,
                                DebiSayac = (decimal?)DM_Sicak_Su_Uretim_Total,
                            });
                        }
                    }
                }
            }
            return suKaynakLogs;
        }


        #endregion SuKaynakLogData

        public async Task BulkInsertSuKaynakLogYeniDataAsync(SqlConnection connection, IEnumerable<SuKaynakLogYeni> data)
        {
            try
            {
                var dataTable = new DataTable();
                var accessor = TypeAccessor.Create(typeof(SuKaynakLogYeni)); // FastMember ile eriþim

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

                // Baðlantýyý açýyoruz
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync(); // Baðlantýyý açýyoruz
                }

                // SqlBulkCopy ile veriyi MSSQL'e ekleyelim
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "SuKaynakLogYeni"; // Verinin ekleneceði tablo adý
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
                // Baðlantýyý manuel olarak kapatýyoruz
                if (connection.State != ConnectionState.Closed)
                {
                    await connection.CloseAsync();
                }
            }
        }
        public async Task BulkInsertEtiketLogYeniDataAsync(SqlConnection connection, IEnumerable<EtiketLogYeni> data)
        {
            try
            {
                var dataTable = new DataTable();
                var accessor = TypeAccessor.Create(typeof(EtiketLogYeni)); // FastMember ile eriþim

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

                // Baðlantýyý açýyoruz
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync(); // Baðlantýyý açýyoruz
                }

                // SqlBulkCopy ile veriyi MSSQL'e ekleyelim
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "EtiketLogYeni"; // Verinin ekleneceði tablo adý
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
                // Baðlantýyý manuel olarak kapatýyoruz
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
                await UpdateServisDurumAsync("SuIzlemeLogNodeRedWorker", 3, "Servis durduruldu");
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                await UpdateServisDurumAsync("SuIzlemeLogNodeRedWorker", 0, ex.Message);
            }
        }
    }
}