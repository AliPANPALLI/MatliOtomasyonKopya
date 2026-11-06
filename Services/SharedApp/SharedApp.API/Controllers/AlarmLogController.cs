using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;
using SharedApp.Application.ViewModels.RequestModel;
using SharedApp.Infrastructure.Sender;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlarmLogController : BaseApiController<AlarmLogViewModel>
    {
        private readonly IAlarmLogAppService _alarmLogAppService;
        private readonly SmsSender _smsSender;
        private readonly SesliMesajSender _sesliMesajSender;

        public AlarmLogController(IAlarmLogAppService alarmLogAppService, SmsSender smsSender, SesliMesajSender sesliMesajSender) : base(alarmLogAppService)
        {
            _alarmLogAppService = alarmLogAppService;
            _smsSender = smsSender;
            _sesliMesajSender = sesliMesajSender;
        }

        //[HttpPost("ViyolYanginAlarmBildir")]
        //public async Task<IActionResult> ViyolYanginAlarmBildir([FromBody] KameraAlarmRequest request)
        //{
        //    if (string.IsNullOrWhiteSpace(request.AlarmTip))
        //        return BadRequest("Alarm tipi zorunludur.");

        //    var alarmTip = request.AlarmTip.Trim().ToUpperInvariant();

        //    var alarmMesaji = alarmTip switch
        //    {
        //        "FIRE" => $"{request.KameraNo} tarafından yangın tespit edildi.",
        //        "VIDEOLOSS" => $"{request.KameraNo} görüntü bağlantısı kesildi.",
        //        _ => null
        //    };

        //    if (alarmMesaji == null)
        //        return BadRequest("Geçersiz alarm tipi.");

        //    // Burada veritabanına kayıt, SignalR veya bildirim işlemi yapılabilir.

        //    return Ok(new
        //    {
        //        success = true,
        //        request.KameraNo,
        //        alarmTip,
        //        alarmMesaji
        //    });
        //}

        [HttpPost("ViyolYanginAlarmBildir")]
        public async Task<IActionResult> AlarmBildir([FromBody] KameraAlarmRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _alarmLogAppService.AlarmBildirAsync(request, cancellationToken);
                //return Ok("Alarm başarıyla işlendi.");
                return Ok(result);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(exception.Message);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception exception)
            {
                //_logger.LogError(exception, "Kamera alarmı işlenirken hata oluştu.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Kamera alarmı işlenirken bir hata oluştu.");
            }
        }

        //[HttpPost("NodeRedAlarmSMS")]
        //[AllowAnonymous]
        //public async Task<IActionResult> NodeRedSMS([FromBody] NodeRedNotificationRequest request)
        //{
        //    var phoneNumbers = new List<string> { "5316646978", "5379555164" };
        //    string durum = request.Status == "UP" ? "TEKRAR AKTIF" : "DURDU";
        //    string message = $"Node-RED servisi {durum}. " + $"Sunucu IP: {request.ServerIp}, " + $"Sunucu: {request.ServerName}, " + $"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
        //    _smsSender.SendSms(phoneNumbers, message);
        //    return Ok(new { success = true });
        //}

        //[HttpPost("NodeRedAlarmSesliMesaj")]
        //[AllowAnonymous]
        //public async Task<IActionResult> NodeRedSesliMesaj([FromBody] NodeRedNotificationRequest request)
        //{
        //    var phoneNumbers = new List<string> { "5316646978", "5379555164" };
        //    string durum = request.Status == "UP" ? "TEKRAR AKTIF" : "DURDU";
        //    string message = $"Node-RED servisi {durum}. " + $"Sunucu IP: {request.ServerIp}, " + $"Sunucu: {request.ServerName}, " + $"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
        //    await _sesliMesajSender.SendVoiceCallAsync(message, phoneNumbers);
        //    return Ok(new { success = true });
        //}
    }
}

public class NodeRedNotificationRequest
{
    public string ServerIp { get; set; }
    public string ServerName { get; set; }
    public string Service { get; set; }
    public string Status { get; set; }
    public DateTime EventTime { get; set; }
    public string Detail { get; set; }
}