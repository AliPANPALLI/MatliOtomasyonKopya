using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlarmGruplarController : BaseApiController<AlarmGruplarViewModel>
    {
        private readonly IAlarmGruplarAppService _alarmGruplarAppService;

        public AlarmGruplarController(IAlarmGruplarAppService alarmGruplarAppService) : base(alarmGruplarAppService)
        {
            _alarmGruplarAppService = alarmGruplarAppService;
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _alarmGruplarAppService.SetSilindi(id);

            return Ok();
        }
    }
}