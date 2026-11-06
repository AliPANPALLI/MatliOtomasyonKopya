namespace MobilGuncelleme.Application.Dtos
{
    /// <summary>
    /// Sürüm kontrolünün cevabı. GuncellemeVar = false ise diğer alanlar boştur ve uygulama
    /// ana ekrana geçer; true ise güncelleme ekranı açılır ve APK SurumId ile indirilir.
    /// </summary>
    public class SurumKontrolSonucDto
    {
        public bool GuncellemeVar { get; set; }
        public int? SurumId { get; set; }
        public int? VersionCode { get; set; }
        public string? VersionName { get; set; }
        public string? SurumNotu { get; set; }

        /// <summary>Telefonun indirdiği dosyayı doğrulamak için kullandığı SHA-256.</summary>
        public string? Sha256 { get; set; }

        /// <summary>Byte cinsinden APK boyutu.</summary>
        public long? Boyut { get; set; }
    }
}
