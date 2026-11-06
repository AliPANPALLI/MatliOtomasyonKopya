using SharedKernel.Core;
using Ticket.Domain.DbViewModels;
using Ticket.Domain.Models;

namespace Ticket.Domain.Interfaces
{
    public interface ITicketSorumluUserEslemeRepository : IRepository<TicketSorumluUserEsleme>
    {
        Task<List<vwTicketSorumluUserEsleme>> GetAllvwTicketSorumluUserEslemes();
        Task<List<vwTicketSorumluUserEsleme>> GetAllvwTicketSorumluUserEslemeByTicketId(int ticketId);
        Task<List<vwTicketSorumluUserEsleme>> GetAllvwTicketSorumluUserEslemesByTicketIds(List<int> ticketIds);
    }
}