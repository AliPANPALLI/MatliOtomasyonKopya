using Ticket.Domain.DbViewModels;
using Ticket.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace Ticket.Domain.Interfaces
{
    public interface ITicketSorumluUserEslemeRepository : IRepository<TicketSorumluUserEsleme>
    {
        Task<List<vwTicketSorumluUserEsleme>> GetAllvwTicketSorumluUserEslemes();
        Task<List<vwTicketSorumluUserEsleme>> GetAllvwTicketSorumluUserEslemeByTicketId(int ticketId);
        Task<List<vwTicketSorumluUserEsleme>> GetAllvwTicketSorumluUserEslemesByTicketIds(List<int> ticketIds);
    }
}