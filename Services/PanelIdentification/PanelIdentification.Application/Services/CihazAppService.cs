using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using PanelIdentification.Application.Interfaces;
using PanelIdentification.Application.Services.Core;
using PanelIdentification.Application.ViewModels;
using PanelIdentification.Domain.Interfaces;
using PanelIdentification.Domain.Models;
using System.Text.Json;
using static SharedKernel.ApplicationConstants;

namespace PanelIdentification.Application.Services
{
    public class CihazAppService : AppService<Cihaz, CihazViewModel>, ICihazAppService
    {
        private readonly ICihazRepository _cihazRepository;
        private readonly IOnayIslemKayitRepository _onayIslemKayitRepository;

        public CihazAppService(IMapper mapper, ICihazRepository cihazRepository, IOnayIslemKayitRepository onayIslemKayitRepository) : base(mapper, cihazRepository)
        {
            _cihazRepository = cihazRepository;
            _onayIslemKayitRepository = onayIslemKayitRepository;
        }

        public async Task SetSilindi(int id, string userId)
        {
            try
            {
                var cihaz = await _cihazRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
                var lokasyonId = await _cihazRepository.FindLokasyonIdByCihazId(id);

                if (cihaz.OnayIslemdeMi.GetValueOrDefault(false))
                {
                    var onayIslemKayitSilmeVarMi = await _onayIslemKayitRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.IslemTipi == PanoKimlikOnayIslemTipi.Silme && x.OgeTipi == TreeViewNodeTypes.Cihaz && x.OgeId == cihaz.Id);
                    if (onayIslemKayitSilmeVarMi != null)
                        return;
                }

                var onayIslemKayit = new OnayIslemKayit();
                onayIslemKayit.UserId = userId;
                onayIslemKayit.OgeTipi = TreeViewNodeTypes.Bolum;
                onayIslemKayit.OgeId = id;
                onayIslemKayit.IslemTipi = PanoKimlikOnayIslemTipi.Silme;
                onayIslemKayit.IslemVerisi = JsonSerializer.Serialize(cihaz);
                onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Beklemede;
                onayIslemKayit.KayitZamani = DateTime.Now;
                onayIslemKayit.LokasyonId = lokasyonId;

                await _onayIslemKayitRepository.AddAsync(onayIslemKayit);
                await _onayIslemKayitRepository.SaveChangesAsync();

                cihaz.Silindi = true;
                await _cihazRepository.SaveChangesAsync();

                var onayIslemKayitlarVarMi = await _onayIslemKayitRepository.GetAllAsyncByPredicate(x => x.OgeId == cihaz.Id && x.OgeTipi == TreeViewNodeTypes.Cihaz && x.Durum == PanoKimlikOnayIslemDurum.Beklemede).ToListAsync();
                foreach (var onayIslemKaydi in onayIslemKayitlarVarMi)
                {
                    onayIslemKaydi.Durum = PanoKimlikOnayIslemDurum.Red;
                    onayIslemKaydi.GuncelleyenUserId = userId;
                    onayIslemKaydi.GuncellemeZamani = DateTime.Now;
                }
                await _onayIslemKayitRepository.SaveChangesAsync();
            }
            catch (Exception)
            {
            }
        }

        public async Task EditCihaz(CihazViewModel cihazViewModel, string userId)
        {
            //TODO if (IUserMakineYetkiRepository varsa guncelleme yapılır)
            //{_repo.Update(model);
            //await _repo.SaveChangesAsync();}
            //else
            var model = _mapper.Map<Cihaz>(cihazViewModel);
            if (model.OnayIslemdeMi.GetValueOrDefault(false))
            {
                var onayIslemKayitSilmeVarMi = await _onayIslemKayitRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.IslemTipi == PanoKimlikOnayIslemTipi.Silme && x.OgeTipi == TreeViewNodeTypes.Cihaz && x.OgeId == model.Id);
                if (onayIslemKayitSilmeVarMi != null)
                    return;
            }
            var lokasyonId = await _cihazRepository.FindLokasyonIdByCihazId(model.Id);

            var onayIslemKayit = new OnayIslemKayit();
            onayIslemKayit.UserId = userId;
            onayIslemKayit.OgeTipi = TreeViewNodeTypes.Bolum;
            onayIslemKayit.OgeId = model.Id;
            onayIslemKayit.IslemTipi = PanoKimlikOnayIslemTipi.Duzenleme;
            onayIslemKayit.IslemVerisi = JsonSerializer.Serialize(model);
            onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Beklemede;
            onayIslemKayit.KayitZamani = DateTime.Now;
            onayIslemKayit.LokasyonId = lokasyonId;

            await _onayIslemKayitRepository.AddAsync(onayIslemKayit);
            await _onayIslemKayitRepository.SaveChangesAsync();
        }
    }
}
