using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlarmlarController : BaseApiController<AlarmlarViewModel>
    {
        private readonly IAlarmlarAppService _alarmlarAppService;

        public AlarmlarController(IAlarmlarAppService alarmlarAppService) : base(alarmlarAppService)
        {
            _alarmlarAppService = alarmlarAppService;
        }
        [HttpGet("GetAllvwAlarmlarDx")]
        public async Task<IActionResult> GetAllvwAlarmlarDx()
        {
            var result = await _alarmlarAppService.GetAllvwAlarmlarDx();
            return Ok(result);
        }

        [HttpGet("GetAllvwAlarmlarByAlarmGrupIdDx")]
        public async Task<IActionResult> GetAllvwAlarmlarByAlarmGrupIdDx(int alarmGrupId)
        {
            var result = await _alarmlarAppService.GetAllvwAlarmlarByAlarmGrupIdDx(alarmGrupId);
            return Ok(result);
        }

        [HttpGet("GetAllAlarmlarByAlarmGrupId")]
        public async Task<IActionResult> GetAllAlarmlarByAlarmGrupId(int alarmGrupId)
        {
            var result = await _alarmlarAppService.GetAllAlarmlarByAlarmGrupId(alarmGrupId);
            return Ok(result);
        }

        [HttpPut("AddAlarmToAlarmGrup")]
        public async Task<IActionResult> AddAlarmToAlarmGrup(int alarmId, int alarmGrupId)
        {
            await _alarmlarAppService.AddAlarmToAlarmGrup(alarmId, alarmGrupId);
            return Ok();
        }

        [HttpPut("RemoveAlarmFromAlarmGrup")]
        public async Task<IActionResult> RemoveAlarmFromAlarmGrup(int alarmId, int alarmGrupId)
        {
            await _alarmlarAppService.RemoveAlarmFromAlarmGrup(alarmId, alarmGrupId);
            return Ok();
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _alarmlarAppService.SetSilindi(id);
            return Ok();
        }
    }
}