using KumesScada.Application.RequestModels;
using KumesScada.Application.ViewModels;

namespace KumesScada.Application.Interfaces
{
    public class NekropsiFotografIndirModel
    {
        public byte[] Veri { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
        public string DosyaAdi { get; set; } = string.Empty;
    }

    public interface INekropsiRaporuAppService
    {
        Task<List<NekropsiRaporuOzetViewModel>> GetKumesNekropsiRaporlari(int kumesId);
        Task<NekropsiRaporuViewModel?> GetNekropsiRaporuById(int id);
        Task<int> KaydetAsync(NekropsiRaporuKaydetRequest request, string? userId);
        Task GuncelleAsync(int id, NekropsiRaporuKaydetRequest request, string? userId);
        Task<NekropsiFotografIndirModel?> GetFotografAsync(int fotografId);
        Task SilFotografAsync(int fotografId);
    }
}
