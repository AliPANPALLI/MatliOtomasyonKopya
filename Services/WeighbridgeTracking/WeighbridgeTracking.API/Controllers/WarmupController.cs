using WeighbridgeTracking.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WeighbridgeTracking.API.Controllers
{
    [ApiController]
    [Route("api/warmup")]
    [AllowAnonymous]
    public class WarmupController : ControllerBase
    {
        private readonly KantarTakipDbContext _context;

        public WarmupController(KantarTakipDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await _context.Firma.AsNoTracking().Take(1).ToListAsync();
            return Ok(new { status = "warmed", service = "WeighbridgeTracking.API", time = DateTimeOffset.UtcNow });
        }
    }
}
