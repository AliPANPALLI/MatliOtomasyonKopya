namespace MatliOtomasyon.Shared.Application.Menus
{
    public static class MenuCache
    {
        public static readonly IReadOnlyDictionary<string, string> TesisScadaLinkMap =
            MenuPathHelper.BuildLinkPathMap(AppMenus.TesisScada);
    }

}
