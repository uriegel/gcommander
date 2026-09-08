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

    public ExifData? ExifData
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(ExifData));
            }
        }
    }    

    public bool IsEditing { get; set; }

    public void AddHistory(string path)
    {
        if (history.Count == 0 || history[^1] != path)
            history.Add(path);
        historyPosition = history.Count - 1;
    }

    public string? GetHistory(bool forward)
    {
        if (history.Count == 0)
            return null;
        historyPosition = forward 
            ? Math.Min(history.Count - 1, historyPosition + 1)
            : Math.Max(0, historyPosition - 1);
        return history[historyPosition];
    }

    void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));

    public event PropertyChangedEventHandler? PropertyChanged;

    readonly List<string> history = [];
    int historyPosition = -1;
}