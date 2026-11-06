using KantarTakip.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KantarTakip.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public sealed class KantarKpiController : ControllerBase
    {
        private readonly IKantarKpiService _service;

        public KantarKpiController(IKantarKpiService service)
        {
            _service = service;
        }

        [HttpGet("GetRapor")]
        public async Task<IActionResult> GetRapor(
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            [FromQuery] int? kantarId = null,
            [FromQuery] string? plaka = null,
            [FromQuery] int? firmaId = null,
            CancellationToken cancellationToken = default)
        {
            if (startDate == default || endDate == default)
            {
                return BadRequest(
                    "Başlangıç ve bitiş tarihi zorunludur.");
            }

            if (endDate < startDate)
            {
                return BadRequest(
                    "Bitiş tarihi başlangıçtan önce olamaz.");
            }

            if (endDate.DayNumber - startDate.DayNumber > 6)
            {
                return BadRequest(
                    "En fazla 7 günlük tarih aralığı seçiniz.");
            }

            if (kantarId is <= 0 || firmaId is <= 0)
            {
                return BadRequest(
                    "Kantar ve firma ID pozitif olmalıdır.");
            }

            var result = await _service.GetRaporAsync(
                startDate,
                endDate,
                kantarId,
                plaka,
                firmaId,
                cancellationToken);

            return Ok(result);
        }
    }
}