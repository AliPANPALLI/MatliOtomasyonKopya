using MapsterMapper;
using EnerjiIzleme.Application.Interfaces;
using EnerjiIzleme.Application.Services.Core;
using EnerjiIzleme.Application.ViewModels;
using EnerjiIzleme.Domain.DbViewModels;
using EnerjiIzleme.Domain.Interfaces;
using EnerjiIzleme.Domain.Models;

namespace EnerjiIzleme.Application.Services
{
    public class CihazAppService : AppService<Cihaz, CihazViewModel>, ICihazAppService
    {
        private readonly ICihazRepository _cihazRepository;
        public CihazAppService(IMapper mapper, ICihazRepository cihazRepository) : base(mapper, cihazRepository)
        {
            _cihazRepository = cihazRepository;
        }

        public async Task AddCihaz(CihazViewModel cihazViewModel, string userId)
        {
            if (cihazViewModel.ParentCihazId == null || cihazViewModel.ParentCihazId.GetValueOrDefault(0) == cihazViewModel.Id)
            {
                cihazViewModel.ParentCihazId = 0;
            }

            var model = _mapper.Map<Cihaz>(cihazViewModel);
            model.LokasyonEnerjiHesabindaKullan = true;
            model.Silindi = false;
            model.KayitZamani = DateTime.Now;
            model.EkleyenUserId = userId;
            model.EklenmeZamani = DateTime.Now;
            model.Onaylandi = false;
            model.OnaylayanUserId = null;
            model.OnaylanmaZamani = null;
            await _cihazRepository.AddCihaz(model);
        }

        public async Task EditCihaz(CihazViewModel cihazViewModel, string userId)
        {
            var model = await _cihazRepository.GetAsyncByPredicate(x => x.Id == cihazViewModel.Id);

            if (model == null)
                throw new Exception("Cihaz bulunamadı.");

            if (cihazViewModel.ParentCihazId == null || cihazViewModel.ParentCihazId.GetValueOrDefault(0) == cihazViewModel.Id)
            {
                cihazViewModel.ParentCihazId = 0;
            }
            model.Name = cihazViewModel.Name;
            model.MakineId = cihazViewModel.MakineId.GetValueOrDefault();
            model.ModelId = cihazViewModel.ModelId.GetValueOrDefault();
            model.Aktif = cihazViewModel.Aktif.GetValueOrDefault();
            model.SlaveId = cihazViewModel.SlaveId.GetValueOrDefault();
            model.GatewayId = cihazViewModel.GatewayId.GetValueOrDefault();
            model.ParentCihazId = cihazViewModel.ParentCihazId.GetValueOrDefault();
            model.LokasyonEnerjiHesabindaKullan = true;
            model.GuncelleyenUserId = userId;
            model.GuncellemeZamani = DateTime.Now;
            model.Onaylandi = false;
            model.OnaylayanUserId = null;
            model.OnaylanmaZamani = null;
            await _cihazRepository.UpdateAsync(model);
            await _cihazRepository.SaveChangesAsync();
        }

        public async Task<List<vwCihaz>> GetAllCihaz(int lokasyonId)
        {
            return await _cihazRepository.GetAllCihaz(lokasyonId);
        }

        public async Task<List<CihazModel>> GetAllCihazModel()
        {
            return await _cihazRepository.GetAllCihazModel();
        }

        public async Task<List<Gateway>> GetAllGateway()
        {
            return await _cihazRepository.GetAllGateway();
        }

        public async Task<CihazViewModel> GetvwCihazOnayById(int id)
        {
            var cihaz = await _cihazRepository.GetvwCihazOnayById(id);
            var model = _mapper.Map<CihazViewModel>(cihaz);
            return model;
        }
        public async Task<bool> OnaylaCihaz(int id, string userId)
        {
            try
            {
                var cihaz = await _cihazRepository.GetByIdAsync(id);
                cihaz.Onaylandi = true;
                cihaz.OnaylayanUserId = userId;
                cihaz.OnaylanmaZamani = DateTime.Now;
                await _cihazRepository.UpdateAsync(cihaz);
                await _cihazRepository.SaveChangesAsync();
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
                var cihaz = await _cihazRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
                if (cihaz != null)
                {
                    cihaz.Silindi = true;
                    cihaz.Aktif = false;
                    await _cihazRepository.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
