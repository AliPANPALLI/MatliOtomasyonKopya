using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services;
using SharedApp.Application.ViewModels;
using SharedKernel.Extensions;
using System.Security.Claims;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserLinkYetkiController : BaseApiController<UserLinkYetkiViewModel>
    {
        private readonly IUserLinkYetkiAppService _userLinkYetkiAppService;
        private readonly IUserExtension _userExtension;

        public UserLinkYetkiController(IUserLinkYetkiAppService userLinkYetkiAppService, IUserExtension userExtension) : base(userLinkYetkiAppService)
        {
            _userLinkYetkiAppService = userLinkYetkiAppService;
            _userExtension = userExtension;
        }

        [HttpGet("GetAllUserLinkYetkiForTesisScada")]
        public async Task<IActionResult> GetAllUserLinkYetkiForTesisScada()
        {
            var result = await _userLinkYetkiAppService.GetAllUserLinkYetkiForTesisScada();
            return Ok(result);
        }

        [HttpGet("GetAllUserLinkYetkiForSunucuScada")]
        public async Task<IActionResult> GetAllUserLinkYetkiForSunucuScada()
        {
            var result = await _userLinkYetkiAppService.GetAllUserLinkYetkiForSunucuScada();
            return Ok(result);
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _userLinkYetkiAppService.SetSilindi(id);
            return Ok();
        }

        [HttpPost("AddUserLinkYetki")]
        public async Task<IActionResult> AddUserLinkYetki([FromForm] List<string> secilenKullanicilar, [FromForm] List<string> secilenLinks, [FromForm] int formId, [FromForm] bool okumaYetkisi, [FromForm] bool yazmaYetkisi, [FromForm] bool sorumluMu, [FromForm] bool silindi, [FromForm] bool aktif)
        {
            await _userLinkYetkiAppService.AddUserLinkYetki(secilenKullanicilar, secilenLinks, formId, okumaYetkisi, yazmaYetkisi, sorumluMu, silindi, aktif);

            return Ok();
        }

        [HttpGet("GetAllFormForTesisScada")]
        public async Task<IActionResult> GetAllFormForTesisScada()
        {
            var result = await _userLinkYetkiAppService.GetAllFormForTesisScada();
            return Ok(result);
        }

        [HttpGet("GetAllFormForSunucuScada")]
        public async Task<IActionResult> GetAllFormForSunucuScada()
        {
            var result = await _userLinkYetkiAppService.GetAllFormForSunucuScada();
            return Ok(result);
        }

        [HttpPut("EditUserLinkYetki")]
        public async Task<IActionResult> EditUserLinkYetki(UserLinkYetkiViewModel userLinkYetkiViewModel)
        {
            await _userLinkYetkiAppService.EditUserLinkYetki(userLinkYetkiViewModel);
            return Ok();
        }
    }
}