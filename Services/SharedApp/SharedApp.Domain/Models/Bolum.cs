namespace SharedApp.Domain.Models
{
    public class Bolum
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TesisId { get; set; }
        public Tesis Tesis { get; set; }
    }
}