namespace PanelIdentification.Domain.Models
{
    public class PanoMakineEsleme
    {
        public int Id { get; set; }
        //public string PanoKimlikNo { get; set; }
        public int MakineId { get; set; }
        public int PanoId { get; set; }
        //public int OnayIslemKayitId { get; set; }
        //public byte? OnayIslemDurum { get; set; }
        public bool? Silindi { get; set; }

        //iliski: bir pano olabilir
        public virtual Pano Pano { get; set; }
        //iliski: bir makine olabilir
        public virtual Makine Makine { get; set; }
    }
}
