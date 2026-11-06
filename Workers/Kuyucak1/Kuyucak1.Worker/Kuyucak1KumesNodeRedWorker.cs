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
using AlarmLog = PoultryHouseScada.Domain.Models.AlarmLog;
using Kuyucak1.Application.Models;
using Kuyucak1.Infrastructure.Caching;

namespace Kuyucak1.Worker
{
    public class Kuyucak1KumesNodeRedWorker : BackgroundService
    {

        private readonly ILogger<Kuyucak1KumesNodeRedWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly AlarmCacheService _alarmCacheService;
        private readonly SemaphoreSlim _kumesIslemSiniri = new(4, 4);

        private List<Kumes> _kumesListesi = new();

        // Kuyucak-1: KumesId 73..80
        private static readonly int[] KumesIdleri = Enumerable.Range(73, 8).ToArray();

        private readonly Dictionary<int, DateTime> _kumesHayvanLogAylikKayitAtildi = new();
        private readonly Dictionary<int, DateTime> _sonHayvanKayitKontrolZamani = new();
        private readonly Dictionary<int, int?> _gunlukOluHayvanSayilari = new();

        private const string ServisAdi = "Kuyucak1KumesNodeRedWorker";

        private const string PlcKoptuAlarmEslemeSozcugu =
            "Kuyucak1_PLC_KOPTU";

        private const string GunlukOluHayvanSayisiGirilmediAlarmEslemeSozcugu =
            "Kuyucak1_GUNLUK_OLU_HAYVAN_SAYISI_GIRILMEDI";

        private const string HaftaSonuOluHayvanSayisiGirilmediAlarmEslemeSozcugu =
            "Kuyucak1_HAFTA_SONU_OLU_HAYVAN_SAYISI_GIRILMEDI";

        private static readonly TimeSpan PlcVeriZamanAsimi = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan AlarmTanimYenilemeSuresi = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan HayvanKayitKontrolAraligi = TimeSpan.FromMinutes(5);

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

        private readonly record struct AlarmDurumIstegi(
            string AlarmEslemeSozcugu,
            bool Aktif,
            string? Detay = null);

