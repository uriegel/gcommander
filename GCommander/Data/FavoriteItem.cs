class FavoriteItem : SelectableItem
{
    public string Path { get; }

    public FavoriteItem(string name, string path) : base(name) => Path = path;
}
