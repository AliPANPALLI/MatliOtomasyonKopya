using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services;
using SharedApp.Application.ViewModels;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SonGelistirmelerController : BaseApiController<SonGelistirmelerViewModel>
    {
        private readonly ISonGelistirmelerAppService _sonGelistirmelerAppService;

        public SonGelistirmelerController(ISonGelistirmelerAppService sonGelistirmelerAppService) : base(sonGelistirmelerAppService)
        {
            _sonGelistirmelerAppService = sonGelistirmelerAppService;
        }

        [HttpGet("GetSonGelistirmeler")]
        public async Task<IActionResult> GetSonGelistirmeler()
        {
            var result = await _sonGelistirmelerAppService.GetSonGelistirmeler();
            return new JsonResult(result);
        }
    }
}