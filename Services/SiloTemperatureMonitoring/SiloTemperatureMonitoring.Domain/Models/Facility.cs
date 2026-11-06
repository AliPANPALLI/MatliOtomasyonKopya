namespace SiloTemperatureMonitoring.Domain.Models
{
    public class Facility
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
