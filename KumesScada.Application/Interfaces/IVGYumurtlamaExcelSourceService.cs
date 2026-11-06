using KumesScada.Domain.Models.Excel;

namespace KumesScada.Application.Interfaces
{
    public sealed class VGYumurtlamaExcelSourceResult
    {
        public List<VGExcelKumesGunlukData> Data { get; set; } = new();

        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Birden fazla çiftlik/format için tek giriş noktası. Ana dosyanın
    /// sayfa adlarına bakarak hangi okuyucunun kullanılacağına karar
    /// verir (Rahmiye "Veri Giriş Tablosu" tek dosya formatı vs.
    /// Verim Gıda "Günlük Yem Hesapları" + opsiyonel "Kümes Tasnif
    /// Sayımları" iki dosya formatı) ve sonucu birleştirilmiş, ortak
    /// <see cref="VGExcelKumesGunlukData"/> listesi olarak döndürür.
    /// </summary>
    public interface IVGYumurtlamaExcelSourceService
    {
        Task<VGYumurtlamaExcelSourceResult> ReadAsync(
            Stream primaryStream,
            string primaryFileName,
            Stream? tasnifStream,
            string? tasnifFileName,
            int? year,
            CancellationToken cancellationToken = default);
    }
}
