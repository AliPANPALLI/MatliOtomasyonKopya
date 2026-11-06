using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;

namespace SharedApp.API.Controllers
{
    // UserLinkYetkiController'ın GENEL (herhangi bir modül/form için
    // kullanılabilen) karşılığı — bkz. UserYetki/FormLinkEsleme modelleri
    // üzerindeki notlar. Temel CRUD (GetAllList/GetById/Create/Update/
    // Delete/GetAllDx) BaseApiController'dan geliyor.
    [Route("api/[controller]")]
    [ApiController]
    public class UserYetkiController : BaseApiController<UserYetkiViewModel>
    {
        private readonly IUserYetkiAppService _userYetkiAppService;

        public UserYetkiController(IUserYetkiAppService userYetkiAppService) : base(userYetkiAppService)
        {
            _userYetkiAppService = userYetkiAppService;
        }

        [HttpPost("AddUserYetki")]
        public async Task<IActionResult> AddUserYetki([FromForm] List<string> secilenKullanicilar, [FromForm] List<string> secilenLinks, [FromForm] int formId, [FromForm] bool okumaYetkisi, [FromForm] bool yazmaYetkisi, [FromForm] bool degistirmeYetkisi, [FromForm] bool silmeYetkisi, [FromForm] bool onaylayabilirMi, [FromForm] bool sorumluMu, [FromForm] bool silindi, [FromForm] bool aktif)
        {
            await _userYetkiAppService.AddUserYetki(secilenKullanicilar, secilenLinks, formId, okumaYetkisi, yazmaYetkisi, degistirmeYetkisi, silmeYetkisi, onaylayabilirMi, sorumluMu, silindi, aktif);
            return Ok();
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _userYetkiAppService.SetSilindi(id);
            return Ok();
        }

        [HttpGet("GetAllByUserId")]
        public async Task<IActionResult> GetAllByUserId(string userId)
        {
            var result = await _userYetkiAppService.GetAllByUserId(userId);
            return Ok(result);
        }

        [HttpGet("GetKantarYetkiByUserId")]
        [Authorize]
        public async Task<IActionResult> GetKantarYetkiByUserId(string userId)
        {
            var result = await _userYetkiAppService.GetKantarYetkiByUserId(userId);
            return Ok(result);
        }

        [HttpGet("GetAllByFormId")]
        public async Task<IActionResult> GetAllByFormId(int formId)
        {
            var result = await _userYetkiAppService.GetAllByFormId(formId);
            return Ok(result);
        }

        [HttpGet("GetAllByModulId")]
        public async Task<IActionResult> GetAllByModulId(int modulId)
        {
            var result = await _userYetkiAppService.GetAllByModulId(modulId);
            return Ok(result);
        }

        [HttpGet("GetAllEnriched")]
        public async Task<IActionResult> GetAllEnriched()
        {
            var result = await _userYetkiAppService.GetAllEnriched();
            return Ok(result);
        }

        // Kaynak kullanıcının seçilen moduldeki tüm UserYetki satırlarını
        // (Form+Link+Okuma/Yazma/Degistirme/Silme/Onaylayabilir/SorumluMu)
        // bir veya birden fazla hedef kullanıcıya kopyalar.
        [HttpPost("TransferUserYetkiByModul")]
        public async Task<IActionResult> TransferUserYetkiByModul([FromForm] string kaynakUserId, [FromForm] List<string> hedefUserIdler, [FromForm] int modulId)
        {
            var kopyalanan = await _userYetkiAppService.TransferUserYetkiByModul(kaynakUserId, hedefUserIdler, modulId);
            return Ok(kopyalanan);
        }
    }
}
