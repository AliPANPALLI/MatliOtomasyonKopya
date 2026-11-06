using FastMember;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using SharedKernel;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using VerimGidaPiKumesWorker.Core;
using VerimGidaPiKumesWorker.Models;
using static SharedKernel.ApplicationConstants;
using AlarmLog = PoultryHouseScada.Domain.Models.AlarmLog;

namespace VerimGidaPiKumesWorker
{
    public class VerimGidaPiKumesNodeRedWorker : BackgroundService
    {
        private readonly ILogger<VerimGidaPiKumesNodeRedWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly AlarmCacheService _alarmCacheService;
        private readonly SemaphoreSlim _kumesIslemSiniri = new(4, 4);

        private List<Kumes> _kumesListesi = new();
        private readonly Dictionary<int, DateTime> _kumesHayvanLogAylikKayitAtildi = new();
        //private const int LokasyonId = 47;
        private static readonly int[] LokasyonIdleri = { 47, 48 };
        private const string ServisAdi = "VerimGidaPiKumesNodeRedWorker";

        // Alarmlar.AlarmEslemeSozcugu alanındaki PLC okuma alarmının anahtarı.
        // Karşılaştırma normalize edilerek yapılır; PLC_OKUNMUYOR, Plc Okunmuyor
        // ve plc-okunmuyor aynı alarm anahtarı kabul edilir.
        private const string PlcOkunmuyorAlarmEslemeSozcugu = "PLC_OKUNMUYOR";

        private static readonly TimeSpan PlcVeriZamanAsimi = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan AlarmTanimYenilemeSuresi = TimeSpan.FromMinutes(5);

        /*
         * Alarm tanımları alarm türüne özel ayrı sözlüklerde tutulmaz.
         * Anahtar KumesId + AlarmEslemeSozcugu olduğu için aynı kümese ait
         * PLC, sıcaklık, su, yem vb. bütün alarmlar aynı yapıdan yönetilebilir.
         */
        private IReadOnlyDictionary<KumesAlarmTanimAnahtari, KumesAlarmTanimi>
            _alarmTanimlari =
                new Dictionary<KumesAlarmTanimAnahtari, KumesAlarmTanimi>();

        private DateTime _sonAlarmTanimYuklemeZamani = DateTime.MinValue;
        private readonly SemaphoreSlim _alarmTanimYuklemeKilidi = new(1, 1);

        private readonly record struct KumesAlarmTanimAnahtari(
            int KumesId,
            string EslemeAnahtari);

        private sealed record KumesAlarmTanimi(
            int AlarmId,
            string AlarmEslemeSozcugu,
            string Mesaj);

        public VerimGidaPiKumesNodeRedWorker(
            ILogger<VerimGidaPiKumesNodeRedWorker> logger,
            IServiceProvider serviceProvider,
            AlarmCacheService alarmCacheService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _alarmCacheService = alarmCacheService;
        }

        //public override async Task StartAsync(CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Verim Gıda Ticari Yumurta Node-RED worker başlatılıyor. LokasyonId: {LokasyonId}", LokasyonId);

        //        await UpdateServisDurumAsync(ServisAdi, 2, "Servis başlatılıyor");
        //        _kumesListesi = await _alarmCacheService.GetKumeslerAsync(LokasyonId);

        //        _logger.LogInformation("Lokasyon {LokasyonId} için {KumesSayisi} kümes bulundu.", LokasyonId, _kumesListesi.Count);

        //        await base.StartAsync(cancellationToken);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Verim Gıda Ticari Yumurta worker başlatılamadı.");
        //        await UpdateServisDurumAsync(ServisAdi, 0, ex.Message);
        //        throw;
        //    }
        //}
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var lokasyonlarMetin = string.Join(", ", LokasyonIdleri);

                _logger.LogInformation(
                    "Verim Gıda Ticari Yumurta Node-RED worker başlatılıyor. LokasyonIdleri: {LokasyonIdleri}",
                    lokasyonlarMetin);

                await UpdateServisDurumAsync(
                    ServisAdi,
                    2,
                    $"Servis başlatılıyor. Lokasyonlar: {lokasyonlarMetin}");

                /*
                 * İki lokasyonun kümesleri paralel olarak alınır.
                 */
                var kumesListeGorevleri = LokasyonIdleri
                    .Select(lokasyonId =>
                        _alarmCacheService.GetKumeslerAsync(lokasyonId))
                    .ToArray();

                var lokasyonKumesListeleri = await Task.WhenAll(kumesListeGorevleri);

                /*
                 * Lokasyonlardan gelen kümes listeleri tek listede birleştirilir.
                 * Aynı kümes yanlışlıkla iki kez dönerse Id üzerinden tekilleştirilir.
                 */
                _kumesListesi = lokasyonKumesListeleri
                    .SelectMany(kumesler => kumesler)
                    .GroupBy(kumes => kumes.Id)
                    .Select(grup => grup.First())
                    .ToList();

                _logger.LogInformation(
                    "{LokasyonSayisi} lokasyon için toplam {KumesSayisi} kümes bulundu. Lokasyonlar: {LokasyonIdleri}",
                    LokasyonIdleri.Length,
                    _kumesListesi.Count,
                    lokasyonlarMetin);

                await AlarmTanimlariniYukleAsync(cancellationToken);
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Verim Gıda Ticari Yumurta worker başlatılamadı.");

                await UpdateServisDurumAsync(
                    ServisAdi,
                    0,
                    ex.Message);

                throw;
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await AlarmTanimlariniGerekirseYenileAsync(stoppingToken);

                    /*
                     * Kümesleri tek tek beklemek yerine en fazla 4 kümesi aynı anda
                     * hazırlıyoruz. SQL'e ise her kümes için ayrı ayrı değil,
                     * bütün kümeslerin kayıtlarını iki toplu işlemle yazıyoruz.
                     */
                    var hazirlamaGorevleri = _kumesListesi
                        .Select(kumes => KumesVerileriniHazirlaAsync(kumes.Id, stoppingToken))
                        .ToArray();

