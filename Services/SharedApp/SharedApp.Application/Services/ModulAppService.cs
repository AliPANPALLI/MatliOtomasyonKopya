using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class ModulAppService : AppService<Modul, ModulViewModel>, IModulAppService
    {
        private readonly IModulRepository _modulRepository;

        public ModulAppService(IMapper mapper, IModulRepository modulRepository) : base(mapper, modulRepository)
        {
            _modulRepository = modulRepository;
        }

        public async Task SetSilindi(int id)
        {
            var modul = await _modulRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
            modul.Silindi = true;
            await _modulRepository.SaveChangesAsync();
        }
    }
}
