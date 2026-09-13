class DirectoryItem : FileSystemItem
{
    public static DirectoryItem New(DirectoryInfo info) => new(info);
    public DirectoryItem(DirectoryItem a) : base(a.Name, a.IsHidden)
    {
        DateTime = a.DateTime;
        IsSelected = a.IsSelected;
    }
    DirectoryItem(DirectoryInfo info) : base(info) {}
}