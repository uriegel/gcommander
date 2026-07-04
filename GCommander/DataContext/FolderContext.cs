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
                // if (IsLeft)
                //     Storage.SaveLeftPath(value);
                // else
                //     Storage.SaveRightPath(value);
                OnChanged(nameof(CurrentPath));
            }
        }
    } = "";

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
    
    void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));

    public event PropertyChangedEventHandler? PropertyChanged;
}