namespace KumesScada.Domain.Models
{
    public class Sekil
    {
        public int Id { get; set; }
        /// <summary>
        /// Etiket ismidir
        /// </summary>
        public string? Name { get; set; }

        public string SekilJson { get; set; }

        //iliski: bire cok CizimEtiketEsleme
        public virtual ICollection<CizimSekilEsleme> CizimSekilEslemes { get; set; }

        //iliski: bire cok EtiketKural
        public virtual ICollection<SekilEtiketKural> EtiketKurals { get; set; }
    }
}
