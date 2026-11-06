using Ticket.Domain.Interfaces;
using Ticket.Domain.Models;
using Ticket.Infrastructure.Repositories.Core;

namespace Ticket.Infrastructure.Repositories
{
    public class TicketLogsRepository : Repository<TicketLogs>, ITicketLogsRepository
    {
        private readonly TicketDbContext _context;
        public TicketLogsRepository(TicketDbContext context) : base(context)
        {
            _context = context;
        }
    }
}