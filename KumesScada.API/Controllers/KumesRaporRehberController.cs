using KumesScada.Application.Interfaces;
using KumesScada.Application.RequestModels;
using KumesScada.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Extensions;
using System.Security.Claims;

namespace KumesScada.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KumesRaporRehberController : BaseApiController<KumesRaporRehberViewModel>
    {
        private readonly IKumesRaporRehberAppService _kumesRaporRehberAppService;
        private readonly IUserExtension _userExtension;

        public KumesRaporRehberController(IKumesRaporRehberAppService kumesRaporRehberAppService, IUserExtension userExtension) : base(kumesRaporRehberAppService)
        {
            _kumesRaporRehberAppService = kumesRaporRehberAppService;
            _userExtension = userExtension;
        }

        [HttpGet("GetGuideTipleri")]
        public IActionResult GetGuideTipleri()
        {
            var result = _kumesRaporRehberAppService.GetGuideTipleri();
            return Ok(result);
        }

        [HttpGet("GetByLokasyonId")]
        public async Task<IActionResult> GetByLokasyonId(int lokasyonId)
        {
            var result = await _kumesRaporRehberAppService.GetByLokasyonId(lokasyonId);
            return Ok(result);
        }

        [HttpGet("GetByLokasyonIdAndGuideTipi")]
        public async Task<IActionResult> GetByLokasyonIdAndGuideTipi(int lokasyonId, string guideTipi, int hayvanCinsId)
        {
            var result = await _kumesRaporRehberAppService.GetByLokasyonIdAndGuideTipi(lokasyonId, guideTipi, hayvanCinsId);
            return Ok(result);
        }

        [HttpPost("SaveGuides")]
        public async Task<IActionResult> SaveGuides([FromBody] KumesRapoRehberSaveRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                userId = "system";

            await _kumesRaporRehberAppService.SaveGuides(request, userId);

            return Ok(new
            {
                success = true,
                message = "Hedef tanımları kaydedildi."
            });
        }

        [HttpDelete("DeleteGuide")]
        public async Task<IActionResult> DeleteGuide(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                userId = "system";

            await _kumesRaporRehberAppService.DeleteGuide(id, userId);

            return Ok(new
            {
                success = true,
                message = "Hedef kaydı silindi."
            });
        }
    }
}