using Microsoft.AspNetCore.Mvc;
using PanelIdentification.Application.Interfaces;
using PanelIdentification.Application.ViewModels;
using SharedKernel.Extensions;
using System.Security.Claims;

namespace PanelIdentification.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MakineController : BaseApiController<MakineViewModel>
    {
        private readonly IMakineAppService _makineAppService;
        private readonly IUserExtension _userExtension;

        public MakineController(IMakineAppService makineAppService, IUserExtension userExtension) : base(makineAppService)
        {
            _makineAppService = makineAppService;
            _userExtension = userExtension;
        }

        [HttpPut("EditMakine")]
        public async Task<IActionResult> EditMakine([FromBody] MakineViewModel makineViewModel)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _makineAppService.EditMakine(makineViewModel, userId);

            return Ok();
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _makineAppService.SetSilindi(id, userId);

            return Ok();
        }

        [HttpGet("GetLokasyonIdsByMakineIds")]
        public async Task<IActionResult> GetLokasyonIdsByMakineIds([FromQuery] List<int> makineIds)
        {
            var result = await _makineAppService.GetLokasyonIdsByMakineIds(makineIds);

            return Ok(result);
        }

        [HttpGet("GetAllMakineForYetki")]
        public async Task<IActionResult> GetAllMakineForYetki()
        {
            var result = await _makineAppService.GetAllMakineForYetki();

            return Ok(result);
        }
    }
}
