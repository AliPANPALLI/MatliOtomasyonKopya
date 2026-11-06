using FastMember;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using static SharedKernel.ApplicationConstants;
using AlarmLog = PoultryHouseScada.Domain.Models.AlarmLog;
using Kayislar1.Application.Models;
using Kayislar1.Infrastructure.Caching;

namespace Kayislar1.Worker
{
    public class Kayislar1KumesNodeRedWorker : BackgroundService
    {
        private readonly ILogger<Kayislar1KumesNodeRedWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly AlarmCacheService _alarmCacheService;
        private readonly SemaphoreSlim _kumesIslemSiniri = new(4, 4);

        private List<Kumes> _kumesListesi = new();

        /*
         * Bunlar kümes sayýsý kadar küçük sözlüklerdir.
         * Her 30 saniyelik turda DB'ye gitmemek için sadece son kontrol bilgisi tutulur.
         */
        private readonly Dictionary<int, DateTime> _kumesHayvanLogAylikKayitAtildi = new();
        private readonly Dictionary<int, DateTime> _sonHayvanKayitKontrolZamani = new();
        private readonly Dictionary<int, int?> _gunlukOluHayvanSayilari = new();

        private const int LokasyonId = 32;
        private const string ServisAdi = "Kayislar1KumesNodeRedWorker";

        // Alarmlar.AlarmEslemeSozcugu alanýndaki PLC okuma alarmýnýn anahtarý.
        // Karþýlaþtýrma normalize edilerek yapýlýr; PLC_OKUNMUYOR, Plc Okunmuyor
        // ve plc-okunmuyor ayný alarm anahtarý kabul edilir.
        //private const string PlcKoptuAlarmEslemeSozcugu = "PLC_KOPTU";
        private const string PlcKoptuAlarmEslemeSozcugu = "PLC_KOPTU";

        private const string GunlukOluHayvanSayisiGirilmediAlarmEslemeSozcugu =
            "GUNLUK_OLU_HAYVAN_SAYISI_GIRILMEDI";

        private const string HaftaSonuOluHayvanSayisiGirilmediAlarmEslemeSozcugu =
            "HAFTA_SONU_OLU_HAYVAN_SAYISI_GIRILMEDI";

        private static readonly TimeSpan PlcVeriZamanAsimi = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan AlarmTanimYenilemeSuresi = TimeSpan.FromMinutes(5);

        /*
         * Worker 30 saniyede bir döner; fakat kullanýcý veri giriþ kontrolü
         * için DB'yi 30 saniyede bir sorgulamak gereksizdir.
         * Alarmýn girilen kayýt sonrasý makul sürede kapanmasý için 5 dakika yeterlidir.
         */
        private static readonly TimeSpan HayvanKayitKontrolAraligi = TimeSpan.FromMinutes(5);

