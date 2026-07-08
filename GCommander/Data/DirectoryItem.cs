using System.ComponentModel;

record DirectoryItem(
    string Name,
    DirectoryItemType Type,
    bool IsHidden,
    string? IconPath = null,
    DateTime? DateTime = null,
    long? Size = null
) : INotifyPropertyChanged
{
    public ExifData? ExifData
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(ExifData));
        }
    }

    public bool IsSelected
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(IsSelected));
        }
    } 

    public static DirectoryItem CreateDirItem(DirectoryInfo info)
        => new(info.Name, DirectoryItemType.Directory, (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden || info.Name.StartsWith('.'))
        {
            DateTime = info.LastWriteTime
        };

    public static DirectoryItem CreateFileItem(FileInfo info)
        => new(info.Name, DirectoryItemType.File, (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden || info.Name.StartsWith('.'))
        {
            Size = info.Length,
            DateTime = info.LastWriteTime,
        };
    public event PropertyChangedEventHandler? PropertyChanged;

    //void OnChanged(string name) => Gtk.BeginInvoke(200, () => PropertyChanged?.Invoke(this, new(name)));
    void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));
}

enum DirectoryItemType
{
    Parent,
    Directory,
    File
}