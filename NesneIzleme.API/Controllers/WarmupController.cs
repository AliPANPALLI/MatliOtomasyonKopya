using NesneIzleme.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace NesneIzleme.API.Controllers
{
    [ApiController]
    [Route("api/warmup")]
    [AllowAnonymous]
    public class WarmupController : ControllerBase
    {
        private readonly NesneIzlemeDbContext _context;

        public WarmupController(NesneIzlemeDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await _context.Tbl_Nesne.AsNoTracking().Take(1).ToListAsync();
            return Ok(new { status = "warmed", service = "NesneIzleme.API", time = DateTimeOffset.UtcNow });
        }
    }
}
