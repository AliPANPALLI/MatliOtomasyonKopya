using SharedApp.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SharedApp.API.Controllers
{
    [ApiController]
    [Route("api/warmup")]
    [AllowAnonymous]
    public class WarmupController : ControllerBase
    {
        private readonly SharedDbContext _context;

        public WarmupController(SharedDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await _context.Tesis.AsNoTracking().Take(1).ToListAsync();
            return Ok(new { status = "warmed", service = "SharedApp.API", time = DateTimeOffset.UtcNow });
        }
    }
}
