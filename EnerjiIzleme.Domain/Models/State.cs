using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnerjiIzleme.Domain.Models
{
    public class State
    {
        [BsonId]
        [BsonRepresentation(BsonType.Int32)]
        public int _id { get; set; }
        public string? name  { get; set; }
        public string? color { get; set; }
        public string? description { get; set; }
    }
}
