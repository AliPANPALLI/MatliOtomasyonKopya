using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnerjiIzleme.Domain.Models
{
    public class Bolum
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int LokasyonId { get; set; }
        public bool? Aktif { get; set; }
        public bool? GES { get; set; }
        public bool? Silindi { get; set; }
        public bool? Onaylandi { get; set; }
        public DateTime? OnaylanmaZamani { get; set; }
        public string? OnaylayanUserId { get; set; }
        public DateTime? EklenmeZamani { get; set; }
        public string? EkleyenUserId { get; set; }
        public DateTime? GuncellemeZamani { get; set; }
        public string? GuncelleyenUserId { get; set; }
        public ICollection<Makine> Makineler { get; set; } = new List<Makine>();

    }
}