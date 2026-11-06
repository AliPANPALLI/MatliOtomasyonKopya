
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using KantarTakip.Application.DTOs;
using KantarTakip.Application.Interfaces;
using KantarTakip.Application.ViewModels;
using KantarTakip.Domain.Exceptions;
using KantarTakip.Domain.Helpers;
using KantarTakip.Domain.Interfaces;
using KantarTakip.Domain.Models;

using MapsterMapper;

namespace KantarTakip.Application.Services
{
    // Kalite kilidi kontrolleri repository'de, transaction ve
    // satır kilidi altında yapılır; burada tekrarlanmaz.
    public class KantarTakipAppService : IKantarTakipAppService
    {
        private readonly IMapper _mapper;
        private readonly IKantarTakipRepository _kantarTakipRepository;

        public KantarTakipAppService(
            IMapper mapper,
            IKantarTakipRepository kantarTakipRepository)
        {
            _mapper = mapper;
            _kantarTakipRepository = kantarTakipRepository;
        }

        #region Firma

        public async Task<List<FirmaDto>> GetAllFirma()
        {
            var list = await _kantarTakipRepository.GetAllFirma();
            return _mapper.Map<List<FirmaDto>>(list);
        }

        public async Task<FirmaDto?> GetFirmaById(int id)
        {
            var model = await _kantarTakipRepository.GetFirmaById(id);
            return model == null ? null : _mapper.Map<FirmaDto>(model);
        }

        public async Task AddFirma(FirmaViewModel firmaViewModel)
        {
            var model = _mapper.Map<Firma>(firmaViewModel);

            model.Id = 0;
            model.Silindi = false;

            await _kantarTakipRepository.AddFirma(model);
        }

        public async Task EditFirma(FirmaViewModel firmaViewModel)
        {
            var model = await _kantarTakipRepository
                .GetFirmaById(firmaViewModel.Id);

            if (model == null)
                throw new KayitBulunamadiException("Firma bulunamadı.");

            model.Name = firmaViewModel.Name;

            await _kantarTakipRepository.UpdateFirma(model);
        }

        public async Task DeleteFirma(int id)
        {
            await _kantarTakipRepository.DeleteFirma(id);
        }

        #endregion

        #region KaliteKontrolPersonel

        public async Task<List<KaliteKontrolPersonelDto>> GetAllKaliteKontrolPersonel()
        {
            var list = await _kantarTakipRepository.GetAllKaliteKontrolPersonel();
            return _mapper.Map<List<KaliteKontrolPersonelDto>>(list);
        }

        public async Task<KaliteKontrolPersonelDto?> GetKaliteKontrolPersonelById(int id)
        {
            var model = await _kantarTakipRepository.GetKaliteKontrolPersonelById(id);
            return model == null ? null : _mapper.Map<KaliteKontrolPersonelDto>(model);
        }

        public async Task AddKaliteKontrolPersonel(
            KaliteKontrolPersonelViewModel personelViewModel)
        {
            var adSoyad = PersonelAdSoyadKontrolEt(personelViewModel.AdSoyad);

            var model = _mapper.Map<KaliteKontrolPersonel>(personelViewModel);

            model.Id = 0;
            model.AdSoyad = adSoyad;
            model.Silindi = false;

            await _kantarTakipRepository.AddKaliteKontrolPersonel(model);
        }

        public async Task EditKaliteKontrolPersonel(
            KaliteKontrolPersonelViewModel personelViewModel)
        {
            var adSoyad = PersonelAdSoyadKontrolEt(personelViewModel.AdSoyad);

            var model = await _kantarTakipRepository
                .GetKaliteKontrolPersonelById(personelViewModel.Id);

            if (model == null)
                throw new KayitBulunamadiException("Kalite kontrol personeli bulunamadı.");

            model.AdSoyad = adSoyad;

            await _kantarTakipRepository.UpdateKaliteKontrolPersonel(model);
        }

