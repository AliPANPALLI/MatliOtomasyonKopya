using KumesScada.Application.Interfaces;
using KumesScada.Application.RequestModels;
using KumesScada.Application.ViewModels;
using KumesScada.Domain.Interfaces;
using KumesScada.Domain.Models;
using MapsterMapper;
using PanoKimliklendirme.Application.Services.Core;
using static SharedKernel.ApplicationConstants;

namespace KumesScada.Application.Services
{
    public class KumesRaporRehberAppService : AppService<KumesRaporRehber, KumesRaporRehberViewModel>, IKumesRaporRehberAppService
    {
        private readonly IKumesRaporRehberRepository _kumesRaporRehberRepository;

        public KumesRaporRehberAppService(IMapper mapper, IKumesRaporRehberRepository kumesRaporRehberRepository) : base(mapper, kumesRaporRehberRepository)
        {
            _kumesRaporRehberRepository = kumesRaporRehberRepository;
        }

        public async Task<List<KumesRaporRehberViewModel>> GetByLokasyonId(int lokasyonId)
        {
            if (lokasyonId <= 0)
                return new List<KumesRaporRehberViewModel>();

            var guides = await _kumesRaporRehberRepository.GetByLokasyonId(lokasyonId);

            return guides.Select(static x => new KumesRaporRehberViewModel
            {
                Id = x.Id,
                LokasyonId = x.LokasyonId,
                RehberAdi = x.RehberAdi,
                HaftaSayisi = x.HaftaSayisi,
                Deger = x.Deger
            }).ToList();
        }

        public async Task<List<KumesRaporRehberViewModel>> GetByLokasyonIdAndGuideTipi(int lokasyonId, string guideTipi, int hayvanCinsId)
        {
            if (lokasyonId <= 0)
                return new List<KumesRaporRehberViewModel>();

            if (string.IsNullOrWhiteSpace(guideTipi))
                return new List<KumesRaporRehberViewModel>();

            var guides = await _kumesRaporRehberRepository.GetKumesRaporRehberByLokasyonIdAndGuideTipi(lokasyonId, guideTipi, hayvanCinsId);

            return guides.Select(x => new KumesRaporRehberViewModel
            {
                Id = x.Id,
                LokasyonId = x.LokasyonId,
                RehberAdi = x.RehberAdi,
                HaftaSayisi = x.HaftaSayisi,
                Deger = x.Deger
            }).ToList();
        }

        public async Task SaveGuides(KumesRapoRehberSaveRequest request, string userId)
        {
            if (request == null)
                throw new Exception("Kayıt isteği boş olamaz.");

            if (request.LokasyonId <= 0)
                throw new Exception("Lokasyon bilgisi zorunludur.");

            if (string.IsNullOrWhiteSpace(request.RehberAdi))
                throw new Exception("Hedef adı zorunludur.");

            request.Guides ??= new List<KumesRaporRehberViewModel>();

            ValidateGuides(request.Guides);

            var aktifGuides = await _kumesRaporRehberRepository.GetKumesRaporRehberByLokasyonIdAndGuideTipi(request.LokasyonId, request.RehberAdi, request.HayvanCinsId.GetValueOrDefault(0));

            var gelenHaftalar = request.Guides.Select(x => x.HaftaSayisi).Distinct().ToList();

            foreach (var item in request.Guides)
            {
                var mevcut = aktifGuides.FirstOrDefault(x =>
                    x.HaftaSayisi == item.HaftaSayisi
                );

                if (mevcut == null)
                {
                    var yeni = new KumesRaporRehber
                    {
                        LokasyonId = request.LokasyonId,
                        RehberAdi = request.RehberAdi,
                        HaftaSayisi = item.HaftaSayisi,
                        Deger = item.Deger,
                        HayvanCinsId = request.HayvanCinsId,
                        KayitZamani = DateTime.Now,
                        KaydedenUserId = userId,
                        Silindi = false
                    };

                    await _kumesRaporRehberRepository.AddAsync(yeni);
                }
                else
                {
                    mevcut.Deger = item.Deger;
                    mevcut.GuncellemeZamani = DateTime.Now;
                    mevcut.GuncelleyenUserId = userId;

                    await _kumesRaporRehberRepository.UpdateAsync(mevcut);
                }
            }

            // Grid’den kaldırılan haftaları soft delete yapar.
            var silinecekler = aktifGuides
                .Where(x => !gelenHaftalar.Contains(x.HaftaSayisi))
                .ToList();

            foreach (var item in silinecekler)
            {
                item.Silindi = true;
                item.GuncellemeZamani = DateTime.Now;
                item.GuncelleyenUserId = userId;

                await _kumesRaporRehberRepository.UpdateAsync(item);
            }

            await _kumesRaporRehberRepository.SaveChangesAsync();
        }

        public async Task DeleteGuide(int id, string userId)
        {
            var guide = await _kumesRaporRehberRepository.GetByIdAsync(id);

            if (guide == null)
                throw new Exception("Hedef kaydı bulunamadı.");

            guide.Silindi = true;
            guide.GuncellemeZamani = DateTime.Now;
            guide.GuncelleyenUserId = userId;

            await _kumesRaporRehberRepository.UpdateAsync(guide);
            await _kumesRaporRehberRepository.SaveChangesAsync();
        }

        public List<string> GetGuideTipleri()
        {
            return KumesRaporRehberTipleri.GetAll();
        }
        private void ValidateGuides(List<KumesRaporRehberViewModel> guides)
        {
            var haftaSet = new HashSet<int>();

            foreach (var item in guides)
            {
                if (item.HaftaSayisi <= 0)
                    throw new Exception("Hafta sayısı 0'dan büyük olmalıdır.");

                if (item.Deger < 0)
                    throw new Exception("Hedef değeri negatif olamaz.");

                if (haftaSet.Contains(item.HaftaSayisi))
                    throw new Exception($"{item.HaftaSayisi}. hafta birden fazla kez girilmiş.");

                haftaSet.Add(item.HaftaSayisi);
            }
        }
    }
}