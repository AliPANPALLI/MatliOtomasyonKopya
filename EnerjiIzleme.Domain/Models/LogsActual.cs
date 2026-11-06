using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace EnerjiIzleme.Domain.Models
{
    public class LogsActual
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId _id { get; set; }
        public int okunan_analizor_id { get; set; }
        public DateTime okunma_zamani { get; set; }
        public veri veri { get; set; }
        public int state { get; set; }
    }
}
