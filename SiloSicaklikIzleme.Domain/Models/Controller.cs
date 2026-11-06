namespace SiloSicaklikIzleme.Domain.Models
{
    // Tablo adı "ITG_CONTROLLER" tarihsel bir isim; şema facility-agnostic olduğu
    // için bu entity Silo tesisindeki kontrolcüleri de aynı tabloda temsil eder.
    public class Controller
    {
        public Guid Id { get; set; }
        public Guid FacilityId { get; set; }
        public Guid EdgeDeviceId { get; set; }
        public int ControllerNo { get; set; }
        public string? Code { get; set; }
    }
}
