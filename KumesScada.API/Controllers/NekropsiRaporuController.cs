using System.Security.Claims;
using KumesScada.Application.Interfaces;
using KumesScada.Application.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace KumesScada.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NekropsiRaporuController : ControllerBase
    {
        private readonly INekropsiRaporuAppService _nekropsiRaporuAppService;

        public NekropsiRaporuController(INekropsiRaporuAppService nekropsiRaporuAppService)
        {
            _nekropsiRaporuAppService = nekropsiRaporuAppService;
        }

        [HttpGet("GetKumesNekropsiRaporlari")]
        public async Task<IActionResult> GetKumesNekropsiRaporlari([FromQuery] int kumesId)
        {
            if (kumesId <= 0)
                return BadRequest("Kümes bilgisi zorunludur.");

            var result = await _nekropsiRaporuAppService.GetKumesNekropsiRaporlari(kumesId);
            return Ok(result);
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _nekropsiRaporuAppService.GetNekropsiRaporuById(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost("Kaydet")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Kaydet([FromForm] NekropsiRaporuKaydetRequest request)
        {
            if (request.KumesId <= 0)
                return BadRequest("Kümes bilgisi zorunludur.");

            if (string.IsNullOrWhiteSpace(request.NekropsiSebebi))
                return BadRequest("Nekropsi sebebi zorunludur.");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            try
            {
                var id = await _nekropsiRaporuAppService.KaydetAsync(request, userId);
                return Ok(new { id });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("Guncelle/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Guncelle(int id, [FromForm] NekropsiRaporuKaydetRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NekropsiSebebi))
                return BadRequest("Nekropsi sebebi zorunludur.");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            try
            {
                await _nekropsiRaporuAppService.GuncelleAsync(id, request, userId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Fotograf/{fotografId}")]
        public async Task<IActionResult> Fotograf(int fotografId)
        {
            var fotograf = await _nekropsiRaporuAppService.GetFotografAsync(fotografId);

            if (fotograf == null)
                return NotFound();

            return File(fotograf.Veri, fotograf.ContentType, fotograf.DosyaAdi);
        }

        [HttpDelete("Fotograf/{fotografId}")]
        public async Task<IActionResult> FotografSil(int fotografId)
        {
            await _nekropsiRaporuAppService.SilFotografAsync(fotografId);
            return Ok();
        }
    }
}
