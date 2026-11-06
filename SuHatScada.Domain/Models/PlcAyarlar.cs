namespace SuHatScada.Domain.Models
{
    public class PlcAyarlar
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? LokasyonId { get; set; }
        public bool? PlcIpAdresFlag { get; set; }
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
        public bool? Silindi { get; set; }
    }
}