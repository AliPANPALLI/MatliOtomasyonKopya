namespace Authentication.API.Security
{
    /// <summary>
    /// Kullanıcı yönetimi uçlarında kullanılan yetki politikalarının adlarını tek yerde toplar.
    /// Okuma ve yazma ayrımı, SharedApp'teki form yetkisinin Okuma/Yazma ayrımıyla aynıdır.
    /// </summary>
    public static class UserManagementPolicies
    {
        /// <summary>Kullanıcı listesini ve giriş/sayfa ziyaret raporlarını görüntüleme yetkisi.</summary>
        public const string Read = "UserManagement.Read";

        /// <summary>Kullanıcı oluşturma, düzenleme, silme ve ilk şifre gönderme yetkisi.</summary>
        public const string Write = "UserManagement.Write";

        /// <summary>
        /// Okuma yetkisi ya da geçerli bir iç servis anahtarı. SharedApp'in kullanıcı listesini
        /// kullanıcı token'ı olmadan çekebilmesi için geçici olarak kullanılıyor.
        /// </summary>
        public const string ReadOrInternalService = "UserManagement.ReadOrInternalService";
    }
}
