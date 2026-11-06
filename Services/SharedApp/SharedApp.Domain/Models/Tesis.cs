using System.Text.Json.Serialization;

namespace SharedApp.Domain.Models
{
    public class Tesis
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int LokasyonId { get; set; }
        [JsonIgnore] public virtual ICollection<Bolum> Bolumler { get; set; }
    }
}
