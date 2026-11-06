namespace SharedApp.Domain.Models
{
    public class Alarmlar
    {
        public int Id { get; set; }
        public string? AlarmAd { get; set; }
        // Mapping key to identify alarm from external systems/workers (e.g. "A3_TankKritik").
        // This allows workers to lookup alarm definitions dynamically without enums.
        //public string? MappingKey { get; set; }
        public string? AlarmMetni { get; set; }
        public string? Aciklama { get; set; }
        public string? SesliMesajMetni { get; set; }
        public string? Tip { get; set; }//Uyarı veya Alarm
        public int? LokasyonId { get; set; }//su izleme modeller icin
        public int? KumesId { get; set; }//kumes worker icin
        public int? KonveyorId { get; set; }//konveyor icin
        public int? CihazId { get; set; }//enerji izleme cihazlar
        //public int? AlarmNoServis { get; set; }
        //public int? UyariNoServis { get; set; }
        //public int? AlarmEnum { get; set; }
        public bool? AlarmScadaEkrandaGosterilsinMi { get; set; }
        public bool? Silindi { get; set; }
        public string? AlarmEslemeSozcugu { get; set; }
        public int? FizikselLokasyonId { get; set; }
    }
}