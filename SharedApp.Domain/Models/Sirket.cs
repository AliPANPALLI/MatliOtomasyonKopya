using System.Text.Json.Serialization;

namespace SharedApp.Domain.Models
{    public class Sirket
    {
        public int Id { get; set; }
        /// <summary>
        /// Varsa, SAP kodudur.
        /// </summary>
        public string SirketNo { get; set; }
        public string Name { get; set; }
        public bool? Silindi { get; set; }
        //iliski: bir sirketin birden fazla lokasyonu olabilir
        [JsonIgnore] public virtual ICollection<Lokasyon> Lokasyons { get; set; }
    }
}
