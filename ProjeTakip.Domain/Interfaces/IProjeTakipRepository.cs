using ProjeTakip.Domain.Entities;

namespace ProjeTakip.Domain.Interfaces;

public interface IProjeTakipRepository
{
    Task<List<Sirket>> SirketleriGetirAsync(CancellationToken cancellationToken);
    Task<Sirket?> SirketGetirAsync(int id, CancellationToken cancellationToken);
    Task<List<Tesis>> TesisleriGetirAsync(CancellationToken cancellationToken);
    Task<Tesis?> TesisGetirAsync(int id, CancellationToken cancellationToken);
    Task<List<AdimBasligi>> BasliklariGetirAsync(CancellationToken cancellationToken);
    Task<AdimBasligi?> BaslikGetirAsync(int id, CancellationToken cancellationToken);
    Task<List<Sorumlu>> SorumlulariGetirAsync(CancellationToken cancellationToken);
    Task<Sorumlu?> SorumluGetirAsync(string adSoyad, CancellationToken cancellationToken);
    Task<List<IsAdimi>> IsAdimlariniGetirAsync(CancellationToken cancellationToken);
    Task<IsAdimi?> IsAdimiGetirAsync(int id, CancellationToken cancellationToken);
    Task<List<ProjeDokumani>> DokumanlariGetirAsync(CancellationToken cancellationToken);
    Task<ProjeDokumani?> DokumanGetirAsync(int id, bool icerikle, CancellationToken cancellationToken);
    Task<string> SiradakiIsNoAsync(CancellationToken cancellationToken);
    void Ekle<T>(T entity) where T : class;
    void Sil<T>(T entity) where T : class;
    /// <summary>
    /// Formun yüklendiği andaki RowVersion'ı entity'nin "orijinal" (DB'den okunmuş
    /// varsayılan) değeri olarak işaretler. SaveChanges bu değeri WHERE koşuluna
    /// koyar; araya başka bir güncelleme girmişse 0 satır etkilenir ve
    /// DbUpdateConcurrencyException fırlatılır (409'a çevrilir).
    /// </summary>
    void SetOriginalRowVersion<T>(T entity, byte[] rowVersion) where T : class;
    Task<int> KaydetAsync(CancellationToken cancellationToken);
}
