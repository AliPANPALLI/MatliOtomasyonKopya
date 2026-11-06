using Microsoft.AspNetCore.Mvc;
using PanoKimliklendirme.Application.Interfaces;
using PanoKimliklendirme.Application.ViewModels;
using SharedKernel.Extensions;
using System.Security.Claims;

namespace PanoKimliklendirme.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OnayIslemKayitController : BaseApiController<OnayIslemKayitViewModel>
    {
        private readonly IOnayIslemKayitAppService _onayIslemKayitAppService;
        private readonly IUserExtension _userExtension;
        public OnayIslemKayitController(IOnayIslemKayitAppService onayIslemKayitAppService, IUserExtension userExtension) : base(onayIslemKayitAppService)
        {
            _onayIslemKayitAppService = onayIslemKayitAppService;
            _userExtension = userExtension;
        }

        [HttpGet("GetAllOnayIslemKayit")]
        public async Task<IActionResult> GetAllOnayIslemKayit()
        {
            var result = await _onayIslemKayitAppService.GetAllOnayIslemKayit();
            return new JsonResult(result);
        }

        [HttpGet("GetAllvwDistinctOnayIslemKayit")]
        public async Task<IActionResult> GetAllvwDistinctOnayIslemKayit()
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _onayIslemKayitAppService.GetAllvwDistinctOnayIslemKayit(userId);
            return new JsonResult(result);
        }

        [HttpPut("IslemOnayla")]
        public async Task<IActionResult> IslemOnayla([FromBody] int onayIslemKayitId)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _onayIslemKayitAppService.IslemOnayla(onayIslemKayitId, userId);
            return Ok(result);
        }

        [HttpPut("IslemReddet")]
        public async Task<IActionResult> IslemReddet([FromBody] int onayIslemKayitId)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _onayIslemKayitAppService.IslemReddet(onayIslemKayitId, userId);
            return Ok(result);
        }

        [HttpGet("GetOnayIslemKayitFromTreeNodeId")]
        public async Task<IActionResult> GetOnayIslemKayitFromTreeNodeId(int id, string nodeType)
        {
            var result = await _onayIslemKayitAppService.GetOnayIslemKayitFromTreeNodeId(id, nodeType);
            return Ok(result);
        }

        [HttpGet("GetOnayIslemKayitlarByIdForDosya")]
        public async Task<IActionResult> GetOnayIslemKayitlarByIdForDosya(int onayIslemKayitId)
        {
            var result = await _onayIslemKayitAppService.GetOnayIslemKayitlarByIdForDosya(onayIslemKayitId);
            return Ok(result);
        }

        [HttpPut("DosyaIslemOnayla")]
        public async Task<IActionResult> DosyaIslemOnayla(int onayIslemKayitId)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _onayIslemKayitAppService.DosyaIslemOnayla(onayIslemKayitId, userId);
            return Ok();
        }

        [HttpPut("DosyaIslemReddet")]
        public async Task<IActionResult> DosyaIslemReddet(int onayIslemKayitId)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _onayIslemKayitAppService.DosyaIslemReddet(onayIslemKayitId, userId);
            return Ok();
        }

        [HttpPut("OnayaGonderByNodeIdAndType")]
        public async Task<IActionResult> OnayaGonderByNodeIdAndType(int nodeId, string nodeType, string nodeName)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _onayIslemKayitAppService.OnayaGonderByNodeIdAndType(nodeId, nodeType, nodeName, userId);
            return Ok(result);
        }

        [HttpPut("CihazIslemOnayla")]
        public async Task<IActionResult> CihazIslemOnayla(int cihazId)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _onayIslemKayitAppService.CihazIslemOnayla(cihazId, userId);
            return Ok();
        }

        [HttpPut("CihazIslemReddet")]
        public async Task<IActionResult> CihazIslemReddet(int cihazId)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _onayIslemKayitAppService.CihazIslemReddet(cihazId, userId);
            return Ok();
        }
    }
}