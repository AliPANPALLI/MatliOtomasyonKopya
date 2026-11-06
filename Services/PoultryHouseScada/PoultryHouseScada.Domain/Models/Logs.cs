using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoultryHouseScada.Domain.Models
{
    public class Logs
    {
        public int Id { get; set; }
        public DateTime? KayitZamani { get; set; }
        public string? Mesaj { get; set; }
    }
}
