namespace KumesScada.Application.ViewModels
{
    /// <summary>
    /// Bkz. KumesScada.Domain.Models.VgYumurtlamaDonem — kümesin (en
    /// güncel) dönemine ait kümese giriş tarihi / başlangıç hayvan
    /// sayısı / ırk bilgisi.
    /// </summary>
    public class VgYumurtlamaDonemViewModel
    {
        public string KumesNo { get; set; } = string.Empty;

        public DateTime GirisTarihi { get; set; }

        public decimal BaslangicHayvanSayisi { get; set; }

        public string? Irk { get; set; }

        public int? GirisYasiGun { get; set; }

        public int? GirisHaftasi { get; set; }
    }
}
