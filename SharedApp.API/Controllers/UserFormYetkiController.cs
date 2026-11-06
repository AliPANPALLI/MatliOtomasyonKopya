using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserFormYetkiController : BaseApiController<UserFormYetkiViewModel>
    {
        private readonly IUserFormYetkiAppService _userFormYetkiAppService;

        public UserFormYetkiController(IUserFormYetkiAppService userFormYetkiAppService) : base(userFormYetkiAppService)
        {
            _userFormYetkiAppService = userFormYetkiAppService;
        }

        [HttpGet("GetAllUserFormYetkis")]
        public async Task<IActionResult> GetAllUserFormYetkis()
        {
            var result = await _userFormYetkiAppService.GetAllUserFormYetkis();

            return Ok(result);
        }

        [HttpGet("GetAllUserFormYetkiByUserId")]
        public async Task<IActionResult> GetAllUserFormYetkiByUserId(string userId)
        {
            var result = await _userFormYetkiAppService.GetAllUserFormYetkiByUserId(userId);

            return Ok(result);
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _userFormYetkiAppService.SetSilindi(id);

            return Ok();
        }

        [HttpPost("AddUserFormYetki")]
        public async Task<IActionResult> AddUserFormYetki([FromForm] List<string> secilenKullanicilar, [FromForm] List<int> secilenFormlar, [FromForm] bool okumaYetkisi, [FromForm] bool yazmaYetkisi, [FromForm] bool degistirmeYetkisi, [FromForm] bool silmeYetkisi)
        {
            await _userFormYetkiAppService.AddUserFormYetki(secilenKullanicilar, secilenFormlar, okumaYetkisi, yazmaYetkisi, degistirmeYetkisi, silmeYetkisi);

            return Ok();
        }

        [HttpPut("EditUserFormYetki")]
        public async Task<IActionResult> EditUserFormYetki( UserFormYetkiViewModel userFormYetkiViewModel)
        {
            await _userFormYetkiAppService.EditUserFormYetki(userFormYetkiViewModel);

            return Ok();
        }
    }
}