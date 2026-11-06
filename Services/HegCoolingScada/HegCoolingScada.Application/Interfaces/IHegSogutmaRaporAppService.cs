using HegCoolingScada.Domain.Models;
using HegCoolingScada.Domain.Models.ReportModels;

namespace HegCoolingScada.Application.Interfaces
{
    public interface IHegSogutmaRaporAppService
    {
        Task<List<Oda>> GetOdalarAsync();
        Task<List<Kompresor>> GetKompresorlerAsync();
        Task<List<VanaViewModel>> GetVanalarAsync();

        // periyot: "saatlik" | "gunluk" | "aylik"
        Task<List<VanaDurumRaporSatiri>> GetVanaDurumRaporuAsync(DateTime baslangic, DateTime bitis, string periyot, IReadOnlyCollection<short>? vanaIds = null);

        // periyot: "saatlik" | "gunluk" | "aylik" | "ozet"
        // "ozet": seçilen aralığı saat saat değil, kompresör başına tek satırda özetler.
        Task<List<KompresorRaporSatiri>> GetKompresorRaporuAsync(DateTime baslangic, DateTime bitis, string periyot, IReadOnlyCollection<short>? kompresorIds = null);
    }
}
