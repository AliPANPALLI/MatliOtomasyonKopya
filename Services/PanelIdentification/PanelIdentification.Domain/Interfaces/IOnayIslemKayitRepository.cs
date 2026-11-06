using Microsoft.EntityFrameworkCore.Storage;
using PanelIdentification.Domain.DbViewModels;
using PanelIdentification.Domain.Models;
using PanelIdentification.Domain.Models.ReportModels;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace PanelIdentification.Domain.Interfaces
{
    public interface IOnayIslemKayitRepository : IRepository<OnayIslemKayit>
    {
        Task<List<vwDistinctOnayIslemKayit>> GetAllvwDistinctOnayIslemKayit(List<NodeYetkiModel> nodeYetkiler, string userId);
        Task<bool> IslemOnayla(int id, string userId);
        //Task<bool> IslemReddet(int id, string userId);
        Task<bool> IslemReddet(int id, string userId, IDbContextTransaction? transaction = null);
        Task<List<string>> GetOnayIslemKayitFromTreeNodeId(int id, string nodeType);
        Task<List<string>> GetOnayIslemKayitlarByIdForDosya(int onayIslemKayitId);
        Task<bool> OnayaGonderByNodeIdAndType(List<NodeYetkiModel> nodeYetkiler, int nodeId, string nodeType, string nodeName, string userId);
        Task DosyaIslemOnayla(int islemId, string userId);
        Task DosyaIslemReddet(int islemId, string userId);
        Task CihazIslemOnayla(int cihazId, string userId);
        Task CihazIslemReddet(int cihazId, string userId);
    }
}