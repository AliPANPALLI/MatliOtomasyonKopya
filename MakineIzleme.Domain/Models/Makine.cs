using System.Text.Json.Serialization;

namespace MakineIzleme.Domain.Models
{
    public class Makine
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? BolumId { get; set; }
        public byte State { get; set; }
        public bool Connected { get; set; }
        [JsonIgnore] public virtual Bolum Bolum { get; set; }
    }
}
