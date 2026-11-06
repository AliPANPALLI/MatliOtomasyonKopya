using Microsoft.Extensions.Configuration;
using SharedApp.Domain.Models;
using System.Net;
using System.Text;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;

namespace SharedApp.Infrastructure.Sender
{
    public interface ISesliMesajSender
    {
        Task<string> SendVoiceCallAsync(MesajGonderimTakip mesaj);
        //Task<NetgsmVoiceReport?> GetReportAsync(string callId);
    }
    public class SesliMesajSender : ISesliMesajSender
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        public SesliMesajSender(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<string> SendVoiceCallAsync(string message, List<string> phoneNumberList)
        {
            string usercode = "2243341246" /*_config["Netgsm:Usercode"]*/;
            string password = "E9.wd9KF"/*_config["Netgsm:Password"]*/;
            string appkey = "E9.wd9KF" /*_config["Netgsm:AppKey"]*/;// opsiyonel
            string body = "";
            body += "<?xml version='1.0' encoding='UTF-8'?>";
            body += "<mainbody>";
            body += "<header>";
            body += "<usercode>2243341246</usercode>";
            body += "<password>E9.wd9KF</password>";
            body += "<key>0</key>";
            body += "<appkey>E9.wd9KF</appkey>";
            body += "<url>https://otomasyon.matli.com.tr:44313/shared/NetgsmReport/report</url>";
            body += "</header>";
            body += "<body>";
            body += $"<text>{System.Security.SecurityElement.Escape(message)}</text>";

            foreach (var phoneNumber in phoneNumberList)
            {
                body += $"<no>{phoneNumber}</no>";
            }

            body += "</body>";
            body += "</mainbody>";

            var content = new StringContent(body, Encoding.UTF8, "application/xml");

            var response = await _httpClient.PostAsync("https://api.netgsm.com.tr/voicesms/send", content);

            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode || result.StartsWith("ERR"))
            {
                throw new Exception($"Netgsm Hatası: {result}");
            }

            var parts = result.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2 && parts[0] == "00")
            {
                var callId = parts[1];
                return callId;
            }

