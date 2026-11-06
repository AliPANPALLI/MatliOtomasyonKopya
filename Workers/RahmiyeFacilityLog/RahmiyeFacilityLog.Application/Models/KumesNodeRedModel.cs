using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RahmiyeFacilityLog.Application.Models
{
    public class KumesNodeRedModel
    {
        public int? Head { get; set; }
        public DateTime? TimeNow { get; set; }
        public int? Sicaklik_AktifSensSayisi { get; set; }
        public bool? Sicaklik_Kumes_Aktif { get; set; }
        public bool? Sicaklik_Dis_Aktif { get; set; }
        public bool? Sicaklik_Sensor_1_Aktif { get; set; }
        public bool? Sicaklik_Sensor_2_Aktif { get; set; }
        public bool? Sicaklik_Sensor_3_Aktif { get; set; }
        public bool? Sicaklik_Sensor_4_Aktif { get; set; }
        public bool? Sicaklik_Sensor_5_Aktif { get; set; }
        public bool? Sicaklik_Sensor_6_Aktif { get; set; }
        public decimal? Sicaklik_Kumes_Value { get; set; }
        public decimal? Sicaklik_Dis_Value { get; set; }
        public decimal? Sicaklik_Sensor_1_Value { get; set; }
        public decimal? Sicaklik_Sensor_2_Value { get; set; }
        public decimal? Sicaklik_Sensor_3_Value { get; set; }
        public decimal? Sicaklik_Sensor_4_Value { get; set; }
        public decimal? Sicaklik_Sensor_5_Value { get; set; }
        public decimal? Sicaklik_Sensor_6_Value { get; set; }
        public int? Sicaklik_Reserve_1 { get; set; }
        public int? Sicaklik_Reserve_2 { get; set; }
        public int? Sicaklik_Reserve_3 { get; set; }
        public int? Sicaklik_Reserve_4 { get; set; }
        public int? Nem_AktifSensSayisi { get; set; }
        public bool? Nem_Kumes_Aktif { get; set; }
        public bool? Nem_Dis_Aktif { get; set; }
        public bool? Nem_Sensor_1_Aktif { get; set; }
        public bool? Nem_Sensor_2_Aktif { get; set; }
        public bool? Nem_Sensor_3_Aktif { get; set; }
        public bool? Nem_Sensor_4_Aktif { get; set; }
        public bool? Nem_Sensor_5_Aktif { get; set; }
        public bool? Nem_Sensor_6_Aktif { get; set; }
        public decimal? Nem_Kumes_Value { get; set; }
        public decimal? Nem_Dis_Value { get; set; }
        public decimal? Nem_Sensor_1_Value { get; set; }
        public decimal? Nem_Sensor_2_Value { get; set; }
        public decimal? Nem_Sensor_3_Value { get; set; }
        public decimal? Nem_Sensor_4_Value { get; set; }
        public decimal? Nem_Sensor_5_Value { get; set; }
        public decimal? Nem_Sensor_6_Value { get; set; }
        public int? Nem_Reserve_1 { get; set; }
        public int? Nem_Reserve_2 { get; set; }
        public int? Nem_Reserve_3 { get; set; }
        public int? Nem_Reserve_4 { get; set; }
        public bool? Basinc_Sens_Aktif { get; set; }
        public decimal? Basinc_Sens_Value { get; set; }
        public int? Basinc_Reserve_1 { get; set; }
        public int? Basinc_Reserve_2 { get; set; }
        public int? Basinc_Reserve_3 { get; set; }
        public int? Basinc_Reserve_4 { get; set; }
        public bool? CO2_Sens_Aktif { get; set; }
        public decimal? CO2_Sens_Value { get; set; }
        public int? CO2_Reserve_1 { get; set; }
        public int? CO2_Reserve_2 { get; set; }
        public int? CO2_Reserve_3 { get; set; }
        public int? CO2_Reserve_4 { get; set; }
        public bool? IcmeSuyu_VanaAc { get; set; }
        public bool? IcmeSuyu_SayacAktif { get; set; }
        public int? IcmeSuyu_VanaState { get; set; }
        public decimal? IcmeSuyu_Sayac { get; set; }
        public decimal? IcmeSuyu_TuketimBugun { get; set; }
        public decimal? IcmeSuyu_GunSonuSayacDun { get; set; }
        public decimal? IcmeSuyu_TuketimDun { get; set; }
        public int? IcmeSuyu_ModeVana { get; set; }
        public int? IcmeSuyu_Reserve_2 { get; set; }
        public int? IcmeSuyu_Reserve_3 { get; set; }
        public int? IcmeSuyu_Reserve_4 { get; set; }
        public bool? Yem_Silo_1_Aktif { get; set; }
        public bool? Yem_Silo_2_Aktif { get; set; }
        public decimal? Yem_Silo_1_Value { get; set; }
        public decimal? Yem_Silo_2_Value { get; set; }
        public decimal? Yem_Silo_1_VarlikOran { get; set; }
        public decimal? Yem_Silo_2_VarlikOran { get; set; }
        public decimal? Yem_TuketimBugun { get; set; }
        public decimal? Yem_Mevcut { get; set; }
        public decimal? Yem_MevcutOran { get; set; }
        public int? Yem_Reserve_1 { get; set; }
        public int? Yem_Reserve_2 { get; set; }
        public int? Yem_Reserve_3 { get; set; }
        public int? Yem_Reserve_4 { get; set; }
        public int? Yemleme_1_Durum { get; set; }
        public int? Yemleme_2_Durum { get; set; }
        public int? Yemleme_3_Durum { get; set; }
        public int? Yemleme_4_Durum { get; set; }
        public int? Yemleme_5_Durum { get; set; }
        public int? Yemleme_6_Durum { get; set; }
        public int? Yemleme_Bolum_1_Durum { get; set; }
        public int? Yemleme_Bolum_2_Durum { get; set; }
        public int? Yemleme_Bolum_3_Durum { get; set; }
        public int? Yemleme_Bolum_4_Durum { get; set; }
        public Int64? Yemleme_BaslamaZamani { get; set; }
        public Int64? Yemleme_BolumAktuelSure { get; set; }
        public int? Fan_AktifGrup { get; set; }
        public bool? Fan_MinHavalandirma { get; set; }
        public bool? Fan_Kademe_1 { get; set; }
        public bool? Fan_Kademe_2 { get; set; }
        public bool? Fan_Kademe_3 { get; set; }
        public bool? Fan_Kademe_4 { get; set; }
        public bool? Fan_Kademe_5 { get; set; }
        public bool? Fan_Kademe_6 { get; set; }
        public bool? Fan_Kademe_7 { get; set; }
        public bool? Fan_Kademe_8 { get; set; }
        public bool? Fan_TermikOk { get; set; }
        public int? Fan_Mode { get; set; }
        public int? Fan_Reserve_2 { get; set; }
        public int? Fan_Reserve_3 { get; set; }
        public int? Fan_Reserve_4 { get; set; }
        public int? Buffel_Sol_Cmd_CmdBit { get; set; }
        public int? Buffel_Sol_Rpr_MotorState { get; set; }
        public int? Buffel_Sol_Rpr_Durum { get; set; }
        public bool? Buffel_Sol_Rpr_Switch_Kapali { get; set; }
        public bool? Buffel_Sol_Rpr_Switch_Acik { get; set; }
        public int? Buffel_Sag_Cmd_CmdBit { get; set; }
        public int? Buffel_Sag_Rpr_MotorState { get; set; }
        public int? Buffel_Sag_Rpr_Durum { get; set; }
        public bool? Buffel_Sag_Rpr_Switch_Kapali { get; set; }
        public bool? Buffel_Sag_Rpr_Switch_Acik { get; set; }
        public int? Buffel_Mode { get; set; }
        public int? Buffel_Reserve_1 { get; set; }
        public int? Buffel_Reserve_2 { get; set; }
        public int? Buffel_Reserve_3 { get; set; }
        public int? Buffel_Reserve_4 { get; set; }
        public bool? Ped_PompaRun { get; set; }
        public bool? Ped_PedKapakAc { get; set; }
        public bool? Ped_PompaTermikOk { get; set; }
        public int? Ped_PompaDurum { get; set; }
        public int? Ped_PedKapakDurum { get; set; }
        public int? Ped_ModePompa { get; set; }
        public int? Ped_ModeKapak { get; set; }
        public int? Ped_Reserve_3 { get; set; }
        public int? Ped_Reserve_4 { get; set; }
        public bool? Aydinlatma_Ac { get; set; }
        public int? Aydinlatma_Durum { get; set; }
        public decimal? Aydinlatma_Oran { get; set; }
        public int? Aydinlatma_Mode { get; set; }
        public int? Aydinlatma_Reserve_2 { get; set; }
        public int? Aydinlatma_Reserve_3 { get; set; }
        public int? Aydinlatma_Reserve_4 { get; set; }
        public int? Donem_BugunOlenSayisi { get; set; }
        public int? Donem_Reserve_1 { get; set; }
        public int? Donem_Reserve_2 { get; set; }
        public int? Donem_Reserve_3 { get; set; }
        public int? Donem_Reserve_4 { get; set; }
        public bool? SicaklikAlarm_AktifSensSayisiAz { get; set; }
        public bool? SicaklikAlarm_DusukKumes { get; set; }
        public bool? SicaklikAlarm_FazlaKumes { get; set; }
        public bool? SicaklikAlarm_DusukSens1 { get; set; }
        public bool? SicaklikAlarm_DusukSens2 { get; set; }
        public bool? SicaklikAlarm_DusukSens3 { get; set; }
        public bool? SicaklikAlarm_DusukSens4 { get; set; }
        public bool? SicaklikAlarm_DusukSens5 { get; set; }
        public bool? SicaklikAlarm_DusukSens6 { get; set; }
        public bool? SicaklikAlarm_FazlaSens1 { get; set; }
        public bool? SicaklikAlarm_FazlaSens2 { get; set; }
        public bool? SicaklikAlarm_FazlaSens3 { get; set; }
        public bool? SicaklikAlarm_FazlaSens4 { get; set; }
        public bool? SicaklikAlarm_FazlaSens5 { get; set; }
        public bool? SicaklikAlarm_FazlaSens6 { get; set; }
        public bool? NemAlarm_AktifSensSayisiAz { get; set; }
        public bool? CO2Alarm_AktifDegil { get; set; }
        public bool? CO2Alarm_Fazla { get; set; }
        public bool? BasincAlarm_AktifDegil { get; set; }
        public bool? BasincAlarm_Fazla { get; set; }
        public bool? IcmeSuyuAlarm_AktifDegil { get; set; }
        public bool? FanAlarm_TermikNOK { get; set; }
        public bool? PedAlarm_PompaTermikNOK { get; set; }
        public bool? BuffelAlarm_SolTermikNOK { get; set; }
        public bool? BuffelAlarm_SagTermikNOK { get; set; }
        public bool? GenelAlarm_SebekedeElektrikYok { get; set; }
        public bool? GenelAlarm_AlarmReserve_1 { get; set; }
        public bool? GenelAlarm_AlarmReserve_2 { get; set; }
        public bool? GenelAlarm_AlarmReserve_3 { get; set; }
        public bool? GenelAlarm_AlarmReserve_4 { get; set; }
        public bool? GenelAlarm_AlarmReserve_5 { get; set; }
        public bool? GenelAlarm_AlarmReserve_6 { get; set; }
        public bool? GenelAlarm_AlarmReserve_7 { get; set; }
        public bool? GenelAlarm_AlarmReserve_8 { get; set; }
        public bool? GenelAlarm_AlarmReserve_9 { get; set; }
        public bool? GenelAlarm_AlarmReserve_10 { get; set; }
        public bool? GenelAlarm_AlarmReserve_11 { get; set; }
        public bool? GenelAlarm_AlarmReserve_12 { get; set; }
        public bool? GenelAlarm_AlarmReserve_13 { get; set; }
        public bool? GenelAlarm_AlarmReserve_14 { get; set; }
        public bool? GenelAlarm_AlarmReserve_15 { get; set; }
        public bool? YemAlarm_Silo1BaglantiKoptu { get; set; }
        public bool? YemAlarm_Silo2BaglantiKoptu { get; set; }
        public bool? YemAlarm_AlarmReserve_3 { get; set; }
        public bool? YemAlarm_AlarmReserve_4 { get; set; }
        public bool? YemAlarm_AlarmReserve_5 { get; set; }
        public bool? YemAlarm_AlarmReserve_6 { get; set; }
        public bool? YemAlarm_AlarmReserve_7 { get; set; }
        public bool? YemAlarm_AlarmReserve_8 { get; set; }
        public bool? YemAlarm_AlarmReserve_9 { get; set; }
        public bool? YemAlarm_AlarmReserve_10 { get; set; }
        public bool? YemAlarm_AlarmReserve_11 { get; set; }
        public bool? YemAlarm_AlarmReserve_12 { get; set; }
        public bool? YemAlarm_AlarmReserve_13 { get; set; }
        public bool? YemAlarm_AlarmReserve_14 { get; set; }
        public bool? YemAlarm_AlarmReserve_15 { get; set; }
        public bool? YemAlarm_AlarmReserve_16 { get; set; }
        public int? Reserve_4 { get; set; }

        public bool? Buffel_2_Sol_State_Auto { get; set; }
        public bool? Buffel_2_Sol_State_Ac_Kontaktor { get; set; }
        public bool? Buffel_2_Sol_State_Kapat_Kontaktor { get; set; }
        public bool? Buffel_2_Sol_State_SwitchAcik { get; set; }
        public bool? Buffel_2_Sol_State_SwitchKapali { get; set; }
        public int? Buffel_2_Sol_State_Durum { get; set; }
        public bool? Buffel_2_Sol_Cmd_Ac { get; set; }
        public bool? Buffel_2_Sol_Cmd_Kapat { get; set; }
        public int? Buffel_2_Sol_Cmd_CmdBit { get; set; }

        public bool? Buffel_2_Sag_State_Auto { get; set; }
        public bool? Buffel_2_Sag_State_Ac_Kontaktor { get; set; }
        public bool? Buffel_2_Sag_State_Kapat_Kontaktor { get; set; }
        public bool? Buffel_2_Sag_State_SwitchAcik { get; set; }
        public bool? Buffel_2_Sag_State_SwitchKapali { get; set; }
        public int? Buffel_2_Sag_State_Durum { get; set; }
        public bool? Buffel_2_Sag_Cmd_Ac { get; set; }
        public bool? Buffel_2_Sag_Cmd_Kapat { get; set; }
        public int? Buffel_2_Sag_Cmd_CmdBit { get; set; }

        public bool? PedKapak_On_1_State_Auto { get; set; }
        public bool? PedKapak_On_1_State_Ac_Kontaktor { get; set; }
        public bool? PedKapak_On_1_State_Kapat_Kontaktor { get; set; }
        public bool? PedKapak_On_1_State_SwitchAcik { get; set; }
        public bool? PedKapak_On_1_State_SwitchKapali { get; set; }
        public int? PedKapak_On_1_State_Durum { get; set; }
        public bool? PedKapak_On_1_Cmd_Ac { get; set; }
        public bool? PedKapak_On_1_Cmd_Kapat { get; set; }
        public int? PedKapak_On_1_Cmd_CmdBit { get; set; }

        public bool? PedKapak_On_2_State_Auto { get; set; }
        public bool? PedKapak_On_2_State_Ac_Kontaktor { get; set; }
        public bool? PedKapak_On_2_State_Kapat_Kontaktor { get; set; }
        public bool? PedKapak_On_2_State_SwitchAcik { get; set; }
        public bool? PedKapak_On_2_State_SwitchKapali { get; set; }
        public int? PedKapak_On_2_State_Durum { get; set; }
        public bool? PedKapak_On_2_Cmd_Ac { get; set; }
        public bool? PedKapak_On_2_Cmd_Kapat { get; set; }
        public int? PedKapak_On_2_Cmd_CmdBit { get; set; }

        public bool? PedKapak_On_3_State_Auto { get; set; }
        public bool? PedKapak_On_3_State_Ac_Kontaktor { get; set; }
        public bool? PedKapak_On_3_State_Kapat_Kontaktor { get; set; }
        public bool? PedKapak_On_3_State_SwitchAcik { get; set; }
        public bool? PedKapak_On_3_State_SwitchKapali { get; set; }
        public int? PedKapak_On_3_State_Durum { get; set; }
        public bool? PedKapak_On_3_Cmd_Ac { get; set; }
        public bool? PedKapak_On_3_Cmd_Kapat { get; set; }
        public int? PedKapak_On_3_Cmd_CmdBit { get; set; }

        public bool? PedKapak_On_4_State_Auto { get; set; }
        public bool? PedKapak_On_4_State_Ac_Kontaktor { get; set; }
        public bool? PedKapak_On_4_State_Kapat_Kontaktor { get; set; }
        public bool? PedKapak_On_4_State_SwitchAcik { get; set; }
        public bool? PedKapak_On_4_State_SwitchKapali { get; set; }
        public int? PedKapak_On_4_State_Durum { get; set; }
        public bool? PedKapak_On_4_Cmd_Ac { get; set; }
        public bool? PedKapak_On_4_Cmd_Kapat { get; set; }
        public int? PedKapak_On_4_Cmd_CmdBit { get; set; }

        public bool? PedKapak_Arka_1_State_Auto { get; set; }
        public bool? PedKapak_Arka_1_State_Ac_Kontaktor { get; set; }
        public bool? PedKapak_Arka_1_State_Kapat_Kontaktor { get; set; }
        public bool? PedKapak_Arka_1_State_SwitchAcik { get; set; }
        public bool? PedKapak_Arka_1_State_SwitchKapali { get; set; }
        public int? PedKapak_Arka_1_State_Durum { get; set; }
        public bool? PedKapak_Arka_1_Cmd_Ac { get; set; }
        public bool? PedKapak_Arka_1_Cmd_Kapat { get; set; }
        public int? PedKapak_Arka_1_Cmd_CmdBit { get; set; }

        public bool? PedKapak_Arka_2_State_Auto { get; set; }
        public bool? PedKapak_Arka_2_State_Ac_Kontaktor { get; set; }
        public bool? PedKapak_Arka_2_State_Kapat_Kontaktor { get; set; }
        public bool? PedKapak_Arka_2_State_SwitchAcik { get; set; }
        public bool? PedKapak_Arka_2_State_SwitchKapali { get; set; }
        public int? PedKapak_Arka_2_State_Durum { get; set; }
        public bool? PedKapak_Arka_2_Cmd_Ac { get; set; }
        public bool? PedKapak_Arka_2_Cmd_Kapat { get; set; }
        public int? PedKapak_Arka_2_Cmd_CmdBit { get; set; }

        public bool? PedKapak_Arka_3_State_Auto { get; set; }
        public bool? PedKapak_Arka_3_State_Ac_Kontaktor { get; set; }
        public bool? PedKapak_Arka_3_State_Kapat_Kontaktor { get; set; }
        public bool? PedKapak_Arka_3_State_SwitchAcik { get; set; }
        public bool? PedKapak_Arka_3_State_SwitchKapali { get; set; }
        public int? PedKapak_Arka_3_State_Durum { get; set; }
        public bool? PedKapak_Arka_3_Cmd_Ac { get; set; }
        public bool? PedKapak_Arka_3_Cmd_Kapat { get; set; }
        public int? PedKapak_Arka_3_Cmd_CmdBit { get; set; }

        public bool? PedKapak_Arka_4_State_Auto { get; set; }
        public bool? PedKapak_Arka_4_State_Ac_Kontaktor { get; set; }
        public bool? PedKapak_Arka_4_State_Kapat_Kontaktor { get; set; }
        public bool? PedKapak_Arka_4_State_SwitchAcik { get; set; }
        public bool? PedKapak_Arka_4_State_SwitchKapali { get; set; }
        public int? PedKapak_Arka_4_State_Durum { get; set; }
        public bool? PedKapak_Arka_4_Cmd_Ac { get; set; }
        public bool? PedKapak_Arka_4_Cmd_Kapat { get; set; }
        public int? PedKapak_Arka_4_Cmd_CmdBit { get; set; }

        public bool? PedKapak_Sol_1_State_Auto { get; set; }
        public bool? PedKapak_Sol_1_State_Ac_Kontaktor { get; set; }
        public bool? PedKapak_Sol_1_State_Kapat_Kontaktor { get; set; }
        public bool? PedKapak_Sol_1_State_SwitchAcik { get; set; }
        public bool? PedKapak_Sol_1_State_SwitchKapali { get; set; }
        public int? PedKapak_Sol_1_State_Durum { get; set; }
        public bool? PedKapak_Sol_1_Cmd_Ac { get; set; }
        public bool? PedKapak_Sol_1_Cmd_Kapat { get; set; }
        public int? PedKapak_Sol_1_Cmd_CmdBit { get; set; }

        public bool? PedKapak_Sol_2_State_Auto { get; set; }
        public bool? PedKapak_Sol_2_State_Ac_Kontaktor { get; set; }
        public bool? PedKapak_Sol_2_State_Kapat_Kontaktor { get; set; }
        public bool? PedKapak_Sol_2_State_SwitchAcik { get; set; }
        public bool? PedKapak_Sol_2_State_SwitchKapali { get; set; }
        public int? PedKapak_Sol_2_State_Durum { get; set; }
        public bool? PedKapak_Sol_2_Cmd_Ac { get; set; }
        public bool? PedKapak_Sol_2_Cmd_Kapat { get; set; }
        public int? PedKapak_Sol_2_Cmd_CmdBit { get; set; }

        public bool? PedKapak_Sag_1_State_Auto { get; set; }
        public bool? PedKapak_Sag_1_State_Ac_Kontaktor { get; set; }
        public bool? PedKapak_Sag_1_State_Kapat_Kontaktor { get; set; }
        public bool? PedKapak_Sag_1_State_SwitchAcik { get; set; }
        public bool? PedKapak_Sag_1_State_SwitchKapali { get; set; }
        public int? PedKapak_Sag_1_State_Durum { get; set; }
        public bool? PedKapak_Sag_1_Cmd_Ac { get; set; }
        public bool? PedKapak_Sag_1_Cmd_Kapat { get; set; }
        public int? PedKapak_Sag_1_Cmd_CmdBit { get; set; }

        public bool? PedKapak_Sag_2_State_Auto { get; set; }
        public bool? PedKapak_Sag_2_State_Ac_Kontaktor { get; set; }
        public bool? PedKapak_Sag_2_State_Kapat_Kontaktor { get; set; }
        public bool? PedKapak_Sag_2_State_SwitchAcik { get; set; }
        public bool? PedKapak_Sag_2_State_SwitchKapali { get; set; }
        public int? PedKapak_Sag_2_State_Durum { get; set; }
        public bool? PedKapak_Sag_2_Cmd_Ac { get; set; }
        public bool? PedKapak_Sag_2_Cmd_Kapat { get; set; }
        public int? PedKapak_Sag_2_Cmd_CmdBit { get; set; }

        public bool? BacaFani_1_Auto { get; set; }
        public bool? BacaFani_1_Calisiyor { get; set; }
        public bool? BacaFani_1_Cmd_Ac { get; set; }
        public bool? BacaFani_1_Cmd_Kapat { get; set; }
        public int? BacaFani_1_Cmd_CmdBit { get; set; }

        public bool? BacaFani_2_Auto { get; set; }
        public bool? BacaFani_2_Calisiyor { get; set; }
        public bool? BacaFani_2_Cmd_Ac { get; set; }
        public bool? BacaFani_2_Cmd_Kapat { get; set; }
        public int? BacaFani_2_Cmd_CmdBit { get; set; }

        public bool? BacaFani_3_Auto { get; set; }
        public bool? BacaFani_3_Calisiyor { get; set; }
        public bool? BacaFani_3_Cmd_Ac { get; set; }
        public bool? BacaFani_3_Cmd_Kapat { get; set; }
        public int? BacaFani_3_Cmd_CmdBit { get; set; }

        public bool? BacaFani_4_Auto { get; set; }
        public bool? BacaFani_4_Calisiyor { get; set; }
        public bool? BacaFani_4_Cmd_Ac { get; set; }
        public bool? BacaFani_4_Cmd_Kapat { get; set; }
        public int? BacaFani_4_Cmd_CmdBit { get; set; }

        public bool? BacaFani_5_Auto { get; set; }
        public bool? BacaFani_5_Calisiyor { get; set; }
        public bool? BacaFani_5_Cmd_Ac { get; set; }
        public bool? BacaFani_5_Cmd_Kapat { get; set; }
        public int? BacaFani_5_Cmd_CmdBit { get; set; }

        public bool? BacaFani_6_Auto { get; set; }
        public bool? BacaFani_6_Calisiyor { get; set; }
        public bool? BacaFani_6_Cmd_Ac { get; set; }
        public bool? BacaFani_6_Cmd_Kapat { get; set; }
        public int? BacaFani_6_Cmd_CmdBit { get; set; }

        public bool? BacaFani_7_Auto { get; set; }
        public bool? BacaFani_7_Calisiyor { get; set; }
        public bool? BacaFani_7_Cmd_Ac { get; set; }
        public bool? BacaFani_7_Cmd_Kapat { get; set; }
        public int? BacaFani_7_Cmd_CmdBit { get; set; }

        public bool? BacaFani_8_Auto { get; set; }
        public bool? BacaFani_8_Calisiyor { get; set; }
        public bool? BacaFani_8_Cmd_Ac { get; set; }
        public bool? BacaFani_8_Cmd_Kapat { get; set; }
        public int? BacaFani_8_Cmd_CmdBit { get; set; }

        public bool? BacaFani_9_Auto { get; set; }
        public bool? BacaFani_9_Calisiyor { get; set; }
        public bool? BacaFani_9_Cmd_Ac { get; set; }
        public bool? BacaFani_9_Cmd_Kapat { get; set; }
        public int? BacaFani_9_Cmd_CmdBit { get; set; }

        public bool? BacaFani_10_Auto { get; set; }
        public bool? BacaFani_10_Calisiyor { get; set; }
        public bool? BacaFani_10_Cmd_Ac { get; set; }
        public bool? BacaFani_10_Cmd_Kapat { get; set; }
        public int? BacaFani_10_Cmd_CmdBit { get; set; }

        public bool? BacaFani_11_Auto { get; set; }
        public bool? BacaFani_11_Calisiyor { get; set; }
        public bool? BacaFani_11_Cmd_Ac { get; set; }
        public bool? BacaFani_11_Cmd_Kapat { get; set; }
        public int? BacaFani_11_Cmd_CmdBit { get; set; }

        public bool? BacaFani_12_Auto { get; set; }
        public bool? BacaFani_12_Calisiyor { get; set; }
        public bool? BacaFani_12_Cmd_Ac { get; set; }
        public bool? BacaFani_12_Cmd_Kapat { get; set; }
        public int? BacaFani_12_Cmd_CmdBit { get; set; }

        public bool? BacaFani_13_Auto { get; set; }
        public bool? BacaFani_13_Calisiyor { get; set; }
        public bool? BacaFani_13_Cmd_Ac { get; set; }
        public bool? BacaFani_13_Cmd_Kapat { get; set; }
        public int? BacaFani_13_Cmd_CmdBit { get; set; }

        public bool? BacaFani_14_Auto { get; set; }
        public bool? BacaFani_14_Calisiyor { get; set; }
        public bool? BacaFani_14_Cmd_Ac { get; set; }
        public bool? BacaFani_14_Cmd_Kapat { get; set; }
        public int? BacaFani_14_Cmd_CmdBit { get; set; }

        public bool? BacaFani_15_Auto { get; set; }
        public bool? BacaFani_15_Calisiyor { get; set; }
        public bool? BacaFani_15_Cmd_Ac { get; set; }
        public bool? BacaFani_15_Cmd_Kapat { get; set; }
        public int? BacaFani_15_Cmd_CmdBit { get; set; }

        public bool? BacaFani_16_Auto { get; set; }
        public bool? BacaFani_16_Calisiyor { get; set; }
        public bool? BacaFani_16_Cmd_Ac { get; set; }
        public bool? BacaFani_16_Cmd_Kapat { get; set; }
        public int? BacaFani_16_Cmd_CmdBit { get; set; }

        public bool? BacaFani_17_Auto { get; set; }
        public bool? BacaFani_17_Calisiyor { get; set; }
        public bool? BacaFani_17_Cmd_Ac { get; set; }
        public bool? BacaFani_17_Cmd_Kapat { get; set; }
        public int? BacaFani_17_Cmd_CmdBit { get; set; }

        public bool? BacaFani_18_Auto { get; set; }
        public bool? BacaFani_18_Calisiyor { get; set; }
        public bool? BacaFani_18_Cmd_Ac { get; set; }
        public bool? BacaFani_18_Cmd_Kapat { get; set; }
        public int? BacaFani_18_Cmd_CmdBit { get; set; }

        public bool? BacaFani_19_Auto { get; set; }
        public bool? BacaFani_19_Calisiyor { get; set; }
        public bool? BacaFani_19_Cmd_Ac { get; set; }
        public bool? BacaFani_19_Cmd_Kapat { get; set; }
        public int? BacaFani_19_Cmd_CmdBit { get; set; }

        public bool? BacaFani_20_Auto { get; set; }
        public bool? BacaFani_20_Calisiyor { get; set; }
        public bool? BacaFani_20_Cmd_Ac { get; set; }
        public bool? BacaFani_20_Cmd_Kapat { get; set; }
        public int? BacaFani_20_Cmd_CmdBit { get; set; }

        public bool? BacaFani_21_Auto { get; set; }
        public bool? BacaFani_21_Calisiyor { get; set; }
        public bool? BacaFani_21_Cmd_Ac { get; set; }
        public bool? BacaFani_21_Cmd_Kapat { get; set; }
        public int? BacaFani_21_Cmd_CmdBit { get; set; }

        public bool? BacaFani_22_Auto { get; set; }
        public bool? BacaFani_22_Calisiyor { get; set; }
        public bool? BacaFani_22_Cmd_Ac { get; set; }
        public bool? BacaFani_22_Cmd_Kapat { get; set; }
        public int? BacaFani_22_Cmd_CmdBit { get; set; }

        public bool? BacaFani_23_Auto { get; set; }
        public bool? BacaFani_23_Calisiyor { get; set; }
        public bool? BacaFani_23_Cmd_Ac { get; set; }
        public bool? BacaFani_23_Cmd_Kapat { get; set; }
        public int? BacaFani_23_Cmd_CmdBit { get; set; }

        public bool? BacaFani_24_Auto { get; set; }
        public bool? BacaFani_24_Calisiyor { get; set; }
        public bool? BacaFani_24_Cmd_Ac { get; set; }
        public bool? BacaFani_24_Cmd_Kapat { get; set; }
        public int? BacaFani_24_Cmd_CmdBit { get; set; }
        public int? END { get; set; }
    }
}