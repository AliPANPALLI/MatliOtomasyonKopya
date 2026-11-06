using SharedApp.Domain.Models;
using SharedKernel.Core;

namespace SharedApp.Domain.Interfaces
{
    public interface IMailGonderimTakipRepository : IRepository<MailGonderimTakip>
    {
        Task MailKontrolEtVeGonder();
        Task<bool> AddMailGonderimTakip(string? mailBasligi, string? mailIcerik, string? ekYolu, bool? ekyoluKlasorMu, string? hataMesaji, string? gonderilecekTo, string? gonderilecekCc, bool? isHtml);
    }
}
