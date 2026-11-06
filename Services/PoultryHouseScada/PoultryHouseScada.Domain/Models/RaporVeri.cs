namespace PoultryHouseScada.Domain.Models
{
    public class RaporVeri
    {
        public int RaporVerilerId { get; set; }

        public int RaporKategoriId { get; set; }

        public int HaftaSayisi { get; set; }

        public decimal? RaporHedefDeger { get; set; }

        public RaporKategori RaporKategori { get; set; } = null!;
    }
}
