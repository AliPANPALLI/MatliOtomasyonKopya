using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace EnerjiIzleme.Domain.Models
{
    public class Logs
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId _id { get; set; }
        public payload payload { get; set; }
        public int state { get; set; }
        public string _msgid { get; set; }
    }
}
