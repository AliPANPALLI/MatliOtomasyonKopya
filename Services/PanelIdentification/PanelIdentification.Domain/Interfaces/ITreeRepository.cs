
using PanelIdentification.Domain.Models.ReportModels;
using SharedApp.Domain.Models;
using Bolum = PanelIdentification.Domain.Models.Bolum;

namespace PanelIdentification.Domain.Interfaces
{
    public interface ITreeRepository
    {
        Task<List<TreeModel>> GetTreeByLokasyonId(string userId, Lokasyon lokasyon, List<NodeYetkiModel> nodeYetkiler, bool onayliIslemlerDahil);
        Task<Bolum?> GetBolumByPanoId(int panoId);
        //Task<UserNodeYetkiModel> GetTreeYetki(string userId);
        Task<List<FilesModel>> GetDosyalarByPanoId(int panoId, string dosya);
        Task<List<FilesModel>> GetAllDosyalarByPanoId(List<NodeYetkiModel> nodeYetkiler, string userId, int panoId, string dosya, bool onayliIslemlerDahil);
        Task<List<FilesModel>> GetDosyalarByDosyaYolu(List<NodeYetkiModel> nodeYetkiler, string userId, string dosyaYolu, int panoId, bool? ustKlasorVarMi, bool onayliIslemlerDahil);
        Task<List<CihazModel>> GetCihazlarByPanoId(List<NodeYetkiModel> nodeYetkiler, int panoId, string userId, bool onayliIslemlerDahil);

        //Task<bool> AddNodeToTree(NodeModel node);
        //Task<bool> DeleteNodeFromTree(NodeModel node);
    }
}
