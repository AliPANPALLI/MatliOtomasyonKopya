using FastMember;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RahmiyeTesisWorker.Core;
using RahmiyeTesisWorker.Models;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using SharedKernel;
//using StackExchange.Redis;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using static SharedKernel.ApplicationConstants;
using AlarmLog = PoultryHouseScada.Domain.Models.AlarmLog;

namespace RahmiyeTesisWorker
{
    public class RahmiyeNodeRedWorker : BackgroundService
    {
        private readonly ILogger<RahmiyeNodeRedWorker> _logger;
        private readonly SqlConnection _sqlConnection;
        private readonly IServiceProvider _serviceProvider;
        private readonly AlarmCacheService _alarmCacheService;
        private List<Kumes> kumesListesi = new List<Kumes>();
        private Dictionary<int, EggActualDbModel?> yumurtaBilgileriDict = new Dictionary<int, EggActualDbModel?>();
        private List<ValueTuple<int, bool, DateTime>> hayvanSayisiMailGonderildi = new List<ValueTuple<int, bool, DateTime>>();
        private Dictionary<int, DateTime> kumesHayvanLogAylikKayitAtildi = new();
        private readonly string _vbapConnStr;
        //private readonly IDatabase _db;
        private readonly TimeSpan _ttl = TimeSpan.FromHours(24);

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

        public RahmiyeNodeRedWorker(ILogger<RahmiyeNodeRedWorker> logger, SqlConnection sqlConnection, IServiceProvider serviceProvider, AlarmCacheService alarmCacheService, IConfiguration configuration)
        {
            string connectionString = "10.1.180.10:6379";
            _vbapConnStr = configuration.GetConnectionString("SqlServerVbap");
            _logger = logger;
            _sqlConnection = sqlConnection;
            _alarmCacheService = alarmCacheService;
            _serviceProvider = serviceProvider;
            //var redis = ConnectionMultiplexer.Connect(connectionString);
            //_db = redis.GetDatabase();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("RahmiyeKumesWorker working: {time}", DateTimeOffset.Now);
                await UpdateServisDurumAsync("RahmiyeTesisWorker", 2, "Servis başlatılıyor");
                kumesListesi = await _alarmCacheService.GetKumeslerAsync(12);
                for (int i = 0; i < kumesListesi.Count(); i++)
                {
                    hayvanSayisiMailGonderildi.Add(new ValueTuple<int, bool, DateTime>((int)kumesListesi[i].Id, false, DateTime.Now));
                }
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                await UpdateServisDurumAsync("RahmiyeTesisWorker", 0, ex.Message);
            }
        }
        //private async Task RedisTest()
        //{
        //    try
        //    {
        //        // SUNUCU IP
        //        string connectionString = "10.1.180.10:6379";

        //        var redis = await ConnectionMultiplexer.ConnectAsync(connectionString);

        //        Console.WriteLine("Valkey ba�lant�s� ba�ar�l�");

        //        IDatabase db = redis.GetDatabase();

        //        // SUNUCUDAK� KEY
        //        var value = await db.StringGetAsync("ad");

