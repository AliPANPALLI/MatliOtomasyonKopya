using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuHatScada.Application.Interfaces;
using SuHatScada.Application.ViewModels;
using System.Security.Claims;

namespace SuHatScada.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuKaynakController : BaseApiController<SuKaynakViewModel>
    {
        private readonly ISuKaynakAppService _suKaynakAppService;

        public SuKaynakController(ISuKaynakAppService suKaynakAppService) : base(suKaynakAppService)
        {
            _suKaynakAppService = suKaynakAppService;
        }

        [HttpGet("GetAllSuKaynakForYetki")]
        public async Task<IActionResult> GetAllKumesForYetki()
        {
            var result = await _suKaynakAppService.GetAllSuKaynakForYetki();

            return Ok(result);
        }

        [HttpGet("GetAllSuKaynakCizimForYetki")]
        public async Task<IActionResult> GetAllSuKaynakCizimForYetki()
        {
            var result = await _suKaynakAppService.GetAllSuKaynakCizimForYetki();

            return Ok(result);
        }

        [HttpGet("GetAllCizimForYetki")]
        public async Task<IActionResult> GetAllCizimForYetki()
        {
            var result = await _suKaynakAppService.GetAllCizimForYetki();

            return Ok(result);
        }

        [HttpGet("GetLokasyonIdsBySuKaynakIds")]
        public async Task<IActionResult> GetLokasyonIdsBySuKaynakIds([FromQuery] List<int> suKaynakIds)
        {
            var result = await _suKaynakAppService.GetLokasyonIdsBySuKaynakIds(suKaynakIds);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("GetSuHatRaporByCizimId")]
        public async Task<IActionResult> GetSuHatRaporByCizimId(int cizimId, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip)
        {
            var result = await _suKaynakAppService.GetSuHatRaporByCizimId(cizimId, baslangicZamani, bitisZamani, raporlamaTip);

            return new JsonResult(result);
        }

        [AllowAnonymous]
        [HttpGet("GetRaporLokasyonlarByCizimId")]
        public async Task<IActionResult> GetRaporLokasyonlarByCizimId(int cizimId)
        {
            var result = await _suKaynakAppService.GetRaporLokasyonlarByCizimId(cizimId);

            return new JsonResult(result);
        }

        [AllowAnonymous]
        [HttpGet("GetSuHatRaporByLokasyonIds")]
        public async Task<IActionResult> GetSuHatRaporByLokasyonIds([FromQuery] List<int> lokasyonIds,    [FromQuery] DateTime baslangicZamani,    [FromQuery] DateTime bitisZamani,    [FromQuery] string raporlamaTip)
        {
            var result = await _suKaynakAppService.GetSuHatRaporByLokasyonIds(lokasyonIds, baslangicZamani, bitisZamani, raporlamaTip);

            return new JsonResult(result);
        }

        [AllowAnonymous]
        [HttpGet("GetSuHatRaporByCizimIdYeni")]
        public async Task<IActionResult> GetSuHatRaporByCizimIdYeni(int cizimId, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip)
        {
            var result = await _suKaynakAppService.GetSuHatRaporByCizimIdYeni(cizimId, baslangicZamani, bitisZamani, raporlamaTip);

            return new JsonResult(result);
        }

        [AllowAnonymous]
        [HttpGet("GetLokasyonNamesForSuHatRaporByCizimId")]
        public async Task<IActionResult> GetLokasyonNamesForSuHatRaporByCizimId(int cizimId)
        {
            var result = await _suKaynakAppService.GetLokasyonNamesForSuHatRaporByCizimId(cizimId);

            return new JsonResult(result);
        }

        [AllowAnonymous]
        [HttpGet("GetSuHatRaporByLokasyonId")]
        public async Task<IActionResult> GetSuHatRaporByLokasyonId(int lokasyonId, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip)
        {
            var result = await _suKaynakAppService.GetSuHatRaporByLokasyonId(lokasyonId, baslangicZamani, bitisZamani, raporlamaTip);

            return new JsonResult(result);
        }

        [HttpGet("GetAlarmLogForSuIzleme")]
        public async Task<IActionResult> GetAlarmLogForSuIzleme(int lokasyonId)
        {
            var result = await _suKaynakAppService.GetAlarmLogForSuIzleme(lokasyonId);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("GetAktifAlarmLogForSuIzleme")]
        public async Task<IActionResult> GetAktifAlarmLogForSuIzleme(int cizimId)
        {
            var result = await _suKaynakAppService.GetAktifAlarmLogForSuIzleme(cizimId);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("GetPasifAlarmLogForSuIzleme")]
        public async Task<IActionResult> GetPasifAlarmLogForSuIzleme(int cizimId)
        {
            var result = await _suKaynakAppService.GetPasifAlarmLogForSuIzleme(cizimId);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("GetAktifAlarmLogForSuIzlemeByLokasyonId")]
        public async Task<IActionResult> GetAktifAlarmLogForSuIzlemeByLokasyonId(int lokasyonId)
        {
            var result = await _suKaynakAppService.GetAktifAlarmLogForSuIzlemeByLokasyonId(lokasyonId);

            return Ok(result);
        }

        [HttpGet("GetSuHatPlcAyarlar")]
        public async Task<IActionResult> GetSuHatPlcAyarlar()
        {
            //var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            //if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }

            var result = await _suKaynakAppService.GetSuHatPlcAyarlar();

            return new JsonResult(result);
        }

        [HttpGet("GetSuHatPlcAyarlarBySuLokasyonId")]
        public async Task<IActionResult> GetSuHatPlcAyarlarBySuLokasyonId(int suLokasyonId)
        {
            //var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            //if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }

            var result = await _suKaynakAppService.GetSuHatPlcAyarlarBySuLokasyonId(suLokasyonId);

            return new JsonResult(result);
        }

        [HttpPut("SuKaynakHaberlesmeBilgileriDegistir")]
        public async Task<IActionResult> SuKaynakHaberlesmeBilgileriDegistir(int suLokasyonId, string plcIpAdres, bool plcAutoConnect, bool aktif)
        {
            await _suKaynakAppService.SuKaynakHaberlesmeBilgileriDegistir(suLokasyonId, plcIpAdres, plcAutoConnect, aktif);

            return Ok();
        }
    }
}