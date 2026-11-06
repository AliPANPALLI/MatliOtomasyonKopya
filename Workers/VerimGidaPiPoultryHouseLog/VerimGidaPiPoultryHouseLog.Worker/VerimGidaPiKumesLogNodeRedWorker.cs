using FastMember;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using static MatliOtomasyon.Shared.Application.Constants.ApplicationConstants;
using VerimGidaPiPoultryHouseLog.Application.Models;
using VerimGidaPiPoultryHouseLog.Infrastructure.Caching;

namespace VerimGidaPiPoultryHouseLog.Worker
{
    public class VerimGidaPiKumesLogNodeRedWorker : BackgroundService
    {
        private readonly ILogger<VerimGidaPiKumesLogNodeRedWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly AlarmCacheService _alarmCacheService;
        private readonly SemaphoreSlim _kumesIslemSiniri = new(4, 4);

        private List<Kumes> _kumesListesi = new();
        private readonly Dictionary<int, DateTime> _kumesHayvanLogAylikKayitAtildi = new();
        //private const int LokasyonId = 47;
        private static readonly int[] LokasyonIdleri = { 47, 48 };
        private const string ServisAdi = "VerimGidaPiKumesLogNodeRedWorker";
        private static readonly TimeSpan PlcVeriZamanAsimi = TimeSpan.FromMinutes(3);


