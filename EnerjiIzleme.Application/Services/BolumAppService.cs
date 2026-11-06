using MapsterMapper;
using EnerjiIzleme.Application.Interfaces;
using EnerjiIzleme.Application.Services.Core;
using EnerjiIzleme.Application.ViewModels;
using EnerjiIzleme.Domain.Interfaces;
using EnerjiIzleme.Domain.Models;

namespace EnerjiIzleme.Application.Services
{
    public class BolumAppService : AppService<Bolum, BolumViewModel>, IBolumAppService
    {
        private readonly IBolumRepository _bolumRepository;
        public BolumAppService(IMapper mapper, IBolumRepository bolumRepository) : base(mapper, bolumRepository)
        {
            _bolumRepository = bolumRepository;
        }

        public async Task AddBolum(BolumViewModel bolumViewModel, string userId)
        {
            var model = _mapper.Map<Bolum>(bolumViewModel);
            model.EklenmeZamani = DateTime.Now;
            model.EkleyenUserId = userId;
            model.Onaylandi = false;
            model.OnaylayanUserId = null;
            model.OnaylanmaZamani = null;
            model.Silindi = false;
            await _bolumRepository.AddBolum(model);
        }

        public async Task EditBolum(BolumViewModel bolumViewModel, string userId)
        {
            var model = await _bolumRepository.GetByIdAsync(bolumViewModel.Id);
            if (model == null)
                throw new Exception("Bölüm bulunamadı");

            model.Name = bolumViewModel.Name;
            model.LokasyonId = bolumViewModel.LokasyonId;
            model.Silindi = bolumViewModel.Silindi;
            model.Aktif = bolumViewModel.Aktif;
            //var model = _mapper.Map<Bolum>(bolumViewModel);
            model.GuncelleyenUserId = userId;
            model.GuncellemeZamani = DateTime.Now;
            model.Onaylandi = false;
            model.OnaylayanUserId = null;
            model.OnaylanmaZamani = null;
            await _bolumRepository.UpdateAsync(model);
            await _bolumRepository.SaveChangesAsync();
        }

        public async Task<List<Bolum>> GetAllBolumByLokasyonId(int lokasyonId)
        {
            var bolumList = await _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyonId && x.Silindi != true);
            return bolumList;
        }

        public async Task<BolumViewModel> GetvwBolumById(int id)
        {
            var vwBolum = await _bolumRepository.GetvwBolumById(id);
            var model = _mapper.Map<BolumViewModel>(vwBolum);
            return model;
        }

        public async Task<bool> OnaylaBolum(int id, string userId)
        {
            try
            {
                var bolum = await _bolumRepository.GetByIdAsync(id);
                bolum.Onaylandi = true;
                bolum.OnaylayanUserId = userId;
                bolum.OnaylanmaZamani = DateTime.Now;
                await _bolumRepository.UpdateAsync(bolum);
                await _bolumRepository.SaveChangesAsync();
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
                await _bolumRepository.SetSilindi(id);
            }
            catch (Exception)
            {
            }
        }
    }
}