namespace WaterMonitoringLog.Application.Models
{
    public class DogalgazliKazanNodeRedModel
    {
        public int? Head { get; set; }
        public DateTime? TimeNow { get; set; }


        public bool? Tank_1_Transmitter_Active { get; set; }
        public decimal? Tank_1_Transmitter_Value { get; set; }
        public decimal? Tank_1_Transmitter_Ham { get; set; }
        public bool? Tank_1_SensMin_Aktif { get; set; }
        public bool? Tank_1_SensOrta_Aktif { get; set; }
        public bool? Tank_1_SensMax_Aktif { get; set; }



        public bool? Tank_1_SensMin_Value { get; set; }
        public bool? Tank_1_SensOrta_Value { get; set; }
        public bool? Tank_1_SensMax_Value { get; set; }
        public decimal? Tank_1_SensBasinc { get; set; }
        public decimal? Tank_1_SuSeviye { get; set; }
        public decimal? Tank_1_SuSeviye_Yuzde { get; set; }
        public decimal? Tank_1_SuHacim { get; set; }
        public decimal? Tank_1_SuHacim_Yuzde { get; set; }
        //public decimal? Tank_1_T_TankHacim { get; set; }

        public bool? DM_ROAtikSu_Connected { get; set; }
        public decimal? DM_ROAtikSu_Total { get; set; }
        public decimal? DM_ROAtikSu_Debi { get; set; }

        public bool? DM_ROKumFiltresi_Connected { get; set; }
        public decimal? DM_ROKumFiltresi_Total { get; set; }
        public decimal? DM_ROKumFiltresi_Debi { get; set; }

        public bool? DM_OsmozSuTakiGirisi_Connected { get; set; }
        public decimal? DM_OsmozSuTakiGirisi_Total { get; set; }
        public decimal? DM_OsmozSuTakiGirisi_Debi { get; set; }

        public bool? DM_OsmozUretimHatti_Connected { get; set; }
        public decimal? DM_OsmozUretimHatti_Total { get; set; }
        public decimal? DM_OsmozUretimHatti_Debi { get; set; }

        public bool? Uretim_Atik_Sert_Su_Sayaci_Connected { get; set; }
        public decimal? Uretim_Atik_Sert_Su_Sayaci_Total { get; set; }
        public decimal? Uretim_Atik_Sert_Su_Sayaci_Debi { get; set; }

        public bool? DM_Yedek2_Connected { get; set; }
        public decimal? DM_Yedek2_Total { get; set; }
        public decimal? DM_Yedek2_Debi { get; set; }

        public bool? Hidrofor_Osmoz_Cmd_Calis { get; set; }
        public int? Hidrofor_Osmoz_Rpr_Mode { get; set; }
        public int? Hidrofor_Osmoz_Rpr_Durum { get; set; }
        public bool? Hidrofor_Osmoz_Motor1_Calisiyor { get; set; }
        public bool? Hidrofor_Osmoz_Motor1_Ariza { get; set; }
        public int? Hidrofor_Osmoz_Motor1_Durum { get; set; }
        public bool? Hidrofor_Osmoz_Motor2_Calisiyor { get; set; }
        public bool? Hidrofor_Osmoz_Motor2_Ariza { get; set; }
        public int? Hidrofor_Osmoz_Motor2_Durum { get; set; }
        public bool? Hidrofor_Osmoz_Motor3_Calisiyor { get; set; }
        public bool? Hidrofor_Osmoz_Motor3_Ariza { get; set; }
        public int? Hidrofor_Osmoz_Motor3_Durum { get; set; }
        public bool? Hidrofor_Osmoz_Motor4_Calisiyor { get; set; }
        public bool? Hidrofor_Osmoz_Motor4_Ariza { get; set; }
        public int? Hidrofor_Osmoz_Motor4_Durum { get; set; }
        //public decimal? Hidrofor_Osmoz_Basinc { get; set; }
        public bool? SebekedeElektrikVar { get; set; }

