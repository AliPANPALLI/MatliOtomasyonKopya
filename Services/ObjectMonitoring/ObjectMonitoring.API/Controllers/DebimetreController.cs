using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ObjectMonitoring.Application.Interfaces;

namespace ObjectMonitoring.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebimetreController : ControllerBase
    {
        private readonly IDebimetreAppService _debimetreAppService;

        public DebimetreController(IDebimetreAppService debimetreAppService)
        {
            _debimetreAppService = debimetreAppService;
        }
        [AllowAnonymous]
        [HttpGet("GetDebimetreRaporByLokasyonIds")]
        public async Task<IActionResult> GetDebimetreRaporByLokasyonIds([FromQuery] List<int> lokasyonIds, [FromQuery] DateTime baslangicZamani, [FromQuery] DateTime bitisZamani, [FromQuery] string raporlamaTip, CancellationToken ct)
        {
            var result = await _debimetreAppService.GetDebimetreRaporByLokasyonIds(lokasyonIds, baslangicZamani, bitisZamani, raporlamaTip,ct);

            return new JsonResult(result);
        }
        [AllowAnonymous]
        [HttpGet("test")]
        public async Task<IActionResult> test()
        {
            return Ok("Debimetre API çalýþýyor.");
        }
    }
}
