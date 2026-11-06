using PanelIdentification.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PanelIdentification.API.Controllers
{
    [ApiController]
    [Route("api/warmup")]
    [AllowAnonymous]
    public class WarmupController : ControllerBase
    {
        private readonly PanoKimliklendirmeDbContext _context;

        public WarmupController(PanoKimliklendirmeDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await _context.Pano.AsNoTracking().Take(1).ToListAsync();
            return Ok(new { status = "warmed", service = "PanelIdentification.API", time = DateTimeOffset.UtcNow });
        }
    }
}
