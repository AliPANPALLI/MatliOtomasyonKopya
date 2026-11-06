using Authentication.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Authentication.API.Controllers
{
    [ApiController]
    [Route("api/warmup")]
    [AllowAnonymous]
    public class WarmupController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public WarmupController(AuthDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // SignIn akışının ısınması: aynı tabloya (User) salt-okunur sorgu
            await _context.User.AsNoTracking().Take(1).ToListAsync();
            return Ok(new { status = "warmed", service = "Authentication.API", time = DateTimeOffset.UtcNow });
        }
    }
}
