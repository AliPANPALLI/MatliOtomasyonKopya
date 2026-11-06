using FastMember;
using Kuyucak1LogWorker.Core;
using Kuyucak1LogWorker.Models;
using KumesScada.Domain.Models;
using KumesScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;

namespace Kuyucak1LogWorker
{
    public class Kuyucak1TesisLogNodeRedWorker : BackgroundService
    {
        private readonly ILogger<Kuyucak1TesisLogNodeRedWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly CacheService _cacheService;
        private readonly SemaphoreSlim _kumesIslemSiniri = new(4, 4);

        // Kuyucak-1: 172.20.45.10 ... 172.20.45.80 -> KumesId 73 ... 80
        private static readonly int[] KumesIdleri = Enumerable.Range(73, 8).ToArray();

        private const string ServisAdi = "Kuyucak1TesisLogWorker";
        private static readonly TimeSpan LogAraligi = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan PlcVeriZamanAsimi = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan EtiketTanimYenilemeSuresi = TimeSpan.FromDays(1);

        private IReadOnlyDictionary<string, int> _etiketTanimlari =
            new Dictionary<string, int>();

        private DateTime _sonEtiketTanimYuklemeZamani = DateTime.MinValue;
        private readonly SemaphoreSlim _etiketTanimYuklemeKilidi = new(1, 1);

        private const string PlcConnectedEtiketEslemeSozcugu = "PLC_CONNECTED";
        private const string PlcReadEtiketEslemeSozcugu = "PLC_READ";
        private const string GunlukCanliHayvanSayisiEtiketEslemeSozcugu = "Kuyucak1_GUNLUK_CANLI_HAYVAN_SAYISI";
        private const string GunlukOluHayvanSayisiEtiketEslemeSozcugu = "Kuyucak1_GUNLUK_OLU_HAYVAN_SAYISI";
        private const string HayvanBasinaSuTuketimEtiketEslemeSozcugu = "Kuyucak1_HAYVAN_BASINA_SU_TUKETIM";
        private const string HayvanBasinaYemTuketimEtiketEslemeSozcugu = "Kuyucak1_HAYVAN_BASINA_YEM_TUKETIM";

