namespace KumesScada.Application.ViewModels
{
    public class VGYumurtlamaViewModel
    {
        public string KumesNo { get; set; } = string.Empty;

        public string? Irk { get; set; }

        public DateTime KumeseGirisTarihi { get; set; }

        public decimal BaslangicHayvanSayisi { get; set; }

        public string? IcSiparisNo { get; set; }

        public List<VGYumurtlamaGunlukViewModel> GunlukVeriler { get; set; } = new();

        public List<VGYumurtlamaHaftalikViewModel> HaftalikVeriler { get; set; } = new();
    }
}