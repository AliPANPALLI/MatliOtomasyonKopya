using SuHatScada.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SuHatScada.API.Controllers
{
    [ApiController]
    [Route("api/warmup")]
    [AllowAnonymous]
    public class WarmupController : ControllerBase
    {
        private readonly SuHatScadaDbContext _context;

        public WarmupController(SuHatScadaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await _context.SuKaynak.AsNoTracking().Take(1).ToListAsync();
            return Ok(new { status = "warmed", service = "SuHatScada.API", time = DateTimeOffset.UtcNow });
        }
    }
}
