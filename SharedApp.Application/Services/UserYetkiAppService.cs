using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class UserYetkiAppService : AppService<UserYetki, UserYetkiViewModel>, IUserYetkiAppService
    {
        private readonly IUserYetkiRepository _userYetkiRepository;

        public UserYetkiAppService(IMapper mapper, IUserYetkiRepository userYetkiRepository) : base(mapper, userYetkiRepository)
        {
            _userYetkiRepository = userYetkiRepository;
        }

        public async Task AddUserYetki(List<string> secilenKullanicilar, List<string> secilenLinks, int formId, bool okumaYetkisi, bool yazmaYetkisi, bool degistirmeYetkisi, bool silmeYetkisi, bool onaylayabilirMi, bool sorumluMu, bool silindi, bool aktif)
        {
            await _userYetkiRepository.AddUserYetki(secilenKullanicilar, secilenLinks, formId, okumaYetkisi, yazmaYetkisi, degistirmeYetkisi, silmeYetkisi, onaylayabilirMi, sorumluMu, silindi, aktif);
        }

        public async Task SetSilindi(int id)
        {
            var userYetki = await _userYetkiRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
            userYetki.Silindi = true;
            userYetki.SilinmeZamani = DateTime.Now;
            await _userYetkiRepository.SaveChangesAsync();
        }

        public async Task<List<UserYetkiViewModel>> GetAllByUserId(string userId)
        {
            var result = await _userYetkiRepository.GetAllByUserId(userId);
            return _mapper.Map<List<UserYetkiViewModel>>(result);
        }

        public async Task<List<UserYetkiViewModel>> GetKantarYetkiByUserId(string userId)
        {
            var result = await _userYetkiRepository.GetKantarYetkiByUserId(userId);
            return _mapper.Map<List<UserYetkiViewModel>>(result);
        }

        public async Task<List<UserYetkiViewModel>> GetAllByFormId(int formId)
        {
            var result = await _userYetkiRepository.GetAllByFormId(formId);
            return _mapper.Map<List<UserYetkiViewModel>>(result);
        }

        public async Task<List<UserYetkiDetayModel>> GetAllByModulId(int modulId)
        {
            return await _userYetkiRepository.GetAllByModulId(modulId);
        }

        public async Task<List<UserYetkiDetayModel>> GetAllEnriched()
        {
            return await _userYetkiRepository.GetAllEnriched();
        }

        public async Task<int> TransferUserYetkiByModul(string kaynakUserId, List<string> hedefUserIdler, int modulId)
        {
            return await _userYetkiRepository.TransferUserYetkiByModul(kaynakUserId, hedefUserIdler, modulId);
        }
    }
}
