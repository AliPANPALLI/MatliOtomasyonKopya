using PoultryHouseScada.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PoultryHouseScada.API.Controllers
{
    [ApiController]
    [Route("api/warmup")]
    [AllowAnonymous]
    public class WarmupController : ControllerBase
    {
        private readonly KumesScadaDbContext _context;

        public WarmupController(KumesScadaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await _context.Kumes.AsNoTracking().Take(1).ToListAsync();
            return Ok(new { status = "warmed", service = "PoultryHouseScada.API", time = DateTimeOffset.UtcNow });
        }
    }
}
