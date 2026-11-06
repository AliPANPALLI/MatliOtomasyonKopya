using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.ViewModels;
using PoultryHouseScada.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PoultryHouseScada.API.Controllers
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

        [HttpGet("GetCizimJsonByKumesId")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCizimJsonByKumesId(int kumesId)
        {
            var result = await _cizimAppService.GetCizimJsonByKumesId(kumesId);

            return new JsonResult(result);
        }

        [HttpPost("EditSekil")]
        public async Task<IActionResult> EditSekil([FromBody] SekilJson sekil2)
        {
            await _cizimAppService.EditSekil(sekil2);

            return Ok();
        }

        [HttpGet("GetShapeEditorEtiketler")]
        public async Task<IActionResult> GetShapeEditorEtiketler()
        {
            return Ok(await _cizimAppService.GetShapeEditorEtiketler());
        }

        [HttpGet("GetShapeEditorKurallar")]
        public async Task<IActionResult> GetShapeEditorKurallar(string shapeJsonId)
        {
            return Ok(await _cizimAppService.GetShapeEditorKurallar(shapeJsonId));
        }

        [HttpPost("SaveShapeEditorRule")]
        public async Task<IActionResult> SaveShapeEditorRule([FromBody] SaveShapeEditorRuleViewModel model)
        {
            return Ok(await _cizimAppService.SaveShapeEditorRule(model));
        }

        [HttpDelete("DeleteShapeEditorRule/{id:int}")]
        public async Task<IActionResult> DeleteShapeEditorRule(int id)
        {
            await _cizimAppService.DeleteShapeEditorRule(id);
            return NoContent();
        }
    }
}
