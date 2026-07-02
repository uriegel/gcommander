record DirectoryItem(
    string Name
)
{
    public static DirectoryItem CreateDirItem(DirectoryInfo info)
        => new(info.Name);
        // => new(info.Name, idx)
        // {
        //     IsDirectory = true,
        //     IsHidden = (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden || info.Name.StartsWith('.'),
        //     Time = info.LastWriteTime
        // };

    public static DirectoryItem CreateFileItem(FileInfo info)
        => new(info.Name);
        // => new(info.Name, idx)
        // {
        //     Size = info.Length,
        //     IconPath = Directory.GetIconPath(info.Name, info.DirectoryName),
        //     IsHidden = (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden || info.Name.StartsWith('.'),
        //     Time = info.LastWriteTime
        // };
}

