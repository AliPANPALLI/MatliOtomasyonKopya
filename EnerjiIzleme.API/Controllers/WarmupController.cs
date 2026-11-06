using EnerjiIzleme.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnerjiIzleme.API.Controllers
{
    [ApiController]
    [Route("api/warmup")]
    [AllowAnonymous]
    public class WarmupController : ControllerBase
    {
        private readonly EnerjiIzlemeDbContext _context;

        public WarmupController(EnerjiIzlemeDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await _context.AlarmLog.AsNoTracking().Take(1).ToListAsync();
            return Ok(new { status = "warmed", service = "EnerjiIzleme.API", time = DateTimeOffset.UtcNow });
        }
    }
}
