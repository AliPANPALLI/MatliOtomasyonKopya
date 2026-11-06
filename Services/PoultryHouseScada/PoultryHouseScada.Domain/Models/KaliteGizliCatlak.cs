namespace PoultryHouseScada.Domain.Models
{
    public class KaliteGizliCatlak
    {
        public int KaliteGizliCatlakId { get; set; }

        public DateTime KaliteGizliCatlakTarih { get; set; }

        public decimal KaliteGizliCatlakMiktar { get; set; }

        public int KumeslerId { get; set; }

        public Kumesler Kumesler { get; set; } = null!;
    }
}