            throw new Exception($"Beklenmeyen Netgsm Yanıtı: {result}");
        }
        public async Task<string> SendVoiceCallAsync(MesajGonderimTakip mesaj)
        {
            string usercode = "2243341246" /*_config["Netgsm:Usercode"]*/;
            string password = "E9.wd9KF"/*_config["Netgsm:Password"]*/;
            string appkey = "E9.wd9KF" /*_config["Netgsm:AppKey"]*/;// opsiyonel
            string body = "";
            body += "<?xml version='1.0' encoding='UTF-8'?>";
            body += "<mainbody>";
            body += "<header>";
            body += "<usercode>2243341246</usercode>";
            body += "<password>E9.wd9KF</password>";
            body += "<key>0</key>";
            body += "<appkey>E9.wd9KF</appkey>";
            body += "<url>https://otomasyon.matli.com.tr:44313/shared/NetgsmReport/report</url>";
            //body += "<url>https://webhook.site/ae21eb82-650a-4f5a-a605-763c2c834afe</url>";
            body += "</header>";
            body += "<body>";
            //ss += "<audioid>sesid</audioid>";
            body += $"<text>{System.Security.SecurityElement.Escape(mesaj.SmsIcerik)}</text>";
            if (string.IsNullOrEmpty(mesaj.SesDosyaYolu)) body += $"<no>{mesaj.TelefonNo}</no>";
            else body += $"<audioid>{mesaj.SesDosyaYolu}</audioid>";
            body += "</body>";
            body += "</mainbody>";
            //// Ses dosyası yüklendiyse audioid ile, yoksa TTS ile gönder
            //var body = $@"
            //            <?xml version='1.0' encoding='UTF-8'?>
            //            <mainbody>
            //              <header>
            //                <usercode>{usercode}</usercode>
            //                <password>{password}</password>

            //                <key>0</key>
            //                <appkey>{appkey}</appkey>
            //              </header>
            //              <body>
            //                {(string.IsNullOrEmpty(mesaj.SesDosyaYolu)
            //                   ? $"<text>{System.Security.SecurityElement.Escape(mesaj.SmsIcerik)}</text>"
            //                   : $"<audioid>{mesaj.SesDosyaYolu}</audioid>")}
            //                <no>90{mesaj.TelefonNo}</no>
            //              </body>
            //            </mainbody>";

            var content = new StringContent(body, Encoding.UTF8, "application/xml");
            var response = await _httpClient.PostAsync("https://api.netgsm.com.tr/voicesms/send", content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode || result.StartsWith("ERR"))
            {
                throw new Exception($"Netgsm Hatası: {result}");
            }

            var parts = result.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[0] == "00")
            {
                var callId = parts[1];
                return callId;
            }
            throw new Exception($"Beklenmeyen Netgsm Yanıtı: {result}");
        }

        //TODO:sesli mesaj iptali

        //public async Task<NetgsmVoiceReport?> GetReportAsync(string callId)
        //{
        //    var username = "2243341246";
        //    var password = "E9.wd9KF";

        //    var response = await _httpClient.GetAsync(
        //        $"https://api.netgsm.com.tr/voicesms/report/?usercode={username}&password={password}&bulkid={callId}&type=0");

        //    response.EnsureSuccessStatusCode();

        //    var xml = await response.Content.ReadAsStringAsync();

        //    // XML parse et
        //    var doc = XDocument.Parse(xml);
        //    var statusCode = doc.Descendants("status").FirstOrDefault()?.Value;
        //    var statusDesc = doc.Descendants("statusdesc").FirstOrDefault()?.Value;
        //    var answer = doc.Descendants("answer").FirstOrDefault()?.Value;
        //    var duration = doc.Descendants("duration").FirstOrDefault()?.Value;

        //    return new NetgsmVoiceReport
        //    {
        //        CallId = callId,
        //        StatusCode = statusCode,
        //        StatusDesc = statusDesc,
        //        Answered = answer == "1",
        //        Duration = int.TryParse(duration, out var d) ? d : null
        //    };
        //}

        private async Task<string> RunVoiceCallAPI()
        {
            try
            {
                string ss = "";
                ss += "<?xml version='1.0' encoding='UTF-8'?>";
                ss += "<mainbody>";
                ss += "<header>";
                ss += "<usercode>2243341246</usercode>";
                ss += "<password>E9.wd9KF</password>";
                ss += "<startdate>27082025</startdate>";
                ss += "<starttime>1657</starttime>";
                ss += "<stopdate>27082025</stopdate>";
                ss += "<stoptime>1659</stoptime>";
                ss += "<key>0</key>";
                ss += "<appkey>E9.wd9KF</appkey>";
                ss += "</header>";
                ss += "<body>";
                //ss += "<audioid>sesid</audioid>";
                ss += "<text>Kümes 1 plc bağlantısı koptu. Sıcaklık düşük. Nem fazla.</text>\"";
                ss += "<no>905532087258</no>";
                ss += "<no>905379555164</no>";
                //ss += "<no>905551234568</no>";
                //ss += "<keys>";
                //ss += "   <keydetail>";
                //ss += "     <keyinfo>1</keyinfo>";
                //ss += "     <text>1i tuslayanlara okutulacak metin</text>";
                //ss += " </keydetail>";
                //ss += " <keydetail>";
                //ss += "     <keyinfo>2</keyinfo>";
                //ss += "   <audioid>sesid2</audioid>";
                //ss += " </keydetail>";
                //ss += "   </keys>";
                ss += "</body>";
                ss += "</mainbody>";
                var postAddress = "https://api.netgsm.com.tr/voicesms/send";
                WebClient wUpload = new WebClient();
                HttpWebRequest request = WebRequest.Create(postAddress) as HttpWebRequest;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                Byte[] bPostArray = Encoding.UTF8.GetBytes(ss);
                Byte[] bResponse = wUpload.UploadData(postAddress, "POST", bPostArray);
                Char[] sReturnChars = Encoding.UTF8.GetChars(bResponse);
                string sWebPage = new string(sReturnChars);
                return sWebPage;
            }
            catch
            {
                return "-1";
            }
        }
    }
}
public class NetgsmVoiceReport
{
    public string? CallId { get; set; }
    public string? StatusCode { get; set; }
    public string? StatusDesc { get; set; }
    public bool Answered { get; set; }
    public int? Duration { get; set; }
}