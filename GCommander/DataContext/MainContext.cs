using System.ComponentModel;

class MainContext : INotifyPropertyChanged
{
    public static MainContext Instance = new();

    public string? SelectedPath
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(SelectedPath));
                //OnChanged(nameof(StatusChoice));
            }
        }
    }

    public bool ShowHiddenItems
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(ShowHiddenItems));
            }
        }
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));
}
