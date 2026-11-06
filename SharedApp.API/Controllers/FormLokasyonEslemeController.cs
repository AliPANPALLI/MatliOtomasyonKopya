using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;
using System.Xml.Linq;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FormLokasyonEslemeController : BaseApiController<FormLokasyonEslemeViewModel>
    {
        private readonly IFormLokasyonEslemeAppService _formLokasyonEslemeAppService;

        public FormLokasyonEslemeController(IFormLokasyonEslemeAppService formLokasyonEslemeAppService) : base(formLokasyonEslemeAppService)
        {
            _formLokasyonEslemeAppService = formLokasyonEslemeAppService;
        }

        [HttpGet("GetAllFormLokasyonEslemeDx")]
        public async Task<IActionResult> GetAllFormLokasyonEslemeDx(DataSourceLoadOptions loadOptions)
        {
            var result = _formLokasyonEslemeAppService.GetAllFormLokasyonEslemeDx();
            return Json(await DataSourceLoader.LoadAsync(result, loadOptions));
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _formLokasyonEslemeAppService.SetSilindi(id);

            return Ok();
        }

        [HttpPost("AddFormLokasyonEsleme")]
        public async Task<IActionResult> AddFormLokasyonEsleme([FromForm] List<int> secilenLokasyonlar, [FromForm] List<int> secilenFormlar, [FromForm] List<int> secilenCizimler)
        {
            var boolResult = await _formLokasyonEslemeAppService.AddFormLokasyonEsleme(secilenLokasyonlar, secilenFormlar, secilenCizimler);

            return Ok(boolResult);
        }

        [HttpPut("EditFormLokasyonEsleme")]
        public async Task<IActionResult> EditFormLokasyonEsleme([FromBody] FormLokasyonEslemeViewModel formLokasyonEslemeModel)
        {
            var boolResult = await _formLokasyonEslemeAppService.EditFormLokasyonEsleme(formLokasyonEslemeModel);

            return Ok(boolResult);
        }
    }
}
