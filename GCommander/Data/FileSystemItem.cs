abstract class FileSystemItem : SelectableItem
{
    public DateTime DateTime
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(DateTime));
        }
    }
    
    public bool IsHidden { get; } 

    public FileSystemItem(string name, bool isHidden) : base(name) => IsHidden = isHidden;

    public FileSystemItem(FileSystemInfo info) 
        : this(info.Name, (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden || info.Name.StartsWith('.'))
        => DateTime = info.LastWriteTime;
}
