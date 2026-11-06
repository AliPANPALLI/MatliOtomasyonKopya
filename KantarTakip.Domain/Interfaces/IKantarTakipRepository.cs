using KantarTakip.Domain.Models;

namespace KantarTakip.Domain.Interfaces
{
    public interface IKantarTakipRepository
    {
        // Firma
        Task<List<Firma>> GetAllFirma();
        Task<Firma?> GetFirmaById(int id);
        Task AddFirma(Firma firma);
        Task UpdateFirma(Firma firma);
        Task DeleteFirma(int id);

        // KaliteKontrolPersonel
        Task<List<KaliteKontrolPersonel>> GetAllKaliteKontrolPersonel();
        Task<KaliteKontrolPersonel?> GetKaliteKontrolPersonelById(int id);
        Task AddKaliteKontrolPersonel(KaliteKontrolPersonel personel);
        Task UpdateKaliteKontrolPersonel(KaliteKontrolPersonel personel);
        Task DeleteKaliteKontrolPersonel(int id);

        // KantarGuvenlik
        Task<List<KantarGuvenlik>> GetAllKantarGuvenlik(
            DateTime? begin = null,
            DateTime? endExclusive = null);
        Task<KantarGuvenlik?> GetKantarGuvenlikById(int id);
        Task<List<KantarGuvenlik>> GetKantarGuvenlikByPlaka(string plaka);
        Task<KantarGuvenlik?> GetKantarGuvenlikByZiyaretciKayitId(string ziyaretciKayitId);
        Task<List<KantarGuvenlik>> GetKantarGuvenlikByKayitAraligi(
            DateTime begin,
            DateTime endExclusive,
            CancellationToken cancellationToken = default);
        Task AddKantarGuvenlik(KantarGuvenlik kantarGuvenlik);
        Task UpdateKantarGuvenlik(KantarGuvenlik kantarGuvenlik);
        Task DeleteKantarGuvenlik(int id);
        Task BosaltimKaydet(
            int kantarGuvenlikId,
            string dokulduguYer,
            string userId);
        Task KaliteTamamlaAsync(
            int kantarGuvenlikId,
            string userId,
            CancellationToken cancellationToken = default);

        // KantarKalite
        Task<List<KantarKalite>> GetAllKantarKalite(
            DateTime? begin = null,
            DateTime? endExclusive = null);
        Task<KantarKalite?> GetKantarKaliteById(int id);
        Task<List<KantarKalite>> GetKantarKaliteByKantarGuvenlikId(int kantarGuvenlikId);
        Task<List<KantarKalite>> GetKantarKaliteByKantarGuvenlikIds(
            IReadOnlyCollection<int> kantarGuvenlikIds,
            CancellationToken cancellationToken = default);
        Task AddKantarKalite(KantarKalite kantarKalite);
        Task UpdateKantarKalite(KantarKalite kantarKalite);
        Task DeleteKantarKalite(int id);
    }
}
