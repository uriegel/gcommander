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
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ChangeFolderContext(FolderContext? folderContext)
    {
        if (folderContext != null)
        {
            if (this.folderContext != null)
                this.folderContext.PropertyChanged -= FolderContextPropertyChanged;
            this.folderContext = folderContext;
            this.folderContext.PropertyChanged += FolderContextPropertyChanged;
            // CurrentDirectories = folderContext.CurrentDirectories;
            // CurrentFiles = folderContext.CurrentFiles;
            SelectedPath = folderContext.SelectedPath;
            // ExifData = folderContext.ExifData;
            // BackgroundAction = folderContext.BackgroundAction;
            // SelectedFiles = folderContext.SelectedFiles;
        }
    }

    void FolderContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (folderContext != null)
            switch (e.PropertyName)
            {
                // case nameof(CurrentDirectories):
                //     CurrentDirectories = folderContext.CurrentDirectories;
                //     break;
                // case nameof(CurrentFiles):
                //     CurrentFiles = folderContext.CurrentFiles;
                //     break;
                case nameof(SelectedPath):
                    SelectedPath = folderContext.SelectedPath;
                    break;
                // case nameof(ExifData):
                //     ExifData = folderContext.ExifData;
                //     break;
                // case nameof(SelectedFiles):
                //     SelectedFiles = folderContext.SelectedFiles;
                //     break;
                // case nameof(BackgroundAction):
                //     BackgroundAction = folderContext.BackgroundAction;
                //     break;
            }
    }

    FolderContext? folderContext;

    void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));
}
