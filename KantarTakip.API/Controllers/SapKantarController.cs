
using KantarTakip.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KantarTakip.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SapKantarController : ControllerBase
    {
        private readonly ISapKantarService _sapKantarService;

        public SapKantarController(
            ISapKantarService sapKantarService)
        {
            _sapKantarService = sapKantarService;
        }


        [HttpGet("GetByDate")]
        public async Task<IActionResult> GetByDate(
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            [FromQuery] int? kantarId = null,
            CancellationToken cancellationToken = default)
        {
            if (startDate == default || endDate == default)
            {
                return BadRequest("Başlangıç ve bitiş tarihi zorunludur.");
            }

            if (endDate < startDate ||
                endDate.DayNumber - startDate.DayNumber > 6)
            {
                return BadRequest("En fazla 7 günlük tarih aralığı giriniz.");
            }

            if (kantarId.HasValue && kantarId.Value <= 0)
            {
                return BadRequest("Kantar ID sıfırdan büyük olmalıdır.");
            }

            var result = await _sapKantarService.GetByDateAsync(
                startDate,
                endDate,
                cancellationToken);

            // SAP'den gelen verileri kantar ID'ye göre filtrele
            if (kantarId.HasValue)
            {
                result = result
                    .Where(x => x.KantarId == kantarId.Value)
                    .ToList();
            }

            return Ok(result);
        }


        [HttpGet("GetByFisNo")]
        public async Task<IActionResult> GetByFisNo(
            [FromQuery] string fisNo,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fisNo) ||
                fisNo.Length > 20 ||
                !fisNo.All(char.IsDigit))
            {
                return BadRequest("Geçerli bir fiş numarası giriniz.");
            }

            var result = await _sapKantarService.GetByFisNoAsync(
                fisNo, cancellationToken);

            return Ok(result);
        }
    }
}
