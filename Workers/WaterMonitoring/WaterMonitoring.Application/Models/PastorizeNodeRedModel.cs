namespace WaterMonitoring.Application.Models
{
    public class PastorizeNodeRedModel
    {
        public int? Head { get; set; }
        public DateTime? TimeNow { get; set; }

        public bool? Tank_HamSu_Transmitter_Active { get; set; }
        public decimal? Tank_HamSu_Transmitter_Value { get; set; }
        public decimal? Tank_HamSu_Transmitter_Ham { get; set; }
        public bool? Tank_HamSu_SensMin_Aktif { get; set; }
        public bool? Tank_HamSu_SensOrta_Aktif { get; set; }
        public bool? Tank_HamSu_SensMax_Aktif { get; set; }
        public bool? Tank_YumusakSu_Transmitter_Active { get; set; }
        public decimal? Tank_YumusakSu_Transmitter_Value { get; set; }
        public decimal? Tank_YumusakSu_Transmitter_Ham { get; set; }
        public bool? Tank_YumusakSu_SensMin_Aktif { get; set; }
        public bool? Tank_YumusakSu_SensOrta_Aktif { get; set; }
        public bool? Tank_YumusakSu_SensMax_Aktif { get; set; }
        public bool? Tank_OsmozSu_Transmitter_Active { get; set; }
        public decimal? Tank_OsmozSu_Transmitter_Value { get; set; }
        public decimal? Tank_OsmozSu_Transmitter_Ham { get; set; }
        public bool? Tank_OsmozSu_SensMin_Aktif { get; set; }
        public bool? Tank_OsmozSu_SensOrta_Aktif { get; set; }
        public bool? Tank_OsmozSu_SensMax_Aktif { get; set; }
        public bool? Tank_SicakSu_Transmitter_Active { get; set; }
        public decimal? Tank_SicakSu_Transmitter_Value { get; set; }
        public decimal? Tank_SicakSu_Transmitter_Ham { get; set; }
        public bool? Tank_SicakSu_SensMin_Aktif { get; set; }
        public bool? Tank_SicakSu_SensOrta_Aktif { get; set; }
        public bool? Tank_SicakSu_SensMax_Aktif { get; set; }
        public bool? Tank_Yangin_Transmitter_Active { get; set; }
        public decimal? Tank_Yangin_Transmitter_Value { get; set; }
        public decimal? Tank_Yangin_Transmitter_Ham { get; set; }
        public bool? Tank_Yangin_SensMin_Aktif { get; set; }
        public bool? Tank_Yangin_SensOrta_Aktif { get; set; }
        public bool? Tank_Yangin_SensMax_Aktif { get; set; }


        public bool? Tank_HamSu_SensMin_Value { get; set; }
        public bool? Tank_HamSu_SensOrta_Value { get; set; }
        public bool? Tank_HamSu_SensMax_Value { get; set; }
        public decimal? Tank_HamSu_SensBasinc { get; set; }
        public decimal? Tank_HamSu_SuSeviye { get; set; }
        public decimal? Tank_HamSu_SuSeviye_Yuzde { get; set; }
        public decimal? Tank_HamSu_SuHacim { get; set; }
        public decimal? Tank_HamSu_SuHacim_Yuzde { get; set; }
        //public decimal? Tank_1_HamSu_T_TankHacim { get; set; }

        public bool? Tank_YumusakSu_SensMin_Value { get; set; }
        public bool? Tank_YumusakSu_SensOrta_Value { get; set; }
        public bool? Tank_YumusakSu_SensMax_Value { get; set; }
        public decimal? Tank_YumusakSu_SensBasinc { get; set; }
        public decimal? Tank_YumusakSu_SuSeviye { get; set; }
        public decimal? Tank_YumusakSu_SuSeviye_Yuzde { get; set; }
        public decimal? Tank_YumusakSu_SuHacim { get; set; }
        public decimal? Tank_YumusakSu_SuHacim_Yuzde { get; set; }
        //public decimal? Tank_2_YumusakSu_T_TankHacim { get; set; }

        public bool? Tank_OsmozSu_SensMin_Value { get; set; }
        public bool? Tank_OsmozSu_SensOrta_Value { get; set; }
        public bool? Tank_OsmozSu_SensMax_Value { get; set; }
        public decimal? Tank_OsmozSu_SensBasinc { get; set; }
        public decimal? Tank_OsmozSu_SuSeviye { get; set; }
        public decimal? Tank_OsmozSu_SuSeviye_Yuzde { get; set; }
        public decimal? Tank_OsmozSu_SuHacim { get; set; }
        public decimal? Tank_OsmozSu_SuHacim_Yuzde { get; set; }
        //public decimal? Tank_3_OsmozSu_T_TankHacim { get; set; }

        public bool? Tank_SicakSu_SensMin_Value { get; set; }
        public bool? Tank_SicakSu_SensOrta_Value { get; set; }
        public bool? Tank_SicakSu_SensMax_Value { get; set; }
        public decimal? Tank_SicakSu_SensBasinc { get; set; }
        public decimal? Tank_SicakSu_SuSeviye { get; set; }
        public decimal? Tank_SicakSu_SuSeviye_Yuzde { get; set; }
        public decimal? Tank_SicakSu_SuHacim { get; set; }
        public decimal? Tank_SicakSu_SuHacim_Yuzde { get; set; }
        //public decimal? Tank_4_SicakSu_T_TankHacim { get; set; }

        public bool? Tank_Yangin_SensMin_Value { get; set; }
        public bool? Tank_Yangin_SensOrta_Value { get; set; }
        public bool? Tank_Yangin_SensMax_Value { get; set; }
        public decimal? Tank_Yangin_SensBasinc { get; set; }
        public decimal? Tank_Yangin_SuSeviye { get; set; }
        public decimal? Tank_Yangin_SuSeviye_Yuzde { get; set; }
        public decimal? Tank_Yangin_SuHacim { get; set; }
        public decimal? Tank_Yangin_SuHacim_Yuzde { get; set; }
        //public decimal? Tank_5_Yangin_T_TankHacim { get; set; }

        public bool? DM_HamSuHatti_Connected { get; set; }
        public decimal? DM_HamSuHatti_Total { get; set; }
        public decimal? DM_HamSuHatti_Debi { get; set; }

        public bool? DM_YumusakAtik_Connected { get; set; }
        public decimal? DM_YumusakAtik_Total { get; set; }
        public decimal? DM_YumusakAtik_Debi { get; set; }

        public bool? DM_YumusakSuHatti_Connected { get; set; }
        public decimal? DM_YumusakSuHatti_Total { get; set; }
        public decimal? DM_YumusakSuHatti_Debi { get; set; }

        public bool? DM_Uretim1_Connected { get; set; }
        public decimal? DM_Uretim1_Total { get; set; }
        public decimal? DM_Uretim1_Debi { get; set; }

        public bool? DM_Uretim2_Connected { get; set; }
        public decimal? DM_Uretim2_Total { get; set; }
        public decimal? DM_Uretim2_Debi { get; set; }

        public bool? DM_AracYikama_Connected { get; set; }
        public decimal? DM_AracYikama_Total { get; set; }
        public decimal? DM_AracYikama_Debi { get; set; }

        public bool? DM_Lavabolar_Connected { get; set; }
        public decimal? DM_Lavabolar_Total { get; set; }
        public decimal? DM_Lavabolar_Debi { get; set; }

        public bool? DM_OsmozGirisHatti_Connected { get; set; }
        public decimal? DM_OsmozGirisHatti_Total { get; set; }
        public decimal? DM_OsmozGirisHatti_Debi { get; set; }

        public bool? DM_ROAtikSu_Connected { get; set; }
        public decimal? DM_ROAtikSu_Total { get; set; }
        public decimal? DM_ROAtikSu_Debi { get; set; }

        public bool? DM_MembranAtikSu_Connected { get; set; }
        public decimal? DM_MembranAtikSu_Total { get; set; }
        public decimal? DM_MembranAtikSu_Debi { get; set; }

        public bool? DM_Peyzaj_Connected { get; set; }
        public decimal? DM_Peyzaj_Total { get; set; }
        public decimal? DM_Peyzaj_Debi { get; set; }

        public bool? DM_Rendering_Connected { get; set; }
        public decimal? DM_Rendering_Total { get; set; }
        public decimal? DM_Rendering_Debi { get; set; }

        public bool? DM_RingHattiDonus_Connected { get; set; }
        public decimal? DM_RingHattiDonus_Total { get; set; }
        public decimal? DM_RingHattiDonus_Debi { get; set; }

        public bool? DM_SicakSuUretim_Connected { get; set; }
        public decimal? DM_SicakSuUretim_Total { get; set; }
        public decimal? DM_SicakSuUretim_Debi { get; set; }

        public bool? Hidrofor_HamSu1_Cmd_Calis { get; set; }
        public int? Hidrofor_HamSu1_Rpr_Mode { get; set; }
        public int? Hidrofor_HamSu1_Rpr_Durum { get; set; }
        public bool? Hidrofor_HamSu1_Motor1_Calisiyor { get; set; }
        public bool? Hidrofor_HamSu1_Motor1_Ariza { get; set; }
        public int? Hidrofor_HamSu1_Motor1_Durum { get; set; }
        public bool? Hidrofor_HamSu1_Motor2_Calisiyor { get; set; }
        public bool? Hidrofor_HamSu1_Motor2_Ariza { get; set; }
        public int? Hidrofor_HamSu1_Motor2_Durum { get; set; }
        public bool? Hidrofor_HamSu1_Motor3_Calisiyor { get; set; }
        public bool? Hidrofor_HamSu1_Motor3_Ariza { get; set; }
        public int? Hidrofor_HamSu1_Motor3_Durum { get; set; }
        public decimal? Hidrofor_HamSu1_Basinc { get; set; }

        public bool? Hidrofor_HamSu2_Cmd_Calis { get; set; }
        public int? Hidrofor_HamSu2_Rpr_Mode { get; set; }
        public int? Hidrofor_HamSu2_Rpr_Durum { get; set; }
        public bool? Hidrofor_HamSu2_Motor1_Calisiyor { get; set; }
        public bool? Hidrofor_HamSu2_Motor1_Ariza { get; set; }
        public int? Hidrofor_HamSu2_Motor1_Durum { get; set; }
        public bool? Hidrofor_HamSu2_Motor2_Calisiyor { get; set; }
        public bool? Hidrofor_HamSu2_Motor2_Ariza { get; set; }
        public int? Hidrofor_HamSu2_Motor2_Durum { get; set; }
        public bool? Hidrofor_HamSu2_Motor3_Calisiyor { get; set; }
        public bool? Hidrofor_HamSu2_Motor3_Ariza { get; set; }
        public int? Hidrofor_HamSu2_Motor3_Durum { get; set; }
        public decimal? Hidrofor_HamSu2_Basinc { get; set; }

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

        public bool? Hidrofor_SicakSu_Cmd_Calis { get; set; }
        public int? Hidrofor_SicakSu_Rpr_Mode { get; set; }
        public int? Hidrofor_SicakSu_Rpr_Durum { get; set; }
        public bool? Hidrofor_SicakSu_Motor1_Calisiyor { get; set; }
        public bool? Hidrofor_SicakSu_Motor1_Ariza { get; set; }
        public int? Hidrofor_SicakSu_Motor1_Durum { get; set; }
        public bool? Hidrofor_SicakSu_Motor2_Calisiyor { get; set; }
        public bool? Hidrofor_SicakSu_Motor2_Ariza { get; set; }
        public int? Hidrofor_SicakSu_Motor2_Durum { get; set; }
        public bool? Hidrofor_SicakSu_Motor3_Calisiyor { get; set; }
        public bool? Hidrofor_SicakSu_Motor3_Ariza { get; set; }
        public int? Hidrofor_SicakSu_Motor3_Durum { get; set; }
        public decimal? Hidrofor_SicakSu_Basinc { get; set; }

        public bool? Hidrofor_YanginSu_Cmd_Calis { get; set; }
        public int? Hidrofor_YanginSu_Rpr_Mode { get; set; }
        public int? Hidrofor_YanginSu_Rpr_Durum { get; set; }
        public bool? Hidrofor_YanginSu_Motor1_Calisiyor { get; set; }
        public bool? Hidrofor_YanginSu_Motor1_Ariza { get; set; }
        public int? Hidrofor_YanginSu_Motor1_Durum { get; set; }
        public bool? Hidrofor_YanginSu_Motor2_Calisiyor { get; set; }
        public bool? Hidrofor_YanginSu_Motor2_Ariza { get; set; }
        public int? Hidrofor_YanginSu_Motor2_Durum { get; set; }
        public bool? Hidrofor_YanginSu_Motor3_Calisiyor { get; set; }
        public bool? Hidrofor_YanginSu_Motor3_Ariza { get; set; }
        public int? Hidrofor_YanginSu_Motor3_Durum { get; set; }
        public decimal? Hidrofor_YanginSu_Basinc { get; set; }
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
        public bool? Alarm_33 { get; set; }
        public bool? Alarm_34 { get; set; }
        public bool? Alarm_35 { get; set; }
        public bool? Alarm_36 { get; set; }
        public bool? Alarm_37 { get; set; }
        public bool? Alarm_38 { get; set; }
        public bool? Alarm_39 { get; set; }
        public bool? Alarm_40 { get; set; }
        public bool? Alarm_41 { get; set; }
        public bool? Alarm_42 { get; set; }
        public bool? Alarm_43 { get; set; }
        public bool? Alarm_44 { get; set; }
        public bool? Alarm_45 { get; set; }
        public bool? Alarm_46 { get; set; }
        public bool? Alarm_47 { get; set; }
        public bool? Alarm_48 { get; set; }
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
