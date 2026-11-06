using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserEnerjiIzlemeYetkiController : BaseApiController<UserEnerjiIzlemeYetkiViewModel>
    {
        private readonly IUserEnerjiIzlemeYetkiAppService _userEnerjiIzlemeYetkiAppService;

        public UserEnerjiIzlemeYetkiController(IUserEnerjiIzlemeYetkiAppService userEnerjiIzlemeYetkiAppService) : base(userEnerjiIzlemeYetkiAppService)
        {
            _userEnerjiIzlemeYetkiAppService = userEnerjiIzlemeYetkiAppService;
        }

        [HttpGet("GetAllUserEnerjiIzlemeYetkis")]
        public async Task<IActionResult> GetAllUserEnerjiIzlemeYetkis()
        {
            var result = await _userEnerjiIzlemeYetkiAppService.GetAllUserEnerjiIzlemeYetkis();

            return Ok(result);
        }

        [HttpGet("GetAllUserEnerjiIzlemeYetkiByUserId")]
        public async Task<IActionResult> GetAllUserEnerjiIzlemeYetkiByUserId(string userId)
        {
            var result = await _userEnerjiIzlemeYetkiAppService.GetAllUserEnerjiIzlemeYetkiByUserId(userId);

            return Ok(result);
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _userEnerjiIzlemeYetkiAppService.SetSilindi(id);

            return Ok();
        }

        [HttpPost("AddUserEnerjiIzlemeYetki")]
        public async Task<IActionResult> AddUserEnerjiIzlemeYetki([FromForm] List<string> secilenKullanicilar, [FromForm] List<int> secilenLokasyonlar, [FromForm] List<int> secilenSirketler, [FromForm] bool okumaYetkisi, [FromForm] bool yazmaYetkisi, [FromForm] bool sorumluMu, [FromForm] bool silindi, [FromForm] bool aktif)
        {
            await _userEnerjiIzlemeYetkiAppService.AddUserEnerjiIzlemeYetki(secilenKullanicilar, secilenLokasyonlar, secilenSirketler, okumaYetkisi, yazmaYetkisi, sorumluMu, silindi, aktif);

            return Ok();
        }
    }
}
