using HegCoolingScada.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HegCoolingScada.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class HegSogutmaRaporController : ControllerBase
    {
        private readonly IHegSogutmaRaporAppService _hegSogutmaRaporAppService;

        public HegSogutmaRaporController(IHegSogutmaRaporAppService hegSogutmaRaporAppService)
        {
            _hegSogutmaRaporAppService = hegSogutmaRaporAppService;
        }

        [HttpGet("Odalar")]
        public async Task<IActionResult> GetOdalar()
        {
            var result = await _hegSogutmaRaporAppService.GetOdalarAsync();
            return Ok(result);
        }

        [HttpGet("Kompresorler")]
        public async Task<IActionResult> GetKompresorler()
        {
            var result = await _hegSogutmaRaporAppService.GetKompresorlerAsync();
            return Ok(result);
        }

        [HttpGet("Vanalar")]
        public async Task<IActionResult> GetVanalar()
        {
            var result = await _hegSogutmaRaporAppService.GetVanalarAsync();
            return Ok(result);
        }

        [HttpGet("VanaDurumRaporu")]
        public async Task<IActionResult> GetVanaDurumRaporu(DateTime baslangic, DateTime bitis, string periyot = "saatlik", [FromQuery] List<short>? vanaIds = null)
        {
            var result = await _hegSogutmaRaporAppService.GetVanaDurumRaporuAsync(AsUtc(baslangic), AsUtc(bitis), periyot, vanaIds);
            return Ok(result);
        }

        [HttpGet("KompresorRaporu")]
        public async Task<IActionResult> GetKompresorRaporu(DateTime baslangic, DateTime bitis, string periyot = "saatlik", [FromQuery] List<short>? kompresorIds = null)
        {
            var result = await _hegSogutmaRaporAppService.GetKompresorRaporuAsync(AsUtc(baslangic), AsUtc(bitis), periyot, kompresorIds);
            return Ok(result);
        }

        // Postgres taraftaki kolonlar timestamptz; Npgsql, Kind=Unspecified DateTime'ları
        // reddediyor. API sözleşmesi gereği baslangic/bitis UTC olarak kabul edilir.
        private static DateTime AsUtc(DateTime value)
            => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
