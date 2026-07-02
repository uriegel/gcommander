record DirectoryItem(
    string Name,
    DirectoryItemType Type,
    string? IconPath = null,
    DateTime? DateTime = null,
    long? Size = null
)
{
    public static DirectoryItem CreateDirItem(DirectoryInfo info)
        => new(info.Name, DirectoryItemType.Directory)
            {
                DateTime = info.LastWriteTime
            //     IsHidden = (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden || info.Name.StartsWith('.'),
            };

    public static DirectoryItem CreateFileItem(FileInfo info)
        => new(info.Name, DirectoryItemType.File)
            {
                Size = info.Length,
                //     IsHidden = (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden || info.Name.StartsWith('.'),
                DateTime = info.LastWriteTime
            };
}

enum DirectoryItemType
{
    Parent,
    Directory,
    File
}