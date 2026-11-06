namespace VerimGidaPiKumesWorker.Models
{
    public class KumesNodeRedModel
    {
        public int? Head { get; set; }
        public DateTime? TimeNow { get; set; }

        // Sıcaklık
        public decimal? Sicaklik_Kumes_Value { get; set; }
        public decimal? Sicaklik_Dis_Value { get; set; }
        public decimal? Sicaklik_Sensor_1_Value { get; set; }
        public decimal? Sicaklik_Sensor_2_Value { get; set; }
        public decimal? Sicaklik_Sensor_3_Value { get; set; }
        public decimal? Sicaklik_Sensor_4_Value { get; set; }
        public decimal? Sicaklik_Sensor_5_Value { get; set; }

        // Nem ve basınç
        public decimal? Nem_Kumes_Value { get; set; }
        public decimal? Nem_Sensor_1_Value { get; set; }
        public decimal? Basinc_Sens_Value { get; set; }

        // Yem siloları
        public decimal? Yem_Silo_1_Value { get; set; }
        public decimal? Yem_Silo_2_Value { get; set; }

        // İçme suyu
        public decimal? IcmeSuyu_Sayac { get; set; }
        public decimal? IcmeSuyu_TuketimDun { get; set; }
        public decimal? IcmeSuyu_TuketimBugun { get; set; }
        public decimal? IcmeSuyu_Toplam { get; set; }

        //Yem
        public decimal? Yem_TuketimBugun { get; set; }

        // Ana fan grubu ve aydınlatma
        public int? Fan_AktifGrup { get; set; }
        public bool? Fan_TermikOk { get; set; }
        public bool? Aydinlatma_Ac { get; set; }

        // Buffel: yalnızca gerçek durum ve komut kodu
        public int? Buffel_2_Sol_State_Durum { get; set; }
        public int? Buffel_2_Sol_Cmd_CmdBit { get; set; }
        public int? Buffel_2_Sag_State_Durum { get; set; }
        public int? Buffel_2_Sag_Cmd_CmdBit { get; set; }

        // Ped kapaklar: M2401-M2404 = On 1-4, M2405-M2408 = Arka 1-4
        public int? PedKapak_On_1_State_Durum { get; set; }
        public int? PedKapak_On_1_Cmd_CmdBit { get; set; }
        public int? PedKapak_On_2_State_Durum { get; set; }
        public int? PedKapak_On_2_Cmd_CmdBit { get; set; }
        public int? PedKapak_On_3_State_Durum { get; set; }
        public int? PedKapak_On_3_Cmd_CmdBit { get; set; }
        public int? PedKapak_On_4_State_Durum { get; set; }
        public int? PedKapak_On_4_Cmd_CmdBit { get; set; }
        public int? PedKapak_Arka_1_State_Durum { get; set; }
        public int? PedKapak_Arka_1_Cmd_CmdBit { get; set; }
        public int? PedKapak_Arka_2_State_Durum { get; set; }
        public int? PedKapak_Arka_2_Cmd_CmdBit { get; set; }
        public int? PedKapak_Arka_3_State_Durum { get; set; }
        public int? PedKapak_Arka_3_Cmd_CmdBit { get; set; }
        public int? PedKapak_Arka_4_State_Durum { get; set; }
        public int? PedKapak_Arka_4_Cmd_CmdBit { get; set; }

        // Ped pompalar: M2301-M2308
        public int? PedPompa_1_Komut { get; set; }
        public bool? PedPompa_1_TermikOk { get; set; }
        public int? PedPompa_2_Komut { get; set; }
        public bool? PedPompa_2_TermikOk { get; set; }
        public int? PedPompa_3_Komut { get; set; }
        public bool? PedPompa_3_TermikOk { get; set; }
        public int? PedPompa_4_Komut { get; set; }
        public bool? PedPompa_4_TermikOk { get; set; }
        public int? PedPompa_5_Komut { get; set; }
        public bool? PedPompa_5_TermikOk { get; set; }
        public int? PedPompa_6_Komut { get; set; }
        public bool? PedPompa_6_TermikOk { get; set; }
        public int? PedPompa_7_Komut { get; set; }
        public bool? PedPompa_7_TermikOk { get; set; }
        public int? PedPompa_8_Komut { get; set; }
        public bool? PedPompa_8_TermikOk { get; set; }

        // Baca fanları: M2201-M2225
        public bool? BacaFani_1_Calisiyor { get; set; }
        public bool? BacaFani_2_Calisiyor { get; set; }
        public bool? BacaFani_3_Calisiyor { get; set; }
        public bool? BacaFani_4_Calisiyor { get; set; }
        public bool? BacaFani_5_Calisiyor { get; set; }
        public bool? BacaFani_6_Calisiyor { get; set; }
        public bool? BacaFani_7_Calisiyor { get; set; }
        public bool? BacaFani_8_Calisiyor { get; set; }
        public bool? BacaFani_9_Calisiyor { get; set; }
        public bool? BacaFani_10_Calisiyor { get; set; }
        public bool? BacaFani_11_Calisiyor { get; set; }
        public bool? BacaFani_12_Calisiyor { get; set; }
        public bool? BacaFani_13_Calisiyor { get; set; }
        public bool? BacaFani_14_Calisiyor { get; set; }
        public bool? BacaFani_15_Calisiyor { get; set; }
        public bool? BacaFani_16_Calisiyor { get; set; }
        public bool? BacaFani_17_Calisiyor { get; set; }
        public bool? BacaFani_18_Calisiyor { get; set; }
        public bool? BacaFani_19_Calisiyor { get; set; }
        public bool? BacaFani_20_Calisiyor { get; set; }
        public bool? BacaFani_21_Calisiyor { get; set; }
        public bool? BacaFani_22_Calisiyor { get; set; }
        public bool? BacaFani_23_Calisiyor { get; set; }
        public bool? BacaFani_24_Calisiyor { get; set; }
        public bool? BacaFani_25_Calisiyor { get; set; }
        public bool? BacaFani_TermikOk { get; set; }
    }
}
