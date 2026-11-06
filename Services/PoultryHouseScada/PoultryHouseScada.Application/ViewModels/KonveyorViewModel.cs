namespace PoultryHouseScada.Application.ViewModels
{
    public class KonveyorViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int LokasyonId { get; set; }
        public byte? Tip { get; set; }
        public int? ParentId { get; set; }
    }
}