        private static readonly string[] BeklenenAlarmEslemeSozcukleri =
        {
            PlcKoptuAlarmEslemeSozcugu,
            GunlukOluHayvanSayisiGirilmediAlarmEslemeSozcugu,
            HaftaSonuOluHayvanSayisiGirilmediAlarmEslemeSozcugu,
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Basinc_Ust_Limit),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Basinc_Alt_Limit),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_CO2_Limit),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Aktif_Sensor_Sayisi_Az),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Tunel_Fan_TermikNOK),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Mini_Fan_TermikNOK),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Ped_Pompa_TermikNOK),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Nem_Ust_Limit),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Nem_Alt_Limit),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TempHum_1_Aktif_Degil),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TempHum_2_Aktif_Degil),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TempHum_3_Aktif_Degil),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TempHum_4_Aktif_Degil),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Dis_TempHum_Aktif_Degil),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Basinc_Aktif_Degil),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_CO2_Aktif_Degil),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_IcmeSuyu_Aktif_Degil),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sebeke_Yok),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TFan_1_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TFan_2_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TFan_3_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TFan_4_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TFan_5_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TFan_6_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_MFan_1_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_MFan_2_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_MFan_3_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Ped_Pompa_Sol_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Ped_Pompa_Sag_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Buffel_Acilmiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Buffel_Kapanmiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Buffel_TermikNOK),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Aydinlatma_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Aperey_1_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Aperey_2_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Aperey_3_Calismiyor),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Aperey_TermikNOK),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Buffel_Sensor_Ariza),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Ara_Silo_Baglanti_Koptu),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_1_Degeri_Dusuk),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_1_Degeri_Yuksek),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_2_Degeri_Dusuk),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_2_Degeri_Yuksek),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_3_Degeri_Dusuk),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_3_Degeri_Yuksek),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_4_Degeri_Dusuk),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_4_Degeri_Yuksek),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_Dis_Degeri_Dusuk),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_Dis_Degeri_Yuksek),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Ortalama_Sicaklik_Degeri_Dusuk),
            nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Ortalama_Sicaklik_Degeri_Yuksek),
        };

        private static readonly TimeSpan EtiketTanimYenilemeSuresi = TimeSpan.FromDays(1);

        private IReadOnlyDictionary<string, int> _etiketTanimlari =
            new Dictionary<string, int>();

        private DateTime _sonEtiketTanimYuklemeZamani = DateTime.MinValue;
        private readonly SemaphoreSlim _etiketTanimYuklemeKilidi = new(1, 1);

        private const string PlcConnectedEtiketEslemeSozcugu =
            "PLC_CONNECTED";

        private const string PlcReadEtiketEslemeSozcugu =
            "PLC_READ";

        private const string GunlukCanliHayvanSayisiEtiketEslemeSozcugu =
            "Kuyucak1_GUNLUK_CANLI_HAYVAN_SAYISI";

        private const string GunlukOluHayvanSayisiEtiketEslemeSozcugu =
            "Kuyucak1_GUNLUK_OLU_HAYVAN_SAYISI";

        private const string HayvanBasinaSuTuketimEtiketEslemeSozcugu =
            "Kuyucak1_HAYVAN_BASINA_SU_TUKETIM";

        private const string HayvanBasinaYemTuketimEtiketEslemeSozcugu =
            "Kuyucak1_HAYVAN_BASINA_YEM_TUKETIM";

        private static readonly string[] BeklenenEtiketEslemeSozcukleri =
        {
            "PLC_CONNECTED",
            "PLC_READ",
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
            "Kuyucak1_Yem_Silo_1_Aktif",
            "Kuyucak1_Yem_Silo_1_Silo_Value",
            "Kuyucak1_Yem_Silo_1_Silo_Tuketim_Bugun",
            "Kuyucak1_Yem_Silo_1_Silo_VarlikOran",
            "Kuyucak1_Yemleme_Kumes_1_Tuketim_Bugun",
            "Kuyucak1_Yemleme_Kumes_2_Tuketim_Bugun",
            "Kuyucak1_Mini_Fan_1_State",
            "Kuyucak1_Mini_Fan_2_State",
            "Kuyucak1_Mini_Fan_3_State",
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
            "Kuyucak1_GUNLUK_CANLI_HAYVAN_SAYISI",
            "Kuyucak1_GUNLUK_OLU_HAYVAN_SAYISI",
            "Kuyucak1_HAYVAN_BASINA_SU_TUKETIM",
            "Kuyucak1_HAYVAN_BASINA_YEM_TUKETIM",
        };

        public Kuyucak1KumesNodeRedWorker(
            ILogger<Kuyucak1KumesNodeRedWorker> logger,
            IServiceProvider serviceProvider,
            AlarmCacheService alarmCacheService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _alarmCacheService = alarmCacheService;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Kuyucak1 Node-RED anlık worker başlatılıyor. KumesId: {KumesIdleri}",
                    string.Join(",", KumesIdleri));

                await UpdateServisDurumAsync(
                    ServisAdi,
                    2,
                    "Servis başlatılıyor.");

                _kumesListesi = await _alarmCacheService
                    .GetKumeslerByIdsAsync(
                        KumesIdleri,
                        cancellationToken);

                _kumesListesi = _kumesListesi
                    .GroupBy(x => x.Id)
                    .Select(x => x.First())
                    .OrderBy(x => x.Id)
                    .ToList();

                _logger.LogInformation(
                    "Kuyucak1 için toplam {KumesSayisi} kümes bulundu. KumesId: {KumesIdleri}",
                    _kumesListesi.Count,
                    string.Join(",", _kumesListesi.Select(x => x.Id)));

                var eksikKumesIds = KumesIdleri
                    .Except(_kumesListesi.Select(x => x.Id))
                    .ToArray();

                if (eksikKumesIds.Length > 0)
                {
                    _logger.LogWarning(
                        "Kuyucak1 için Kumes tablosunda bulunamayan Id'ler var: {EksikKumesIds}",
                        string.Join(",", eksikKumesIds));
                }

                await AlarmTanimlariniYukleAsync(cancellationToken);
                await EtiketTanimlariniYukleAsync(cancellationToken);

                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kuyucak1 worker başlatılamadı.");
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
                    await AlarmTanimlariniGerekirseYenileAsync(stoppingToken);
                    await EtiketTanimlariniGerekirseYenileAsync(stoppingToken);

                    /*
                     * Aylık KumesHayvanLog placeholder kayıtları kümes başına günde
                     * yalnızca bir kere kontrol edilir.
                     */
                    await AylikKumesHayvanLoglariniGerekirseTamamlaAsync(stoppingToken);

                    /*
                     * Günlük / hafta sonu ölü hayvan giriş kontrolleri 30 saniyelik
                     * SCADA döngüsünden bağımsız olarak seyrek yapılır.
                     */
                    await HayvanKayitAlarmlariniGerekirseKontrolEtAsync(stoppingToken);

                    /*
                     * Canlı hayvan sayısı AlarmCacheService içinde 15 dakika cache'lenir.
                     * Burada kümesler için sıralı alınır; aynı scoped DbContext üzerinde
                     * paralel EF sorgusu çalıştırılmaz.
                     */
                    var canliHayvanSayilari = await CanliHayvanSayilariniGetirAsync(stoppingToken);

                    /*
                     * Kümesleri tek tek beklemek yerine en fazla 4 kümesi aynı anda
                     * hazırlıyoruz. SQL'e ise her kümes için ayrı ayrı değil,
                     * bütün kümeslerin kayıtlarını iki toplu işlemle yazıyoruz.
                     */
                    var hazirlamaGorevleri = _kumesListesi
                        .Select(kumes => KumesVerileriniHazirlaAsync(
                            kumes.Id,
                            canliHayvanSayilari.GetValueOrDefault(kumes.Id),
                            _gunlukOluHayvanSayilari.GetValueOrDefault(kumes.Id),
                            stoppingToken))
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
                     * EtiketLog ve KumesLog tarafı current-state tabloları olarak UPSERT çalışır.
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

                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Kuyucak1 worker döngüsünde hata oluştu.");

                    await UpdateServisDurumAsync(ServisAdi, 0, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private async Task BulkUpsertKumesLogYeniDataAsync(
            SqlConnection connection,
            IEnumerable<KumesLog_TempYeni> data,
            CancellationToken cancellationToken)
        {
            /*
             * KumesLog_TempYeni anlık SCADA tablosudur.
             * Her KumesId için tek güncel satır tutulur.
             *
             * Aynı turda aynı kümese ait birden fazla kayıt oluşursa
             * en güncel KayitZamani olan kayıt kullanılır.
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

                /*
                 * Hedef tablonun şemasını birebir kopyalayan geçici tablo.
                 * Identity değerini stage'e taşımıyoruz; modelde Id olsa bile
                 * aşağıdaki DataTable'da Id kolonu hariç tutulur.
                 */
                const string stageTableSql = @"
SELECT TOP (0) *
INTO #KumesLogStage
FROM dbo.KumesLog_TempYeni;";

                using (var stageCommand = new SqlCommand(
                    stageTableSql,
                    connection,
                    transaction))
                {
                    await stageCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                var dataTable = CreateDataTableWithoutId(dataList);

                // PLC REAL değerlerinden biri SQL decimal kolonunun precision/scale
                // sınırını aşarsa SqlBulkCopy bütün batch'i düşürmesin.
                // Hedef tablo metadata'sına göre decimal değerleri yuvarlar;
                // aralık dışı tek alanı NULL yapıp warning loglar.
                await NormalizeDecimalColumnsForSqlAsync(
                    dataTable,
                    "dbo.KumesLog_TempYeni",
                    connection,
                    transaction,
                    cancellationToken);

                using (var bulkCopy = new SqlBulkCopy(
                    connection,
                    SqlBulkCopyOptions.Default,
                    transaction))
                {
                    bulkCopy.DestinationTableName = "#KumesLogStage";
                    bulkCopy.BulkCopyTimeout = 600;

                    foreach (DataColumn column in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(
                            column.ColumnName,
                            column.ColumnName);
                    }

                    await bulkCopy.WriteToServerAsync(
                        dataTable,
                        cancellationToken);
                }

                /*
                 * KumesLog_TempYeni modelinde zamanla yeni kolonlar eklenebileceği
                 * için UPDATE kolon listesini tablo metadata'sından üretiyoruz.
                 *
                 * Id / KumesId / identity / computed / rowversion kolonları
                 * güncellenmez.
                 */
                const string upsertSql = @"
DECLARE @SetList nvarchar(max);

SELECT @SetList =
    STRING_AGG(
        CAST(
            'hedef.' + QUOTENAME(c.name) +
            ' = kaynak.' + QUOTENAME(c.name)
            AS nvarchar(max)
        ),
        ','
    )
FROM sys.columns c
INNER JOIN tempdb.sys.columns tc
    ON tc.object_id = OBJECT_ID('tempdb..#KumesLogStage')
   AND tc.name COLLATE DATABASE_DEFAULT
       = c.name COLLATE DATABASE_DEFAULT
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
    @InsertColumns = STRING_AGG(
        CAST(QUOTENAME(c.name) AS nvarchar(max)),
        ','
    ),
    @SelectColumns = STRING_AGG(
        CAST('kaynak.' + QUOTENAME(c.name) AS nvarchar(max)),
        ','
    )
FROM sys.columns c
INNER JOIN tempdb.sys.columns tc
    ON tc.object_id = OBJECT_ID('tempdb..#KumesLogStage')
   AND tc.name COLLATE DATABASE_DEFAULT
       = c.name COLLATE DATABASE_DEFAULT
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

                using (var upsertCommand = new SqlCommand(
                    upsertSql,
                    connection,
                    transaction))
                {
                    upsertCommand.CommandTimeout = 600;

                    await upsertCommand.ExecuteNonQueryAsync(
                        cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    try
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    catch
                    {
                        // Asıl hatayı gölgelemiyoruz.
                    }
                }

                _logger.LogError(
                    ex,
                    "KumesLog_TempYeni bulk upsert işlemi başarısız oldu.");
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

                using (var stageCommand = new SqlCommand(
                    stageTableSql,
                    connection,
                    transaction))
                {
                    await stageCommand.ExecuteNonQueryAsync(
                        cancellationToken);
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
                    row["Deger"] = item.Deger.HasValue
                        ? item.Deger.Value
                        : DBNull.Value;

                    dataTable.Rows.Add(row);
                }

                await NormalizeDecimalColumnsForSqlAsync(
                    dataTable,
                    "dbo.EtiketLog_TempYeni",
                    connection,
                    transaction,
                    cancellationToken);

                using (var bulkCopy = new SqlBulkCopy(
                    connection,
                    SqlBulkCopyOptions.Default,
                    transaction))
                {
                    bulkCopy.DestinationTableName = "#EtiketLogStage";
                    bulkCopy.BulkCopyTimeout = 600;

                    bulkCopy.ColumnMappings.Add("KumesId", "KumesId");
                    bulkCopy.ColumnMappings.Add("EtiketId", "EtiketId");
                    bulkCopy.ColumnMappings.Add("KayitZamani", "KayitZamani");
                    bulkCopy.ColumnMappings.Add("Deger", "Deger");

                    await bulkCopy.WriteToServerAsync(
                        dataTable,
                        cancellationToken);
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

                using (var upsertCommand = new SqlCommand(
                    upsertSql,
                    connection,
                    transaction))
                {
                    upsertCommand.CommandTimeout = 600;

                    await upsertCommand.ExecuteNonQueryAsync(
                        cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    try
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    catch
                    {
                    }
                }

                _logger.LogError(
                    ex,
                    "EtiketLog_TempYeni bulk upsert işlemi başarısız oldu.");
            }
            finally
            {
                transaction?.Dispose();

                if (connection.State != ConnectionState.Closed)
                    await connection.CloseAsync();
            }
        }

        private async Task NormalizeDecimalColumnsForSqlAsync(
            DataTable dataTable,
            string tableName,
            SqlConnection connection,
            SqlTransaction transaction,
            CancellationToken cancellationToken)
        {
            /*
             * SqlBulkCopy şu tip hatayı, kaynak decimal değeri hedef decimal/numeric
             * kolonun precision/scale aralığına sığmadığında verir:
             *
             * "The given value of type SqlDecimal from the data source cannot be
             *  converted to type decimal of the specified target column."
             *
             * Temp tablo hedef tablodan kopyalandığı için burada gerçek hedef
             * kolon metadata'sını okuyup DataTable içindeki decimal değerleri
             * SQL'e uygun hale getiriyoruz.
             *
             * Aralık dışı PLC değeri bütün kümes batch'ini durdurmak yerine NULL
             * yazılır ve ayrıntılı warning log bırakılır.
             */

            const string metadataSql = @"
SELECT
    c.name AS ColumnName,
    c.precision AS [Precision],
    c.scale AS [Scale]
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(@TableName)
  AND TYPE_NAME(c.user_type_id) IN ('decimal', 'numeric');";

            var decimalColumns =
                new Dictionary<string, (byte Precision, byte Scale)>(
                    StringComparer.OrdinalIgnoreCase);

            using (var command = new SqlCommand(
                metadataSql,
                connection,
                transaction))
            {
                command.Parameters.AddWithValue(
                    "@TableName",
                    tableName);

                using var reader =
                    await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    decimalColumns[reader.GetString(0)] =
                    (
                        reader.GetByte(1),
                        reader.GetByte(2)
                    );
                }
            }

            foreach (DataColumn column in dataTable.Columns)
            {
                if (!decimalColumns.TryGetValue(
                        column.ColumnName,
                        out var sqlInfo))
                {
                    continue;
                }

                foreach (DataRow row in dataTable.Rows)
                {
                    if (row.IsNull(column))
                        continue;

                    decimal value;

                    try
                    {
                        value = Convert.ToDecimal(row[column]);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Decimal dönüşümü başarısız. Tablo={Tablo}, Kolon={Kolon}, Değer={Deger}. NULL yazılacak.",
                            tableName,
                            column.ColumnName,
                            row[column]);

                        row[column] = DBNull.Value;
                        continue;
                    }

                    var sqlScale = Math.Min((int)sqlInfo.Scale, 28);
                    var rounded =
                        Math.Round(
                            value,
                            sqlScale,
                            MidpointRounding.AwayFromZero);

                    var integerDigits =
                        (int)sqlInfo.Precision - (int)sqlInfo.Scale;

                    var fits = true;

                    /*
                     * .NET decimal yaklaşık 29 anlamlı basamağa kadar çıkar.
                     * SQL kolonunun integer tarafı 29+ basamaksa .NET decimal
                     * aralık kontrolüne ayrıca gerek yok.
                     */
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
                            "SQL decimal aralık dışı değer atlandı. Tablo={Tablo}, Kolon={Kolon}, Precision={Precision}, Scale={Scale}, KumesId={KumesId}, Değer={Deger}. NULL yazılacak.",
                            tableName,
                            column.ColumnName,
                            sqlInfo.Precision,
                            sqlInfo.Scale,
                            kumesId,
                            value);

                        row[column] = DBNull.Value;
                        continue;
                    }

                    row[column] = rounded;
                }
            }
        }

        private static DataTable CreateDataTableWithoutId<T>(
    IEnumerable<T> data)
        {
            var dataTable = new DataTable();
            var accessor = TypeAccessor.Create(typeof(T));

            var members = accessor
                .GetMembers()
                .Where(x =>
                    !string.Equals(
                        x.Name,
                        "Id",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var member in members)
            {
                var type = Nullable.GetUnderlyingType(member.Type)
                           ?? member.Type;

                dataTable.Columns.Add(member.Name, type);
            }

            foreach (var item in data)
            {
                var row = dataTable.NewRow();

                foreach (var member in members)
                {
                    row[member.Name] =
                        accessor[item!, member.Name] ?? DBNull.Value;
                }

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        private static DataTable CreateDataTable<T>(
            IEnumerable<T> data)
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
                    row[member.Name] =
                        accessor[item!, member.Name] ?? DBNull.Value;
                }

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        private async Task<(
            List<EtiketLog_TempYeni> EtiketLoglari,
            List<KumesLog_TempYeni> KumesLoglari)> KumesVerileriniHazirlaAsync(
                int kumesId,
                int gunlukCanliHayvanSayisi,
                int? gunlukOluHayvanSayisi,
                CancellationToken cancellationToken)
        {
            await _kumesIslemSiniri.WaitAsync(cancellationToken);

            try
            {
                var now = DateTime.Now;
                var etiketLoglari = new List<EtiketLog_TempYeni>();
                var kumesLoglari = new List<KumesLog_TempYeni>();

                /*
                 * Aynı kümes için Node-RED kaydı bir kez okunur ve JSON bir kez
                 * deserialize edilir. Eski yapıda EtiketLog ve KumesLog hazırlığı
                 * aynı kaydı iki defa sorguluyordu.
                 */
                await using var scope = _serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider
                    .GetRequiredService<KumesScadaDbContext>();

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

                /*
                 * PLC okunmuyorsa son bilinen proses değerlerini silmiyoruz.
                 * Sadece PLC durum etiketi 0 olur ve PLC alarmı aktifleşir.
                 */
                if (!plcOkuyor)
                {
                    await AlarmDurumlariniSenkronizeEtAsync(
                        kumesId,
                        new[]
                        {
                            new AlarmDurumIstegi(
                                PlcKoptuAlarmEslemeSozcugu,
                                true,
                                "Node-RED kaydı yok, geçersiz veya zaman aşımına uğramış.")
                        },
                        cancellationToken);

                    return (etiketLoglari, kumesLoglari);
                }

                var model = JsonSerializer.Deserialize<Kuyucak1NodeRedModel>(
                    nodeRedKaydi!.Veriler!);

                if (model == null)
                {
                    await AlarmDurumlariniSenkronizeEtAsync(
                        kumesId,
                        new[]
                        {
                            new AlarmDurumIstegi(
                                PlcKoptuAlarmEslemeSozcugu,
                                true,
                                "Node-RED Veriler alanı Kuyucak1NodeRedModel'e dönüştürülemedi.")
                        },
                        cancellationToken);

                    return (etiketLoglari, kumesLoglari);
                }

                /*
                 * PLC kaynaklı bütün alarm bitleri Alarmlar.AlarmEslemeSozcugu ile
                 * gerçek AlarmId'ye çevrilir.
                 */
                var alarmDurumlari = new List<AlarmDurumIstegi>
                {
                    new(
                        PlcKoptuAlarmEslemeSozcugu,
                        false,
                        "PLC/Node-RED verisi güncel.")
                };

                alarmDurumlari.AddRange(KumesAlarmDurumlariniOlustur(model));

                await AlarmDurumlariniSenkronizeEtAsync(
                    kumesId,
                    alarmDurumlari,
                    cancellationToken);

                /*
                 * PLC'deki bugünkü su tüketimi anormal yüksek geldiyse mevcut
                 * sayaçtan son geçerli sayaç referansı çıkarılır.
                 * Su etiket ID'leri statik değildir; EslemeSozcugu cache'inden bulunur.
                 */
                var hesaplananIcmeSuyuTuketimBugun =
                    await IcmeSuyuTuketiminiHesaplaAsync(
                        kumesId,
                        model,
                        cancellationToken);

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
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
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

        private static KumesLog_TempYeni KumesLogOlustur(
            int kumesId,
            DateTime kayitZamani,
            Kuyucak1NodeRedModel model,
            decimal? hesaplananIcmeSuyuTuketimBugun)
        {
            return new KumesLog_TempYeni
            {
                KumesId = kumesId,
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

                // Ara silo stok değeri
                Silo1 = model.Yemleme_Silo_Value / 1000,
                Silo2 = null,

                Basinc1 = model.Basinc_Sens_Value,
                Co2_1 = model.CO2_Sens_Value,

                // Her fiziksel kümes kendi PLC'sindeki Kumes_1 tüketimini kullanır.
                // Kumes_2 kendi tüketimi için kullanılmaz.
                YemTuketimBugun = model.Yemleme_Kumes_1_Tuketim_Bugun / 1000,
                SuTuketimBugun = hesaplananIcmeSuyuTuketimBugun / 100,

                // Kuyucak-1 DB42 anlık bölümünde ped su sayacı yok.
                PedSuTuketimBugun = null
            };
        }

        private void EtiketLoglariniOlustur(
            ICollection<EtiketLog_TempYeni> etiketLoglari,
            int kumesId,
            DateTime kayitZamani,
            Kuyucak1NodeRedModel model,
            int gunlukCanliHayvanSayisi,
            int? gunlukOluHayvanSayisi,
            decimal? hesaplananIcmeSuyuTuketimBugun)
        {
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
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Basinc_Sens_Aktif", kayitZamani, model.Basinc_Sens_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Basinc_Sens_Value", kayitZamani, model.Basinc_Sens_Value);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_CO2_Sens_Aktif", kayitZamani, model.CO2_Sens_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_CO2_Sens_Value", kayitZamani, model.CO2_Sens_Value);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_IcmeSuyu_VanaAc", kayitZamani, model.IcmeSuyu_VanaAc);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_IcmeSuyu_Sayac_Aktif", kayitZamani, model.IcmeSuyu_Sayac_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_IcmeSuyu_Vana_State", kayitZamani, model.IcmeSuyu_Vana_State);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_IcmeSuyu_Sayac", kayitZamani, model.IcmeSuyu_Sayac / 100);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_IcmeSuyu_Tuketim_Bugun", kayitZamani, hesaplananIcmeSuyuTuketimBugun);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_IcmeSuyu_GunSonuSayac_Dun", kayitZamani, model.IcmeSuyu_GunSonuSayac_Dun / 100);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_IcmeSuyu_Tuketim_Dun", kayitZamani, model.IcmeSuyu_Tuketim_Dun / 100);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Yem_Silo_1_Aktif", kayitZamani, model.Yemleme_Aktif);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Yem_Silo_1_Silo_Value", kayitZamani, model.Yemleme_Silo_Value / 1000);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Yem_Silo_1_Silo_Tuketim_Bugun", kayitZamani, model.Yemleme_Tuketim_Bugun / 1000);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Yem_Silo_1_Silo_VarlikOran", kayitZamani, model.Yemleme_VarlikOran);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Yemleme_Kumes_1_Tuketim_Bugun", kayitZamani, model.Yemleme_Kumes_1_Tuketim_Bugun / 1000);
            EtiketEkle(etiketLoglari, kumesId, "Kuyucak1_Yemleme_Kumes_2_Tuketim_Bugun", kayitZamani, model.Yemleme_Kumes_2_Tuketim_Bugun / 1000);
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
            ICollection<EtiketLog_TempYeni> etiketLoglari,
            int kumesId,
            DateTime kayitZamani,
            Kuyucak1NodeRedModel model,
            int gunlukCanliHayvanSayisi,
            int? gunlukOluHayvanSayisi,
            decimal? hesaplananIcmeSuyuTuketimBugun)
        {
            EtiketEkle(etiketLoglari, kumesId,
                GunlukCanliHayvanSayisiEtiketEslemeSozcugu,
                kayitZamani, gunlukCanliHayvanSayisi);

            EtiketEkle(etiketLoglari, kumesId,
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

            EtiketEkle(etiketLoglari, kumesId,
                HayvanBasinaSuTuketimEtiketEslemeSozcugu,
                kayitZamani, hayvanBasinaSuTuketim);

            EtiketEkle(etiketLoglari, kumesId,
                HayvanBasinaYemTuketimEtiketEslemeSozcugu,
                kayitZamani, hayvanBasinaYemTuketim);
        }

        private Task<decimal?> IcmeSuyuTuketiminiHesaplaAsync(
            int kumesId,
            Kuyucak1NodeRedModel model,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(model.IcmeSuyu_Tuketim_Bugun);
        }

        private void EtiketEkle(
            ICollection<EtiketLog_TempYeni> etiketLogs,
            int kumesId,
            string etiketEslemeSozcugu,
            DateTime kayitZamani,
            decimal? deger)
        {
            if (!EtiketIdBul(etiketEslemeSozcugu, out var etiketId))
                return;

            /*
             * EtiketLog_TempYeni artık current-state/upsert tablosu.
             * Kaynak değer null olduğunda kaydı atlamak eski değerin ekranda
             * kalmasına yol açar. Bu nedenle null da açıkça yazılır.
             */
            etiketLogs.Add(
                new EtiketLog_TempYeni
                {
                    KumesId = kumesId,
                    EtiketId = etiketId,
                    KayitZamani = kayitZamani,
                    Deger = deger
                });
        }

        private void EtiketEkle(
            ICollection<EtiketLog_TempYeni> etiketLogs,
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

        private bool EtiketIdBul(
            string etiketEslemeSozcugu,
            out int etiketId)
        {
            var anahtar = EslemeSozcuguNormalizeEt(etiketEslemeSozcugu);

            return _etiketTanimlari.TryGetValue(
                anahtar,
                out etiketId);
        }

        private static IReadOnlyList<AlarmDurumIstegi> KumesAlarmDurumlariniOlustur(
            Kuyucak1NodeRedModel model)
        {
            return new AlarmDurumIstegi[]
            {
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Basinc_Ust_Limit),
                    model.Kuyucak1_Alarm_Basinc_Ust_Limit == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Basinc_Alt_Limit),
                    model.Kuyucak1_Alarm_Basinc_Alt_Limit == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_CO2_Limit),
                    model.Kuyucak1_Alarm_CO2_Limit == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Aktif_Sensor_Sayisi_Az),
                    model.Kuyucak1_Alarm_Aktif_Sensor_Sayisi_Az == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Tunel_Fan_TermikNOK),
                    model.Kuyucak1_Alarm_Tunel_Fan_TermikNOK == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Mini_Fan_TermikNOK),
                    model.Kuyucak1_Alarm_Mini_Fan_TermikNOK == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Ped_Pompa_TermikNOK),
                    model.Kuyucak1_Alarm_Ped_Pompa_TermikNOK == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Nem_Ust_Limit),
                    model.Kuyucak1_Alarm_Nem_Ust_Limit == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Nem_Alt_Limit),
                    model.Kuyucak1_Alarm_Nem_Alt_Limit == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TempHum_1_Aktif_Degil),
                    model.Kuyucak1_Alarm_TempHum_1_Aktif_Degil == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TempHum_2_Aktif_Degil),
                    model.Kuyucak1_Alarm_TempHum_2_Aktif_Degil == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TempHum_3_Aktif_Degil),
                    model.Kuyucak1_Alarm_TempHum_3_Aktif_Degil == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TempHum_4_Aktif_Degil),
                    model.Kuyucak1_Alarm_TempHum_4_Aktif_Degil == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Dis_TempHum_Aktif_Degil),
                    model.Kuyucak1_Alarm_Dis_TempHum_Aktif_Degil == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Basinc_Aktif_Degil),
                    model.Kuyucak1_Alarm_Basinc_Aktif_Degil == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_CO2_Aktif_Degil),
                    model.Kuyucak1_Alarm_CO2_Aktif_Degil == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_IcmeSuyu_Aktif_Degil),
                    model.Kuyucak1_Alarm_IcmeSuyu_Aktif_Degil == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sebeke_Yok),
                    model.Kuyucak1_Alarm_Sebeke_Yok == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TFan_1_Calismiyor),
                    model.Kuyucak1_Alarm_TFan_1_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TFan_2_Calismiyor),
                    model.Kuyucak1_Alarm_TFan_2_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TFan_3_Calismiyor),
                    model.Kuyucak1_Alarm_TFan_3_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TFan_4_Calismiyor),
                    model.Kuyucak1_Alarm_TFan_4_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TFan_5_Calismiyor),
                    model.Kuyucak1_Alarm_TFan_5_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_TFan_6_Calismiyor),
                    model.Kuyucak1_Alarm_TFan_6_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_MFan_1_Calismiyor),
                    model.Kuyucak1_Alarm_MFan_1_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_MFan_2_Calismiyor),
                    model.Kuyucak1_Alarm_MFan_2_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_MFan_3_Calismiyor),
                    model.Kuyucak1_Alarm_MFan_3_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Ped_Pompa_Sol_Calismiyor),
                    model.Kuyucak1_Alarm_Ped_Pompa_Sol_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Ped_Pompa_Sag_Calismiyor),
                    model.Kuyucak1_Alarm_Ped_Pompa_Sag_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Buffel_Acilmiyor),
                    model.Kuyucak1_Alarm_Buffel_Acilmiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Buffel_Kapanmiyor),
                    model.Kuyucak1_Alarm_Buffel_Kapanmiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Buffel_TermikNOK),
                    model.Kuyucak1_Alarm_Buffel_TermikNOK == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Aydinlatma_Calismiyor),
                    model.Kuyucak1_Alarm_Aydinlatma_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Aperey_1_Calismiyor),
                    model.Kuyucak1_Alarm_Aperey_1_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Aperey_2_Calismiyor),
                    model.Kuyucak1_Alarm_Aperey_2_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Aperey_3_Calismiyor),
                    model.Kuyucak1_Alarm_Aperey_3_Calismiyor == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Aperey_TermikNOK),
                    model.Kuyucak1_Alarm_Aperey_TermikNOK == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Buffel_Sensor_Ariza),
                    model.Kuyucak1_Alarm_Buffel_Sensor_Ariza == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Ara_Silo_Baglanti_Koptu),
                    model.Kuyucak1_Alarm_Ara_Silo_Baglanti_Koptu == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_1_Degeri_Dusuk),
                    model.Kuyucak1_Alarm_Sicaklik_Sensor_1_Degeri_Dusuk == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_1_Degeri_Yuksek),
                    model.Kuyucak1_Alarm_Sicaklik_Sensor_1_Degeri_Yuksek == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_2_Degeri_Dusuk),
                    model.Kuyucak1_Alarm_Sicaklik_Sensor_2_Degeri_Dusuk == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_2_Degeri_Yuksek),
                    model.Kuyucak1_Alarm_Sicaklik_Sensor_2_Degeri_Yuksek == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_3_Degeri_Dusuk),
                    model.Kuyucak1_Alarm_Sicaklik_Sensor_3_Degeri_Dusuk == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_3_Degeri_Yuksek),
                    model.Kuyucak1_Alarm_Sicaklik_Sensor_3_Degeri_Yuksek == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_4_Degeri_Dusuk),
                    model.Kuyucak1_Alarm_Sicaklik_Sensor_4_Degeri_Dusuk == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_4_Degeri_Yuksek),
                    model.Kuyucak1_Alarm_Sicaklik_Sensor_4_Degeri_Yuksek == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_Dis_Degeri_Dusuk),
                    model.Kuyucak1_Alarm_Sicaklik_Sensor_Dis_Degeri_Dusuk == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Sicaklik_Sensor_Dis_Degeri_Yuksek),
                    model.Kuyucak1_Alarm_Sicaklik_Sensor_Dis_Degeri_Yuksek == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Ortalama_Sicaklik_Degeri_Dusuk),
                    model.Kuyucak1_Alarm_Ortalama_Sicaklik_Degeri_Dusuk == true),
                new(nameof(Kuyucak1NodeRedModel.Kuyucak1_Alarm_Ortalama_Sicaklik_Degeri_Yuksek),
                    model.Kuyucak1_Alarm_Ortalama_Sicaklik_Degeri_Yuksek == true),
            };
        }

        private async Task EtiketTanimlariniGerekirseYenileAsync(
            CancellationToken cancellationToken)
        {
            var now = DateTime.Now;

            /*
             * Başarılı yüklenen cache 24 saat boyunca kullanılır.
             * Yükleme başarısız olursa son başarılı zaman güncellenmediği için
             * bir sonraki 30 saniyelik worker turunda tekrar denenir.
             */
            if (_etiketTanimlari.Count > 0 &&
                now - _sonEtiketTanimYuklemeZamani <
                EtiketTanimYenilemeSuresi)
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

                /*
                 * Kilidi beklerken başka bir çağrı cache'i yenilemiş olabilir.
                 */
                if (_etiketTanimlari.Count > 0 &&
                    now - _sonEtiketTanimYuklemeZamani <
                    EtiketTanimYenilemeSuresi)
                {
                    return;
                }

                await using var scope = _serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider
                    .GetRequiredService<KumesScadaDbContext>();

                /*
                 * DbSet property adına bağımlı değiliz; entity tipi üzerinden erişiyoruz.
                 * Belleğe tüm Etiket entity'si değil yalnızca Id + EslemeSozcugu alınır.
                 */
                var beklenenEtiketEslemeSozcukleri = BeklenenEtiketEslemeSozcukleri
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var beklenenEtiketAnahtarlari = beklenenEtiketEslemeSozcukleri
                    .Select(EslemeSozcuguNormalizeEt)
                    .ToHashSet();

                /*
                 * Etiket tablosunda binlerce kayıt olsa bile tamamı çekilmez.
                 * Contains, EF Core tarafından SQL IN (...) olarak çevrilir ve
                 * yalnızca bu worker'ın kullandığı EslemeSozcugu kayıtları gelir.
                 *
                 * Bu nedenle Etiket.EslemeSozcugu değerlerini model property adlarıyla
                 * birebir tutmak önemlidir.
                 */
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
                        EslemeAnahtari =
                            EslemeSozcuguNormalizeEt(x.EslemeSozcugu)
                    })
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.EslemeAnahtari) &&
                        beklenenEtiketAnahtarlari.Contains(x.EslemeAnahtari))
                    .ToList();

                var yeniEtiketTanimlari = workerEtiketAdaylari
                    .GroupBy(x => x.EslemeAnahtari)
                    .ToDictionary(
                        grup => grup.Key,
                        grup => grup
                            .OrderBy(x => x.Id)
                            .First()
                            .Id);

                /*
                 * Dictionary bundan sonra mutate edilmez.
                 * Paralel kümes task'ları eski veya yeni dictionary referansını
                 * güvenli şekilde okuyabilir.
                 */
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
                        "Etiket tablosunda EslemeSozcugu tanımı olmayan alanlar var. " +
                        "Bu alanlar EtiketLog_TempYeni'ye yazılmayacak. " +
                        "Eksikler: {EksikEtiketler}",
                        string.Join(", ", eksikEtiketler));
                }

                _logger.LogInformation(
                    "Etiket eşleme cache'i yenilendi. Etiket sayısı: {EtiketSayisi}",
                    _etiketTanimlari.Count);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                /*
                 * Eski cache varsa korunur. Böylece geçici DB hatasında worker
                 * etiket yazmayı tamamen bırakmaz.
                 */
                _logger.LogError(
                    ex,
                    "Etiket eşleme cache'i yenilenemedi. Mevcut cache korunuyor.");
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

        private async Task<Dictionary<int, int>> CanliHayvanSayilariniGetirAsync(
            CancellationToken cancellationToken)
        {
            var sonuc = new Dictionary<int, int>(_kumesListesi.Count);

            foreach (var kumes in _kumesListesi)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    sonuc[kumes.Id] = await _alarmCacheService
                        .GetGunlukCanliHayvanSayisiByKumesId(
                            kumes.Id,
                            cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Kümes {KumesId} canlı hayvan sayısı alınamadı.",
                        kumes.Id);

                    sonuc[kumes.Id] = 0;
                }
            }

            return sonuc;
        }

        private async Task AylikKumesHayvanLoglariniGerekirseTamamlaAsync(
            CancellationToken cancellationToken)
        {
            var bugun = DateTime.Today;

            foreach (var kumes in _kumesListesi)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_kumesHayvanLogAylikKayitAtildi.TryGetValue(
                        kumes.Id,
                        out var sonCalisma) &&
                    sonCalisma.Date == bugun)
                {
                    continue;
                }

                try
                {
                    await AddAylikKumesHayvanLog(
                        kumes.Id,
                        cancellationToken);

                    _kumesHayvanLogAylikKayitAtildi[kumes.Id] = bugun;
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    /*
                     * Başarısızsa tarih dictionary'ye yazılmaz.
                     * Bir sonraki 30 saniyelik turda tekrar denenebilir.
                     */
                    _logger.LogError(
                        ex,
                        "Kümes {KumesId} aylık hayvan log hazırlığı başarısız.",
                        kumes.Id);
                }
            }
        }

        private async Task HayvanKayitAlarmlariniGerekirseKontrolEtAsync(
            CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            var haftaIci =
                now.DayOfWeek >= DayOfWeek.Monday &&
                now.DayOfWeek <= DayOfWeek.Friday;

            foreach (var kumes in _kumesListesi)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_sonHayvanKayitKontrolZamani.TryGetValue(
                        kumes.Id,
                        out var sonKontrol) &&
                    now - sonKontrol < HayvanKayitKontrolAraligi)
                {
                    continue;
                }

                try
                {
                    /*
                     * Bugünkü sayı etikette de kullanılacağı için "kayıt var mı"
                     * ve "kaç adet" bilgisi birlikte güncellenir.
                     */
                    var gunlukKayitGirildiMi = await _alarmCacheService
                        .GetOluHayvanSayisiGirildiMiByKumesId(
                            kumes.Id,
                            cancellationToken);

                    var gunlukOluHayvanSayisi = await _alarmCacheService
                        .GetGunlukOluHayvanSayisiByKumesId(
                            kumes.Id,
                            cancellationToken);

                    /*
                     * Bugünkü ölüm sayısı değiştiyse canlı hayvan cache'i artık
                     * eski kalmıştır. Hemen temizlenir; aynı worker turunda aşağıda
                     * canlı hayvan sayısı yeniden hesaplanır.
                     */
                    var oncekiGunlukOluHayvanSayisi =
                        _gunlukOluHayvanSayilari.GetValueOrDefault(kumes.Id);

                    if (oncekiGunlukOluHayvanSayisi != gunlukOluHayvanSayisi)
                    {
                        _alarmCacheService.ClearHayvanSayisiCache(kumes.Id);
                    }

                    _gunlukOluHayvanSayilari[kumes.Id] =
                        gunlukOluHayvanSayisi;

                    var alarmDurumlari = new List<AlarmDurumIstegi>();

                    /*
                     * Rahmiye davranışı:
                     * hafta içi saat 15:00 sonrası günlük ölüm girişi beklenir.
                     * Hafta sonu bu günlük alarm açık tutulmaz.
                     */
                    var gunlukAlarmAktif =
                        haftaIci &&
                        now.Hour >= 15 &&
                        !gunlukKayitGirildiMi;

                    alarmDurumlari.Add(
                        new AlarmDurumIstegi(
                            GunlukOluHayvanSayisiGirilmediAlarmEslemeSozcugu,
                            gunlukAlarmAktif,
                            gunlukAlarmAktif
                                ? "Saat 15:00 sonrası günlük ölü hayvan sayısı girilmemiş."
                                : "Günlük ölü hayvan kaydı kontrol edildi."));

                    /*
                     * Önceki hafta sonu kaydı pazartesiden itibaren kontrol edilir.
                     * Kullanıcı salı/çarşamba düzeltme yaparsa alarmın kapanabilmesi için
                     * yalnızca pazartesiyle sınırlamıyoruz.
                     */
                    if (haftaIci && now.Hour >= 10)
                    {
                        var haftaSonuKaydiGirildiMi = await _alarmCacheService
                            .GetHaftaSonuOluHayvanSayisiGirildiMiByKumesId(
                                kumes.Id,
                                cancellationToken);

                        alarmDurumlari.Add(
                            new AlarmDurumIstegi(
                                HaftaSonuOluHayvanSayisiGirilmediAlarmEslemeSozcugu,
                                !haftaSonuKaydiGirildiMi,
                                !haftaSonuKaydiGirildiMi
                                    ? "Önceki Cumartesi/Pazar için ölü hayvan kaydı eksik."
                                    : "Önceki hafta sonu ölü hayvan kayıtları tamam."));
                    }

                    await AlarmDurumlariniSenkronizeEtAsync(
                        kumes.Id,
                        alarmDurumlari,
                        cancellationToken);

                    _sonHayvanKayitKontrolZamani[kumes.Id] = now;
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    /*
                     * Başarısız kontrolde son kontrol zamanı güncellenmez;
                     * sonraki worker turunda tekrar denenir.
                     */
                    _logger.LogError(
                        ex,
                        "Kümes {KumesId} hayvan kayıt kontrolleri yapılamadı.",
                        kumes.Id);
                }
            }
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
                 * AlarmId hesaplanmaz; AlarmEslemeSozcugu üzerinden gerçek alarm
                 * tanımı bulunur.
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
                 * Worker'ın kullandığı AlarmEslemeSozcugu değerlerinden eksik olanları
                 * başlangıçta/yenilemede raporla.
                 */
                var eksikAlarmTanimlari = new List<string>();

                foreach (var kumesId in kumesIds)
                {
                    foreach (var alarmEslemeSozcugu in BeklenenAlarmEslemeSozcukleri)
                    {
                        var anahtar = new KumesAlarmTanimAnahtari(
                            kumesId,
                            AlarmEslemeSozcuguNormalizeEt(alarmEslemeSozcugu));

                        if (!alarmTanimlari.ContainsKey(anahtar))
                        {
                            eksikAlarmTanimlari.Add(
                                $"KumesId={kumesId}, AlarmEslemeSozcugu={alarmEslemeSozcugu}");
                        }
                    }
                }

                if (eksikAlarmTanimlari.Count > 0)
                {
                    _logger.LogWarning(
                        "Alarmlar tablosunda eksik AlarmEslemeSozcugu tanımları var. " +
                        "Eksik tanımlar: {EksikAlarmTanimlari}",
                        string.Join(" | ", eksikAlarmTanimlari));
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
        /// Aynı kümese ait alarm durumlarını tek sorgu ve tek SaveChanges ile
        /// Alarmlar.AlarmEslemeSozcugu üzerinden senkronize eder.
        ///
        /// AlarmId hiçbir şekilde KumesId üzerinden hesaplanmaz.
        /// AlarmEnum'a bağımlılık yoktur.
        /// </summary>
        private async Task AlarmDurumlariniSenkronizeEtAsync(
            int kumesId,
            IReadOnlyCollection<AlarmDurumIstegi> alarmDurumlari,
            CancellationToken cancellationToken)
        {
            if (alarmDurumlari.Count == 0)
                return;

            try
            {
                var eslesenAlarmDurumlari = new List<(
                    AlarmDurumIstegi Istek,
                    KumesAlarmTanimi Tanim)>();

                foreach (var alarmDurumu in alarmDurumlari)
                {
                    if (!AlarmTaniminiBul(
                            kumesId,
                            alarmDurumu.AlarmEslemeSozcugu,
                            out var alarmTanimi))
                    {
                        _logger.LogDebug(
                            "Kümes alarm tanımı bulunamadı. " +
                            "KumesId: {KumesId}, AlarmEslemeSozcugu: {AlarmEslemeSozcugu}",
                            kumesId,
                            alarmDurumu.AlarmEslemeSozcugu);

                        continue;
                    }

                    eslesenAlarmDurumlari.Add((alarmDurumu, alarmTanimi));
                }

                if (eslesenAlarmDurumlari.Count == 0)
                    return;

                await using var scope = _serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider
                    .GetRequiredService<KumesScadaDbContext>();

                /*
                 * Kümese ait aktif alarm kayıtları bir kere alınır.
                 * Sonrasında gerçek AlarmId üzerinden bellekte eşleştirme yapılır.
                 */
                var aktifAlarmKayitlari = await dbContext.AlarmLog
                    .Where(x =>
                        x.KumesId == kumesId &&
                        x.Aktif != false &&
                        x.Silindi != true)
                    .ToListAsync(cancellationToken);

                var degisiklikVar = false;
                var now = DateTime.Now;

                foreach (var (istek, alarmTanimi) in eslesenAlarmDurumlari)
                {
                    var aktifAlarmlar = aktifAlarmKayitlari
                        .Where(x => x.AlarmId == alarmTanimi.AlarmId)
                        .ToList();

                    if (istek.Aktif)
                    {
                        if (aktifAlarmlar.Count == 0)
                        {
                            var yeniAlarm = new AlarmLog
                            {
                                KayitZamani = now,
                                AlarmSeviye = 0,
                                KumesId = kumesId,
                                AlarmId = alarmTanimi.AlarmId,
                                AlarmEnum = null,
                                Mesaj = alarmTanimi.Mesaj,
                                KonveyorId = null,
                                Aktif = true,
                                Silindi = false
                            };

                            await dbContext.AlarmLog.AddAsync(
                                yeniAlarm,
                                cancellationToken);

                            aktifAlarmKayitlari.Add(yeniAlarm);
                            degisiklikVar = true;

                            _logger.LogWarning(
                                "Kümes alarmı oluşturuldu. " +
                                "KumesId: {KumesId}, AlarmId: {AlarmId}, " +
                                "AlarmEslemeSozcugu: {AlarmEslemeSozcugu}, Detay: {Detay}",
                                kumesId,
                                alarmTanimi.AlarmId,
                                alarmTanimi.AlarmEslemeSozcugu,
                                istek.Detay);

                            continue;
                        }

                        foreach (var aktifAlarm in aktifAlarmlar)
                        {
                            if (aktifAlarm.Mesaj == alarmTanimi.Mesaj)
                                continue;

                            aktifAlarm.Mesaj = alarmTanimi.Mesaj;
                            degisiklikVar = true;
                        }

                        continue;
                    }

                    foreach (var aktifAlarm in aktifAlarmlar)
                    {
                        aktifAlarm.Aktif = false;
                        aktifAlarm.PasifZamani = now;
                        degisiklikVar = true;
                    }
                }

                if (!degisiklikVar)
                    return;

                await dbContext.SaveChangesAsync(cancellationToken);
                _alarmCacheService.ClearMemoryCache(kumesId);
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
                    "Kümes alarm durumları güncellenemedi. KumesId: {KumesId}",
                    kumesId);
            }
        }

        private async Task AddAylikKumesHayvanLog(
            int kumesId,
            CancellationToken cancellationToken)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider
                .GetRequiredService<KumesScadaDbContext>();

            var now = DateTime.Now;
            var ayinIlkGunu = new DateTime(now.Year, now.Month, 1);
            var sonrakiAyinIlkGunu = ayinIlkGunu.AddMonths(1);

            /*
             * Silindi=true placeholder kayıtları da mevcut kabul edilir.
             * Böylece her çalışmada aynı gün için tekrar placeholder üretilmez.
             */
            var mevcutKayitGunleri = (await dbContext.KumesHayvanLog
                .AsNoTracking()
                .Where(x =>
                    x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                    x.KumesId == kumesId &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value >= ayinIlkGunu &&
                    x.KayitZamani.Value < sonrakiAyinIlkGunu)
                .Select(x => x.KayitZamani!.Value.Date)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var eklenecekKayitlar = new List<KumesHayvanLog>();

            for (var gun = ayinIlkGunu;
                 gun < sonrakiAyinIlkGunu;
                 gun = gun.AddDays(1))
            {
                if (mevcutKayitGunleri.Contains(gun.Date))
                    continue;

                eklenecekKayitlar.Add(
                    new KumesHayvanLog
                    {
                        KumesId = kumesId,
                        DonemId = null,
                        Adet = 0,
                        SaglikDurumu = KumesHayvanSaglikDurum.Olu,
                        GirisZamani = null,
                        KayitZamani = gun,
                        KayitYapanUserId = null,

                        /*
                         * Placeholder gerçek kullanıcı girişi sayılmasın diye Silindi=true.
                         * Günlük ölüm alarm sorguları sadece Silindi != true kayıtları sayar.
                         */
                        Silindi = true
                    });
            }

            if (eklenecekKayitlar.Count == 0)
                return;

            await dbContext.KumesHayvanLog.AddRangeAsync(
                eklenecekKayitlar,
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Kümes {KumesId} için {KayitSayisi} adet aylık hayvan log placeholder kaydı oluşturuldu.",
                kumesId,
                eklenecekKayitlar.Count);
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