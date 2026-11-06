using EnergyMonitoring.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnergyMonitoring.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlarmLogController : ControllerBase
    {
        private readonly IAlarmLogAppService _alarmLogAppService;

        public AlarmLogController(IAlarmLogAppService alarmLogAppService)
        {
            _alarmLogAppService = alarmLogAppService;
        }

        [AllowAnonymous]
        [HttpGet("GetAktifAlarmLogForEnerjiIzleme")]
        public async Task<IActionResult> GetAktifAlarmLogForEnerjiIzleme(int lokasyonId)
        {
            var result = await _alarmLogAppService.GetAktifAlarmLogForEnerjiIzleme(lokasyonId);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("GetPasifAlarmLogForEnerjiIzleme")]
        public async Task<IActionResult> GetPasifAlarmLogForEnerjiIzleme(int lokasyonId)
        {
            var result = await _alarmLogAppService.GetPasifAlarmLogForEnerjiIzleme(lokasyonId);
            return Ok(result);
        }
    }
}
