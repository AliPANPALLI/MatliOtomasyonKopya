using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumesScada.Application.RequestModels
{
    public sealed class VGYumurtlamaGenerateRequest
    {
        /// <summary>Günlük Yem Hesapları dosyası (ölüm/yem/su/kalite).</summary>
        public IFormFile File { get; set; } = null!;

        /// <summary>
        /// Kümes Tasnif Sayımları dosyası (yumurta adedi/gramaj/kırılım).
        /// Opsiyoneldir; verilmezse rapor yumurta kırılımı olmadan
        /// (yalnızca ölüm/yem/su) üretilir.
        /// </summary>
        public IFormFile? TasnifFile { get; set; }

        public int Year { get; set; }

        public string KumesNo { get; set; } = string.Empty;

        public string? Irk { get; set; }

        public DateTime KumeseGirisTarihi { get; set; }

        public decimal BaslangicHayvanSayisi { get; set; }

        public string? IcSiparisNo { get; set; }

        /// <summary>Bkz. YumurtlamaDonem.GirisYasiGun üzerindeki not. Opsiyoneldir.</summary>
        public int? GirisYasiGun { get; set; }
    }
}