        //        Console.WriteLine($"ad = {value}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //}
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    yumurtaBilgileriDict.Clear();
                    for (int i = 0; i < kumesListesi.Count(); i++)
                    {
                        var yumurtaBilgileri = await GetVbapYumurtaBilgileri(kumesListesi[i].Id, DateTime.Now, stoppingToken);
                        yumurtaBilgileriDict[kumesListesi[i].Id] = yumurtaBilgileri;
                        var dataAlarm = await GetAlarmLogDataForBulkInsert(kumesListesi[i].Id, stoppingToken);
                        await BulkInsertAlarmLogDataAsync(_sqlConnection, dataAlarm);
                        var dataAlarmUretilmis = await GetUretilmisAlarmLogDataForKumes(kumesListesi[i].Id, stoppingToken);
                        await BulkInsertAlarmLogDataAsync(_sqlConnection, dataAlarmUretilmis);
                        //var dataEtiketLog = await GetEtiketLogDataForBulkInsert(kumesListesi[i].Id, stoppingToken);
                        //await BulkInsertEtiketLogDataAsync(_sqlConnection, dataEtiketLog);
                        var dataEtiketLogYeni = await GetEtiketLogYeniDataForBulkInsert(kumesListesi[i].Id, stoppingToken);
                        await BulkUpsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeni, stoppingToken);
                        //await RedisBulkSetEtiketLogAsync(dataEtiketLogYeni);
                        //var dataKumesLog = await GetKumesLogDataForBulkInsertAsync(kumesListesi[i].Id, stoppingToken);
                        //if (dataKumesLog.Count() > 0) await BulkInsertKumesLogDataAsync(_sqlConnection, dataKumesLog);
                        var dataKumesLogYeni = await GetKumesLogYeniDataForBulkInsertAsync(kumesListesi[i].Id, stoppingToken);
                        if (dataKumesLogYeni.Count() > 0) await BulkUpsertKumesLogYeniDataAsync(_sqlConnection, dataKumesLogYeni, stoppingToken);
                        var today = DateTime.Today;
                        if (!kumesHayvanLogAylikKayitAtildi.ContainsKey(kumesListesi[i].Id) || kumesHayvanLogAylikKayitAtildi[kumesListesi[i].Id] < today)
                        {
                            await AddAylikKumesHayvanLog(kumesListesi[i].Id);
                            kumesHayvanLogAylikKayitAtildi[kumesListesi[i].Id] = today;
                        }
                    }
                    await UpdateServisDurumAsync("RahmiyeTesisWorker", 1, "Servis başlıyor", true);

                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    await UpdateServisDurumAsync("RahmiyeTesisWorker", 0, ex.Message);
                }
            }
        }

        public void PlcLogYaz(string alarmMesaj)
        {
            try
            {
                if (_sqlConnection.State != ConnectionState.Open)
                {
                    _sqlConnection.Open();
                }
                if (_sqlConnection.State == ConnectionState.Open)
                {
                    const string queryString = "INSERT INTO Logs (KayitZamani, Mesaj) " +
                                               "VALUES (@KayitZamani, @Mesaj);";
                    SqlCommand command = new(queryString, _sqlConnection);

                    command.Parameters.AddWithValue("@KayitZamani", System.DateTime.Now);
                    command.Parameters.AddWithValue("@Mesaj", alarmMesaj);

                    command.ExecuteNonQuery();
                }

                if (_sqlConnection.State != ConnectionState.Closed)
                {
                    _sqlConnection.Close();
                }
            }
            catch (Exception)
            {
            }
        }

        public async Task BulkInsertKumesLogDataAsync(SqlConnection connection, IEnumerable<KumesLog_Temp> data)
        {
            try
            {
                var dataTable = new DataTable();
                var accessor = TypeAccessor.Create(typeof(KumesLog_Temp)); // FastMember ile eri�im

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
                    bulkCopy.DestinationTableName = "KumesLog_Temp"; // Verinin eklenece�i tablo ad�
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

        public async Task BulkUpsertKumesLogYeniDataAsync(
            SqlConnection connection,
            IEnumerable<KumesLog_TempYeni> data,
            CancellationToken cancellationToken)
        {
            /*
             * KumesLog_TempYeni anl�k SCADA tablosudur. Her KumesId i�in tek
             * g�ncel sat�r tutulur (Kayislar1KumesNodeRedWorker ile ayn� desen).
             *
             * Ayn� turda ayn� k�mese ait birden fazla kay�t olu�ursa
             * en g�ncel KayitZamani olan kay�t kullan�l�r.
             */
            var dataList = data
                .GroupBy(x => x.KumesId)
                .Select(grup => grup
                    .OrderByDescending(x => x.KayitZamani)
                    .First())
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
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }

                    await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
                }

                /*
                 * KumesLog_TempYeni modelinde zamanla yeni kolonlar eklenebilece�i
                 * i�in UPDATE kolon listesini tablo metadata's�ndan �retiyoruz.
                 * Id / KumesId / identity / computed / rowversion kolonlar� g�ncellenmez.
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

                _logger.LogError(ex, "KumesLog_TempYeni bulk upsert i�lemi ba�ar�s�z oldu.");
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

        public async Task BulkInsertEtiketLogDataAsync(SqlConnection connection, IEnumerable<EtiketLog_Temp> data)
        {
            try
            {
                var dataTable = new DataTable();
                var accessor = TypeAccessor.Create(typeof(EtiketLog_Temp)); // FastMember ile eri�im

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
                    bulkCopy.DestinationTableName = "EtiketLog_Temp"; // Verinin eklenece�i tablo ad�
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

        //public async Task RedisBulkSetEtiketLogAsync(IEnumerable<EtiketLog_TempYeni> etiketLogs)
        //{
        //    var grouped = etiketLogs.Where(x => x.KumesId.HasValue && x.EtiketId.HasValue).GroupBy(x => x.KumesId!.Value);
        //    foreach (var group in grouped)
        //    {
        //        var key = $"kumes:{group.Key}:etiketler";

        //        var hashFields = group.Select(e => new HashEntry(
        //            e.EtiketId!.Value.ToString(),
        //            JsonSerializer.Serialize(new
        //            {
        //                Deger = e.Deger,
        //                KayitZamani = e.KayitZamani
        //            })
        //        )).ToArray();

        //        await _db.HashSetAsync(key, hashFields);
        //        await _db.KeyExpireAsync(key, _ttl);
        //    }
        //}

        // Tek etiket al
        //public async Task<(decimal? Deger, DateTime? KayitZamani)> GetEtiketDegerAsync(int kumesId, int etiketId)
        //{
        //    var key = $"kumes:{kumesId}:etiketler";

        //    var values = await _db.HashGetAsync(key, new RedisValue[]
        //    {
        //$"{etiketId}:deger",
        //$"{etiketId}:zaman"
        //    });

        //    decimal? deger = null;
        //    DateTime? zaman = null;

        //    if (!values[0].IsNullOrEmpty && decimal.TryParse(values[0].ToString(), out var d))
        //        deger = d;

        //    if (!values[1].IsNullOrEmpty && DateTime.TryParse(values[1].ToString(), out var z))
        //        zaman = z;

        //    return (deger, zaman);
        //}

        // T�m etiketleri al
        //public async Task<Dictionary<int, (decimal? Deger, DateTime? KayitZamani)>> GetAllEtiketlerAsync(int kumesId)
        //{
        //    var key = $"kumes:{kumesId}:etiketler";
        //    var entries = await _db.HashGetAllAsync(key);

        //    var result = new Dictionary<int, (decimal?, DateTime?)>();

        //    var degerler = entries.Where(e => e.Name.ToString().EndsWith(":deger"));

        //    foreach (var entry in degerler)
        //    {
        //        var etiketId = int.Parse(entry.Name.ToString().Replace(":deger", ""));

        //        decimal? deger = decimal.TryParse(entry.Value.ToString(), out var d) ? d : null;

        //        var zamanEntry = entries.FirstOrDefault(e => e.Name == $"{etiketId}:zaman");
        //        DateTime? zaman = DateTime.TryParse(zamanEntry.Value.ToString(), out var z) ? z : null;

        //        result[etiketId] = (deger, zaman);
        //    }

        //    return result;
        //}
        public async Task BulkUpsertEtiketLogYeniDataAsync(
            SqlConnection connection,
            IEnumerable<EtiketLog_TempYeni> data,
            CancellationToken cancellationToken)
        {
            /*
             * EtiketLog_TempYeni anl�k SCADA tablosudur. Her (KumesId, EtiketId)
             * i�in tek g�ncel sat�r tutulur (Kayislar1KumesNodeRedWorker ile ayn� desen).
             * KonveyorId bu worker'da hi� kullan�lmad��� i�in anahtara dahil edilmez.
             */
            var dataList = data
                .GroupBy(x => new { x.KumesId, x.EtiketId })
                .Select(grup => grup
                    .OrderByDescending(x => x.KayitZamani)
                    .First())
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

                _logger.LogError(ex, "EtiketLog_TempYeni bulk upsert i�lemi ba�ar�s�z oldu.");
            }
            finally
            {
                transaction?.Dispose();

                if (connection.State != ConnectionState.Closed)
                    await connection.CloseAsync();
            }
        }

        public async Task BulkInsertAlarmLogDataAsync(SqlConnection connection, IEnumerable<AlarmLog> data)
        {
            try
            {
                var dataTable = new DataTable();
                var accessor = TypeAccessor.Create(typeof(AlarmLog)); // FastMember ile eri�im

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
                    bulkCopy.DestinationTableName = "AlarmLog"; // Verinin eklenece�i tablo ad�
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

        private async Task<IEnumerable<KumesLog_Temp>> GetKumesLogDataForBulkInsertAsync(int kumesId, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;
                KumesIzlemeNodeRed? model;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    var yumurtaBilgileri = GetYumurtaBilgiHelper(kumesId);

                    model = await kumesScadaDbContext.KumesIzlemeNodeRed.AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
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

                                // PLC'den al�nan verileri burada d�nd�r�yoruz
                                return new List<KumesLog_Temp>
                                 {
                                       new KumesLog_Temp { KumesId = kumesId,
                                           DonemId=1,
                                           KayitZamani=DateTime.Now,
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
                                           YumurtaAdet= (int?)yumurtaBilgileri.KumesYumurtaAdet,
                                       },
                                 };
                            }
                            else
                            {
                                return new List<KumesLog_Temp>
                                 {
                                       new KumesLog_Temp { KumesId = kumesId,
                                           DonemId=1,
                                           KayitZamani=DateTime.Now,
                                           YumurtaAdet= (int?)yumurtaBilgileri.KumesYumurtaAdet,
                                       },
                                 };
                            }
                        }
                        else
                        {
                            return new List<KumesLog_Temp>
                                 {
                                       new KumesLog_Temp { KumesId = kumesId,
                                           DonemId=1,
                                           KayitZamani=DateTime.Now,
                                           YumurtaAdet= (int?)yumurtaBilgileri.KumesYumurtaAdet,
                                       },
                                 };
                        }
                    }
                    else
                    {
                        return new List<KumesLog_Temp>
                                 {
                                       new KumesLog_Temp { KumesId = kumesId,
                                           DonemId=1,
                                           KayitZamani=DateTime.Now,
                                           YumurtaAdet= (int?)yumurtaBilgileri.KumesYumurtaAdet,
                                       },
                                 };
                    }
                }
            }
            catch (Exception)
            {
                return new List<KumesLog_Temp>();
            }
        }

        private async Task<IEnumerable<KumesLog_TempYeni>> GetKumesLogYeniDataForBulkInsertAsync(int kumesId, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;
                KumesIzlemeNodeRed? model;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    model = await kumesScadaDbContext.KumesIzlemeNodeRed.AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                    var gunlukHayvanSayisi = await _alarmCacheService.GetGunlukCanliHayvanSayisiByKumesId(kumesId);
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
                                    var lastCounter = await _alarmCacheService.GetDunIcmeSuyuSayacByKumesId(kumesId, kumesNodeRedModel.IcmeSuyu_Sayac.GetValueOrDefault(0));
                                    var currentCounter = kumesNodeRedModel.IcmeSuyu_Sayac.GetValueOrDefault(0);

                                    decimal fark = 0;
                                    fark = currentCounter >= lastCounter ? currentCounter - lastCounter : currentCounter;
                                    icmeSuyuTuketim = fark;
                                }

                                // PLC'den al�nan verileri burada d�nd�r�yoruz
                                return new List<KumesLog_TempYeni>
                                 {
                                       new KumesLog_TempYeni { KumesId = kumesId,
                                           DonemId=1,
                                           KayitZamani=DateTime.Now,
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
                                return new List<KumesLog_TempYeni>
                                {
                                    new KumesLog_TempYeni { KumesId = kumesId,
                                        DonemId=1,
                                        KayitZamani=DateTime.Now,
                                        YumurtaAdet=yumurtaBilgileri==null ? null :yumurtaBilgileri.HicKirliYumurtaAdet,
                                        SAdet=yumurtaBilgileri==null ? null :yumurtaBilgileri.SAdet,
                                        MAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.MAdet,
                                        LAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.LAdet,
                                        XLAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.XLAdet,
                                        AsiriKucukAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.AsiriKucukAdet,
                                        KirikYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.KirikYumurtaAdet,
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
                        }
                        else
                        {
                            return new List<KumesLog_TempYeni>
                            {
                                new KumesLog_TempYeni { KumesId = kumesId,
                                    DonemId=1,
                                    KayitZamani=DateTime.Now,
                                    YumurtaAdet=yumurtaBilgileri==null ? null :yumurtaBilgileri.HicKirliYumurtaAdet,
                                    SAdet=yumurtaBilgileri==null ? null :yumurtaBilgileri.SAdet,
                                    MAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.MAdet,
                                    LAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.LAdet,
                                    XLAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.XLAdet,
                                    AsiriKucukAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.AsiriKucukAdet,
                                    KirikYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.KirikYumurtaAdet,
                                    HicKirliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.HicKirliYumurtaAdet,
                                    AzKirliYumurtaAdet=yumurtaBilgileri==null ? null : yumurtaBilgileri.AzKirliYumurtaAdet,
                                    OrtaKirliYumurtaAdet = yumurtaBilgileri==null ? null :yumurtaBilgileri.OrtaKirliYumurtaAdet,
                                    CokKirliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.CokKirliYumurtaAdet,
                                    HicKanliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.HicKanliYumurtaAdet,
                                    AzKanliYumurtaAdet =yumurtaBilgileri==null ? null :yumurtaBilgileri.AzKanliYumurtaAdet,
                                    OrtaKanliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.OrtaKanliYumurtaAdet,
                                    CokKanliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.CokKanliYumurtaAdet,
                                    HicDiskiliYumurtaAdet = yumurtaBilgileri==null ? null :yumurtaBilgileri.HicDiskiliYumurtaAdet,
                                    AzDiskiliYumurtaAdet = yumurtaBilgileri==null ? null :yumurtaBilgileri.AzDiskiliYumurtaAdet,
                                    OrtaDiskiliYumurtaAdet =yumurtaBilgileri==null ? null : yumurtaBilgileri.OrtaDiskiliYumurtaAdet,
                                    CokDiskiliYumurtaAdet = yumurtaBilgileri==null ? null :yumurtaBilgileri.CokDiskiliYumurtaAdet,
                                },
                            };
                        }
                    }
                    else
                    {
                        if (kumesId == 1)
                        {
                            return new List<KumesLog_TempYeni>
                             {
                                 new KumesLog_TempYeni { KumesId = kumesId,
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
                        else
                        {
                            //return new List<KumesLog_TempYeni>(); 
                            return new List<KumesLog_TempYeni>
                             {  new KumesLog_TempYeni
                                  {
                                     KumesId = kumesId,
                                     DonemId = 1,
                                     KayitZamani = DateTime.Now,
                                     YumurtaAdet = yumurtaBilgileri == null ? null : yumurtaBilgileri.KumesYumurtaAdet,
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
                }
            }
            catch (Exception)
            {
                return new List<KumesLog_TempYeni>();
            }
        }

        //private IEnumerable<KumesLog_Temp> GetKumesLogDataForBulkInsert(_Kumes mainClassRahmiyeKumes)
        //{
        //    try
        //    {
        //        var plcConnected = mainClassRahmiyeKumes.Plc.PlcConnected == true ? 1 : 0;
        //        if (plcConnected == 0) new List<KumesLog_Temp>();

        //        var sicaklik_Kumes = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Sicaklik.Kumes.Value;
        //        var sicaklik_Dis = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Sicaklik.Dis.Value;
        //        var sicaklik_Sensor1 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Sicaklik.Sensor_1.Value;
        //        var sicaklik_Sensor2 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Sicaklik.Sensor_2.Value;
        //        var sicaklik_Sensor3 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Sicaklik.Sensor_3.Value;
        //        var sicaklik_Sensor4 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Sicaklik.Sensor_4.Value;
        //        var sicaklik_Sensor5 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Sicaklik.Sensor_5.Value;
        //        var sicaklik_Sensor6 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Sicaklik.Sensor_6.Value;
        //        var sicaklik_Sensor7 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Sicaklik.Sensor_7.Value;
        //        var sicaklik_Sensor8 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Sicaklik.Sensor_8.Value;
        //        var nem_Kumes = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Nem.Kumes.Value;
        //        var nem_Dis = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Nem.Dis.Value;
        //        var nem_Sensor1 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Nem.Sensor_1.Value;
        //        var nem_Sensor2 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Nem.Sensor_2.Value;
        //        var nem_Sensor3 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Nem.Sensor_3.Value;
        //        var nem_Sensor4 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Nem.Sensor_4.Value;
        //        var nem_Sensor5 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Nem.Sensor_5.Value;
        //        var nem_Sensor6 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Nem.Sensor_6.Value;
        //        var silo1 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Yem.Silo_1.Value;
        //        var silo2 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Yem.Silo_2.Value;
        //        var basinc = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.Basinc.Value;
        //        var co2 = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Iklimlendirme.CO2.Value;
        //        var icmeSuyuTuketim = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.IcmeSuyu.TuketimBugun;
        //        var yemTuketim = mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Yem.TuketimBugun;

        //        // PLC'den al�nan verileri burada d�nd�r�yoruz
        //        return new List<KumesLog_Temp>
        //    {
        //          new KumesLog_Temp { KumesId = (int)mainClassRahmiyeKumes.KumesTanim,
        //              DonemId=1,
        //              KayitZamani=DateTime.Now,
        //              SicaklikKumes=(decimal?)sicaklik_Kumes,
        //              SicaklikDis=(decimal?)sicaklik_Dis,
        //              Sicaklik1=(decimal?)sicaklik_Sensor1,
        //              Sicaklik2=(decimal?)sicaklik_Sensor2,
        //              Sicaklik3=(decimal?)sicaklik_Sensor3,
        //              Sicaklik4=(decimal?)sicaklik_Sensor4,
        //              Sicaklik5=(decimal?)sicaklik_Sensor5,
        //              Sicaklik6=(decimal?)sicaklik_Sensor6,
        //              Sicaklik7=(decimal?)sicaklik_Sensor7,
        //              Sicaklik8=(decimal?)sicaklik_Sensor8,
        //              NemKumes=(decimal?)nem_Kumes,
        //              NemDis=(decimal?)nem_Dis,
        //              Nem1=(decimal?)nem_Sensor1,
        //              Nem2=(decimal?)nem_Sensor2,
        //              Nem3=(decimal?)nem_Sensor3,
        //                                    Nem4=(decimal?)nem_Sensor4,
        //              Nem5=(decimal?)nem_Sensor5,
        //              Nem6=(decimal?)nem_Sensor6,

        //              Silo1=(decimal?)silo1,
        //              Silo2=(decimal?)silo2,
        //              Basinc1=(decimal?)basinc,
        //              Co2_1=(decimal?)co2,
        //              YemTuketimBugun=(decimal?)yemTuketim,
        //              SuTuketimBugun=(decimal?)icmeSuyuTuketim,
        //          },
        //    };
        //    }
        //    catch (Exception)
        //    {
        //        return new List<KumesLog_Temp>();
        //    }
        //}

        private async Task<IEnumerable<EtiketLog_Temp>> GetEtiketLogDataForBulkInsert(int kumesId, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;
                List<EtiketLog_Temp> etiketLogs = new List<EtiketLog_Temp>();
                KumesIzlemeNodeRed? model;
                var gunlukHayvanSayisi = await _alarmCacheService.GetGunlukCanliHayvanSayisiByKumesId(kumesId);
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

                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 106, KayitZamani = now, Deger = (decimal?)yumurtaAdet });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 107, KayitZamani = now, Deger = (decimal?)sAdet });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 108, KayitZamani = now, Deger = (decimal?)mAdet });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 109, KayitZamani = now, Deger = (decimal?)lAdet });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 110, KayitZamani = now, Deger = (decimal?)xlAdet });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 111, KayitZamani = now, Deger = (decimal?)asiriKucukAdet });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 112, KayitZamani = now, Deger = (decimal?)kirik });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 113, KayitZamani = now, Deger = (decimal?)hicKirli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 114, KayitZamani = now, Deger = (decimal?)azKirli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 115, KayitZamani = now, Deger = (decimal?)ortaKirli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 116, KayitZamani = now, Deger = (decimal?)cokKirli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 117, KayitZamani = now, Deger = (decimal?)hicKanli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 118, KayitZamani = now, Deger = (decimal?)azKanli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 119, KayitZamani = now, Deger = (decimal?)ortaKanli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 120, KayitZamani = now, Deger = (decimal?)cokKanli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 121, KayitZamani = now, Deger = (decimal?)hicDiskili });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 122, KayitZamani = now, Deger = (decimal?)azDiskili });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 123, KayitZamani = now, Deger = (decimal?)ortaDiskili });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 124, KayitZamani = now, Deger = (decimal?)cokDiskili });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 125, KayitZamani = now, Deger = (decimal?)hayvanBasiYumurtaAdet });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 126, KayitZamani = now, Deger = (decimal?)hayvanBasiSAdet });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 127, KayitZamani = now, Deger = (decimal?)hayvanBasiMAdet });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 128, KayitZamani = now, Deger = (decimal?)hayvanBasiLAdet });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 129, KayitZamani = now, Deger = (decimal?)hayvanBasiXlAdet });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 130, KayitZamani = now, Deger = (decimal?)hayvanBasiAsiriKucukAdet });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 131, KayitZamani = now, Deger = (decimal?)hayvanBasiKirik });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 132, KayitZamani = now, Deger = (decimal?)hayvanBasiHicKirli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 133, KayitZamani = now, Deger = (decimal?)hayvanBasiAzKirli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 134, KayitZamani = now, Deger = (decimal?)hayvanBasiOrtaKirli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 135, KayitZamani = now, Deger = (decimal?)hayvanBasiCokKirli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 136, KayitZamani = now, Deger = (decimal?)hayvanBasiHicKanli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 137, KayitZamani = now, Deger = (decimal?)hayvanBasiAzKanli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 138, KayitZamani = now, Deger = (decimal?)hayvanBasiOrtaKanli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 139, KayitZamani = now, Deger = (decimal?)hayvanBasiCokKanli });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 140, KayitZamani = now, Deger = (decimal?)hayvanBasiHicDiskili });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 141, KayitZamani = now, Deger = (decimal?)hayvanBasiAzDiskili });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 142, KayitZamani = now, Deger = (decimal?)hayvanBasiOrtaDiskili });
                    etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 143, KayitZamani = now, Deger = (decimal?)hayvanBasiCokDiskili });
                }
                using (var scope = _serviceProvider.CreateScope())
                {
                    var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    model = await kumesScadaDbContext.KumesIzlemeNodeRed.AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                    if (model == null)
                    {
                        var plcRead = 0;
                        var plcConnected = 0;
                        etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                        etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
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
                        //var activeAlarms = await _alarmCacheService.GetAlarmLogsAsync(kumesId/*, DateTime.Now*/);
                        //List<AlarmLog> alarmLogs = new List<AlarmLog>();
                        //if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Plc_Koptu))
                        //{
                        //    if (plcConnected == 0)
                        //    {
                        //        PlcLogYaz(kumesId + " numaral� k�meste plc ba�lant�s� koptu");
                        //        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Plc_Koptu + kumesId, AlarmEnum = (int?)AlarmLogTipler.Plc_Koptu, Mesaj = AlarmLogTipler.Plc_Koptu.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                        //        _alarmCacheService.ClearMemoryCache(kumesId);
                        //    }
                        //}
                        //else
                        //{
                        //    if (plcConnected == 1)
                        //    {
                        //        PlcLogYaz(kumesId + " numaral� k�meste plc ba�lant�s� sa�land�");
                        //        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Plc_Koptu/*, now*/);
                        //    }
                        //}
                        ////if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Plc_Okunmuyor))
                        ////{
                        ////    if (plcRead == 0)
                        ////    {
                        ////        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Plc_Okunmuyor + kumesId, AlarmEnum = (int?)AlarmLogTipler.Plc_Okunmuyor, Mesaj = AlarmLogTipler.Plc_Okunmuyor.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                        ////        _alarmCacheService.ClearMemoryCache(kumesId);
                        ////    }
                        ////}
                        ////else
                        ////{
                        ////    if (plcRead == 1)
                        ////    {
                        ////        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Plc_Okunmuyor/*, now*/);
                        ////    }
                        ////}
                        //await BulkInsertAlarmLogDataAsync(_sqlConnection, alarmLogs);

                        if (PlcVerisiGuncelMi(model, now))
                        {
                            if (model.Veriler != null)
                            {
                                KumesNodeRedModel kumesNodeRedModel = JsonSerializer.Deserialize<KumesNodeRedModel>(model.Veriler);

                                var byteDensity = 0;
                                var pingAverage = 0;
                                var lastPackageLength = 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 71, KayitZamani = DateTime.Now, Deger = (decimal?)byteDensity });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 72, KayitZamani = DateTime.Now, Deger = (decimal?)lastPackageLength });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 73, KayitZamani = DateTime.Now, Deger = (decimal?)pingAverage });

                                if (plcConnected == 0) return etiketLogs;
                                //if (plcRead == 0) return etiketLogs;

                                if (plcConnected == 0) return etiketLogs;

                                var sicaklik_Kumes_Ort = kumesNodeRedModel.Sicaklik_Kumes_Value;
                                var sicaklik_Kumes_Ort_Aktif = kumesNodeRedModel.Sicaklik_Kumes_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 4, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Kumes_Ort });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 12, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Kumes_Ort_Aktif });
                                var sicaklik_Dis = kumesNodeRedModel.Sicaklik_Dis_Value;
                                var sicaklik_Dis_Aktif = kumesNodeRedModel.Sicaklik_Dis_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 5, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Dis });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 13, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Dis_Aktif });
                                var sicaklik_Sensor1 = kumesNodeRedModel.Sicaklik_Sensor_1_Value;
                                var sicaklik_Sensor1_Aktif = kumesNodeRedModel.Sicaklik_Sensor_1_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 6, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor1 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 36, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor1_Aktif });
                                var sicaklik_Sensor2 = kumesNodeRedModel.Sicaklik_Sensor_2_Value;
                                var sicaklik_Sensor2_Aktif = kumesNodeRedModel.Sicaklik_Sensor_2_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 7, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor2 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 37, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor2_Aktif });
                                var sicaklik_Sensor3 = kumesNodeRedModel.Sicaklik_Sensor_3_Value;
                                var sicaklik_Sensor3_Aktif = kumesNodeRedModel.Sicaklik_Sensor_3_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 8, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor3 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 38, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor3_Aktif });
                                var sicaklik_Sensor4 = kumesNodeRedModel.Sicaklik_Sensor_4_Value;
                                var sicaklik_Sensor4_Aktif = kumesNodeRedModel.Sicaklik_Sensor_4_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 9, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor4 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 39, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor4_Aktif });
                                var sicaklik_Sensor5 = kumesNodeRedModel.Sicaklik_Sensor_5_Value;
                                var sicaklik_Sensor5_Aktif = kumesNodeRedModel.Sicaklik_Sensor_5_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 10, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor5 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 40, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor5_Aktif });
                                var sicaklik_Sensor6 = kumesNodeRedModel.Sicaklik_Sensor_6_Value;
                                var sicaklik_Sensor6_Aktif = kumesNodeRedModel.Sicaklik_Sensor_6_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 11, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor6 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 41, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor6_Aktif });
                                var silo1_aktif = kumesNodeRedModel.Yem_Silo_1_Aktif == true ? 1 : 0;
                                var silo1_value = kumesNodeRedModel.Yem_Silo_1_Value;
                                var silo1_varlikoran = kumesNodeRedModel.Yem_Silo_1_VarlikOran;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 27, KayitZamani = DateTime.Now, Deger = (decimal?)silo1_aktif });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 28, KayitZamani = DateTime.Now, Deger = (decimal?)silo1_value });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 29, KayitZamani = DateTime.Now, Deger = (decimal?)silo1_varlikoran });
                                var silo2_aktif = kumesNodeRedModel.Yem_Silo_2_Aktif == true ? 1 : 0; ;
                                var silo2_value = kumesNodeRedModel.Yem_Silo_2_Value;
                                var silo2_varlikoran = kumesNodeRedModel.Yem_Silo_2_VarlikOran;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 30, KayitZamani = DateTime.Now, Deger = (decimal?)silo2_aktif });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 31, KayitZamani = DateTime.Now, Deger = (decimal?)silo2_value });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 32, KayitZamani = DateTime.Now, Deger = (decimal?)silo2_varlikoran });
                                var nem_Dis = kumesNodeRedModel.Nem_Dis_Value;
                                var nem_Dis_Aktif = kumesNodeRedModel.Nem_Dis_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 15, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Dis_Aktif });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 14, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Dis });
                                var nem_Kumes = kumesNodeRedModel.Nem_Kumes_Value;
                                var nem_Kumes_Aktif = kumesNodeRedModel.Nem_Kumes_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 80, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Kumes_Aktif });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 81, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Kumes });
                                var nem_Sensor1 = kumesNodeRedModel.Nem_Sensor_1_Value;
                                var nem_Sensor1_Aktif = kumesNodeRedModel.Nem_Sensor_1_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 46, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor1 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 52, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor1_Aktif });
                                var nem_Sensor2 = kumesNodeRedModel.Nem_Sensor_2_Value;
                                var nem_Sensor2_Aktif = kumesNodeRedModel.Nem_Sensor_2_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 47, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor2 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 53, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor2_Aktif });
                                var nem_Sensor3 = kumesNodeRedModel.Nem_Sensor_3_Value;
                                var nem_Sensor3_Aktif = kumesNodeRedModel.Nem_Sensor_3_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 48, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor3 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 54, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor3_Aktif });
                                var nem_Sensor4 = kumesNodeRedModel.Nem_Sensor_4_Value;
                                var nem_Sensor4_Aktif = kumesNodeRedModel.Nem_Sensor_4_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 49, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor4 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 55, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor4_Aktif });
                                var nem_Sensor5 = kumesNodeRedModel.Nem_Sensor_5_Value;
                                var nem_Sensor5_Aktif = kumesNodeRedModel.Nem_Sensor_5_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 50, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor5 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 56, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor5_Aktif });
                                var nem_Sensor6 = kumesNodeRedModel.Nem_Sensor_6_Value;
                                var nem_Sensor6_Aktif = kumesNodeRedModel.Nem_Sensor_6_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 51, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor6 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 57, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor6_Aktif });
                                var basinc = kumesNodeRedModel.Basinc_Sens_Value;
                                var basinc_aktif = kumesNodeRedModel.Basinc_Sens_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 43, KayitZamani = DateTime.Now, Deger = (decimal?)basinc_aktif });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 42, KayitZamani = DateTime.Now, Deger = (decimal?)basinc });
                                var co2 = kumesNodeRedModel.CO2_Sens_Value;
                                var co2_aktif = kumesNodeRedModel.CO2_Sens_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 45, KayitZamani = DateTime.Now, Deger = (decimal?)co2_aktif });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 44, KayitZamani = DateTime.Now, Deger = (decimal?)co2 });

                                var suVanaAc = kumesNodeRedModel.IcmeSuyu_VanaAc == true ? 1 : 0;
                                var suSayacTuketim = kumesNodeRedModel.IcmeSuyu_Sayac;
                                var suTuketimDun = kumesNodeRedModel.IcmeSuyu_TuketimDun;
                                var suTuketimBugun = kumesNodeRedModel.IcmeSuyu_TuketimBugun;
                                var vanaState = kumesNodeRedModel.IcmeSuyu_VanaState;
                                var vana_Aktif = kumesNodeRedModel.IcmeSuyu_SayacAktif == true ? 1 : 0;
                                var vana_Mode = kumesNodeRedModel.IcmeSuyu_ModeVana;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 74, KayitZamani = DateTime.Now, Deger = (decimal?)suVanaAc });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 75, KayitZamani = DateTime.Now, Deger = (decimal?)suTuketimDun });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 76, KayitZamani = DateTime.Now, Deger = (decimal?)suSayacTuketim });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 16, KayitZamani = DateTime.Now, Deger = (decimal?)suTuketimBugun });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 24, KayitZamani = DateTime.Now, Deger = (decimal?)vana_Mode });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 25, KayitZamani = DateTime.Now, Deger = (decimal?)vana_Aktif });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 26, KayitZamani = DateTime.Now, Deger = (decimal?)vanaState });
                                var yemTuketim = kumesNodeRedModel.Yem_TuketimBugun;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 17, KayitZamani = DateTime.Now, Deger = (decimal?)yemTuketim });
                                var pedPompaRun = kumesNodeRedModel.Ped_PompaRun == true ? 1 : 0;
                                var pedPompaDurum = kumesNodeRedModel.Ped_PompaDurum;
                                var pedPompaMode = kumesNodeRedModel.Ped_ModePompa;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 33, KayitZamani = DateTime.Now, Deger = (decimal?)pedPompaRun });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 34, KayitZamani = DateTime.Now, Deger = (decimal?)pedPompaDurum });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 60, KayitZamani = DateTime.Now, Deger = (decimal?)pedPompaMode });
                                var pedKapakAc = kumesNodeRedModel.Ped_PedKapakAc == true ? 1 : 0;
                                var pedKapakDurum = kumesNodeRedModel.Ped_PedKapakDurum;
                                var pedKapakMode = kumesNodeRedModel.Ped_ModeKapak;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 79, KayitZamani = DateTime.Now, Deger = (decimal?)pedKapakAc });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 35, KayitZamani = DateTime.Now, Deger = (decimal?)pedKapakDurum });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 61, KayitZamani = DateTime.Now, Deger = (decimal?)pedKapakMode });
                                var aydinlatma_Ac = kumesNodeRedModel.Aydinlatma_Ac == true ? 1 : 0;
                                var aydinlatma_Oran = kumesNodeRedModel.Aydinlatma_Oran;
                                var aydinlatma_Durum = kumesNodeRedModel.Aydinlatma_Durum;
                                var aydinlatma_Mode = kumesNodeRedModel.Aydinlatma_Mode;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 77, KayitZamani = DateTime.Now, Deger = (decimal?)aydinlatma_Ac });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 78, KayitZamani = DateTime.Now, Deger = (decimal?)aydinlatma_Oran });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 59, KayitZamani = DateTime.Now, Deger = (decimal?)aydinlatma_Durum });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 58, KayitZamani = DateTime.Now, Deger = (decimal?)aydinlatma_Mode });
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
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 18, KayitZamani = DateTime.Now, Deger = (decimal?)fanMode });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 62, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademeMin });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 63, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe1 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 64, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe2 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 65, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe3 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 66, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe4 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 67, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe5 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 68, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe6 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 69, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe7 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 70, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe8 });
                                var yemlemeState1 = kumesNodeRedModel.Yemleme_1_Durum;
                                var yemlemeState2 = kumesNodeRedModel.Yemleme_2_Durum;
                                var yemlemeState3 = kumesNodeRedModel.Yemleme_3_Durum;
                                var yemlemeState4 = kumesNodeRedModel.Yemleme_4_Durum;
                                var yemlemeState5 = kumesNodeRedModel.Yemleme_5_Durum;
                                var yemlemeState6 = kumesNodeRedModel.Yemleme_6_Durum;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 82, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState1 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 83, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState2 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 84, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState3 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 85, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState4 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 86, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState5 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 87, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState6 });
                                var yemlemeBolumState1 = kumesNodeRedModel.Yemleme_Bolum_1_Durum;
                                var yemlemeBolumState2 = kumesNodeRedModel.Yemleme_Bolum_2_Durum;
                                var yemlemeBolumState3 = kumesNodeRedModel.Yemleme_Bolum_3_Durum;
                                var yemlemeBolumState4 = kumesNodeRedModel.Yemleme_Bolum_4_Durum;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 88, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumState1 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 89, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumState2 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 90, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumState3 });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 91, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumState4 });
                                var yemlemeBaslamaZamani = TimeSpan.FromMilliseconds((double)kumesNodeRedModel.Yemleme_BaslamaZamani).TotalSeconds;
                                var yemlemeBolumAktuelSure = TimeSpan.FromMilliseconds((double)kumesNodeRedModel.Yemleme_BolumAktuelSure).TotalSeconds /*mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Yemleme.BolumAktuelSure.TotalSeconds*/;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 92, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBaslamaZamani });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 93, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumAktuelSure });
                                var buffelSol_CmdBit = kumesNodeRedModel.Buffel_Sol_Cmd_CmdBit;
                                var buffelSol_RprMotorState = kumesNodeRedModel.Buffel_Sol_Rpr_MotorState;
                                var buffelSol_RprDurum = kumesNodeRedModel.Buffel_Sol_Rpr_Durum;
                                var buffelSol_RprSwitchKapali = kumesNodeRedModel.Buffel_Sol_Rpr_Switch_Kapali == true ? 1 : 0;
                                var buffelSol_RprSwitchAcik = kumesNodeRedModel.Buffel_Sol_Rpr_Switch_Acik == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 94, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_CmdBit });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 95, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_RprMotorState });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 96, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_RprDurum });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 97, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_RprSwitchKapali });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 98, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_RprSwitchAcik });
                                var buffelSag_CmdBit = kumesNodeRedModel.Buffel_Sag_Cmd_CmdBit;
                                var buffelSag_RprMotorState = kumesNodeRedModel.Buffel_Sag_Rpr_MotorState;
                                var buffelSag_RprDurum = kumesNodeRedModel.Buffel_Sag_Rpr_Durum;
                                var buffelSag_RprSwitchKapali = kumesNodeRedModel.Buffel_Sag_Rpr_Switch_Kapali == true ? 1 : 0;
                                var buffelSag_RprSwitchAcik = kumesNodeRedModel.Buffel_Sag_Rpr_Switch_Acik == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 99, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_CmdBit });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 100, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_RprMotorState });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 101, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_RprDurum });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 102, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_RprSwitchKapali });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 103, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_RprSwitchAcik });

                                var hayvanBasinaSuTuketim = gunlukHayvanSayisi == 0 ? 0 : (kumesNodeRedModel.IcmeSuyu_TuketimBugun * 1000000) / gunlukHayvanSayisi;
                                var hayvanBasinaYemTuketim = gunlukHayvanSayisi == 0 ? 0 : (kumesNodeRedModel.Yem_TuketimBugun * 1000000) / gunlukHayvanSayisi;
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 104, KayitZamani = DateTime.Now, Deger = (decimal?)hayvanBasinaSuTuketim });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 105, KayitZamani = DateTime.Now, Deger = (decimal?)hayvanBasinaYemTuketim });
                            }
                            else
                            {
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                                etiketLogs.Add(new EtiketLog_Temp { KumesId = kumesId, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                            }
                        }
                    }
                    return etiketLogs;
                }
            }
            catch (Exception)
            {
                return new List<EtiketLog_Temp>();
            }
        }

        private async Task<IEnumerable<EtiketLog_TempYeni>> GetEtiketLogYeniDataForBulkInsert(int kumesId, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;
                List<EtiketLog_TempYeni> etiketLogs = new List<EtiketLog_TempYeni>();
                KumesIzlemeNodeRed? model;
                var gunlukHayvanSayisi = await _alarmCacheService.GetGunlukCanliHayvanSayisiByKumesId(kumesId);
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

                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 106, KayitZamani = now, Deger = (decimal?)yumurtaAdet });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 107, KayitZamani = now, Deger = (decimal?)sAdet });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 108, KayitZamani = now, Deger = (decimal?)mAdet });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 109, KayitZamani = now, Deger = (decimal?)lAdet });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 110, KayitZamani = now, Deger = (decimal?)xlAdet });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 111, KayitZamani = now, Deger = (decimal?)asiriKucukAdet });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 112, KayitZamani = now, Deger = (decimal?)kirik });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 113, KayitZamani = now, Deger = (decimal?)hicKirli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 114, KayitZamani = now, Deger = (decimal?)azKirli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 115, KayitZamani = now, Deger = (decimal?)ortaKirli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 116, KayitZamani = now, Deger = (decimal?)cokKirli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 117, KayitZamani = now, Deger = (decimal?)hicKanli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 118, KayitZamani = now, Deger = (decimal?)azKanli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 119, KayitZamani = now, Deger = (decimal?)ortaKanli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 120, KayitZamani = now, Deger = (decimal?)cokKanli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 121, KayitZamani = now, Deger = (decimal?)hicDiskili });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 122, KayitZamani = now, Deger = (decimal?)azDiskili });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 123, KayitZamani = now, Deger = (decimal?)ortaDiskili });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 124, KayitZamani = now, Deger = (decimal?)cokDiskili });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 125, KayitZamani = now, Deger = (decimal?)hayvanBasiYumurtaAdet });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 126, KayitZamani = now, Deger = (decimal?)hayvanBasiSAdet });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 127, KayitZamani = now, Deger = (decimal?)hayvanBasiMAdet });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 128, KayitZamani = now, Deger = (decimal?)hayvanBasiLAdet });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 129, KayitZamani = now, Deger = (decimal?)hayvanBasiXlAdet });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 130, KayitZamani = now, Deger = (decimal?)hayvanBasiAsiriKucukAdet });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 131, KayitZamani = now, Deger = (decimal?)hayvanBasiKirik });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 132, KayitZamani = now, Deger = (decimal?)hayvanBasiHicKirli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 133, KayitZamani = now, Deger = (decimal?)hayvanBasiAzKirli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 134, KayitZamani = now, Deger = (decimal?)hayvanBasiOrtaKirli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 135, KayitZamani = now, Deger = (decimal?)hayvanBasiCokKirli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 136, KayitZamani = now, Deger = (decimal?)hayvanBasiHicKanli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 137, KayitZamani = now, Deger = (decimal?)hayvanBasiAzKanli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 138, KayitZamani = now, Deger = (decimal?)hayvanBasiOrtaKanli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 139, KayitZamani = now, Deger = (decimal?)hayvanBasiCokKanli });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 140, KayitZamani = now, Deger = (decimal?)hayvanBasiHicDiskili });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 141, KayitZamani = now, Deger = (decimal?)hayvanBasiAzDiskili });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 142, KayitZamani = now, Deger = (decimal?)hayvanBasiOrtaDiskili });
                    etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 143, KayitZamani = now, Deger = (decimal?)hayvanBasiCokDiskili });
                }
                using (var scope = _serviceProvider.CreateScope())
                {
                    var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    model = await kumesScadaDbContext.KumesIzlemeNodeRed.AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                    if (model == null)
                    {
                        var plcRead = 0;
                        var plcConnected = 0;
                        etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                        etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
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
                        var activeAlarms = await _alarmCacheService.GetAlarmLogsAsync(kumesId/*, DateTime.Now*/);
                        List<AlarmLog> alarmLogs = new List<AlarmLog>();
                        if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Plc_Koptu))
                        {
                            if (plcConnected == 0)
                            {
                                PlcLogYaz(kumesId + " numaral� k�meste plc ba�lant�s� koptu");
                                alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Plc_Koptu + kumesId, AlarmEnum = (int?)AlarmLogTipler.Plc_Koptu, Mesaj = AlarmLogTipler.Plc_Koptu.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                _alarmCacheService.ClearMemoryCache(kumesId);
                            }
                        }
                        else
                        {
                            if (plcConnected == 1)
                            {
                                PlcLogYaz(kumesId + " numaral� k�meste plc ba�lant�s� sa�land�");
                                await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Plc_Koptu/*, now*/);
                            }
                        }
                        //if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Plc_Okunmuyor))
                        //{
                        //    if (plcRead == 0)
                        //    {
                        //        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Plc_Okunmuyor + kumesId, AlarmEnum = (int?)AlarmLogTipler.Plc_Okunmuyor, Mesaj = AlarmLogTipler.Plc_Okunmuyor.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                        //        _alarmCacheService.ClearMemoryCache(kumesId);
                        //    }
                        //}
                        //else
                        //{
                        //    if (plcRead == 1)
                        //    {
                        //        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Plc_Okunmuyor/*, now*/);
                        //    }
                        //}
                        await BulkInsertAlarmLogDataAsync(_sqlConnection, alarmLogs);

                        if (PlcVerisiGuncelMi(model, now))
                        {
                            if (model.Veriler != null)
                            {
                                KumesNodeRedModel kumesNodeRedModel = JsonSerializer.Deserialize<KumesNodeRedModel>(model.Veriler);

                                var byteDensity = 0;
                                var pingAverage = 0;
                                var lastPackageLength = 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 2, KayitZamani = DateTime.Now, Deger = (decimal?)plcConnected });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 3, KayitZamani = DateTime.Now, Deger = (decimal?)plcRead });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 71, KayitZamani = DateTime.Now, Deger = (decimal?)byteDensity });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 72, KayitZamani = DateTime.Now, Deger = (decimal?)lastPackageLength });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 73, KayitZamani = DateTime.Now, Deger = (decimal?)pingAverage });

                                if (plcConnected == 0) return etiketLogs;
                                //if (plcRead == 0) return etiketLogs;


                                if (plcConnected == 0) return etiketLogs;

                                var sicaklik_Kumes_Ort = kumesNodeRedModel.Sicaklik_Kumes_Value;
                                var sicaklik_Kumes_Ort_Aktif = kumesNodeRedModel.Sicaklik_Kumes_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 4, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Kumes_Ort });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 12, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Kumes_Ort_Aktif });
                                var sicaklik_Dis = kumesNodeRedModel.Sicaklik_Dis_Value;
                                var sicaklik_Dis_Aktif = kumesNodeRedModel.Sicaklik_Dis_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 5, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Dis });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 13, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Dis_Aktif });
                                var sicaklik_Sensor1 = kumesNodeRedModel.Sicaklik_Sensor_1_Value;
                                var sicaklik_Sensor1_Aktif = kumesNodeRedModel.Sicaklik_Sensor_1_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 6, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor1 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 36, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor1_Aktif });
                                var sicaklik_Sensor2 = kumesNodeRedModel.Sicaklik_Sensor_2_Value;
                                var sicaklik_Sensor2_Aktif = kumesNodeRedModel.Sicaklik_Sensor_2_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 7, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor2 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 37, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor2_Aktif });
                                var sicaklik_Sensor3 = kumesNodeRedModel.Sicaklik_Sensor_3_Value;
                                var sicaklik_Sensor3_Aktif = kumesNodeRedModel.Sicaklik_Sensor_3_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 8, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor3 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 38, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor3_Aktif });
                                var sicaklik_Sensor4 = kumesNodeRedModel.Sicaklik_Sensor_4_Value;
                                var sicaklik_Sensor4_Aktif = kumesNodeRedModel.Sicaklik_Sensor_4_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 9, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor4 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 39, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor4_Aktif });
                                var sicaklik_Sensor5 = kumesNodeRedModel.Sicaklik_Sensor_5_Value;
                                var sicaklik_Sensor5_Aktif = kumesNodeRedModel.Sicaklik_Sensor_5_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 10, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor5 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 40, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor5_Aktif });
                                var sicaklik_Sensor6 = kumesNodeRedModel.Sicaklik_Sensor_6_Value;
                                var sicaklik_Sensor6_Aktif = kumesNodeRedModel.Sicaklik_Sensor_6_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 11, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor6 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 41, KayitZamani = DateTime.Now, Deger = (decimal?)sicaklik_Sensor6_Aktif });
                                var silo1_aktif = kumesNodeRedModel.Yem_Silo_1_Aktif == true ? 1 : 0;
                                var silo1_value = kumesNodeRedModel.Yem_Silo_1_Value;
                                var silo1_varlikoran = kumesNodeRedModel.Yem_Silo_1_VarlikOran;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 27, KayitZamani = DateTime.Now, Deger = (decimal?)silo1_aktif });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 28, KayitZamani = DateTime.Now, Deger = (decimal?)silo1_value });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 29, KayitZamani = DateTime.Now, Deger = (decimal?)silo1_varlikoran });
                                var silo2_aktif = kumesNodeRedModel.Yem_Silo_2_Aktif == true ? 1 : 0; ;
                                var silo2_value = kumesNodeRedModel.Yem_Silo_2_Value;
                                var silo2_varlikoran = kumesNodeRedModel.Yem_Silo_2_VarlikOran;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 30, KayitZamani = DateTime.Now, Deger = (decimal?)silo2_aktif });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 31, KayitZamani = DateTime.Now, Deger = (decimal?)silo2_value });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 32, KayitZamani = DateTime.Now, Deger = (decimal?)silo2_varlikoran });
                                var nem_Dis = kumesNodeRedModel.Nem_Dis_Value;
                                var nem_Dis_Aktif = kumesNodeRedModel.Nem_Dis_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 15, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Dis_Aktif });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 14, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Dis });
                                var nem_Kumes = kumesNodeRedModel.Nem_Kumes_Value;
                                var nem_Kumes_Aktif = kumesNodeRedModel.Nem_Kumes_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 80, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Kumes_Aktif });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 81, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Kumes });
                                var nem_Sensor1 = kumesNodeRedModel.Nem_Sensor_1_Value;
                                var nem_Sensor1_Aktif = kumesNodeRedModel.Nem_Sensor_1_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 46, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor1 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 52, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor1_Aktif });
                                var nem_Sensor2 = kumesNodeRedModel.Nem_Sensor_2_Value;
                                var nem_Sensor2_Aktif = kumesNodeRedModel.Nem_Sensor_2_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 47, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor2 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 53, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor2_Aktif });
                                var nem_Sensor3 = kumesNodeRedModel.Nem_Sensor_3_Value;
                                var nem_Sensor3_Aktif = kumesNodeRedModel.Nem_Sensor_3_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 48, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor3 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 54, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor3_Aktif });
                                var nem_Sensor4 = kumesNodeRedModel.Nem_Sensor_4_Value;
                                var nem_Sensor4_Aktif = kumesNodeRedModel.Nem_Sensor_4_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 49, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor4 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 55, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor4_Aktif });
                                var nem_Sensor5 = kumesNodeRedModel.Nem_Sensor_5_Value;
                                var nem_Sensor5_Aktif = kumesNodeRedModel.Nem_Sensor_5_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 50, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor5 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 56, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor5_Aktif });
                                var nem_Sensor6 = kumesNodeRedModel.Nem_Sensor_6_Value;
                                var nem_Sensor6_Aktif = kumesNodeRedModel.Nem_Sensor_6_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 51, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor6 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 57, KayitZamani = DateTime.Now, Deger = (decimal?)nem_Sensor6_Aktif });
                                var basinc = kumesNodeRedModel.Basinc_Sens_Value;
                                var basinc_aktif = kumesNodeRedModel.Basinc_Sens_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 43, KayitZamani = DateTime.Now, Deger = (decimal?)basinc_aktif });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 42, KayitZamani = DateTime.Now, Deger = (decimal?)basinc });
                                var co2 = kumesNodeRedModel.CO2_Sens_Value;
                                var co2_aktif = kumesNodeRedModel.CO2_Sens_Aktif == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 45, KayitZamani = DateTime.Now, Deger = (decimal?)co2_aktif });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 44, KayitZamani = DateTime.Now, Deger = (decimal?)co2 });

                                var suVanaAc = kumesNodeRedModel.IcmeSuyu_VanaAc == true ? 1 : 0;
                                var suSayacTuketim = kumesNodeRedModel.IcmeSuyu_Sayac;
                                var suTuketimDun = kumesNodeRedModel.IcmeSuyu_TuketimDun;
                                var suTuketimBugun = kumesNodeRedModel.IcmeSuyu_TuketimBugun;
                                var vanaState = kumesNodeRedModel.IcmeSuyu_VanaState;
                                var vana_Aktif = kumesNodeRedModel.IcmeSuyu_SayacAktif == true ? 1 : 0;
                                var vana_Mode = kumesNodeRedModel.IcmeSuyu_ModeVana;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 74, KayitZamani = DateTime.Now, Deger = (decimal?)suVanaAc });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 75, KayitZamani = DateTime.Now, Deger = (decimal?)suTuketimDun });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 76, KayitZamani = DateTime.Now, Deger = (decimal?)suSayacTuketim });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 16, KayitZamani = DateTime.Now, Deger = (decimal?)suTuketimBugun });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 24, KayitZamani = DateTime.Now, Deger = (decimal?)vana_Mode });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 25, KayitZamani = DateTime.Now, Deger = (decimal?)vana_Aktif });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 26, KayitZamani = DateTime.Now, Deger = (decimal?)vanaState });
                                var yemTuketim = kumesNodeRedModel.Yem_TuketimBugun;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 17, KayitZamani = DateTime.Now, Deger = (decimal?)yemTuketim });
                                var pedPompaRun = kumesNodeRedModel.Ped_PompaRun == true ? 1 : 0;
                                var pedPompaDurum = kumesNodeRedModel.Ped_PompaDurum;
                                var pedPompaMode = kumesNodeRedModel.Ped_ModePompa;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 33, KayitZamani = DateTime.Now, Deger = (decimal?)pedPompaRun });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 34, KayitZamani = DateTime.Now, Deger = (decimal?)pedPompaDurum });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 60, KayitZamani = DateTime.Now, Deger = (decimal?)pedPompaMode });
                                var pedKapakAc = kumesNodeRedModel.Ped_PedKapakAc == true ? 1 : 0;
                                var pedKapakDurum = kumesNodeRedModel.Ped_PedKapakDurum;
                                var pedKapakMode = kumesNodeRedModel.Ped_ModeKapak;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 79, KayitZamani = DateTime.Now, Deger = (decimal?)pedKapakAc });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 35, KayitZamani = DateTime.Now, Deger = (decimal?)pedKapakDurum });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 61, KayitZamani = DateTime.Now, Deger = (decimal?)pedKapakMode });
                                var aydinlatma_Ac = kumesNodeRedModel.Aydinlatma_Ac == true ? 1 : 0;
                                var aydinlatma_Oran = kumesNodeRedModel.Aydinlatma_Oran;
                                var aydinlatma_Durum = kumesNodeRedModel.Aydinlatma_Durum;
                                var aydinlatma_Mode = kumesNodeRedModel.Aydinlatma_Mode;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 77, KayitZamani = DateTime.Now, Deger = (decimal?)aydinlatma_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 78, KayitZamani = DateTime.Now, Deger = (decimal?)aydinlatma_Oran });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 59, KayitZamani = DateTime.Now, Deger = (decimal?)aydinlatma_Durum });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 58, KayitZamani = DateTime.Now, Deger = (decimal?)aydinlatma_Mode });
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
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 18, KayitZamani = DateTime.Now, Deger = (decimal?)fanMode });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 62, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademeMin });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 63, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe1 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 64, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe2 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 65, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe3 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 66, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe4 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 67, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe5 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 68, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe6 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 69, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe7 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 70, KayitZamani = DateTime.Now, Deger = (decimal?)fanKademe8 });
                                var yemlemeState1 = kumesNodeRedModel.Yemleme_1_Durum;
                                var yemlemeState2 = kumesNodeRedModel.Yemleme_2_Durum;
                                var yemlemeState3 = kumesNodeRedModel.Yemleme_3_Durum;
                                var yemlemeState4 = kumesNodeRedModel.Yemleme_4_Durum;
                                var yemlemeState5 = kumesNodeRedModel.Yemleme_5_Durum;
                                var yemlemeState6 = kumesNodeRedModel.Yemleme_6_Durum;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 82, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState1 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 83, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState2 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 84, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState3 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 85, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState4 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 86, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState5 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 87, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeState6 });
                                var yemlemeBolumState1 = kumesNodeRedModel.Yemleme_Bolum_1_Durum;
                                var yemlemeBolumState2 = kumesNodeRedModel.Yemleme_Bolum_2_Durum;
                                var yemlemeBolumState3 = kumesNodeRedModel.Yemleme_Bolum_3_Durum;
                                var yemlemeBolumState4 = kumesNodeRedModel.Yemleme_Bolum_4_Durum;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 88, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumState1 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 89, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumState2 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 90, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumState3 });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 91, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumState4 });
                                var yemlemeBaslamaZamani = TimeSpan.FromMilliseconds((double)kumesNodeRedModel.Yemleme_BaslamaZamani).TotalSeconds;
                                var yemlemeBolumAktuelSure = TimeSpan.FromMilliseconds((double)kumesNodeRedModel.Yemleme_BolumAktuelSure).TotalSeconds /*mainClassRahmiyeKumes.Plc.Memory.ToScadaDB.KumesReport.Yemleme.BolumAktuelSure.TotalSeconds*/;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 92, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBaslamaZamani });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 93, KayitZamani = DateTime.Now, Deger = (decimal?)yemlemeBolumAktuelSure });
                                var buffelSol_CmdBit = kumesNodeRedModel.Buffel_Sol_Cmd_CmdBit;
                                var buffelSol_RprMotorState = kumesNodeRedModel.Buffel_Sol_Rpr_MotorState;
                                var buffelSol_RprDurum = kumesNodeRedModel.Buffel_Sol_Rpr_Durum;
                                var buffelSol_RprSwitchKapali = kumesNodeRedModel.Buffel_Sol_Rpr_Switch_Kapali == true ? 1 : 0;
                                var buffelSol_RprSwitchAcik = kumesNodeRedModel.Buffel_Sol_Rpr_Switch_Acik == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 94, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_CmdBit });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 95, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_RprMotorState });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 96, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_RprDurum });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 97, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_RprSwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 98, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSol_RprSwitchAcik });
                                var buffelSag_CmdBit = kumesNodeRedModel.Buffel_Sag_Cmd_CmdBit;
                                var buffelSag_RprMotorState = kumesNodeRedModel.Buffel_Sag_Rpr_MotorState;
                                var buffelSag_RprDurum = kumesNodeRedModel.Buffel_Sag_Rpr_Durum;
                                var buffelSag_RprSwitchKapali = kumesNodeRedModel.Buffel_Sag_Rpr_Switch_Kapali == true ? 1 : 0;
                                var buffelSag_RprSwitchAcik = kumesNodeRedModel.Buffel_Sag_Rpr_Switch_Acik == true ? 1 : 0;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 99, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_CmdBit });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 100, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_RprMotorState });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 101, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_RprDurum });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 102, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_RprSwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 103, KayitZamani = DateTime.Now, Deger = (decimal?)buffelSag_RprSwitchAcik });

                                var Buffel_2_Sol_State_Auto = kumesNodeRedModel.Buffel_2_Sol_State_Auto == true ? 1 : 0; ;
                                var Buffel_2_Sol_State_Ac_Kontaktor = kumesNodeRedModel.Buffel_2_Sol_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var Buffel_2_Sol_State_Kapat_Kontaktor = kumesNodeRedModel.Buffel_2_Sol_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var Buffel_2_Sol_State_SwitchAcik = kumesNodeRedModel.Buffel_2_Sol_State_SwitchAcik == true ? 1 : 0;
                                var Buffel_2_Sol_State_SwitchKapali = kumesNodeRedModel.Buffel_2_Sol_State_SwitchKapali == true ? 1 : 0;
                                var Buffel_2_Sol_State_Durum = kumesNodeRedModel.Buffel_2_Sol_State_Durum;
                                var Buffel_2_Sol_Cmd_Ac = kumesNodeRedModel.Buffel_2_Sol_Cmd_Ac == true ? 1 : 0;
                                var Buffel_2_Sol_Cmd_Kapat = kumesNodeRedModel.Buffel_2_Sol_Cmd_Kapat == true ? 1 : 0;
                                var Buffel_2_Sol_Cmd_CmdBit = kumesNodeRedModel.Buffel_2_Sol_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 569, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_State_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 570, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 571, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 572, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 573, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 574, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 575, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 576, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 577, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sol_Cmd_CmdBit.GetValueOrDefault(0) });

                                var Buffel_2_Sag_State_Auto = kumesNodeRedModel.Buffel_2_Sag_State_Auto == true ? 1 : 0; ;
                                var Buffel_2_Sag_State_Ac_Kontaktor = kumesNodeRedModel.Buffel_2_Sag_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var Buffel_2_Sag_State_Kapat_Kontaktor = kumesNodeRedModel.Buffel_2_Sag_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var Buffel_2_Sag_State_SwitchAcik = kumesNodeRedModel.Buffel_2_Sag_State_SwitchAcik == true ? 1 : 0;
                                var Buffel_2_Sag_State_SwitchKapali = kumesNodeRedModel.Buffel_2_Sag_State_SwitchKapali == true ? 1 : 0;
                                var Buffel_2_Sag_State_Durum = kumesNodeRedModel.Buffel_2_Sag_State_Durum;
                                var Buffel_2_Sag_Cmd_Ac = kumesNodeRedModel.Buffel_2_Sag_Cmd_Ac == true ? 1 : 0;
                                var Buffel_2_Sag_Cmd_Kapat = kumesNodeRedModel.Buffel_2_Sag_Cmd_Kapat == true ? 1 : 0;
                                var Buffel_2_Sag_Cmd_CmdBit = kumesNodeRedModel.Buffel_2_Sag_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 578, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_State_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 579, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 580, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 581, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 582, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 583, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 584, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 585, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 586, KayitZamani = DateTime.Now, Deger = (decimal?)Buffel_2_Sag_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_1_State_Auto = kumesNodeRedModel.PedKapak_On_1_State_Auto == true ? 1 : 0; ;
                                var PedKapak_On_1_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_On_1_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_1_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_On_1_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_1_State_SwitchAcik = kumesNodeRedModel.PedKapak_On_1_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_On_1_State_SwitchKapali = kumesNodeRedModel.PedKapak_On_1_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_On_1_State_Durum = kumesNodeRedModel.PedKapak_On_1_State_Durum;
                                var PedKapak_On_1_Cmd_Ac = kumesNodeRedModel.PedKapak_On_1_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_On_1_Cmd_Kapat = kumesNodeRedModel.PedKapak_On_1_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_On_1_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_1_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 587, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_State_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 588, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 589, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 590, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 591, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 592, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 593, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 594, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 595, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_1_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_2_State_Auto = kumesNodeRedModel.PedKapak_On_2_State_Auto == true ? 1 : 0; ;
                                var PedKapak_On_2_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_On_2_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_2_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_On_2_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_2_State_SwitchAcik = kumesNodeRedModel.PedKapak_On_2_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_On_2_State_SwitchKapali = kumesNodeRedModel.PedKapak_On_2_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_On_2_State_Durum = kumesNodeRedModel.PedKapak_On_2_State_Durum;
                                var PedKapak_On_2_Cmd_Ac = kumesNodeRedModel.PedKapak_On_2_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_On_2_Cmd_Kapat = kumesNodeRedModel.PedKapak_On_2_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_On_2_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_2_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 596, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_State_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 597, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 598, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 599, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 600, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 601, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 602, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 603, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 604, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_2_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_3_State_Auto = kumesNodeRedModel.PedKapak_On_3_State_Auto == true ? 1 : 0; ;
                                var PedKapak_On_3_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_On_3_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_3_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_On_3_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_3_State_SwitchAcik = kumesNodeRedModel.PedKapak_On_3_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_On_3_State_SwitchKapali = kumesNodeRedModel.PedKapak_On_3_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_On_3_State_Durum = kumesNodeRedModel.PedKapak_On_3_State_Durum;
                                var PedKapak_On_3_Cmd_Ac = kumesNodeRedModel.PedKapak_On_3_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_On_3_Cmd_Kapat = kumesNodeRedModel.PedKapak_On_3_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_On_3_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_3_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 605, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_State_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 606, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 607, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 608, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 609, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 610, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 611, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 612, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 613, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_3_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_On_4_State_Auto = kumesNodeRedModel.PedKapak_On_4_State_Auto == true ? 1 : 0; ;
                                var PedKapak_On_4_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_On_4_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_4_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_On_4_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_On_4_State_SwitchAcik = kumesNodeRedModel.PedKapak_On_4_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_On_4_State_SwitchKapali = kumesNodeRedModel.PedKapak_On_4_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_On_4_State_Durum = kumesNodeRedModel.PedKapak_On_4_State_Durum;
                                var PedKapak_On_4_Cmd_Ac = kumesNodeRedModel.PedKapak_On_4_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_On_4_Cmd_Kapat = kumesNodeRedModel.PedKapak_On_4_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_On_4_Cmd_CmdBit = kumesNodeRedModel.PedKapak_On_4_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 614, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_State_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 615, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 616, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 617, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 618, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 619, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 620, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 621, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 622, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_On_4_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_1_State_Auto = kumesNodeRedModel.PedKapak_Arka_1_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Arka_1_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Arka_1_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_1_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Arka_1_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_1_State_SwitchAcik = kumesNodeRedModel.PedKapak_Arka_1_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Arka_1_State_SwitchKapali = kumesNodeRedModel.PedKapak_Arka_1_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Arka_1_State_Durum = kumesNodeRedModel.PedKapak_Arka_1_State_Durum;
                                var PedKapak_Arka_1_Cmd_Ac = kumesNodeRedModel.PedKapak_Arka_1_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Arka_1_Cmd_Kapat = kumesNodeRedModel.PedKapak_Arka_1_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Arka_1_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_1_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 623, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_State_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 624, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 625, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 626, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 627, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 628, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 629, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 630, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 631, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_1_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_2_State_Auto = kumesNodeRedModel.PedKapak_Arka_2_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Arka_2_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Arka_2_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_2_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Arka_2_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_2_State_SwitchAcik = kumesNodeRedModel.PedKapak_Arka_2_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Arka_2_State_SwitchKapali = kumesNodeRedModel.PedKapak_Arka_2_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Arka_2_State_Durum = kumesNodeRedModel.PedKapak_Arka_2_State_Durum;
                                var PedKapak_Arka_2_Cmd_Ac = kumesNodeRedModel.PedKapak_Arka_2_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Arka_2_Cmd_Kapat = kumesNodeRedModel.PedKapak_Arka_2_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Arka_2_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_2_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 632, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_State_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 633, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 634, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 635, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 636, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 637, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 638, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 639, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 640, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_2_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_3_State_Auto = kumesNodeRedModel.PedKapak_Arka_3_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Arka_3_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Arka_3_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_3_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Arka_3_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_3_State_SwitchAcik = kumesNodeRedModel.PedKapak_Arka_3_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Arka_3_State_SwitchKapali = kumesNodeRedModel.PedKapak_Arka_3_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Arka_3_State_Durum = kumesNodeRedModel.PedKapak_Arka_3_State_Durum;
                                var PedKapak_Arka_3_Cmd_Ac = kumesNodeRedModel.PedKapak_Arka_3_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Arka_3_Cmd_Kapat = kumesNodeRedModel.PedKapak_Arka_3_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Arka_3_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_3_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 641, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_State_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 642, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 643, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 644, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 645, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 646, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 647, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 648, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 649, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_3_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Arka_4_State_Auto = kumesNodeRedModel.PedKapak_Arka_4_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Arka_4_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Arka_4_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_4_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Arka_4_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Arka_4_State_SwitchAcik = kumesNodeRedModel.PedKapak_Arka_4_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Arka_4_State_SwitchKapali = kumesNodeRedModel.PedKapak_Arka_4_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Arka_4_State_Durum = kumesNodeRedModel.PedKapak_Arka_4_State_Durum;
                                var PedKapak_Arka_4_Cmd_Ac = kumesNodeRedModel.PedKapak_Arka_4_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Arka_4_Cmd_Kapat = kumesNodeRedModel.PedKapak_Arka_4_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Arka_4_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Arka_4_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 650, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_State_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 651, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 652, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 653, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 654, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 655, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 656, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 657, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 658, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Arka_4_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Sol_1_State_Auto = kumesNodeRedModel.PedKapak_Sol_1_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Sol_1_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Sol_1_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sol_1_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Sol_1_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sol_1_State_SwitchAcik = kumesNodeRedModel.PedKapak_Sol_1_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Sol_1_State_SwitchKapali = kumesNodeRedModel.PedKapak_Sol_1_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Sol_1_State_Durum = kumesNodeRedModel.PedKapak_Sol_1_State_Durum;
                                var PedKapak_Sol_1_Cmd_Ac = kumesNodeRedModel.PedKapak_Sol_1_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Sol_1_Cmd_Kapat = kumesNodeRedModel.PedKapak_Sol_1_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Sol_1_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Sol_1_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 659, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_State_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 660, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 661, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 662, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 663, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 664, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 665, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 666, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 667, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_1_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Sol_2_State_Auto = kumesNodeRedModel.PedKapak_Sol_2_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Sol_2_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Sol_2_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sol_2_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Sol_2_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sol_2_State_SwitchAcik = kumesNodeRedModel.PedKapak_Sol_2_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Sol_2_State_SwitchKapali = kumesNodeRedModel.PedKapak_Sol_2_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Sol_2_State_Durum = kumesNodeRedModel.PedKapak_Sol_2_State_Durum;
                                var PedKapak_Sol_2_Cmd_Ac = kumesNodeRedModel.PedKapak_Sol_2_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Sol_2_Cmd_Kapat = kumesNodeRedModel.PedKapak_Sol_2_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Sol_2_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Sol_2_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 668, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_State_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 669, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 670, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 671, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 672, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 673, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 674, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 675, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 676, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sol_2_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Sag_1_State_Auto = kumesNodeRedModel.PedKapak_Sag_1_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Sag_1_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Sag_1_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sag_1_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Sag_1_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sag_1_State_SwitchAcik = kumesNodeRedModel.PedKapak_Sag_1_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Sag_1_State_SwitchKapali = kumesNodeRedModel.PedKapak_Sag_1_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Sag_1_State_Durum = kumesNodeRedModel.PedKapak_Sag_1_State_Durum;
                                var PedKapak_Sag_1_Cmd_Ac = kumesNodeRedModel.PedKapak_Sag_1_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Sag_1_Cmd_Kapat = kumesNodeRedModel.PedKapak_Sag_1_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Sag_1_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Sag_1_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 677, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_State_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 678, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 679, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 680, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 681, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 682, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 683, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 684, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 685, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_1_Cmd_CmdBit.GetValueOrDefault(0) });

                                var PedKapak_Sag_2_State_Auto = kumesNodeRedModel.PedKapak_Sag_2_State_Auto == true ? 1 : 0; ;
                                var PedKapak_Sag_2_State_Ac_Kontaktor = kumesNodeRedModel.PedKapak_Sag_2_State_Ac_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sag_2_State_Kapat_Kontaktor = kumesNodeRedModel.PedKapak_Sag_2_State_Kapat_Kontaktor == true ? 1 : 0; ;
                                var PedKapak_Sag_2_State_SwitchAcik = kumesNodeRedModel.PedKapak_Sag_2_State_SwitchAcik == true ? 1 : 0;
                                var PedKapak_Sag_2_State_SwitchKapali = kumesNodeRedModel.PedKapak_Sag_2_State_SwitchKapali == true ? 1 : 0;
                                var PedKapak_Sag_2_State_Durum = kumesNodeRedModel.PedKapak_Sag_2_State_Durum;
                                var PedKapak_Sag_2_Cmd_Ac = kumesNodeRedModel.PedKapak_Sag_2_Cmd_Ac == true ? 1 : 0;
                                var PedKapak_Sag_2_Cmd_Kapat = kumesNodeRedModel.PedKapak_Sag_2_Cmd_Kapat == true ? 1 : 0;
                                var PedKapak_Sag_2_Cmd_CmdBit = kumesNodeRedModel.PedKapak_Sag_2_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 686, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_State_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 687, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_State_Ac_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 688, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_State_Kapat_Kontaktor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 689, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_State_SwitchAcik });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 690, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_State_SwitchKapali });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 691, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_State_Durum.GetValueOrDefault(0) });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 692, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 693, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 694, KayitZamani = DateTime.Now, Deger = (decimal?)PedKapak_Sag_2_Cmd_CmdBit.GetValueOrDefault(0) });

                                var BacaFani_1_Auto = kumesNodeRedModel.BacaFani_1_Auto == true ? 1 : 0; ;
                                var BacaFani_1_Calisiyor = kumesNodeRedModel.BacaFani_1_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_1_Cmd_Ac = kumesNodeRedModel.BacaFani_1_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_1_Cmd_Kapat = kumesNodeRedModel.BacaFani_1_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_1_Cmd_CmdBit = kumesNodeRedModel.BacaFani_1_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 695, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_1_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 696, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_1_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 697, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_1_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 698, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_1_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 699, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_1_Cmd_CmdBit });

                                var BacaFani_2_Auto = kumesNodeRedModel.BacaFani_2_Auto == true ? 1 : 0; ;
                                var BacaFani_2_Calisiyor = kumesNodeRedModel.BacaFani_2_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_2_Cmd_Ac = kumesNodeRedModel.BacaFani_2_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_2_Cmd_Kapat = kumesNodeRedModel.BacaFani_2_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_2_Cmd_CmdBit = kumesNodeRedModel.BacaFani_2_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 700, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_2_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 701, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_2_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 702, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_2_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 703, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_2_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 704, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_2_Cmd_CmdBit });

                                var BacaFani_3_Auto = kumesNodeRedModel.BacaFani_3_Auto == true ? 1 : 0; ;
                                var BacaFani_3_Calisiyor = kumesNodeRedModel.BacaFani_3_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_3_Cmd_Ac = kumesNodeRedModel.BacaFani_3_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_3_Cmd_Kapat = kumesNodeRedModel.BacaFani_3_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_3_Cmd_CmdBit = kumesNodeRedModel.BacaFani_3_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 705, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_3_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 706, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_3_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 707, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_3_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 708, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_3_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 709, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_3_Cmd_CmdBit });

                                var BacaFani_4_Auto = kumesNodeRedModel.BacaFani_4_Auto == true ? 1 : 0; ;
                                var BacaFani_4_Calisiyor = kumesNodeRedModel.BacaFani_4_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_4_Cmd_Ac = kumesNodeRedModel.BacaFani_4_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_4_Cmd_Kapat = kumesNodeRedModel.BacaFani_4_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_4_Cmd_CmdBit = kumesNodeRedModel.BacaFani_4_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 710, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_4_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 711, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_4_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 712, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_4_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 713, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_4_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 714, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_4_Cmd_CmdBit });

                                var BacaFani_5_Auto = kumesNodeRedModel.BacaFani_5_Auto == true ? 1 : 0; ;
                                var BacaFani_5_Calisiyor = kumesNodeRedModel.BacaFani_5_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_5_Cmd_Ac = kumesNodeRedModel.BacaFani_5_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_5_Cmd_Kapat = kumesNodeRedModel.BacaFani_5_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_5_Cmd_CmdBit = kumesNodeRedModel.BacaFani_5_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 715, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_5_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 716, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_5_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 717, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_5_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 718, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_5_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 719, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_5_Cmd_CmdBit });

                                var BacaFani_6_Auto = kumesNodeRedModel.BacaFani_6_Auto == true ? 1 : 0; ;
                                var BacaFani_6_Calisiyor = kumesNodeRedModel.BacaFani_6_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_6_Cmd_Ac = kumesNodeRedModel.BacaFani_6_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_6_Cmd_Kapat = kumesNodeRedModel.BacaFani_6_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_6_Cmd_CmdBit = kumesNodeRedModel.BacaFani_6_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 720, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_6_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 721, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_6_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 722, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_6_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 723, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_6_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 724, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_6_Cmd_CmdBit });

                                var BacaFani_7_Auto = kumesNodeRedModel.BacaFani_7_Auto == true ? 1 : 0; ;
                                var BacaFani_7_Calisiyor = kumesNodeRedModel.BacaFani_7_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_7_Cmd_Ac = kumesNodeRedModel.BacaFani_7_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_7_Cmd_Kapat = kumesNodeRedModel.BacaFani_7_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_7_Cmd_CmdBit = kumesNodeRedModel.BacaFani_7_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 725, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_7_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 726, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_7_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 727, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_7_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 728, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_7_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 729, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_7_Cmd_CmdBit });

                                var BacaFani_8_Auto = kumesNodeRedModel.BacaFani_8_Auto == true ? 1 : 0; ;
                                var BacaFani_8_Calisiyor = kumesNodeRedModel.BacaFani_8_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_8_Cmd_Ac = kumesNodeRedModel.BacaFani_8_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_8_Cmd_Kapat = kumesNodeRedModel.BacaFani_8_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_8_Cmd_CmdBit = kumesNodeRedModel.BacaFani_8_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 730, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_8_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 731, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_8_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 732, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_8_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 733, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_8_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 734, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_8_Cmd_CmdBit });

                                var BacaFani_9_Auto = kumesNodeRedModel.BacaFani_9_Auto == true ? 1 : 0; ;
                                var BacaFani_9_Calisiyor = kumesNodeRedModel.BacaFani_9_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_9_Cmd_Ac = kumesNodeRedModel.BacaFani_9_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_9_Cmd_Kapat = kumesNodeRedModel.BacaFani_9_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_9_Cmd_CmdBit = kumesNodeRedModel.BacaFani_9_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 735, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_9_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 736, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_9_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 737, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_9_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 738, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_9_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 739, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_9_Cmd_CmdBit });

                                var BacaFani_10_Auto = kumesNodeRedModel.BacaFani_10_Auto == true ? 1 : 0; ;
                                var BacaFani_10_Calisiyor = kumesNodeRedModel.BacaFani_10_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_10_Cmd_Ac = kumesNodeRedModel.BacaFani_10_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_10_Cmd_Kapat = kumesNodeRedModel.BacaFani_10_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_10_Cmd_CmdBit = kumesNodeRedModel.BacaFani_10_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 740, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_10_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 741, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_10_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 742, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_10_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 743, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_10_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 744, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_10_Cmd_CmdBit });

                                var BacaFani_11_Auto = kumesNodeRedModel.BacaFani_11_Auto == true ? 1 : 0; ;
                                var BacaFani_11_Calisiyor = kumesNodeRedModel.BacaFani_11_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_11_Cmd_Ac = kumesNodeRedModel.BacaFani_11_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_11_Cmd_Kapat = kumesNodeRedModel.BacaFani_11_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_11_Cmd_CmdBit = kumesNodeRedModel.BacaFani_11_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 745, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_11_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 746, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_11_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 747, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_11_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 748, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_11_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 749, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_11_Cmd_CmdBit });

                                var BacaFani_12_Auto = kumesNodeRedModel.BacaFani_12_Auto == true ? 1 : 0; ;
                                var BacaFani_12_Calisiyor = kumesNodeRedModel.BacaFani_12_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_12_Cmd_Ac = kumesNodeRedModel.BacaFani_12_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_12_Cmd_Kapat = kumesNodeRedModel.BacaFani_12_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_12_Cmd_CmdBit = kumesNodeRedModel.BacaFani_12_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 750, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_12_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 751, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_12_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 752, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_12_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 753, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_12_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 754, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_12_Cmd_CmdBit });

                                var BacaFani_13_Auto = kumesNodeRedModel.BacaFani_13_Auto == true ? 1 : 0; ;
                                var BacaFani_13_Calisiyor = kumesNodeRedModel.BacaFani_13_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_13_Cmd_Ac = kumesNodeRedModel.BacaFani_13_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_13_Cmd_Kapat = kumesNodeRedModel.BacaFani_13_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_13_Cmd_CmdBit = kumesNodeRedModel.BacaFani_13_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 755, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_13_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 756, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_13_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 757, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_13_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 758, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_13_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 759, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_13_Cmd_CmdBit });

                                var BacaFani_14_Auto = kumesNodeRedModel.BacaFani_14_Auto == true ? 1 : 0; ;
                                var BacaFani_14_Calisiyor = kumesNodeRedModel.BacaFani_14_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_14_Cmd_Ac = kumesNodeRedModel.BacaFani_14_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_14_Cmd_Kapat = kumesNodeRedModel.BacaFani_14_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_14_Cmd_CmdBit = kumesNodeRedModel.BacaFani_14_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 760, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_14_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 761, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_14_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 762, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_14_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 763, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_14_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 764, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_14_Cmd_CmdBit });

                                var BacaFani_15_Auto = kumesNodeRedModel.BacaFani_15_Auto == true ? 1 : 0; ;
                                var BacaFani_15_Calisiyor = kumesNodeRedModel.BacaFani_15_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_15_Cmd_Ac = kumesNodeRedModel.BacaFani_15_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_15_Cmd_Kapat = kumesNodeRedModel.BacaFani_15_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_15_Cmd_CmdBit = kumesNodeRedModel.BacaFani_15_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 765, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_15_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 766, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_15_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 767, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_15_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 768, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_15_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 769, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_15_Cmd_CmdBit });

                                var BacaFani_16_Auto = kumesNodeRedModel.BacaFani_16_Auto == true ? 1 : 0; ;
                                var BacaFani_16_Calisiyor = kumesNodeRedModel.BacaFani_16_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_16_Cmd_Ac = kumesNodeRedModel.BacaFani_16_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_16_Cmd_Kapat = kumesNodeRedModel.BacaFani_16_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_16_Cmd_CmdBit = kumesNodeRedModel.BacaFani_16_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 770, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_16_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 771, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_16_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 772, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_16_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 773, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_16_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 774, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_16_Cmd_CmdBit });

                                var BacaFani_17_Auto = kumesNodeRedModel.BacaFani_17_Auto == true ? 1 : 0; ;
                                var BacaFani_17_Calisiyor = kumesNodeRedModel.BacaFani_17_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_17_Cmd_Ac = kumesNodeRedModel.BacaFani_17_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_17_Cmd_Kapat = kumesNodeRedModel.BacaFani_17_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_17_Cmd_CmdBit = kumesNodeRedModel.BacaFani_17_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 775, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_17_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 776, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_17_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 777, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_17_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 778, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_17_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 779, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_17_Cmd_CmdBit });

                                var BacaFani_18_Auto = kumesNodeRedModel.BacaFani_18_Auto == true ? 1 : 0; ;
                                var BacaFani_18_Calisiyor = kumesNodeRedModel.BacaFani_18_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_18_Cmd_Ac = kumesNodeRedModel.BacaFani_18_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_18_Cmd_Kapat = kumesNodeRedModel.BacaFani_18_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_18_Cmd_CmdBit = kumesNodeRedModel.BacaFani_18_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 780, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_18_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 781, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_18_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 782, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_18_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 783, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_18_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 784, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_18_Cmd_CmdBit });

                                var BacaFani_19_Auto = kumesNodeRedModel.BacaFani_19_Auto == true ? 1 : 0; ;
                                var BacaFani_19_Calisiyor = kumesNodeRedModel.BacaFani_19_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_19_Cmd_Ac = kumesNodeRedModel.BacaFani_19_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_19_Cmd_Kapat = kumesNodeRedModel.BacaFani_19_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_19_Cmd_CmdBit = kumesNodeRedModel.BacaFani_19_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 785, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_19_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 786, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_19_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 787, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_19_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 788, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_19_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 789, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_19_Cmd_CmdBit });

                                var BacaFani_20_Auto = kumesNodeRedModel.BacaFani_20_Auto == true ? 1 : 0; ;
                                var BacaFani_20_Calisiyor = kumesNodeRedModel.BacaFani_20_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_20_Cmd_Ac = kumesNodeRedModel.BacaFani_20_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_20_Cmd_Kapat = kumesNodeRedModel.BacaFani_20_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_20_Cmd_CmdBit = kumesNodeRedModel.BacaFani_20_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 790, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_20_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 791, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_20_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 792, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_20_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 793, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_20_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 794, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_20_Cmd_CmdBit });

                                var BacaFani_21_Auto = kumesNodeRedModel.BacaFani_21_Auto == true ? 1 : 0; ;
                                var BacaFani_21_Calisiyor = kumesNodeRedModel.BacaFani_21_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_21_Cmd_Ac = kumesNodeRedModel.BacaFani_21_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_21_Cmd_Kapat = kumesNodeRedModel.BacaFani_21_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_21_Cmd_CmdBit = kumesNodeRedModel.BacaFani_21_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 795, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_21_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 796, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_21_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 797, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_21_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 798, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_21_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 799, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_21_Cmd_CmdBit });

                                var BacaFani_22_Auto = kumesNodeRedModel.BacaFani_22_Auto == true ? 1 : 0; ;
                                var BacaFani_22_Calisiyor = kumesNodeRedModel.BacaFani_22_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_22_Cmd_Ac = kumesNodeRedModel.BacaFani_22_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_22_Cmd_Kapat = kumesNodeRedModel.BacaFani_22_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_22_Cmd_CmdBit = kumesNodeRedModel.BacaFani_22_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 800, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_22_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 801, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_22_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 802, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_22_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 803, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_22_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 804, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_22_Cmd_CmdBit });

                                var BacaFani_23_Auto = kumesNodeRedModel.BacaFani_23_Auto == true ? 1 : 0; ;
                                var BacaFani_23_Calisiyor = kumesNodeRedModel.BacaFani_23_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_23_Cmd_Ac = kumesNodeRedModel.BacaFani_23_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_23_Cmd_Kapat = kumesNodeRedModel.BacaFani_23_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_23_Cmd_CmdBit = kumesNodeRedModel.BacaFani_23_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 805, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_23_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 806, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_23_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 807, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_23_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 808, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_23_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 809, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_23_Cmd_CmdBit });

                                var BacaFani_24_Auto = kumesNodeRedModel.BacaFani_24_Auto == true ? 1 : 0; ;
                                var BacaFani_24_Calisiyor = kumesNodeRedModel.BacaFani_24_Calisiyor == true ? 1 : 0; ;
                                var BacaFani_24_Cmd_Ac = kumesNodeRedModel.BacaFani_24_Cmd_Ac == true ? 1 : 0; ;
                                var BacaFani_24_Cmd_Kapat = kumesNodeRedModel.BacaFani_24_Cmd_Kapat == true ? 1 : 0; ;
                                var BacaFani_24_Cmd_CmdBit = kumesNodeRedModel.BacaFani_24_Cmd_CmdBit;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 810, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_24_Auto });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 811, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_24_Calisiyor });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 812, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_24_Cmd_Ac });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 813, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_24_Cmd_Kapat });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 814, KayitZamani = DateTime.Now, Deger = (decimal?)BacaFani_24_Cmd_CmdBit });

                                var hayvanBasinaSuTuketim = gunlukHayvanSayisi == 0 ? 0 : (kumesNodeRedModel.IcmeSuyu_TuketimBugun * 1000000) / gunlukHayvanSayisi;
                                var hayvanBasinaYemTuketim = gunlukHayvanSayisi == 0 ? 0 : (kumesNodeRedModel.Yem_TuketimBugun * 1000000) / gunlukHayvanSayisi;
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 104, KayitZamani = DateTime.Now, Deger = (decimal?)hayvanBasinaSuTuketim });
                                etiketLogs.Add(new EtiketLog_TempYeni { KumesId = kumesId, EtiketId = 105, KayitZamani = DateTime.Now, Deger = (decimal?)hayvanBasinaYemTuketim });
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

        private async Task<IEnumerable<AlarmLog>> GetUretilmisAlarmLogDataForKumes(int kumesId, CancellationToken stoppingToken)
        {
            List<AlarmLog> alarmLogs = new List<AlarmLog>();
            var now = DateTime.Now;

            var activeAlarms = await _alarmCacheService.GetAlarmLogsAsync(kumesId);
            if (activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.SebekedeElektrikYok))
            {
                if (activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Plc_Koptu))
                {
                    if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.ElektrikKesintisiPlcKapandi))
                    {
                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.ElektrikKesintisiPlcKapandi + kumesId, AlarmEnum = (int?)AlarmLogTipler.ElektrikKesintisiPlcKapandi, Mesaj = AlarmLogTipler.ElektrikKesintisiPlcKapandi.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                        _alarmCacheService.ClearMemoryCache(kumesId);
                    }
                }
                else
                {
                    if (activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.ElektrikKesintisiPlcKapandi))
                    {
                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.ElektrikKesintisiPlcKapandi);
                    }
                }
            }
            else
            {
                if (activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.ElektrikKesintisiPlcKapandi))
                {
                    await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.ElektrikKesintisiPlcKapandi);
                }
            }
            return alarmLogs;
        }

        private async Task<IEnumerable<AlarmLog>> GetAlarmLogDataForBulkInsert(int kumesId, CancellationToken stoppingToken)
        {
            try
            {
                var now = DateTime.Now;
                List<AlarmLog> alarmLogs = new List<AlarmLog>();
                KumesIzlemeNodeRed? model;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    model = await kumesScadaDbContext.KumesIzlemeNodeRed.AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                    var activeAlarms = await _alarmCacheService.GetAlarmLogsAsync(kumesId/*, now*/);
                    if (model != null)
                    {
                        if (PlcVerisiGuncelMi(model, now))
                        {
                            if (model.Veriler != null)
                            {
                                KumesNodeRedModel kumesNodeRedModel = JsonSerializer.Deserialize<KumesNodeRedModel>(model.Veriler);
                                //if (activeAlarms.Any(x => x.KayitZamani.Value.Date != now.Date) || activeAlarms.Count() == 0)
                                //{
                                //    await _alarmCacheService.SetOldAlarmsPassiveByKumesId(kumesId, now);
                                //    activeAlarms = await _alarmCacheService.GetActiveAlarmsByKumesId(kumesId/*, now*/);
                                //}

                                bool sicaklik_AktifSensSayisiAz = kumesNodeRedModel.SicaklikAlarm_AktifSensSayisiAz.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_AktifSensSayisiAz))
                                {
                                    if (sicaklik_AktifSensSayisiAz)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_AktifSensSayisiAz + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_AktifSensSayisiAz, Mesaj = AlarmLogTipler.Sicaklik_AktifSensSayisiAz.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_AktifSensSayisiAz)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_AktifSensSayisiAz/*, now*/);
                                    }
                                }
                                bool sicaklik_DusukKumes = kumesNodeRedModel.SicaklikAlarm_DusukKumes.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_DusukKumes))
                                {
                                    if (sicaklik_DusukKumes)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_DusukKumes + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_DusukKumes, Mesaj = AlarmLogTipler.Sicaklik_DusukKumes.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_DusukKumes)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_DusukKumes/*, now*/);
                                    }
                                }
                                bool sicaklik_DusukSens1 = kumesNodeRedModel.SicaklikAlarm_DusukSens1.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_DusukSens1))
                                {
                                    if (sicaklik_DusukSens1)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_DusukSens1 + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_DusukSens1, Mesaj = AlarmLogTipler.Sicaklik_DusukSens1.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_DusukSens1)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_DusukSens1/*, now*/);
                                    }
                                }
                                bool sicaklik_DusukSens2 = kumesNodeRedModel.SicaklikAlarm_DusukSens2.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_DusukSens2))
                                {
                                    if (sicaklik_DusukSens2)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_DusukSens2 + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_DusukSens2, Mesaj = AlarmLogTipler.Sicaklik_DusukSens2.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_DusukSens2)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_DusukSens2/*, now*/);
                                    }
                                }
                                bool sicaklik_DusukSens3 = kumesNodeRedModel.SicaklikAlarm_DusukSens3.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_DusukSens3))
                                {
                                    if (sicaklik_DusukSens3)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_DusukSens3 + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_DusukSens3, Mesaj = AlarmLogTipler.Sicaklik_DusukSens3.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_DusukSens3)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_DusukSens3/*, now*/);
                                    }
                                }
                                bool sicaklik_DusukSens4 = kumesNodeRedModel.SicaklikAlarm_DusukSens4.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_DusukSens4))
                                {
                                    if (sicaklik_DusukSens4)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_DusukSens4 + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_DusukSens4, Mesaj = AlarmLogTipler.Sicaklik_DusukSens4.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_DusukSens4)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_DusukSens4/*, now*/);
                                    }
                                }
                                bool sicaklik_DusukSens5 = kumesNodeRedModel.SicaklikAlarm_DusukSens5.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_DusukSens5))
                                {
                                    if (sicaklik_DusukSens5)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_DusukSens5 + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_DusukSens5, Mesaj = AlarmLogTipler.Sicaklik_DusukSens5.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_DusukSens5)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_DusukSens5/*, now*/);
                                    }
                                }
                                bool sicaklik_DusukSens6 = kumesNodeRedModel.SicaklikAlarm_DusukSens6.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_DusukSens6))
                                {
                                    if (sicaklik_DusukSens6)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_DusukSens6 + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_DusukSens6, Mesaj = AlarmLogTipler.Sicaklik_DusukSens6.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_DusukSens6)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_DusukSens6/*, now*/);
                                    }
                                }
                                bool sicaklik_FazlaKumes = kumesNodeRedModel.SicaklikAlarm_FazlaKumes.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_FazlaKumes))
                                {
                                    if (sicaklik_FazlaKumes)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_FazlaKumes + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_FazlaKumes, Mesaj = AlarmLogTipler.Sicaklik_FazlaKumes.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_FazlaKumes)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_FazlaKumes/*, now*/);
                                    }
                                }
                                bool sicaklik_FazlaSens1 = kumesNodeRedModel.SicaklikAlarm_FazlaSens1.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_FazlaSens1))
                                {
                                    if (sicaklik_FazlaSens1)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_FazlaSens1 + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_FazlaSens1, Mesaj = AlarmLogTipler.Sicaklik_FazlaSens1.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_FazlaSens1)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_FazlaSens1/*, now*/);
                                    }
                                }
                                bool sicaklik_FazlaSens2 = kumesNodeRedModel.SicaklikAlarm_FazlaSens2.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_FazlaSens2))
                                {
                                    if (sicaklik_FazlaSens2)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_FazlaSens2 + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_FazlaSens2, Mesaj = AlarmLogTipler.Sicaklik_FazlaSens2.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_FazlaSens2)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_FazlaSens2/*, now*/);
                                    }
                                }
                                bool sicaklik_FazlaSens3 = kumesNodeRedModel.SicaklikAlarm_FazlaSens3.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_FazlaSens3))
                                {
                                    if (sicaklik_FazlaSens3)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_FazlaSens3 + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_FazlaSens3, Mesaj = AlarmLogTipler.Sicaklik_FazlaSens3.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_FazlaSens3)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_FazlaSens3/*, now*/);
                                    }
                                }
                                bool sicaklik_FazlaSens4 = kumesNodeRedModel.SicaklikAlarm_FazlaSens4.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_FazlaSens4))
                                {
                                    if (sicaklik_FazlaSens4)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_FazlaSens4 + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_FazlaSens4, Mesaj = AlarmLogTipler.Sicaklik_FazlaSens4.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_FazlaSens4)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_FazlaSens4/*, now*/);
                                    }
                                }
                                bool sicaklik_FazlaSens5 = kumesNodeRedModel.SicaklikAlarm_FazlaSens5.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_FazlaSens5))
                                {
                                    if (sicaklik_FazlaSens5)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_FazlaSens5 + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_FazlaSens5, Mesaj = AlarmLogTipler.Sicaklik_FazlaSens5.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_FazlaSens5)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_FazlaSens5/*, now*/);
                                    }
                                }
                                bool sicaklik_FazlaSens6 = kumesNodeRedModel.SicaklikAlarm_FazlaSens6.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sicaklik_FazlaSens6))
                                {
                                    if (sicaklik_FazlaSens6)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sicaklik_FazlaSens6 + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sicaklik_FazlaSens6, Mesaj = AlarmLogTipler.Sicaklik_FazlaSens6.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sicaklik_FazlaSens6)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sicaklik_FazlaSens6/*, now*/);
                                    }
                                }
                                bool nem_AktifSensSayisiAz = kumesNodeRedModel.NemAlarm_AktifSensSayisiAz.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Nem_AktifSensSayisiAz))
                                {
                                    if (nem_AktifSensSayisiAz)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Nem_AktifSensSayisiAz + kumesId, AlarmEnum = (int?)AlarmLogTipler.Nem_AktifSensSayisiAz, Mesaj = AlarmLogTipler.Nem_AktifSensSayisiAz.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!nem_AktifSensSayisiAz)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Nem_AktifSensSayisiAz/*, now*/);
                                    }
                                }
                                bool co2_AktifDegil = kumesNodeRedModel.CO2Alarm_AktifDegil.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Co2_AktifDegil))
                                {
                                    if (co2_AktifDegil)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Co2_AktifDegil + kumesId, AlarmEnum = (int?)AlarmLogTipler.Co2_AktifDegil, Mesaj = AlarmLogTipler.Co2_AktifDegil.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!co2_AktifDegil)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Co2_AktifDegil/*, now*/);
                                    }
                                }
                                bool co2_Fazla = kumesNodeRedModel.CO2Alarm_Fazla.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Co2_Fazla))
                                {
                                    if (co2_Fazla)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Co2_Fazla + kumesId, AlarmEnum = (int?)AlarmLogTipler.Co2_Fazla, Mesaj = AlarmLogTipler.Co2_Fazla.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!co2_Fazla)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Co2_Fazla/*, now*/);
                                    }
                                }
                                bool basinc_AktifDegil = kumesNodeRedModel.BasincAlarm_AktifDegil.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Basinc_AktifDegil))
                                {
                                    if (basinc_AktifDegil)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Basinc_AktifDegil + kumesId, AlarmEnum = (int?)AlarmLogTipler.Basinc_AktifDegil, Mesaj = AlarmLogTipler.Basinc_AktifDegil.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!basinc_AktifDegil)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Basinc_AktifDegil/*, now*/);
                                    }
                                }
                                bool basinc_Fazla = kumesNodeRedModel.BasincAlarm_Fazla.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Basinc_Fazla))
                                {
                                    if (basinc_Fazla)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Basinc_Fazla + kumesId, AlarmEnum = (int?)AlarmLogTipler.Basinc_Fazla, Mesaj = AlarmLogTipler.Basinc_Fazla.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!basinc_Fazla)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Basinc_Fazla/*, now*/);
                                    }
                                }
                                bool icmeSuyu_SayacAktifDegil = kumesNodeRedModel.IcmeSuyuAlarm_AktifDegil.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.IcmeSuyu_SayacAktifDegil))
                                {
                                    if (icmeSuyu_SayacAktifDegil)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.IcmeSuyu_SayacAktifDegil + kumesId, AlarmEnum = (int?)AlarmLogTipler.IcmeSuyu_SayacAktifDegil, Mesaj = AlarmLogTipler.IcmeSuyu_SayacAktifDegil.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!icmeSuyu_SayacAktifDegil)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.IcmeSuyu_SayacAktifDegil/*, now*/);
                                    }
                                }
                                bool fan_TermikNOK = kumesNodeRedModel.FanAlarm_TermikNOK.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Fan_TermikNOK))
                                {
                                    if (fan_TermikNOK)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Fan_TermikNOK + kumesId, AlarmEnum = (int?)AlarmLogTipler.Fan_TermikNOK, Mesaj = AlarmLogTipler.Fan_TermikNOK.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!fan_TermikNOK)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Fan_TermikNOK/*, now*/);
                                    }
                                }
                                bool ped_PompaTermikNOK = kumesNodeRedModel.PedAlarm_PompaTermikNOK.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Ped_PompaTermikNOK))
                                {
                                    if (ped_PompaTermikNOK)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Ped_PompaTermikNOK + kumesId, AlarmEnum = (int?)AlarmLogTipler.Ped_PompaTermikNOK, Mesaj = AlarmLogTipler.Ped_PompaTermikNOK.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!ped_PompaTermikNOK)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Ped_PompaTermikNOK/*, now*/);
                                    }
                                }
                                bool solBuffel_TermikNOK = kumesNodeRedModel.BuffelAlarm_SolTermikNOK.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sol_Buffel_TermikNOK))
                                {
                                    if (solBuffel_TermikNOK)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sol_Buffel_TermikNOK + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sol_Buffel_TermikNOK, Mesaj = AlarmLogTipler.Sol_Buffel_TermikNOK.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!solBuffel_TermikNOK)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sol_Buffel_TermikNOK/*, now*/);
                                    }
                                }

                                bool sagBuffel_TermikNOK = kumesNodeRedModel.BuffelAlarm_SagTermikNOK.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Sag_Buffel_TermikNOK))
                                {
                                    if (sagBuffel_TermikNOK)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Sag_Buffel_TermikNOK + kumesId, AlarmEnum = (int?)AlarmLogTipler.Sag_Buffel_TermikNOK, Mesaj = AlarmLogTipler.Sag_Buffel_TermikNOK.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sagBuffel_TermikNOK)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Sag_Buffel_TermikNOK/*, now*/);
                                    }
                                }

                                bool sebekedeElektrikYok = kumesNodeRedModel.GenelAlarm_SebekedeElektrikYok.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.SebekedeElektrikYok))
                                {
                                    if (sebekedeElektrikYok)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.SebekedeElektrikYok + kumesId, AlarmEnum = (int?)AlarmLogTipler.SebekedeElektrikYok, Mesaj = AlarmLogTipler.SebekedeElektrikYok.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!sebekedeElektrikYok)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.SebekedeElektrikYok/*, now*/);
                                    }
                                }

                                bool silo1BaglantiYok = kumesNodeRedModel.YemAlarm_Silo1BaglantiKoptu.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Silo1BaglantiKoptu))
                                {
                                    if (silo1BaglantiYok)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Silo1BaglantiKoptu + kumesId, AlarmEnum = (int?)AlarmLogTipler.Silo1BaglantiKoptu, Mesaj = AlarmLogTipler.Silo1BaglantiKoptu.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!silo1BaglantiYok)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Silo1BaglantiKoptu/*, now*/);
                                    }
                                }

                                bool silo2BaglantiYok = kumesNodeRedModel.YemAlarm_Silo2BaglantiKoptu.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Silo2BaglantiKoptu))
                                {
                                    if (silo2BaglantiYok)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Silo2BaglantiKoptu + kumesId, AlarmEnum = (int?)AlarmLogTipler.Silo2BaglantiKoptu, Mesaj = AlarmLogTipler.Silo2BaglantiKoptu.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!silo2BaglantiYok)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Silo2BaglantiKoptu/*, now*/);
                                    }
                                }

                                bool silo1YuklemeAniAlarm = kumesNodeRedModel.GenelAlarm_AlarmReserve_1.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Silo1YuklemeAniAlarm))
                                {
                                    if (silo1YuklemeAniAlarm)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Silo1YuklemeAniAlarm + kumesId, AlarmEnum = (int?)AlarmLogTipler.Silo1YuklemeAniAlarm, Mesaj = AlarmLogTipler.Silo1YuklemeAniAlarm.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!silo1YuklemeAniAlarm)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Silo1YuklemeAniAlarm/*, now*/);
                                    }
                                }

                                bool silo2YuklemeAniAlarm = kumesNodeRedModel.GenelAlarm_AlarmReserve_2.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Silo2YuklemeAniAlarm))
                                {
                                    if (silo2YuklemeAniAlarm)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Silo2YuklemeAniAlarm + kumesId, AlarmEnum = (int?)AlarmLogTipler.Silo2YuklemeAniAlarm, Mesaj = AlarmLogTipler.Silo2YuklemeAniAlarm.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!silo2YuklemeAniAlarm)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Silo2YuklemeAniAlarm/*, now*/);
                                    }
                                }

                                bool silo1DegisimAniAlarm = kumesNodeRedModel.GenelAlarm_AlarmReserve_3.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Silo1DegisimAniAlarm))
                                {
                                    if (silo1DegisimAniAlarm)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Silo1DegisimAniAlarm + kumesId, AlarmEnum = (int?)AlarmLogTipler.Silo1DegisimAniAlarm, Mesaj = AlarmLogTipler.Silo1DegisimAniAlarm.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!silo1DegisimAniAlarm)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Silo1DegisimAniAlarm/*, now*/);
                                    }
                                }

                                bool silo2DegisimAniAlarm = kumesNodeRedModel.GenelAlarm_AlarmReserve_4.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Silo2DegisimAniAlarm))
                                {
                                    if (silo2DegisimAniAlarm)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Silo2DegisimAniAlarm + kumesId, AlarmEnum = (int?)AlarmLogTipler.Silo2DegisimAniAlarm, Mesaj = AlarmLogTipler.Silo2DegisimAniAlarm.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!silo2DegisimAniAlarm)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Silo2DegisimAniAlarm/*, now*/);
                                    }
                                }

                                bool silo1NegatifDegerAlarm = kumesNodeRedModel.GenelAlarm_AlarmReserve_5.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Silo1NegatifDegerAlarm))
                                {
                                    if (silo1NegatifDegerAlarm)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Silo1NegatifDegerAlarm + kumesId, AlarmEnum = (int?)AlarmLogTipler.Silo1NegatifDegerAlarm, Mesaj = AlarmLogTipler.Silo1NegatifDegerAlarm.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!silo1NegatifDegerAlarm)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Silo1NegatifDegerAlarm/*, now*/);
                                    }
                                }

                                bool silo2NegatifDegerAlarm = kumesNodeRedModel.GenelAlarm_AlarmReserve_6.GetValueOrDefault(false);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.Silo2NegatifDegerAlarm))
                                {
                                    if (silo2NegatifDegerAlarm)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Silo2NegatifDegerAlarm + kumesId, AlarmEnum = (int?)AlarmLogTipler.Silo2NegatifDegerAlarm, Mesaj = AlarmLogTipler.Silo2NegatifDegerAlarm.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (!silo2NegatifDegerAlarm)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.Silo2NegatifDegerAlarm/*, now*/);
                                    }
                                }
                                int icmeSuyuAkisYok = kumesNodeRedModel.IcmeSuyu_Reserve_2.GetValueOrDefault(1);
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.IcmeSuyuAkisYok))
                                {
                                    if (icmeSuyuAkisYok == 2)
                                    {
                                        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.IcmeSuyuAkisYok + kumesId, AlarmEnum = (int?)AlarmLogTipler.IcmeSuyuAkisYok, Mesaj = AlarmLogTipler.IcmeSuyuAkisYok.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                        _alarmCacheService.ClearMemoryCache(kumesId);
                                    }
                                }
                                else
                                {
                                    if (icmeSuyuAkisYok == 1)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.IcmeSuyuAkisYok/*, now*/);
                                    }
                                }

                            }
                        }
                    }
                    var hayvanSayisiKontrol = hayvanSayisiMailGonderildi.Where(x => x.Item1 == kumesId).FirstOrDefault();
                    if (hayvanSayisiKontrol != default)
                    {
                        var isHaftaIci = now.DayOfWeek >= DayOfWeek.Monday && now.DayOfWeek <= DayOfWeek.Friday;
                        var isHaftaSonu = now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;

                        if (isHaftaIci)
                        {
                            // Pazartesi ozel: hafta sonu girilmemis verileri kontrol et
                            if (now.DayOfWeek == DayOfWeek.Monday && now.Hour >= 10)
                            {
                                var haftaSonuGirildiMi = await _alarmCacheService.GetHaftaSonuOluHayvanSayisiGirildiMiByKumesId(kumesId);
                                if (!haftaSonuGirildiMi && !activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.HaftaSonuOluHayvanSayisiGirilmedi))
                                {
                                    alarmLogs.Add(new AlarmLog
                                    {
                                        KayitZamani = now,
                                        AlarmSeviye = 0,
                                        KumesId = kumesId,
                                        AlarmId = (int)AlarmIds.HaftaSonuOluHayvanSayisiGirilmedi + kumesId,
                                        AlarmEnum = (int?)AlarmLogTipler.HaftaSonuOluHayvanSayisiGirilmedi,
                                        Mesaj = AlarmLogTipler.HaftaSonuOluHayvanSayisiGirilmedi.GetDescription(),
                                        KonveyorId = null,
                                        Aktif = true,
                                        Silindi = false
                                    });
                                    _alarmCacheService.ClearMemoryCache(kumesId);
                                }
                                if (haftaSonuGirildiMi)
                                {
                                    await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.HaftaSonuOluHayvanSayisiGirilmedi);
                                }
                            }

                            // Mevcut haftaici alarm
                            if (now.Hour >= 15)
                            {
                                if (!activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.GunlukOluHayvanSayisiGirilmedi))
                                {
                                    if (!(hayvanSayisiKontrol.Item2 == true && hayvanSayisiKontrol.Item3.Date == now.Date))
                                    {
                                        var gunlukHayvanSayisiGirildiMi = await _alarmCacheService.GetOluHayvanSayisiGirildiMiByKumesId(kumesId);
                                        if (!gunlukHayvanSayisiGirildiMi)
                                        {
                                            alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.GunlukOluHayvanSayisiGirilmedi + kumesId, AlarmEnum = (int?)AlarmLogTipler.GunlukOluHayvanSayisiGirilmedi, Mesaj = AlarmLogTipler.GunlukOluHayvanSayisiGirilmedi.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                            _alarmCacheService.ClearMemoryCache(kumesId);
                                        }
                                        hayvanSayisiKontrol = (kumesId, true, now);
                                    }
                                }
                                else if (activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.GunlukOluHayvanSayisiGirilmedi))
                                {
                                    var gunlukHayvanSayisiGirildiMi = await _alarmCacheService.GetOluHayvanSayisiGirildiMiByKumesId(kumesId);
                                    if (gunlukHayvanSayisiGirildiMi)
                                    {
                                        await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.GunlukOluHayvanSayisiGirilmedi);
                                    }
                                }
                            }
                            else
                            {
                                //alarm� silmek laz�m 24 sonras�                  
                                if (activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.GunlukOluHayvanSayisiGirilmedi))
                                {
                                    await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.GunlukOluHayvanSayisiGirilmedi);
                                    hayvanSayisiKontrol = (kumesId, false, now);
                                }
                            }
                        }
                        else if (isHaftaSonu)
                        {
                            if (activeAlarms.Any(x => x.KumesId == kumesId && x.AlarmEnum == (int)AlarmLogTipler.GunlukOluHayvanSayisiGirilmedi))
                            {
                                await _alarmCacheService.SetAlarmPassive(kumesId, (int)AlarmLogTipler.GunlukOluHayvanSayisiGirilmedi);
                                hayvanSayisiKontrol = (kumesId, false, now);
                            }
                        }
                    }
                    return alarmLogs;
                }
            }
            catch (Exception)
            {
                return new List<AlarmLog>();
            }
        }

        public async Task AddAylikKumesHayvanLog(int kumesId)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var now = DateTime.Now;
                    var kumesScadaDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                    var ayinIlkGunu = new DateTime(now.Year, now.Month, 1);
                    var ayinSonGunu = ayinIlkGunu.AddMonths(1).AddDays(-1);

                    //var aktifDonem = await kumesScadaDbContext.Donem.Where(x => x.KumesId == kumesId && x.Silindi != true && x.Aktif != false).FirstOrDefaultAsync();
                    //if (aktifDonem == null)
                    //    return;
                    var mevcutKayitlar = await kumesScadaDbContext.KumesHayvanLog.Where(x => x.SaglikDurumu == KumesHayvanSaglikDurum.Olu && x.KumesId == kumesId && x.KayitZamani >= ayinIlkGunu && x.KayitZamani <= ayinSonGunu /*&& x.Silindi != true*/).Select(x => x.KayitZamani.Value.Date).ToListAsync();

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
            }
            catch (Exception)
            {
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                //_logger.LogInformation("AlarmBilgilendirmeServis stop: {time}", DateTimeOffset.Now);
                await UpdateServisDurumAsync("RahmiyeTesisWorker", 3, "Servis durduruldu");
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                await UpdateServisDurumAsync("RahmiyeTesisWorker", 0, ex.Message);
            }
        }

        //private async Task<int?> GetDirtyEggCleanAsync(int kumesId, DateTime day, CancellationToken ct)
        //{
        //    try
        //    {
        //        const string sql = @"SELECT TOP (1) DirtEggClean FROM dbo.EggActual WITH (NOLOCK) WHERE LocationId = @kumesId AND CAST([LastModificationTime] as date) = CAST(@day as date) ORDER BY [LastModificationTime] DESC;";

        //        await using var conn = new SqlConnection(_vbapConnStr);
        //        await conn.OpenAsync(ct);

        //        await using var cmd = new SqlCommand(sql, conn);
        //        cmd.Parameters.Add("@kumesId", SqlDbType.Int).Value = kumesId;
        //        cmd.Parameters.Add("@day", SqlDbType.DateTime2).Value = day;

        //        var obj = await cmd.ExecuteScalarAsync(ct);

        //        if (obj == null || obj == DBNull.Value) return null;

        //        return Convert.ToInt32(obj);
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //        //throw;
        //    }
        //}
        private EggActualDbModel? GetYumurtaBilgiHelper(int kumesId)
        {
            return yumurtaBilgileriDict.TryGetValue(kumesId, out var dto) ? dto : null;
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

        //public async Task Disconnect(int kumesId)
        //{
        //    await _mainClassRahmiye.Change_Connection((Desc.eKumesTanim)kumesId, Desc.eConnection.Disconnect, _sqlConnection.ConnectionString);
        //    _logger.LogInformation("Disconnect metodu �al��t�r�ld�!");
        //    var kumesler = await _alarmCacheService.GetKumeslerAsync(12);
        //    kumesListesi = _mainClassRahmiye.FindKumeslerByTcIds(kumesler.Select(x => x.KumesTcId).ToList());
        //}

        //public async Task Connect(int kumesId)
        //{
        //    await _mainClassRahmiye.Change_Connection((Desc.eKumesTanim)kumesId, Desc.eConnection.Connect, _sqlConnection.ConnectionString);
        //    _logger.LogInformation("Connect metodu �al��t�r�ld�!");
        //    var kumesler = await _alarmCacheService.GetKumeslerAsync(12);
        //    kumesListesi = _mainClassRahmiye.FindKumeslerByTcIds(kumesler.Select(x => x.KumesTcId).ToList());
        //}
        //public async Task CheckFlag(int kumesId)
        //{
        //    await _mainClassRahmiye.CheckFlag((Desc.eKumesTanim)kumesId, _sqlConnection.ConnectionString);
        //    _logger.LogInformation("CheckFlag metodu �al��t�r�ld�!");
        //    var kumesler = await _alarmCacheService.GetKumeslerAsync(12);
        //    kumesListesi = _mainClassRahmiye.FindKumeslerByTcIds(kumesler.Select(x => x.KumesTcId).ToList());
        //}

        //public async Task<bool> GetIsPlcReading(int kumesId)
        //{
        //    var result = await _mainClassRahmiye.GetPlcRead((Desc.eKumesTanim)kumesId);
        //    //_logger.LogInformation("GetIsPlcReading metodu �al��t�r�ld�!");
        //    return await Task.FromResult(result);
        //    //return true;
        //}

        //public async Task<bool> GetIsPlcConnect(int kumesId)
        //{
        //    var result = await _mainClassRahmiye.GetPlcConnected((Desc.eKumesTanim)kumesId);
        //    //_logger.LogInformation("GetIsPlcConnect metodu �al��t�r�ld�!");
        //    return await Task.FromResult(result);
        //    //return true;
        //}

        //public async Task<bool> GetPlcConnectedAndReading(int kumesId)
        //{
        //    var result = await _mainClassRahmiye.GetPlcConnectedAndReading((Desc.eKumesTanim)kumesId);
        //    //_logger.LogInformation("GetIsPlcConnect metodu �al��t�r�ld�!");
        //    return await Task.FromResult(result);
        //    //return true;
        //}

        //public async Task<List<Tuple<int, bool>>> GetAllPlcConnectedAndReading(List<int> kumesIds)
        //{
        //    var result = await _mainClassRahmiye.GetAllPlcConnectedAndReading(kumesIds);
        //    //_logger.LogInformation("GetIsPlcConnect metodu �al��t�r�ld�!");
        //    return await Task.FromResult(result);
        //    //return true;
        //}

        //public async Task<Tuple<bool, bool>> GetPlcBilgileriByKumesId(int kumesId)
        //{
        //    var result = await _mainClassRahmiye.GetPlcBilgileriByKumesId((Desc.eKumesTanim)kumesId);
        //    return await Task.FromResult(result);
        //}
    }
    public enum AlarmIds : int
    {
        [Description("Aktif Sıcaklık sensör sayısı az")]
        Sicaklik_AktifSensSayisiAz = 22,
        [Description("Düşük Kümes Sıcaklığı")]
        Sicaklik_DusukKumes = 33,
        [Description("Düşük Sensör 1 Sıcaklığı")]
        Sicaklik_DusukSens1 = 44,
        [Description("Düşük Sensör 2 Sıcaklığı")]
        Sicaklik_DusukSens2 = 55,
        [Description("Düşük Sensör 3 Sıcaklığı")]
        Sicaklik_DusukSens3 = 66,
        [Description("Düşük Sensör 4 Sıcaklığı")]
        Sicaklik_DusukSens4 = 77,
        [Description("Düşük Sensör 5 Sıcaklığı")]
        Sicaklik_DusukSens5 = 88,
        [Description("Düşük Sensör 6 Sıcaklığı")]
        Sicaklik_DusukSens6 = 99,
        [Description("Fazla Sensör 1 Sıcaklığı")]
        Sicaklik_FazlaSens1 = 110,
        [Description("Fazla Sensör 2 Sıcaklığı")]
        Sicaklik_FazlaSens2 = 121,
        [Description("Fazla Sensör 3 Sıcaklığı")]
        Sicaklik_FazlaSens3 = 132,
        [Description("Fazla Sensör 4 Sıcaklığı")]
        Sicaklik_FazlaSens4 = 143,
        [Description("Fazla Sensör 5 Sıcaklığı")]
        Sicaklik_FazlaSens5 = 154,
        [Description("Fazla Sensör 6 Sıcaklığı")]
        Sicaklik_FazlaSens6 = 165,
        [Description("Aktif Nem sensör sayısı az")]
        Nem_AktifSensSayisiAz = 176,
        [Description("CO2-Aktif değil")]
        Co2_AktifDegil = 187,
        [Description("Yüksek CO2 değeri")]
        Co2_Fazla = 198,
        [Description("Basınç-Aktif değil")]
        Basinc_AktifDegil = 209,
        [Description("Yüksek Basınç değeri")]
        Basinc_Fazla = 220,
        [Description("İçme Suyu-Sayaç aktif değil")]
        IcmeSuyu_SayacAktifDegil = 231,
        [Description("Fan-Termik NOK")]
        Fan_TermikNOK = 242,
        [Description("Ped-Pompa Termik NOK")]
        Ped_PompaTermikNOK = 253,
        [Description("Sol Buffel Termik NOK")]
        Sag_Buffel_TermikNOK = 264,
        [Description("Sağ Buffel Termik NOK")]
        Sol_Buffel_TermikNOK = 275,
        [Description("Yüksek Kümes Sıcaklığı")]
        Sicaklik_FazlaKumes = 286,
        [Description("Plc Bağlantısı Koptu")]
        Plc_Koptu = 0,
        [Description("Plc Okunmuyor")]
        Plc_Okunmuyor = 11,
        [Description("Şebekede Elektrik Yok")]
        SebekedeElektrikYok = 647,
        [Description("Silo 1 Bağlantısı Koptu")]
        Silo1BaglantiKoptu = 658,
        [Description("Silo 2 Bağlantısı Koptu")]
        Silo2BaglantiKoptu = 669,
        [Description("Günlük ölü hayvan sayısı girilmedi")]
        GunlukOluHayvanSayisiGirilmedi = 680,
        [Description("Hafta sonu ölü hayvan sayısı girilmedi")]
        HaftaSonuOluHayvanSayisiGirilmedi = 873,
        [Description("Elektrik kesildiği için Plc kapandı")]
        ElektrikKesintisiPlcKapandi = 886,
        [Description("Silo 1'de Yem Yükleme öncesi Siloda Beklenenden Fazla Yem Bulunuyor")]
        Silo1YuklemeAniAlarm = 1407,
        [Description("Silo 2'de Yem Yükleme öncesi Siloda Beklenenden Fazla Yem Bulunuyor")]
        Silo2YuklemeAniAlarm = 1418,
        [Description("Silo Değişimi Sonrası Silo 1'de Yem Kaldı")]
        Silo1DegisimAniAlarm = 1429,
        [Description("Silo Değişimi Sonrası Silo 2'de Yem Kaldı")]
        Silo2DegisimAniAlarm = 1440,
        [Description("Silo Değişimi Sonrası Silo 1'de Negatif Değer Tespit Edildi")]
        Silo1NegatifDegerAlarm = 1451,
        [Description("Silo Değişimi Sonrası Silo 2'de Negatif Değer Tespit Edildi")]
        Silo2NegatifDegerAlarm = 1462,
        [Description("İçme suyu akış yok")]
        IcmeSuyuAkisYok = 3212,
    }

    public enum AlarmLogTipler : int
    {
        [Description("Sıcaklık-Aktif sensör sayısı az")]
        Sicaklik_AktifSensSayisiAz = 1,
        [Description("Sıcaklık-Düşük Kümes Sıcaklığı")]
        Sicaklik_DusukKumes = 2,
        [Description("Sıcaklık-Düşük Sensör 1 Sıcaklığı")]
        Sicaklik_DusukSens1 = 3,
        [Description("Sıcaklık-Düşük Sensör 2 Sıcaklığı")]
        Sicaklik_DusukSens2 = 4,
        [Description("Sıcaklık-Düşük Sensör 3 Sıcaklığı")]
        Sicaklik_DusukSens3 = 5,
        [Description("Sıcaklık-Düşük Sensör 4 Sıcaklığı")]
        Sicaklik_DusukSens4 = 6,
        [Description("Sıcaklık-Düşük Sensör 5 Sıcaklığı")]
        Sicaklik_DusukSens5 = 7,
        [Description("Sıcaklık-Düşük Sensör 6 Sıcaklığı")]
        Sicaklik_DusukSens6 = 8,
        [Description("Sıcaklık-Fazla Sensör 1 Sıcaklığı")]
        Sicaklik_FazlaSens1 = 9,
        [Description("Sıcaklık-Fazla Sensör 2 Sıcaklığı")]
        Sicaklik_FazlaSens2 = 10,
        [Description("Sıcaklık-Fazla Sensör 3 Sıcaklığı")]
        Sicaklik_FazlaSens3 = 11,
        [Description("Sıcaklık-Fazla Sensör 4 Sıcaklığı")]
        Sicaklik_FazlaSens4 = 12,
        [Description("Sıcaklık-Fazla Sensör 5 Sıcaklığı")]
        Sicaklik_FazlaSens5 = 13,
        [Description("Sıcaklık-Fazla Sensör 6 Sıcaklığı")]
        Sicaklik_FazlaSens6 = 14,
        [Description("Nem-Aktif sensör sayısı az")]
        Nem_AktifSensSayisiAz = 15,
        [Description("CO2-Aktif değil")]
        Co2_AktifDegil = 16,
        [Description("CO2-Yüksek CO2 değeri")]
        Co2_Fazla = 17,
        [Description("Basınç-Aktif değil")]
        Basinc_AktifDegil = 18,
        [Description("Basınç-Yüksek Basınç değeri")]
        Basinc_Fazla = 19,
        [Description("İçme Suyu-Sayaç aktif değil")]
        IcmeSuyu_SayacAktifDegil = 20,
        [Description("Fan-Termik NOK")]
        Fan_TermikNOK = 21,
        [Description("Ped-Pompa Termik NOK")]
        Ped_PompaTermikNOK = 22,
        [Description("Buffel-Sol Buffel Termik NOK")]
        Sag_Buffel_TermikNOK = 23,
        [Description("Buffel-Sağ Buffel Termik NOK")]
        Sol_Buffel_TermikNOK = 24,
        [Description("Sıcaklık-Yüksek Kümes Sıcaklığı")]
        Sicaklik_FazlaKumes = 25,
        [Description("Plc Bağlantısı Koptu")]
        Plc_Koptu = 26,
        [Description("Plc Okunmuyor")]
        Plc_Okunmuyor = 27,
        [Description("Şebekede Elektrik Yok")]
        SebekedeElektrikYok = 28,
        [Description("Silo 1 Bağlantısı Koptu")]
        Silo1BaglantiKoptu = 29,
        [Description("Silo 2 Bağlantısı Koptu")]
        Silo2BaglantiKoptu = 30,
        [Description("Günlük ölü hayvan sayısı girilmedi")]
        GunlukOluHayvanSayisiGirilmedi = 31,
        [Description("Hafta sonu ölü hayvan sayısı girilmedi")]
        HaftaSonuOluHayvanSayisiGirilmedi = 32,
        [Description("Elektrik kesildiüi için Plc kapandı")]
        ElektrikKesintisiPlcKapandi = 33,
        [Description("Silo 1'de Yem Yükleme öncesi Siloda Beklenenden Fazla Yem Bulunuyor")]
        Silo1YuklemeAniAlarm = 34,
        [Description("Silo 2'de Yem Yükleme öncesi Siloda Beklenenden Fazla Yem Bulunuyor")]
        Silo2YuklemeAniAlarm = 35,
        [Description("Silo Değişimi Sonrası Silo 1'de Yem Kaldı")]
        Silo1DegisimAniAlarm = 36,
        [Description("Silo Değişimi Sonrası Silo 2'de Yem Kaldı")]
        Silo2DegisimAniAlarm = 37,
        [Description("Silo Değişimi Sonrası Silo 1'de Negatif Değer Tespit Edildi")]
        Silo1NegatifDegerAlarm = 38,
        [Description("Silo Değişimi Sonrası Silo 1'de Negatif Değer Tespit Edildi")]
        Silo2NegatifDegerAlarm = 39,
        [Description("İçme suyu akış yok")]
        IcmeSuyuAkisYok = 40,
    }
}