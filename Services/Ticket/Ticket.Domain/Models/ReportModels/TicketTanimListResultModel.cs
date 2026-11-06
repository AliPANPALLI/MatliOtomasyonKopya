using Ticket.Domain.DbViewModels;

namespace Ticket.Domain.Models.ReportModels
{
    public class TicketTanimListResultModel
    {
        public List<TicketTanimReportModel> Items { get; set; } = new();
        public vwTedarikciUser? Sorumlu { get; set; }
    }
}
