using FastMember;
using Kayislar1LogWorker.Core;
using Kayislar1LogWorker.Models;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;

namespace Kayislar1LogWorker
{
    public class Kayislar1TesisLogNodeRedWorker : BackgroundService
    {
        private readonly ILogger<Kayislar1TesisLogNodeRedWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly CacheService _cacheService;
        private readonly SemaphoreSlim _kumesIslemSiniri = new(4, 4);

        private List<Kumes> _kumesListesi = new();

        private const int LokasyonId = 32;
        private const string ServisAdi = "Kayislar1TesisLogWorker";
        private static readonly TimeSpan LogAraligi = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan PlcVeriZamanAsimi = TimeSpan.FromMinutes(3);

        private static readonly TimeSpan EtiketTanimYenilemeSuresi = TimeSpan.FromDays(1);

        private IReadOnlyDictionary<string, int> _etiketTanimlari =
            new Dictionary<string, int>();

        private DateTime _sonEtiketTanimYuklemeZamani = DateTime.MinValue;
        private readonly SemaphoreSlim _etiketTanimYuklemeKilidi = new(1, 1);

        private const string PlcConnectedEtiketEslemeSozcugu = "PLC_CONNECTED";
        private const string PlcReadEtiketEslemeSozcugu = "PLC_READ";

        /*
         * PLC'den direkt gelmeyen, worker tarafýndan hesaplanan etiketler.
         */
        private const string GunlukCanliHayvanSayisiEtiketEslemeSozcugu =
            "GUNLUK_CANLI_HAYVAN_SAYISI";

        private const string GunlukOluHayvanSayisiEtiketEslemeSozcugu =
            "GUNLUK_OLU_HAYVAN_SAYISI";

        private const string HayvanBasinaSuTuketimEtiketEslemeSozcugu =
            "HAYVAN_BASINA_SU_TUKETIM";

        private const string HayvanBasinaYemTuketimEtiketEslemeSozcugu =
            "HAYVAN_BASINA_YEM_TUKETIM";

