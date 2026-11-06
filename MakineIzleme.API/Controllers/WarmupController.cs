using MakineIzleme.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MakineIzleme.API.Controllers
{
    [ApiController]
    [Route("api/warmup")]
    [AllowAnonymous]
    public class WarmupController : ControllerBase
    {
        private readonly MakineIzlemeDbContext _context;

        public WarmupController(MakineIzlemeDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await _context.Bolum.AsNoTracking().Take(1).ToListAsync();
            return Ok(new { status = "warmed", service = "MakineIzleme.API", time = DateTimeOffset.UtcNow });
        }
    }
}
