using Microsoft.AspNetCore.Mvc;
using PanelIdentification.Application.Interfaces;
using PanelIdentification.Application.ViewModels;
using System.Security.Claims;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace PanelIdentification.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CihazController : BaseApiController<CihazViewModel>
    {
        private readonly ICihazAppService _cihazAppService;
        private readonly IUserExtension _userExtension;

        public CihazController(ICihazAppService cihazAppService, IUserExtension userExtension) : base(cihazAppService)
        {
            _cihazAppService = cihazAppService;
            _userExtension = userExtension;
        }

        [HttpPut("EditCihaz")]
        public async Task<IActionResult> EditCihaz([FromBody] CihazViewModel cihazViewModel)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _cihazAppService.EditCihaz(cihazViewModel, userId);

            return Ok();
        }       

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _cihazAppService.SetSilindi(id, userId);

            return Ok();
        }
    }
}
