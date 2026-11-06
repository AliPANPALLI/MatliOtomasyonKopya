using SharedKernel.Core;
using Ticket.Domain.DbViewModels;
using Ticket.Domain.Models;

namespace Ticket.Domain.Interfaces
{
    public interface ITicketMatliTakipciEslemeRepository : IRepository<TicketMatliTakipciEsleme>
    {
        Task<List<vwTicketMatliTakipciEsleme>> GetAllvwTicketMatliTakipciEslemes();
        Task<List<vwTicketMatliTakipciEsleme>> GetAllvwTicketMatliTakipciEslemesByTicketId(int ticketId);
        Task<List<vwTicketMatliTakipciEsleme>> GetAllvwTicketMatliTakipciEslemesByTicketIds(List<int> ticketIds);
    }
}
