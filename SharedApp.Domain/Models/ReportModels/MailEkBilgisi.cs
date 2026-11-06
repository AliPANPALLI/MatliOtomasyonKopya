using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedApp.Domain.Models.ReportModels
{
    public sealed class MailEkBilgisi
    {
        public int? AlarmLogId { get; set; }

        /// <summary>
        /// Yangın fotoğraf ana klasörüne göre göreli dosya yolu.
        /// Örnek: 2026\07\16\foto.jpg
        /// </summary>
        public string GoreliDosyaYolu { get; set; } = null!;

        public string? DosyaAdi { get; set; }

        public string? ContentType { get; set; }

        public long? DosyaBoyutu { get; set; }
    }
}
