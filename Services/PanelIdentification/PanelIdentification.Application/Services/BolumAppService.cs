using MapsterMapper;
using PanelIdentification.Application.Interfaces;
using PanelIdentification.Application.Services.Core;
using PanelIdentification.Application.ViewModels;
using PanelIdentification.Domain.Interfaces;
using PanelIdentification.Domain.Models;
using System.Text.Json;
using static SharedKernel.ApplicationConstants;

namespace PanelIdentification.Application.Services
{
    public class BolumAppService : AppService<Bolum, BolumViewModel>, IBolumAppService
    {
        private readonly IBolumRepository _bolumRepository;
        private readonly IOnayIslemKayitRepository _onayIslemKayitRepository;
        public BolumAppService(IMapper mapper, IBolumRepository bolumRepository, IOnayIslemKayitRepository onayIslemKayitRepository) : base(mapper, bolumRepository)
        {
            _bolumRepository = bolumRepository;
            _onayIslemKayitRepository = onayIslemKayitRepository;
        }

        public async Task<List<int>> GetLokasyonIdsByBolumIds(List<int> bolumIds)
        {
            var lokasyonList = (await _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(x => bolumIds.Contains(x.Id) && x.Silindi != true)).Select(x => x.LokasyonId).ToList();
            return lokasyonList;
        }

        public async Task EditBolum(BolumViewModel bolumViewModel, string userId)
        {
            //Eğer yetkisi varsa kullanıcının onaya düşmeden güncelleme işlemi yapılır. Aksi halde:
            //Kullanıcı bir Bölüm adını değiştirmek istediğinde bu değişiklik Actions tablosuna kaydedilir, fakat Bölüm tablosu hemen güncellenmez.
            //Yetkili kullanıcı, düzenleme işlemini onayladığında Bölüm tablosu yeni bilgilerle güncellenir.
            //Yetkili kullanıcı işlemi reddettiğinde, asıl tabloda hiçbir değişiklik yapılmaz ve işlem "reddedildi" olarak işaretlenir.

            //TODO if (IUserMakineYetkiRepository varsa guncelleme yapılır)
            //{_repo.Update(model);
            //await _repo.SaveChangesAsync();}
            //else
            var model = _mapper.Map<Bolum>(bolumViewModel);
            if (model.OnayIslemdeMi.GetValueOrDefault(false))
            {
                var onayIslemKayitSilmeVarMi = await _onayIslemKayitRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.IslemTipi == PanoKimlikOnayIslemTipi.Silme && x.OgeTipi == TreeViewNodeTypes.Bolum && x.OgeId == model.Id);
                if (onayIslemKayitSilmeVarMi != null)
                    return;
            }
            var onayIslemKayit = new OnayIslemKayit();
            onayIslemKayit.UserId = userId;
            onayIslemKayit.OgeTipi = TreeViewNodeTypes.Bolum;
            onayIslemKayit.OgeId = model.Id;
            onayIslemKayit.IslemTipi = PanoKimlikOnayIslemTipi.Duzenleme;
            onayIslemKayit.IslemVerisi = JsonSerializer.Serialize(model);
            onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Beklemede;
            onayIslemKayit.KayitZamani = DateTime.Now;
            onayIslemKayit.LokasyonId = bolumViewModel.LokasyonId;

            await _onayIslemKayitRepository.AddAsync(onayIslemKayit);
            await _onayIslemKayitRepository.SaveChangesAsync();
        }

        public async Task SetSilindi(int id, string userId)
        {
            try
            {
                var bolum = await _bolumRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
                if (bolum.OnayIslemdeMi.GetValueOrDefault(false))
                {
                    var onayIslemKayitSilmeVarMi = await _onayIslemKayitRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Durum == PanoKimlikOnayIslemDurum.Beklemede && x.IslemTipi == PanoKimlikOnayIslemTipi.Silme && x.OgeTipi == TreeViewNodeTypes.Bolum && x.OgeId == bolum.Id);
                    if (onayIslemKayitSilmeVarMi != null)
                        return;
                }
                var lokasyonId = await _bolumRepository.FindLokasyonIdByBolumId(id);
                var onayIslemKayit = new OnayIslemKayit();
                onayIslemKayit.UserId = userId;
                onayIslemKayit.OgeTipi = TreeViewNodeTypes.Bolum;
                onayIslemKayit.OgeId = id;
                onayIslemKayit.IslemTipi = PanoKimlikOnayIslemTipi.Silme;
                onayIslemKayit.IslemVerisi = JsonSerializer.Serialize(bolum);
                onayIslemKayit.Durum = PanoKimlikOnayIslemDurum.Beklemede;
                onayIslemKayit.KayitZamani = DateTime.Now;
                onayIslemKayit.LokasyonId = lokasyonId;

                await _onayIslemKayitRepository.AddAsync(onayIslemKayit);
                await _onayIslemKayitRepository.SaveChangesAsync();

                bolum.Silindi = true;
                await _bolumRepository.SaveChangesAsync();
            }
            catch (Exception)
            {
            }
        }

        public async Task<List<Bolum>> GetAllBolumForYetki()
        {
            var listBolum = new List<Bolum>();

            var bolumler = await _bolumRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Silindi != true && x.OnayIslemdeMi != true);
            listBolum.AddRange(bolumler);

            return listBolum;
        }
    }
}
