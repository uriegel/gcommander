record DirectoryItem(
    string Name,
    DirectoryItemType Type,
    bool IsHidden,
    string? IconPath = null,
    DateTime? DateTime = null,
    long? Size = null
)
{
    public static DirectoryItem CreateDirItem(DirectoryInfo info)
        => new(info.Name, DirectoryItemType.Directory, (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden || info.Name.StartsWith('.'))
            {
                DateTime = info.LastWriteTime
            };

    public static DirectoryItem CreateFileItem(FileInfo info)
        => new(info.Name, DirectoryItemType.File, (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden || info.Name.StartsWith('.'))
            {
                Size = info.Length,
                DateTime = info.LastWriteTime
            };
}

enum DirectoryItemType
{
    Parent,
    Directory,
    File
}