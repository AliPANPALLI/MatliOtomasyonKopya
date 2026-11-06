using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class MailGonderimTakipAppService : AppService<MailGonderimTakip, MailGonderimTakipViewModel>, IMailGonderimTakipAppService
    {
        private readonly IMailGonderimTakipRepository _mailGonderimTakipRepository;

        public MailGonderimTakipAppService(IMapper mapper, IMailGonderimTakipRepository mailGonderimTakipRepository) : base(mapper, mailGonderimTakipRepository)
        {
            _mailGonderimTakipRepository = mailGonderimTakipRepository;
        }

        public async Task<bool> AddMailGonderimTakip(string? mailBasligi, string? mailIcerik, string? ekYolu, bool? ekYoluKlasorMu, string? hataMesaji, string? gonderilecekTo, string? gonderilecekCc, bool? isHtml)
        {
            return await _mailGonderimTakipRepository.AddMailGonderimTakip(mailBasligi, mailIcerik, ekYolu, ekYoluKlasorMu, hataMesaji, gonderilecekTo, gonderilecekCc, isHtml);
        }
    }
}