        /*
         * Buradaki isimler Etiket.EslemeSozcugu değerleridir.
         * Kuyucak-1'e ait tüm etiket eşleme sözcükleri Kuyucak1_ prefix'i ile tutulur.
         * Node-RED JSON alan adları prefixsiz kalır; worker JSON modelinden aldığı değeri
         * Kuyucak1_ prefiksli Etiket kaydıyla eşleştirir. Etiket tablosunda tanım yoksa
         * worker hata vermez, uyarı yazar ve ilgili alanı atlar.
         */
        private static readonly string[] BeklenenEtiketEslemeSozcukleri =
        {
            PlcConnectedEtiketEslemeSozcugu,
            PlcReadEtiketEslemeSozcugu,

            "Kuyucak1_Sicaklik_AktifSensSayisi",
            "Kuyucak1_Sicaklik_Kumes_Ortalama",
            "Kuyucak1_Sicaklik_Dis_Aktif",
            "Kuyucak1_Sicaklik_Dis",
            "Kuyucak1_Sicaklik_Sensor_1_Aktif",
            "Kuyucak1_Sicaklik_Sensor_1_Value",
            "Kuyucak1_Sicaklik_Sensor_2_Aktif",
            "Kuyucak1_Sicaklik_Sensor_2_Value",
            "Kuyucak1_Sicaklik_Sensor_3_Aktif",
            "Kuyucak1_Sicaklik_Sensor_3_Value",
            "Kuyucak1_Sicaklik_Sensor_4_Aktif",
            "Kuyucak1_Sicaklik_Sensor_4_Value",
            "Kuyucak1_Sicaklik_Aperey_Min_Olculen",
            "Kuyucak1_Sicaklik_Aperey_Max_Olculen",

            "Kuyucak1_Nem_AktifSensSayisi",
            "Kuyucak1_Nem_Kumes_Ortalama",
            "Kuyucak1_Nem_Dis_Aktif",
            "Kuyucak1_Nem_Dis",
            "Kuyucak1_Nem_Sensor_1_Aktif",
            "Kuyucak1_Nem_Sensor_1_Value",
            "Kuyucak1_Nem_Sensor_2_Aktif",
            "Kuyucak1_Nem_Sensor_2_Value",
            "Kuyucak1_Nem_Sensor_3_Aktif",
            "Kuyucak1_Nem_Sensor_3_Value",
            "Kuyucak1_Nem_Sensor_4_Aktif",
            "Kuyucak1_Nem_Sensor_4_Value",

            "Kuyucak1_Basinc_Sens_Aktif",
            "Kuyucak1_Basinc_Sens_Value",
            "Kuyucak1_CO2_Sens_Aktif",
            "Kuyucak1_CO2_Sens_Value",

            "Kuyucak1_IcmeSuyu_VanaAc",
            "Kuyucak1_IcmeSuyu_Sayac_Aktif",
            "Kuyucak1_IcmeSuyu_Vana_State",
            "Kuyucak1_IcmeSuyu_Sayac",
            "Kuyucak1_IcmeSuyu_Tuketim_Bugun",
            "Kuyucak1_IcmeSuyu_GunSonuSayac_Dun",
            "Kuyucak1_IcmeSuyu_Tuketim_Dun",

            // Kuyucak-1 silo etiketleri
            "Kuyucak1_Yem_Silo_1_Aktif",
            "Kuyucak1_Yem_Silo_1_Silo_Value",
            "Kuyucak1_Yem_Silo_1_Silo_Tuketim_Bugun",
            "Kuyucak1_Yem_Silo_1_Silo_VarlikOran",
            "Kuyucak1_Yemleme_Kumes_1_Tuketim_Bugun",
            "Kuyucak1_Yemleme_Kumes_2_Tuketim_Bugun",

            // Kuyucak MFan -> Kuyucak1_Mini_Fan etiketleri
            "Kuyucak1_Mini_Fan_1_State",
            "Kuyucak1_Mini_Fan_2_State",
            "Kuyucak1_Mini_Fan_3_State",

            // Kuyucak TFan -> Kuyucak1_Fan etiketleri
            "Kuyucak1_Fan_1_State",
            "Kuyucak1_Fan_2_State",
            "Kuyucak1_Fan_3_State",
            "Kuyucak1_Fan_4_State",
            "Kuyucak1_Fan_5_State",
            "Kuyucak1_Fan_6_State",
            "Kuyucak1_Havalandirma_Mode",

            "Kuyucak1_Ped_Pompa_1_State",
            "Kuyucak1_Ped_Pompa_2_State",
            "Kuyucak1_Buffel_Durum",
            "Kuyucak1_Aydinlatma_State",
            "Kuyucak1_Soba_Aperey_1_State",
            "Kuyucak1_Soba_Aperey_2_State",
            "Kuyucak1_Soba_Aperey_3_State",
            "Kuyucak1_Soba_Hat_Sicakligi",
            "Kuyucak1_SebekedeElektrikVar",

            GunlukCanliHayvanSayisiEtiketEslemeSozcugu,
            GunlukOluHayvanSayisiEtiketEslemeSozcugu,
            HayvanBasinaSuTuketimEtiketEslemeSozcugu,
            HayvanBasinaYemTuketimEtiketEslemeSozcugu
        };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public Kuyucak1TesisLogNodeRedWorker(
            ILogger<Kuyucak1TesisLogNodeRedWorker> logger,
            IServiceProvider serviceProvider,
            CacheService cacheService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _cacheService = cacheService;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Kuyucak-1 tesis log worker başlatılıyor. KumesId: {KumesIdleri}",
                    string.Join(",", KumesIdleri));

                await UpdateServisDurumAsync(ServisAdi, 2, "Servis başlatılıyor.");
                await EtiketTanimlariniYukleAsync(cancellationToken);
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kuyucak-1 tesis log worker başlatılamadı.");
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
                    await EtiketTanimlariniGerekirseYenileAsync(stoppingToken);

                    var hazirlamaGorevleri = KumesIdleri
                        .Select(kumesId => KumesLoglariniHazirlaAsync(kumesId, stoppingToken))
                        .ToArray();

                    var hazirlananlar = await Task.WhenAll(hazirlamaGorevleri);

                    var etiketLoglari = hazirlananlar
                        .SelectMany(x => x.EtiketLoglari)
                        .ToList();

                    var kumesLoglari = hazirlananlar
                        .SelectMany(x => x.KumesLoglari)
                        .ToList();

                    await LoglariTopluInsertEtAsync(
                        etiketLoglari,
                        kumesLoglari,
                        stoppingToken);

                    await UpdateServisDurumAsync(
                        ServisAdi,
                        1,
                        $"Servis çalışıyor. EtiketLogYeni: {etiketLoglari.Count}, KumesLogYeni: {kumesLoglari.Count}",
                        true);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kuyucak-1 tesis log worker döngüsünde hata oluştu.");
                    await UpdateServisDurumAsync(ServisAdi, 0, ex.Message);
                }