        public bool? Sistem_Besleme_Pompasi_1_Start { get; set; }
        public bool? Sistem_Besleme_Pompasi_2_Start { get; set; }
        public bool? Urun_Hatti_Pompasi_1_Start { get; set; }
        public bool? Urun_Hatti_Pompasi_2_Start { get; set; }
        public bool? Sistem_Besleme_Pompasi_1_Devrede { get; set; }
        public bool? Sistem_Besleme_Pompasi_1_Arizada { get; set; }
        public bool? Sistem_Besleme_Pompasi_2_Devrede { get; set; }
        public bool? Sistem_Besleme_Pompasi_2_Arizada { get; set; }
        public bool? Urun_Hatti_Pompasi_1_Devrede { get; set; }
        public bool? Urun_Hatti_Pompasi_1_Arizada { get; set; }
        public bool? Urun_Hatti_Pompasi_2_Devrede { get; set; }
        public bool? Urun_Hatti_Pompasi_2_Arizada { get; set; }

        public bool? Reject_Stok_Tanki_Transmitter_Active { get; set; }
        public decimal? Reject_Stok_Tanki_Transmitter_Value { get; set; }
        public decimal? Reject_Stok_Tanki_Transmitter_Ham { get; set; }
        public bool? Reject_Stok_Tanki_SensMin_Aktif { get; set; }
        public bool? Reject_Stok_Tanki_SensOrta_Aktif { get; set; }
        public bool? Reject_Stok_Tanki_SensMax_Aktif { get; set; }
        public bool? Reject_Stok_Tanki_SensMin_Value { get; set; }
        public bool? Reject_Stok_Tanki_SensOrta_Value { get; set; }
        public bool? Reject_Stok_Tanki_SensMax_Value { get; set; }
        public decimal? Reject_Stok_Tanki_SensBasinc { get; set; }
        public decimal? Reject_Stok_Tanki_SuSeviye { get; set; }
        public decimal? Reject_Stok_Tanki_SuSeviye_Yuzde { get; set; }
        public decimal? Reject_Stok_Tanki_SuHacim { get; set; }
        public decimal? Reject_Stok_Tanki_SuHacim_Yuzde { get; set; }

        public bool? Proses_Suyu_Depolama_Tanki_Transmitter_Active { get; set; }
        public decimal? Proses_Suyu_Depolama_Tanki_Transmitter_Value { get; set; }
        public decimal? Proses_Suyu_Depolama_Tanki_Transmitter_Ham { get; set; }
        public bool? Proses_Suyu_Depolama_Tanki_SensMin_Aktif { get; set; }
        public bool? Proses_Suyu_Depolama_Tanki_SensOrta_Aktif { get; set; }
        public bool? Proses_Suyu_Depolama_Tanki_SensMax_Aktif { get; set; }
        public bool? Proses_Suyu_Depolama_Tanki_SensMin_Value { get; set; }
        public bool? Proses_Suyu_Depolama_Tanki_SensOrta_Value { get; set; }
        public bool? Proses_Suyu_Depolama_Tanki_SensMax_Value { get; set; }
        public decimal? Proses_Suyu_Depolama_Tanki_SensBasinc { get; set; }
        public decimal? Proses_Suyu_Depolama_Tanki_SuSeviye { get; set; }
        public decimal? Proses_Suyu_Depolama_Tanki_SuSeviye_Yuzde { get; set; }
        public decimal? Proses_Suyu_Depolama_Tanki_SuHacim { get; set; }
        public decimal? Proses_Suyu_Depolama_Tanki_SuHacim_Yuzde { get; set; }

        public int? Sistem_Besleme_Pompasi_1_Durum { get; set; }
        public int? Sistem_Besleme_Pompasi_2_Durum { get; set; }
        public int? Urun_Hatti_Pompasi_1_Durum { get; set; }
        public int? Urun_Hatti_Pompasi_2_Durum { get; set; }

        public bool? DM_Osmoz_Kazan_Dairesi_Connected { get; set; }
        public decimal? DM_Osmoz_Kazan_Dairesi_Total { get; set; }
        public decimal? DM_Osmoz_Kazan_Dairesi_Debi { get; set; }

        public bool? DM_Osmoz_Makine_Dairesi_Connected { get; set; }
        public decimal? DM_Osmoz_Makine_Dairesi_Total { get; set; }
        public decimal? DM_Osmoz_Makine_Dairesi_Debi { get; set; }


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
