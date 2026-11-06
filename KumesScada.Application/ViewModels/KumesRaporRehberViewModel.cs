namespace KumesScada.Application.ViewModels
{
    public class KumesRaporRehberViewModel
    {
        public int Id { get; set; }

        public int LokasyonId { get; set; }

        public string? RehberAdi { get; set; }

        public int HaftaSayisi { get; set; }

        public decimal Deger { get; set; }
    }
}
