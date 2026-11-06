using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FormController : BaseApiController<FormViewModel>
    {
        private readonly IFormAppService _formAppService;

        public FormController(IFormAppService formAppService) : base(formAppService)
        {
            _formAppService = formAppService;
        }

        [HttpGet("GetAllForm")]
        public async Task<IActionResult> GetAllForm()
        {
            var result = await _formAppService.GetAllForm();
            return Ok(result);
        }

        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _formAppService.SetSilindi(id);

            return Ok();
        }
    }
}