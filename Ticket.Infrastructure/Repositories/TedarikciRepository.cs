using Ticket.Domain.Interfaces;
using Ticket.Domain.Models;
using Ticket.Infrastructure.Repositories.Core;

namespace Ticket.Infrastructure.Repositories
{
    public class TedarikciRepository : Repository<Tedarikci>, ITedarikciRepository
    {
        private readonly TicketDbContext _context;
        public TedarikciRepository(TicketDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
