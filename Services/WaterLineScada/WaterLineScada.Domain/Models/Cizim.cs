namespace WaterLineScada.Domain.Models
{
    public class Cizim
    {
        public int Id { get; set; }
        /// <summary>
        /// Cizimin ismidir
        /// </summary>
        public string? Name { get; set; }
        public bool? Silindi { get; set; }
        public int? SidebarSira { get; set; }
        public string? CizimUrl { get; set; }
        public string? CizimTipi { get; set; }
        public string? HmiUrl { get; set; }

        //iliski: bire cok CizimEtiketEsleme
        public virtual ICollection<CizimSekilEsleme> CizimSekilEslemes { get; set; }
    }
}
