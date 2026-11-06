using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlarmKullaniciEslemeController : BaseApiController<AlarmKullaniciEslemeViewModel>
    {
        private readonly IAlarmKullaniciEslemeAppService _alarmKullaniciEslemeAppService;

        public AlarmKullaniciEslemeController(IAlarmKullaniciEslemeAppService alarmKullaniciEslemeAppService) : base(alarmKullaniciEslemeAppService)
        {
            _alarmKullaniciEslemeAppService = alarmKullaniciEslemeAppService;
        }

        [HttpGet("GetAllvwAlarmKullaniciEsleme")]
        public async Task<IActionResult> GetAllvwAlarmKullaniciEsleme()
        {
            var result = await _alarmKullaniciEslemeAppService.GetAllvwAlarmKullaniciEsleme();

            return Ok(result);
        }

        [HttpGet("GetAlarmKullaniciEslemelerForEdit")]
        public async Task<IActionResult> GetAlarmKullaniciEslemelerForEdit(int alarmKullaniciEslemeId)
        {
            var result = await _alarmKullaniciEslemeAppService.GetAlarmKullaniciEslemelerForEdit(alarmKullaniciEslemeId);

            return Ok(result);
        }

        [HttpGet("GetAlarmKullaniciEslemeVarMiKontrolEt")]
        public async Task<IActionResult> GetAlarmKullaniciEslemeVarMiKontrolEt(int alarmGrupId, string bildirimYontemi)
        {
            var result = await _alarmKullaniciEslemeAppService.GetAlarmKullaniciEslemeVarMiKontrolEt(alarmGrupId, bildirimYontemi);

            return Ok(result);
        }

        [HttpPost("AddEditAlarmKullaniciEsleme")]
        public async Task<IActionResult> AddEditAlarmKullaniciEsleme([FromForm] int alarmGrupId, [FromForm] List<string> secilenKullanicilarSeviye1, [FromForm] List<string> secilenKullanicilarSeviye2, [FromForm] List<string> secilenKullanicilarSeviye3,
             [FromForm] string bildirimYontemi, [FromForm] bool aktif, [FromForm] string bildirimSuresiSeviye1, [FromForm] string bildirimSuresiSeviye2, [FromForm] string bildirimSuresiSeviye3, [FromForm] bool tekrarlansinMi, [FromForm] bool silindi,
             [FromForm] bool seviyeAktif1, [FromForm] bool seviyeAktif2, [FromForm] bool seviyeAktif3)
        {
            await _alarmKullaniciEslemeAppService.AddEditAlarmKullaniciEsleme(alarmGrupId, secilenKullanicilarSeviye1, secilenKullanicilarSeviye2, secilenKullanicilarSeviye3, bildirimYontemi, aktif, bildirimSuresiSeviye1,
             bildirimSuresiSeviye2, bildirimSuresiSeviye3, tekrarlansinMi, silindi, seviyeAktif1, seviyeAktif2, seviyeAktif3);
            return Ok();
        }

        [HttpPost("EditAlarmKullaniciEsleme")]
        public async Task<IActionResult> EditAlarmKullaniciEsleme([FromForm] int alarmKullaniciEslemeId, [FromForm] string bildirimYontemi, [FromForm] bool aktif, [FromForm] string bildirimSuresi, [FromForm] bool tekrarlansinMi, [FromForm] bool silindi)
        {
            await _alarmKullaniciEslemeAppService.EditAlarmKullaniciEsleme(alarmKullaniciEslemeId, bildirimYontemi, aktif, bildirimSuresi, tekrarlansinMi, silindi);
            return Ok();
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _alarmKullaniciEslemeAppService.SetSilindi(id);

            return Ok();
        }

        [HttpPost("AddAlarmKullaniciEsleme2")]
        public async Task<IActionResult> AddAlarmKullaniciEsleme2([FromForm] List<string> secilenKullanicilar, [FromForm] List<int> secilenAlarmGruplar, [FromForm] List<int> secilenAlarmlar, [FromForm] bool silindi, [FromForm] bool aktif,
           [FromForm] bool seviyeAktif, [FromForm] int seviyeNo, [FromForm] string bildirimSuresi, [FromForm] string bildirimYontemi, [FromForm] bool tekrarlansinMi, [FromForm] string tekrarlamaSuresi)
        {
            await _alarmKullaniciEslemeAppService.AddAlarmKullaniciEsleme2(secilenKullanicilar, secilenAlarmGruplar, secilenAlarmlar, silindi, aktif, seviyeAktif, seviyeNo, bildirimSuresi, bildirimYontemi, tekrarlansinMi, tekrarlamaSuresi);

            return Ok();
        }

        [HttpPost("EditAlarmKullaniciEsleme2")]
        public async Task<IActionResult> EditAlarmKullaniciEsleme2([FromForm] int alarmKullaniciEslemeId, [FromForm] bool silindi, [FromForm] bool aktif, [FromForm] bool seviyeAktif, [FromForm] int seviyeNo, [FromForm] string bildirimSuresi,
            [FromForm] string bildirimYontemi, [FromForm] bool tekrarlansinMi, [FromForm] string tekrarlamaSuresi)
        {
            await _alarmKullaniciEslemeAppService.EditAlarmKullaniciEsleme2(alarmKullaniciEslemeId, silindi, aktif, seviyeAktif, seviyeNo, bildirimSuresi, bildirimYontemi, tekrarlansinMi, tekrarlamaSuresi);

            return Ok();
        }
    }
}