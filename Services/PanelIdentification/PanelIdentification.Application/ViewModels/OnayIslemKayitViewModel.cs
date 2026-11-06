namespace PanelIdentification.Application.ViewModels
{
    public class OnayIslemKayitViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        /// <summary>
        /// Dosya, Bolum, Pano
        /// </summary>
        public string OgeTipi { get; set; }

        /// <summary>
        /// Dosya için bir benzersiz kimlik veya isim, BolumId, PanoId
        /// </summary>
        public int? OgeId { get; set; }
        /// <summary>
        /// add,delete,edit
        /// </summary>
        public byte IslemTipi { get; set; }
        /// <summary>
        /// Dosya bilgileri (örn. {"fileName": "A.docx", "tempPath": "/temp/A.docx", "finalPath": "/X/A.docx"})
        /// </summary>
        public string? IslemVerisi { get; set; }
        public byte? Durum { get; set; }
        public DateTime? KayitZamani { get; set; }
        public DateTime? GuncellemeZamani { get; set; }
        public string? GuncelleyenUserId { get; set; }
    }
}
