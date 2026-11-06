using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnerjiIzleme.Domain.Models
{
    public class BolumMongo
    {
        [BsonId]
        [BsonRepresentation(BsonType.Int32)]
        public int Id { get; set; }
        public string? Name { get; set; }
        public int LokasyonId { get; set; }
        public bool? Silindi { get; set; }
    }
}