        public async Task DeleteKaliteKontrolPersonel(int id)
        {
            await _kantarTakipRepository.DeleteKaliteKontrolPersonel(id);
        }

        #endregion

        #region KantarGuvenlik

        public async Task<List<KantarGuvenlikDto>> GetAllKantarGuvenlik(
            DateOnly? startDate = null,
            DateOnly? endDate = null)
        {
            var (begin, endExclusive) = ToRange(startDate, endDate);

            var list = await _kantarTakipRepository
                .GetAllKantarGuvenlik(begin, endExclusive);

            return _mapper.Map<List<KantarGuvenlikDto>>(list);
        }

        public async Task<KantarGuvenlikDto?> GetKantarGuvenlikById(
            int id)
        {
            var model = await _kantarTakipRepository
                .GetKantarGuvenlikById(id);

            return model == null
                ? null
                : _mapper.Map<KantarGuvenlikDto>(model);
        }

        public async Task<List<KantarGuvenlikDto>> GetKantarGuvenlikByPlaka(
            string plaka)
        {
            var list = await _kantarTakipRepository
                .GetKantarGuvenlikByPlaka(plaka);

            return _mapper.Map<List<KantarGuvenlikDto>>(list);
        }

        public async Task<ZiyaretTakipDto?> GetByZiyaretciKayitId(
            string ziyaretciKayitId)
        {
            if (string.IsNullOrWhiteSpace(ziyaretciKayitId))
            {
                throw new GecersizIstekException(
                    "Ziyaretçi kayıt ID'si giriniz.");
            }

            var guvenlik = await _kantarTakipRepository
                .GetKantarGuvenlikByZiyaretciKayitId(ziyaretciKayitId);

            if (guvenlik == null)
                return null;

            var kaliteKayitlari = await _kantarTakipRepository
                .GetKantarKaliteByKantarGuvenlikId(guvenlik.Id);

            return new ZiyaretTakipDto
            {
                Guvenlik = _mapper.Map<KantarGuvenlikDto>(guvenlik),
                KaliteKayitlari = _mapper.Map<List<KantarKaliteDto>>(kaliteKayitlari)
            };
        }

        public async Task AddKantarGuvenlik(
            KantarGuvenlikViewModel kantarGuvenlikViewModel,
            string userId)
        {
            GenelKontrolZorunluAlanlariKontrolEt(kantarGuvenlikViewModel);

            await FirmaGecerliMiKontrolEt(kantarGuvenlikViewModel.FirmaId);

            var model = _mapper.Map<KantarGuvenlik>(
                kantarGuvenlikViewModel);

            model.Id = 0;
            model.Tarih = kantarGuvenlikViewModel.Tarih!.Value.Date;
            model.ZiyaretciKayitId = TrimOrNull(kantarGuvenlikViewModel.ZiyaretciKayitId);
            model.KayitZamani = DateTime.Now;
            model.GuncelleyenUserId = userId;
            model.Silindi = false;

            await _kantarTakipRepository
                .AddKantarGuvenlik(model);
        }

        public async Task EditKantarGuvenlik(
            KantarGuvenlikViewModel kantarGuvenlikViewModel,
            string userId)
        {
            GenelKontrolZorunluAlanlariKontrolEt(kantarGuvenlikViewModel);

            await FirmaGecerliMiKontrolEt(kantarGuvenlikViewModel.FirmaId);

            // KayitZamani ve kalite tamamlama alanları
            // repository tarafından korunur.
            await _kantarTakipRepository.UpdateKantarGuvenlik(
                new KantarGuvenlik
                {
                    Id = kantarGuvenlikViewModel.Id,
                    ZiyaretciKayitId = TrimOrNull(kantarGuvenlikViewModel.ZiyaretciKayitId),
                    Plaka = kantarGuvenlikViewModel.Plaka,
                    Tarih = kantarGuvenlikViewModel.Tarih!.Value.Date,
                    UrunAdi = kantarGuvenlikViewModel.UrunAdi,
                    FirmaId = kantarGuvenlikViewModel.FirmaId,
                    IrsaliyeNo = kantarGuvenlikViewModel.IrsaliyeNo,
                    SonUrun = kantarGuvenlikViewModel.SonUrun,
                    GuncelleyenUserId = userId
                });
        }

