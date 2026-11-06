namespace PoultryHouseScada.Application.ViewModels
{
    public class ShapeEditorEtiketViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class ShapeEditorRuleViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int SekilId { get; set; }
        public int EtiketId { get; set; }
        public decimal MinDeger { get; set; }
        public decimal MaxDeger { get; set; }
        public bool VeriYok { get; set; }
        public string EtiketKuralJson { get; set; } = string.Empty;
    }

    public class SaveShapeEditorRuleViewModel
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string ShapeJsonId { get; set; } = string.Empty;
        public int EtiketId { get; set; }
        public decimal MinDeger { get; set; }
        public decimal MaxDeger { get; set; }
        public bool VeriYok { get; set; }
        public string EtiketKuralJson { get; set; } = string.Empty;
    }
}
