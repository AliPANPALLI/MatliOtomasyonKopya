namespace KumesScada.Domain.Models
{
    public class Kumes
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? KumesNo { get; set; }
        public string? KumesTcId { get; set; }
        public int LokasyonId { get; set; }
        /// <summary>
        /// Ticari yumurta, damızlık vb. kümes tipleri belirtilmelidir.
        /// </summary>
        public int? CizimId { get; set; }
        public string? CizimUrl { get; set; }
        public int KumesTip { get; set; }
        public int? Kapasite { get; set; }
        public bool? Silindi { get; set; }
        public bool? PlcIpAdresFlag { get; set; }
        public string? HmiIpAdres { get; set; }
        public string? PlcIpAdres { get; set; }
        public string? PlcNewIpAdres { get; set; }
        public bool? PlcAutoConnectFlag { get; set; }
        public bool? PlcAutoConnect { get; set; }
        public bool? PlcNewAutoConnect { get; set; }
        public bool? PlcAktifFlag { get; set; }
        public bool? PlcNewAktif { get; set; }
        public bool? Aktif { get; set; }
        //TODO:rack ve slot alanlari da eklenecek
        public string? ServisApiUrl { get; set; }
        public int? PlcConnectedEtiketId { get; set; }
    }
}