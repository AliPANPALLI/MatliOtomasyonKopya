using System.Text.Json.Serialization;

namespace SharedApp.Domain.Models
{
    public class FormLokasyonEsleme
    {
        public int Id { get; set; }
        public int FormId { get; set; }
        public int LokasyonId { get; set; }
        public int? CizimId { get; set; }
        public int? KumesId { get; set; }
        public bool? Silindi { get; set; }

        //iliski: bir form olabilir
        [JsonIgnore] public virtual Form Form { get; set; }
        ////iliski: bir lokasyon olabilir
        //[JsonIgnore] public virtual Lokasyon Lokasyon { get; set; }
    }
}