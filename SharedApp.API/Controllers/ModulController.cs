using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModulController : BaseApiController<ModulViewModel>
    {
        private readonly IModulAppService _modulAppService;

        public ModulController(IModulAppService modulAppService) : base(modulAppService)
        {
            _modulAppService = modulAppService;
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _modulAppService.SetSilindi(id);

            return Ok();
        }
    }
}
