namespace SharedKernel.Menus
{
    public sealed class MenuNode
    {
        public string Title { get; init; } = default!;
        public string? Link { get; init; }
        public List<MenuNode>? SubItems { get; init; }
    }

}