        /*
         * Bunlar worker'ýn EtiketLogYeni'ya yazmaya çalýþtýðý alanlardýr.
         * EslemeSozcugu olarak mümkün olduðunca KumesNodeRedModel property adý
         * kullanýlýr. Böylece PLC/Node-RED modeli ile Etiket tablosu okunabilir kalýr.
         */
        private static readonly string[] BeklenenEtiketEslemeSozcukleri =
        {
            PlcConnectedEtiketEslemeSozcugu,
            PlcReadEtiketEslemeSozcugu,

            nameof(KumesNodeRedModel.Sicaklik_Kumes_Ortalama),
            nameof(KumesNodeRedModel.Sicaklik_Dis_Aktif),
            nameof(KumesNodeRedModel.Sicaklik_Dis),
            nameof(KumesNodeRedModel.Sicaklik_Sensor_1_Aktif),
            nameof(KumesNodeRedModel.Sicaklik_Sensor_1_Value),
            nameof(KumesNodeRedModel.Sicaklik_Sensor_2_Aktif),
            nameof(KumesNodeRedModel.Sicaklik_Sensor_2_Value),
            nameof(KumesNodeRedModel.Sicaklik_Sensor_3_Aktif),
            nameof(KumesNodeRedModel.Sicaklik_Sensor_3_Value),
            nameof(KumesNodeRedModel.Sicaklik_Sensor_4_Aktif),
            nameof(KumesNodeRedModel.Sicaklik_Sensor_4_Value),

            nameof(KumesNodeRedModel.Nem_Kumes_Aktif),
            nameof(KumesNodeRedModel.Nem_Kumes_Ortalama),
            nameof(KumesNodeRedModel.Nem_Dis_Aktif),
            nameof(KumesNodeRedModel.Nem_Dis),
            nameof(KumesNodeRedModel.Nem_Sensor_1_Aktif),
            nameof(KumesNodeRedModel.Nem_Sensor_1_Value),
            nameof(KumesNodeRedModel.Nem_Sensor_2_Aktif),
            nameof(KumesNodeRedModel.Nem_Sensor_2_Value),
            nameof(KumesNodeRedModel.Nem_Sensor_3_Aktif),
            nameof(KumesNodeRedModel.Nem_Sensor_3_Value),
            nameof(KumesNodeRedModel.Nem_Sensor_4_Aktif),
            nameof(KumesNodeRedModel.Nem_Sensor_4_Value),

            nameof(KumesNodeRedModel.Basinc_Sens_Aktif),
            nameof(KumesNodeRedModel.Basinc_Sens_Value),
            nameof(KumesNodeRedModel.CO2_Sens_Aktif),
            nameof(KumesNodeRedModel.CO2_Sens_Value),

            nameof(KumesNodeRedModel.IcmeSuyu_Sayac_Aktif),
            nameof(KumesNodeRedModel.IcmeSuyu_Sayac),
            nameof(KumesNodeRedModel.IcmeSuyu_Tuketim_Bugun),
            nameof(KumesNodeRedModel.IcmeSuyu_Tuketim_Dun),

            nameof(KumesNodeRedModel.Yem_Silo_1_Aktif),
            nameof(KumesNodeRedModel.Yem_Silo_1_Silo_Value),
            nameof(KumesNodeRedModel.Yem_Silo_1_Silo_Tuketim_Bugun),
            nameof(KumesNodeRedModel.Yem_Silo_1_Silo_VarlikOran),

            nameof(KumesNodeRedModel.Yemleme_Bolge_1_ZincirState),
            nameof(KumesNodeRedModel.Yemleme_Bolge_1_YemSet),
            nameof(KumesNodeRedModel.Yemleme_Bolge_1_YemGerceklesen),
            nameof(KumesNodeRedModel.Yemleme_Bolge_2_ZincirState),
            nameof(KumesNodeRedModel.Yemleme_Bolge_2_YemSet),
            nameof(KumesNodeRedModel.Yemleme_Bolge_2_YemGerceklesen),
            nameof(KumesNodeRedModel.Yemleme_Bolge_3_ZincirState),
            nameof(KumesNodeRedModel.Yemleme_Bolge_3_YemSet),
            nameof(KumesNodeRedModel.Yemleme_Bolge_3_YemGerceklesen),
            nameof(KumesNodeRedModel.Yemleme_Helezon_State),
            nameof(KumesNodeRedModel.Yemleme_Kantar_Aktuel),

            nameof(KumesNodeRedModel.Mini_Fan_1_State),
            nameof(KumesNodeRedModel.Mini_Fan_2_State),
            nameof(KumesNodeRedModel.Mini_Fan_3_State),
            nameof(KumesNodeRedModel.Mini_Fan_4_State),
            nameof(KumesNodeRedModel.Fan_1_State),
            nameof(KumesNodeRedModel.Fan_2_State),
            nameof(KumesNodeRedModel.Fan_3_State),
            nameof(KumesNodeRedModel.Fan_4_State),
            nameof(KumesNodeRedModel.Fan_5_State),
            nameof(KumesNodeRedModel.Fan_6_State),
            nameof(KumesNodeRedModel.Fan_7_State),
            nameof(KumesNodeRedModel.Fan_8_State),
            nameof(KumesNodeRedModel.Havalandirma_Mode),

            nameof(KumesNodeRedModel.Ped_Pompa_1_State),
            nameof(KumesNodeRedModel.Ped_Pompa_2_State),
            nameof(KumesNodeRedModel.Ped_Kapak_1_Oran),
            nameof(KumesNodeRedModel.Ped_Kapak_2_Oran),
            nameof(KumesNodeRedModel.Ped_Su_Sayac_Aktif),
            nameof(KumesNodeRedModel.Ped_Su_Sayac_Value),
            nameof(KumesNodeRedModel.Ped_Su_Tuketim_Bugun),
            nameof(KumesNodeRedModel.Ped_Su_Tuketim_Dun),

            nameof(KumesNodeRedModel.Buffel_Kapak_Sag_Oran),
            nameof(KumesNodeRedModel.Buffel_Kapak_Sol_Oran),
            nameof(KumesNodeRedModel.Aydinlatma_State),
            nameof(KumesNodeRedModel.Soba_State),
            nameof(KumesNodeRedModel.SebekedeElektrikVar),

            GunlukCanliHayvanSayisiEtiketEslemeSozcugu,
            GunlukOluHayvanSayisiEtiketEslemeSozcugu,
            HayvanBasinaSuTuketimEtiketEslemeSozcugu,
            HayvanBasinaYemTuketimEtiketEslemeSozcugu
        };

