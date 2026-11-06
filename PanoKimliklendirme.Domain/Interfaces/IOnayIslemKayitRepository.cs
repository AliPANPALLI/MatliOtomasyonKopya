using Microsoft.EntityFrameworkCore.Storage;
using PanoKimliklendirme.Domain.DbViewModels;
using PanoKimliklendirme.Domain.Models;
using PanoKimliklendirme.Domain.Models.ReportModels;
using SharedKernel.Core;

namespace PanoKimliklendirme.Domain.Interfaces
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