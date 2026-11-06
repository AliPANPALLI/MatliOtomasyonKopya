using KumesScada.Application.Interfaces;
using KumesScada.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Extensions;

namespace KumesScada.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KumesHaftalikCatiKontrolController : BaseApiController<KumesHaftalikCatiKontrolViewModel>
    {
        private readonly IKumesHaftalikCatiKontrolAppService _kumesHaftalikCatiKontrolAppService;
        private readonly IUserExtension _userExtension;

        public KumesHaftalikCatiKontrolController(IKumesHaftalikCatiKontrolAppService kumesHaftalikCatiKontrolAppService, IUserExtension userExtension) : base(kumesHaftalikCatiKontrolAppService)
        {
            _kumesHaftalikCatiKontrolAppService = kumesHaftalikCatiKontrolAppService;
            _userExtension = userExtension;
        }
        //[HttpGet("GetCatiKontrol")]
        //public async Task<IActionResult> GetCatiKontrol([FromQuery] int kumesId, [FromQuery] int? donemId, [FromQuery] int haftaSayisi)
        //{
        //    var result = await _kumesHaftalikCatiKontrolAppService.GetKumesHaftalikCatiKontrol(kumesId, donemId, haftaSayisi);

        //    return Ok(result);
        //}
        [HttpGet("GetKumesCatiKontrolHaftalari")]
        public async Task<IActionResult> GetKumesCatiKontrolHaftalari([FromQuery] int kumesId)
        {
            var result = await _kumesHaftalikCatiKontrolAppService.GetKumesCatiKontrolHaftalari(kumesId);
            return Ok(result);
        }

        [HttpPost("SaveCatiKontrol")]
        public async Task<IActionResult> SaveCatiKontrol([FromBody] KumesHaftalikCatiKontrolViewModel model)
        {
            if (model == null)
                return BadRequest("Model boş olamaz.");

            if (model.KumesId <= 0)
                return BadRequest("Kümes bilgisi zorunludur.");

            if (model.HaftaSayisi <= 0)
                return BadRequest("Hafta bilgisi zorunludur.");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            await _kumesHaftalikCatiKontrolAppService.SaveKumesHaftalikCatiKontrol(model, userId);
            return Ok();
        }
    }
}