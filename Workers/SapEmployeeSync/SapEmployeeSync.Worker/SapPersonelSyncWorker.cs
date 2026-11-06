using Authentication.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using SapEmployeeSync.Application.Models;
using SapEmployeeSync.Infrastructure.Sap;

namespace SapEmployeeSync.Worker
{
    /// <summary>
    /// Günde bir kez (bkz. SapSettings.RunAtHour) SAP'teki ZABAP_PA_MAT_FM002
    /// ve ZHR_PERLIST03 servislerinden personel listesini çeker; SAP tablosuna
    /// hiçbir şey kaydetmez (SharedApp.Domain.Models.SapPersonel KULLANILMIYOR).
    ///
    /// Tek işi: Authentication.User tablosunda Ad+Soyad'ı SAP kaydıyla eşleşen
    /// kullanıcı varsa, o kullanıcının SicilNo alanını SAP'teki PERNR ile
    /// doldurmak. SicilNo'su zaten dolu olan kullanıcılara dokunulmaz; aynı
    /// ad+soyad'a sahip birden fazla kullanıcı varsa (belirsiz eşleşme)
    /// güvenlik için atlanır ve loglanır.
    /// </summary>
    public class SapPersonelSyncWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SapPaMatFm002Service _paMatFm002Service;
        private readonly SapPerlist03Service _perlist03Service;
        private readonly SapSettings _settings;
        private readonly ILogger<SapPersonelSyncWorker> _logger;

