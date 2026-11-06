namespace Kayislar1Log.Worker.Models
{
    public class KumesNodeRedModel
    {
        // Genel
        public short? Head { get; set; }
        public DateTime? TimeNow { get; set; }


        // Sıcaklık
        public byte? Sicaklik_Dis_Aktif { get; set; }
        public byte? Sicaklik_Sensor_1_Aktif { get; set; }
        public byte? Sicaklik_Sensor_2_Aktif { get; set; }
        public byte? Sicaklik_Sensor_3_Aktif { get; set; }
        public byte? Sicaklik_Sensor_4_Aktif { get; set; }

        public decimal? Sicaklik_Kumes_Ortalama { get; set; }
        public decimal? Sicaklik_Dis { get; set; }
        public decimal? Sicaklik_Sensor_1_Value { get; set; }
        public decimal? Sicaklik_Sensor_2_Value { get; set; }
        public decimal? Sicaklik_Sensor_3_Value { get; set; }
        public decimal? Sicaklik_Sensor_4_Value { get; set; }


        // Nem
        public byte? Nem_Kumes_Aktif { get; set; }
        public byte? Nem_Dis_Aktif { get; set; }
        public byte? Nem_Sensor_1_Aktif { get; set; }
        public byte? Nem_Sensor_2_Aktif { get; set; }
        public byte? Nem_Sensor_3_Aktif { get; set; }
        public byte? Nem_Sensor_4_Aktif { get; set; }

        public decimal? Nem_Kumes_Ortalama { get; set; }
        public decimal? Nem_Dis { get; set; }
        public decimal? Nem_Sensor_1_Value { get; set; }
        public decimal? Nem_Sensor_2_Value { get; set; }
        public decimal? Nem_Sensor_3_Value { get; set; }
        public decimal? Nem_Sensor_4_Value { get; set; }


        // Basınç
        public byte? Basinc_Sens_Aktif { get; set; }
        public decimal? Basinc_Sens_Value { get; set; }


        // CO2
        public byte? CO2_Sens_Aktif { get; set; }
        public decimal? CO2_Sens_Value { get; set; }


        // İçme Suyu
        public byte? IcmeSuyu_Sayac_Aktif { get; set; }
        public decimal? IcmeSuyu_Sayac { get; set; }
        public decimal? IcmeSuyu_Tuketim_Bugun { get; set; }
        public decimal? IcmeSuyu_Tuketim_Dun { get; set; }


        // Yem Silo 1
        public byte? Yem_Silo_1_Aktif { get; set; }
        public decimal? Yem_Silo_1_Silo_Value { get; set; }
        public decimal? Yem_Silo_1_Silo_Tuketim_Bugun { get; set; }
        public decimal? Yem_Silo_1_Silo_VarlikOran { get; set; }


        // Yemleme Bölge 1
        public byte? Yemleme_Bolge_1_ZincirState { get; set; }
        public decimal? Yemleme_Bolge_1_YemSet { get; set; }
        public decimal? Yemleme_Bolge_1_YemGerceklesen { get; set; }


        // Yemleme Bölge 2
        public byte? Yemleme_Bolge_2_ZincirState { get; set; }
        public decimal? Yemleme_Bolge_2_YemSet { get; set; }
        public decimal? Yemleme_Bolge_2_YemGerceklesen { get; set; }


        // Yemleme Bölge 3
        public byte? Yemleme_Bolge_3_ZincirState { get; set; }
        public decimal? Yemleme_Bolge_3_YemSet { get; set; }
        public decimal? Yemleme_Bolge_3_YemGerceklesen { get; set; }


        // Yemleme Genel
        public byte? Yemleme_Helezon_State { get; set; }
        public decimal? Yemleme_Kantar_Aktuel { get; set; }


        // Fanlar
        public byte? Mini_Fan_1_State { get; set; }
        public byte? Mini_Fan_2_State { get; set; }
        public byte? Mini_Fan_3_State { get; set; }
        public byte? Mini_Fan_4_State { get; set; }

        public byte? Fan_1_State { get; set; }
        public byte? Fan_2_State { get; set; }
        public byte? Fan_3_State { get; set; }
        public byte? Fan_4_State { get; set; }
        public byte? Fan_5_State { get; set; }
        public byte? Fan_6_State { get; set; }
        public byte? Fan_7_State { get; set; }
        public byte? Fan_8_State { get; set; }

        public byte? Havalandirma_Mode { get; set; }


        // Ped
        public byte? Ped_Pompa_1_State { get; set; }
        public byte? Ped_Pompa_2_State { get; set; }

        public decimal? Ped_Kapak_1_Oran { get; set; }
        public decimal? Ped_Kapak_2_Oran { get; set; }

        public byte? Ped_Su_Sayac_Aktif { get; set; }
        public decimal? Ped_Su_Sayac_Value { get; set; }
        public decimal? Ped_Su_Tuketim_Bugun { get; set; }
        public decimal? Ped_Su_Tuketim_Dun { get; set; }


        // Buffel
        public decimal? Buffel_Kapak_Sag_Oran { get; set; }
        public decimal? Buffel_Kapak_Sol_Oran { get; set; }


        // Aydınlatma
        public byte? Aydinlatma_State { get; set; }


        // Soba
        public byte? Soba_State { get; set; }


        // Elektrik
        public bool? SebekedeElektrikVar { get; set; }


        // Sıcaklık Alarmları
        public bool? Alarm_Sicaklik_AktifSensSayisiAz { get; set; }
        public bool? Alarm_Sicaklik_DusukKumes { get; set; }
        public bool? Alarm_Sicaklik_FazlaKumes { get; set; }

        public bool? Alarm_Sicaklik_DusukSens1 { get; set; }
        public bool? Alarm_Sicaklik_DusukSens2 { get; set; }
        public bool? Alarm_Sicaklik_DusukSens3 { get; set; }
        public bool? Alarm_Sicaklik_DusukSens4 { get; set; }
        public bool? Alarm_Sicaklik_DusukSens5 { get; set; }
        public bool? Alarm_Sicaklik_DusukSens6 { get; set; }

        public bool? Alarm_Sicaklik_FazlaSens1 { get; set; }
        public bool? Alarm_Sicaklik_FazlaSens2 { get; set; }
        public bool? Alarm_Sicaklik_FazlaSens3 { get; set; }
        public bool? Alarm_Sicaklik_FazlaSens4 { get; set; }
        public bool? Alarm_Sicaklik_FazlaSens5 { get; set; }
        public bool? Alarm_Sicaklik_FazlaSens6 { get; set; }


        // Nem Alarmları
        public bool? Alarm_Nem_AktifSensSayisiAz { get; set; }


        // CO2 Alarmları
        public bool? Alarm_CO2_AktifDegil { get; set; }
        public bool? Alarm_CO2_Fazla { get; set; }


        // Basınç Alarmları
        public bool? Alarm_Basinc_AktifDegil { get; set; }
        public bool? Alarm_Basinc_Fazla { get; set; }


        // İçme Suyu Alarmı
        public bool? Alarm_IcmeSuyuSayac_AktifDegil { get; set; }


        // Fan Alarmları
        public bool? Alarm_Fan_TermikNok_Mini { get; set; }
        public bool? Alarm_Fan_TermikNok_Tunel { get; set; }


        // Ped Alarmı
        public bool? Alarm_Ped_PompaTermikNok { get; set; }


        // Buffel
        public short? Alarm_Buffel_Reserve_1 { get; set; }


        // Silo Alarmı
        public bool? Alarm_Yem_Silo_1_BaglantiKoptu { get; set; }


        // Yemleme Alarmı
        public bool? Alarm_Yemleme_TermikNok_Zincirler { get; set; }


        // Kantar Alarmları
        public bool? Alarm_Kantar_KapakAcmaHatasi { get; set; }
        public bool? Alarm_Kantar_KapakKapatmaHatasi { get; set; }
        public bool? Alarm_Kantar_YemDolumHatasi { get; set; }
        public bool? Alarm_Kantar_YemDagitimHatasi { get; set; }

        public bool? Alarm_Kantar_BolgeyeDagitimSuresiGirilmediHatasi { get; set; }
        public bool? Alarm_Kantar_MinKapasiteGirilmediHatasi { get; set; }
        public bool? Alarm_Kantar_OtomatikBaslatmaKapakAcikHatasi { get; set; }
        public bool? Alarm_Kantar_MotorTermikNOK { get; set; }

        public bool? Alarm_Kantar_Hat_1_YemSensor_1_Hatasi { get; set; }
        public bool? Alarm_Kantar_KantarYemDolumSonsor_3_Hatasi { get; set; }


        public bool? Reserve_YemlemeUpDownTermikNOK { get; set; }
        public bool? Reserve_NemDegeri_Yuksek { get; set; }
        public bool? Reserve_PedSayacAktifDegil { get; set; }
        public bool? Reserve_Fan_1_CalismasiGerekirkenCalismiyor { get; set; }
        public bool? Reserve_Fan_2_CalismasiGerekirkenCalismiyor { get; set; }
        public bool? Reserve_Fan_3_CalismasiGerekirkenCalismiyor { get; set; }
        public bool? Reserve_Fan_4_CalismasiGerekirkenCalismiyor { get; set; }
        public bool? Reserve_Fan_5_CalismasiGerekirkenCalismiyor { get; set; }
        public bool? Reserve_Fan_6_CalismasiGerekirkenCalismiyor { get; set; }
        public bool? Reserve_Fan_7_CalismasiGerekirkenCalismiyor { get; set; }
        public bool? Reserve_Fan_8_CalismasiGerekirkenCalismiyor { get; set; }
        public bool? Reserve_MFan_1_CalismasiGerekirkenCalismiyor { get; set; }
        public bool? Reserve_MFan_2_CalismasiGerekirkenCalismiyor { get; set; }
        public bool? Reserve_MFan_3_CalismasiGerekirkenCalismiyor { get; set; }
        public bool? Reserve_MFan_4_CalismasiGerekirkenCalismiyor { get; set; }
        public bool? Reserve_Ped_Pompa_1_CalismasiGerekirkenCalismiyor { get; set; }
        public bool? Reserve_Ped_Pompa_2_CalismasiGerekirkenCalismiyor { get; set; }
        public bool? Reserve_Ped_Kapak_Sag_AcilmasiGerekirkenAcilmiyor { get; set; }
        public bool? Reserve_Ped_Kapak_Sag_KapanmasiGerekirkenKapanmiyor { get; set; }
        public bool? Reserve_Ped_Kapak_Sol_AcilmasiGerekirkenAcilmiyor { get; set; }
        public bool? Reserve_Ped_Kapak_Sol_KapanmasiGerekirkenKapanmiyor { get; set; }
        public bool? Reserve_Buffel_Kapak_Sag_AcilmasiGerekirkenAcilmiyor { get; set; }
        public bool? Reserve_Buffel_Kapak_Sag_KapanmasiGerekirkenKapanmiyor { get; set; }
        public bool? Reserve_Buffel_Kapak_Sol_AcilmasiGerekirkenAcilmiyor { get; set; }
        public bool? Reserve_Buffel_Kapak_Sol_KapanmasiGerekirkenKapanmiyor { get; set; }

        public bool? Yemleme_YemDagitimMotorCalisiyor { get; set; }
        public bool? Yemleme_Bolge_1_Bitti { get; set; }
        public bool? Yemleme_Bolge_2_Bitti { get; set; }
        public bool? Yemleme_Bolge_3_Bitti { get; set; }
        public bool? Yemleme_Reserve_1 { get; set; }
        public bool? Yemleme_Reserve_2 { get; set; }
        public bool? Yemleme_Reserve_3 { get; set; }
        public bool? Yemleme_Reserve_4 { get; set; }

        // Reserve Alanları      
        public bool? Reserve_26 { get; set; }
        public bool? Reserve_27 { get; set; }
        public bool? Reserve_28 { get; set; }
        public bool? Reserve_29 { get; set; }
        public bool? Reserve_30 { get; set; }
        public bool? Reserve_31 { get; set; }
        public bool? Reserve_32 { get; set; }

        // Son
        public short? End { get; set; }
    }
}
