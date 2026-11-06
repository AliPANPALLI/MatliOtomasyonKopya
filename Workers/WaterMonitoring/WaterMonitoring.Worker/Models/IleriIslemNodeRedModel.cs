namespace WaterMonitoring.Worker.Models
{
    public class IleriIslemNodeRedModel
    {
        public int? Head { get; set; }
        public DateTime? TimeNow { get; set; }
        public bool? Tank_1_Sens_Min { get; set; }
        //public bool? Tank_1_Sens_Orta { get; set; }
        public bool? Tank_1_Sens_Max { get; set; }
        public decimal? Tank_1_SensBasinc { get; set; }
        public decimal? Tank_1_SuSeviye { get; set; }
        public decimal? Tank_1_SuSeviye_Yuzde { get; set; }
        public decimal? Tank_1_SuHacim { get; set; }
        public decimal? Tank_1_SuHacim_Yuzde { get; set; }
        public decimal? Tank_1_T_TankHacim { get; set; }

        public bool? Tank_2_Sens_Min { get; set; }
        //public bool? Tank_2_Sens_Orta { get; set; }
        public bool? Tank_2_Sens_Max { get; set; }
        public decimal? Tank_2_SensBasinc { get; set; }
        public decimal? Tank_2_SuSeviye { get; set; }
        public decimal? Tank_2_SuSeviye_Yuzde { get; set; }
        public decimal? Tank_2_SuHacim { get; set; }
        public decimal? Tank_2_SuHacim_Yuzde { get; set; }
        public decimal? Tank_2_T_TankHacim { get; set; }

        public bool? Tank_3_Sens_Min { get; set; }
        //public bool? Tank_3_Sens_Orta { get; set; }
        public bool? Tank_3_Sens_Max { get; set; }
        public decimal? Tank_3_SensBasinc { get; set; }
        public decimal? Tank_3_SuSeviye { get; set; }
        public decimal? Tank_3_SuSeviye_Yuzde { get; set; }
        public decimal? Tank_3_SuHacim { get; set; }
        public decimal? Tank_3_SuHacim_Yuzde { get; set; }
        public decimal? Tank_3_T_TankHacim { get; set; }

        public bool? DM_YumusatmaGiris_Connected { get; set; }
        public decimal? DM_YumusatmaGiris_Total { get; set; }
        public decimal? DM_YumusatmaGiris_Debi { get; set; }

        public bool? DM_OsmozGiris_Connected { get; set; }
        public decimal? DM_OsmozGiris_Total { get; set; }
        public decimal? DM_OsmozGiris_Debi { get; set; }

        public bool? DM_KazanDairesi_Connected { get; set; }
        public decimal? DM_KazanDairesi_Total { get; set; }
        public decimal? DM_KazanDairesi_Debi { get; set; }

        public bool? DM_YumusatmaAtik_Connected { get; set; }
        public decimal? DM_YumusatmaAtik_Total { get; set; }
        public decimal? DM_YumusatmaAtik_Debi { get; set; }

        public bool? DM_Yedek1_Connected { get; set; }
        public decimal? DM_Yedek1_Total { get; set; }
        public decimal? DM_Yedek1_Debi { get; set; }

        public bool? DM_Yedek2_Connected { get; set; }
        public decimal? DM_Yedek2_Total { get; set; }
        public decimal? DM_Yedek2_Debi { get; set; }

        public bool? Hidrofor_HamSu_Cmd_Calis { get; set; }
        public int? Hidrofor_HamSu_Rpr_Mode { get; set; }
        public int? Hidrofor_HamSu_Rpr_Durum { get; set; }
        public bool? Hidrofor_HamSu_Motor1_Calisiyor { get; set; }
        public bool? Hidrofor_HamSu_Motor1_Ariza { get; set; }
        public int? Hidrofor_HamSu_Motor1_Durum { get; set; }
        public bool? Hidrofor_HamSu_Motor2_Calisiyor { get; set; }
        public bool? Hidrofor_HamSu_Motor2_Ariza { get; set; }
        public int? Hidrofor_HamSu_Motor2_Durum { get; set; }
        public bool? Hidrofor_HamSu_Motor3_Calisiyor { get; set; }
        public bool? Hidrofor_HamSu_Motor3_Ariza { get; set; }
        public int? Hidrofor_HamSu_Motor3_Durum { get; set; }
        public decimal? Hidrofor_HamSu_Basinc { get; set; }

        public bool? Hidrofor_YumusakSu_Cmd_Calis { get; set; }
        public int? Hidrofor_YumusakSu_Rpr_Mode { get; set; }
        public int? Hidrofor_YumusakSu_Rpr_Durum { get; set; }
        public bool? Hidrofor_YumusakSu_Motor1_Calisiyor { get; set; }
        public bool? Hidrofor_YumusakSu_Motor1_Ariza { get; set; }
        public int? Hidrofor_YumusakSu_Motor1_Durum { get; set; }
        public bool? Hidrofor_YumusakSu_Motor2_Calisiyor { get; set; }
        public bool? Hidrofor_YumusakSu_Motor2_Ariza { get; set; }
        public int? Hidrofor_YumusakSu_Motor2_Durum { get; set; }
        public bool? Hidrofor_YumusakSu_Motor3_Calisiyor { get; set; }
        public bool? Hidrofor_YumusakSu_Motor3_Ariza { get; set; }
        public int? Hidrofor_YumusakSu_Motor3_Durum { get; set; }
        public decimal? Hidrofor_YumusakSu_Basinc { get; set; }

        public bool? Hidrofor_OsmozSu_Cmd_Calis { get; set; }
        public int? Hidrofor_OsmozSu_Rpr_Mode { get; set; }
        public int? Hidrofor_OsmozSu_Rpr_Durum { get; set; }
        public bool? Hidrofor_OsmozSu_Motor1_Calisiyor { get; set; }
        public bool? Hidrofor_OsmozSu_Motor1_Ariza { get; set; }
        public int? Hidrofor_OsmozSu_Motor1_Durum { get; set; }
        public bool? Hidrofor_OsmozSu_Motor2_Calisiyor { get; set; }
        public bool? Hidrofor_OsmozSu_Motor2_Ariza { get; set; }
        public int? Hidrofor_OsmozSu_Motor2_Durum { get; set; }
        public bool? Hidrofor_OsmozSu_Motor3_Calisiyor { get; set; }
        public bool? Hidrofor_OsmozSu_Motor3_Ariza { get; set; }
        public int? Hidrofor_OsmozSu_Motor3_Durum { get; set; }
        public decimal? Hidrofor_OsmozSu_Basinc { get; set; }

        public bool? Hidrofor_Yangin_Cmd_Calis { get; set; }
        public int? Hidrofor_Yangin_Rpr_Mode { get; set; }
        public int? Hidrofor_Yangin_Rpr_Durum { get; set; }
        public bool? Hidrofor_Yangin_Motor1_Calisiyor { get; set; }
        public bool? Hidrofor_Yangin_Motor1_Ariza { get; set; }
        public int? Hidrofor_Yangin_Motor1_Durum { get; set; }
        public bool? Hidrofor_Yangin_Motor2_Calisiyor { get; set; }
        public bool? Hidrofor_Yangin_Motor2_Ariza { get; set; }
        public int? Hidrofor_Yangin_Motor2_Durum { get; set; }
        public bool? Hidrofor_Yangin_Motor3_Calisiyor { get; set; }
        public bool? Hidrofor_Yangin_Motor3_Ariza { get; set; }
        public int? Hidrofor_Yangin_Motor3_Durum { get; set; }
        public decimal? Hidrofor_Yangin_Basinc { get; set; }

        public bool? Hidrofor_SicakSu1_Cmd_Calis { get; set; }
        public int? Hidrofor_SicakSu1_Rpr_Mode { get; set; }
        public int? Hidrofor_SicakSu1_Rpr_Durum { get; set; }
        public bool? Hidrofor_SicakSu1_Motor1_Calisiyor { get; set; }
        public bool? Hidrofor_SicakSu1_Motor1_Ariza { get; set; }
        public int? Hidrofor_SicakSu1_Motor1_Durum { get; set; }
        public bool? Hidrofor_SicakSu1_Motor2_Calisiyor { get; set; }
        public bool? Hidrofor_SicakSu1_Motor2_Ariza { get; set; }
        public int? Hidrofor_SicakSu1_Motor2_Durum { get; set; }
        public bool? Hidrofor_SicakSu1_Motor3_Calisiyor { get; set; }
        public bool? Hidrofor_SicakSu1_Motor3_Ariza { get; set; }
        public int? Hidrofor_SicakSu1_Motor3_Durum { get; set; }
        public decimal? Hidrofor_SicakSu1_Basinc { get; set; }

        public bool? Hidrofor_SicakSu2_Cmd_Calis { get; set; }
        public int? Hidrofor_SicakSu2_Rpr_Mode { get; set; }
        public int? Hidrofor_SicakSu2_Rpr_Durum { get; set; }
        public bool? Hidrofor_SicakSu2_Motor1_Calisiyor { get; set; }
        public bool? Hidrofor_SicakSu2_Motor1_Ariza { get; set; }
        public int? Hidrofor_SicakSu2_Motor1_Durum { get; set; }
        public bool? Hidrofor_SicakSu2_Motor2_Calisiyor { get; set; }
        public bool? Hidrofor_SicakSu2_Motor2_Ariza { get; set; }
        public int? Hidrofor_SicakSu2_Motor2_Durum { get; set; }
        public bool? Hidrofor_SicakSu2_Motor3_Calisiyor { get; set; }
        public bool? Hidrofor_SicakSu2_Motor3_Ariza { get; set; }
        public int? Hidrofor_SicakSu2_Motor3_Durum { get; set; }
        public decimal? Hidrofor_SicakSu2_Basinc { get; set; }

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
