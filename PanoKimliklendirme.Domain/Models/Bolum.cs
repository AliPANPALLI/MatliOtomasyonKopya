using SharedApp.Domain.Models;
using System.Text.Json.Serialization;

namespace PanoKimliklendirme.Domain.Models
{
    public class Bolum
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int LokasyonId { get; set; }
        /// <summary>
        /// Parent varsa Bolum Id bilgisi, aksi halde 0
        /// </summary>
        public int ParentBolumId { get; set; }
        /// <summary>
        /// onay takibi icin OnayIslemKayit tablosu Id bilgisidir
        /// </summary>
        //public int OnayIslemKayitId { get; set; }
        public bool? OnayIslemdeMi { get; set; }
        public bool? OnayaGonderildiMi { get; set; }
        public bool? Silindi { get; set; }
        //iliski: bir bolumun birden fazla makinesi olabilir
        [JsonIgnore] public ICollection<Makine>? Makines { get; set; }
    }
}
