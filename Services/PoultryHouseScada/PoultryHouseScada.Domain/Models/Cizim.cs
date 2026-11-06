namespace PoultryHouseScada.Domain.Models
{
    public class Cizim
    {
        public int Id { get; set; }
        /// <summary>
        /// Cizimin ismidir
        /// </summary>
        public string? Name { get; set; }

        //iliski: bire cok CizimEtiketEsleme
        public virtual ICollection<CizimSekilEsleme> CizimSekilEslemes { get; set; }
    }
}
