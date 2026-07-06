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

    public bool ViewerVisible
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(ViewerVisible));
            }
        }
    }

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

    public string ErrorText
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(ErrorText));
                if (value != "")
                {
                    Reset();
                    async void Reset()
                    {
                        await Task.Delay(6000);
                        ErrorText = "";
                    }
                }
            }
        }
    } = "";

    public string? Restriction
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(Restriction));
                OnChanged(nameof(StatusChoice));
            }
        }
    }

    public int SelectedFiles
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(SelectedFiles));
                OnChanged(nameof(StatusChoice));
            }
        }
    }

    public StatusChoice StatusChoice
    {
        get => Restriction?.Trim()?.Length > 0
                ? StatusChoice.Restriction
                : BackgroundAction != BackgroundAction.None
                ? StatusChoice.BackgroundAction
                : SelectedFiles > 0
                ? StatusChoice.SelectedItems
                : StatusChoice.Status;
    }

    public BackgroundAction BackgroundAction
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                OnChanged(nameof(BackgroundAction));
                OnChanged(nameof(StatusChoice));
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
            CurrentDirectoryCount = folderContext.CurrentDirectoryCount;
            CurrentFileCount = folderContext.CurrentFileCount;
            SelectedPath = folderContext.SelectedPath;
            // ExifData = folderContext.ExifData;
            BackgroundAction = folderContext.BackgroundAction;
            // SelectedFiles = folderContext.SelectedFiles;
        }
    }

    void FolderContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (folderContext != null)
            switch (e.PropertyName)
            {
                case nameof(CurrentDirectoryCount):
                    CurrentDirectoryCount = folderContext.CurrentDirectoryCount;
                    break;
                case nameof(CurrentFileCount):
                    CurrentFileCount = folderContext.CurrentFileCount;
                    break;
                case nameof(SelectedPath):
                    SelectedPath = folderContext.SelectedPath;
                    break;
                // case nameof(ExifData):
                //     ExifData = folderContext.ExifData;
                //     break;
                // case nameof(SelectedFiles):
                //     SelectedFiles = folderContext.SelectedFiles;
                //     break;
                case nameof(BackgroundAction):
                    BackgroundAction = folderContext.BackgroundAction;
                    break;
            }
    }

    FolderContext? folderContext;

    void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));
}
