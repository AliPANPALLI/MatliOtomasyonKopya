namespace MatliOtomasyon.Shared.Application.Menus
{
    public static class MenuPathHelper
    {
        public static string NormalizeLink(string link)
        {
            if (string.IsNullOrWhiteSpace(link)) return "";
            var x = link.Trim();
            var q = x.IndexOf('?'); if (q >= 0) x = x[..q];
            var h = x.IndexOf('#'); if (h >= 0) x = x[..h];
            if (!x.StartsWith("/")) x = "/" + x;
            if (x.Length > 1 && x.EndsWith("/")) x = x[..^1];
            return x.ToLowerInvariant();
        }

        public static Dictionary<string, string> BuildLinkPathMap(IEnumerable<MenuNode> roots)
        {
            var map = new Dictionary<string, string>();

            void Walk(IEnumerable<MenuNode> items, List<string> ancestors)
            {
                foreach (var item in items)
                {
                    var next = new List<string>(ancestors) { item.Title };

                    if (!string.IsNullOrWhiteSpace(item.Link))
                    {
                        map[NormalizeLink(item.Link!)] = string.Join(" / ", next);
                    }

                    if (item.SubItems is { Count: > 0 })
                        Walk(item.SubItems, next);
                }
            }

            Walk(roots, new List<string>());
            return map;
        }
    }

}
