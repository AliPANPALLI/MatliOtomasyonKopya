using System.Text.Json.Serialization;

namespace SharedApp.Domain.Models
{
    public class Lokasyon
    {
        public int Id { get; set; }
        /// <summary>
        /// Varsa, SAP kodudur.
        /// </summary>
        public string? LokasyonNo { get; set; }
        public string? Name { get; set; }
        /// <summary>
        /// Lokasyonun ait oldugu sirketin sirket tablosu id bilgisidir.
        /// </summary>
        public int SirketId { get; set; }
        public string? Adres { get; set; }
        public bool? Silindi { get; set; }
        //iliski: bir lokasyonun bir sirketi olabilir
        public Sirket Sirket { get; set; }

        //iliski: bire cok FormLokasyonEsleme
        [JsonIgnore] public virtual ICollection<FormLokasyonEsleme> FormLokasyonEslemes { get; set; }
        [JsonIgnore] public virtual ICollection<Tesis> Tesisler { get; set; }
    }
}
