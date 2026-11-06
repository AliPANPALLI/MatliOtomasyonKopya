namespace SuHatScada.Domain.Models
{
    public class ScadaData
    {
        //public List<EtiketLog_Temp> EtiketDegerleri { get; set; } = new List<EtiketLog_Temp>();
        //public List<EtiketLog> DMGunlukEtiketDegerleri { get; set; } = new List<EtiketLog>();
        public List<EtiketLog_TempYeni> EtiketDegerleri { get; set; } = new List<EtiketLog_TempYeni>();
        public List<EtiketLogYeni> DMGunlukEtiketDegerleri { get; set; } = new List<EtiketLogYeni>();
   }
}
