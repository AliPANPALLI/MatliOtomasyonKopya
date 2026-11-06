using SharedApp.Application.ViewModels;
using SharedApp.Application.ViewModels.RequestModel;

namespace SharedApp.Application.Interfaces
{
    public interface IYanginFotografDosyaService
    {
        Task<KaydedilenFotografBilgisi> KaydetAsync(string base64Veri, string? contentType, CancellationToken cancellationToken = default);
        Task DosyaSilAsync(string goreliDosyaYolu, CancellationToken cancellationToken = default);
        string TamDosyaYolunuOlustur(string goreliDosyaYolu);
    }
}