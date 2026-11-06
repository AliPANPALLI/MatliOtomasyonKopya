using Microsoft.EntityFrameworkCore;
using Ticket.Domain.DbViewModels;
using Ticket.Domain.Interfaces;
using Ticket.Domain.Models;
using Ticket.Infrastructure.Repositories.Core;

namespace Ticket.Infrastructure.Repositories
{
    public class TicketSorumluUserEslemeRepository : Repository<TicketSorumluUserEsleme>, ITicketSorumluUserEslemeRepository
    {
        private readonly TicketDbContext _context;
        public TicketSorumluUserEslemeRepository(TicketDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<vwTicketSorumluUserEsleme>> GetAllvwTicketSorumluUserEslemeByTicketId(int ticketId)
        {
            return await _context.vwTicketSorumluUserEsleme.Where(x => x.TicketId == ticketId && x.Silindi != true).ToListAsync();
        }

        public async Task<List<vwTicketSorumluUserEsleme>> GetAllvwTicketSorumluUserEslemes()
        {
            return await _context.vwTicketSorumluUserEsleme.ToListAsync();
        }

        public async Task<List<vwTicketSorumluUserEsleme>> GetAllvwTicketSorumluUserEslemesByTicketIds(List<int> ticketIds)
        {
            return await _context.vwTicketSorumluUserEsleme.Where(x => ticketIds.Contains(x.TicketId) && x.Silindi != true).ToListAsync();
        }
    }
}