using HegSogutmaScada.Domain.DbViewModels;
using HegSogutmaScada.Domain.Models;
using HegSogutmaScada.Domain.Models.ReportModels;

namespace HegSogutmaScada.Domain.Interfaces
{
    public interface IHegSogutmaRepository
    {
        Task<List<Oda>> GetAllOdalarAsync();
        Task<List<OdaVanasi>> GetAllVanalarAsync();
        Task<List<Kompresor>> GetAllKompresorlerAsync();
        Task<List<VanaAnlik>> GetAllVanaAnlikAsync();
        Task<List<KompresorAnlik>> GetAllKompresorAnlikAsync();

        // Vana seçim listesi için oda_vanalari/odalar/vana_anlik tabloları DB
        // tarafında tek bir LEFT JOIN sorgusuyla birleştirilir. Amaç, uzak/latency'li
        // DB bağlantısında 3 ayrı round-trip yerine tek round-trip'e inmek (bkz.
        // HegSogutmaRaporAppService.GetVanalarAsync).
        Task<List<VanaViewModel>> GetVanalarWithDurumAsync();

        // Bir vananın verilen tarih aralığında kaç saniye açık kaldığını, DB'deki
        // vana_acik_saniye(vana_id, from, to) fonksiyonunu çağırarak anlık hesaplar.
        // Fonksiyon veri bulunamadığında NULL dönebiliyor.
        Task<decimal?> GetVanaAcikSaniyeAsync(short vanaId, DateTime baslangic, DateTime bitis);

        Task<List<KompresorOkumaSaatlik>> GetKompresorOkumaSaatlikAsync(DateTime baslangic, DateTime bitis, IReadOnlyCollection<short>? kompresorIds = null);
        Task<List<KompresorOkumaGunluk>> GetKompresorOkumaGunlukAsync(DateTime baslangic, DateTime bitis, IReadOnlyCollection<short>? kompresorIds = null);
        Task<List<KompresorOkumaAylik>> GetKompresorOkumaAylikAsync(DateTime baslangic, DateTime bitis, IReadOnlyCollection<short>? kompresorIds = null);
    }
}
