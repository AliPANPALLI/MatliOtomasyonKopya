using Ticket.Domain.Interfaces;
using Ticket.Domain.Models;
using Ticket.Infrastructure.Repositories.Core;

namespace Ticket.Infrastructure.Repositories
{
    public class TicketDurumRepository : Repository<TicketDurum>, ITicketDurumRepository
    {
        private readonly TicketDbContext _context;
        public TicketDurumRepository(TicketDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
