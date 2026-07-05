using System.ComponentModel;

class FolderContext : INotifyPropertyChanged
{
    public bool IsLeft { get; set; }

    public string CurrentPath
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(CurrentPath));
            }
        }
    } = "";
    
    public int CurrentFileCount
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(CurrentFileCount));
            }
        }
    }
    
    public int CurrentDirectoryCount
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(CurrentDirectoryCount));
            }
        }
    }

    public string SelectedPath
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(SelectedPath));
            }
        }
    } = "";

    public bool IsEditing { get; set; }

    void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));

    public event PropertyChangedEventHandler? PropertyChanged;
}