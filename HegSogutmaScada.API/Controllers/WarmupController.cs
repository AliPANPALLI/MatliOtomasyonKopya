using HegSogutmaScada.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HegSogutmaScada.API.Controllers
{
    [ApiController]
    [Route("api/warmup")]
    [AllowAnonymous]
    public class WarmupController : ControllerBase
    {
        private readonly HegSogutmaScadaDbContext _context;

        public WarmupController(HegSogutmaScadaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await _context.OdaVanalari.AsNoTracking().Take(1).ToListAsync();
            return Ok(new { status = "warmed", service = "HegSogutmaScada.API", time = DateTimeOffset.UtcNow });
        }
    }
}
