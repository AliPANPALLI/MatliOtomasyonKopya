using Microsoft.EntityFrameworkCore;
using Ticket.Domain.DbViewModels;
using Ticket.Domain.Interfaces;
using Ticket.Domain.Models;
using Ticket.Infrastructure.Repositories.Core;

namespace Ticket.Infrastructure.Repositories
{
    public class TicketMatliTakipciEslemeRepository : Repository<TicketMatliTakipciEsleme>, ITicketMatliTakipciEslemeRepository
    {
        private readonly TicketDbContext _context;
        public TicketMatliTakipciEslemeRepository(TicketDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<vwTicketMatliTakipciEsleme>> GetAllvwTicketMatliTakipciEslemes()
        {
            return await _context.vwTicketMatliTakipciEsleme.ToListAsync();
        }

        public async Task<List<vwTicketMatliTakipciEsleme>> GetAllvwTicketMatliTakipciEslemesByTicketIds(List<int> ticketIds)
        {
            return await _context.vwTicketMatliTakipciEsleme.Where(x => ticketIds.Contains(x.TicketId) && x.Silindi != true).ToListAsync();
        }

        public async Task<List<vwTicketMatliTakipciEsleme>> GetAllvwTicketMatliTakipciEslemesByTicketId(int ticketId)
        {
            return await _context.vwTicketMatliTakipciEsleme.Where(x => x.TicketId == ticketId && x.Silindi != true).ToListAsync();
        }
    }
}
