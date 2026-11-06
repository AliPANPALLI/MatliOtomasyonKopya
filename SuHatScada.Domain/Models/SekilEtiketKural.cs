using System.Text.Json.Serialization;

namespace SuHatScada.Domain.Models
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
        /// Seklin fiziki degisecegi ozellikler json olarak burada tanımlanır.
        /// </summary>
        public string EtiketKuralJson { get; set; }
        /// <summary>
        /// Etiketin kuralları json olarak burada tanımlanır.
        /// </summary>
        public string? EtiketJson { get; set; }
        public bool? Silindi { get; set; }
        public string? ModTuru { get; set; }

        //iliski: bir sekil olabilir
        [JsonIgnore] public virtual Sekil Sekil { get; set; }
        //iliski: bir etiket olabilir
        [JsonIgnore] public virtual Etiket Etiket { get; set; }
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