        /*
         * Alarm tanýmlarý alarm türüne özel ayrý sözlüklerde tutulmaz.
         * Anahtar KumesId + AlarmEslemeSozcugu olduðu için ayný kümese ait
         * PLC, sýcaklýk, su, yem vb. bütün alarmlar ayný yapýdan yönetilebilir.
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

        private readonly record struct AlarmDurumIstegi(
            string AlarmEslemeSozcugu,
            bool Aktif,
            string? Detay = null);

        /*
         * Alarmlar tablosundaki AlarmEslemeSozcugu alanýnda bu deðerler kullanýlmalýdýr.
         * nameof kullanýldýðý için KumesNodeRedModel property adý deðiþirse derleme aþamasýnda
         * fark edilir.
         */
        private static readonly string[] BeklenenAlarmEslemeSozcukleri =
        {
            PlcKoptuAlarmEslemeSozcugu,
            PlcKoptuAlarmEslemeSozcugu,
            GunlukOluHayvanSayisiGirilmediAlarmEslemeSozcugu,
            HaftaSonuOluHayvanSayisiGirilmediAlarmEslemeSozcugu,

            nameof(KumesNodeRedModel.Alarm_Sicaklik_AktifSensSayisiAz),
            nameof(KumesNodeRedModel.Alarm_Sicaklik_DusukKumes),
            nameof(KumesNodeRedModel.Alarm_Sicaklik_FazlaKumes),
            nameof(KumesNodeRedModel.Alarm_Sicaklik_DusukSens1),
            nameof(KumesNodeRedModel.Alarm_Sicaklik_DusukSens2),
            nameof(KumesNodeRedModel.Alarm_Sicaklik_DusukSens3),
            nameof(KumesNodeRedModel.Alarm_Sicaklik_DusukSens4),
            nameof(KumesNodeRedModel.Alarm_Sicaklik_DusukSens5),
            nameof(KumesNodeRedModel.Alarm_Sicaklik_DusukSens6),
            nameof(KumesNodeRedModel.Alarm_Sicaklik_FazlaSens1),
            nameof(KumesNodeRedModel.Alarm_Sicaklik_FazlaSens2),
            nameof(KumesNodeRedModel.Alarm_Sicaklik_FazlaSens3),
            nameof(KumesNodeRedModel.Alarm_Sicaklik_FazlaSens4),
            nameof(KumesNodeRedModel.Alarm_Sicaklik_FazlaSens5),
            nameof(KumesNodeRedModel.Alarm_Sicaklik_FazlaSens6),

            nameof(KumesNodeRedModel.Alarm_Nem_AktifSensSayisiAz),

            nameof(KumesNodeRedModel.Alarm_CO2_AktifDegil),
            nameof(KumesNodeRedModel.Alarm_CO2_Fazla),

            nameof(KumesNodeRedModel.Alarm_Basinc_AktifDegil),
            nameof(KumesNodeRedModel.Alarm_Basinc_Fazla),

            nameof(KumesNodeRedModel.Alarm_IcmeSuyuSayac_AktifDegil),

            nameof(KumesNodeRedModel.Alarm_Fan_TermikNok_Mini),
            nameof(KumesNodeRedModel.Alarm_Fan_TermikNok_Tunel),

            nameof(KumesNodeRedModel.Alarm_Ped_PompaTermikNok),
            nameof(KumesNodeRedModel.Alarm_Buffel_Reserve_1),

            nameof(KumesNodeRedModel.Alarm_Yem_Silo_1_BaglantiKoptu),
            nameof(KumesNodeRedModel.Alarm_Yemleme_TermikNok_Zincirler),

            nameof(KumesNodeRedModel.Alarm_Kantar_KapakAcmaHatasi),
            nameof(KumesNodeRedModel.Alarm_Kantar_KapakKapatmaHatasi),
            nameof(KumesNodeRedModel.Alarm_Kantar_YemDolumHatasi),
            nameof(KumesNodeRedModel.Alarm_Kantar_YemDagitimHatasi),
            nameof(KumesNodeRedModel.Alarm_Kantar_BolgeyeDagitimSuresiGirilmediHatasi),
            nameof(KumesNodeRedModel.Alarm_Kantar_MinKapasiteGirilmediHatasi),
            nameof(KumesNodeRedModel.Alarm_Kantar_OtomatikBaslatmaKapakAcikHatasi),
            nameof(KumesNodeRedModel.Alarm_Kantar_MotorTermikNOK),
            nameof(KumesNodeRedModel.Alarm_Kantar_Hat_1_YemSensor_1_Hatasi),
            nameof(KumesNodeRedModel.Alarm_Kantar_KantarYemDolumSonsor_3_Hatasi),


            nameof(KumesNodeRedModel.Reserve_YemlemeUpDownTermikNOK),
            nameof(KumesNodeRedModel.Reserve_NemDegeri_Yuksek),
            nameof(KumesNodeRedModel.Reserve_PedSayacAktifDegil),

            nameof(KumesNodeRedModel.Reserve_Fan_1_CalismasiGerekirkenCalismiyor),
            nameof(KumesNodeRedModel.Reserve_Fan_2_CalismasiGerekirkenCalismiyor),
            nameof(KumesNodeRedModel.Reserve_Fan_3_CalismasiGerekirkenCalismiyor),
            nameof(KumesNodeRedModel.Reserve_Fan_4_CalismasiGerekirkenCalismiyor),
            nameof(KumesNodeRedModel.Reserve_Fan_5_CalismasiGerekirkenCalismiyor),
            nameof(KumesNodeRedModel.Reserve_Fan_6_CalismasiGerekirkenCalismiyor),
            nameof(KumesNodeRedModel.Reserve_Fan_7_CalismasiGerekirkenCalismiyor),
            nameof(KumesNodeRedModel.Reserve_Fan_8_CalismasiGerekirkenCalismiyor),

            nameof(KumesNodeRedModel.Reserve_MFan_1_CalismasiGerekirkenCalismiyor),
            nameof(KumesNodeRedModel.Reserve_MFan_2_CalismasiGerekirkenCalismiyor),
            nameof(KumesNodeRedModel.Reserve_MFan_3_CalismasiGerekirkenCalismiyor),
            nameof(KumesNodeRedModel.Reserve_MFan_4_CalismasiGerekirkenCalismiyor),

            nameof(KumesNodeRedModel.Reserve_Ped_Pompa_1_CalismasiGerekirkenCalismiyor),
            nameof(KumesNodeRedModel.Reserve_Ped_Pompa_2_CalismasiGerekirkenCalismiyor),

            nameof(KumesNodeRedModel.Reserve_Ped_Kapak_Sag_AcilmasiGerekirkenAcilmiyor),
            nameof(KumesNodeRedModel.Reserve_Ped_Kapak_Sag_KapanmasiGerekirkenKapanmiyor),
            nameof(KumesNodeRedModel.Reserve_Ped_Kapak_Sol_AcilmasiGerekirkenAcilmiyor),
            nameof(KumesNodeRedModel.Reserve_Ped_Kapak_Sol_KapanmasiGerekirkenKapanmiyor),

            nameof(KumesNodeRedModel.Reserve_Buffel_Kapak_Sag_AcilmasiGerekirkenAcilmiyor),
            nameof(KumesNodeRedModel.Reserve_Buffel_Kapak_Sag_KapanmasiGerekirkenKapanmiyor),
            nameof(KumesNodeRedModel.Reserve_Buffel_Kapak_Sol_AcilmasiGerekirkenAcilmiyor),
            nameof(KumesNodeRedModel.Reserve_Buffel_Kapak_Sol_KapanmasiGerekirkenKapanmiyor)
    };