                    var hazirlananVeriler = await Task.WhenAll(hazirlamaGorevleri);

                    var tumEtiketLoglari = hazirlananVeriler
                        .SelectMany(x => x.EtiketLoglari)
                        .ToList();

                    var tumKumesLoglari = hazirlananVeriler
                        .SelectMany(x => x.KumesLoglari)
                        .ToList();

                    /*
                     * Bir tur için yalnızca bir scoped SqlConnection alıyoruz.
                     * Bulk insert metotları bağlantıyı gerektiğinde açıp kapatır.
                     */
                    using var sqlScope = _serviceProvider.CreateScope();
                    var sqlConnection = sqlScope.ServiceProvider
                        .GetRequiredService<SqlConnection>();

                    if (tumEtiketLoglari.Count > 0)
                    {
                        await BulkUpsertEtiketLogYeniDataAsync(
                            sqlConnection,
                            tumEtiketLoglari,
                            stoppingToken);
                    }

                    if (tumKumesLoglari.Count > 0)
                    {
                        await BulkUpsertKumesLogYeniDataAsync(
                            sqlConnection,
                            tumKumesLoglari,
                            stoppingToken);
                    }

                    await UpdateServisDurumAsync(
                        ServisAdi,
                        1,
                        "Servis çalışıyor",
                        true);

                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Verim Gıda Ticari Yumurta worker döngüsünde hata oluştu.");

                    await UpdateServisDurumAsync(ServisAdi, 0, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }

        private async Task<(
            List<EtiketLog_TempYeni> EtiketLoglari,
            List<KumesLog_TempYeni> KumesLoglari)> KumesVerileriniHazirlaAsync(
                int kumesId,
                CancellationToken cancellationToken)
        {
            await _kumesIslemSiniri.WaitAsync(cancellationToken);

            try
            {
                /*
                 * Bu iki işlem kendi DbContext scope'larını oluşturduğu için
                 * aynı kümes için paralel çalıştırılabilir.
                 */
                var etiketGorevi = GetEtiketLogYeniDataForBulkInsert(
                    kumesId,
                    cancellationToken);

                var kumesLogGorevi = GetKumesLogYeniDataForBulkInsertAsync(
                    kumesId,
                    cancellationToken);

                await Task.WhenAll(etiketGorevi, kumesLogGorevi);

                return (
                    (await etiketGorevi).ToList(),
                    (await kumesLogGorevi).ToList());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Kümes {KumesId} verileri hazırlanırken hata oluştu.",
                    kumesId);

                return (
                    new List<EtiketLog_TempYeni>(),
                    new List<KumesLog_TempYeni>());
            }
            finally
            {
                _kumesIslemSiniri.Release();
            }
        }

        private async Task BulkUpsertKumesLogYeniDataAsync(
            SqlConnection connection,
            IEnumerable<KumesLog_TempYeni> data,
            CancellationToken cancellationToken)
        {
            /*
             * KumesLog_TempYeni anlık SCADA tablosudur. Her KumesId için tek
             * güncel satır tutulur (Kayislar1KumesNodeRedWorker ile aynı desen).
             */
            var dataList = data
                .GroupBy(x => x.KumesId)
                .Select(grup => grup.OrderByDescending(x => x.KayitZamani).First())
                .ToList();

            if (dataList.Count == 0)
                return;

            SqlTransaction? transaction = null;

            try
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync(cancellationToken);

                transaction = connection.BeginTransaction();

                const string stageTableSql = @"
SELECT TOP (0) *
INTO #KumesLogStage
FROM dbo.KumesLog_TempYeni;";

                using (var stageCommand = new SqlCommand(stageTableSql, connection, transaction))
                {
                    await stageCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                var dataTable = CreateDataTableWithoutId(dataList);

                using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = "#KumesLogStage";
                    bulkCopy.BulkCopyTimeout = 600;

                    foreach (DataColumn column in dataTable.Columns)
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);

                    await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
                }

                /*
                 * KumesLog_TempYeni modelinde zamanla yeni kolonlar eklenebileceği
                 * için UPDATE kolon listesini tablo metadata'sından üretiyoruz.
                 * Id / KumesId / identity / computed / rowversion kolonları güncellenmez.
                 */
                const string upsertSql = @"
DECLARE @SetList nvarchar(max);

SELECT @SetList =
    STRING_AGG(
        CAST('hedef.' + QUOTENAME(c.name) + ' = kaynak.' + QUOTENAME(c.name) AS nvarchar(max)),
        ','
    )
FROM sys.columns c
INNER JOIN tempdb.sys.columns tc
    ON tc.object_id = OBJECT_ID('tempdb..#KumesLogStage')
   AND tc.name COLLATE DATABASE_DEFAULT = c.name COLLATE DATABASE_DEFAULT
WHERE c.object_id = OBJECT_ID('dbo.KumesLog_TempYeni')
  AND c.name NOT IN ('Id', 'KumesId')
  AND c.is_identity = 0
  AND c.is_computed = 0
  AND c.system_type_id <> 189;

IF NULLIF(@SetList, '') IS NOT NULL
BEGIN
    DECLARE @UpdateSql nvarchar(max) =
        N'UPDATE hedef
          SET ' + @SetList + N'
          FROM dbo.KumesLog_TempYeni hedef
          INNER JOIN #KumesLogStage kaynak
              ON kaynak.KumesId = hedef.KumesId;';