        public VerimGidaPiKumesLogNodeRedWorker(ILogger<VerimGidaPiKumesLogNodeRedWorker> logger, IServiceProvider serviceProvider, AlarmCacheService alarmCacheService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _alarmCacheService = alarmCacheService;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var lokasyonlarMetin = string.Join(", ", LokasyonIdleri);

                _logger.LogInformation(
                    "Verim G�da Ticari Yumurta Node-RED worker ba�lat�l�yor. LokasyonIdleri: {LokasyonIdleri}",
                    lokasyonlarMetin);

                await UpdateServisDurumAsync(
                    ServisAdi,
                    2,
                    $"Servis ba�lat�l�yor. Lokasyonlar: {lokasyonlarMetin}");

                /*
                 * �ki lokasyonun k�mesleri paralel olarak al�n�r.
                 */
                var kumesListeGorevleri = LokasyonIdleri
                    .Select(lokasyonId =>
                        _alarmCacheService.GetKumeslerAsync(lokasyonId))
                    .ToArray();

                var lokasyonKumesListeleri = await Task.WhenAll(kumesListeGorevleri);

                /*
                 * Lokasyonlardan gelen k�mes listeleri tek listede birle�tirilir.
                 * Ayn� k�mes yanl��l�kla iki kez d�nerse Id �zerinden tekille�tirilir.
                 */
                _kumesListesi = lokasyonKumesListeleri
                    .SelectMany(kumesler => kumesler)
                    .GroupBy(kumes => kumes.Id)
                    .Select(grup => grup.First())
                    .ToList();

                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Verim G�da Ticari Yumurta worker ba�lat�lamad�.");

                await UpdateServisDurumAsync(ServisAdi, 0, ex.Message);

                throw;
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    /*
                     * Her k�mes i�in g�nde bir kez, i�inde bulunulan ay�n
                     * "�l� hayvan say�s�" girilmemi� g�nlerine bo� (Silindi=true)
                     * kay�t at�l�r; RahmiyeTesisWorker'daki AddAylikKumesHayvanLog
                     * ile ayn� mant�k.
                     */
                    var bugun = DateTime.Today;
                    foreach (var kumes in _kumesListesi)
                    {
                        if (!_kumesHayvanLogAylikKayitAtildi.TryGetValue(kumes.Id, out var sonKayitTarihi) || sonKayitTarihi < bugun)
                        {
                            await AddAylikKumesHayvanLog(kumes.Id);
                            _kumesHayvanLogAylikKayitAtildi[kumes.Id] = bugun;
                        }
                    }

                    /*
                     * K�mesleri tek tek beklemek yerine en fazla 4 k�mesi ayn� anda
                     * haz�rl�yoruz. SQL'e ise her k�mes i�in ayr� ayr� de�il,
                     * b�t�n k�meslerin kay�tlar�n� iki toplu i�lemle yaz�yoruz.
                     */
                    var hazirlamaGorevleri = _kumesListesi
                        .Select(kumes => KumesVerileriniHazirlaAsync(kumes.Id, stoppingToken))
                        .ToArray();

                    var hazirlananVeriler = await Task.WhenAll(hazirlamaGorevleri);

                    var tumEtiketLoglari = hazirlananVeriler.SelectMany(x => x.EtiketLoglari).ToList();

                    var tumKumesLoglari = hazirlananVeriler.SelectMany(x => x.KumesLoglari).ToList();

                    /*
                     * Bir tur i�in yaln�zca bir scoped SqlConnection al�yoruz.
                     * Bulk insert metotlar� ba�lant�y� gerekti�inde a��p kapat�r.
                     */
                    using var sqlScope = _serviceProvider.CreateScope();
                    var sqlConnection = sqlScope.ServiceProvider.GetRequiredService<SqlConnection>();

                    if (tumEtiketLoglari.Count > 0)
                    {
                        await BulkInsertEtiketLogYeniDataAsync(sqlConnection, tumEtiketLoglari, stoppingToken);
                    }

                    if (tumKumesLoglari.Count > 0)
                    {
                        await BulkInsertKumesLogYeniDataAsync(sqlConnection, tumKumesLoglari, stoppingToken);
                    }

                    await UpdateServisDurumAsync(
                        ServisAdi,
                        1,
                        "Servis �al���yor",
                        true);

                    await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Verim G�da Ticari Yumurta worker d�ng�s�nde hata olu�tu.");

                    await UpdateServisDurumAsync(ServisAdi, 0, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }

        private async Task<(List<EtiketLogYeni> EtiketLoglari,
            List<KumesLogYeni> KumesLoglari)> KumesVerileriniHazirlaAsync(int kumesId, CancellationToken cancellationToken)
        {
            await _kumesIslemSiniri.WaitAsync(cancellationToken);

            try
            {
                /*
                 * Bu iki i�lem kendi DbContext scope'lar�n� olu�turdu�u i�in
                 * ayn� k�mes i�in paralel �al��t�r�labilir.
                 */
                var etiketGorevi = GetEtiketLogYeniDataForBulkInsert(kumesId, cancellationToken);

                var kumesLogGorevi = GetKumesLogYeniDataForBulkInsertAsync(kumesId, cancellationToken);

                await Task.WhenAll(etiketGorevi, kumesLogGorevi);

                return ((await etiketGorevi).ToList(), (await kumesLogGorevi).ToList());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "K�mes {KumesId} verileri haz�rlan�rken hata olu�tu.", kumesId);

                return (new List<EtiketLogYeni>(), new List<KumesLogYeni>());
            }
            finally
            {
                _kumesIslemSiniri.Release();
            }
        }

        private async Task BulkInsertKumesLogYeniDataAsync(SqlConnection connection, IEnumerable<KumesLogYeni> data, CancellationToken cancellationToken)
        {
            var dataList = data.ToList();
            if (dataList.Count == 0)
                return;

            try
            {
                var dataTable = CreateDataTable(dataList);

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync(cancellationToken);

                using var bulkCopy = new SqlBulkCopy(connection)
                {
                    DestinationTableName = "KumesLogYeni",
                    BulkCopyTimeout = 600
                };

                foreach (DataColumn column in dataTable.Columns)
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

                await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KumesLogYeni bulk insert i�lemi ba�ar�s�z oldu.");
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                    await connection.CloseAsync();
            }
        }

        private async Task BulkInsertEtiketLogYeniDataAsync(SqlConnection connection, IEnumerable<EtiketLogYeni> data, CancellationToken cancellationToken)
        {
            var dataList = data.ToList();
            if (dataList.Count == 0)
                return;

            try
            {
                var dataTable = CreateDataTable(dataList);

                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync(cancellationToken);

                using var bulkCopy = new SqlBulkCopy(connection)
                {
                    DestinationTableName = "EtiketLogYeni",
                    BulkCopyTimeout = 600
                };

                foreach (DataColumn column in dataTable.Columns)
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

                await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EtiketLogYeni bulk insert i�lemi ba�ar�s�z oldu.");
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                    await connection.CloseAsync();
            }
        }

