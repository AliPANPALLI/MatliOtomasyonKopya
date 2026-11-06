namespace EnerjiIzleme.Domain.Models
{
    public class Gateway
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Marka { get; set; }
        public string? Model { get; set; }
        public string IpAdres { get; set; }
        public string? Port { get; set; }
        public string? SubnetMask { get; set; }
        public string? RouterIp { get; set; }
        public bool Aktif { get; set; }
        public ICollection<Cihaz> Cihazlar { get; set; } = new List<Cihaz>();

    }
}