    EXEC sys.sp_executesql @UpdateSql;
END;

DECLARE @InsertColumns nvarchar(max);
DECLARE @SelectColumns nvarchar(max);

SELECT
    @InsertColumns = STRING_AGG(CAST(QUOTENAME(c.name) AS nvarchar(max)), ','),
    @SelectColumns = STRING_AGG(CAST('kaynak.' + QUOTENAME(c.name) AS nvarchar(max)), ',')
FROM sys.columns c
INNER JOIN tempdb.sys.columns tc
    ON tc.object_id = OBJECT_ID('tempdb..#KumesLogStage')
   AND tc.name COLLATE DATABASE_DEFAULT = c.name COLLATE DATABASE_DEFAULT
WHERE c.object_id = OBJECT_ID('dbo.KumesLog_TempYeni')
  AND c.name <> 'Id'
  AND c.is_identity = 0
  AND c.is_computed = 0
  AND c.system_type_id <> 189;

DECLARE @InsertSql nvarchar(max) =
    N'INSERT INTO dbo.KumesLog_TempYeni (' + @InsertColumns + N')
      SELECT ' + @SelectColumns + N'
      FROM #KumesLogStage kaynak
      WHERE NOT EXISTS
      (
          SELECT 1
          FROM dbo.KumesLog_TempYeni hedef WITH (UPDLOCK, HOLDLOCK)
          WHERE hedef.KumesId = kaynak.KumesId
      );';

EXEC sys.sp_executesql @InsertSql;";

                using (var upsertCommand = new SqlCommand(upsertSql, connection, transaction))
                {
                    upsertCommand.CommandTimeout = 600;
                    await upsertCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    try { await transaction.RollbackAsync(cancellationToken); }
                    catch { }
                }

