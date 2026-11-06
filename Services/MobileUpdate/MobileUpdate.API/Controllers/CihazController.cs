using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobileUpdate.API.Yetki;
using MobileUpdate.Application.Dtos;
using MobileUpdate.Application.Interfaces;

namespace MobileUpdate.API.Controllers
{
    /// <summary>
    /// Sahadaki cihazlar ve kurulu sürümleri. Yeni sürüm yayınlandıktan sonra eski sürümde
    /// kalan telefonları takip etmek için admin panel kullanır.
    /// </summary>
    [Authorize]
    [FormYetki(FormYetkiTuru.Okuma)]
    [Route("api/[controller]")]
    [ApiController]
    public class CihazController : ControllerBase
    {
        private readonly IMobilSurumAppService _mobilSurumAppService;

        public CihazController(IMobilSurumAppService mobilSurumAppService)
        {
            _mobilSurumAppService = mobilSurumAppService;
        }

        /// <summary>Cihazlar, en son görülen önce. GuncelMi = false olanlar eski sürümde kalmıştır.</summary>
        [HttpGet]
        public async Task<ActionResult<List<CihazDto>>> Listele()
        {
            return Ok(await _mobilSurumAppService.CihazlariListeleAsync());
        }
    }
}
