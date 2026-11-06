using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using PanoKimliklendirme.Application.Interfaces;
using PanoKimliklendirme.Application.Services.Core;
using PanoKimliklendirme.Application.ViewModels;
using PanoKimliklendirme.Domain.Interfaces;
using PanoKimliklendirme.Domain.Models;
using System.Text.Json;
using static SharedKernel.ApplicationConstants;

namespace PanoKimliklendirme.Application.Services
{
    public class MakineAppService : AppService<Makine, MakineViewModel>, IMakineAppService
    {
        private readonly IMakineRepository _makineRepository;
        private readonly IBolumRepository _bolumRepository;
        private readonly IPanoMakineEslemeRepository _panoMakineEslemeRepository;
        private readonly ICihazRepository _cihazRepository;
        private readonly IPanoRepository _panoRepository;
        private readonly IOnayIslemKayitRepository _onayIslemKayitRepository;


        public MakineAppService(IMapper mapper, IMakineRepository makineRepository, IBolumRepository bolumRepository,
            IPanoMakineEslemeRepository panoMakineEslemeRepository, ICihazRepository cihazRepository, IPanoRepository panoRepository,
            IOnayIslemKayitRepository onayIslemKayitRepository) : base(mapper, makineRepository)
        {
            _makineRepository = makineRepository;
            _bolumRepository = bolumRepository;
            _panoMakineEslemeRepository = panoMakineEslemeRepository;
            _cihazRepository = cihazRepository;
            _panoRepository = panoRepository;
            _onayIslemKayitRepository = onayIslemKayitRepository;
        }

        public async Task SetSilindi(int id, string userId)
        {
            try
            {
                var makine = await _makineRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
                var lokasyonId = await _makineRepository.FindLokasyonIdByMakineId(id);

                if (makine.OnayIslemdeMi.GetValueOrDefault(false))
                {
                    var onayIslemKayitSilmeVarMi = await _onayIslemKayitRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.IslemTipi == PanoKimlikOnayIslemTipi.Silme && x.OgeTipi == TreeViewNodeTypes.Makine && x.OgeId == makine.Id);
                    if (onayIslemKayitSilmeVarMi != null)
                        return;
                }
                var onayIslemKayit = new OnayIslemKayit();
                onayIslemKayit.UserId = userId;
                onayIslemKayit.OgeTipi = TreeViewNodeTypes.Bolum;
                onayIslemKayit.OgeId = id;
                onayIslemKayit.IslemTipi = PanoKimlikOnayIslemTipi.Silme;
                onayIslemKayit.IslemVerisi = JsonSerializer.Serialize(makine);
                onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Beklemede;
                onayIslemKayit.KayitZamani = DateTime.Now;
                onayIslemKayit.LokasyonId = lokasyonId;

                await _onayIslemKayitRepository.AddAsync(onayIslemKayit);
                await _onayIslemKayitRepository.SaveChangesAsync();

                //TODO: Yetkisi varsa onaya düşmemeli zaten
                var panoMakineEslemeler = await _panoMakineEslemeRepository.GetAllAsyncByPredicate(x => x.MakineId == id && x.Silindi != true).ToListAsync();
                foreach (var panoMakineEsleme in panoMakineEslemeler)
                {
                    panoMakineEsleme.Silindi = true;
                }

                var panolar = await _panoRepository.GetAllAsyncByPredicate(x => panoMakineEslemeler.Select(y => y.PanoId).ToList().Contains(x.Id) && x.Silindi != true).ToListAsync();
                foreach (var pano in panolar)
                {
                    var cihazlar = await _cihazRepository.GetAllAsyncByPredicate(x => x.PanoId == pano.Id && x.Silindi != true).ToListAsync();
                    foreach (var cihaz in cihazlar)
                    {
                        cihaz.Silindi = true;
                    }
                }

                makine.Silindi = true;
                await _makineRepository.SaveChangesAsync();
            }
            catch (Exception)
            {
            }
        }

        public async Task<List<int>> GetLokasyonIdsByMakineIds(List<int> makineIds)
        {
            var bolumIdList = (await _makineRepository.GetAllAsyncByPredicateWithAsNoTracking(x => makineIds.Contains(x.Id) && x.Silindi != true)).Select(x => x.BolumId).ToList();
            var lokasyonList = (await _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(x => bolumIdList.Contains(x.Id) && x.Silindi != true)).Select(x => x.LokasyonId).ToList();
            return lokasyonList;
        }

        public async Task EditMakine(MakineViewModel makineViewModel, string userId)
        {
            //TODO if (IUserMakineYetkiRepository varsa guncelleme yapılır)
            //{_repo.Update(model);
            //await _repo.SaveChangesAsync();}
            //else
            var model = _mapper.Map<Makine>(makineViewModel);
            if (model.OnayIslemdeMi.GetValueOrDefault(false))
            {
                var onayIslemKayitSilmeVarMi = await _onayIslemKayitRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.IslemTipi == PanoKimlikOnayIslemTipi.Silme && x.OgeTipi == TreeViewNodeTypes.Makine && x.OgeId == model.Id);
                if (onayIslemKayitSilmeVarMi != null)
                    return;
            }
            var lokasyonId = await _makineRepository.FindLokasyonIdByMakineId(model.Id);

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

        public async Task<List<Makine>> GetAllMakineForYetki()
        {
            var listMakine = new List<Makine>();

            var makineler = await _makineRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Silindi != true && x.OnayIslemdeMi != true);
            listMakine.AddRange(makineler);

            return listMakine;
        }
    }
}
