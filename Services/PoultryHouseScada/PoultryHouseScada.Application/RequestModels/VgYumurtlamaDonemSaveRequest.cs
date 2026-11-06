namespace PoultryHouseScada.Application.RequestModels
{
    /// <summary>
    /// "Yumurtlama Kartı Excel'ini Oluştur" formundan elle girilen
    /// dönem bilgisini kaydeder — bir kez girilince bir daha sorulmaz,
    /// bkz. VgYumurtlamaDonemAppService.GetLatestByKumesNo.
    /// </summary>
    public sealed class VgYumurtlamaDonemSaveRequest
    {
        public string KumesNo { get; set; } = string.Empty;

        public DateTime GirisTarihi { get; set; }

        public decimal BaslangicHayvanSayisi { get; set; }

        public string? Irk { get; set; }

        /// <summary>
        /// Hayvanın GirisTarihi günündeki yaşı (gün) — bkz.
        /// YumurtlamaDonem.GirisYasiGun üzerindeki not. Opsiyoneldir.
        /// </summary>
        public int? GirisYasiGun { get; set; }

        /// <summary>Bkz. YumurtlamaDonem.GirisHaftasi üzerindeki not.</summary>
        public int? GirisHaftasi { get; set; }
    }
}
