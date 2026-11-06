namespace SharedApp.Domain.Models
{
    public class ServisKontrol
    {
        public int Id { get; set; }
        public string ServisName { get; set; }
        public byte? Durum { get; set; }
        public string? Mesaj { get; set; }
        public DateTime? SonAktifZaman { get; set; }
        public DateTime? SonBasariliZaman { get; set; }
        public decimal? ZamanAsimiDakika { get; set; }
    }
}
