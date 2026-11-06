using Microsoft.Extensions.Configuration;
using SharedApp.Domain.Models;
using System.Security;
using System.Text;

namespace SharedApp.Infrastructure.Sender
{
    /// <summary>
    /// Netgsm sesli mesaj (sesli arama) gönderiminin sözleşmesi.
    /// </summary>
    public interface ISesliMesajSender
    {
        /// <summary>
        /// Kuyruktaki sesli mesaj kaydını Netgsm üzerinden arama olarak başlatır ve Netgsm'in verdiği arama kimliğini döner.
        /// </summary>
        Task<string> SendVoiceCallAsync(MesajGonderimTakip mesaj);
    }

    /// <summary>
    /// Netgsm sesli mesaj servisiyle metinden sese (TTS) ya da yüklenmiş ses dosyasıyla arama başlatır.
    /// Netgsm hesabı koda gömülmez; <c>Netgsm__UserCode</c>, <c>Netgsm__Password</c> ve <c>Netgsm__AppKey</c>
    /// ortam değişkenlerinden okunur.
    /// </summary>
    public class SesliMesajSender : ISesliMesajSender
    {
        private const string SendUrl = "https://api.netgsm.com.tr/voicesms/send";
        private const string ReportCallbackUrl = "https://otomasyon.matli.com.tr:44313/shared/NetgsmReport/report";

        private readonly HttpClient _httpClient;
        private readonly string _userCode;
        private readonly string _password;
        private readonly string _appKey;

        /// <summary>
        /// HTTP istemcisini ve Netgsm hesabını konfigürasyondan alır.
        /// </summary>
        public SesliMesajSender(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _userCode = config["Netgsm:UserCode"] ?? string.Empty;
            _password = config["Netgsm:Password"] ?? string.Empty;
            _appKey = config["Netgsm:AppKey"] ?? string.Empty;
        }

        /// <summary>
        /// Verilen metni TTS ile okuyan sesli aramayı listedeki tüm numaralara başlatır.
        /// </summary>
        /// <returns>Netgsm'in döndürdüğü arama kimliği.</returns>
        /// <exception cref="Exception">Netgsm hata döndürürse veya cevap beklenen biçimde değilse.</exception>
        public async Task<string> SendVoiceCallAsync(string message, List<string> phoneNumberList)
        {
            var body = new StringBuilder();
            body.Append("<?xml version='1.0' encoding='UTF-8'?>");
            body.Append("<mainbody>");
            body.Append(BuildHeaderXml());
            body.Append("<body>");
            body.Append($"<text>{SecurityElement.Escape(message)}</text>");
            foreach (var phoneNumber in phoneNumberList)
            {
                body.Append($"<no>{phoneNumber}</no>");
            }
            body.Append("</body>");
            body.Append("</mainbody>");

            return await PostAsync(body.ToString());
        }

        /// <inheritdoc />
        public async Task<string> SendVoiceCallAsync(MesajGonderimTakip mesaj)
        {
            var body = new StringBuilder();
            body.Append("<?xml version='1.0' encoding='UTF-8'?>");
            body.Append("<mainbody>");
            body.Append(BuildHeaderXml());
            body.Append("<body>");
            body.Append($"<text>{SecurityElement.Escape(mesaj.SmsIcerik)}</text>");
            if (string.IsNullOrEmpty(mesaj.SesDosyaYolu)) body.Append($"<no>{mesaj.TelefonNo}</no>");
            else body.Append($"<audioid>{mesaj.SesDosyaYolu}</audioid>");
            body.Append("</body>");
            body.Append("</mainbody>");

            return await PostAsync(body.ToString());
        }

        /// <summary>
        /// Netgsm isteğinin hesap bilgilerini ve rapor geri çağırma adresini içeren XML başlığını oluşturur.
        /// </summary>
        private string BuildHeaderXml() =>
            "<header>" +
            $"<usercode>{SecurityElement.Escape(_userCode)}</usercode>" +
            $"<password>{SecurityElement.Escape(_password)}</password>" +
            "<key>0</key>" +
            $"<appkey>{SecurityElement.Escape(_appKey)}</appkey>" +
            $"<url>{ReportCallbackUrl}</url>" +
            "</header>";

        /// <summary>
        /// XML gövdeyi Netgsm'e gönderir ve "00 &lt;aramaKimliği&gt;" cevabından arama kimliğini çıkarır.
        /// </summary>
        /// <exception cref="Exception">Netgsm hata döndürürse veya cevap beklenen biçimde değilse.</exception>
        private async Task<string> PostAsync(string body)
        {
            var content = new StringContent(body, Encoding.UTF8, "application/xml");
            var response = await _httpClient.PostAsync(SendUrl, content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode || result.StartsWith("ERR"))
            {
                throw new Exception($"Netgsm Hatası: {result}");
            }

            var parts = result.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[0] == "00")
            {
                return parts[1];
            }

            throw new Exception($"Beklenmeyen Netgsm Yanıtı: {result}");
        }
    }
}
