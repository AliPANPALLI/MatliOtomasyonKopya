using System.ComponentModel.DataAnnotations.Schema;

namespace MachineMonitoring.Domain.Models
{
    public class MakineLog
    {
        public long Id { get; set; }
        public int MakineId { get; set; }
        public byte State { get; set; }
        public DateTime KayitZamani { get; set; }
        public long DeltaTime { get; set; } //timespan
        public string? Aciklama { get; set; }

        [NotMapped]
        public TimeSpan DeltaTimeTicks
        {
            get => TimeSpan.FromTicks(DeltaTime);
            set => DeltaTime = value.Ticks;
        }
    }
}