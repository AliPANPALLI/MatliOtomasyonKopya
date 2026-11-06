using System.Text.Json.Serialization;

namespace OrtamIzlemeScada.Domain.Models
{
    public class Oda
    {
        public int Id { get; set; }
        public int BolumId { get; set; }
        public string? OdaAdi { get; set; }

        public ICollection<Cihaz> Cihazlar { get; set; } = new List<Cihaz>();
        [JsonIgnore] public virtual Bolum Bolum{ get; set; }
    }
}