        public SapPersonelSyncWorker(
            IServiceProvider serviceProvider,
            SapPaMatFm002Service paMatFm002Service,
            SapPerlist03Service perlist03Service,
            IOptions<SapSettings> settings,
            ILogger<SapPersonelSyncWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _paMatFm002Service = paMatFm002Service;
            _perlist03Service = perlist03Service;
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var bekleme = SureNextRunUntil(DateTime.Now, _settings.RunAtHour);

                try
                {
                    await Task.Delay(bekleme, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                if (stoppingToken.IsCancellationRequested)
                    break;

                await SyncAsync(stoppingToken);
            }
        }

        /// <summary>
        /// Bir sonraki çalışma zamanına kadar beklenecek süreyi hesaplar —
        /// bugün o saat henüz gelmediyse bugün, geçtiyse yarın o saat.
        /// </summary>
        public static TimeSpan SureNextRunUntil(DateTime simdi, int runAtHour)
        {
            var saat = Math.Clamp(runAtHour, 0, 23);
            var bugunkuCalismaZamani = simdi.Date.AddHours(saat);
            var hedefZaman = bugunkuCalismaZamani > simdi
                ? bugunkuCalismaZamani
                : bugunkuCalismaZamani.AddDays(1);

            return hedefZaman - simdi;
        }

        public async Task SyncAsync(CancellationToken cancellationToken)
        {
            try
            {
                var listSonuclar = await _perlist03Service.GetPersonelListesiAsync(cancellationToken);
                var matSonuclar = await _paMatFm002Service.GetPersonelListesiAsync(pernr: null, cancellationToken);

                var merged = new Dictionary<string, SapPersonelRaw>();

                 foreach (var item in listSonuclar)
                {
                    if (merged.TryGetValue(item.Pernr, out var mevcut))
                    {
                        BosAlanlariTamamla(mevcut, item);
                    }
                    else
                    {
                        merged[item.Pernr] = item;
                    }
                }
                
                foreach (var item in matSonuclar)
                {
                    merged[item.Pernr] = item;
                }               

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

                var kullanicilar = await dbContext.User
                    .Where(x => x.Silindi != true)
                    .ToListAsync(cancellationToken);

                // Türkçe kurallarla normalize edilmiş ad -> kullanıcı(lar) sözlüğü.
                var kullaniciGruplari = kullanicilar
                    .Where(u => !string.IsNullOrWhiteSpace(u.Name) && !string.IsNullOrWhiteSpace(u.Surname))
                    .GroupBy(u => MetniNormalizeEt(u.Name))
                    .ToDictionary(g => g.Key, g => g.ToList());

                int guncellenen = 0, zatenDoluAtlanan = 0, eslesmeyen = 0, belirsizEslesme = 0;

                foreach (var raw in merged.Values)
                {
                    if (string.IsNullOrWhiteSpace(raw.Vorna) || string.IsNullOrWhiteSpace(raw.Nachn))
                    {
                        eslesmeyen++;
                        continue;
                    }

                    var adKey = MetniNormalizeEt(raw.Vorna);
                    if (!kullaniciGruplari.TryGetValue(adKey, out var ayniAddakiKullanicilar))
                    {
                        eslesmeyen++;
                        continue;
                    }

                    // Tam soyad eşleşmesini önceliklendir. Tam eşleşme yoksa iki
                    // taraftan birindeki ek soyadı kabul et
                    // Yalnızca sondaki soyad parçaları eşleşir.
                    var puanlanmisEslesmeler = ayniAddakiKullanicilar
                        .Select(u => new { Kullanici = u, Puan = SoyadEslesmePuani(u.Surname, raw.Nachn) })
                        .Where(x => x.Puan > 0)
                        .ToList();

                    if (puanlanmisEslesmeler.Count == 0)
                    {
                        eslesmeyen++;
                        continue;
                    }

                    var enYuksekPuan = puanlanmisEslesmeler.Max(x => x.Puan);
                    var eslesenler = puanlanmisEslesmeler
                        .Where(x => x.Puan == enYuksekPuan)
                        .Select(x => x.Kullanici)
                        .ToList();

                    if (eslesenler.Count > 1)
                    {
                        _logger.LogWarning(
                            "Belirsiz eşleşme: '{Vorna} {Nachn}' adında {Sayi} kullanıcı var, SicilNo={Pernr} atlandı.",
                            raw.Vorna, raw.Nachn, eslesenler.Count, raw.Pernr);
                        belirsizEslesme++;
                        continue;
                    }

                    var kullanici = eslesenler[0];
                    if (!string.IsNullOrWhiteSpace(kullanici.SicilNo))
                    {
                        zatenDoluAtlanan++;
                        continue;
                    }

                    kullanici.SicilNo = BastakiSifirlariAt(raw.Pernr);
                    guncellenen++;
                }

                if (guncellenen > 0)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation(
                    "SAP personel senkronu bitti — güncellenen: {Guncellenen}, zaten dolu (atlandı): {ZatenDolu}, eşleşmeyen: {Eslesmeyen}, belirsiz eşleşme: {Belirsiz}.",
                    guncellenen, zatenDoluAtlanan, eslesmeyen, belirsizEslesme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAP personel senkronu sırasında hata oluştu.");
            }
        }

        private static readonly CultureInfo TurkceKultur = CultureInfo.GetCultureInfo("tr-TR");

        /// <summary>
        /// SAP ve veritabanındaki metinleri Türkçe büyük/küçük harf kurallarıyla
        /// aynı biçime getirir. Birden fazla boşluk da tek boşluğa indirgenir.
        /// </summary>
        internal static string MetniNormalizeEt(string metin)
        {
            var bosluklariDuzeltilmis = string.Join(' ',
                metin.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

            return bosluklariDuzeltilmis
                .Normalize(NormalizationForm.FormC)
                .ToUpper(TurkceKultur);
        }

        /// <summary>
        /// 2: tam soyad eşleşmesi, 1: taraflardan birinde ek soyad var, 0: eşleşmez.
        /// Ek soyad eşleşmesi yalnızca kelime sınırında ve sondan yapılır.
        /// </summary>
        internal static int SoyadEslesmePuani(string kullaniciSoyadi, string sapSoyadi)
        {
            var kullanici = MetniNormalizeEt(kullaniciSoyadi);
            var sap = MetniNormalizeEt(sapSoyadi);

            if (string.Equals(kullanici, sap, StringComparison.Ordinal))
                return 2;

            return sap.EndsWith($" {kullanici}", StringComparison.Ordinal)
                || kullanici.EndsWith($" {sap}", StringComparison.Ordinal)
                    ? 1
                    : 0;
        }

        /// <summary>SAP PERNR'ı "00004369" gibi sıfırla dolduruyor — SicilNo'ya baştaki sıfırlar atılarak yazılır.</summary>
        private static string BastakiSifirlariAt(string pernr)
        {
            var atilmis = pernr.TrimStart('0');
            return atilmis.Length > 0 ? atilmis : "0";
        }

        /// <summary>Hedefte boş olan alanları kaynaktan tamamlar — mevcut dolu alanlara dokunmaz.</summary>
        private static void BosAlanlariTamamla(SapPersonelRaw hedef, SapPersonelRaw kaynak)
        {
            hedef.Begda ??= kaynak.Begda;
            hedef.Endda ??= kaynak.Endda;
            hedef.Nachn ??= kaynak.Nachn;
            hedef.Vorna ??= kaynak.Vorna;
            hedef.Btrtl ??= kaynak.Btrtl;
            hedef.Btext ??= kaynak.Btext;
            hedef.Plans ??= kaynak.Plans;
            hedef.Plstx ??= kaynak.Plstx;
            hedef.Orgeh ??= kaynak.Orgeh;
            hedef.Orgtx ??= kaynak.Orgtx;
            hedef.Persk ??= kaynak.Persk;
            hedef.Ptext ??= kaynak.Ptext;
            hedef.Usrid ??= kaynak.Usrid;
            hedef.Pernr1 ??= kaynak.Pernr1;
            hedef.Orgeh1 ??= kaynak.Orgeh1;
            hedef.Gbdat ??= kaynak.Gbdat;
            hedef.Stat2 ??= kaynak.Stat2;
            hedef.Begda2 ??= kaynak.Begda2;
            hedef.Bukrs ??= kaynak.Bukrs;
            hedef.Butxt ??= kaynak.Butxt;
            hedef.Werks ??= kaynak.Werks;
            hedef.Name1 ??= kaynak.Name1;
            hedef.GsmPrivate ??= kaynak.GsmPrivate;
            hedef.GsmBusiness ??= kaynak.GsmBusiness;
            hedef.TcLast4 ??= kaynak.TcLast4;
        }
    }
}
