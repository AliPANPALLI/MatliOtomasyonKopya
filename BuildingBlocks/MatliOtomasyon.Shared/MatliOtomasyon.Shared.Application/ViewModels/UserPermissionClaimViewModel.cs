namespace MatliOtomasyon.Shared.Application.ViewModels
{
    public class UserPermissionClaimViewModel
    {
        public class UserPermissions
        {
            public List<PermissionDetail> KullaniciYetkiler { get; set; }
        }

        public class PermissionDetail
        {
            public int FormId { get; set; }
            public string? ModuleLink { get; set; }
            public string FormLink { get; set; }
            public List<int> LokasyonIds { get; set; }
            public List<int> CizimIds { get; set; }
            public List<int> KumesIds { get; set; }
            public List<string> AllowedPaths { get; set; } //statik menuler icin
            //public bool? Okuma { get; set; }
            //public bool? Yazma { get; set; }
            //public bool? Degistirme { get; set; }
            //public bool? Silme { get; set; }
        }
    }
}
