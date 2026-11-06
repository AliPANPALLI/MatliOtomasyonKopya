namespace PanoKimliklendirme.Domain.Models
{
    public class Cihaz
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Marka { get; set; }
        public string? UrunKodu { get; set; }
        public string? IP { get; set; }
        /// <summary>
        /// Cihazin bulundugu panonun id bilgisidir
        /// </summary>
        public int PanoId { get; set; }
        //public int OnayIslemKayitId { get; set; }
        public bool? OnayIslemdeMi { get; set; }
        public bool? OnayaGonderildiMi { get; set; }
        public bool? Silindi { get; set; }
        /// <summary>
        /// Markadan makineye mi panoya mı bagli oldugu anlasilacaktir:iptal
        /// </summary>
        //public int MakineId { get; set; }
    }
}
