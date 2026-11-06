using Microsoft.AspNetCore.Authorization;

namespace Authentication.API.Security
{
    /// <summary>
    /// Kullanıcı yönetimi uçlarına erişim şartı. Kararı <see cref="UserManagementAuthorizationHandler"/> verir.
    /// </summary>
    public sealed class UserManagementRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Yeni bir kullanıcı yönetimi şartı oluşturur.
        /// </summary>
        /// <param name="requiresWrite">True ise formda Yazma yetkisi aranır, false ise forma erişim yeterlidir.</param>
        /// <param name="allowInternalService">True ise geçerli iç servis anahtarıyla gelen istek de kabul edilir.</param>
        public UserManagementRequirement(bool requiresWrite, bool allowInternalService)
        {
            RequiresWrite = requiresWrite;
            AllowInternalService = allowInternalService;
        }

        /// <summary>Formda Yazma yetkisinin gerekip gerekmediği.</summary>
        public bool RequiresWrite { get; }

        /// <summary>İç servis anahtarıyla gelen isteklerin kabul edilip edilmediği.</summary>
        public bool AllowInternalService { get; }
    }
}
