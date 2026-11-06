namespace SharedKernel
{
    public static class ApiUrls
    {
        //public static string TicketDosyalarPath = @"C:\Users\beray.bayazit\Desktop\TicketDosyalar";
        //public static string NekropsiFotograflariPath = @"C:\Users\beray.bayazit\Desktop\NekropsiFotograflari";
        //public const string GatewayBaseUrl = "http://localhost:5050/api";
        //public const string AuthenticationBaseUrl = "http://localhost:5021/api";
        //public const string PanoKimliklendirmeBaseUrl = "http://localhost:5041/api";
        //public const string KumesScadaBaseUrl = "http://localhost:5031/api";
        //public const string SuHatScadaBaseUrl = "http://localhost:49380/api";
        //public const string SharedAppBaseUrl = "http://localhost:5051/api";
        //public const string EnerjiIzlemeAppBaseUrl = "http://localhost:5033/api";
        //public const string TicketAppBaseUrl = "http://localhost:5061/api";
        //public const string SeqLogServerUrl = "http://localhost:5341";
        //public const string RahmiyeTesisWorkerUrl = "http://localhost:5000";

        public static string TicketDosyalarPath = @"E:\TicketDosyalar";
        public static string ViyolYanginDosyalarPath = @"E:\YanginAlgilamaFotograflar";
        public static string NekropsiFotograflariPath = @"E:\NekropsiFotograflari";
        public const string GatewayBaseUrl = "https://otomasyon.matli.com.tr:44313/api";
        public const string AuthenticationBaseUrl = "https://otomasyon.matli.com.tr:44338/api";
        public const string PanoKimliklendirmeBaseUrl = "https://otomasyon.matli.com.tr:44316/api";
        public const string KumesScadaBaseUrl = "https://otomasyon.matli.com.tr:44317/api";
        public const string SuHatScadaBaseUrl = "https://otomasyon.matli.com.tr:44319/api";
        public const string SharedAppBaseUrl = "https://otomasyon.matli.com.tr:44339/api";
        public const string EnerjiIzlemeAppBaseUrl = "https://otomasyon.matli.com.tr:44343/api";
        public const string TicketAppBaseUrl = "https://otomasyon.matli.com.tr:44321/api";
        public const string SeqLogServerUrl = "http://localhost:5341";
        public const string RahmiyeTesisWorkerUrl = "http://localhost:5000";

        //public const string GatewayBaseUrl = "https://otomasyontest.matli.com.tr:44313/api";
        //public const string AuthenticationBaseUrl = "https://otomasyontest.matli.com.tr:44338/api";
        //public const string PanoKimliklendirmeBaseUrl = "https://otomasyontest.matli.com.tr:44316/api";
        //public const string KumesScadaBaseUrl = "https://otomasyontest.matli.com.tr:44317/api";
        //public const string SuHatScadaBaseUrl = "https://otomasyontest.matli.com.tr:44319/api";
        //public const string SharedAppBaseUrl = "https://otomasyontest.matli.com.tr:44339/api";
        //public const string EnerjiIzlemeAppBaseUrl = "https://otomasyontest.matli.com.tr:44343/api";
        //public const string TicketAppBaseUrl = "https://otomasyontest.matli.com.tr:44321/api";
        //public const string SeqLogServerUrl = "http://localhost:5341";
        //public const string RahmiyeTesisWorkerUrl = "http://localhost:5000";

        public static class AuthenticationUrls
        {
            public const string GetUserById = "/users/{0}";
        }

        public static class PanoKimliklendirmeUrls
        {
            public const string GetMakines = "/Makine/GetAllList";
            public const string GetBolums = "/Bolum/GetAllList";
            public const string GetLokasyonIdsByMakineIds = "/Makine/GetLokasyonIdsByMakineIds";
            public const string GetLokasyonIdsByBolumIds = "/Bolum/GetLokasyonIdsByBolumIds";
            public const string GetAllUserMakineYetkiByUserId = "/UserMakineYetki/GetAllUserMakineYetkiByUserId";
        }

        public static class KumesScadaUrls
        {
            public const string GetLokasyonIdsByKumesIds = "/Kumes/GetLokasyonIdsByKumesIds";
            public const string GetKumesIdsByLokasyonIds = "/Kumes/GetKumesIdsByLokasyonIds";
        }

        public static class SuHatScadaUrls
        {
            public const string GetLokasyonIdsBySuKaynakIds = "/SuKaynak/GetLokasyonIdsBySuKaynakIds";
        }

        public static class SharedAppUrls
        {
            public const string GetGenelParametrelerDegerByName = "/GenelParametreler/GetGenelParametrelerDegerByName";
            public const string GetUserFormYetkiListByUserId = "/Yetki/GetUserFormYetkiListByUserId";
            public const string GetUserFormYetkiByUserIdAndFormLink = "/Yetki/GetUserFormYetkiByUserIdAndFormLink";
            public const string GetUserPermissions = "/Yetki/GetUserPermissions";
            public const string AddMailGonderimTakip = "/MailGonderimTakip/AddMailGonderimTakip";
        }

        public static class RahmiyeTesisWorkerUrls
        {
            public const string GetIsPlcConnected = "/Service/GetIsPlcConnect";
            public const string GetPlcConnectedAndReading = "/Service/GetPlcConnectedAndReading";
            public const string GetAllPlcConnectedAndReading = "/Service/GetAllPlcConnectedAndReading";
            public const string CheckFlag = "/Service/CheckFlag";
            public const string GetPlcBilgileriByKumesId = "/Service/GetPlcBilgileriByKumesId";
        }

        public static class SuIzlemeWorkerUrls
        {
            public const string GetIsPlcConnected = "/SuIzlemeService/GetIsPlcConnect";
            public const string GetPlcConnectedAndReading = "/SuIzlemeService/GetPlcConnectedAndReading";
            public const string CheckFlag = "/SuIzlemeService/CheckFlag";
            public const string GetPlcBilgileriByKumesId = "/SuIzlemeService/GetPlcBilgileriByKumesId";
        }
    }
}