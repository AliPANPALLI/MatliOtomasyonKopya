using System.Text.Json.Serialization;

namespace EnerjiIzleme.Domain.Models
{
    public class Cihaz
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        //Sirket-Lokasyon-Bolum-Makine
        public int MakineId { get; set; }
        public int ParentCihazId { get; set; }
        public DateTime? KayitZamani { get; set; }
        //public string? Marka { get; set; }
        //public string? Model { get; set; }
        public int GatewayId { get; set; }
        public int ModelId { get; set; }
        //public bool? PlcAutoConnect { get; set; }
        //public string? PlcIpAdres { get; set; }
        //public int? PlcPort { get; set; }
        public int SlaveId { get; set; }
        public bool? LokasyonEnerjiHesabindaKullan { get; set; }
        public bool Aktif { get; set; }
        public bool? GES { get; set; }
        public bool? Silindi { get; set; }
        public DateTime? SilinmeZamani { get; set; }
        public bool? Onaylandi { get; set; }
        public DateTime? OnaylanmaZamani { get; set; }
        public string? OnaylayanUserId { get; set; }
        public DateTime? EklenmeZamani { get; set; }
        public string? EkleyenUserId { get; set; }
        public DateTime? GuncellemeZamani { get; set; }
        public string? GuncelleyenUserId { get; set; }
        [JsonIgnore] public virtual Makine Makine { get; set; }
        [JsonIgnore] public virtual Gateway Gateway { get; set; }
        [JsonIgnore] public virtual CihazModel CihazModel { get; set; }
    }
}