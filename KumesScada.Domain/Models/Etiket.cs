namespace KumesScada.Domain.Models
{
    public class Etiket
    {
        public int Id { get; set; }
        /// <summary>
        /// Etiket ismidir
        /// </summary>
        public string? Name { get; set; }
        public string? EslemeSozcugu { get; set; }

        //iliski: bire cok EtiketKural
        public virtual ICollection<SekilEtiketKural> EtiketKurals { get; set; }
    }
}
