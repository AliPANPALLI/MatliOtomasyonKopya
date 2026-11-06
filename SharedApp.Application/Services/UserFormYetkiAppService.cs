using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class UserFormYetkiAppService : AppService<UserFormYetki, UserFormYetkiViewModel>, IUserFormYetkiAppService
    {
        private readonly UserCacheService _userCacheService;
        private readonly IUserFormYetkiRepository _userFormYetkiRepository;
        private readonly IFormRepository _formRepository;
        private readonly IModulRepository _modulRepository;

        public UserFormYetkiAppService(IMapper mapper, UserCacheService userCacheService, IUserFormYetkiRepository userFormYetkiRepository,
            IFormRepository formRepository, IModulRepository modulRepository) : base(mapper, userFormYetkiRepository)
        {
            _userCacheService = userCacheService;
            _userFormYetkiRepository = userFormYetkiRepository;
            _formRepository = formRepository;
            _modulRepository = modulRepository;
        }

        public async Task AddUserFormYetki(List<string> secilenKullanicilar, List<int> secilenFormlar, bool okumaYetkisi, bool yazmaYetkisi, bool degistirmeYetkisi, bool silmeYetkisi)
        {
            var userFormYetkiList = new List<UserFormYetki>();
            var userFormYetkiler = await _userFormYetkiRepository.GetAllAsyncByPredicateWithAsNoTracking(x => secilenFormlar.Contains(x.FormId) && secilenKullanicilar.Equals(x.UserId));

            foreach (var userId in secilenKullanicilar)
            {
                foreach (var formId in secilenFormlar)
                {
                    var userFormYetki = userFormYetkiler.Where(x => x.FormId == formId && x.UserId == userId).FirstOrDefault();
                    if (userFormYetki == null)
                    {
                        var addModel = new UserFormYetki { UserId = userId, FormId = formId, Okuma = okumaYetkisi, Yazma = yazmaYetkisi, Degistirme = degistirmeYetkisi, Silme = silmeYetkisi, Silindi = false };
                        userFormYetkiList.Add(addModel);
                    }
                    else
                    {
                        if (userFormYetki.Silindi.GetValueOrDefault(false))
                        {
                            _userFormYetkiRepository.Attach(userFormYetki);
                            userFormYetki.Okuma = okumaYetkisi;
                            userFormYetki.Yazma = yazmaYetkisi;
                            userFormYetki.Degistirme = degistirmeYetkisi;
                            userFormYetki.Silme = silmeYetkisi;
                            userFormYetki.Silindi = false;
                            userFormYetki.SilinmeZamani = null;
                        }
                    }
                }
            }
            await _userFormYetkiRepository.AddRangeAsync(userFormYetkiList);
            await _userFormYetkiRepository.SaveChangesAsync();
        }

        public async Task EditUserFormYetki(UserFormYetkiViewModel userFormYetkiViewModel)
        {
            var userFormYetki = await _userFormYetkiRepository.GetAsyncByPredicateWithAsNoTracking(x => x.FormId == userFormYetkiViewModel.FormId && x.UserId == userFormYetkiViewModel.UserId);

            if (userFormYetki == null)
            {
                var addModel = new UserFormYetki
                {
                    UserId = userFormYetkiViewModel.UserId,
                    FormId = userFormYetkiViewModel.FormId,
                    Okuma = userFormYetkiViewModel.Okuma,
                    Yazma = userFormYetkiViewModel.Yazma,
                    Degistirme = userFormYetkiViewModel.Degistirme,
                    Silme = userFormYetkiViewModel.Silme,
                    Silindi = false
                };
                await _userFormYetkiRepository.AddAsync(addModel);
            }
            else
            {
                _userFormYetkiRepository.Attach(userFormYetki);
                userFormYetki.Okuma = userFormYetki.Okuma;
                userFormYetki.Yazma = userFormYetkiViewModel.Yazma;
                userFormYetki.Degistirme = userFormYetkiViewModel.Degistirme;
                userFormYetki.Silme = userFormYetkiViewModel.Silme;
                userFormYetki.Silindi = userFormYetkiViewModel.Silindi;
                userFormYetki.SilinmeZamani = userFormYetkiViewModel.Silindi.GetValueOrDefault(false) ? DateTime.Now : null;
            }
            await _userFormYetkiRepository.SaveChangesAsync();
        }

        public async Task<List<UserFormYetki>> GetAllUserFormYetkiByUserId(string userId)
        {
            return await _userFormYetkiRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.UserId == userId);
        }

        public async Task<List<UserFormYetkiViewModel>> GetAllUserFormYetkis()
        {
            var userFormYetkiler = await _userFormYetkiRepository.GetAllListAsync();

            var users = await _userCacheService.GetUsersAsync("a");
            var moduls = await _modulRepository.GetAllListAsync();
            var forms = await _formRepository.GetAllListAsync();

            // Kullanıcı bilgilerini eşleştir ve döndür
            return userFormYetkiler.Select(ufy => new UserFormYetkiViewModel
            {
                Id = ufy.Id,
                UserId = users.FirstOrDefault(u => u.Id == ufy.UserId)?.Id,
                UserName = users.FirstOrDefault(u => u.Id == ufy.UserId)?.UserName,
                FullName = users.FirstOrDefault(u => u.Id == ufy.UserId)?.Name + " " + users.FirstOrDefault(u => u.Id == ufy.UserId)?.Surname,
                //ModulId = forms.FirstOrDefault(f => f.Id == ufy.FormId)?.ModulId,
                ModulName = moduls.FirstOrDefault(m => m.Id == forms.FirstOrDefault(f => f.Id == ufy.FormId)?.ModulId)?.Name,
                FormId = ufy.FormId,
                FormName = forms.FirstOrDefault(f => f.Id == ufy.FormId)?.Name,
                Okuma = ufy.Okuma,
                Yazma = ufy.Yazma,
                Degistirme = ufy.Degistirme,
                Silme = ufy.Silme,
                Silindi = ufy.Silindi,
                SilinmeZamani = ufy.SilinmeZamani
            }).ToList();
        }

        public async Task SetSilindi(int id)
        {
            var form = await _userFormYetkiRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
            form.Silindi = true;
            form.SilinmeZamani = DateTime.Now;
            await _userFormYetkiRepository.SaveChangesAsync();
        }
    }
}