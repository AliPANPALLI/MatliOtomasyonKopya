using Microsoft.AspNetCore.Mvc;
using PanelIdentification.Application.Interfaces;
using PanelIdentification.Application.ViewModels;
using SharedKernel.Extensions;
using System.Security.Claims;

namespace PanelIdentification.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PanoController : BaseApiController<PanoViewModel>
    {
        private readonly IPanoAppService _panoAppService;
        private readonly IUserExtension _userExtension;
        public PanoController(IPanoAppService panoAppService, IUserExtension userExtension) : base(panoAppService)
        {
            _panoAppService = panoAppService;
            _userExtension = userExtension;
        }

        [HttpPut("EditPano")]
        public async Task<IActionResult> EditCihaz([FromBody] PanoViewModel panoViewModel)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _panoAppService.EditPano(panoViewModel, userId);

            return Ok();
        }

        [HttpGet("GetPanoByPanoKimlikNo")]
        public async Task<IActionResult> GetPanoByPanoKimlikNo(string panoKimlikNo)
        {
            var result = await _panoAppService.GetPanoByPanoKimlikNo(panoKimlikNo);

            return new JsonResult(result);
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _panoAppService.SetSilindi(id, userId);

            return Ok();
        }
    }
}