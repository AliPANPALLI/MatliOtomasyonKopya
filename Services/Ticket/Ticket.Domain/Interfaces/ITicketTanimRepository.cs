using SharedKernel.Core;
using Ticket.Domain.DbViewModels;
using Ticket.Domain.Models;

namespace Ticket.Domain.Interfaces
{
    public interface ITicketTanimRepository : IRepository<TicketTanim>
    {
        Task<List<vwTicketTanim>> GetAllvwTicketTanim();
        Task<List<vwTicketTanim>> GetAllFilteredvwTicketTanimByTedarikciId(int tedarikciId);
        Task<List<vwTicketTanim>> GetAllFilteredvwTicketTanim(DateTime baslangicZamani, DateTime bitisZamani, int? tedarikciId);
        IQueryable<vwTicketTanim> GetvwTicketTanimQueryable(int? tedarikciId, DateTime? baslangicZamani, DateTime? bitisZamani);
        Task<vwTicketTanim> GetvwTicketTanimById(int ticketId);
        Task<List<vwTedarikciUser>> GetAllvwTedarikciUser();
        Task<vwTedarikciUser> GetUserSorumluMuByLoginUserId(string loginUserId);
        Task<bool> GetTakipciUserKisitliYetki(string loginUserId);
        Task<bool> GetTakipciUserYetkisiz(string loginUserId);
        Task<List<vwTedarikciUser>> GetSorumlularForMailByTedarikciId(int tedarikciId);
        Task<vwTedarikciUser> GetvwTedarikciUserForMatliUser(string loginUserId, int tedarikciId);
        Task<List<vwTicketMatliTakipciEsleme>> GetTakipcilerForMailByTicketId(int ticketId);
        Task<List<vwTicketAciklamaEsleme>> GetAllvwTicketAciklamaEsleme();
        Task<List<vwTicketAciklamaEsleme>> GetAllSorumluAciklamalarByTicketId(int ticketId);
        Task<List<vwTicketAciklamaEsleme>> GetAllvwTicketAciklamaEslemeByTicketId(int ticketId);
        Task<List<vwTicketAciklamaEsleme>> GetAllTakipciAciklamalarByTicketId(int ticketId);
        Task<List<ParaBirimi>> GetAllParaBirimis();
        //Task<List<vwTicketDurumLog>> GetAllvwTicketDurumLogs();
        Task<List<vwTicketDurumLog>> GetAllvwTicketDurumLogsByTicketId(int ticketId);
        Task<List<vwTicketTeklifFiyatLog>> GetAllvwTicketTeklifFiyatLogs(int ticketId);
        Task AddTicketDurumLog(int ticketId, int durumIdOncesi, int durumIdSonrasi, bool tedarikciMi, int? userId, string? matliUserId);
        Task AddTicketTeklifFiyatLog(int ticketId, decimal teklifFiyatOncesi, int teklifParaBirimiOncesi, decimal teklifFiyatSonrasi, int teklifParaBirimiSonrasi, int? userId, string? matliUserId);
        Task AddTicketAciklamaEsleme(int ticketId, string aciklama, string loginUserId, int? userId, string? matliUserId);
        Task<List<vwTicketAciklamaEsleme>> GetAllvwTicketAciklamaEslemeByTicketIds(List<int> ticketIds);
        Task<List<int>> GetTicketIdsWithAciklamaByTicketIds(List<int> ticketIds);
    }
}
