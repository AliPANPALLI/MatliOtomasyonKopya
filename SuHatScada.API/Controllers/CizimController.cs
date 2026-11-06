using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuHatScada.Application.Interfaces;
using SuHatScada.Application.ViewModels;
using SuHatScada.Domain.Models;

namespace SuHatScada.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CizimController : BaseApiController<CizimViewModel>
    {
        private readonly ICizimAppService _cizimAppService;

        public CizimController(ICizimAppService cizimAppService) : base(cizimAppService)
        {
            _cizimAppService = cizimAppService;
        }

        [HttpGet("GetCizimUrlById")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCizimUrlById(int cizimId)
        {
            var result = await _cizimAppService.GetCizimUrlById(cizimId);

            return new JsonResult(result);
        }

        [HttpGet("GetCizimById")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCizimById(int cizimId)
        {
            var result = await _cizimAppService.GetCizimById(cizimId);
            return new JsonResult(result);
        }

        [HttpGet("GetCizimJsonByNameWithValue")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCizimJsonByNameWithValue(string cizimName)
        {
            var result = await _cizimAppService.GetCizimJsonByNameWithValue(cizimName);

            return new JsonResult(result);
        }

        [HttpGet("GetCizimJsonById")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCizimJsonById(int cizimId)
        {
            var result = await _cizimAppService.GetCizimJsonById(cizimId);

            return new JsonResult(result);
        }

        [HttpGet("GetCizimJsonByLokasyonId")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCizimJsonByLokasyonId(int lokasyonId)
        {
            var result = await _cizimAppService.GetCizimJsonByLokasyonId(lokasyonId);
            // Sunucudan alınan veriyi SignalR ile frontend'e gönder
            //await _hubContext.Clients.All.SendAsync("receiveCizimData", result);

            return new JsonResult(result);
        }

        [HttpGet("GetCizimJsonByCizimId")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCizimJsonByCizimId(int lokasyonId)
        {
            var result = await _cizimAppService.GetCizimJsonByLokasyonId(lokasyonId);
            // Sunucudan alınan veriyi SignalR ile frontend'e gönder
            //await _hubContext.Clients.All.SendAsync("receiveCizimData", result);

            return new JsonResult(result);
        }

        [HttpGet("GetCizimJsonByLokasyonIdAndUserId")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCizimJsonByLokasyonIdAndUserId(int lokasyonId, string userId)
        {
            var result = await _cizimAppService.GetCizimJsonByLokasyonId(lokasyonId);
            // Sunucudan alınan veriyi SignalR ile frontend'e gönder
            //await _hubContext.Clients.All.SendAsync("receiveCizimData", result);

            return new JsonResult(result);
        }

        [HttpPost("EditSekil")]
        public async Task<IActionResult> EditSekil([FromBody] SekilJson sekil2)
        {
            await _cizimAppService.EditSekil(sekil2);

            return Ok();
        }

        [HttpGet("GetKullaniciSekilYetki")]
        [AllowAnonymous]
        public async Task<IActionResult> GetKullaniciSekilYetki(int lokasyonId, string userId)
        {
            var result = await _cizimAppService.GetKullaniciSekilYetki(lokasyonId, userId);

            return new JsonResult(result);
        }

        [HttpGet("GetSekilEtiketKural")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSekilEtiketKural(int lokasyonId, string userId)
        {
            var result = await _cizimAppService.GetSekilEtiketKural(lokasyonId, userId);

            return new JsonResult(result);
        }

        [HttpGet("GetSekilEtiketKuralByCizimId")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSekilEtiketKuralByCizimId(int cizimId, string userId)
        {
            var result = await _cizimAppService.GetSekilEtiketKuralByCizimId(cizimId, userId);

            return new JsonResult(result);
        }

        [HttpGet("GetSekil")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSekil(int lokasyonId)
        {
            var result = await _cizimAppService.GetSekil(lokasyonId);

            return new JsonResult(result);
        }

        [HttpGet("GetSekilByCizimId")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSekilByCizimId(int cizimId)
        {
            var result = await _cizimAppService.GetSekilByCizimId(cizimId);

            return new JsonResult(result);
        }

        [HttpPut("UpdateDMModTuru")]
        public async Task<IActionResult> UpdateDMModTuru(int cizimId, string modTuru, string userId)
        {
            await _cizimAppService.UpdateDMModTuru(cizimId, modTuru, userId);

            return Ok();
        }

        [HttpPut("UpdateTankModTuru")]
        public async Task<IActionResult> UpdateTankModTuru(int cizimId, string modTuru, string userId)
        {
            await _cizimAppService.UpdateTankModTuru(cizimId, modTuru, userId);

            return Ok();
        }

        [HttpPut("UpdateSekilModTuru")]
        public async Task<IActionResult> UpdateSekilModTuru(string sekilId, string targetSekilId, string modTuru, string userId)
        {
            await _cizimAppService.UpdateSekilModTuru2(sekilId, targetSekilId, modTuru, userId);

            return Ok();
        }

        [HttpPut("UpdateSekilPozisyon")]
        public async Task<IActionResult> UpdateSekilPozisyon(string sekilId, int pozisyonX, int pozisyonY, string userId)
        {
            await _cizimAppService.UpdateSekilPozisyon(sekilId, pozisyonX, pozisyonY, userId);

            return Ok();
        }
        [HttpPut("UpdateSekilVisible")]
        public async Task<IActionResult> UpdateSekilVisible(string sekilId, bool gorunsunMu, string userId)
        {
            await _cizimAppService.UpdateSekilVisible(sekilId, gorunsunMu, userId);

            return Ok();
        }
    }
}