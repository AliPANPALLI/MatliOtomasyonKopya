using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserKumesYetkiController : BaseApiController<UserKumesYetkiViewModel>
    {
        private readonly IUserKumesYetkiAppService _userKumesYetkiAppService;

        public UserKumesYetkiController(IUserKumesYetkiAppService userKumesYetkiAppService) : base(userKumesYetkiAppService)
        {
            _userKumesYetkiAppService = userKumesYetkiAppService;
        }

        [HttpGet("GetAllUserKumesYetkis")]
        public async Task<IActionResult> GetAllUserKumesYetkis()
        {
            var result = await _userKumesYetkiAppService.GetAllUserKumesYetkis();

            return Ok(result);
        }

        [HttpGet("GetAllUserKumesYetkiByUserId")]
        public async Task<IActionResult> GetAllUserKumesYetkiByUserId(string userId)
        {
            var result = await _userKumesYetkiAppService.GetAllUserKumesYetkiByUserId(userId);

            return Ok(result);
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _userKumesYetkiAppService.SetSilindi(id);

            return Ok();
        }

        [HttpPost("AddUserKumesYetki")]
        public async Task<IActionResult> AddUserKumesYetki([FromForm] List<string> secilenKullanicilar, [FromForm] List<int> secilenKumesler, [FromForm] List<int> secilenLokasyonlar, [FromForm] List<int> secilenSirketler,
           [FromForm] bool okumaYetkisi, [FromForm] bool yazmaYetkisi, [FromForm] bool sorumluMu, [FromForm] bool silindi, [FromForm] bool aktif, [FromForm] bool yemRasyonYetki, [FromForm] bool raporRehberYetki)
        {
            await _userKumesYetkiAppService.AddUserKumesYetki(secilenKullanicilar, secilenKumesler, secilenLokasyonlar, secilenSirketler, okumaYetkisi, yazmaYetkisi, sorumluMu, silindi, aktif, yemRasyonYetki, raporRehberYetki);
            return Ok();
        }
    }
}
