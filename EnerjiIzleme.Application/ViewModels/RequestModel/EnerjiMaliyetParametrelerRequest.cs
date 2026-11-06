using EnerjiIzleme.Domain.Models;

namespace EnerjiIzleme.Application.ViewModels.RequestModel
{
    public class EnerjiMaliyetParametrelerRequest
    {
        public List<EnerjiMaliyetParametrelerViewModel> EnerjiMaliyetParametreler { get; set; }
        public int SeciliYil { get; set; }
        public int LokasyonId { get; set; }
        public string TarifeTipi { get; set; }
    }

    public class EnerjiMaliyetParametreKaydetModel
    {
        public int SeciliYil { get; set; }
        public int EnerjiMaliyetGrupId { get; set; }
        public string TarifeTipi { get; set; }
        public List<EnerjiMaliyetParametreler> EnerjiMaliyetParametreler { get; set; }
    }
}
