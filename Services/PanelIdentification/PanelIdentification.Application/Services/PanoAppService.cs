using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using PanelIdentification.Application.Interfaces;
using PanelIdentification.Application.Services.Core;
using PanelIdentification.Application.ViewModels;
using PanelIdentification.Domain.Interfaces;
using PanelIdentification.Domain.Models;
using System.Text.Json;
using static MatliOtomasyon.Shared.Application.Constants.ApplicationConstants;

namespace PanelIdentification.Application.Services
{
    public class PanoAppService : AppService<Pano, PanoViewModel>, IPanoAppService
    {
        private readonly IPanoRepository _panoRepository;
        private readonly IPanoMakineEslemeRepository _panoMakineEslemeRepository;
        private readonly ICihazRepository _cihazRepository;
        private readonly IOnayIslemKayitRepository _onayIslemKayitRepository;
        public PanoAppService(IMapper mapper, IPanoRepository panoRepository, IPanoMakineEslemeRepository panoMakineEslemeRepository, ICihazRepository cihazRepository,
            IOnayIslemKayitRepository onayIslemKayitRepository) : base(mapper, panoRepository)
        {
            _panoRepository = panoRepository;
            _panoMakineEslemeRepository = panoMakineEslemeRepository;
            _cihazRepository = cihazRepository;
            _onayIslemKayitRepository = onayIslemKayitRepository;
        }

        public async Task<PanoViewModel> GetPanoByPanoKimlikNo(string panoKimlikNo)
        {
            var pano = await _panoRepository.GetAsyncByPredicateWithAsNoTracking(x => x.PanoKimlikNo.Equals(panoKimlikNo) && x.Silindi != true);
            if (pano == null) { throw new Exception(panoKimlikNo + " kimlik numaralı pano bulunamadı!"); }
            return _mapper.Map<PanoViewModel>(pano);
        }
        public async Task EditPano(PanoViewModel panoViewModel, string userId)
        {
            //TODO
            //if (IUserMakineYetkiRepository varsa guncelleme yapılır)
            //{_repo.Update(model);
            //await _repo.SaveChangesAsync();}
            //else
            //TODO: sorumluMu kontrolu eklenecek tum editlere...

            var model = _mapper.Map<Pano>(panoViewModel);
            //TODO: onayislemdemi alani dolu geliyor mu kontrol etmek lazim
            if (model.OnayIslemdeMi.GetValueOrDefault(false))
            {
                var onayIslemKayitSilmeVarMi = await _onayIslemKayitRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.IslemTipi == PanoKimlikOnayIslemTipi.Silme && x.OgeTipi == TreeViewNodeTypes.Pano && x.OgeId == model.Id);
                if (onayIslemKayitSilmeVarMi != null)
                    return;
            }
            var lokasyonId = await _panoRepository.FindLokasyonIdByPanoId(model.Id);

            var onayIslemKayit = new OnayIslemKayit();
            onayIslemKayit.UserId = userId;
            onayIslemKayit.OgeTipi = TreeViewNodeTypes.Pano;
            onayIslemKayit.OgeId = model.Id;
            onayIslemKayit.IslemTipi = PanoKimlikOnayIslemTipi.Duzenleme;
            onayIslemKayit.IslemVerisi = JsonSerializer.Serialize(model);
            onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Beklemede;
            onayIslemKayit.KayitZamani = DateTime.Now;
            onayIslemKayit.LokasyonId = lokasyonId;

            await _onayIslemKayitRepository.AddAsync(onayIslemKayit);
            await _onayIslemKayitRepository.SaveChangesAsync();
        }

        public async Task SetSilindi(int id, string userId)
        {
            try
            {
                var pano = await _panoRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
                var lokasyonId = await _panoRepository.FindLokasyonIdByPanoId(id);

                if (pano.OnayIslemdeMi.GetValueOrDefault(false))
                {
                    var onayIslemKayitSilmeVarMi = await _onayIslemKayitRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.IslemTipi == PanoKimlikOnayIslemTipi.Silme && x.OgeTipi == TreeViewNodeTypes.Pano && x.OgeId == pano.Id);
                    if (onayIslemKayitSilmeVarMi != null)
                        return;
                }

                var onayIslemKayit = new OnayIslemKayit();
                onayIslemKayit.UserId = userId;
                onayIslemKayit.OgeTipi = TreeViewNodeTypes.Pano;
                onayIslemKayit.OgeId = id;
                onayIslemKayit.IslemTipi = PanoKimlikOnayIslemTipi.Silme;
                onayIslemKayit.IslemVerisi = JsonSerializer.Serialize(pano);
                onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Beklemede;
                onayIslemKayit.KayitZamani = DateTime.Now;
                onayIslemKayit.LokasyonId = lokasyonId;

                await _onayIslemKayitRepository.AddAsync(onayIslemKayit);
                await _onayIslemKayitRepository.SaveChangesAsync();

                pano.Silindi = true;
                //await _panoRepository.SaveChangesAsync();

                var panoMakineEslemeler = await _panoMakineEslemeRepository.GetAllAsyncByPredicate(x => x.PanoId == id && x.Silindi != true).ToListAsync();
                foreach (var panoMakineEsleme in panoMakineEslemeler)
                {
                    panoMakineEsleme.Silindi = true;
                }
                //await _panoMakineEslemeRepository.SaveChangesAsync();

                var cihazlar = await _cihazRepository.GetAllAsyncByPredicate(x => x.PanoId == id && x.Silindi != true).ToListAsync();
                foreach (var cihaz in cihazlar)
                {
                    cihaz.Silindi = true;
                }
                //await _cihazRepository.SaveChangesAsync();
                await _panoRepository.SaveChangesAsync();
            }
            catch (Exception)
            {
            }
        }
    }
}