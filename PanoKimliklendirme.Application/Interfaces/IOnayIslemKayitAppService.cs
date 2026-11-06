using PanoKimliklendirme.Application.ViewModels;
using PanoKimliklendirme.Domain.DbViewModels;
using PanoKimliklendirme.Domain.Models;
using SharedKernel.Core;

namespace PanoKimliklendirme.Application.Interfaces
{
    public interface IOnayIslemKayitAppService : IAppService<OnayIslemKayitViewModel>
    {
        Task<List<OnayIslemKayit>> GetAllOnayIslemKayit();
        Task<List<vwDistinctOnayIslemKayit>> GetAllvwDistinctOnayIslemKayit(string userId);
        Task<List<string>> GetOnayIslemKayitFromTreeNodeId(int id, string nodeType);
        Task<List<string>> GetOnayIslemKayitlarByIdForDosya(int onayIslemKayitId);
        Task<bool> OnayaGonderByNodeIdAndType(int nodeId, string nodeType, string nodeName, string userId);
        Task<bool> IslemOnayla(int id, string userId);
        Task<bool> IslemReddet(int id, string userId);
        Task DosyaIslemOnayla(int islemId, string userId);
        Task DosyaIslemReddet(int islemId, string userId);
        Task CihazIslemOnayla(int cihazId, string userId);
        Task CihazIslemReddet(int cihazId, string userId);
    }
}