                _logger.LogError(ex, "KumesLog_TempYeni bulk upsert işlemi başarısız oldu.");
            }
            finally
            {
                transaction?.Dispose();

                if (connection.State != ConnectionState.Closed)
                    await connection.CloseAsync();
            }
        }

        private async Task BulkUpsertEtiketLogYeniDataAsync(
            SqlConnection connection,
            IEnumerable<EtiketLog_TempYeni> data,
            CancellationToken cancellationToken)
        {
            /*
             * EtiketLog_TempYeni anlık SCADA tablosudur. Her (KumesId, EtiketId)
             * için tek güncel satır tutulur (Kayislar1KumesNodeRedWorker ile aynı desen).
             */
            var dataList = data
                .GroupBy(x => new { x.KumesId, x.EtiketId })
                .Select(grup => grup.OrderByDescending(x => x.KayitZamani).First())
                .ToList();

            if (dataList.Count == 0)
                return;

            SqlTransaction? transaction = null;

            try
            {
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync(cancellationToken);

                transaction = connection.BeginTransaction();

                const string stageTableSql = @"
SELECT TOP (0)
       KumesId,
       EtiketId,
       KayitZamani,
       Deger
INTO #EtiketLogStage
FROM dbo.EtiketLog_TempYeni;";

                using (var stageCommand = new SqlCommand(stageTableSql, connection, transaction))
                {
                    await stageCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                var dataTable = new DataTable();
                dataTable.Columns.Add("KumesId", typeof(int));
                dataTable.Columns.Add("EtiketId", typeof(int));
                dataTable.Columns.Add("KayitZamani", typeof(DateTime));
                dataTable.Columns.Add("Deger", typeof(decimal));

                foreach (var item in dataList)
                {
                    var row = dataTable.NewRow();
                    row["KumesId"] = item.KumesId;
                    row["EtiketId"] = item.EtiketId;
                    row["KayitZamani"] = item.KayitZamani;
                    row["Deger"] = item.Deger.HasValue ? item.Deger.Value : DBNull.Value;
                    dataTable.Rows.Add(row);
                }

                using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = "#EtiketLogStage";
                    bulkCopy.BulkCopyTimeout = 600;

                    bulkCopy.ColumnMappings.Add("KumesId", "KumesId");
                    bulkCopy.ColumnMappings.Add("EtiketId", "EtiketId");
                    bulkCopy.ColumnMappings.Add("KayitZamani", "KayitZamani");
                    bulkCopy.ColumnMappings.Add("Deger", "Deger");

                    await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
                }

                const string upsertSql = @"
UPDATE hedef
SET
    hedef.KayitZamani = kaynak.KayitZamani,
    hedef.Deger = kaynak.Deger
FROM dbo.EtiketLog_TempYeni AS hedef
INNER JOIN #EtiketLogStage AS kaynak
    ON kaynak.KumesId = hedef.KumesId
   AND kaynak.EtiketId = hedef.EtiketId;

INSERT INTO dbo.EtiketLog_TempYeni
(
    KumesId,
    EtiketId,
    KayitZamani,
    Deger
)
SELECT
    kaynak.KumesId,
    kaynak.EtiketId,
    kaynak.KayitZamani,
    kaynak.Deger
FROM #EtiketLogStage AS kaynak
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.EtiketLog_TempYeni AS hedef WITH (UPDLOCK, HOLDLOCK)
    WHERE hedef.KumesId = kaynak.KumesId
      AND hedef.EtiketId = kaynak.EtiketId
);";

                using (var upsertCommand = new SqlCommand(upsertSql, connection, transaction))
                {
                    upsertCommand.CommandTimeout = 600;
                    await upsertCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    try { await transaction.RollbackAsync(cancellationToken); }
                    catch { }
                }

                _logger.LogError(ex, "EtiketLog_TempYeni bulk upsert işlemi başarısız oldu.");
            }
            finally
            {
                transaction?.Dispose();

                if (connection.State != ConnectionState.Closed)
                    await connection.CloseAsync();
            }
        }

        private static DataTable CreateDataTableWithoutId<T>(IEnumerable<T> data)
        {
            var dataTable = new DataTable();
            var accessor = TypeAccessor.Create(typeof(T));

            var members = accessor
                .GetMembers()
                .Where(x => !string.Equals(x.Name, "Id", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var member in members)
            {
                var type = Nullable.GetUnderlyingType(member.Type) ?? member.Type;
                dataTable.Columns.Add(member.Name, type);
            }

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

        private async Task<IEnumerable<KumesLog_TempYeni>> GetKumesLogYeniDataForBulkInsertAsync(int kumesId, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

                var nodeRedKaydi = await dbContext.KumesIzlemeNodeRed.AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.HasValue && x.KayitZamani.Value >= now.Date && x.KayitZamani.Value < now.Date.AddDays(1)).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);

                if (!PlcOkuyorMu(nodeRedKaydi, now))
                    return Array.Empty<KumesLog_TempYeni>();

                var model = JsonSerializer.Deserialize<KumesNodeRedModel>(nodeRedKaydi.Veriler);
                if (model == null)
                    return Array.Empty<KumesLog_TempYeni>();

                var icmeSuyuTuketim = model.IcmeSuyu_TuketimBugun / 1000;

                // PLC tüketim değeri beklenenden yüksek gelirse sayaç farkından yeniden hesapla.
                if (icmeSuyuTuketim.GetValueOrDefault() > 50)
                {
                    var mevcutSayac = model.IcmeSuyu_Sayac.GetValueOrDefault();
                    var dunSayac = await _alarmCacheService.GetDunIcmeSuyuSayacByKumesId(kumesId, mevcutSayac);

                    icmeSuyuTuketim = mevcutSayac >= dunSayac ? mevcutSayac - dunSayac : mevcutSayac;
                }

                return new[]
                {
                    new KumesLog_TempYeni
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
                        // Yumurta alanları özellikle doldurulmamaktadır.
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kümes {KumesId} için KumesLog_TempYeni verisi hazırlanamadı.", kumesId);
                return Array.Empty<KumesLog_TempYeni>();
            }
        }

        private async Task<IEnumerable<EtiketLog_TempYeni>> GetEtiketLogYeniDataForBulkInsert(int kumesId, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;
                var etiketLogs = new List<EtiketLog_TempYeni>();
                KumesIzlemeNodeRed? model;
                var gunlukHayvanSayisi = await _alarmCacheService.GetGunlukCanliHayvanSayisiByKumesId(kumesId);

                // EtiketId 106-143 yumurta bilgilerine ait olduğu için bu worker içinde üretilmez.
                using (var scope = _serviceProvider.CreateScope())
                {
                    var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    model = await kumesScadaDbContext.KumesIzlemeNodeRed.AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.HasValue && x.KayitZamani.Value >= now.Date && x.KayitZamani.Value < now.Date.AddDays(1)).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);

                    var plcOkuyorMu = PlcOkuyorMu(model, now);
                    await AlarmDurumunuSenkronizeEtAsync(
                        kumesId: kumesId,
                        alarmEslemeSozcugu: PlcOkunmuyorAlarmEslemeSozcugu,
                        alarmAktifOlmali: !plcOkuyorMu,
                        durumDetayi: model?.KayitZamani.HasValue == true
                            ? $"Son PLC kaydı: {model.KayitZamani:dd.MM.yyyy HH:mm:ss}"
                            : "PLC kaydı bulunamadı.",
                        cancellationToken: stoppingToken);

                    if (!plcOkuyorMu)
                    {
                        etiketLogs.Add(new EtiketLog_TempYeni
                        {
                            KumesId = kumesId,
                            EtiketId = 2,
                            KayitZamani = now,
                            Deger = 0
                        });
                        etiketLogs.Add(new EtiketLog_TempYeni
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
                        etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                        etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
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
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 71, KayitZamani = DateTime.Now, Deger = (decimal?)byteDensity });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 72, KayitZamani = DateTime.Now, Deger = (decimal?)lastPackageLength });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 73, KayitZamani = DateTime.Now, Deger = (decimal?)pingAverage });

                                if (plcConnected == 0) return etiketLogs;
                                // PLC okuma durumu ayrıca kontrol edilmek istenirse plcRead kullanılabilir.

                                var sicaklik_Kumes_Ort = kumesNodeRedModel.Sicaklik_Kumes_Value;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 4, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Kumes_Ort });
                                var sicaklik_Dis = kumesNodeRedModel.Sicaklik_Dis_Value;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 5, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Dis });
                                var sicaklik_Sensor1 = kumesNodeRedModel.Sicaklik_Sensor_1_Value;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 6, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor1 });
                                var sicaklik_Sensor2 = kumesNodeRedModel.Sicaklik_Sensor_2_Value;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 7, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor2 });
                                var sicaklik_Sensor3 = kumesNodeRedModel.Sicaklik_Sensor_3_Value;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 8, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor3 });
                                var sicaklik_Sensor4 = kumesNodeRedModel.Sicaklik_Sensor_4_Value;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 9, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor4 });
                                var sicaklik_Sensor5 = kumesNodeRedModel.Sicaklik_Sensor_5_Value;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 10, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor5 });
                                var silo1_value = kumesNodeRedModel.Yem_Silo_1_Value / 1000;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 28, KayitZamani = DateTime.Now, Deger = (decimal?)silo1_value });
                                var silo2_value = kumesNodeRedModel.Yem_Silo_2_Value / 1000;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 31, KayitZamani = DateTime.Now, Deger = (decimal?)silo2_value });
                                var nem_Kumes = kumesNodeRedModel.Nem_Kumes_Value;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 81, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Kumes });
                                var nem_Sensor1 = kumesNodeRedModel.Nem_Sensor_1_Value;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 46, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor1 });

                                var basinc = kumesNodeRedModel.Basinc_Sens_Value;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 42, KayitZamani = DateTime.Now, Deger = (decimal?)basinc });

                                var suSayacTuketim = kumesNodeRedModel.IcmeSuyu_Sayac;
                                var suTuketimDun = kumesNodeRedModel.IcmeSuyu_TuketimDun / 1000;
                                var suTuketimBugun = kumesNodeRedModel.IcmeSuyu_TuketimBugun / 1000;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 75, KayitZamani = DateTime.Now, Deger = (decimal?)suTuketimDun });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 76, KayitZamani = DateTime.Now, Deger = (decimal?)suSayacTuketim });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 16, KayitZamani = DateTime.Now, Deger = (decimal?)suTuketimBugun });
                                var yemTuketim = kumesNodeRedModel.Yem_TuketimBugun / 1000;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 17, KayitZamani = DateTime.Now, Deger = (decimal?)yemTuketim });
                                var aydinlatma_Ac = kumesNodeRedModel.Aydinlatma_Ac == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 77, KayitZamani = DateTime.Now, Deger = (decimal?)aydinlatma_Ac });
                                var fanMode = kumesNodeRedModel.Fan_AktifGrup;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 18, KayitZamani = DateTime.Now, Deger = (decimal?)fanMode });



                                var Buffel_2_Sol_State_Durum = kumesNodeRedModel.Buffel_2_Sol_State_Durum;
                                var Buffel_2_Sol_Cmd_CmdBit = kumesNodeRedModel.Buffel_2_Sol_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 574, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 577, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_Cmd_CmdBit.GetValueOrDefault(0) });

                                var Buffel_2_Sag_State_Durum = kumesNodeRedModel.Buffel_2_Sag_State_Durum;
                                var Buffel_2_Sag_Cmd_CmdBit = kumesNodeRedModel.Buffel_2_Sag_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 583, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 586, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_1_State_Durum = kumesNodeRedModel.PedKapak_On_1_State_Durum;
                                var PedKapak_On_1_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_1_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 592, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 595, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_2_State_Durum = kumesNodeRedModel.PedKapak_On_2_State_Durum;
                                var PedKapak_On_2_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_2_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 601, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 604, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_3_State_Durum = kumesNodeRedModel.PedKapak_On_3_State_Durum;
                                var PedKapak_On_3_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_3_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 610, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 613, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_4_State_Durum = kumesNodeRedModel.PedKapak_On_4_State_Durum;
                                var PedKapak_On_4_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_4_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 619, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 622, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_1_State_Durum = kumesNodeRedModel.PedKapak_Arka_1_State_Durum;
                                var PedKapak_Arka_1_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_1_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 628, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 631, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_2_State_Durum = kumesNodeRedModel.PedKapak_Arka_2_State_Durum;
                                var PedKapak_Arka_2_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_2_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 637, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 640, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_3_State_Durum = kumesNodeRedModel.PedKapak_Arka_3_State_Durum;
                                var PedKapak_Arka_3_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_3_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 646, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 649, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_4_State_Durum = kumesNodeRedModel.PedKapak_Arka_4_State_Durum;
                                var PedKapak_Arka_4_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_4_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 655, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 658, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_Cmd_CmdBit.GetValueOrDefault(0) });


                                var BacaFani_1_Calisiyor = kumesNodeRedModel.BacaFani_1_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 696, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_1_Calisiyor });

                                var BacaFani_2_Calisiyor = kumesNodeRedModel.BacaFani_2_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 701, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_2_Calisiyor });

                                var BacaFani_3_Calisiyor = kumesNodeRedModel.BacaFani_3_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 706, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_3_Calisiyor });

                                var BacaFani_4_Calisiyor = kumesNodeRedModel.BacaFani_4_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 711, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_4_Calisiyor });

                                var BacaFani_5_Calisiyor = kumesNodeRedModel.BacaFani_5_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 716, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_5_Calisiyor });

                                var BacaFani_6_Calisiyor = kumesNodeRedModel.BacaFani_6_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 721, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_6_Calisiyor });

                                var BacaFani_7_Calisiyor = kumesNodeRedModel.BacaFani_7_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 726, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_7_Calisiyor });

                                var BacaFani_8_Calisiyor = kumesNodeRedModel.BacaFani_8_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 731, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_8_Calisiyor });

                                var BacaFani_9_Calisiyor = kumesNodeRedModel.BacaFani_9_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 736, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_9_Calisiyor });

                                var BacaFani_10_Calisiyor = kumesNodeRedModel.BacaFani_10_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 741, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_10_Calisiyor });

                                var BacaFani_11_Calisiyor = kumesNodeRedModel.BacaFani_11_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 746, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_11_Calisiyor });

                                var BacaFani_12_Calisiyor = kumesNodeRedModel.BacaFani_12_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 751, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_12_Calisiyor });

                                var BacaFani_13_Calisiyor = kumesNodeRedModel.BacaFani_13_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 756, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_13_Calisiyor });

                                var BacaFani_14_Calisiyor = kumesNodeRedModel.BacaFani_14_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 761, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_14_Calisiyor });

                                var BacaFani_15_Calisiyor = kumesNodeRedModel.BacaFani_15_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 766, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_15_Calisiyor });

                                var BacaFani_16_Calisiyor = kumesNodeRedModel.BacaFani_16_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 771, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_16_Calisiyor });

                                var BacaFani_17_Calisiyor = kumesNodeRedModel.BacaFani_17_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 776, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_17_Calisiyor });

                                var BacaFani_18_Calisiyor = kumesNodeRedModel.BacaFani_18_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 781, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_18_Calisiyor });

                                var BacaFani_19_Calisiyor = kumesNodeRedModel.BacaFani_19_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 786, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_19_Calisiyor });

                                var BacaFani_20_Calisiyor = kumesNodeRedModel.BacaFani_20_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 791, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_20_Calisiyor });

                                var BacaFani_21_Calisiyor = kumesNodeRedModel.BacaFani_21_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 796, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_21_Calisiyor });

                                var BacaFani_22_Calisiyor = kumesNodeRedModel.BacaFani_22_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 801, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_22_Calisiyor });

                                var BacaFani_23_Calisiyor = kumesNodeRedModel.BacaFani_23_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 806, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_23_Calisiyor });

                                var BacaFani_24_Calisiyor = kumesNodeRedModel.BacaFani_24_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 811, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_24_Calisiyor });

                                var hayvanBasinaSuTuketim = gunlukHayvanSayisi == 0 ? 0 : (kumesNodeRedModel.IcmeSuyu_TuketimBugun * 1000) / gunlukHayvanSayisi;
                                var hayvanBasinaYemTuketim = gunlukHayvanSayisi == 0 ? 0 : (kumesNodeRedModel.Yem_TuketimBugun * 1000) / gunlukHayvanSayisi;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 104, KayitZamani = DateTime.Now, Deger = (decimal?)hayvanBasinaSuTuketim });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 105, KayitZamani = DateTime.Now, Deger = (decimal?)hayvanBasinaYemTuketim });

                                var BacaFani_25_Calisiyor = kumesNodeRedModel.BacaFani_25_Calisiyor == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 816, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_25_Calisiyor });

                                var IcmeSuyu_Toplam = kumesNodeRedModel.IcmeSuyu_Toplam;
                                var BacaFani_TermikOk = kumesNodeRedModel.BacaFani_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 820, KayitZamani = DateTime.Now, Deger = (decimal?)IcmeSuyu_Toplam });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 821, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_TermikOk });

                                var PedPompa_1_Komut = kumesNodeRedModel.PedPompa_1_Komut;
                                var PedPompa_1_TermikOk = kumesNodeRedModel.PedPompa_1_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 822, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_1_Komut });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 823, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_1_TermikOk });

                                var PedPompa_2_Komut = kumesNodeRedModel.PedPompa_2_Komut;
                                var PedPompa_2_TermikOk = kumesNodeRedModel.PedPompa_2_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 824, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_2_Komut });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 825, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_2_TermikOk });

                                var PedPompa_3_Komut = kumesNodeRedModel.PedPompa_3_Komut;
                                var PedPompa_3_TermikOk = kumesNodeRedModel.PedPompa_3_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 826, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_3_Komut });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 827, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_3_TermikOk });

                                var PedPompa_4_Komut = kumesNodeRedModel.PedPompa_4_Komut;
                                var PedPompa_4_TermikOk = kumesNodeRedModel.PedPompa_4_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 828, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_4_Komut });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 829, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_4_TermikOk });

                                var PedPompa_5_Komut = kumesNodeRedModel.PedPompa_5_Komut;
                                var PedPompa_5_TermikOk = kumesNodeRedModel.PedPompa_5_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 830, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_5_Komut });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 831, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_5_TermikOk });

                                var PedPompa_6_Komut = kumesNodeRedModel.PedPompa_6_Komut;
                                var PedPompa_6_TermikOk = kumesNodeRedModel.PedPompa_6_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 832, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_6_Komut });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 833, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_6_TermikOk });

                                var PedPompa_7_Komut = kumesNodeRedModel.PedPompa_7_Komut;
                                var PedPompa_7_TermikOk = kumesNodeRedModel.PedPompa_7_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 834, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_7_Komut });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 835, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_7_TermikOk });

                                var PedPompa_8_Komut = kumesNodeRedModel.PedPompa_8_Komut;
                                var PedPompa_8_TermikOk = kumesNodeRedModel.PedPompa_8_TermikOk == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 836, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_8_Komut });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 837, KayitZamani = DateTime.Now, Deger = (decimal?)PedPompa_8_TermikOk });
                            }
                            else
                            {
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            }
                        }
                    }
                    return etiketLogs;
                }
            }
            catch (Exception)
            {
                return new List<EtiketLog_TempYeni>();
            }
        }


        private static bool PlcOkuyorMu(
            KumesIzlemeNodeRed? nodeRedKaydi,
            DateTime kontrolZamani)
        {
            if (nodeRedKaydi?.Durum != 1 ||
                string.IsNullOrWhiteSpace(nodeRedKaydi.Veriler) ||
                !nodeRedKaydi.KayitZamani.HasValue)
            {
                return false;
            }

            return nodeRedKaydi.KayitZamani.Value >=
                   kontrolZamani.Subtract(PlcVeriZamanAsimi);
        }

        private async Task AlarmTanimlariniGerekirseYenileAsync(
            CancellationToken cancellationToken)
        {
            if (DateTime.Now - _sonAlarmTanimYuklemeZamani <
                AlarmTanimYenilemeSuresi)
            {
                return;
            }

            await AlarmTanimlariniYukleAsync(cancellationToken);
        }

        private async Task AlarmTanimlariniYukleAsync(
            CancellationToken cancellationToken)
        {
            await _alarmTanimYuklemeKilidi.WaitAsync(cancellationToken);

            try
            {
                if (DateTime.Now - _sonAlarmTanimYuklemeZamani <
                    AlarmTanimYenilemeSuresi)
                {
                    return;
                }

                var kumesIds = _kumesListesi
                    .Select(x => x.Id)
                    .Distinct()
                    .ToList();

                if (kumesIds.Count == 0)
                {
                    _alarmTanimlari =
                        new Dictionary<KumesAlarmTanimAnahtari, KumesAlarmTanimi>();
                    _sonAlarmTanimYuklemeZamani = DateTime.Now;
                    return;
                }

                await using var scope = _serviceProvider.CreateAsyncScope();
                var sharedDbContext = scope.ServiceProvider
                    .GetRequiredService<SharedDbContext>();

                /*
                 * Worker'ın yönettiği kümeslere ait bütün alarm tanımları alınır.
                 * Burada PLC alarmına özel filtre uygulanmaz. Böylece daha sonra
                 * yeni bir alarm geldiğinde yalnızca AlarmDurumunuSenkronizeEtAsync
                 * metoduna yeni eşleme sözcüğüyle çağrı yapılması yeterlidir.
                 */
                var alarmAdaylari = await sharedDbContext.Alarmlar
                    .AsNoTracking()
                    .Where(x =>
                        x.KumesId.HasValue &&
                        kumesIds.Contains(x.KumesId.Value) &&
                        x.Silindi != true &&
                        x.AlarmEslemeSozcugu != null)
                    .Select(x => new
                    {
                        x.Id,
                        KumesId = x.KumesId!.Value,
                        x.AlarmAd,
                        x.Aciklama,
                        x.AlarmEslemeSozcugu
                    })
                    .ToListAsync(cancellationToken);

                var alarmTanimlari = alarmAdaylari
                    .Select(x => new
                    {
                        Alarm = x,
                        EslemeAnahtari = AlarmEslemeSozcuguNormalizeEt(
                            x.AlarmEslemeSozcugu)
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.EslemeAnahtari))
                    .GroupBy(x => new KumesAlarmTanimAnahtari(
                        x.Alarm.KumesId,
                        x.EslemeAnahtari))
                    .ToDictionary(
                        grup => grup.Key,
                        grup =>
                        {
                            var alarm = grup
                                .OrderBy(x => x.Alarm.Id)
                                .First()
                                .Alarm;

                            var mesaj = !string.IsNullOrWhiteSpace(alarm.Aciklama)
                                ? alarm.Aciklama.Trim()
                                : !string.IsNullOrWhiteSpace(alarm.AlarmAd)
                                    ? alarm.AlarmAd.Trim()
                                    : alarm.AlarmEslemeSozcugu!.Trim();

                            return new KumesAlarmTanimi(
                                alarm.Id,
                                alarm.AlarmEslemeSozcugu!.Trim(),
                                mesaj);
                        });

                _alarmTanimlari = alarmTanimlari;
                _sonAlarmTanimYuklemeZamani = DateTime.Now;

                var tekrarliTanimlar = alarmAdaylari
                    .Select(x => new
                    {
                        x.KumesId,
                        x.AlarmEslemeSozcugu,
                        EslemeAnahtari = AlarmEslemeSozcuguNormalizeEt(
                            x.AlarmEslemeSozcugu)
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.EslemeAnahtari))
                    .GroupBy(x => new
                    {
                        x.KumesId,
                        x.EslemeAnahtari
                    })
                    .Where(grup => grup.Count() > 1)
                    .Select(grup =>
                        $"KumesId={grup.Key.KumesId}, " +
                        $"AlarmEslemeSozcugu={grup.First().AlarmEslemeSozcugu}")
                    .ToList();

                if (tekrarliTanimlar.Count > 0)
                {
                    _logger.LogWarning(
                        "Aynı kümes ve alarm eşleme sözcüğü için birden fazla " +
                        "alarm tanımı bulundu. En küçük Alarm Id kullanıldı. " +
                        "Tanımlar: {AlarmTanimlari}",
                        string.Join(" | ", tekrarliTanimlar));
                }

                /*
                 * Şu anda worker'ın mutlaka kullanması gereken tek alarm PLC
                 * okunmuyor alarmıdır. Diğer alarm türleri eklendiğinde kendi
                 * kontrolleri sırasında aynı genel metot kullanılacaktır.
                 */
                var plcAlarmAnahtari = AlarmEslemeSozcuguNormalizeEt(
                    PlcOkunmuyorAlarmEslemeSozcugu);

                var plcAlarmTanimiOlmayanKumesler = kumesIds
                    .Where(kumesId =>
                        !alarmTanimlari.ContainsKey(
                            new KumesAlarmTanimAnahtari(
                                kumesId,
                                plcAlarmAnahtari)))
                    .OrderBy(x => x)
                    .ToList();

                if (plcAlarmTanimiOlmayanKumesler.Count > 0)
                {
                    _logger.LogWarning(
                        "PLC okunmuyor alarm tanımı bulunamayan kümesler var. " +
                        "Alarmlar tablosunda KumesId ve " +
                        "AlarmEslemeSozcugu={AlarmEslemeSozcugu} kontrol edilmelidir. " +
                        "KumesIdleri: {KumesIdleri}",
                        PlcOkunmuyorAlarmEslemeSozcugu,
                        string.Join(", ", plcAlarmTanimiOlmayanKumesler));
                }
            }
            finally
            {
                _alarmTanimYuklemeKilidi.Release();
            }
        }

        private static string AlarmEslemeSozcuguNormalizeEt(string? deger)
        {
            if (string.IsNullOrWhiteSpace(deger))
                return string.Empty;

            return new string(
                deger
                    .Where(char.IsLetterOrDigit)
                    .Select(char.ToUpperInvariant)
                    .ToArray());
        }

        private bool AlarmTaniminiBul(
            int kumesId,
            string alarmEslemeSozcugu,
            out KumesAlarmTanimi alarmTanimi)
        {
            var anahtar = new KumesAlarmTanimAnahtari(
                kumesId,
                AlarmEslemeSozcuguNormalizeEt(alarmEslemeSozcugu));

            return _alarmTanimlari.TryGetValue(
                anahtar,
                out alarmTanimi!);
        }

        /// <summary>
        /// Kümes alarmını, Alarmlar tablosundaki KumesId ve
        /// AlarmEslemeSozcugu eşleşmesine göre açar veya kapatır.
        ///
        /// AlarmId hiçbir şekilde KumesId üzerinden hesaplanmaz. Aktif kayıt
        /// doğrudan gerçek AlarmId ile arandığı için farklı alarm türleri
        /// birbirinin kayıtlarını etkilemez.
        /// </summary>
        private async Task AlarmDurumunuSenkronizeEtAsync(
            int kumesId,
            string alarmEslemeSozcugu,
            bool alarmAktifOlmali,
            string? durumDetayi,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!AlarmTaniminiBul(
                        kumesId,
                        alarmEslemeSozcugu,
                        out var alarmTanimi))
                {
                    // Eksik tanımlar AlarmTanimlariniYukleAsync içinde toplu olarak
                    // raporlanır. Bu metot her döngüde çalıştığı için burada warning
                    // basılması logları gereksiz yere doldurur.
                    _logger.LogDebug(
                        "Kümes alarm tanımı bulunamadı. " +
                        "KumesId: {KumesId}, AlarmEslemeSozcugu: {AlarmEslemeSozcugu}",
                        kumesId,
                        alarmEslemeSozcugu);
                    return;
                }

                await using var scope = _serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider
                    .GetRequiredService<KumesScadaDbContext>();

                /*
                 * AlarmEnum ile genel arama yapılmaz. AlarmEnum aynı olan veya
                 * ileride aynı grupta değerlendirilen farklı alarm tanımları
                 * bulunabilir. Gerçek AlarmId en güvenli ve tekil eşleşmedir.
                 */
                var aktifAlarmlar = await dbContext.AlarmLog
                    .Where(x =>
                        x.KumesId == kumesId &&
                        x.AlarmId == alarmTanimi.AlarmId &&
                        x.Aktif != false &&
                        x.Silindi != true)
                    .ToListAsync(cancellationToken);

                if (alarmAktifOlmali)
                {
                    if (aktifAlarmlar.Count == 0)
                    {
                        var alarm = new AlarmLog
                        {
                            KayitZamani = DateTime.Now,
                            AlarmSeviye = 0,
                            KumesId = kumesId,
                            AlarmId = alarmTanimi.AlarmId,
                            // Alarm tipi gerçek AlarmId üzerinden belirlenir.
                            // Rahmiye projesine özel AlarmLogTipler enum'una bağımlılık yoktur.
                            AlarmEnum = null,
                            Mesaj = alarmTanimi.Mesaj,
                            KonveyorId = null,
                            Aktif = true,
                            Silindi = false
                        };

                        await dbContext.AlarmLog.AddAsync(
                            alarm,
                            cancellationToken);

                        await dbContext.SaveChangesAsync(cancellationToken);
                        _alarmCacheService.ClearMemoryCache(kumesId);

                        _logger.LogWarning(
                            "Kümes alarmı oluşturuldu. KumesId: {KumesId}, " +
                            "AlarmId: {AlarmId}, AlarmEslemeSozcugu: {AlarmEslemeSozcugu}, " +
                            "Detay: {DurumDetayi}",
                            kumesId,
                            alarmTanimi.AlarmId,
                            alarmTanimi.AlarmEslemeSozcugu,
                            durumDetayi);

                        return;
                    }

                    var alarmGuncellendiMi = false;

                    foreach (var aktifAlarm in aktifAlarmlar)
                    {
                        if (aktifAlarm.Mesaj != alarmTanimi.Mesaj)
                        {
                            aktifAlarm.Mesaj = alarmTanimi.Mesaj;
                            alarmGuncellendiMi = true;
                        }

                    }

                    if (alarmGuncellendiMi)
                    {
                        await dbContext.SaveChangesAsync(cancellationToken);
                        _alarmCacheService.ClearMemoryCache(kumesId);
                    }

                    return;
                }

                if (aktifAlarmlar.Count == 0)
                    return;

                var pasifZamani = DateTime.Now;

                foreach (var aktifAlarm in aktifAlarmlar)
                {
                    aktifAlarm.Aktif = false;
                    aktifAlarm.PasifZamani = pasifZamani;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                _alarmCacheService.ClearMemoryCache(kumesId);

                //_logger.LogInformation(
                //    "Kümes alarmı pasife çekildi. KumesId: {KumesId}, " +
                //    "AlarmId: {AlarmId}, AlarmEslemeSozcugu: {AlarmEslemeSozcugu}, " +
                //    "Pasife çekilen kayıt sayısı: {AlarmSayisi}",
                //    kumesId,
                //    alarmTanimi.AlarmId,
                //    alarmTanimi.AlarmEslemeSozcugu,
                //    aktifAlarmlar.Count);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Kümes alarm durumu güncellenemedi. " +
                    "KumesId: {KumesId}, AlarmEslemeSozcugu: {AlarmEslemeSozcugu}",
                    kumesId,
                    alarmEslemeSozcugu);
            }
        }

        public async Task AddAylikKumesHayvanLog(int kumesId, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

                var now = DateTime.Now;
                var ayinIlkGunu = new DateTime(now.Year, now.Month, 1);
                var ayinSonGunu = ayinIlkGunu.AddMonths(1).AddDays(-1);

                var mevcutKayitlar = await dbContext.KumesHayvanLog
                    .Where(x =>
                        x.SaglikDurumu == KumesHayvanSaglikDurum.Olu
                        && x.KumesId == kumesId
                        && x.KayitZamani >= ayinIlkGunu
                        && x.KayitZamani <= ayinSonGunu)
                    .Select(x => x.KayitZamani!.Value.Date)
                    .ToListAsync(cancellationToken);

                for (var gun = ayinIlkGunu; gun <= ayinSonGunu; gun = gun.AddDays(1))
                {
                    if (mevcutKayitlar.Contains(gun.Date))
                        continue;

                    await dbContext.KumesHayvanLog.AddAsync(
                        new KumesHayvanLog
                        {
                            KumesId = kumesId,
                            DonemId = null,
                            Adet = 0,
                            SaglikDurumu = KumesHayvanSaglikDurum.Olu,
                            GirisZamani = null,
                            KayitZamani = gun,
                            KayitYapanUserId = null,
                            Silindi = true
                        },
                        cancellationToken);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kümes {KumesId} için aylık hayvan log kayıtları hazırlanamadı.", kumesId);
            }
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
                _logger.LogError(ex, "Verim Gıda Ticari Yumurta worker durdurulurken hata oluştu.");
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
                _logger.LogError(ex, "Servis durum bilgisi güncellenemedi. Servis: {ServisAdi}", servisName);
            }
        }
    }
}
