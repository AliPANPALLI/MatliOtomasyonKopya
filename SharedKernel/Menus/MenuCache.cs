namespace SharedKernel.Menus
{
    public static class MenuCache
    {
        public static readonly IReadOnlyDictionary<string, string> TesisScadaLinkMap =
            MenuPathHelper.BuildLinkPathMap(AppMenus.TesisScada);
    }

}
