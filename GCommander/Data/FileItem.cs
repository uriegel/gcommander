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

    public static FileItem New(FileInfo info) => new(info);
    public FileItem(FileItem a) : base(a.Name,a.IsHidden)
    {
        DateTime = a.DateTime;
        IsSelected = a.IsSelected;
        Size = a.Size;
    }

    FileItem(FileInfo info) : base(info) => Size = info.Length;
}