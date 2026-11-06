using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MailGonderimTakipController : BaseApiController<MailGonderimTakipViewModel>
    {
        private readonly IMailGonderimTakipAppService _mailGonderimTakipAppService;

        public MailGonderimTakipController(IMailGonderimTakipAppService mailGonderimTakipAppService) : base(mailGonderimTakipAppService)
        {
            _mailGonderimTakipAppService = mailGonderimTakipAppService;
        }

        [HttpPost("AddMailGonderimTakip")]
        public async Task<IActionResult> AddMailGonderimTakip([FromForm] string? mailBasligi, [FromForm] string? mailIcerik, [FromForm] string? ekYolu, [FromForm] bool? ekYoluKlasorMu, [FromForm] string? hataMesaji, [FromForm] string? gonderilecekTo, [FromForm] string? gonderilecekCc, [FromForm] bool? isHtml)
        {
            var result = await _mailGonderimTakipAppService.AddMailGonderimTakip(mailBasligi, mailIcerik, ekYolu, ekYoluKlasorMu, hataMesaji, gonderilecekTo, gonderilecekCc, isHtml);
            return Ok(result);
        }
    }
}