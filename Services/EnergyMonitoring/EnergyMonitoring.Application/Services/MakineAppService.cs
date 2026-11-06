using EnergyMonitoring.Application.Interfaces;
using EnergyMonitoring.Application.Services.Core;
using EnergyMonitoring.Application.ViewModels;
using EnergyMonitoring.Domain.Interfaces;
using EnergyMonitoring.Domain.Models;
using MapsterMapper;

namespace EnergyMonitoring.Application.Services
{
    public class MakineAppService : AppService<Makine, MakineViewModel>, IMakineAppService
    {
        private readonly IMakineRepository _makineRepository;
        public MakineAppService(IMapper mapper, IMakineRepository makineRepository) : base(mapper, makineRepository)
        {
            _makineRepository = makineRepository;
        }

        public async Task AddMakine(MakineViewModel makineViewModel, string userId)
        {
            var model = _mapper.Map<Makine>(makineViewModel);
            model.EklenmeZamani = DateTime.Now;
            model.EkleyenUserId = userId;
            model.Onaylandi = false;
            model.OnaylanmaZamani = null;
            model.OnaylayanUserId = null;
            await _makineRepository.AddMakine(model);
        }

        public async Task EditMakine(MakineViewModel makineViewModel, string userId)
        {
            var model = await _makineRepository.GetByIdAsync(makineViewModel.Id);
            model.Name = makineViewModel.Name;
            model.BolumId = makineViewModel.BolumId;
            model.MakineNo = makineViewModel.MakineNo;
            model.Aciklama = makineViewModel.Aciklama;
            model.Silindi = makineViewModel.Silindi;
            model.Aktif = makineViewModel.Aktif;
            //var model = _mapper.Map<Makine>(makineViewModel);
            model.GuncellemeZamani = DateTime.Now;
            model.GuncelleyenUserId = userId;
            model.Onaylandi = false;
            model.OnaylanmaZamani = null;
            model.OnaylayanUserId = null;
            await _makineRepository.UpdateAsync(model);
            await _makineRepository.SaveChangesAsync();
        }

        public async Task<List<Makine>> GetAllMakineByLokasyonId(int lokasyonId)
        {
            var makineList = await _makineRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Bolum.LokasyonId == lokasyonId && x.Silindi != true);
            return makineList;
        }

        public async Task<MakineViewModel> GetvwMakineById(int id)
        {
            var vwMakine = await _makineRepository.GetvwMakineById(id);
            var model = _mapper.Map<MakineViewModel>(vwMakine);
            return model;
        }

        public async Task<bool> OnaylaMakine(int id, string userId)
        {
            try
            {
                var makine = await _makineRepository.GetByIdAsync(id);
                makine.Onaylandi = true;
                makine.OnaylayanUserId = userId;
                makine.OnaylanmaZamani = DateTime.Now;
                await _makineRepository.UpdateAsync(makine);
                await _makineRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task SetSilindi(int id)
        {
            try
            {
                await _makineRepository.SetSilindi(id);
            }
            catch (Exception)
            {
            }
        }
    }
}