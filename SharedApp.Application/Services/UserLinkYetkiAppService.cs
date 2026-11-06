using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class UserLinkYetkiAppService : AppService<UserLinkYetki, UserLinkYetkiViewModel>, IUserLinkYetkiAppService
    {
        private readonly UserCacheService _userCacheService;
        private readonly IUserLinkYetkiRepository _userLinkYetkiRepository;
        private readonly IFormRepository _formRepository;

        public UserLinkYetkiAppService(IMapper mapper, UserCacheService userCacheService, IUserLinkYetkiRepository userLinkYetkiRepository, IFormRepository formRepository) : base(mapper, userLinkYetkiRepository)
        {
            _userLinkYetkiRepository = userLinkYetkiRepository;
            _userCacheService = userCacheService;
            _formRepository = formRepository;
        }

        public async Task AddUserLinkYetki(List<string> secilenKullanicilar, List<string> secilenLinks, int formId, bool okumaYetkisi, bool yazmaYetkisi, bool sorumluMu, bool silindi, bool aktif)
        {
            await _userLinkYetkiRepository.AddUserLinkYetki(secilenKullanicilar, secilenLinks, formId, okumaYetkisi, yazmaYetkisi, sorumluMu, silindi, aktif);
        }

        public async Task<List<vwUserLinkYetki>> GetAllUserLinkYetkiForTesisScada()
        {
            return await _userLinkYetkiRepository.GetAllUserLinkYetkiForTesisScada();
        }

        public async Task<List<vwUserLinkYetki>> GetAllUserLinkYetkiForSunucuScada()
        {
            return await _userLinkYetkiRepository.GetAllUserLinkYetkiForSunucuScada();
        }

        public async Task SetSilindi(int id)
        {
            var userMakineYetki = await _userLinkYetkiRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
            userMakineYetki.Silindi = true;
            userMakineYetki.SilinmeZamani = DateTime.Now;
            await _userLinkYetkiRepository.SaveChangesAsync();
        }

        public async Task<List<Form>> GetAllFormForTesisScada()
        {
            return await _formRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.ModulId == 11);
        }

        public async Task<List<Form>> GetAllFormForSunucuScada()
        {
            return await _formRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.ModulId == 10);
        }

        public async Task EditUserLinkYetki(UserLinkYetkiViewModel userLinkYetkiViewModel)
        {
            var model = _mapper.Map<UserLinkYetki>(userLinkYetkiViewModel);
            await _userLinkYetkiRepository.UpdateAsync(model);
            await _userLinkYetkiRepository.SaveChangesAsync();
        }
    }
}