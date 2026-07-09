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
    } = string.Empty;
    
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
    } = string.Empty;

    public string Restriction
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(Restriction));
            }
        }
    } = string.Empty;

    public BackgroundAction BackgroundAction
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(BackgroundAction));
            }
        }
    }

    public bool IsEditing { get; set; }

    void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));

    public event PropertyChangedEventHandler? PropertyChanged;
}