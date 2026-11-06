namespace WaterMonitoring.Worker.Models
{
    public class RenderingNodeRedModel
    {
        public int? Head { get; set; }
        public DateTime? TimeNow { get; set; }
        public bool? Tank_1_Sens_Min { get; set; }
        public bool? Tank_1_Sens_Max { get; set; }
        public decimal? Tank_1_SensBasinc { get; set; }
        public decimal? Tank_1_SuSeviye { get; set; }
        public decimal? Tank_1_SuSeviye_Yuzde { get; set; }
        public decimal? Tank_1_SuHacim { get; set; }
        public decimal? Tank_1_SuHacim_Yuzde { get; set; }
        public decimal? Tank_1_T_TankHacim { get; set; }

        public bool? DM_SicakSuRingHatti_Connected { get; set; }
        public decimal? DM_SicakSuRingHatti_Total { get; set; }
        public decimal? DM_SicakSuRingHatti_Debi { get; set; }

        public bool? DM_SicakSuHatti_Connected { get; set; }
        public decimal? DM_SicakSuHatti_Total { get; set; }
        public decimal? DM_SicakSuHatti_Debi { get; set; }

        public bool? Hidrofor_Sicaksu_Cmd_Calis { get; set; }
        public int? Hidrofor_Sicaksu_Rpr_Mode { get; set; }
        public int? Hidrofor_Sicaksu_Rpr_Durum { get; set; }
        public bool? Hidrofor_Sicaksu_Motor_1_Calisiyor { get; set; }
        public bool? Hidrofor_Sicaksu_Motor_1_Ariza { get; set; }
        public int? Hidrofor_Sicaksu_Motor_1_Durum { get; set; }
        public bool? Hidrofor_Sicaksu_Motor_2_Calisiyor { get; set; }
        public bool? Hidrofor_Sicaksu_Motor_2_Ariza { get; set; }
        public int? Hidrofor_Sicaksu_Motor_2_Durum { get; set; }
        public bool? Hidrofor_Sicaksu_Motor_3_Calisiyor { get; set; }
        public bool? Hidrofor_Sicaksu_Motor_3_Ariza { get; set; }
        public int? Hidrofor_Sicaksu_Motor_3_Durum { get; set; }
        public bool? Hidrofor_Sicaksu_Motor_4_Calisiyor { get; set; }
        public bool? Hidrofor_Sicaksu_Motor_4_Ariza { get; set; }
        public int? Hidrofor_Sicaksu_Motor_4_Durum { get; set; }
        public decimal? Hidrofor_Sicaksu_Basinc { get; set; }

        public bool? Hidrofor_Ring_Cmd_Calis { get; set; }
        public int? Hidrofor_Ring_Rpr_Mode { get; set; }
        public int? Hidrofor_Ring_Rpr_Durum { get; set; }
        public bool? Hidrofor_Ring_Motor_1_Calisiyor { get; set; }
        public bool? Hidrofor_Ring_Motor_1_Ariza { get; set; }
        public int? Hidrofor_Ring_Motor_1_Durum { get; set; }
        public bool? Hidrofor_Ring_Motor_2_Calisiyor { get; set; }
        public bool? Hidrofor_Ring_Motor_2_Ariza { get; set; }
        public int? Hidrofor_Ring_Motor_2_Durum { get; set; }
        public bool? Hidrofor_Ring_Motor_3_Calisiyor { get; set; }
        public bool? Hidrofor_Ring_Motor_3_Ariza { get; set; }
        public int? Hidrofor_Ring_Motor_3_Durum { get; set; }
        public decimal? Hidrofor_Ring_Kollektor_Sicaklik { get; set; }

        public bool? SebekedeElektrikVar { get; set; }
        public bool? Alarm_1 { get; set; }
        public bool? Alarm_2 { get; set; }
        public bool? Alarm_3 { get; set; }
        public bool? Alarm_4 { get; set; }
        public bool? Alarm_5 { get; set; }
        public bool? Alarm_6 { get; set; }
        public bool? Alarm_7 { get; set; }
        public bool? Alarm_8 { get; set; }
        public bool? Alarm_9 { get; set; }
        public bool? Alarm_10 { get; set; }
        public bool? Alarm_11 { get; set; }
        public bool? Alarm_12 { get; set; }
        public bool? Alarm_13 { get; set; }
        public bool? Alarm_14 { get; set; }
        public bool? Alarm_15 { get; set; }
        public bool? Alarm_16 { get; set; }
        public bool? Alarm_17 { get; set; }
        public bool? Alarm_18 { get; set; }
        public bool? Alarm_19 { get; set; }
        public bool? Alarm_20 { get; set; }
        public bool? Alarm_21 { get; set; }
        public bool? Alarm_22 { get; set; }
        public bool? Alarm_23 { get; set; }
        public bool? Alarm_24 { get; set; }
        public bool? Alarm_25 { get; set; }
        public bool? Alarm_26 { get; set; }
        public bool? Alarm_27 { get; set; }
        public bool? Alarm_28 { get; set; }
        public bool? Alarm_29 { get; set; }
        public bool? Alarm_30 { get; set; }
        public bool? Alarm_31 { get; set; }
        public bool? Alarm_32 { get; set; }
        public bool? Uyari_1 { get; set; }
        public bool? Uyari_2 { get; set; }
        public bool? Uyari_3 { get; set; }
        public bool? Uyari_4 { get; set; }
        public bool? Uyari_5 { get; set; }
        public bool? Uyari_6 { get; set; }
        public bool? Uyari_7 { get; set; }
        public bool? Uyari_8 { get; set; }
        public bool? Uyari_9 { get; set; }
        public bool? Uyari_10 { get; set; }
        public bool? Uyari_11 { get; set; }
        public bool? Uyari_12 { get; set; }
        public bool? Uyari_13 { get; set; }
        public bool? Uyari_14 { get; set; }
        public bool? Uyari_15 { get; set; }
        public bool? Uyari_16 { get; set; }
        public decimal? Reserve_1 { get; set; }
        public decimal? Reserve_2 { get; set; }
        public decimal? Reserve_3 { get; set; }
        public decimal? Reserve_4 { get; set; }
        public decimal? Reserve_5 { get; set; }
        public decimal? Reserve_6 { get; set; }
        public decimal? Reserve_7 { get; set; }
        public decimal? Reserve_8 { get; set; }
        public bool? Reserve_9 { get; set; }
        public bool? Reserve_10 { get; set; }
        public bool? Reserve_11 { get; set; }
        public bool? Reserve_12 { get; set; }
        public int? END { get; set; }
    }
}
