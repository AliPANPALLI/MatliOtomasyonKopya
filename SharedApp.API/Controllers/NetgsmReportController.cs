using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using System.Text;
using System.Text.Json.Serialization;
using static SharedKernel.ApplicationConstants;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NetgsmReportController : ControllerBase
    {
        private readonly SharedDbContext _context;

        public NetgsmReportController(SharedDbContext db)
        {
            _context = db;
        }
        //[AllowAnonymous]
        //[HttpPost("report")]
        //public async Task<IActionResult> ReceiveReport([FromBody] XDocument xml)
        //{
        //    var report = xml.Descendants("report").FirstOrDefault();
        //    if (report == null) return BadRequest();

        //    var callId = report.Element("bulkid")?.Value;
        //    var status = report.Element("status")?.Value;
        //    var desc = report.Element("statusdesc")?.Value;
        //    var answer = report.Element("answer")?.Value == "1";
        //    var duration = int.TryParse(report.Element("duration")?.Value, out var d) ? d : (int?)null;

        //    var log = await _context.MesajGonderimTakip.FirstOrDefaultAsync(x => x.CallId == callId);
        //    if (log != null)
        //    {
        //        log.GonderimBasarili = answer;
        //        log.CevaplandiMi = answer;
        //        log.AramaSure = duration;
        //        log.HataMesaji = desc;
        //        await _context.SaveChangesAsync();
        //    }

        //    return Ok();
        //}

        [HttpGet("test-report")]
        public async Task<IActionResult> TestReport()
        {
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<mainbody>
  <report>
    <bulkid>123456</bulkid>
    <status>100</status>
    <statusdesc>Başarılı</statusdesc>
    <answer>1</answer>
    <duration>30</duration>
  </report>
</mainbody>";

            var content = new StringContent(xml, Encoding.UTF8, "application/xml");

            var httpClient = new HttpClient();
            var response = await httpClient.PostAsync("https://otomasyon.matli.com.tr:44313/shared/NetgsmReport/report", content);

            var responseContent = await response.Content.ReadAsStringAsync();

            return Ok(new { status = response.StatusCode, content = responseContent });
        }

        [AllowAnonymous]
        [HttpPost("report")]
        public async Task<IActionResult> ReceiveReport([FromBody] NetgsmVoiceReport report)
        {
            if (report == null)
                return BadRequest("Geçersiz veri.");

            var callId = report.bulkid.ToString();
            var answer = report.bilsec > 0;
            var duration = report.bilsec;

            var log = await _context.MesajGonderimTakip.FirstOrDefaultAsync(x => x.CallId == callId);
            if (log != null)
            {
                log.GonderimBasarili = answer;
                log.CevaplandiMi = answer;
                log.AramaSure = duration;
                log.HataMesaji = $"state: {report.state}, type: {report.type}";
                log.Durum = (byte?)(report.state >= 0 && report.state <= 9 ? (byte)report.state : (byte?)null);
                log.DurumAciklama = ((NetGsmDurumKodu)report.state).GetDescription();
                ;
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        //[AllowAnonymous]
        //[HttpPost("report")]
        //public async Task<IActionResult> ReceiveReport()
        //{
        //    using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        //    var rawXml = await reader.ReadToEndAsync();

        //    if (string.IsNullOrWhiteSpace(rawXml))
        //        return BadRequest("Boş XML geldi.");

        //    var xml = XDocument.Parse(rawXml);
        //    var report = xml.Descendants("report").FirstOrDefault();
        //    if (report == null) return BadRequest("Report bulunamadı.");

        //    var callId = report.Element("bulkid")?.Value;
        //    var status = report.Element("status")?.Value;
        //    var desc = report.Element("statusdesc")?.Value;
        //    var answer = report.Element("answer")?.Value == "1";
        //    var duration = int.TryParse(report.Element("duration")?.Value, out var d) ? d : (int?)null;

        //    var log = await _context.MesajGonderimTakip.FirstOrDefaultAsync(x => x.CallId == callId);
        //    if (log != null)
        //    {
        //        log.GonderimBasarili = answer;
        //        log.CevaplandiMi = answer;
        //        log.AramaSure = duration;
        //        log.HataMesaji = desc;
        //        await _context.SaveChangesAsync();
        //    }

        //    return Ok();
        //}

        [HttpPost("rapor")]
        [AllowAnonymous]
        public async Task<IActionResult> SesliSmsRapor()
        {
            try
            {
                // 1. Gövde (body) verisini oku
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var rawData = await reader.ReadToEndAsync();

                // 2. HTML karakterlerini decode et (isteğe bağlı)decodedData
                var decodedData = System.Net.WebUtility.HtmlDecode(rawData);

                // 3. Log dosyasına yaz
                var logKlasoru = "D:/Log/";
                var logDosyaAdi = "seslismsRapor.txt";
                var logDosyaYolu = Path.Combine(logKlasoru, logDosyaAdi);
                var log = new MesajGonderimTakip()
                {
                    SmsIcerik = decodedData
                };
                //var log = await _context.MesajGonderimTakip.FirstOrDefaultAsync(x => x.CallId == callId);

                //// Klasör yoksa oluştur
                //if (!Directory.Exists(logKlasoru))
                //    Directory.CreateDirectory(logKlasoru);

                //// Dosyaya ekle
                //await System.IO.File.AppendAllTextAsync(logDosyaYolu, decodedData + Environment.NewLine);

                return Ok("Rapor kaydedildi");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Hata oluştu: {ex.Message}");
            }
        }


    }
}
public class NetgsmVoiceReport
{
    [JsonPropertyName("bulkid")]
    public long? bulkid { get; set; }

    [JsonPropertyName("caller")]
    public string? caller { get; set; }

    [JsonPropertyName("callee")]
    public string? callee { get; set; }

    [JsonPropertyName("state")]
    public int? state { get; set; }

    [JsonPropertyName("type")]
    public int? type { get; set; }

    [JsonPropertyName("name")]
    public string? name { get; set; }

    [JsonPropertyName("creation_time")]
    public string? creation_time { get; set; }

    [JsonPropertyName("start_time")]
    public string start_time { get; set; }

    [JsonPropertyName("answer_time")]
    public string? answer_time { get; set; }

    [JsonPropertyName("hangup_time")]
    public string? hangup_time { get; set; }

    [JsonPropertyName("bilsec")]
    public int? bilsec { get; set; }

    [JsonPropertyName("relationid")]
    public string? relationid { get; set; }

    [JsonPropertyName("detail")]
    public Detail? detail { get; set; }

    public class Detail
    {
        [JsonPropertyName("push_button")]
        public int? push_button { get; set; }

        [JsonPropertyName("survey_taskid")]
        public int? survey_taskid { get; set; }
    }
}
