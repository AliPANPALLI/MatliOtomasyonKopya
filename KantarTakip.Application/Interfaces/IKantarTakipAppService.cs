
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using KantarTakip.Application.DTOs;
using KantarTakip.Application.ViewModels;

namespace KantarTakip.Application.Interfaces
{
    // Kayıt bulunamadığında KayitBulunamadiException,
    // iş kuralı ihlalinde IsKuraliException,
    // geçersiz girdide GecersizIstekException fırlatılır.
    public interface IKantarTakipAppService
    {
        #region Firma

        Task<List<FirmaDto>> GetAllFirma();

        Task<FirmaDto?> GetFirmaById(int id);

        Task AddFirma(FirmaViewModel firmaViewModel);

        Task EditFirma(FirmaViewModel firmaViewModel);

        Task DeleteFirma(int id);

        #endregion

        #region KaliteKontrolPersonel

        Task<List<KaliteKontrolPersonelDto>> GetAllKaliteKontrolPersonel();

        Task<KaliteKontrolPersonelDto?> GetKaliteKontrolPersonelById(int id);

        Task AddKaliteKontrolPersonel(KaliteKontrolPersonelViewModel personelViewModel);

        Task EditKaliteKontrolPersonel(KaliteKontrolPersonelViewModel personelViewModel);

        Task DeleteKaliteKontrolPersonel(int id);

        #endregion

        #region KantarGuvenlik

        Task<List<KantarGuvenlikDto>> GetAllKantarGuvenlik(
            DateOnly? startDate = null,
            DateOnly? endDate = null);

        Task<KantarGuvenlikDto?> GetKantarGuvenlikById(int id);

        Task<List<KantarGuvenlikDto>> GetKantarGuvenlikByPlaka(
            string plaka);

        Task<ZiyaretTakipDto?> GetByZiyaretciKayitId(
            string ziyaretciKayitId);

        Task AddKantarGuvenlik(
            KantarGuvenlikViewModel kantarGuvenlikViewModel,
            string userId);

        Task EditKantarGuvenlik(
            KantarGuvenlikViewModel kantarGuvenlikViewModel,
            string userId);

        Task DeleteKantarGuvenlik(int id);

        #endregion

        #region KantarKalite

        Task<List<KantarKaliteDto>> GetAllKantarKalite(
            DateOnly? startDate = null,
            DateOnly? endDate = null);

        Task<KantarKaliteDto?> GetKantarKaliteById(int id);

        Task<List<KantarKaliteDto>> GetKantarKaliteByKantarGuvenlikId(
            int kantarGuvenlikId);

        Task AddKantarKalite(
            KantarKaliteViewModel kantarKaliteViewModel,
            string userId);

        Task EditKantarKalite(
            KantarKaliteViewModel kantarKaliteViewModel,
            string userId);

        Task DeleteKantarKalite(int id);

        #endregion

        #region Bosaltim

        /// <summary>
        /// Kalite kararı Kabul / Şartlı Kabul olan aracın
        /// döküldüğü yeri kaydeder.
        /// </summary>
        Task BosaltimKaydet(
            BosaltimViewModel bosaltimViewModel,
            string userId);

        #endregion

        #region Kalite Surec Tamamlama

        /// <summary>
        /// İlgili güvenlik ziyaretinin kalite kontrolünü
        /// tamamlar ve kalite veri girişini kilitler.
        /// </summary>
        Task KaliteTamamlaAsync(
            int kantarGuvenlikId,
            string userId,
            CancellationToken cancellationToken = default);

        #endregion
    }
}