        public async Task DeleteKantarGuvenlik(int id)
        {
            await _kantarTakipRepository
                .DeleteKantarGuvenlik(id);
        }

        #endregion

        #region KantarKalite

        public async Task<List<KantarKaliteDto>> GetAllKantarKalite(
            DateOnly? startDate = null,
            DateOnly? endDate = null)
        {
            var (begin, endExclusive) = ToRange(startDate, endDate);

            var list = await _kantarTakipRepository
                .GetAllKantarKalite(begin, endExclusive);

            return _mapper.Map<List<KantarKaliteDto>>(list);
        }

        public async Task<KantarKaliteDto?> GetKantarKaliteById(int id)
        {
            var model = await _kantarTakipRepository
                .GetKantarKaliteById(id);

            return model == null
                ? null
                : _mapper.Map<KantarKaliteDto>(model);
        }

        public async Task<List<KantarKaliteDto>> GetKantarKaliteByKantarGuvenlikId(
            int kantarGuvenlikId)
        {
            var list = await _kantarTakipRepository
                .GetKantarKaliteByKantarGuvenlikId(
                    kantarGuvenlikId);

            return _mapper.Map<List<KantarKaliteDto>>(list);
        }

        // Yeni kalite kaydı ekleme
        public async Task AddKantarKalite(
            KantarKaliteViewModel kantarKaliteViewModel,
            string userId)
        {
            KaliteZorunluAlanlariKontrolEt(kantarKaliteViewModel);

            await PersonelGecerliMiKontrolEt(kantarKaliteViewModel.KaliteKontrolPersonelId);

            var model = _mapper.Map<KantarKalite>(
                kantarKaliteViewModel);

            model.Id = 0;
            model.KayitZamani = DateTime.Now;
            model.GuncelleyenUserId = userId;
            model.Silindi = false;

            await _kantarTakipRepository.AddKantarKalite(model);
        }

        // Mevcut kalite kaydını güncelleme
        public async Task EditKantarKalite(
            KantarKaliteViewModel kantarKaliteViewModel,
            string userId)
        {
            KaliteZorunluAlanlariKontrolEt(kantarKaliteViewModel);

            await PersonelGecerliMiKontrolEt(kantarKaliteViewModel.KaliteKontrolPersonelId);

            var model = _mapper.Map<KantarKalite>(
                kantarKaliteViewModel);

            // KayitZamani korunur.
            // Kalite reaksiyon KPI'ı değişmemeli.
            model.GuncelleyenUserId = userId;

            await _kantarTakipRepository
                .UpdateKantarKalite(model);
        }

        // Kalite kaydını silme
        public async Task DeleteKantarKalite(int id)
        {
            await _kantarTakipRepository
                .DeleteKantarKalite(id);
        }

        #endregion

        #region Bosaltim

        public async Task BosaltimKaydet(
            BosaltimViewModel bosaltimViewModel,
            string userId)
        {
            if (bosaltimViewModel.KantarGuvenlikId <= 0)
            {
                throw new GecersizIstekException(
                    "Geçerli bir güvenlik ID girilmelidir.");
            }

            var dokulduguYer = TrimOrNull(bosaltimViewModel.DokulduguYer);

            if (dokulduguYer == null)
            {
                throw new GecersizIstekException(
                    "Döküldüğü yer zorunludur.");
            }

            if (dokulduguYer.Length > 200)
            {
                throw new GecersizIstekException(
                    "Döküldüğü yer en fazla 200 karakter olabilir.");
            }

            await _kantarTakipRepository.BosaltimKaydet(
                bosaltimViewModel.KantarGuvenlikId,
                dokulduguYer,
                userId);
        }

        #endregion

        #region Kalite Surec Tamamlama

