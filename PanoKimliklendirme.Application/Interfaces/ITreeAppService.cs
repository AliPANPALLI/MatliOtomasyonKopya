using Microsoft.AspNetCore.Http;
using PanoKimliklendirme.Application.ViewModels;
using PanoKimliklendirme.Application.ViewModels.ReportModels;
using PanoKimliklendirme.Domain.Models.ReportModels;

namespace PanoKimliklendirme.Application.Interfaces
{
    public interface ITreeAppService
    {
        Task<(string contentType, FileStream file)> GetDocument(string fileName, string filePath);
        Task<string> ReadTxtDocument(string filePath);
        Task EditTxtDocument(string fileContent, string filePath, string userId, int panoId, int lokasyonId);
        Task<List<TreeModel>> GetTreeByLokasyonId(string userId, int lokasyonId, bool onayliIslemlerDahil);
        Task<Tuple<List<TreeModel>, TreeModel, int>> GetTreeByPanoKimlikNo2(string userId, string panoKimlikNo, bool onayliIslemlerDahil);
        Task<Tuple<List<TreeModel>, TreeModel>> GetTreeByPanoKimlikNo(string userId, string panoKimlikNo, bool onayliIslemlerDahil);
        Task<Tuple<List<TreeModel>, TreeModel>> GetTreeViewByNodeIdAndType(string userId, int nodeId, string nodeType, bool onayliIslemlerDahil);
        Task<UserNodeYetkiModel> GetTreeYetki(string userId);
        Task<List<FilesModel>> GetDosyalarByPanoId(int panoId, string dosya);
        Task<List<FilesModel>> GetAllDosyalarByPanoId(string userId, int panoId, string dosya, bool onayliIslemlerDahil);
        Task<List<FilesModel>> GetDosyalarByDosyaYolu(string userId, string dosyaYolu, int panoId, bool? ustKlasorVarMi, bool onayliIslemlerDahil);
        Task EditDosyaAdiByDosyaYolu(string dosyaYolu, string eskiDosyaAdi, string yeniDosyaAdi, string dosyaUzanti, string userId, int panoId, int lokasyonId);
        Task<List<CihazModel>> GetCihazlarByPanoId(int panoId, string userId, bool onayliIslemlerDahil);
        Task<bool> GetSorumluMuFromTreeNodeId(string userId, int nodeId, string nodeType);
        Task<bool> AddNodeToTree(NodeModel node, string userId);
        Task<bool> AddCihazToTreeView(CihazViewModel cihaz, int lokasyonId, CancellationToken cancellation, string userId);
        Task<bool> NodeOnayaGonder(EditNodeModel node, string userId);
        Task<bool> EditNodeFromTreeView(EditNodeModel node, string userId);
        Task<bool> DeleteNodeFromTreeView(EditNodeModel node, string userId);
        Task<bool> UploadDosya(IFormFile[] files, string savePath, string panoId, string lokasyonId, CancellationToken cancellation, string userId);
        Task DeleteDosya(string dosya, CancellationToken cancellation, string userId, int panoId, int lokasyonId);
        Task DeleteKlasor(string klasor, CancellationToken cancellation, string userId, int panoId, int lokasyonId);
    }
}
