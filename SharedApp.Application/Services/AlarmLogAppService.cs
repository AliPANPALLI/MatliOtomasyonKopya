using DnsClient.Internal;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Application.ViewModels.RequestModel;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class AlarmLogAppService : AppService<AlarmLog, AlarmLogViewModel>, IAlarmLogAppService
    {
        private readonly IAlarmLogRepository _alarmLogRepository;
        private readonly IYanginFotografDosyaService _fotografDosyaService;
        private readonly ILogger<AlarmLogAppService> _logger;

        public AlarmLogAppService(IMapper mapper, IAlarmLogRepository alarmLogRepository, IYanginFotografDosyaService fotografDosyaService, ILogger<AlarmLogAppService> logger) : base(mapper, alarmLogRepository)
        {
            _alarmLogRepository = alarmLogRepository;
            _fotografDosyaService = fotografDosyaService;
            _logger = logger;
        }
        public async Task<KameraAlarmResult> AlarmBildirAsync(KameraAlarmRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentException("İstek bilgisi gönderilmedi.");

            var alarmTip = AlarmTipNormalizeEt(request.AlarmTip);

            if (string.IsNullOrWhiteSpace(alarmTip))
                throw new ArgumentException("Geçersiz alarm tipi.");

            var kayitZamani = DateTime.Now;
            string alarmEslemeSozcugu;

            /*
             * Recorder ve sistem hataları kamera bazlı değildir.
             * Bu nedenle kamera numarası eklenmez.
             */
            string? kameraNo = null;

            if (alarmTip is "RECORDERR" or "SYSERR")
            {
                alarmEslemeSozcugu = alarmTip;
            }
            else
            {
                kameraNo = KameraNoNormalizeEt(request.KameraNo);

                if (string.IsNullOrWhiteSpace(kameraNo))
                {
                    throw new ArgumentException(
                        $"{request.AlarmTip} alarmı için kamera numarası zorunludur.");
                }

                alarmEslemeSozcugu = $"{alarmTip}_{kameraNo}";
            }

            var alarm = await _alarmLogRepository.GetByAlarmEslemeSozcuguAsync(alarmEslemeSozcugu, cancellationToken);

            if (alarm == null)
            {
                throw new KeyNotFoundException($"'{alarmEslemeSozcugu}' eşleme sözcüğüne ait alarm tanımı bulunamadı.");
            }
            //await _alarmLogRepository.AddOrUpdateAlarmLogAsync(alarm, cancellationToken);
            var alarmLogId = await _alarmLogRepository.AddOrUpdateAlarmLogAsync(alarm, cancellationToken);
            KaydedilenFotografBilgisi? kaydedilenFotograf = null;
            string? fotografUyarisi = null;
            var fotografGeldi = !string.IsNullOrWhiteSpace(request.AnlikFotografBase64);
            /* Fotoğraf hatası alarm kaydını engellememeli. Ancak hatayı sessizce de yutmuyoruz. */
            if (fotografGeldi)
            {
                try
                {
                    kaydedilenFotograf = await _fotografDosyaService.KaydetAsync(request.AnlikFotografBase64!, request.AnlikFotografContentType, cancellationToken);
                }
                catch (ArgumentException exception)
                {
                    fotografUyarisi = exception.Message;

                    _logger.LogWarning(exception, "Alarm fotoğrafı doğrulanamadı. " + "AlarmLogId: {AlarmLogId}, KameraNo: {KameraNo}", alarmLogId, kameraNo);
                }
                catch (Exception exception)
                {
                    fotografUyarisi = "Alarm işlendi ancak fotoğraf dosyaya kaydedilemedi.";

                    _logger.LogError(exception, "Alarm fotoğrafı kaydedilemedi. " + "AlarmLogId: {AlarmLogId}, KameraNo: {KameraNo}", alarmLogId, kameraNo);
                }
            }

            var yanginAlgilamaLog = new YanginAlgilamaLogs
            {
                AlarmLogId = alarmLogId,
                AlarmTip = alarmTip,
                KameraNo = kameraNo,
                KayitZamani = kayitZamani,
                NodeRedKayitZamani = request.KayitZamani,
                Silindi = false,
                AnlikFotografDosyaYolu = kaydedilenFotograf?.GoreliDosyaYolu,
                AnlikFotografDosyaAdi = kaydedilenFotograf?.DosyaAdi,
                AnlikFotografContentType = kaydedilenFotograf?.ContentType,
                AnlikFotografBoyutu = kaydedilenFotograf?.BoyutByte
            };


            try
            {
                await _alarmLogRepository.AddYanginAlgilamaLogAsync(yanginAlgilamaLog, cancellationToken);
            }
            catch (Exception exception)
            {
                /*
                 * Dosya diske kaydedildi ancak veritabanı kaydı oluşmadıysa
                 * sahipsiz dosya kalmasını engelliyoruz.
                 */
                if (kaydedilenFotograf != null)
                {
                    await _fotografDosyaService.DosyaSilAsync(kaydedilenFotograf.GoreliDosyaYolu, cancellationToken);
                }

                _logger.LogError(exception, "Yangın algılama detay kaydı oluşturulamadı. " + "AlarmLogId: {AlarmLogId}", alarmLogId);
            }
            return new KameraAlarmResult
            {
                AlarmLogId = alarmLogId,
                AlarmIslendi = true,
                FotografGeldi = fotografGeldi,
                FotografKaydedildi = kaydedilenFotograf != null,
                FotografDosyaYolu = kaydedilenFotograf?.GoreliDosyaYolu,
                Uyari = fotografUyarisi
            };
        }

        private static string KameraNoNormalizeEt(string? kameraNo)
        {
            if (string.IsNullOrWhiteSpace(kameraNo))
                return string.Empty;

            var sonuc = kameraNo.Trim();

            if (sonuc.StartsWith(
                    "Kamera",
                    StringComparison.OrdinalIgnoreCase))
            {
                sonuc = sonuc["Kamera".Length..].Trim();
            }

            return sonuc;
        }

        private static string AlarmTipNormalizeEt(string? alarmTip)
        {
            if (string.IsNullOrWhiteSpace(alarmTip))
                return string.Empty;

            var tip = alarmTip.Trim().ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);

            return tip switch
            {
                "yangin" or
                "yangın" or
                "fire" or
                "firefire"
                    => "YANGIN",

                "smoke" or
                "duman"
                   => "DUMAN",

                "videoloss" or
                "videokaybi" or
                "videokaybı"
                    => "VIDEOLOSS",

                "recorderr" or
                "recorderror" or
                "recordingerror" or
                "kayithatasi" or
                "kayıthatası"
                    => "RECORDERR",

                "syserr" or
                "systemerror" or
                "sistemhatası" or
                "sistemhatasi"
                    => "SYSERR",

                _ => string.Empty
            };
        }
    }
}