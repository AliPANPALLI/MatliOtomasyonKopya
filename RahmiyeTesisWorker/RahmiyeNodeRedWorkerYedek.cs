using FastMember;
using KumesScada.Domain.Models;
using KumesScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RahmiyeTesisWorker.Core;
using RahmiyeTesisWorker.Models;
using SharedKernel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using static SharedKernel.ApplicationConstants;

namespace RahmiyeTesisWorker
{
    public class RahmiyeNodeRedWorkerYedek : BackgroundService
    {
        private readonly ILogger<RahmiyeNodeRedWorker> _logger;
        private readonly SqlConnection _sqlConnection;
        private readonly IServiceProvider _serviceProvider;
        private readonly AlarmCacheService _alarmCacheService;
        private List<Kumes> kumesListesi = new List<Kumes>();
        private Dictionary<int, EggActualDbModel?> yumurtaBilgileriDict = new Dictionary<int, EggActualDbModel?>();
        private List<ValueTuple<int, bool, DateTime>> hayvanSayisiMailGonderildi = new List<ValueTuple<int, bool, DateTime>>();
        private readonly string _vbapConnStr;

        public RahmiyeNodeRedWorkerYedek(ILogger<RahmiyeNodeRedWorker> logger, SqlConnection sqlConnection, IServiceProvider serviceProvider, AlarmCacheService alarmCacheService, IConfiguration configuration)
        {
            _vbapConnStr = configuration.GetConnectionString("SqlServerVbap");
            _logger = logger;
            _sqlConnection = sqlConnection;
            _alarmCacheService = alarmCacheService;
            _serviceProvider = serviceProvider;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("RahmiyeKumesWorker working: {time}", DateTimeOffset.Now);
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
            }
        }

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
                        //var dataAlarm = await GetAlarmLogDataForBulkInsert(kumesListesi[i].Id, stoppingToken);
                        //await BulkInsertAlarmLogDataAsync(_sqlConnection, dataAlarm);
                        //var dataAlarmUretilmis = await GetUretilmisAlarmLogDataForKumes(kumesListesi[i].Id, stoppingToken);
                        //await BulkInsertAlarmLogDataAsync(_sqlConnection, dataAlarmUretilmis);
                        ////var dataEtiketLog = await GetEtiketLogDataForBulkInsert(kumesListesi[i].Id, stoppingToken);
                        ////await BulkInsertEtiketLogDataAsync(_sqlConnection, dataEtiketLog);
                        //var dataEtiketLogYeni = await GetEtiketLogYeniDataForBulkInsert(kumesListesi[i].Id, stoppingToken);
                        //await BulkInsertEtiketLogYeniDataAsync(_sqlConnection, dataEtiketLogYeni);
                        ////var dataKumesLog = await GetKumesLogDataForBulkInsertAsync(kumesListesi[i].Id, stoppingToken);
                        ////if (dataKumesLog.Count() > 0) await BulkInsertKumesLogDataAsync(_sqlConnection, dataKumesLog);
                        var dataKumesLogYeni = await GetKumesLogYeniDataForBulkInsertAsync(kumesListesi[i].Id, stoppingToken);
                        //if (dataKumesLogYeni.Count() > 0) await BulkInsertKumesLogYeniDataAsync(_sqlConnection, dataKumesLogYeni);
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
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
                var accessor = TypeAccessor.Create(typeof(KumesLog_Temp)); // FastMember ile eriþim

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

                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                // SqlBulkCopy ile veriyi MSSQL'e ekleyelim
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "KumesLog_Temp"; // Verinin ekleneceði tablo adý
                    // Dinamik kolon eþleþtirmesi
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

