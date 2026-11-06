using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace EnerjiIzleme.Domain.Models
{
    public class MakineMongo
    {
        [BsonId]
        [BsonRepresentation(BsonType.Int32)]
        public int Id { get; set; }
        public string Name { get; set; }
        public int BolumId { get; set; }
        public string? MakineNo { get; set; }
        public string? Aciklama { get; set; }
        public bool? Silindi { get; set; }
    }
}