                try
                {
                    await Task.Delay(LogAraligi, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task<(
            List<EtiketLogYeni> EtiketLoglari,
            List<KumesLogYeni> KumesLoglari)> KumesLoglariniHazirlaAsync(
                int kumesId,
                CancellationToken cancellationToken)
        {
            await _kumesIslemSiniri.WaitAsync(cancellationToken);

            try
            {
                var now = DateTime.Now;
                var etiketLoglari = new List<EtiketLogYeni>();
                var kumesLoglari = new List<KumesLogYeni>();

                await using var scope = _serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

                var nodeRedKaydi = await dbContext.KumesIzlemeNodeRed
                    .AsNoTracking()
                    .Where(x =>
                        x.KumesId == kumesId &&
                        x.KayitZamani.HasValue &&
                        x.KayitZamani.Value >= now.Date &&
                        x.KayitZamani.Value < now.Date.AddDays(1))
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                var plcOkuyor = PlcOkuyorMu(nodeRedKaydi, now);
                var plcDurum = plcOkuyor ? 1m : 0m;

                EtiketEkle(
                    etiketLoglari,
                    kumesId,
                    PlcConnectedEtiketEslemeSozcugu,
                    now,
                    plcDurum);

                EtiketEkle(
                    etiketLoglari,
                    kumesId,
                    PlcReadEtiketEslemeSozcugu,
                    now,
                    plcDurum);

                if (!plcOkuyor)
                    return (etiketLoglari, kumesLoglari);

                Kuyucak1NodeRedModel? model;
                try
                {
                    model = JsonSerializer.Deserialize<Kuyucak1NodeRedModel>(
                        nodeRedKaydi!.Veriler!,
                        JsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(
                        ex,
                        "Kuyucak-1 Kümes {KumesId} Node-RED JSON verisi parse edilemedi.",
                        kumesId);
                    return (etiketLoglari, kumesLoglari);
                }

                if (model == null)
                    return (etiketLoglari, kumesLoglari);

                var gunlukCanliHayvanSayisi = await _cacheService
                    .GetGunlukCanliHayvanSayisiByKumesId(kumesId, cancellationToken);

                var gunlukOluHayvanSayisi = await _cacheService
                    .GetGunlukOluHayvanSayisiByKumesId(kumesId, cancellationToken);

                var hesaplananIcmeSuyuTuketimBugun =
                    IcmeSuyuTuketiminiHesapla(model);

                EtiketLoglariniOlustur(
                    etiketLoglari,
                    kumesId,
                    now,
                    model,
                    gunlukCanliHayvanSayisi,
                    gunlukOluHayvanSayisi,
                    hesaplananIcmeSuyuTuketimBugun);

                kumesLoglari.Add(
                    KumesLogOlustur(
                        kumesId,
                        now,
                        model,
                        hesaplananIcmeSuyuTuketimBugun));

                return (etiketLoglari, kumesLoglari);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Kuyucak-1 Kümes {KumesId} tarihsel log verisi hazırlanamadı.",
                    kumesId);
                return (new List<EtiketLogYeni>(), new List<KumesLogYeni>());
            }
            finally
            {
                _kumesIslemSiniri.Release();
            }
        }

        private async Task LoglariTopluInsertEtAsync(
            IReadOnlyCollection<EtiketLogYeni> etiketLoglari,
            IReadOnlyCollection<KumesLogYeni> kumesLoglari,
            CancellationToken cancellationToken)
        {
            if (etiketLoglari.Count == 0 && kumesLoglari.Count == 0)
                return;

            using var scope = _serviceProvider.CreateScope();
            var connection = scope.ServiceProvider.GetRequiredService<SqlConnection>();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            using var transaction = connection.BeginTransaction();

            try
            {
                if (etiketLoglari.Count > 0)
                {
                    await BulkInsertAsync(
                        connection,
                        transaction,
                        etiketLoglari,
                        "dbo.EtiketLogYeni",
                        cancellationToken);
                }

                if (kumesLoglari.Count > 0)
                {
                    await BulkInsertAsync(
                        connection,
                        transaction,
                        kumesLoglari,
                        "dbo.KumesLogYeni",
                        cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Kuyucak-1 tarihsel logları yazıldı. EtiketLogYeni: {EtiketSayisi}, KumesLogYeni: {KumesLogSayisi}",
                    etiketLoglari.Count,
                    kumesLoglari.Count);
            }
            catch
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch
                {
                    // Asıl hatayı gölgelemiyoruz.
                }

                throw;
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                    await connection.CloseAsync();
            }
        }

        private async Task BulkInsertAsync<T>(
            SqlConnection connection,
            SqlTransaction transaction,
            IEnumerable<T> data,
            string destinationTable,
            CancellationToken cancellationToken)
        {
            var dataList = data.ToList();
            if (dataList.Count == 0)
                return;

            var columnNames = new List<string>();

            // Hedef tablonun decimal/numeric precision-scale bilgisini de alıyoruz.
            // Böylece SqlBulkCopy öncesinde kaynak değeri hedef SQL tipine göre
            // normalize edebiliriz.
            var decimalColumns =
                new Dictionary<string, (byte Precision, byte Scale)>(
                    StringComparer.OrdinalIgnoreCase);

            const string columnSql = @"
SELECT
    c.name,
    TYPE_NAME(c.user_type_id) AS TypeName,
    c.precision,
    c.scale
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(@TableName)
  AND c.is_identity = 0
  AND c.is_computed = 0
  AND c.system_type_id <> 189
ORDER BY c.column_id;";

            using (var columnCommand = new SqlCommand(columnSql, connection, transaction))
            {
                columnCommand.Parameters.Add("@TableName", SqlDbType.NVarChar, 256).Value = destinationTable;

                using var reader = await columnCommand.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var columnName = reader.GetString(0);
                    var typeName = reader.GetString(1);

                    columnNames.Add(columnName);

                    if (typeName.Equals("decimal", StringComparison.OrdinalIgnoreCase) ||
                        typeName.Equals("numeric", StringComparison.OrdinalIgnoreCase))
                    {
                        decimalColumns[columnName] =
                        (
                            reader.GetByte(2),
                            reader.GetByte(3)
                        );
                    }
                }
            }

            if (columnNames.Count == 0)
                throw new InvalidOperationException($"{destinationTable} tablosunun yazılabilir kolonları bulunamadı.");

            var accessor = TypeAccessor.Create(typeof(T));
            var memberMap = accessor
                .GetMembers()
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            var members = columnNames
                .Where(memberMap.ContainsKey)
                .Select(x => memberMap[x])
                .ToArray();

            if (members.Length == 0)
                throw new InvalidOperationException($"{typeof(T).Name} ile {destinationTable} arasında ortak kolon bulunamadı.");

            var dataTable = new DataTable();

            foreach (var member in members)
            {
                var type = Nullable.GetUnderlyingType(member.Type) ?? member.Type;
                dataTable.Columns.Add(member.Name, type);
            }

            foreach (var item in dataList)
            {
                var row = dataTable.NewRow();

                foreach (var member in members)
                {
                    row[member.Name] = accessor[item!, member.Name] ?? DBNull.Value;
                }

                dataTable.Rows.Add(row);
            }

            // Decimal/numeric kolonları hedef SQL precision/scale sınırlarına göre düzelt.
            foreach (var decimalColumn in decimalColumns)
            {
                if (!dataTable.Columns.Contains(decimalColumn.Key))
                    continue;

                var precision = decimalColumn.Value.Precision;
                var scale = decimalColumn.Value.Scale;
                var integerDigits = (int)precision - (int)scale;
                var targetScale = Math.Min((int)scale, 28);

                foreach (DataRow row in dataTable.Rows)
                {
                    if (row.IsNull(decimalColumn.Key))
                        continue;

                    decimal value;

                    try
                    {
                        value = Convert.ToDecimal(row[decimalColumn.Key]);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Kuyucak-1 decimal dönüşümü başarısız. Tablo={Tablo}, Kolon={Kolon}, Değer={Deger}. NULL yazılacak.",
                            destinationTable,
                            decimalColumn.Key,
                            row[decimalColumn.Key]);

                        row[decimalColumn.Key] = DBNull.Value;
                        continue;
                    }

                    var rounded =
                        Math.Round(
                            value,
                            targetScale,
                            MidpointRounding.AwayFromZero);

                    var fits = true;

                    // .NET decimal yaklaşık 29 anlamlı basamak destekler.
                    // Hedef integer kısmı bundan küçükse güvenli aralık kontrolü yap.
                    if (integerDigits < 29)
                    {
                        decimal exclusiveLimit = 1m;

                        for (var i = 0; i < integerDigits; i++)
                            exclusiveLimit *= 10m;

                        fits =
                            rounded > -exclusiveLimit &&
                            rounded < exclusiveLimit;
                    }

                    if (!fits)
                    {
                        object? kumesId = null;

                        if (dataTable.Columns.Contains("KumesId") &&
                            !row.IsNull("KumesId"))
                        {
                            kumesId = row["KumesId"];
                        }

                        _logger.LogWarning(
                            "Kuyucak-1 SQL decimal aralık dışı değer atlandı. Tablo={Tablo}, Kolon={Kolon}, Precision={Precision}, Scale={Scale}, KumesId={KumesId}, Değer={Deger}. NULL yazılacak.",
                            destinationTable,
                            decimalColumn.Key,
                            precision,
                            scale,
                            kumesId,
                            value);

                        row[decimalColumn.Key] = DBNull.Value;
                        continue;
                    }

                    row[decimalColumn.Key] = rounded;
                }
            }

            using var bulkCopy = new SqlBulkCopy(
                connection,
                SqlBulkCopyOptions.Default,
                transaction)
            {
                DestinationTableName = destinationTable,
                BulkCopyTimeout = 600
            };

            foreach (DataColumn column in dataTable.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
        }

        private static KumesLogYeni KumesLogOlustur(
            int kumesId,
            DateTime kayitZamani,
            Kuyucak1NodeRedModel model,
            decimal? hesaplananIcmeSuyuTuketimBugun)
        {
            return new KumesLogYeni
            {
                KumesId = kumesId,
                // Kayışlar worker ile aynı davranış korunmuştur.
                DonemId = 1,
                KayitZamani = kayitZamani,

                SicaklikKumes = model.Sicaklik_Kumes_Ortalama,
                SicaklikDis = model.Sicaklik_Dis,
                Sicaklik1 = model.Sicaklik_Sensor_1_Value,
                Sicaklik2 = model.Sicaklik_Sensor_2_Value,
                Sicaklik3 = model.Sicaklik_Sensor_3_Value,
                Sicaklik4 = model.Sicaklik_Sensor_4_Value,
                Sicaklik5 = null,
                Sicaklik6 = null,
                Sicaklik7 = null,
                Sicaklik8 = null,

                NemKumes = model.Nem_Kumes_Ortalama,
                NemDis = model.Nem_Dis,
                Nem1 = model.Nem_Sensor_1_Value,
                Nem2 = model.Nem_Sensor_2_Value,
                Nem3 = model.Nem_Sensor_3_Value,
                Nem4 = model.Nem_Sensor_4_Value,
                Nem5 = null,
                Nem6 = null,

                // Ara silo stok değeri. Mevcut /1000 ölçek davranışı korunmuştur.
                Silo1 = model.Yemleme_Silo_Value / 1000,
                Silo2 = null,
                Basinc1 = model.Basinc_Sens_Value,
                Co2_1 = model.CO2_Sens_Value,
                YemTuketimBugun = model.Yemleme_Kumes_1_Tuketim_Bugun / 1000,
                SuTuketimBugun = hesaplananIcmeSuyuTuketimBugun,

                // Kuyucak DB42 anlık alanlarında ped su sayacı/tüketimi yok.
                PedSuTuketimBugun = null,
                PedSuSayac = null,
                IcmeSuyuSayac = model.IcmeSuyu_Sayac / 100
            };
        }

        private void EtiketLoglariniOlustur(
            ICollection<EtiketLogYeni> etiketLoglari,
            int kumesId,
            DateTime kayitZamani,
            Kuyucak1NodeRedModel model,
            int gunlukCanliHayvanSayisi,
            int? gunlukOluHayvanSayisi,
            decimal? hesaplananIcmeSuyuTuketimBugun)
        {
            // Sıcaklık
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Sicaklik_AktifSensSayisi", kayitZamani, model.Sicaklik_AktifSensSayisi);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Sicaklik_Kumes_Ortalama", kayitZamani, model.Sicaklik_Kumes_Ortalama);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Sicaklik_Dis_Aktif", kayitZamani, model.Sicaklik_Dis_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Sicaklik_Dis", kayitZamani, model.Sicaklik_Dis);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Sicaklik_Sensor_1_Aktif", kayitZamani, model.Sicaklik_Sensor_1_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Sicaklik_Sensor_1_Value", kayitZamani, model.Sicaklik_Sensor_1_Value);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Sicaklik_Sensor_2_Aktif", kayitZamani, model.Sicaklik_Sensor_2_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Sicaklik_Sensor_2_Value", kayitZamani, model.Sicaklik_Sensor_2_Value);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Sicaklik_Sensor_3_Aktif", kayitZamani, model.Sicaklik_Sensor_3_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Sicaklik_Sensor_3_Value", kayitZamani, model.Sicaklik_Sensor_3_Value);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Sicaklik_Sensor_4_Aktif", kayitZamani, model.Sicaklik_Sensor_4_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Sicaklik_Sensor_4_Value", kayitZamani, model.Sicaklik_Sensor_4_Value);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Sicaklik_Aperey_Min_Olculen", kayitZamani, model.Sicaklik_Aperey_Min_Olculen);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Sicaklik_Aperey_Max_Olculen", kayitZamani, model.Sicaklik_Aperey_Max_Olculen);

            // Nem
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Nem_AktifSensSayisi", kayitZamani, model.Nem_AktifSensSayisi);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Nem_Kumes_Ortalama", kayitZamani, model.Nem_Kumes_Ortalama);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Nem_Dis_Aktif", kayitZamani, model.Nem_Dis_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Nem_Dis", kayitZamani, model.Nem_Dis);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Nem_Sensor_1_Aktif", kayitZamani, model.Nem_Sensor_1_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Nem_Sensor_1_Value", kayitZamani, model.Nem_Sensor_1_Value);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Nem_Sensor_2_Aktif", kayitZamani, model.Nem_Sensor_2_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Nem_Sensor_2_Value", kayitZamani, model.Nem_Sensor_2_Value);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Nem_Sensor_3_Aktif", kayitZamani, model.Nem_Sensor_3_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Nem_Sensor_3_Value", kayitZamani, model.Nem_Sensor_3_Value);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Nem_Sensor_4_Aktif", kayitZamani, model.Nem_Sensor_4_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Nem_Sensor_4_Value", kayitZamani, model.Nem_Sensor_4_Value);

            // Basınç / CO2
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Basinc_Sens_Aktif", kayitZamani, model.Basinc_Sens_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Basinc_Sens_Value", kayitZamani, model.Basinc_Sens_Value);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_CO2_Sens_Aktif", kayitZamani, model.CO2_Sens_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_CO2_Sens_Value", kayitZamani, model.CO2_Sens_Value);

            // İçme suyu
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_IcmeSuyu_VanaAc", kayitZamani, model.IcmeSuyu_VanaAc);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_IcmeSuyu_Sayac_Aktif", kayitZamani, model.IcmeSuyu_Sayac_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_IcmeSuyu_Vana_State", kayitZamani, model.IcmeSuyu_Vana_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_IcmeSuyu_Sayac", kayitZamani, model.IcmeSuyu_Sayac / 100);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_IcmeSuyu_Tuketim_Bugun", kayitZamani, hesaplananIcmeSuyuTuketimBugun);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_IcmeSuyu_GunSonuSayac_Dun", kayitZamani, model.IcmeSuyu_GunSonuSayac_Dun / 100);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_IcmeSuyu_Tuketim_Dun", kayitZamani, model.IcmeSuyu_Tuketim_Dun / 100);

            // Yem / silo
            // Yemleme_Silo_Value, Yemleme_Tuketim_Bugun ve Yemleme_VarlikOran ARA SİLO bilgisidir.
            // Her fiziksel kümesin kendi günlük yem tüketimi ise kendi PLC'sindeki
            // Yemleme_Kumes_1_Tuketim_Bugun alanıdır.
            // Kumes_2 alanı ham/diagnostic değer olarak ayrıca loglanmaya devam eder.
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Yem_Silo_1_Aktif", kayitZamani, model.Yemleme_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Yem_Silo_1_Silo_Value", kayitZamani, model.Yemleme_Silo_Value / 1000);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Yem_Silo_1_Silo_Tuketim_Bugun", kayitZamani, model.Yemleme_Tuketim_Bugun / 1000);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Yem_Silo_1_Silo_VarlikOran", kayitZamani, model.Yemleme_VarlikOran);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Yemleme_Kumes_1_Tuketim_Bugun", kayitZamani, model.Yemleme_Kumes_1_Tuketim_Bugun / 1000);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Yemleme_Kumes_2_Tuketim_Bugun", kayitZamani, model.Yemleme_Kumes_2_Tuketim_Bugun / 1000);

            // Fanlar - JSON alanları Kuyucak1_ prefiksli Etiket kayıtlarına map edilir.
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Mini_Fan_1_State", kayitZamani, model.MFan_1_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Mini_Fan_2_State", kayitZamani, model.MFan_2_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Mini_Fan_3_State", kayitZamani, model.MFan_3_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Fan_1_State", kayitZamani, model.TFan_1_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Fan_2_State", kayitZamani, model.TFan_2_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Fan_3_State", kayitZamani, model.TFan_3_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Fan_4_State", kayitZamani, model.TFan_4_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Fan_5_State", kayitZamani, model.TFan_5_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Fan_6_State", kayitZamani, model.TFan_6_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Havalandirma_Mode", kayitZamani, model.Havalandirma_Mode);

            // Ped / Buffel / Aydınlatma / Soba / Elektrik
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Ped_Pompa_1_State", kayitZamani, model.Ped_Pompa_1_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Ped_Pompa_2_State", kayitZamani, model.Ped_Pompa_2_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Buffel_Durum", kayitZamani, model.Buffel_Durum);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Aydinlatma_State", kayitZamani, model.Aydinlatma_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Soba_Aperey_1_State", kayitZamani, model.Soba_Aperey_1_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Soba_Aperey_2_State", kayitZamani, model.Soba_Aperey_2_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Soba_Aperey_3_State", kayitZamani, model.Soba_Aperey_3_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Soba_Hat_Sicakligi", kayitZamani, model.Soba_Hat_Sicakligi);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_SebekedeElektrikVar", kayitZamani, model.SebekedeElektrikVar);

            HesaplanmisEtiketleriEkle(
                etiketLoglari,
                kumesId,
                kayitZamani,
                model,
                gunlukCanliHayvanSayisi,
                gunlukOluHayvanSayisi,
                hesaplananIcmeSuyuTuketimBugun);
        }

        private void HesaplanmisEtiketleriEkle(
            ICollection<EtiketLogYeni> etiketLoglari,
            int kumesId,
            DateTime kayitZamani,
            Kuyucak1NodeRedModel model,
            int gunlukCanliHayvanSayisi,
            int? gunlukOluHayvanSayisi,
            decimal? hesaplananIcmeSuyuTuketimBugun)
        {
            EtiketEkle(
                etiketLoglari,
                kumesId,
                GunlukCanliHayvanSayisiEtiketEslemeSozcugu,
                kayitZamani,
                gunlukCanliHayvanSayisi);

            EtiketEkle(
                etiketLoglari,
                kumesId,
                GunlukOluHayvanSayisiEtiketEslemeSozcugu,
                kayitZamani,
                gunlukOluHayvanSayisi.HasValue
                    ? gunlukOluHayvanSayisi.Value
                    : (decimal?)null);

            var hayvanBasinaSuTuketim =
                gunlukCanliHayvanSayisi <= 0 ||
                !hesaplananIcmeSuyuTuketimBugun.HasValue
                    ? (decimal?)null
                    : (hesaplananIcmeSuyuTuketimBugun.Value * 10_000m) /
                      gunlukCanliHayvanSayisi;

            var hayvanBasinaYemTuketim =
                gunlukCanliHayvanSayisi <= 0 ||
                !model.Yemleme_Kumes_1_Tuketim_Bugun.HasValue
                    ? (decimal?)null
                    : (model.Yemleme_Kumes_1_Tuketim_Bugun.Value * 1_000m) /
                      gunlukCanliHayvanSayisi;

            EtiketEkle(
                etiketLoglari,
                kumesId,
                HayvanBasinaSuTuketimEtiketEslemeSozcugu,
                kayitZamani,
                hayvanBasinaSuTuketim);

            EtiketEkle(
                etiketLoglari,
                kumesId,
                HayvanBasinaYemTuketimEtiketEslemeSozcugu,
                kayitZamani,
                hayvanBasinaYemTuketim);
        }

        private static decimal? IcmeSuyuTuketiminiHesapla(Kuyucak1NodeRedModel model)
        {
            // Kayışlar worker ile aynı ölçek davranışı.
            return model.IcmeSuyu_Tuketim_Bugun / 100;
        }

        private void EtiketEkle(
            ICollection<EtiketLogYeni> etiketLogs,
            int kumesId,
            string etiketEslemeSozcugu,
            DateTime kayitZamani,
            decimal? deger)
        {
            if (!EtiketIdBul(etiketEslemeSozcugu, out var etiketId))
                return;

            etiketLogs.Add(
                new EtiketLogYeni
                {
                    KumesId = kumesId,
                    EtiketId = etiketId,
                    KayitZamani = kayitZamani,
                    Deger = deger
                });
        }

        private void EtiketEkle(
            ICollection<EtiketLogYeni> etiketLogs,
            int kumesId,
            string etiketEslemeSozcugu,
            DateTime kayitZamani,
            bool? deger)
        {
            EtiketEkle(
                etiketLogs,
                kumesId,
                etiketEslemeSozcugu,
                kayitZamani,
                deger.HasValue
                    ? (deger.Value ? 1m : 0m)
                    : (decimal?)null);
        }

        private bool EtiketIdBul(string etiketEslemeSozcugu, out int etiketId)
        {
            var anahtar = EslemeSozcuguNormalizeEt(etiketEslemeSozcugu);
            return _etiketTanimlari.TryGetValue(anahtar, out etiketId);
        }

        private async Task EtiketTanimlariniGerekirseYenileAsync(
            CancellationToken cancellationToken)
        {
            var now = DateTime.Now;

            if (_etiketTanimlari.Count > 0 &&
                now - _sonEtiketTanimYuklemeZamani < EtiketTanimYenilemeSuresi)
            {
                return;
            }

            await EtiketTanimlariniYukleAsync(cancellationToken);
        }

        private async Task EtiketTanimlariniYukleAsync(
            CancellationToken cancellationToken)
        {
            await _etiketTanimYuklemeKilidi.WaitAsync(cancellationToken);

            try
            {
                var now = DateTime.Now;

                if (_etiketTanimlari.Count > 0 &&
                    now - _sonEtiketTanimYuklemeZamani < EtiketTanimYenilemeSuresi)
                {
                    return;
                }

                await using var scope = _serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider
                    .GetRequiredService<KumesScadaDbContext>();

                var beklenenEtiketEslemeSozcukleri = BeklenenEtiketEslemeSozcukleri
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var beklenenEtiketAnahtarlari = beklenenEtiketEslemeSozcukleri
                    .Select(EslemeSozcuguNormalizeEt)
                    .ToHashSet();

                var etiketAdaylari = await dbContext
                    .Set<Etiket>()
                    .AsNoTracking()
                    .Where(x =>
                        x.EslemeSozcugu != null &&
                        beklenenEtiketEslemeSozcukleri.Contains(x.EslemeSozcugu))
                    .Select(x => new
                    {
                        x.Id,
                        x.EslemeSozcugu
                    })
                    .ToListAsync(cancellationToken);

                var workerEtiketAdaylari = etiketAdaylari
                    .Select(x => new
                    {
                        x.Id,
                        x.EslemeSozcugu,
                        EslemeAnahtari = EslemeSozcuguNormalizeEt(x.EslemeSozcugu)
                    })
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.EslemeAnahtari) &&
                        beklenenEtiketAnahtarlari.Contains(x.EslemeAnahtari))
                    .ToList();

                var yeniEtiketTanimlari = workerEtiketAdaylari
                    .GroupBy(x => x.EslemeAnahtari)
                    .ToDictionary(
                        grup => grup.Key,
                        grup => grup.OrderBy(x => x.Id).First().Id);

                _etiketTanimlari = yeniEtiketTanimlari;
                _sonEtiketTanimYuklemeZamani = DateTime.Now;

                var tekrarliEtiketler = workerEtiketAdaylari
                    .GroupBy(x => x.EslemeAnahtari)
                    .Where(grup => grup.Count() > 1)
                    .Select(grup =>
                        $"{grup.First().EslemeSozcugu} -> " +
                        string.Join(",", grup.Select(x => x.Id).OrderBy(x => x)))
                    .ToList();

                if (tekrarliEtiketler.Count > 0)
                {
                    _logger.LogWarning(
                        "Etiket tablosunda aynı EslemeSozcugu için birden fazla kayıt var. " +
                        "En küçük EtiketId kullanıldı. Tanımlar: {EtiketTanimlari}",
                        string.Join(" | ", tekrarliEtiketler));
                }

                var eksikEtiketler = BeklenenEtiketEslemeSozcukleri
                    .Where(eslemeSozcugu =>
                        !yeniEtiketTanimlari.ContainsKey(
                            EslemeSozcuguNormalizeEt(eslemeSozcugu)))
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                if (eksikEtiketler.Count > 0)
                {
                    _logger.LogWarning(
                        "Etiket tablosunda EslemeSozcugu tanımı olmayan Kuyucak-1 alanları var. " +
                        "Bu alanlar EtiketLogYeni'ye yazılmayacak. Eksikler: {EksikEtiketler}",
                        string.Join(", ", eksikEtiketler));
                }

                _logger.LogInformation(
                    "Kuyucak-1 etiket eşleme cache'i yenilendi. Etiket sayısı: {EtiketSayisi}",
                    _etiketTanimlari.Count);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Kuyucak-1 etiket eşleme cache'i yenilenemedi. Mevcut cache korunuyor.");
            }
            finally
            {
                _etiketTanimYuklemeKilidi.Release();
            }
        }

        private static string EslemeSozcuguNormalizeEt(string? deger)
        {
            if (string.IsNullOrWhiteSpace(deger))
                return string.Empty;

            return new string(
                deger
                    .Where(char.IsLetterOrDigit)
                    .Select(char.ToUpperInvariant)
                    .ToArray());
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

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UpdateServisDurumAsync(ServisAdi, 3, "Servis durduruldu");
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kuyucak-1 tesis log worker durdurulurken hata oluştu.");
                await UpdateServisDurumAsync(ServisAdi, 0, ex.Message);
            }
        }

        private async Task UpdateServisDurumAsync(
            string servisName,
            byte durum,
            string mesaj,
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
                _logger.LogError(
                    ex,
                    "Servis durum bilgisi güncellenemedi. Servis: {ServisAdi}",
                    servisName);
            }
        }
    }
}
