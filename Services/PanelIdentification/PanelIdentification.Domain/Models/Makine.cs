using System.Text.Json.Serialization;

namespace PanelIdentification.Domain.Models
{
    public class Makine
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BolumId { get; set; }
        public string? MakineNo { get; set; }
        /// <summary>
        /// Parent varsa Bolum Id bilgisi, aksi halde 0
        /// </summary>
        public int ParentMakineId { get; set; }
        public string? Aciklama { get; set; }
        //public int OnayIslemKayitId { get; set; }
        public bool? OnayIslemdeMi { get; set; }
        public bool? OnayaGonderildiMi { get; set; }
        public bool? Silindi { get; set; }
        //iliski: bir bolumun birden fazla makinesi olabilir
        [JsonIgnore] public virtual Bolum Bolum { get; set; }
        //iliski: bire cok PanoMakineEsleme
        [JsonIgnore] public virtual ICollection<PanoMakineEsleme> PanoMakineEslemes { get; set; }
    }
}
