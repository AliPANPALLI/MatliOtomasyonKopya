using SharedApp.Application.ViewModels;
using SharedKernel.Core;

namespace SharedApp.Application.Interfaces
{
    public interface IMailGonderimTakipAppService : IAppService<MailGonderimTakipViewModel>
    {
        Task<bool> AddMailGonderimTakip(string? mailBasligi, string? mailIcerik, string? ekYolu, bool? ekYoluKlasorMu, string? hataMesaji, string? gonderilecekTo, string? gonderilecekCc, bool? isHtml);
    }
}