        public async Task BulkInsertKumesLogYeniDataAsync(SqlConnection connection, IEnumerable<KumesLog_TempYeni> data)
        {
            try
            {
                var dataTable = new DataTable();
                var accessor = TypeAccessor.Create(typeof(KumesLog_TempYeni)); // FastMember ile eriþim

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

                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                // SqlBulkCopy ile veriyi MSSQL'e ekleyelim
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "KumesLog_TempYeni"; // Verinin ekleneceði tablo adý
                    // Dinamik kolon eþleþtirmesi
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

        public async Task BulkInsertEtiketLogDataAsync(SqlConnection connection, IEnumerable<EtiketLog_Temp> data)
        {
            try
            {
                var dataTable = new DataTable();
                var accessor = TypeAccessor.Create(typeof(EtiketLog_Temp)); // FastMember ile eriþim

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

                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                // SqlBulkCopy ile veriyi MSSQL'e ekleyelim
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "EtiketLog_Temp"; // Verinin ekleneceði tablo adý
                    bulkCopy.BulkCopyTimeout = 600;
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
                if (connection.State != ConnectionState.Closed)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task BulkInsertEtiketLogYeniDataAsync(SqlConnection connection, IEnumerable<EtiketLog_TempYeni> data)
        {
            try
            {
                var dataTable = new DataTable();
                var accessor = TypeAccessor.Create(typeof(EtiketLog_TempYeni)); // FastMember ile eriþim

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

                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                // SqlBulkCopy ile veriyi MSSQL'e ekleyelim
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "EtiketLog_TempYeni"; // Verinin ekleneceði tablo adý
                    bulkCopy.BulkCopyTimeout = 600;
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
                if (connection.State != ConnectionState.Closed)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task BulkInsertAlarmLogDataAsync(SqlConnection connection, IEnumerable<AlarmLog> data)
        {
            try
            {
                var dataTable = new DataTable();
                var accessor = TypeAccessor.Create(typeof(AlarmLog)); // FastMember ile eriþim

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

                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                // SqlBulkCopy ile veriyi MSSQL'e ekleyelim
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "AlarmLog"; // Verinin ekleneceði tablo adý
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

                    model = await kumesScadaDbContext.KumesIzlemeNodeRed.AsNoTracking().AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                    if (model != null)
                    {
                        if (model.Durum == 1)
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

                                // PLC'den alýnan verileri burada döndürüyoruz
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
                    model = await kumesScadaDbContext.KumesIzlemeNodeRed.AsNoTracking().AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                    var gunlukHayvanSayisi = await _alarmCacheService.GetGunlukCanliHayvanSayisiByKumesId(kumesId);
                    var yumurtaBilgileri = GetYumurtaBilgiHelper(kumesId);

                    if (model != null)
                    {
                        if (model.Durum == 1)
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

                                //if (icmeSuyuTuketim.GetValueOrDefault(0) > 100)
                                //{
                                //    var lastCounter = await _alarmCacheService.GetDunIcmeSuyuSayacByKumesId(kumesId);
                                //    var currentCounter = kumesNodeRedModel.IcmeSuyu_Sayac.GetValueOrDefault(0);

                                //    decimal fark = 0;
                                //    fark = currentCounter >= lastCounter ? currentCounter - lastCounter : currentCounter;
                                //    icmeSuyuTuketim = fark;
                                //}

                                // PLC'den alýnan verileri burada döndürüyoruz
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
                                           YumurtaAdet=yumurtaBilgileri==null ? null :yumurtaBilgileri.KumesYumurtaAdet,
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
                                           YumurtaAdet=yumurtaBilgileri==null ? null :yumurtaBilgileri.KumesYumurtaAdet,
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

        //        // PLC'den alýnan verileri burada döndürüyoruz
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
                    model = await kumesScadaDbContext.KumesIzlemeNodeRed.AsNoTracking().AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
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
                        //        PlcLogYaz((int)mainClassRahmiyeKumes.KumesTanim + " numaralý kümeste plc baðlantýsý koptu");
                        //        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Plc_Koptu + kumesId, AlarmEnum = (int?)AlarmLogTipler.Plc_Koptu, Mesaj = AlarmLogTipler.Plc_Koptu.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                        //        _alarmCacheService.ClearMemoryCache(kumesId);
                        //    }
                        //}
                        //else
                        //{
                        //    if (plcConnected == 1)
                        //    {
                        //        PlcLogYaz((int)mainClassRahmiyeKumes.KumesTanim + " numaralý kümeste plc baðlantýsý saðlandý");
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
                        //        PlcLogYaz(kumesId + " numaralý kümeste plc baðlantýsý koptu");
                        //        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Plc_Koptu + kumesId, AlarmEnum = (int?)AlarmLogTipler.Plc_Koptu, Mesaj = AlarmLogTipler.Plc_Koptu.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                        //        _alarmCacheService.ClearMemoryCache(kumesId);
                        //    }
                        //}
                        //else
                        //{
                        //    if (plcConnected == 1)
                        //    {
                        //        PlcLogYaz(kumesId + " numaralý kümeste plc baðlantýsý saðlandý");
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

                        if (model.Durum == 1)
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
                    model = await kumesScadaDbContext.KumesIzlemeNodeRed.AsNoTracking().AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
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
                        //        PlcLogYaz((int)mainClassRahmiyeKumes.KumesTanim + " numaralý kümeste plc baðlantýsý koptu");
                        //        alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Plc_Koptu + kumesId, AlarmEnum = (int?)AlarmLogTipler.Plc_Koptu, Mesaj = AlarmLogTipler.Plc_Koptu.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                        //        _alarmCacheService.ClearMemoryCache(kumesId);
                        //    }
                        //}
                        //else
                        //{
                        //    if (plcConnected == 1)
                        //    {
                        //        PlcLogYaz((int)mainClassRahmiyeKumes.KumesTanim + " numaralý kümeste plc baðlantýsý saðlandý");
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
                                PlcLogYaz(kumesId + " numaralý kümeste plc baðlantýsý koptu");
                                alarmLogs.Add(new AlarmLog { KayitZamani = now, AlarmSeviye = 0, KumesId = kumesId, AlarmId = (int)AlarmIds.Plc_Koptu + kumesId, AlarmEnum = (int?)AlarmLogTipler.Plc_Koptu, Mesaj = AlarmLogTipler.Plc_Koptu.GetDescription(), KonveyorId = null, Aktif = true, Silindi = false });
                                _alarmCacheService.ClearMemoryCache(kumesId);
                            }
                        }
                        else
                        {
                            if (plcConnected == 1)
                            {
                                PlcLogYaz(kumesId + " numaralý kümeste plc baðlantýsý saðlandý");
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

                        if (model.Durum == 1)
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
                    model = await kumesScadaDbContext.KumesIzlemeNodeRed.AsNoTracking().AsNoTracking().Where(x => x.KumesId == kumesId && x.KayitZamani.Value.Date == now.Date).OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
                    var activeAlarms = await _alarmCacheService.GetAlarmLogsAsync(kumesId/*, now*/);
                    if (model != null)
                    {
                        if (model.Durum == 1)
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
                                //alarmý silmek lazým 24 sonrasý                  
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

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker stop: {time}", DateTimeOffset.Now);
            //StopAsync metodu, veritabaný baðlantýlarýný düzgün bir þekilde kapatmak,
            //açýlmýþ baðlantýlarý serbest býrakmak, dýþ hizmetlerle olan baðlantýlarý kapatmak veya
            //iþlem tamamlanmadan önce yapýlmasý gereken bitiþ iþlemleri için kullanýlýr.
            await base.StopAsync(cancellationToken);
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
            const string sql = @"SELECT TOP (1) SizeS, SizeM, SizeL, SizeXL, SizeNone, BrokenEgg, DirtEggClean, DirtEggLow, DirtEggMedium, DirtEggHigh, DirtBloodClean, DirtBloodLow, DirtBloodMedium, DirtBloodHigh, DirtFecesClean, DirtFecesLow,
                                DirtFecesMedium, DirtFecesHigh FROM dbo.EggActual WITH (NOLOCK) WHERE LocationId = @kumesId AND CAST(LastModificationTime as date) = CAST(@day as date) ORDER BY LastModificationTime DESC;";

            await using var conn = new SqlConnection(_vbapConnStr);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@kumesId", SqlDbType.Int).Value = kumesId;
            cmd.Parameters.Add("@day", SqlDbType.DateTime2).Value = day;

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct);
            if (!await reader.ReadAsync(ct)) return null;

            return new EggActualDbModel
            {
                SAdet = ReadIntNullable(reader, "SizeS"),
                MAdet = ReadIntNullable(reader, "SizeM"),
                LAdet = ReadIntNullable(reader, "SizeL"),
                XLAdet = ReadIntNullable(reader, "SizeXL"),
                AsiriKucukAdet = ReadIntNullable(reader, "SizeNone"),
                KirikYumurtaAdet = ReadIntNullable(reader, "BrokenEgg"),
                HicKirliYumurtaAdet = ReadIntNullable(reader, "DirtEggClean"),
                AzKirliYumurtaAdet = ReadIntNullable(reader, "DirtEggLow"),
                OrtaKirliYumurtaAdet = ReadIntNullable(reader, "DirtEggMedium"),
                CokKirliYumurtaAdet = ReadIntNullable(reader, "DirtEggHigh"),
                HicKanliYumurtaAdet = ReadIntNullable(reader, "DirtBloodClean"),
                AzKanliYumurtaAdet = ReadIntNullable(reader, "DirtBloodLow"),
                OrtaKanliYumurtaAdet = ReadIntNullable(reader, "DirtBloodMedium"),
                CokKanliYumurtaAdet = ReadIntNullable(reader, "DirtBloodHigh"),
                HicDiskiliYumurtaAdet = ReadIntNullable(reader, "DirtFecesClean"),
                AzDiskiliYumurtaAdet = ReadIntNullable(reader, "DirtFecesLow"),
                OrtaDiskiliYumurtaAdet = ReadIntNullable(reader, "DirtFecesMedium"),
                CokDiskiliYumurtaAdet = ReadIntNullable(reader, "DirtFecesHigh"),
            };
        }

        //public async Task Disconnect(int kumesId)
        //{
        //    await _mainClassRahmiye.Change_Connection((Desc.eKumesTanim)kumesId, Desc.eConnection.Disconnect, _sqlConnection.ConnectionString);
        //    _logger.LogInformation("Disconnect metodu çalýþtýrýldý!");
        //    var kumesler = await _alarmCacheService.GetKumeslerAsync(12);
        //    kumesListesi = _mainClassRahmiye.FindKumeslerByTcIds(kumesler.Select(x => x.KumesTcId).ToList());
        //}

        //public async Task Connect(int kumesId)
        //{
        //    await _mainClassRahmiye.Change_Connection((Desc.eKumesTanim)kumesId, Desc.eConnection.Connect, _sqlConnection.ConnectionString);
        //    _logger.LogInformation("Connect metodu çalýþtýrýldý!");
        //    var kumesler = await _alarmCacheService.GetKumeslerAsync(12);
        //    kumesListesi = _mainClassRahmiye.FindKumeslerByTcIds(kumesler.Select(x => x.KumesTcId).ToList());
        //}
        //public async Task CheckFlag(int kumesId)
        //{
        //    await _mainClassRahmiye.CheckFlag((Desc.eKumesTanim)kumesId, _sqlConnection.ConnectionString);
        //    _logger.LogInformation("CheckFlag metodu çalýþtýrýldý!");
        //    var kumesler = await _alarmCacheService.GetKumeslerAsync(12);
        //    kumesListesi = _mainClassRahmiye.FindKumeslerByTcIds(kumesler.Select(x => x.KumesTcId).ToList());
        //}

        //public async Task<bool> GetIsPlcReading(int kumesId)
        //{
        //    var result = await _mainClassRahmiye.GetPlcRead((Desc.eKumesTanim)kumesId);
        //    //_logger.LogInformation("GetIsPlcReading metodu çalýþtýrýldý!");
        //    return await Task.FromResult(result);
        //    //return true;
        //}

        //public async Task<bool> GetIsPlcConnect(int kumesId)
        //{
        //    var result = await _mainClassRahmiye.GetPlcConnected((Desc.eKumesTanim)kumesId);
        //    //_logger.LogInformation("GetIsPlcConnect metodu çalýþtýrýldý!");
        //    return await Task.FromResult(result);
        //    //return true;
        //}

        //public async Task<bool> GetPlcConnectedAndReading(int kumesId)
        //{
        //    var result = await _mainClassRahmiye.GetPlcConnectedAndReading((Desc.eKumesTanim)kumesId);
        //    //_logger.LogInformation("GetIsPlcConnect metodu çalýþtýrýldý!");
        //    return await Task.FromResult(result);
        //    //return true;
        //}

        //public async Task<List<Tuple<int, bool>>> GetAllPlcConnectedAndReading(List<int> kumesIds)
        //{
        //    var result = await _mainClassRahmiye.GetAllPlcConnectedAndReading(kumesIds);
        //    //_logger.LogInformation("GetIsPlcConnect metodu çalýþtýrýldý!");
        //    return await Task.FromResult(result);
        //    //return true;
        //}

        //public async Task<Tuple<bool, bool>> GetPlcBilgileriByKumesId(int kumesId)
        //{
        //    var result = await _mainClassRahmiye.GetPlcBilgileriByKumesId((Desc.eKumesTanim)kumesId);
        //    return await Task.FromResult(result);
        //}
    }
}