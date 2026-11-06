using MatliOtomasyon.Shared.Web.Http;
using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;
using System.Text.Json;

namespace SharedApp.API.Controllers
{
    /// <summary>
    /// Lokasyon (tesis) tanımlarının CRUD uçlarını ve ticket/yetki ekranlarının ihtiyaç duyduğu lokasyon listelerini sunar.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LokasyonController : BaseApiController<LokasyonViewModel>
    {
        private readonly ILokasyonAppService _lokasyonAppService;
        private readonly IResponseEncryptor _responseEncryptor;

        /// <summary>
        /// Lokasyon uygulama servisini ve frontend'e giden cevapları kodlayan servisi alır.
        /// </summary>
        public LokasyonController(ILokasyonAppService lokasyonAppService, IResponseEncryptor responseEncryptor) : base(lokasyonAppService)
        {
            _lokasyonAppService = lokasyonAppService;
            _responseEncryptor = responseEncryptor;
        }

        /// <summary>
        /// Ticket tanım ekranında seçilebilecek lokasyonları, frontend'in beklediği kodlanmış biçimde döner.
        /// </summary>
        [HttpGet("GetAllLokasyonForTicketTanim")]
        public async Task<IActionResult> GetAllLokasyonForTicketTanim()
        {
            var result = await _lokasyonAppService.GetAllLokasyonForTicketTanim();
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Verilen şirkete ait lokasyonları döner.
        /// </summary>
        [HttpGet("GetAllLokasyonBySirketId")]
        public async Task<IActionResult> GetAllLokasyonBySirketId(int sirketId)
        {
            var result = await _lokasyonAppService.GetAllLokasyonBySirketId(sirketId);

            return Ok(result);
        }

        /// <summary>
        /// Lokasyonu silindi olarak işaretler (soft delete).
        /// </summary>
        [HttpPut("SetSilindi")]
        public async Task<IActionResult> SetSilindi([FromBody] int id)
        {
            await _lokasyonAppService.SetSilindi(id);

            return Ok();
        }

        /// <summary>
        /// Yetki tanım ekranında kullanılacak lokasyon listesini döner.
        /// </summary>
        [HttpGet("GetAllLokasyonForYetki")]
        public async Task<IActionResult> GetAllMakineForYetki()
        {
            var result = await _lokasyonAppService.GetAllLokasyonForYetki();

            return Ok(result);
        }
    }
}
