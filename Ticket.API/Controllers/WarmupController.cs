using Ticket.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ticket.API.Controllers
{
    [ApiController]
    [Route("api/warmup")]
    [AllowAnonymous]
    public class WarmupController : ControllerBase
    {
        private readonly TicketDbContext _context;

        public WarmupController(TicketDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await _context.TicketTanim.AsNoTracking().Take(1).ToListAsync();
            return Ok(new { status = "warmed", service = "Ticket.API", time = DateTimeOffset.UtcNow });
        }
    }
}
