class FileItem : FileSystemItem
{
    public long Size
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(Size));
        }
    }

    public ExifData? ExifData
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(ExifData));
        }
    }

    public string? IconPath { get; }

    public static FileItem New(FileInfo info) => new(info);
    FileItem(FileInfo info) : base(info) => Size = info.Length;
}