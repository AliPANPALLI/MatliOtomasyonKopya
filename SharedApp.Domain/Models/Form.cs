using System.Text.Json.Serialization;

namespace SharedApp.Domain.Models
{
    public class Form
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Aciklama { get; set; }
        public string Link { get; set; }
        public int ModulId { get; set; }
        public bool? TopbarNesnesi { get; set; }
        public bool? SingleTopbarNesnesi { get; set; }
        public int? Sira { get; set; }
        public bool? Silindi { get; set; }
        public int? ParentId { get; set; } 

        //iliski: bire cok FormLokasyonEsleme
        [JsonIgnore] public virtual ICollection<FormLokasyonEsleme> FormLokasyonEslemes { get; set; }
    }
}
