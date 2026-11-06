using System.Security.Claims;
using WeighbridgeTracking.Application.Interfaces;
using WeighbridgeTracking.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace WeighbridgeTracking.API.Controllers
{
    // Hatalar KantarTakipExceptionFilter tarafından
    // 400/404/409 yanıtlarına çevrilir.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class KantarTakipController : ControllerBase
    {
        private readonly IKantarTakipAppService _kantarTakipAppService;
        private readonly IUserExtension _userExtension;

        public KantarTakipController(IKantarTakipAppService kantarTakipAppService, IUserExtension userExtension)
        {
            _kantarTakipAppService = kantarTakipAppService;
            _userExtension = userExtension;
        }

        private string GetUserIdFromContext()
        {
            var claim = _userExtension.GetClaimsIdentity()?.FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier || s.Type == "sub" || s.Type == "id" || s.Type == "userId" || s.Type == "nameid")?.Value;
            return claim ?? string.Empty;
        }

        private IActionResult KullaniciBulunamadi()
        {
            return Unauthorized(new
            {
                message = "Kullanıcı bilgisi bulunamadı."
            });
        }

        #region Kalite Tamamlama

        // Eski KantarKaliteSurec/Tamamla adresi geriye dönük uyumluluk için korunuyor.
        [HttpPost("KaliteTamamla/{kantarGuvenlikId:int}")]
        [HttpPost("~/api/KantarKaliteSurec/Tamamla/{kantarGuvenlikId:int}")]
        public async Task<IActionResult> KaliteTamamla(
            int kantarGuvenlikId,
            CancellationToken cancellationToken)
        {
            if (kantarGuvenlikId <= 0)
            {
                return BadRequest(new
                {
                    message = "Geçerli bir güvenlik ID giriniz."
                });
            }

            var userId = GetUserIdFromContext();

            if (string.IsNullOrWhiteSpace(userId))
                return KullaniciBulunamadi();

            await _kantarTakipAppService.KaliteTamamlaAsync(
                kantarGuvenlikId,
                userId,
                cancellationToken);

            return Ok(new
            {
                kantarGuvenlikId,
                kaliteTamamlandi = true,
                message = "Kalite kontrolü tamamlandı. Veri girişi kapatıldı."
            });
        }

        #endregion Kalite Tamamlama

        #region Bosaltim

        [HttpPut("BosaltimKaydet")]
        public async Task<IActionResult> BosaltimKaydet([FromBody] BosaltimViewModel bosaltimViewModel)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrWhiteSpace(userId)) return KullaniciBulunamadi();

            await _kantarTakipAppService.BosaltimKaydet(bosaltimViewModel, userId);
            return Ok();
        }

        #endregion Bosaltim

        #region Firma

        [HttpGet("GetAllFirma")]
        public async Task<IActionResult> GetAllFirma()
        {
            var result = await _kantarTakipAppService.GetAllFirma();
            return Ok(result);
        }

        [HttpGet("GetFirmaById/{id}")]
        public async Task<IActionResult> GetFirmaById(int id)
        {
            var result = await _kantarTakipAppService.GetFirmaById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("AddFirma")]
        public async Task<IActionResult> AddFirma([FromBody] FirmaViewModel firmaViewModel)
        {
            await _kantarTakipAppService.AddFirma(firmaViewModel);
            return Ok();
        }

        [HttpPut("EditFirma")]
        public async Task<IActionResult> EditFirma([FromBody] FirmaViewModel firmaViewModel)
        {
            await _kantarTakipAppService.EditFirma(firmaViewModel);
            return Ok();
        }

        [HttpDelete("DeleteFirma/{id}")]
        public async Task<IActionResult> DeleteFirma(int id)
        {
            await _kantarTakipAppService.DeleteFirma(id);
            return Ok();
        }

        #endregion Firma

        #region KaliteKontrolPersonel

        [HttpGet("GetAllKaliteKontrolPersonel")]
        public async Task<IActionResult> GetAllKaliteKontrolPersonel()
        {
            var result = await _kantarTakipAppService.GetAllKaliteKontrolPersonel();
            return Ok(result);
        }

        [HttpGet("GetKaliteKontrolPersonelById/{id}")]
        public async Task<IActionResult> GetKaliteKontrolPersonelById(int id)
        {
            var result = await _kantarTakipAppService.GetKaliteKontrolPersonelById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("AddKaliteKontrolPersonel")]
        public async Task<IActionResult> AddKaliteKontrolPersonel([FromBody] KaliteKontrolPersonelViewModel personelViewModel)
        {
            await _kantarTakipAppService.AddKaliteKontrolPersonel(personelViewModel);
            return Ok();
        }

        [HttpPut("EditKaliteKontrolPersonel")]
        public async Task<IActionResult> EditKaliteKontrolPersonel([FromBody] KaliteKontrolPersonelViewModel personelViewModel)
        {
            await _kantarTakipAppService.EditKaliteKontrolPersonel(personelViewModel);
            return Ok();
        }

        [HttpDelete("DeleteKaliteKontrolPersonel/{id}")]
        public async Task<IActionResult> DeleteKaliteKontrolPersonel(int id)
        {
            await _kantarTakipAppService.DeleteKaliteKontrolPersonel(id);
            return Ok();
        }

        #endregion KaliteKontrolPersonel

        #region KantarGuvenlik

        // Tarih filtresi opsiyonel; verilmezse tüm kayıtlar döner.
        [HttpGet("GetAllKantarGuvenlik")]
        public async Task<IActionResult> GetAllKantarGuvenlik(
            [FromQuery] DateOnly? startDate = null,
            [FromQuery] DateOnly? endDate = null)
        {
            var result = await _kantarTakipAppService.GetAllKantarGuvenlik(startDate, endDate);
            return Ok(result);
        }

        [HttpGet("GetKantarGuvenlikById/{id}")]
        public async Task<IActionResult> GetKantarGuvenlikById(int id)
        {
            var result = await _kantarTakipAppService.GetKantarGuvenlikById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("GetKantarGuvenlikByPlaka/{plaka}")]
        public async Task<IActionResult> GetKantarGuvenlikByPlaka(string plaka)
        {
            var result = await _kantarTakipAppService.GetKantarGuvenlikByPlaka(plaka);
            return Ok(result);
        }

        // Ürün takibi: ziyaretçi kodu ile genel kontrol + kalite kayıtları.
        [HttpGet("GetByZiyaretciKayitId/{ziyaretciKayitId}")]
        public async Task<IActionResult> GetByZiyaretciKayitId(string ziyaretciKayitId)
        {
            var result = await _kantarTakipAppService.GetByZiyaretciKayitId(ziyaretciKayitId);
            if (result == null) return NotFound(new { message = "Bu ziyaretçi kayıt ID'si ile kayıt bulunamadı." });
            return Ok(result);
        }

        [HttpPost("AddKantarGuvenlik")]
        public async Task<IActionResult> AddKantarGuvenlik([FromBody] KantarGuvenlikViewModel kantarGuvenlikViewModel)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrWhiteSpace(userId)) return KullaniciBulunamadi();

            await _kantarTakipAppService.AddKantarGuvenlik(kantarGuvenlikViewModel, userId);
            return Ok();
        }

        [HttpPut("EditKantarGuvenlik")]
        public async Task<IActionResult> EditKantarGuvenlik([FromBody] KantarGuvenlikViewModel kantarGuvenlikViewModel)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrWhiteSpace(userId)) return KullaniciBulunamadi();

            await _kantarTakipAppService.EditKantarGuvenlik(kantarGuvenlikViewModel, userId);
            return Ok();
        }

        [HttpDelete("DeleteKantarGuvenlik/{id}")]
        public async Task<IActionResult> DeleteKantarGuvenlik(int id)
        {
            await _kantarTakipAppService.DeleteKantarGuvenlik(id);
            return Ok();
        }

        #endregion KantarGuvenlik

        #region KantarKalite

        // Tarih filtresi opsiyonel; verilmezse tüm kayıtlar döner.
        [HttpGet("GetAllKantarKalite")]
        public async Task<IActionResult> GetAllKantarKalite(
            [FromQuery] DateOnly? startDate = null,
            [FromQuery] DateOnly? endDate = null)
        {
            var result = await _kantarTakipAppService.GetAllKantarKalite(startDate, endDate);
            return Ok(result);
        }

        [HttpGet("GetKantarKaliteById/{id}")]
        public async Task<IActionResult> GetKantarKaliteById(int id)
        {
            var result = await _kantarTakipAppService.GetKantarKaliteById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("GetKantarKaliteByKantarGuvenlikId/{kantarGuvenlikId}")]
        public async Task<IActionResult> GetKantarKaliteByKantarGuvenlikId(int kantarGuvenlikId)
        {
            var result = await _kantarTakipAppService.GetKantarKaliteByKantarGuvenlikId(kantarGuvenlikId);
            return Ok(result);
        }

        [HttpPost("AddKantarKalite")]
        public async Task<IActionResult> AddKantarKalite(
            [FromBody] KantarKaliteViewModel kantarKaliteViewModel)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrWhiteSpace(userId)) return KullaniciBulunamadi();

            await _kantarTakipAppService.AddKantarKalite(
                kantarKaliteViewModel, userId);

            return Ok();
        }

        [HttpPut("EditKantarKalite")]
        public async Task<IActionResult> EditKantarKalite(
            [FromBody] KantarKaliteViewModel kantarKaliteViewModel)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrWhiteSpace(userId)) return KullaniciBulunamadi();

            await _kantarTakipAppService.EditKantarKalite(
                kantarKaliteViewModel, userId);

            return Ok();
        }

        [HttpDelete("DeleteKantarKalite/{id}")]
        public async Task<IActionResult> DeleteKantarKalite(int id)
        {
            await _kantarTakipAppService.DeleteKantarKalite(id);
            return Ok();
        }

        #endregion KantarKalite
    }
}
