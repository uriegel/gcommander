using System.Data;

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

    public string? NewName
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(NewName));
        }
    }

    public static FileItem New(FileInfo info) => new(info);
    public FileItem(FileItem a) : base(a.Name, a.IsHidden)
    {
        DateTime = a.DateTime;
        IsSelected = a.IsSelected;
        Size = a.Size;
    }

    public FileItem(string name, bool isHidden, DateTime dateTime, long size) : base(name, isHidden)
    {
        DateTime = dateTime;
        Size = size;
    }

    FileItem(FileInfo info) : base(info) => Size = info.Length;
}