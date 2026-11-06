using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace EnergyMonitoring.Domain.Models
{
    public class CihazMongo
    {
        [BsonId]
        [BsonRepresentation(BsonType.Int32)]
        public int Id { get; set; }
        public string? Name { get; set; }
        //Sirket-Lokasyon-Bolum-Makine
        public int? MakineId { get; set; }
        public DateTime? KayitZamani { get; set; }
        public string? Marka { get; set; }
        public string? Model { get; set; }
        public int? DeviceModel { get; set; }
        public bool? Aktif { get; set; }
        public bool? PlcAutoConnect { get; set; }
        public string? PlcIpAdres { get; set; }
        public int? PlcPort { get; set; }
        public byte? SlaveId { get; set; }
        public bool? Silindi { get; set; }
    }
}