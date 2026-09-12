class DirectoryItem : FileSystemItem
{
    public static DirectoryItem New(DirectoryInfo info) => new(info);
    DirectoryItem(DirectoryInfo info) : base(info) {}
}