        private static DataTable CreateDataTable<T>(IEnumerable<T> data)
        {
            var dataTable = new DataTable();
            var accessor = TypeAccessor.Create(typeof(T));
            var members = accessor.GetMembers();

            foreach (var member in members)
                dataTable.Columns.Add(member.Name);

            foreach (var item in data)
            {
                var row = dataTable.NewRow();
                foreach (var member in members)
                {
                    row[member.Name] = accessor[item!, member.Name] ?? DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        private async Task<IEnumerable<KumesLogYeni>> GetKumesLogYeniDataForBulkInsertAsync(int kumesId, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

                var nodeRedKaydi = await dbContext.KumesIzlemeNodeRed.AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.HasValue && x.KayitZamani.Value >= now.Date && x.KayitZamani.Value < now.Date.AddDays(1)).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);

                if (!PlcOkuyorMu(nodeRedKaydi, now))
                    return Array.Empty<KumesLogYeni>();

                var model = JsonSerializer.Deserialize<KumesNodeRedModel>(nodeRedKaydi.Veriler);
                if (model == null)
                    return Array.Empty<KumesLogYeni>();

                var icmeSuyuTuketim = model.IcmeSuyu_TuketimBugun / 1000;

                // PLC t�ketim de�eri beklenenden y�ksek gelirse saya� fark�ndan yeniden hesapla.
                if (icmeSuyuTuketim.GetValueOrDefault() > 50)
                {
                    var mevcutSayac = model.IcmeSuyu_Sayac.GetValueOrDefault();
                    var dunSayac = await _alarmCacheService.GetDunIcmeSuyuSayacByKumesId(kumesId, mevcutSayac);

                    icmeSuyuTuketim = mevcutSayac >= dunSayac ? mevcutSayac - dunSayac : mevcutSayac;
                }

                return new[]
                {
                    new KumesLogYeni
                    {
                        KumesId = kumesId,
                        DonemId = 1,
                        KayitZamani = now,
                        SicaklikKumes = model.Sicaklik_Kumes_Value,
                        SicaklikDis = model.Sicaklik_Dis_Value,
                        Sicaklik1 = model.Sicaklik_Sensor_1_Value,
                        Sicaklik2 = model.Sicaklik_Sensor_2_Value,
                        Sicaklik3 = model.Sicaklik_Sensor_3_Value,
                        Sicaklik4 = model.Sicaklik_Sensor_4_Value,
                        Sicaklik5 = model.Sicaklik_Sensor_5_Value,
                        Sicaklik6 = null,
                        Sicaklik7 = 0,
                        Sicaklik8 = 0,
                        NemKumes = model.Nem_Kumes_Value,
                        NemDis = null,
                        Nem1 = null,
                        Nem2 = null,
                        Nem3 = null,
                        Nem4 = null,
                        Nem5 = null,
                        Nem6 = null,
                        Silo1 = model.Yem_Silo_1_Value/1000,
                        Silo2 = model.Yem_Silo_2_Value/1000,
                        Basinc1 = model.Basinc_Sens_Value,
                        Co2_1 =null,
                        YemTuketimBugun = model.Yem_TuketimBugun/1000,
                        SuTuketimBugun = icmeSuyuTuketim
                        // Yumurta alanlar� �zellikle doldurulmamaktad�r.
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "K�mes {KumesId} i�in KumesLogYeni verisi haz�rlanamad�.", kumesId);
                return Array.Empty<KumesLogYeni>();
            }
        }

        private async Task<IEnumerable<EtiketLogYeni>> GetEtiketLogYeniDataForBulkInsert(int kumesId, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;
                var etiketLogs = new List<EtiketLogYeni>();
                KumesIzlemeNodeRed? model;
                var gunlukHayvanSayisi = await _alarmCacheService.GetGunlukCanliHayvanSayisiByKumesId(kumesId);

                // EtiketId 106-143 yumurta bilgilerine ait oldu�u i�in bu worker i�inde �retilmez.
                using (var scope = _serviceProvider.CreateScope())
                {
                    var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    model = await kumesScadaDbContext.KumesIzlemeNodeRed.AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.HasValue && x.KayitZamani.Value >= now.Date && x.KayitZamani.Value < now.Date.AddDays(1)).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);

                    var plcOkuyorMu = PlcOkuyorMu(model, now);

                    if (!plcOkuyorMu)
                    {
                        etiketLogs.Add(new EtiketLogYeni
                        {
                            KumesId = kumesId,
                            EtiketId = 2,
                            KayitZamani = now,
                            Deger = 0
                        });
                        etiketLogs.Add(new EtiketLogYeni
                        {
                            KumesId = kumesId,
                            EtiketId = 3,
                            KayitZamani = now,
                            Deger = 0
                        });

                        return etiketLogs;
                    }
                    if (model == null)
                    {
                        var plcRead = 0;
                        var plcConnected = 0;
                        etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                        etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
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
                                var kumesNodeRedModel = JsonSerializer.Deserialize<KumesNodeRedModel>(model.Veriler);
                                if (kumesNodeRedModel == null)
                                    return etiketLogs;

                                var byteDensity = 0;
                                var pingAverage = 0;
                                var lastPackageLength = 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 71, KayitZamani = DateTime.Now, Deger = (decimal?)byteDensity });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 72, KayitZamani = DateTime.Now, Deger = (decimal?)lastPackageLength });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 73, KayitZamani = DateTime.Now, Deger = (decimal?)pingAverage });

                                if (plcConnected == 0) return etiketLogs;
                                // PLC okuma durumu ayr�ca kontrol edilmek istenirse plcRead kullan�labilir.

                                var sicaklik_Kumes_Ort = kumesNodeRedModel.Sicaklik_Kumes_Value;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 4, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Kumes_Ort });
                                var sicaklik_Dis = kumesNodeRedModel.Sicaklik_Dis_Value;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 5, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Dis });
                                var sicaklik_Sensor1 = kumesNodeRedModel.Sicaklik_Sensor_1_Value;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 6, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor1 });
                                var sicaklik_Sensor2 = kumesNodeRedModel.Sicaklik_Sensor_2_Value;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 7, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor2 });
                                var sicaklik_Sensor3 = kumesNodeRedModel.Sicaklik_Sensor_3_Value;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 8, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor3 });
                                var sicaklik_Sensor4 = kumesNodeRedModel.Sicaklik_Sensor_4_Value;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 9, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor4 });
                                var sicaklik_Sensor5 = kumesNodeRedModel.Sicaklik_Sensor_5_Value;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 10, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor5 });
                                var silo1_value = kumesNodeRedModel.Yem_Silo_1_Value / 1000;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 28, KayitZamani = DateTime.Now, Deger = (decimal?)silo1_value });
                                var silo2_value = kumesNodeRedModel.Yem_Silo_2_Value / 1000;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 31, KayitZamani = DateTime.Now, Deger = (decimal?)silo2_value });
                                var nem_Kumes = kumesNodeRedModel.Nem_Kumes_Value;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 81, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Kumes });
                                var nem_Sensor1 = kumesNodeRedModel.Nem_Sensor_1_Value;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 46, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor1 });

                                var basinc = kumesNodeRedModel.Basinc_Sens_Value;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 42, KayitZamani = DateTime.Now, Deger = (decimal?)basinc });

                                var suSayacTuketim = kumesNodeRedModel.IcmeSuyu_Sayac;
                                var suTuketimDun = kumesNodeRedModel.IcmeSuyu_TuketimDun / 1000;
                                var suTuketimBugun = kumesNodeRedModel.IcmeSuyu_TuketimBugun / 1000;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 75, KayitZamani = DateTime.Now, Deger = (decimal?)suTuketimDun });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 76, KayitZamani = DateTime.Now, Deger = (decimal?)suSayacTuketim });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 16, KayitZamani = DateTime.Now, Deger = (decimal?)suTuketimBugun });
                                var yemTuketim = kumesNodeRedModel.Yem_TuketimBugun / 1000;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 17, KayitZamani = DateTime.Now, Deger = (decimal?)yemTuketim });
                                var aydinlatma_Ac = kumesNodeRedModel.Aydinlatma_Ac == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 77, KayitZamani = DateTime.Now, Deger = (decimal?)aydinlatma_Ac });
                                var fanMode = kumesNodeRedModel.Fan_AktifGrup;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 18, KayitZamani = DateTime.Now, Deger = (decimal?)fanMode });



                                var Buffel_2_Sol_State_Durum = kumesNodeRedModel.Buffel_2_Sol_State_Durum;
                                var Buffel_2_Sol_Cmd_CmdBit = kumesNodeRedModel.Buffel_2_Sol_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 574, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 577, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_Cmd_CmdBit.GetValueOrDefault(0) });

                                var Buffel_2_Sag_State_Durum = kumesNodeRedModel.Buffel_2_Sag_State_Durum;
                                var Buffel_2_Sag_Cmd_CmdBit = kumesNodeRedModel.Buffel_2_Sag_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 583, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 586, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_1_State_Durum = kumesNodeRedModel.PedKapak_On_1_State_Durum;
                                var PedKapak_On_1_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_1_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 592, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 595, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_2_State_Durum = kumesNodeRedModel.PedKapak_On_2_State_Durum;
                                var PedKapak_On_2_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_2_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 601, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 604, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_3_State_Durum = kumesNodeRedModel.PedKapak_On_3_State_Durum;
                                var PedKapak_On_3_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_3_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 610, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 613, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_4_State_Durum = kumesNodeRedModel.PedKapak_On_4_State_Durum;
                                var PedKapak_On_4_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_4_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 619, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 622, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_1_State_Durum = kumesNodeRedModel.PedKapak_Arka_1_State_Durum;
                                var PedKapak_Arka_1_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_1_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 628, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 631, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_2_State_Durum = kumesNodeRedModel.PedKapak_Arka_2_State_Durum;
                                var PedKapak_Arka_2_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_2_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 637, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 640, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_3_State_Durum = kumesNodeRedModel.PedKapak_Arka_3_State_Durum;
                                var PedKapak_Arka_3_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_3_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 646, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 649, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_4_State_Durum = kumesNodeRedModel.PedKapak_Arka_4_State_Durum;
                                var PedKapak_Arka_4_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_4_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 655, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 658, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_Cmd_CmdBit.GetValueOrDefault(0) });


                                var BacaFani_1_Calisiyor = kumesNodeRedModel.BacaFani_1_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 696, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_1_Calisiyor });

                                var BacaFani_2_Calisiyor = kumesNodeRedModel.BacaFani_2_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 701, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_2_Calisiyor });

                                var BacaFani_3_Calisiyor = kumesNodeRedModel.BacaFani_3_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 706, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_3_Calisiyor });

                                var BacaFani_4_Calisiyor = kumesNodeRedModel.BacaFani_4_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 711, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_4_Calisiyor });

                                var BacaFani_5_Calisiyor = kumesNodeRedModel.BacaFani_5_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 716, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_5_Calisiyor });

                                var BacaFani_6_Calisiyor = kumesNodeRedModel.BacaFani_6_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 721, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_6_Calisiyor });

                                var BacaFani_7_Calisiyor = kumesNodeRedModel.BacaFani_7_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 726, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_7_Calisiyor });

                                var BacaFani_8_Calisiyor = kumesNodeRedModel.BacaFani_8_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 731, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_8_Calisiyor });

                                var BacaFani_9_Calisiyor = kumesNodeRedModel.BacaFani_9_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 736, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_9_Calisiyor });

                                var BacaFani_10_Calisiyor = kumesNodeRedModel.BacaFani_10_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 741, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_10_Calisiyor });

                                var BacaFani_11_Calisiyor = kumesNodeRedModel.BacaFani_11_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 746, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_11_Calisiyor });

                                var BacaFani_12_Calisiyor = kumesNodeRedModel.BacaFani_12_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 751, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_12_Calisiyor });

                                var BacaFani_13_Calisiyor = kumesNodeRedModel.BacaFani_13_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 756, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_13_Calisiyor });

                                var BacaFani_14_Calisiyor = kumesNodeRedModel.BacaFani_14_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 761, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_14_Calisiyor });

                                var BacaFani_15_Calisiyor = kumesNodeRedModel.BacaFani_15_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 766, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_15_Calisiyor });

                                var BacaFani_16_Calisiyor = kumesNodeRedModel.BacaFani_16_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 771, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_16_Calisiyor });

                                var BacaFani_17_Calisiyor = kumesNodeRedModel.BacaFani_17_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 776, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_17_Calisiyor });

                                var BacaFani_18_Calisiyor = kumesNodeRedModel.BacaFani_18_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 781, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_18_Calisiyor });

                                var BacaFani_19_Calisiyor = kumesNodeRedModel.BacaFani_19_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 786, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_19_Calisiyor });

                                var BacaFani_20_Calisiyor = kumesNodeRedModel.BacaFani_20_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 791, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_20_Calisiyor });

                                var BacaFani_21_Calisiyor = kumesNodeRedModel.BacaFani_21_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 796, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_21_Calisiyor });

                                var BacaFani_22_Calisiyor = kumesNodeRedModel.BacaFani_22_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 801, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_22_Calisiyor });

                                var BacaFani_23_Calisiyor = kumesNodeRedModel.BacaFani_23_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 806, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_23_Calisiyor });

                                var BacaFani_24_Calisiyor = kumesNodeRedModel.BacaFani_24_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 811, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_24_Calisiyor });

                                var hayvanBasinaSuTuketim = gunlukHayvanSayisi == 0 ? 0 : (kumesNodeRedModel.IcmeSuyu_TuketimBugun * 1000) / gunlukHayvanSayisi;
                                var hayvanBasinaYemTuketim = gunlukHayvanSayisi == 0 ? 0 : (kumesNodeRedModel.Yem_TuketimBugun * 1000) / gunlukHayvanSayisi;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 104, KayitZamani = DateTime.Now, Deger = (decimal?)hayvanBasinaSuTuketim });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 105, KayitZamani = DateTime.Now, Deger = (decimal?)hayvanBasinaYemTuketim });

                                var BacaFani_25_Calisiyor = kumesNodeRedModel.BacaFani_25_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 816, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_25_Calisiyor });

                                var IcmeSuyu_Toplam = kumesNodeRedModel.IcmeSuyu_Toplam;
                                var BacaFani_TermikOk = kumesNodeRedModel.BacaFani_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 820, KayitZamani = DateTime.Now, Deger = (decimal?)IcmeSuyu_Toplam });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 821, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_TermikOk });

                                var PedPompa_1_Komut = kumesNodeRedModel.PedPompa_1_Komut;
                                var PedPompa_1_TermikOk = kumesNodeRedModel.PedPompa_1_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 822, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_1_Komut });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 823, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_1_TermikOk });

                                var PedPompa_2_Komut = kumesNodeRedModel.PedPompa_2_Komut;
                                var PedPompa_2_TermikOk = kumesNodeRedModel.PedPompa_2_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 824, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_2_Komut });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 825, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_2_TermikOk });

                                var PedPompa_3_Komut = kumesNodeRedModel.PedPompa_3_Komut;
                                var PedPompa_3_TermikOk = kumesNodeRedModel.PedPompa_3_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 826, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_3_Komut });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 827, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_3_TermikOk });

                                var PedPompa_4_Komut = kumesNodeRedModel.PedPompa_4_Komut;
                                var PedPompa_4_TermikOk = kumesNodeRedModel.PedPompa_4_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 828, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_4_Komut });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 829, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_4_TermikOk });

                                var PedPompa_5_Komut = kumesNodeRedModel.PedPompa_5_Komut;
                                var PedPompa_5_TermikOk = kumesNodeRedModel.PedPompa_5_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 830, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_5_Komut });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 831, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_5_TermikOk });

                                var PedPompa_6_Komut = kumesNodeRedModel.PedPompa_6_Komut;
                                var PedPompa_6_TermikOk = kumesNodeRedModel.PedPompa_6_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 832, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_6_Komut });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 833, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_6_TermikOk });

                                var PedPompa_7_Komut = kumesNodeRedModel.PedPompa_7_Komut;
                                var PedPompa_7_TermikOk = kumesNodeRedModel.PedPompa_7_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 834, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_7_Komut });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 835, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_7_TermikOk });

                                var PedPompa_8_Komut = kumesNodeRedModel.PedPompa_8_Komut;
                                var PedPompa_8_TermikOk = kumesNodeRedModel.PedPompa_8_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 836, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_8_Komut });
                                etiketLogs.Add(new EtiketLogYeni { KumesId = kumesId, EtiketId = 837, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_8_TermikOk });
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
        private async Task AddAylikKumesHayvanLog(int kumesId)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

                var now = DateTime.Now;
                var ayinIlkGunu = new DateTime(now.Year, now.Month, 1);
                var ayinSonGunu = ayinIlkGunu.AddMonths(1).AddDays(-1);

                var mevcutKayitlar = await kumesScadaDbContext.KumesHayvanLog
                    .Where(x => x.SaglikDurumu == KumesHayvanSaglikDurum.Olu && x.KumesId == kumesId && x.KayitZamani >= ayinIlkGunu && x.KayitZamani <= ayinSonGunu)
                    .Select(x => x.KayitZamani.Value.Date)
                    .ToListAsync();

                for (var gun = ayinIlkGunu; gun <= ayinSonGunu; gun = gun.AddDays(1))
                {
                    if (mevcutKayitlar.Contains(gun.Date))
                        continue;

                    var yeniKayit = new KumesHayvanLog
                    {
                        KumesId = kumesId,
                        DonemId = null,
                        Adet = 0,
                        SaglikDurumu = KumesHayvanSaglikDurum.Olu,
                        GirisZamani = null,
                        KayitZamani = gun,
                        KayitYapanUserId = null,
                        Silindi = true
                    };

                    await kumesScadaDbContext.KumesHayvanLog.AddAsync(yeniKayit);
                }

                await kumesScadaDbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "K�mes {KumesId} i�in ayl�k KumesHayvanLog kayd� olu�turulamad�.", kumesId);
            }
        }

        private static bool PlcOkuyorMu(KumesIzlemeNodeRed? nodeRedKaydi, DateTime kontrolZamani)
        {
            if (nodeRedKaydi?.Durum != 1 || string.IsNullOrWhiteSpace(nodeRedKaydi.Veriler) || !nodeRedKaydi.KayitZamani.HasValue)
            {
                return false;
            }
            if (nodeRedKaydi == null)
            {
                return false;
            }

            return nodeRedKaydi.KayitZamani.Value >= kontrolZamani.Subtract(PlcVeriZamanAsimi);
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UpdateServisDurumAsync(ServisAdi, 3, "Servis durduruldu");
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Verim G�da Ticari Yumurta worker durdurulurken hata olu�tu.");
                await UpdateServisDurumAsync(ServisAdi, 0, ex.Message);
            }
        }

        private async Task UpdateServisDurumAsync(string servisName, byte durum, string mesaj,
            bool basariliMi = false)
        {
            try
            {
                mesaj = mesaj.Length > 500 ? mesaj[..500] : mesaj;

                using var scope = _serviceProvider.CreateScope();
                var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();

                var servis = await sharedDbContext.ServisKontrol
                    .FirstOrDefaultAsync(x => x.ServisName == servisName);

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
                    servis.SonBasariliZaman = DateTime.Now;

                await sharedDbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis durum bilgisi g�ncellenemedi. Servis: {ServisAdi}", servisName);
            }
        }
    }
}