        public Kayislar1TesisLogNodeRedWorker(
            ILogger<Kayislar1TesisLogNodeRedWorker> logger,
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
                _logger.LogInformation("Kayýþlar 1 tesis log worker baþlatýlýyor. LokasyonId: {LokasyonId}", LokasyonId);
                await UpdateServisDurumAsync(ServisAdi, 2, "Servis baþlatýlýyor.");

                _kumesListesi = await _cacheService.GetKumeslerAsync(LokasyonId, cancellationToken);
                _kumesListesi = _kumesListesi
                    .GroupBy(x => x.Id)
                    .Select(x => x.First())
                    .ToList();

                _logger.LogInformation("Kayýþlar lokasyonu için {KumesSayisi} kümes bulundu.", _kumesListesi.Count);

                await EtiketTanimlariniYukleAsync(cancellationToken);
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayýþlar 1 tesis log worker baþlatýlamadý.");
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

                    var hazirlamaGorevleri = _kumesListesi.Select(kumes => KumesLoglariniHazirlaAsync(kumes.Id, stoppingToken)).ToArray();
                    /*KumesLoglariniHazirlaAsync(26, stoppingToken);*/

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
                        $"Servis çalýþýyor. EtiketLogYeni: {etiketLoglari.Count}, KumesLogYeni: {kumesLoglari.Count}",
                        true);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kayýþlar 1 tesis log worker döngüsünde hata oluþtu.");
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

                // PLC/Node-RED güncel deðilse proses deðerlerini tarihçeye yanlýþ/eskimiþ veri olarak yazmýyoruz.
                // Buna raðmen PLC_CONNECTED ve PLC_READ etiketleri 0 olarak loglanýr.
                if (!plcOkuyor)
                    return (etiketLoglari, kumesLoglari);

                KumesNodeRedModel? model;
                try
                {
                    model = JsonSerializer.Deserialize<KumesNodeRedModel>(nodeRedKaydi!.Veriler!);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Kümes {KumesId} Node-RED JSON verisi parse edilemedi.", kumesId);
                    return (etiketLoglari, kumesLoglari);
                }

                if (model == null)
                    return (etiketLoglari, kumesLoglari);

                // Kayýþlar'a özel hesaplanan deðerler log worker içinde de üretilir.
                // Böylece GUNLUK_CANLI_HAYVAN_SAYISI, GUNLUK_OLU_HAYVAN_SAYISI ve
                // hayvan baþý su/yem tüketimleri EtiketLogYeni tarihçesinde de bulunur.
                var gunlukCanliHayvanSayisi = await _cacheService
                    .GetGunlukCanliHayvanSayisiByKumesId(kumesId, cancellationToken);

                var gunlukOluHayvanSayisi = await _cacheService.GetGunlukOluHayvanSayisiByKumesId(kumesId, cancellationToken);

                var hesaplananIcmeSuyuTuketimBugun = await IcmeSuyuTuketiminiHesaplaAsync(
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kümes {KumesId} tarihsel log verisi hazýrlanamadý.", kumesId);
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
                    "Kayýþlar tarihsel loglarý yazýldý. EtiketLogYeni: {EtiketSayisi}, KumesLogYeni: {KumesLogSayisi}",
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
                    // Asýl hatayý gölgelemiyoruz.
                }

