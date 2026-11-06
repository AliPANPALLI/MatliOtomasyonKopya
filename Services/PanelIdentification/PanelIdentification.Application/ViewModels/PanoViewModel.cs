namespace PanelIdentification.Application.ViewModels
{
    public class PanoViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }
        /// <summary>
        /// Parent varsa Pano Id bilgisi, aksi halde 0
        /// </summary>
        public int ParentPanoId { get; set; }
        /// <summary>
        /// File server dosya yolu bilgisidir
        /// </summary>
        public string? DosyaYolu { get; set; }
        /// <summary>
        /// Pano Kimlik numara bilgisidir
        /// </summary>
        public string? PanoKimlikNo { get; set; }
    }
}
