namespace WaterMonitoring.Worker.Models
{
    public class SuIslemeNodeRedModel
    {
        public int? Head { get; set; }
        public DateTime? TimeNow { get; set; }
        public bool? DM_OsmozBesleme_Connected { get; set; }
        public decimal? DM_OsmozBesleme_Total { get; set; }
        public decimal? DM_OsmozBesleme_Debi { get; set; }

        public bool? DM_Kesimhane1ve2Uretim_Connected { get; set; }
        public decimal? DM_Kesimhane1ve2Uretim_Total { get; set; }
        public decimal? DM_Kesimhane1ve2Uretim_Debi { get; set; }

        public bool? DM_Kesimhane3_Connected { get; set; }
        public decimal? DM_Kesimhane3_Total { get; set; }
        public decimal? DM_Kesimhane3_Debi { get; set; }

        public bool? DM_YumusatmaBesleme_Connected { get; set; }
        public decimal? DM_YumusatmaBesleme_Total { get; set; }
        public decimal? DM_YumusatmaBesleme_Debi { get; set; }

        public bool? DM_YumusatmaTank1_Connected { get; set; }
        public decimal? DM_YumusatmaTank1_Total { get; set; }
        public decimal? DM_YumusatmaTank1_Debi { get; set; }

        public bool? DM_YumusatmaTank2_Connected { get; set; }
        public decimal? DM_YumusatmaTank2_Total { get; set; }
        public decimal? DM_YumusatmaTank2_Debi { get; set; }

        public bool? DM_YumusatmaTank3_Connected { get; set; }
        public decimal? DM_YumusatmaTank3_Total { get; set; }
        public decimal? DM_YumusatmaTank3_Debi { get; set; }

        public bool? DM_YumusatmaTank4_Connected { get; set; }
        public decimal? DM_YumusatmaTank4_Total { get; set; }
        public decimal? DM_YumusatmaTank4_Debi { get; set; }

        public bool? DM_YumusakAtik_Connected { get; set; }
        public decimal? DM_YumusakAtik_Total { get; set; }
        public decimal? DM_YumusakAtik_Debi { get; set; }

        public bool? DM_YumusakHatti_Connected { get; set; }
        public decimal? DM_YumusakHatti_Total { get; set; }
        public decimal? DM_YumusakHatti_Debi { get; set; }

        public bool? DM_Rendering_Connected { get; set; }
        public decimal? DM_Rendering_Total { get; set; }
        public decimal? DM_Rendering_Debi { get; set; }

        public bool? DM_YumusakSuCikisi_Connected { get; set; }
        public decimal? DM_YumusakSuCikisi_Total { get; set; }
        public decimal? DM_YumusakSuCikisi_Debi { get; set; }

        public bool? Hidrofor_Ham_Su_1_Cmd_Calis { get; set; }
        public int? Hidrofor_Ham_Su_1_Rpr_Durum { get; set; }
        public int? Hidrofor_Ham_Su_1_Rpr_Mode { get; set; }
        public bool? Hidrofor_Ham_Su_1_Motor1_Calisiyor { get; set; }
        public bool? Hidrofor_Ham_Su_1_Motor1_Ariza { get; set; }
        public int? Hidrofor_Ham_Su_1_Motor1_Durum { get; set; }
        public bool? Hidrofor_Ham_Su_1_Motor2_Calisiyor { get; set; }
        public bool? Hidrofor_Ham_Su_1_Motor2_Ariza { get; set; }
        public int? Hidrofor_Ham_Su_1_Motor2_Durum { get; set; }
        public bool? Hidrofor_Ham_Su_1_Motor3_Calisiyor { get; set; }
        public bool? Hidrofor_Ham_Su_1_Motor3_Ariza { get; set; }
        public int? Hidrofor_Ham_Su_1_Motor3_Durum { get; set; }
        public decimal? Hidrofor_Ham_Su_1_CikisBasinci { get; set; }

        public bool? Hidrofor_Ham_Su_2_Cmd_Calis { get; set; }
        public int? Hidrofor_Ham_Su_2_Rpr_Durum { get; set; }
        public int? Hidrofor_Ham_Su_2_Rpr_Mode { get; set; }
        public bool? Hidrofor_Ham_Su_2_Motor1_Calisiyor { get; set; }
        public bool? Hidrofor_Ham_Su_2_Motor1_Ariza { get; set; }
        public int? Hidrofor_Ham_Su_2_Motor1_Durum { get; set; }
        public bool? Hidrofor_Ham_Su_2_Motor2_Calisiyor { get; set; }
        public bool? Hidrofor_Ham_Su_2_Motor2_Ariza { get; set; }
        public int? Hidrofor_Ham_Su_2_Motor2_Durum { get; set; }
        public bool? Hidrofor_Ham_Su_2_Motor3_Calisiyor { get; set; }
        public bool? Hidrofor_Ham_Su_2_Motor3_Ariza { get; set; }
        public int? Hidrofor_Ham_Su_2_Motor3_Durum { get; set; }
        public decimal? Hidrofor_Ham_Su_2_CikisBasinci { get; set; }

        public bool? Hidrofor_YumusakSu_Cmd_Calis { get; set; }
        public int? Hidrofor_YumusakSu_Rpr_Durum { get; set; }
        public int? Hidrofor_YumusakSu_Rpr_Mode { get; set; }
        public bool? Hidrofor_YumusakSu_Motor1_Calisiyor { get; set; }
        public bool? Hidrofor_YumusakSu_Motor1_Ariza { get; set; }
        public int? Hidrofor_YumusakSu_Motor1_Durum { get; set; }
        public bool? Hidrofor_YumusakSu_Motor2_Calisiyor { get; set; }
        public bool? Hidrofor_YumusakSu_Motor2_Ariza { get; set; }
        public int? Hidrofor_YumusakSu_Motor2_Durum { get; set; }
        public bool? Hidrofor_YumusakSu_Motor3_Calisiyor { get; set; }
        public bool? Hidrofor_YumusakSu_Motor3_Ariza { get; set; }
        public int? Hidrofor_YumusakSu_Motor3_Durum { get; set; }
        public bool? Hidrofor_YumusakSu_Motor4_Calisiyor { get; set; }
        public bool? Hidrofor_YumusakSu_Motor4_Ariza { get; set; }
        public int? Hidrofor_YumusakSu_Motor4_Durum { get; set; }
        public decimal? Hidrofor_YumusakSu_CikisBasinci { get; set; }
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