        // Kullanıcı "Kalite Kontrolünü Tamamla" dediğinde çalışır.
        public async Task KaliteTamamlaAsync(
            int kantarGuvenlikId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (kantarGuvenlikId <= 0)
            {
                throw new GecersizIstekException(
                    "Geçerli bir güvenlik ID girilmelidir.");
            }

            await _kantarTakipRepository.KaliteTamamlaAsync(
                kantarGuvenlikId,
                userId,
                cancellationToken);
        }

        #endregion

        #region Helpers

        // Genel kontrollerde zorunlu: ZiyaretciKayitId, Plaka, Tarih.
        private static void GenelKontrolZorunluAlanlariKontrolEt(
            KantarGuvenlikViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.ZiyaretciKayitId))
            {
                throw new GecersizIstekException(
                    "Ziyaretçi kayıt ID'si zorunludur.");
            }

            if (PlakaHelper.Normalize(vm.Plaka).Length == 0)
            {
                throw new GecersizIstekException(
                    "Araç plakası zorunludur.");
            }

            if (!vm.Tarih.HasValue || vm.Tarih.Value == default)
            {
                throw new GecersizIstekException(
                    "Tarih zorunludur.");
            }

            if (vm.ZiyaretciKayitId?.Trim().Length > 50)
            {
                throw new GecersizIstekException(
                    "Ziyaretçi kayıt ID'si en fazla 50 karakter olabilir.");
            }
        }

        // Kalite kontrolde yalnızca Nem zorunlu.
        private static void KaliteZorunluAlanlariKontrolEt(
            KantarKaliteViewModel vm)
        {
            if (!vm.KantarGuvenlikId.HasValue)
            {
                throw new GecersizIstekException(
                    "Kalite kaydı için güvenlik ID zorunludur.");
            }

            if (!vm.Nem.HasValue)
            {
                throw new GecersizIstekException(
                    "Nem değeri zorunludur.");
            }

            if (vm.Sonuc.HasValue && !Enum.IsDefined(vm.Sonuc.Value))
            {
                throw new GecersizIstekException(
                    "Sonuç 1 (Kabul), 2 (Şartlı Kabul) veya 3 (Red) olmalıdır.");
            }
        }

        private static string? TrimOrNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private async Task FirmaGecerliMiKontrolEt(int? firmaId)
        {
            if (!firmaId.HasValue)
                return;

            var firma = await _kantarTakipRepository
                .GetFirmaById(firmaId.Value);

            if (firma == null)
            {
                throw new GecersizIstekException(
                    "Seçilen firma bulunamadı.");
            }
        }

        private async Task PersonelGecerliMiKontrolEt(int? personelId)
        {
            if (!personelId.HasValue)
                return;

            var personel = await _kantarTakipRepository
                .GetKaliteKontrolPersonelById(personelId.Value);

            if (personel == null)
            {
                throw new GecersizIstekException(
                    "Seçilen kalite kontrol personeli bulunamadı.");
            }
        }

        private static string PersonelAdSoyadKontrolEt(string? adSoyad)
        {
            var deger = TrimOrNull(adSoyad);

            if (deger == null)
            {
                throw new GecersizIstekException(
                    "Personel adı soyadı zorunludur.");
            }

            if (deger.Length > 200)
            {
                throw new GecersizIstekException(
                    "Personel adı soyadı en fazla 200 karakter olabilir.");
            }

            return deger;
        }

        private static (DateTime? Begin, DateTime? EndExclusive) ToRange(
            DateOnly? startDate,
            DateOnly? endDate)
        {
            if (startDate.HasValue &&
                endDate.HasValue &&
                endDate.Value < startDate.Value)
            {
                throw new GecersizIstekException(
                    "Bitiş tarihi başlangıçtan önce olamaz.");
            }

            return (
                startDate?.ToDateTime(TimeOnly.MinValue),
                endDate?.AddDays(1).ToDateTime(TimeOnly.MinValue));
        }

        #endregion
    }
}
