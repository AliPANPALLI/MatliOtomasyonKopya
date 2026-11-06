using System.Text.Json.Serialization;

namespace PanoKimliklendirme.Domain.Models
{
    public class Pano
    {
        public int Id { get; set; }
        public string Name { get; set; }
        //public int MakineId { get; set; }
        /// <summary>
        /// Parent varsa Pano Id bilgisi, aksi halde 0
        /// </summary>
        public int ParentPanoId { get; set; }
        //Pao Kimlik tablosu iptal edildi, bilgiler buraya tasindi. DosyaYolu

        /// <summary>
        /// File server dosya yolu bilgisidir
        /// </summary>
        public string? DosyaYolu { get; set; }
        /// <summary>
        /// Pano Kimlik numara bilgisidir
        /// </summary>
        public string PanoKimlikNo { get; set; }
        //public int OnayIslemKayitId { get; set; }
        public bool? OnayIslemdeMi { get; set; }
        public bool? OnayaGonderildiMi { get; set; }
        public bool? Silindi { get; set; }

        //iliski: bire cok PanoMakineEsleme
        [JsonIgnore] public ICollection<PanoMakineEsleme> PanoMakineEslemes { get; set; }
    }
}
