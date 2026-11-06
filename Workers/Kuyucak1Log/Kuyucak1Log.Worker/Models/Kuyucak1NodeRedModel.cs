namespace Kuyucak1Log.Worker.Models
{
    /// <summary>
    /// OTM_KumesScada.dbo.KumesIzlemeNodeRed.Veriler alanına Node-RED tarafından
    /// yazılan Kuyucak-1 JSON sözleşmesi.
    ///
    /// ÖNEMLİ: System.Text.Json varsayılanında property isimleri birebir eşleşir.
    /// Node-RED JSON key'leri bu property adlarıyla aynı tutulmalıdır.
    /// </summary>
    public class Kuyucak1NodeRedModel
    {
        // Genel
        public short? Head { get; set; }
        public DateTime? TimeNow { get; set; }

        // Sıcaklık
        public short? Sicaklik_AktifSensSayisi { get; set; }
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
        public decimal? Sicaklik_Aperey_Min_Olculen { get; set; }
        public decimal? Sicaklik_Aperey_Max_Olculen { get; set; }

        // Nem
        public short? Nem_AktifSensSayisi { get; set; }
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

        // İçme suyu
        public byte? IcmeSuyu_VanaAc { get; set; }
        public byte? IcmeSuyu_Sayac_Aktif { get; set; }
        public short? IcmeSuyu_Vana_State { get; set; }
        public decimal? IcmeSuyu_Sayac { get; set; }
        public decimal? IcmeSuyu_Tuketim_Bugun { get; set; }
        public decimal? IcmeSuyu_GunSonuSayac_Dun { get; set; }
        public decimal? IcmeSuyu_Tuketim_Dun { get; set; }

        // Yemleme / silo
        public bool? Yemleme_Aktif { get; set; }
        public decimal? Yemleme_Silo_Value { get; set; }
        public decimal? Yemleme_Tuketim_Bugun { get; set; }
        public decimal? Yemleme_VarlikOran { get; set; }
        public decimal? Yemleme_Kumes_1_Tuketim_Bugun { get; set; }
        public decimal? Yemleme_Kumes_2_Tuketim_Bugun { get; set; }

        // Fanlar
        public byte? MFan_1_State { get; set; }
        public byte? MFan_2_State { get; set; }
        public byte? MFan_3_State { get; set; }
        public byte? TFan_1_State { get; set; }
        public byte? TFan_2_State { get; set; }
        public byte? TFan_3_State { get; set; }
        public byte? TFan_4_State { get; set; }
        public byte? TFan_5_State { get; set; }
        public byte? TFan_6_State { get; set; }
        public byte? Havalandirma_Mode { get; set; }

        // Ped
        public byte? Ped_Pompa_1_State { get; set; }
        public byte? Ped_Pompa_2_State { get; set; }

        // Buffel
        public byte? Buffel_Durum { get; set; }

        // Aydınlatma
        public short? Aydinlatma_State { get; set; }

        // Soba / Aperey
        public byte? Soba_Aperey_1_State { get; set; }
        public byte? Soba_Aperey_2_State { get; set; }
        public byte? Soba_Aperey_3_State { get; set; }
        public decimal? Soba_Hat_Sicakligi { get; set; }

        // Elektrik
        public bool? SebekedeElektrikVar { get; set; }

        // Alarmlar - AlarmEslemeSozcugu ile birebir aynı JSON key'leri
        public bool? Kuyucak1_Alarm_Basinc_Ust_Limit { get; set; }
        public bool? Kuyucak1_Alarm_Basinc_Alt_Limit { get; set; }
        public bool? Kuyucak1_Alarm_CO2_Limit { get; set; }
        public bool? Kuyucak1_Alarm_Aktif_Sensor_Sayisi_Az { get; set; }
        public bool? Kuyucak1_Alarm_Tunel_Fan_TermikNOK { get; set; }
        public bool? Kuyucak1_Alarm_Mini_Fan_TermikNOK { get; set; }
        public bool? Kuyucak1_Alarm_Ped_Pompa_TermikNOK { get; set; }
        public bool? Kuyucak1_Alarm_Nem_Ust_Limit { get; set; }
        public bool? Kuyucak1_Alarm_Nem_Alt_Limit { get; set; }
        public bool? Kuyucak1_Alarm_TempHum_1_Aktif_Degil { get; set; }
        public bool? Kuyucak1_Alarm_TempHum_2_Aktif_Degil { get; set; }
        public bool? Kuyucak1_Alarm_TempHum_3_Aktif_Degil { get; set; }
        public bool? Kuyucak1_Alarm_TempHum_4_Aktif_Degil { get; set; }
        public bool? Kuyucak1_Alarm_Dis_TempHum_Aktif_Degil { get; set; }
        public bool? Kuyucak1_Alarm_Basinc_Aktif_Degil { get; set; }
        public bool? Kuyucak1_Alarm_CO2_Aktif_Degil { get; set; }
        public bool? Kuyucak1_Alarm_IcmeSuyu_Aktif_Degil { get; set; }
        public bool? Kuyucak1_Alarm_Sebeke_Yok { get; set; }
        public bool? Kuyucak1_Alarm_TFan_1_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_TFan_2_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_TFan_3_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_TFan_4_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_TFan_5_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_TFan_6_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_MFan_1_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_MFan_2_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_MFan_3_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_Ped_Pompa_Sol_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_Ped_Pompa_Sag_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_Buffel_Acilmiyor { get; set; }
        public bool? Kuyucak1_Alarm_Buffel_Kapanmiyor { get; set; }
        public bool? Kuyucak1_Alarm_Buffel_TermikNOK { get; set; }
        public bool? Kuyucak1_Alarm_Aydinlatma_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_Aperey_1_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_Aperey_2_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_Aperey_3_Calismiyor { get; set; }
        public bool? Kuyucak1_Alarm_Aperey_TermikNOK { get; set; }
        public bool? Kuyucak1_Alarm_Buffel_Sensor_Ariza { get; set; }
        public bool? Kuyucak1_Alarm_Ara_Silo_Baglanti_Koptu { get; set; }
        public bool? Kuyucak1_Alarm_Sicaklik_Sensor_1_Degeri_Dusuk { get; set; }
        public bool? Kuyucak1_Alarm_Sicaklik_Sensor_1_Degeri_Yuksek { get; set; }
        public bool? Kuyucak1_Alarm_Sicaklik_Sensor_2_Degeri_Dusuk { get; set; }
        public bool? Kuyucak1_Alarm_Sicaklik_Sensor_2_Degeri_Yuksek { get; set; }
        public bool? Kuyucak1_Alarm_Sicaklik_Sensor_3_Degeri_Dusuk { get; set; }
        public bool? Kuyucak1_Alarm_Sicaklik_Sensor_3_Degeri_Yuksek { get; set; }
        public bool? Kuyucak1_Alarm_Sicaklik_Sensor_4_Degeri_Dusuk { get; set; }
        public bool? Kuyucak1_Alarm_Sicaklik_Sensor_4_Degeri_Yuksek { get; set; }
        public bool? Kuyucak1_Alarm_Sicaklik_Sensor_Dis_Degeri_Dusuk { get; set; }
        public bool? Kuyucak1_Alarm_Sicaklik_Sensor_Dis_Degeri_Yuksek { get; set; }
        public bool? Kuyucak1_Alarm_Ortalama_Sicaklik_Degeri_Dusuk { get; set; }
        public bool? Kuyucak1_Alarm_Ortalama_Sicaklik_Degeri_Yuksek { get; set; }

        // Şimdilik anlamı tanımlanmamış reserve alarm bitleri
        public bool? Reserve_3 { get; set; }
        public bool? Reserve_4 { get; set; }
        public bool? Reserve_5 { get; set; }
        public bool? Reserve_6 { get; set; }
        public bool? Reserve_7 { get; set; }
        public bool? Reserve_8 { get; set; }
        public bool? Reserve_9 { get; set; }
        public bool? Reserve_10 { get; set; }
        public bool? Reserve_11 { get; set; }
        public bool? Reserve_12 { get; set; }
        public bool? Reserve_13 { get; set; }
        public bool? Reserve_14 { get; set; }
        public bool? Reserve_15 { get; set; }
        public bool? Reserve_16 { get; set; }
        public bool? Reserve_17 { get; set; }
        public bool? Reserve_18 { get; set; }
        public bool? Reserve_19 { get; set; }
        public bool? Reserve_20 { get; set; }
    }
}
