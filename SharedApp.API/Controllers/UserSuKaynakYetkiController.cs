using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services;
using SharedApp.Application.ViewModels;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserSuKaynakYetkiController : BaseApiController<UserSuKaynakYetkiViewModel>
    {
        private readonly IUserSuKaynakYetkiAppService _userSuKaynakYetkiAppService;

        public UserSuKaynakYetkiController(IUserSuKaynakYetkiAppService userSuKaynakYetkiAppService) : base(userSuKaynakYetkiAppService)
        {
            _userSuKaynakYetkiAppService = userSuKaynakYetkiAppService;
        }

        [HttpGet("GetAllUserSuKaynakYetkis")]
        public async Task<IActionResult> GetAllUserSuKaynakYetkis()
        {
            var result = await _userSuKaynakYetkiAppService.GetAllUserSuKaynakYetkis();

            return Ok(result);
        }

        [HttpGet("GetAllUserSuKaynakYetkiByUserId")]
        public async Task<IActionResult> GetAllUserSuKaynakeYetkiByUserId(string userId)
        {
            var result = await _userSuKaynakYetkiAppService.GetAllUserSuKaynakYetkiByUserId(userId);

            return Ok(result);
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _userSuKaynakYetkiAppService.SetSilindi(id);

            return Ok();
        }

        [HttpPost("AddUserSuKaynakYetki2")]
        public async Task<IActionResult> AddUserSuKaynakYetki2([FromForm] List<string> secilenKullanicilar, [FromForm] List<int> secilenSuKaynaklar, [FromForm] List<int> secilenLokasyonlar, [FromForm] List<int> secilenSirketler,
          [FromForm] bool okumaYetkisi, [FromForm] bool yazmaYetkisi, [FromForm] bool silindi, [FromForm] bool aktif)
        {
            await _userSuKaynakYetkiAppService.AddUserSuKaynakYetki2(secilenKullanicilar, secilenSuKaynaklar, secilenLokasyonlar, secilenSirketler, okumaYetkisi, yazmaYetkisi, silindi, aktif);

            return Ok();
        }

        [HttpPost("AddUserSuKaynakYetki")]
        public async Task<IActionResult> AddUserSuKaynakYetki([FromForm] List<string> secilenKullanicilar, [FromForm] List<int> secilenCizimler, [FromForm] List<int> secilenLokasyonlar, [FromForm] List<int> secilenSirketler,
          [FromForm] bool okumaYetkisi, [FromForm] bool yazmaYetkisi, [FromForm] bool silindi, [FromForm] bool aktif)
        {
            await _userSuKaynakYetkiAppService.AddUserSuKaynakYetki(secilenKullanicilar, secilenCizimler, secilenLokasyonlar, secilenSirketler, okumaYetkisi, yazmaYetkisi, silindi, aktif);

            return Ok();
        }

        //[HttpPost("AddUserSuKaynakYetki2")]
        //public async Task<IActionResult> AddUserSuKaynakYetki2([FromForm] List<string> secilenKullanicilar, [FromForm] List<int> secilenCizim,
        //[FromForm] List<int> secilenLokasyonlar, [FromForm] List<int> secilenSirketler,
        //[FromForm] bool okumaYetkisi, [FromForm] bool yazmaYetkisi, [FromForm] bool silindi, [FromForm] bool aktif)
        //{
        //    await _userSuKaynakYetkiAppService.AddUserSuKaynakYetki(secilenKullanicilar, secilenSuKaynaklar, secilenLokasyonlar, secilenSirketler, okumaYetkisi, yazmaYetkisi, silindi, aktif);

        //    return Ok();
        //}
    }
}