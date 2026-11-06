using Microsoft.AspNetCore.Mvc;
using PanoKimliklendirme.Application.Interfaces;
using PanoKimliklendirme.Application.Services;
using PanoKimliklendirme.Application.ViewModels;
using SharedKernel.Extensions;
using System.Security.Claims;
using System.Text.Json;

namespace PanoKimliklendirme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BolumController : BaseApiController<BolumViewModel>
    {
        private readonly IBolumAppService _bolumAppService;
        private readonly IUserExtension _userExtension;

        public BolumController(IBolumAppService bolumAppService, IUserExtension userExtension) : base(bolumAppService)
        {
            _bolumAppService = bolumAppService;
            _userExtension = userExtension;
        }

        [HttpPut("EditBolum")]
        public async Task<IActionResult> EditBolum([FromBody] BolumViewModel bolumViewModel)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _bolumAppService.EditBolum(bolumViewModel, userId);

            return Ok();
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _bolumAppService.SetSilindi(id, userId);

            return Ok();
        }

        [HttpGet("GetLokasyonIdsByBolumIds")]
        public async Task<IActionResult> GetLokasyonIdsByBolumIds([FromQuery] List<int> bolumIds)
        {
            var result = await _bolumAppService.GetLokasyonIdsByBolumIds(bolumIds);

            return Ok(result);
        }

        [HttpGet("GetAllBolumForYetki")]
        public async Task<IActionResult> GetAllBolumForYetki()
        {
            var result = await _bolumAppService.GetAllBolumForYetki();

            return Ok(result);
        }
    }
}
