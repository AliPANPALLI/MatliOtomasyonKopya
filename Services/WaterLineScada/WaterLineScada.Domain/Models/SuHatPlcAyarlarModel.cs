using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterLineScada.Domain.Models
{
    public class SuHatPlcAyarlarModel
    {
        public int? SuKaynakId { get; set; }
        public string? Name { get; set; }
        public string? PlcIpAdres { get; set; }
        public bool? PlcAutoConnect { get; set; } = true;
        public bool? Aktif { get; set; } = false;
        public bool? PlcBagli { get; set; } = false;
        //public bool? SorumluMu { get; set; } = false;
        public string? ServisApiUrl { get; set; }
    }
}