                throw;
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                    await connection.CloseAsync();
            }
        }

        private static async Task BulkInsertAsync<T>(
            SqlConnection connection,
            SqlTransaction transaction,
            IEnumerable<T> data,
            string destinationTable,
            CancellationToken cancellationToken)
        {
            var dataList = data.ToList();
            if (dataList.Count == 0)
                return;

            // Model ile hedef SQL tablosunun ortak, yazýlabilir kolonlarýný buluyoruz.
            // Identity/computed/rowversion kolonlar SqlBulkCopy'ye dahil edilmez.
            var columnNames = new List<string>();

            const string columnSql = @"
SELECT c.name
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
                    columnNames.Add(reader.GetString(0));
                }
            }

            if (columnNames.Count == 0)
                throw new InvalidOperationException($"{destinationTable} tablosunun yazýlabilir kolonlarý bulunamadý.");

            var accessor = TypeAccessor.Create(typeof(T));
            var memberMap = accessor
                .GetMembers()
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            var members = columnNames
                .Where(memberMap.ContainsKey)
                .Select(x => memberMap[x])
                .ToArray();

            if (members.Length == 0)
                throw new InvalidOperationException($"{typeof(T).Name} ile {destinationTable} arasýnda ortak kolon bulunamadý.");

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

        private static KumesLogYeni KumesLogOlustur(int kumesId, DateTime kayitZamani, KumesNodeRedModel model, decimal? hesaplananIcmeSuyuTuketimBugun)
        {
            return new KumesLogYeni
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

                Silo1 = model.Yem_Silo_1_Silo_Value / 1000,
                Silo2 = null,

                Basinc1 = model.Basinc_Sens_Value,
                Co2_1 = model.CO2_Sens_Value,

                YemTuketimBugun = model.Yem_Silo_1_Silo_Tuketim_Bugun / 1000,
                SuTuketimBugun = hesaplananIcmeSuyuTuketimBugun,
                PedSuTuketimBugun = model.Ped_Su_Tuketim_Bugun / 100,
                PedSuSayac = model.Ped_Su_Sayac_Value / 100,
                IcmeSuyuSayac = model.IcmeSuyu_Sayac / 100
            };
        }

        private void EtiketLoglariniOlustur(ICollection<EtiketLogYeni> etiketLoglari, int kumesId, DateTime kayitZamani, KumesNodeRedModel model, int gunlukCanliHayvanSayisi, int? gunlukOluHayvanSayisi, decimal? hesaplananIcmeSuyuTuketimBugun)
        {
            /*
             * Doðrudan PLC / Node-RED alanlarý.
             * EtiketId kod içinde yoktur; property adý EslemeSozcugu olarak kullanýlýr.
             */

            // Sýcaklýk
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Sicaklik_Kumes_Ortalama),
                kayitZamani, model.Sicaklik_Kumes_Ortalama);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Sicaklik_Dis_Aktif),
                kayitZamani, model.Sicaklik_Dis_Aktif);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Sicaklik_Dis),
                kayitZamani, model.Sicaklik_Dis);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Sicaklik_Sensor_1_Aktif),
                kayitZamani, model.Sicaklik_Sensor_1_Aktif);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Sicaklik_Sensor_1_Value),
                kayitZamani, model.Sicaklik_Sensor_1_Value);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Sicaklik_Sensor_2_Aktif),
                kayitZamani, model.Sicaklik_Sensor_2_Aktif);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Sicaklik_Sensor_2_Value),
                kayitZamani, model.Sicaklik_Sensor_2_Value);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Sicaklik_Sensor_3_Aktif),
                kayitZamani, model.Sicaklik_Sensor_3_Aktif);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Sicaklik_Sensor_3_Value),
                kayitZamani, model.Sicaklik_Sensor_3_Value);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Sicaklik_Sensor_4_Aktif),
                kayitZamani, model.Sicaklik_Sensor_4_Aktif);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Sicaklik_Sensor_4_Value),
                kayitZamani, model.Sicaklik_Sensor_4_Value);

            // Nem
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Nem_Kumes_Aktif),
                kayitZamani, model.Nem_Kumes_Aktif);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Nem_Kumes_Ortalama),
                kayitZamani, model.Nem_Kumes_Ortalama);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Nem_Dis_Aktif),
                kayitZamani, model.Nem_Dis_Aktif);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Nem_Dis),
                kayitZamani, model.Nem_Dis);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Nem_Sensor_1_Aktif),
                kayitZamani, model.Nem_Sensor_1_Aktif);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Nem_Sensor_1_Value),
                kayitZamani, model.Nem_Sensor_1_Value);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Nem_Sensor_2_Aktif),
                kayitZamani, model.Nem_Sensor_2_Aktif);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Nem_Sensor_2_Value),
                kayitZamani, model.Nem_Sensor_2_Value);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Nem_Sensor_3_Aktif),
                kayitZamani, model.Nem_Sensor_3_Aktif);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Nem_Sensor_3_Value),
                kayitZamani, model.Nem_Sensor_3_Value);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Nem_Sensor_4_Aktif),
                kayitZamani, model.Nem_Sensor_4_Aktif);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Nem_Sensor_4_Value),
                kayitZamani, model.Nem_Sensor_4_Value);

            // Basýnç / CO2
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Basinc_Sens_Aktif),
                kayitZamani, model.Basinc_Sens_Aktif);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Basinc_Sens_Value),
                kayitZamani, model.Basinc_Sens_Value);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.CO2_Sens_Aktif),
                kayitZamani, model.CO2_Sens_Aktif);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.CO2_Sens_Value),
                kayitZamani, model.CO2_Sens_Value);

            // Ýçme suyu
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.IcmeSuyu_Sayac_Aktif),
                kayitZamani, model.IcmeSuyu_Sayac_Aktif);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.IcmeSuyu_Sayac),
                kayitZamani, model.IcmeSuyu_Sayac / 100);

            /*
             * Bugünkü tüketimde PLC deðeri yerine gerekiyorsa düzeltilmiþ deðer yazýlýr.
             * Böylece KumesLog ve EtiketLog ayný deðeri gösterir.
             */
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.IcmeSuyu_Tuketim_Bugun),
                kayitZamani, hesaplananIcmeSuyuTuketimBugun);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.IcmeSuyu_Tuketim_Dun),
                kayitZamani, model.IcmeSuyu_Tuketim_Dun / 100);

            // Yem / silo
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yem_Silo_1_Aktif),
                kayitZamani, model.Yem_Silo_1_Aktif);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yem_Silo_1_Silo_Value),
                kayitZamani, model.Yem_Silo_1_Silo_Value);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yem_Silo_1_Silo_Tuketim_Bugun),
                kayitZamani, model.Yem_Silo_1_Silo_Tuketim_Bugun / 1000);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yem_Silo_1_Silo_VarlikOran),
                kayitZamani, model.Yem_Silo_1_Silo_VarlikOran);

            // Yemleme
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yemleme_Bolge_1_ZincirState),
                kayitZamani, model.Yemleme_Bolge_1_ZincirState);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yemleme_Bolge_1_YemSet),
                kayitZamani, model.Yemleme_Bolge_1_YemSet);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yemleme_Bolge_1_YemGerceklesen),
                kayitZamani, model.Yemleme_Bolge_1_YemGerceklesen);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yemleme_Bolge_2_ZincirState),
                kayitZamani, model.Yemleme_Bolge_2_ZincirState);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yemleme_Bolge_2_YemSet),
                kayitZamani, model.Yemleme_Bolge_2_YemSet);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yemleme_Bolge_2_YemGerceklesen),
                kayitZamani, model.Yemleme_Bolge_2_YemGerceklesen);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yemleme_Bolge_3_ZincirState),
                kayitZamani, model.Yemleme_Bolge_3_ZincirState);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yemleme_Bolge_3_YemSet),
                kayitZamani, model.Yemleme_Bolge_3_YemSet);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yemleme_Bolge_3_YemGerceklesen),
                kayitZamani, model.Yemleme_Bolge_3_YemGerceklesen);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yemleme_Helezon_State),
                kayitZamani, model.Yemleme_Helezon_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Yemleme_Kantar_Aktuel),
                kayitZamani, model.Yemleme_Kantar_Aktuel);

            // Fanlar / havalandýrma
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Mini_Fan_1_State),
                kayitZamani, model.Mini_Fan_1_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Mini_Fan_2_State),
                kayitZamani, model.Mini_Fan_2_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Mini_Fan_3_State),
                kayitZamani, model.Mini_Fan_3_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Mini_Fan_4_State),
                kayitZamani, model.Mini_Fan_4_State);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Fan_1_State),
                kayitZamani, model.Fan_1_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Fan_2_State),
                kayitZamani, model.Fan_2_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Fan_3_State),
                kayitZamani, model.Fan_3_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Fan_4_State),
                kayitZamani, model.Fan_4_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Fan_5_State),
                kayitZamani, model.Fan_5_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Fan_6_State),
                kayitZamani, model.Fan_6_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Fan_7_State),
                kayitZamani, model.Fan_7_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Fan_8_State),
                kayitZamani, model.Fan_8_State);

            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Havalandirma_Mode),
                kayitZamani, model.Havalandirma_Mode);

            // Ped
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Ped_Pompa_1_State),
                kayitZamani, model.Ped_Pompa_1_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Ped_Pompa_2_State),
                kayitZamani, model.Ped_Pompa_2_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Ped_Kapak_1_Oran),
                kayitZamani, model.Ped_Kapak_1_Oran);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Ped_Kapak_2_Oran),
                kayitZamani, model.Ped_Kapak_2_Oran);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Ped_Su_Sayac_Aktif),
                kayitZamani, model.Ped_Su_Sayac_Aktif);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Ped_Su_Sayac_Value),
                kayitZamani, model.Ped_Su_Sayac_Value / 100);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Ped_Su_Tuketim_Bugun),
                kayitZamani, model.Ped_Su_Tuketim_Bugun / 100);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Ped_Su_Tuketim_Dun),
                kayitZamani, model.Ped_Su_Tuketim_Dun / 100);

            // Diðer
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Buffel_Kapak_Sag_Oran),
                kayitZamani, model.Buffel_Kapak_Sag_Oran);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Buffel_Kapak_Sol_Oran),
                kayitZamani, model.Buffel_Kapak_Sol_Oran);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Aydinlatma_State),
                kayitZamani, model.Aydinlatma_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.Soba_State),
                kayitZamani, model.Soba_State);
            EtiketEkle(etiketLoglari, kumesId,
                nameof(KumesNodeRedModel.SebekedeElektrikVar),
                kayitZamani, model.SebekedeElektrikVar);

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
            KumesNodeRedModel model,
            int gunlukCanliHayvanSayisi,
            int? gunlukOluHayvanSayisi,
            decimal? hesaplananIcmeSuyuTuketimBugun)
        {
            /*
             * Bu alanlar PLC'den doðrudan gelmez.
             * Yeni hesaplanan etiket ekleneceðinde yalnýzca buraya hesap ve
             * BeklenenEtiketEslemeSozcukleri listesine eþleme anahtarý eklenir.
             */

            EtiketEkle(
                etiketLoglari,
                kumesId,
                GunlukCanliHayvanSayisiEtiketEslemeSozcugu,
                kayitZamani,
                gunlukCanliHayvanSayisi);

            /*
             * null = kullanýcý bugün henüz ölüm sayýsý girmedi.
             * 0 = kullanýcý kayýt girdi ve ölüm olmadýðýný belirtti.
             */
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
                !model.Yem_Silo_1_Silo_Tuketim_Bugun.HasValue
                    ? (decimal?)null
                    : (model.Yem_Silo_1_Silo_Tuketim_Bugun.Value * 1_000m) /
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

        private async Task<decimal?> IcmeSuyuTuketiminiHesaplaAsync(
            int kumesId,
            KumesNodeRedModel model,
            CancellationToken cancellationToken)
        {
            var plcTuketim = model.IcmeSuyu_Tuketim_Bugun / 100;
            return plcTuketim;
            //if (!plcTuketim.HasValue ||
            //    //plcTuketim.GetValueOrDefault() <= 50 ||
            //    !model.IcmeSuyu_Sayac.HasValue)
            //{
            //    return plcTuketim;
            //}

            ///*
            // * Artýk 25/76 gibi statik etiket ID yok.
            // */
            //if (!EtiketIdBul(
            //        nameof(KumesNodeRedModel.IcmeSuyu_Sayac_Aktif),
            //        out var suSayacAktifEtiketId) ||
            //    !EtiketIdBul(
            //        nameof(KumesNodeRedModel.IcmeSuyu_Sayac),
            //        out var suSayacEtiketId))
            //{
            //    return plcTuketim;
            //}

            //var mevcutSayac = model.IcmeSuyu_Sayac.Value;

            //var referansSayac = await _cacheService
            //    .GetDunIcmeSuyuSayacByKumesId(
            //        kumesId,
            //        mevcutSayac,
            //        suSayacAktifEtiketId,
            //        suSayacEtiketId,
            //        cancellationToken);

            //return mevcutSayac >= referansSayac
            //    ? mevcutSayac - referansSayac
            //    : mevcutSayac;
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

            /*
             * EtiketLogYeni tarihsel log tablosudur. Kaynak deðer null ise null deðer de tarihçeye yazýlýr.
             */
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

        private bool EtiketIdBul(
            string etiketEslemeSozcugu,
            out int etiketId)
        {
            var anahtar = EslemeSozcuguNormalizeEt(etiketEslemeSozcugu);

            return _etiketTanimlari.TryGetValue(
                anahtar,
                out etiketId);
        }

        private async Task EtiketTanimlariniGerekirseYenileAsync(
            CancellationToken cancellationToken)
        {
            var now = DateTime.Now;

            /*
             * Baþarýlý yüklenen cache 24 saat boyunca kullanýlýr.
             * Yükleme baþarýsýz olursa son baþarýlý zaman güncellenmediði için
             * bir sonraki 3 dakikalýk worker turunda tekrar denenir.
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
                 * Kilidi beklerken baþka bir çaðrý cache'i yenilemiþ olabilir.
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
                 * DbSet property adýna baðýmlý deðiliz; entity tipi üzerinden eriþiyoruz.
                 * Belleðe tüm Etiket entity'si deðil yalnýzca Id + EslemeSozcugu alýnýr.
                 */
                var beklenenEtiketEslemeSozcukleri = BeklenenEtiketEslemeSozcukleri
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var beklenenEtiketAnahtarlari = beklenenEtiketEslemeSozcukleri
                    .Select(EslemeSozcuguNormalizeEt)
                    .ToHashSet();

                /*
                 * Etiket tablosunda binlerce kayýt olsa bile tamamý çekilmez.
                 * Contains, EF Core tarafýndan SQL IN (...) olarak çevrilir ve
                 * yalnýzca bu worker'ýn kullandýðý EslemeSozcugu kayýtlarý gelir.
                 *
                 * Bu nedenle Etiket.EslemeSozcugu deðerlerini model property adlarýyla
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
                 * Paralel kümes task'larý eski veya yeni dictionary referansýný
                 * güvenli þekilde okuyabilir.
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
                        "Etiket tablosunda ayný EslemeSozcugu için birden fazla kayýt var. " +
                        "En küçük EtiketId kullanýldý. Tanýmlar: {EtiketTanimlari}",
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
                        "Etiket tablosunda EslemeSozcugu tanýmý olmayan alanlar var. " +
                        "Bu alanlar EtiketLogYeni'ye yazýlmayacak. " +
                        "Eksikler: {EksikEtiketler}",
                        string.Join(", ", eksikEtiketler));
                }

                _logger.LogInformation(
                    "Etiket eþleme cache'i yenilendi. Etiket sayýsý: {EtiketSayisi}",
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
                 * Eski cache varsa korunur. Böylece geçici DB hatasýnda worker
                 * etiket yazmayý tamamen býrakmaz.
                 */
                _logger.LogError(
                    ex,
                    "Etiket eþleme cache'i yenilenemedi. Mevcut cache korunuyor.");
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
                _logger.LogError(ex, "Kayýþlar 1 tesis log worker durdurulurken hata oluþtu.");
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
