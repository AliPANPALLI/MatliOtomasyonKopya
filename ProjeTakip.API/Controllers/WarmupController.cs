using ProjeTakip.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProjeTakip.API.Controllers
{
    [ApiController]
    [Route("api/warmup")]
    [AllowAnonymous]
    public class WarmupController : ControllerBase
    {
        private readonly ProjeTakipDbContext _context;

        public WarmupController(ProjeTakipDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await _context.IsAdimlari.AsNoTracking().Take(1).ToListAsync();
            return Ok(new { status = "warmed", service = "ProjeTakip.API", time = DateTimeOffset.UtcNow });
        }
    }
}
