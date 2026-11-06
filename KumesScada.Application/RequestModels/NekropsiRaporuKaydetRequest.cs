using Microsoft.AspNetCore.Http;

namespace KumesScada.Application.RequestModels
{
    public class NekropsiRaporuKaydetRequest
    {
        public int KumesId { get; set; }

        public DateTime Tarih { get; set; }
        public int? Hafta { get; set; }

        public string NekropsiSebebi { get; set; } = string.Empty;
        public string Pcr { get; set; } = "BELIRTILMEDI";

        /// <summary>
        /// [{"key":"airsacculit","bulunan":1,"toplam":10}, ...] formatında
        /// JSON-serileştirilmiş bulgu listesi. Multipart form içinde nested
        /// bir liste alanı olarak taşımak yerine tek bir string alan olarak
        /// gönderilip sunucu tarafında ayrıştırılıyor.
        /// </summary>
        public string? BulgularJson { get; set; }

        public List<IFormFile>? Fotograflar { get; set; }
    }

    public class NekropsiBulguKaydetRequest
    {
        public string Key { get; set; } = string.Empty;
        public int? Bulunan { get; set; }
        public int? Toplam { get; set; }
    }
}
