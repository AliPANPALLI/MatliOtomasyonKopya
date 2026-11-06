namespace KumesScada.Application.ViewModels
{
    public class NekropsiBulguViewModel
    {
        public string Key { get; set; } = string.Empty;
        public int? Bulunan { get; set; }
        public int? Toplam { get; set; }
    }

    public class NekropsiFotografViewModel
    {
        public int Id { get; set; }
        public string DosyaAdi { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long BoyutByte { get; set; }
    }

    public class NekropsiRaporuViewModel
    {
        public int Id { get; set; }
        public int KumesId { get; set; }

        public DateTime Tarih { get; set; }
        public int? Hafta { get; set; }

        public string NekropsiSebebi { get; set; } = string.Empty;
        public string Pcr { get; set; } = "BELIRTILMEDI";

        public DateTime KayitZamani { get; set; }
        public string? KayitYapanUserId { get; set; }

        public List<NekropsiBulguViewModel> Bulgular { get; set; } = new();
        public List<NekropsiFotografViewModel> Fotograflar { get; set; } = new();
    }

    public class NekropsiRaporuOzetViewModel
    {
        public int Id { get; set; }
        public int KumesId { get; set; }

        public DateTime Tarih { get; set; }
        public int? Hafta { get; set; }

        public string NekropsiSebebi { get; set; } = string.Empty;
        public string Pcr { get; set; } = "BELIRTILMEDI";

        public DateTime KayitZamani { get; set; }

        public int DoluBulguSayisi { get; set; }
        public int FotografSayisi { get; set; }
    }
}
