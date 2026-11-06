using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;
using SharedApp.Application.ViewModels.ReportViewModels;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SirketController : BaseApiController<SirketViewModel>
    {
        private readonly ISirketAppService _sirketAppService;

        public SirketController(ISirketAppService sirketAppService) : base(sirketAppService)
        {
            _sirketAppService = sirketAppService;
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _sirketAppService.SetSilindi(id);

            return Ok();
        }

        [HttpGet("GetAllSirketForYetki")]
        public async Task<IActionResult> GetAllSirketForYetki()
        {
            var result = await _sirketAppService.GetAllSirketForYetki();

            return Ok(result);
        }

        [HttpGet("GetTesisTreeView")]
        public async Task<IActionResult> GetTesisTreeView()
        {
            var result = await _sirketAppService.GetTesisTreeView();

            return Ok(result);
        }

        [HttpPost("AddLokasyon")]
        public async Task<IActionResult> AddLokasyon([FromBody] LokasyonViewModel lokasyonViewModel)
        {
            await _sirketAppService.AddLokasyon(lokasyonViewModel);
            return Ok();
        }

        [HttpPost("AddTesis")]
        public async Task<IActionResult> AddTesis([FromBody] TesisViewModel tesisViewModel)
        {
            await _sirketAppService.AddTesis(tesisViewModel);
            return Ok();
        }

        [HttpPost("AddBolum")]
        public async Task<IActionResult> AddBolum([FromBody] BolumViewModel bolumViewModel)
        {
            await _sirketAppService.AddBolum(bolumViewModel);
            return Ok();
        }

        [HttpPost("AddMakine")]
        public async Task<IActionResult> AddMakine([FromBody] Tbl_MakineViewModel tbl_MakineViewModel)
        {
            await _sirketAppService.AddMakine(tbl_MakineViewModel);
            return Ok();
        }

        [HttpPost("AddNesne")]
        public async Task<IActionResult> AddNesne([FromBody] Tbl_NesneViewModel tbl_NesneViewModel)
        {
            await _sirketAppService.AddNesne(tbl_NesneViewModel);
            return Ok();
        }
    }
}
