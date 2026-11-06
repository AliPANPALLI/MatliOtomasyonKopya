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
    public class UserMakineYetkiController : BaseApiController<UserMakineYetkiViewModel>
    {
        private readonly IUserMakineYetkiAppService _userMakineYetkiAppService;
        private readonly IUserExtension _userExtension;

        public UserMakineYetkiController(IUserMakineYetkiAppService userMakineYetkiAppService, IUserExtension userExtension) : base(userMakineYetkiAppService)
        {
            _userMakineYetkiAppService = userMakineYetkiAppService;
            _userExtension = userExtension;
        }

        [HttpGet("GetAllUserMakineYetkis")]
        public async Task<IActionResult> GetAllUserMakineYetkis()
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }

            var result = await _userMakineYetkiAppService.GetAllUserMakineYetkis(userId);

            return Ok(result);
        }

        [HttpGet("GetAllUserMakineYetkiByUserId")]
        public async Task<IActionResult> GetAllUserMakineYetkiByUserId(string userId)
        {
            var result = await _userMakineYetkiAppService.GetAllUserMakineYetkiByUserId(userId);

            return Ok(result);
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _userMakineYetkiAppService.SetSilindi(id);

            return Ok();
        }

        //[HttpPut("EditUserMakineYetki")]
        //public async Task<IActionResult> EditUserMakineYetki([FromBody] int id)
        //{
        //    await _userMakineYetkiAppService.SetSilindi(id);

        //    return Ok();
        //}

        [HttpPost("AddUserMakineYetki")]
        public async Task<IActionResult> AddUserMakineYetki([FromForm] List<string> secilenKullanicilar, [FromForm] List<int> secilenMakineler,
            [FromForm] List<int> secilenBolumler, [FromForm] List<int> secilenLokasyonlar, [FromForm] List<int> secilenSirketler,
            [FromForm] bool okumaYetkisi, [FromForm] bool yazmaYetkisi, [FromForm] bool sorumluMu, [FromForm] bool silindi, [FromForm] bool aktif)
        {
            await _userMakineYetkiAppService.AddUserMakineYetki(secilenKullanicilar, secilenMakineler, secilenBolumler, secilenLokasyonlar, secilenSirketler, okumaYetkisi, yazmaYetkisi, sorumluMu, silindi, aktif);

            return Ok();
        }

        [HttpPut("EditUserMakineYetki")]
        public async Task<IActionResult> EditUserMakineYetki(UserMakineYetkiViewModel userMakineYetkiViewModel)
        {
            await _userMakineYetkiAppService.EditUserMakineYetki(userMakineYetkiViewModel);

            return Ok();
        }
    }
}
