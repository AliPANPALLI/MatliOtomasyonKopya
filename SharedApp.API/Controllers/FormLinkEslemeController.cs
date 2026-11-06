using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;

namespace SharedApp.API.Controllers
{
    // Bir Form altında GERÇEKTEN var olan, kullanıcıya atanabilecek
    // link'lerin kataloğu — admin ekranında "bu form için hangi linkler
    // tanımlı" listesini ve UserYetki atama ekranındaki link seçim
    // listesini besler. SidebarGoster alanı hangi linklerin sidebar'da ayrı
    // bir menü öğesi olarak gösterileceğini belirtir.
    [Route("api/[controller]")]
    [ApiController]
    public class FormLinkEslemeController : BaseApiController<FormLinkEslemeViewModel>
    {
        private readonly IFormLinkEslemeAppService _formLinkEslemeAppService;

        public FormLinkEslemeController(IFormLinkEslemeAppService formLinkEslemeAppService) : base(formLinkEslemeAppService)
        {
            _formLinkEslemeAppService = formLinkEslemeAppService;
        }

        [HttpGet("GetAllEnriched")]
        public async Task<IActionResult> GetAllEnriched()
        {
            var result = await _formLinkEslemeAppService.GetAllEnriched();
            return Ok(result);
        }

        [HttpGet("GetAllByFormId")]
        public async Task<IActionResult> GetAllByFormId(int formId)
        {
            var result = await _formLinkEslemeAppService.GetAllByFormId(formId);
            return Ok(result);
        }

        [HttpGet("GetAllByModulId")]
        public async Task<IActionResult> GetAllByModulId(int modulId)
        {
            var result = await _formLinkEslemeAppService.GetAllByModulId(modulId);
            return Ok(result);
        }
    }
}
