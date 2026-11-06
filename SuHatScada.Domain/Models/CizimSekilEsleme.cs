namespace SuHatScada.Domain.Models
{
    public class CizimSekilEsleme
    {
        public int Id { get; set; }
        public int CizimId { get; set; }
        public int SekilId { get; set; }
        public bool? Silindi { get; set; }

        //iliski: bir cizim olabilir
        public virtual Cizim Cizim { get; set; }
        //iliski: bir etiket olabilir
        public virtual Sekil Sekil { get; set; }
    }
}
