namespace KumesScada.Domain.Models
{
    public class SekilEtiketKural
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int SekilId { get; set; }
        public int EtiketId { get; set; }
        public decimal MinDeger { get; set; }
        public decimal MaxDeger { get; set; }
        public bool? VeriYok { get; set; }

        /// <summary>
        /// Etiketin kuralları json olarak burada tanımlanır.
        /// </summary>
        public string EtiketKuralJson { get; set; }
        public bool? Silindi { get; set; }
        //iliski: bir sekil olabilir
        public virtual Sekil Sekil { get; set; }
        //iliski: bir etiket olabilir
        public virtual Etiket Etiket { get; set; }
    }
    //[{
    //  "shapeId": "tank1",
    //  "tagName": "Tank_Level",
    //  "rules": {
    //    "min": 1,
    //    "max": 5,
    //    "colors": {
    //      "belowMin": "yellow",
    //      "aboveMax": "red",
    //      "withinRange": "green"
    //    }
    //  }
    //}]
}