        /*
         * Etiket tablosu çok seyrek deðiþtiði için her worker turunda sorgulanmaz.
         * Bellekte yalnýzca normalize edilmiþ EslemeSozcugu -> EtiketId tutulur.
         * Entity/list cache'lenmediði için bellek kullanýmý çok küçüktür.
         */
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
         * Bunlar worker'ýn EtiketLog_TempYeni'ye yazmaya çalýþtýðý alanlardýr.
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

        public Kayislar1KumesNodeRedWorker(
            ILogger<Kayislar1KumesNodeRedWorker> logger,
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
        //        _logger.LogInformation("Verim Gýda Ticari Yumurta Node-RED worker baþlatýlýyor. LokasyonId: {LokasyonId}", LokasyonId);

        //        await UpdateServisDurumAsync(ServisAdi, 2, "Servis baþlatýlýyor");
        //        _kumesListesi = await _alarmCacheService.GetKumeslerAsync(LokasyonId);

        //        _logger.LogInformation("Lokasyon {LokasyonId} için {KumesSayisi} kümes bulundu.", LokasyonId, _kumesListesi.Count);

        //        await base.StartAsync(cancellationToken);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Verim Gýda Ticari Yumurta worker baþlatýlamadý.");
        //        await UpdateServisDurumAsync(ServisAdi, 0, ex.Message);
        //        throw;
        //    }
        //}
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Kayislar1 Node-RED worker baþlatýlýyor");

                await UpdateServisDurumAsync(ServisAdi, 2, "Servis baþlatýlýyor.");

                /*
                 * Lokasyona ait kümesler alýnýr.
                 */
                _kumesListesi = await _alarmCacheService.GetKumeslerAsync(LokasyonId);

                /*
                 * Ayný kümes yanlýþlýkla iki kez dönerse
                 * Id üzerinden tekilleþtirilir.
                 */
                //_kumesListesi = kumesler.GroupBy(kumes => kumes.Id).Select(grup => grup.First()).ToList();

                _logger.LogInformation("{LokasyonId} lokasyonu için toplam {KumesSayisi} kümes bulundu.", LokasyonId, _kumesListesi.Count);

                await AlarmTanimlariniYukleAsync(cancellationToken);
                await EtiketTanimlariniYukleAsync(cancellationToken);
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayislar1 worker baþlatýlamadý.");
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
                     * Aylýk KumesHayvanLog placeholder kayýtlarý kümes baþýna günde
                     * yalnýzca bir kere kontrol edilir.
                     */
                    await AylikKumesHayvanLoglariniGerekirseTamamlaAsync(stoppingToken);

                    /*
                     * Günlük / hafta sonu ölü hayvan giriþ kontrolleri 30 saniyelik
                     * SCADA döngüsünden baðýmsýz olarak seyrek yapýlýr.
                     */
                    await HayvanKayitAlarmlariniGerekirseKontrolEtAsync(stoppingToken);

                    /*
                     * Canlý hayvan sayýsý AlarmCacheService içinde 15 dakika cache'lenir.
                     * Burada kümesler için sýralý alýnýr; ayný scoped DbContext üzerinde
                     * paralel EF sorgusu çalýþtýrýlmaz.
                     */
                    var canliHayvanSayilari = await CanliHayvanSayilariniGetirAsync(stoppingToken);

                    /*
                     * Kümesleri tek tek beklemek yerine en fazla 4 kümesi ayný anda
                     * hazýrlýyoruz. SQL'e ise her kümes için ayrý ayrý deðil,
                     * bütün kümeslerin kayýtlarýný iki toplu iþlemle yazýyoruz.
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
                     * Bir tur için yalnýzca bir scoped SqlConnection alýyoruz.
                     * EtiketLog ve KumesLog tarafý current-state tablolarý olarak UPSERT çalýþýr.
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
                        "Servis çalýþýyor",
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
                        "Kayislar1 worker döngüsünde hata oluþtu.");

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
             * KumesLog_TempYeni anlýk SCADA tablosudur.
             * Her KumesId için tek güncel satýr tutulur.
             *
             * Ayný turda ayný kümese ait birden fazla kayýt oluþursa
             * en güncel KayitZamani olan kayýt kullanýlýr.
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
                 * Hedef tablonun þemasýný birebir kopyalayan geçici tablo.
                 * Identity deðerini stage'e taþýmýyoruz; modelde Id olsa bile
                 * aþaðýdaki DataTable'da Id kolonu hariç tutulur.
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
                 * KumesLog_TempYeni modelinde zamanla yeni kolonlar eklenebileceði
                 * için UPDATE kolon listesini tablo metadata'sýndan üretiyoruz.
                 *
                 * Id / KumesId / identity / computed / rowversion kolonlarý
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
                        // Asýl hatayý gölgelemiyoruz.
                    }
                }

                _logger.LogError(
                    ex,
                    "KumesLog_TempYeni bulk upsert iþlemi baþarýsýz oldu.");
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
                    "EtiketLog_TempYeni bulk upsert iþlemi baþarýsýz oldu.");
            }
            finally
            {
                transaction?.Dispose();

                if (connection.State != ConnectionState.Closed)
                    await connection.CloseAsync();
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
                 * Ayný kümes için Node-RED kaydý bir kez okunur ve JSON bir kez
                 * deserialize edilir. Eski yapýda EtiketLog ve KumesLog hazýrlýðý
                 * ayný kaydý iki defa sorguluyordu.
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
                 * PLC okunmuyorsa son bilinen proses deðerlerini silmiyoruz.
                 * Sadece PLC durum etiketi 0 olur ve PLC alarmý aktifleþir.
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
                                "Node-RED kaydý yok, geçersiz veya zaman aþýmýna uðramýþ.")
                        },
                        cancellationToken);

                    return (etiketLoglari, kumesLoglari);
                }

                var model = JsonSerializer.Deserialize<KumesNodeRedModel>(
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
                                "Node-RED Veriler alaný KumesNodeRedModel'e dönüþtürülemedi.")
                        },
                        cancellationToken);

                    return (etiketLoglari, kumesLoglari);
                }

                /*
                 * PLC kaynaklý bütün alarm bitleri Alarmlar.AlarmEslemeSozcugu ile
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
                 * sayaçtan son geçerli sayaç referansý çýkarýlýr.
                 * Su etiket ID'leri statik deðildir; EslemeSozcugu cache'inden bulunur.
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
                    "Kümes {KumesId} verileri hazýrlanýrken hata oluþtu.",
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
            KumesNodeRedModel model,
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

                Silo1 = model.Yem_Silo_1_Silo_Value / 1000,
                Silo2 = null,

                Basinc1 = model.Basinc_Sens_Value,
                Co2_1 = model.CO2_Sens_Value,

                YemTuketimBugun = model.Yem_Silo_1_Silo_Tuketim_Bugun / 1000,

                SuTuketimBugun = hesaplananIcmeSuyuTuketimBugun / 100,

                PedSuTuketimBugun = model.Ped_Su_Tuketim_Bugun / 100,

                // Kayýþlar tarafýnda yumurta sýnýflandýrma verisi bu worker'da yoktur.
            };
        }

        private void EtiketLoglariniOlustur(
            ICollection<EtiketLog_TempYeni> etiketLoglari,
            int kumesId,
            DateTime kayitZamani,
            KumesNodeRedModel model,
            int gunlukCanliHayvanSayisi,
            int? gunlukOluHayvanSayisi,
            decimal? hesaplananIcmeSuyuTuketimBugun)
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
                kayitZamani, model.IcmeSuyu_Tuketim_Dun);

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
            ICollection<EtiketLog_TempYeni> etiketLoglari,
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
            var plcTuketim = model.IcmeSuyu_Tuketim_Bugun;
            return plcTuketim;
            if (!plcTuketim.HasValue ||
                plcTuketim.GetValueOrDefault() <= 50 ||
                !model.IcmeSuyu_Sayac.HasValue)
            {
                return plcTuketim;
            }

            /*
             * Artýk 25/76 gibi statik etiket ID yok.
             */
            if (!EtiketIdBul(
                    nameof(KumesNodeRedModel.IcmeSuyu_Sayac_Aktif),
                    out var suSayacAktifEtiketId) ||
                !EtiketIdBul(
                    nameof(KumesNodeRedModel.IcmeSuyu_Sayac),
                    out var suSayacEtiketId))
            {
                return plcTuketim;
            }

            var mevcutSayac = model.IcmeSuyu_Sayac.Value;

            var referansSayac = await _alarmCacheService
                .GetDunIcmeSuyuSayacByKumesId(
                    kumesId,
                    mevcutSayac,
                    suSayacAktifEtiketId,
                    suSayacEtiketId,
                    cancellationToken);

            return mevcutSayac >= referansSayac
                ? mevcutSayac - referansSayac
                : mevcutSayac;
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
             * EtiketLog_TempYeni artýk current-state/upsert tablosu.
             * Kaynak deðer null olduðunda kaydý atlamak eski deðerin ekranda
             * kalmasýna yol açar. Bu nedenle null da açýkça yazýlýr.
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
            KumesNodeRedModel model)
        {
            return new AlarmDurumIstegi[]
            {
                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_AktifSensSayisiAz),
                    model.Alarm_Sicaklik_AktifSensSayisiAz == true),

                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_DusukKumes),
                    model.Alarm_Sicaklik_DusukKumes == true),

                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_FazlaKumes),
                    model.Alarm_Sicaklik_FazlaKumes == true),

                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_DusukSens1),
                    model.Alarm_Sicaklik_DusukSens1 == true),
                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_DusukSens2),
                    model.Alarm_Sicaklik_DusukSens2 == true),
                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_DusukSens3),
                    model.Alarm_Sicaklik_DusukSens3 == true),
                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_DusukSens4),
                    model.Alarm_Sicaklik_DusukSens4 == true),
                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_DusukSens5),
                    model.Alarm_Sicaklik_DusukSens5 == true),
                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_DusukSens6),
                    model.Alarm_Sicaklik_DusukSens6 == true),

                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_FazlaSens1),
                    model.Alarm_Sicaklik_FazlaSens1 == true),
                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_FazlaSens2),
                    model.Alarm_Sicaklik_FazlaSens2 == true),
                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_FazlaSens3),
                    model.Alarm_Sicaklik_FazlaSens3 == true),
                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_FazlaSens4),
                    model.Alarm_Sicaklik_FazlaSens4 == true),
                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_FazlaSens5),
                    model.Alarm_Sicaklik_FazlaSens5 == true),
                new(nameof(KumesNodeRedModel.Alarm_Sicaklik_FazlaSens6),
                    model.Alarm_Sicaklik_FazlaSens6 == true),

                new(nameof(KumesNodeRedModel.Alarm_Nem_AktifSensSayisiAz),
                    model.Alarm_Nem_AktifSensSayisiAz == true),

                new(nameof(KumesNodeRedModel.Alarm_CO2_AktifDegil),
                    model.Alarm_CO2_AktifDegil == true),
                new(nameof(KumesNodeRedModel.Alarm_CO2_Fazla),
                    model.Alarm_CO2_Fazla == true),

                new(nameof(KumesNodeRedModel.Alarm_Basinc_AktifDegil),
                    model.Alarm_Basinc_AktifDegil == true),
                new(nameof(KumesNodeRedModel.Alarm_Basinc_Fazla),
                    model.Alarm_Basinc_Fazla == true),

                new(nameof(KumesNodeRedModel.Alarm_IcmeSuyuSayac_AktifDegil),
                    model.Alarm_IcmeSuyuSayac_AktifDegil == true),

                new(nameof(KumesNodeRedModel.Alarm_Fan_TermikNok_Mini),
                    model.Alarm_Fan_TermikNok_Mini == true),
                new(nameof(KumesNodeRedModel.Alarm_Fan_TermikNok_Tunel),
                    model.Alarm_Fan_TermikNok_Tunel == true),

                new(nameof(KumesNodeRedModel.Alarm_Ped_PompaTermikNok),
                    model.Alarm_Ped_PompaTermikNok == true),

                // PLC tarafýnda INT olduðu için sýfýrdan farklýysa aktif kabul ediliyor.
                new(nameof(KumesNodeRedModel.Alarm_Buffel_Reserve_1),
                    model.Alarm_Buffel_Reserve_1.GetValueOrDefault() != 0),

                new(nameof(KumesNodeRedModel.Alarm_Yem_Silo_1_BaglantiKoptu),
                    model.Alarm_Yem_Silo_1_BaglantiKoptu == true),

                new(nameof(KumesNodeRedModel.Alarm_Yemleme_TermikNok_Zincirler),
                    model.Alarm_Yemleme_TermikNok_Zincirler == true),

                new(nameof(KumesNodeRedModel.Alarm_Kantar_KapakAcmaHatasi),
                    model.Alarm_Kantar_KapakAcmaHatasi == true),

                new(nameof(KumesNodeRedModel.Alarm_Kantar_KapakKapatmaHatasi),
                    model.Alarm_Kantar_KapakKapatmaHatasi == true),

                new(nameof(KumesNodeRedModel.Alarm_Kantar_YemDolumHatasi),
                    model.Alarm_Kantar_YemDolumHatasi == true),

                new(nameof(KumesNodeRedModel.Alarm_Kantar_YemDagitimHatasi),
                    model.Alarm_Kantar_YemDagitimHatasi == true),

                new(nameof(KumesNodeRedModel.Alarm_Kantar_BolgeyeDagitimSuresiGirilmediHatasi),
                    model.Alarm_Kantar_BolgeyeDagitimSuresiGirilmediHatasi == true),

                new(nameof(KumesNodeRedModel.Alarm_Kantar_MinKapasiteGirilmediHatasi),
                    model.Alarm_Kantar_MinKapasiteGirilmediHatasi == true),

                new(nameof(KumesNodeRedModel.Alarm_Kantar_OtomatikBaslatmaKapakAcikHatasi),
                    model.Alarm_Kantar_OtomatikBaslatmaKapakAcikHatasi == true),

                new(nameof(KumesNodeRedModel.Alarm_Kantar_MotorTermikNOK),
                    model.Alarm_Kantar_MotorTermikNOK == true),

                new(nameof(KumesNodeRedModel.Alarm_Kantar_Hat_1_YemSensor_1_Hatasi),
                    model.Alarm_Kantar_Hat_1_YemSensor_1_Hatasi == true),

                new(nameof(KumesNodeRedModel.Alarm_Kantar_KantarYemDolumSonsor_3_Hatasi),
                    model.Alarm_Kantar_KantarYemDolumSonsor_3_Hatasi == true),

                // Yeni Reserve alarm alanlarý

                new(nameof(KumesNodeRedModel.Reserve_YemlemeUpDownTermikNOK),
                    model.Reserve_YemlemeUpDownTermikNOK == true),

                new(nameof(KumesNodeRedModel.Reserve_NemDegeri_Yuksek),
                    model.Reserve_NemDegeri_Yuksek == true),

                new(nameof(KumesNodeRedModel.Reserve_PedSayacAktifDegil),
                    model.Reserve_PedSayacAktifDegil == true),
                
                // Tünel fanlarý
                new(nameof(KumesNodeRedModel.Reserve_Fan_1_CalismasiGerekirkenCalismiyor),
                    model.Reserve_Fan_1_CalismasiGerekirkenCalismiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_Fan_2_CalismasiGerekirkenCalismiyor),
                    model.Reserve_Fan_2_CalismasiGerekirkenCalismiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_Fan_3_CalismasiGerekirkenCalismiyor),
                    model.Reserve_Fan_3_CalismasiGerekirkenCalismiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_Fan_4_CalismasiGerekirkenCalismiyor),
                    model.Reserve_Fan_4_CalismasiGerekirkenCalismiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_Fan_5_CalismasiGerekirkenCalismiyor),
                    model.Reserve_Fan_5_CalismasiGerekirkenCalismiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_Fan_6_CalismasiGerekirkenCalismiyor),
                    model.Reserve_Fan_6_CalismasiGerekirkenCalismiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_Fan_7_CalismasiGerekirkenCalismiyor),
                    model.Reserve_Fan_7_CalismasiGerekirkenCalismiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_Fan_8_CalismasiGerekirkenCalismiyor),
                    model.Reserve_Fan_8_CalismasiGerekirkenCalismiyor == true),
                
                // Mini fanlar
                new(nameof(KumesNodeRedModel.Reserve_MFan_1_CalismasiGerekirkenCalismiyor),
                    model.Reserve_MFan_1_CalismasiGerekirkenCalismiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_MFan_2_CalismasiGerekirkenCalismiyor),
                    model.Reserve_MFan_2_CalismasiGerekirkenCalismiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_MFan_3_CalismasiGerekirkenCalismiyor),
                    model.Reserve_MFan_3_CalismasiGerekirkenCalismiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_MFan_4_CalismasiGerekirkenCalismiyor),
                    model.Reserve_MFan_4_CalismasiGerekirkenCalismiyor == true),
                
                // Ped pompalarý
                new(nameof(KumesNodeRedModel.Reserve_Ped_Pompa_1_CalismasiGerekirkenCalismiyor),
                    model.Reserve_Ped_Pompa_1_CalismasiGerekirkenCalismiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_Ped_Pompa_2_CalismasiGerekirkenCalismiyor),
                    model.Reserve_Ped_Pompa_2_CalismasiGerekirkenCalismiyor == true),
                
                // Ped kapaklarý
                new(nameof(KumesNodeRedModel.Reserve_Ped_Kapak_Sag_AcilmasiGerekirkenAcilmiyor),
                    model.Reserve_Ped_Kapak_Sag_AcilmasiGerekirkenAcilmiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_Ped_Kapak_Sag_KapanmasiGerekirkenKapanmiyor),
                    model.Reserve_Ped_Kapak_Sag_KapanmasiGerekirkenKapanmiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_Ped_Kapak_Sol_AcilmasiGerekirkenAcilmiyor),
                    model.Reserve_Ped_Kapak_Sol_AcilmasiGerekirkenAcilmiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_Ped_Kapak_Sol_KapanmasiGerekirkenKapanmiyor),
                    model.Reserve_Ped_Kapak_Sol_KapanmasiGerekirkenKapanmiyor == true),
                
                // Buffel kapaklarý
                new(nameof(KumesNodeRedModel.Reserve_Buffel_Kapak_Sag_AcilmasiGerekirkenAcilmiyor),
                    model.Reserve_Buffel_Kapak_Sag_AcilmasiGerekirkenAcilmiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_Buffel_Kapak_Sag_KapanmasiGerekirkenKapanmiyor),
                    model.Reserve_Buffel_Kapak_Sag_KapanmasiGerekirkenKapanmiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_Buffel_Kapak_Sol_AcilmasiGerekirkenAcilmiyor),
                    model.Reserve_Buffel_Kapak_Sol_AcilmasiGerekirkenAcilmiyor == true),

                new(nameof(KumesNodeRedModel.Reserve_Buffel_Kapak_Sol_KapanmasiGerekirkenKapanmiyor),
                    model.Reserve_Buffel_Kapak_Sol_KapanmasiGerekirkenKapanmiyor == true)
                    };
        }

        private async Task EtiketTanimlariniGerekirseYenileAsync(
            CancellationToken cancellationToken)
        {
            var now = DateTime.Now;

            /*
             * Baþarýlý yüklenen cache 24 saat boyunca kullanýlýr.
             * Yükleme baþarýsýz olursa son baþarýlý zaman güncellenmediði için
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
                        "Bu alanlar EtiketLog_TempYeni'ye yazýlmayacak. " +
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
                        "Kümes {KumesId} canlý hayvan sayýsý alýnamadý.",
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
                     * Baþarýsýzsa tarih dictionary'ye yazýlmaz.
                     * Bir sonraki 30 saniyelik turda tekrar denenebilir.
                     */
                    _logger.LogError(
                        ex,
                        "Kümes {KumesId} aylýk hayvan log hazýrlýðý baþarýsýz.",
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
                     * Bugünkü sayý etikette de kullanýlacaðý için "kayýt var mý"
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
                     * Bugünkü ölüm sayýsý deðiþtiyse canlý hayvan cache'i artýk
                     * eski kalmýþtýr. Hemen temizlenir; ayný worker turunda aþaðýda
                     * canlý hayvan sayýsý yeniden hesaplanýr.
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
                     * Rahmiye davranýþý:
                     * hafta içi saat 15:00 sonrasý günlük ölüm giriþi beklenir.
                     * Hafta sonu bu günlük alarm açýk tutulmaz.
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
                                ? "Saat 15:00 sonrasý günlük ölü hayvan sayýsý girilmemiþ."
                                : "Günlük ölü hayvan kaydý kontrol edildi."));

                    /*
                     * Önceki hafta sonu kaydý pazartesiden itibaren kontrol edilir.
                     * Kullanýcý salý/çarþamba düzeltme yaparsa alarmýn kapanabilmesi için
                     * yalnýzca pazartesiyle sýnýrlamýyoruz.
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
                                    ? "Önceki Cumartesi/Pazar için ölü hayvan kaydý eksik."
                                    : "Önceki hafta sonu ölü hayvan kayýtlarý tamam."));
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
                     * Baþarýsýz kontrolde son kontrol zamaný güncellenmez;
                     * sonraki worker turunda tekrar denenir.
                     */
                    _logger.LogError(
                        ex,
                        "Kümes {KumesId} hayvan kayýt kontrolleri yapýlamadý.",
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
                 * Worker'ýn yönettiði kümeslere ait bütün alarm tanýmlarý alýnýr.
                 * Burada PLC alarmýna özel filtre uygulanmaz. Böylece daha sonra
                 * AlarmId hesaplanmaz; AlarmEslemeSozcugu üzerinden gerçek alarm
                 * tanýmý bulunur.
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
                        "Ayný kümes ve alarm eþleme sözcüðü için birden fazla " +
                        "alarm tanýmý bulundu. En küçük Alarm Id kullanýldý. " +
                        "Tanýmlar: {AlarmTanimlari}",
                        string.Join(" | ", tekrarliTanimlar));
                }

                /*
                 * Worker'ýn kullandýðý AlarmEslemeSozcugu deðerlerinden eksik olanlarý
                 * baþlangýçta/yenilemede raporla.
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
                        "Alarmlar tablosunda eksik AlarmEslemeSozcugu tanýmlarý var. " +
                        "Eksik tanýmlar: {EksikAlarmTanimlari}",
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
        /// Ayný kümese ait alarm durumlarýný tek sorgu ve tek SaveChanges ile
        /// Alarmlar.AlarmEslemeSozcugu üzerinden senkronize eder.
        ///
        /// AlarmId hiçbir þekilde KumesId üzerinden hesaplanmaz.
        /// AlarmEnum'a baðýmlýlýk yoktur.
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
                            "Kümes alarm tanýmý bulunamadý. " +
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
                 * Kümese ait aktif alarm kayýtlarý bir kere alýnýr.
                 * Sonrasýnda gerçek AlarmId üzerinden bellekte eþleþtirme yapýlýr.
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
                                "Kümes alarmý oluþturuldu. " +
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
                    "Kümes alarm durumlarý güncellenemedi. KumesId: {KumesId}",
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
             * Silindi=true placeholder kayýtlarý da mevcut kabul edilir.
             * Böylece her çalýþmada ayný gün için tekrar placeholder üretilmez.
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
                         * Placeholder gerçek kullanýcý giriþi sayýlmasýn diye Silindi=true.
                         * Günlük ölüm alarm sorgularý sadece Silindi != true kayýtlarý sayar.
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
                "Kümes {KumesId} için {KayitSayisi} adet aylýk hayvan log placeholder kaydý oluþturuldu.",
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
                _logger.LogError(ex, "Verim Gýda Ticari Yumurta worker durdurulurken hata oluþtu.");
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