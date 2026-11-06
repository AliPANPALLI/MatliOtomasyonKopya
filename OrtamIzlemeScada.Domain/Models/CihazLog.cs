namespace OrtamIzlemeScada.Domain.Models
{
    public class CihazLog
    {
        public long Id { get; set; }
        public int OdaId { get; set; }
        public DateTime? KayitZamani { get; set; }
        public bool? SN_1_Connected { get; set; }
        public bool? SN_1_Flag { get; set; }
        public float? SN_1_Sicaklik { get; set; }
        public float? SN_1_Nem { get; set; }
        public int? SN_1_Battery { get; set; }
        public DateTime? SN_1_LastSeen { get; set; }
        public bool? SN_2_Connected { get; set; }
        public bool? SN_2_Flag { get; set; }
        public float? SN_2_Sicaklik { get; set; }
        public float? SN_2_Nem { get; set; }
        public int? SN_2_Battery { get; set; }
        public DateTime? SN_2_LastSeen { get; set; }
        public bool? SB_1_Connected { get; set; }
        public bool? SB_1_Flag { get; set; }
        public bool? SB_1_SuVar { get; set; }
        public int? SB_1_Battery { get; set; }
        public DateTime? SB_1_LastSeen { get; set; }
        public bool? SB_2_Connected { get; set; }
        public bool? SB_2_Flag { get; set; }
        public bool? SB_2_SuVar { get; set; }
        public int? SB_2_Battery { get; set; }
        public DateTime? SB_2_LastSeen { get; set; }
        public bool? HS_1_Connected { get; set; }
        public bool? HS_1_Flag { get; set; }
        public bool? HS_1_HaraketVar { get; set; }
        public DateTime? HS_1_LastSeen { get; set; }
        public bool? KS_1_Connected { get; set; }
        public bool? KS_1_Flag { get; set; }
        public bool? KS_1_KapiAcildi { get; set; }
        public DateTime? KS_1_LastSeen { get; set; }
        public int? KS_1_Battery { get; set; }
        public float? EA_L1_V { get; set; }
        public float? EA_L1_A { get; set; }
        public float? EA_L2_V { get; set; }
        public float? EA_L2_A { get; set; }
        public float? EA_L3_V { get; set; }
        public float? EA_L3_A { get; set; }
        public float? EA_PE_V { get; set; }
    }
}