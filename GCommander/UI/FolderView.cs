using Gtk4DotNet;

class FolderView : Box
{
    public event Action<bool>? ItemsChange;
    public event Action<bool>? ItemsSet;

    public FolderContext Context { get; } = new();

    public int CurrentPos { get; private set; } = -1;

    public FolderView(Builder builder, string name, nint parent)
        : base(builder, "folderview", widget => ReplacePlaceHolder(name, parent, widget))
    {
        id = name == "folderViewLeft" ? "left" : "right";
        DataContext = Context;
        editablePath["editing"].OnNotify += () =>
        {
            Context.IsEditing = editablePath.IsEditing;
            if (!Context.IsEditing)
            {
                ColumnView.GrabFocus();
                ChangePath(editablePath.Text);
            }
        };
        editablePath.Binding("text", nameof(FolderContext.CurrentPath));
        searchBar.SetBinding("search-mode-enabled", nameof(FolderContext.Restriction), BindingFlags.Default, obj => ((string?)obj)?.Length > 0);
        searchEntry.SetBinding("text", nameof(FolderContext.Restriction), BindingFlags.Bidirectional);

        controller = Controller.GetFromPath(id, null, null, this, Context)!;

        ColumnView.OnActivate += Activate;

        var clickGesture = ClickGesture.New();
        clickGesture.OnPressed += (c, x, y, keys) =>
        {
            int pos = ColumnView.GetFocusedItemPos();
            SelectionChanged(pos);
            if (keys.HasFlag(KeyModifiers.Control))
                ToggleSelection(pos);
        };
        ColumnView.AddController(clickGesture);

        var keyController = KeyEventController.New();
        keyController.OnKeyPressed += (chr, modifiers) =>
        {
            if (chr == (char)ConsoleKey.Escape)
                StopRestriction();
            else if (chr == (char)ConsoleKey.Backspace)
            {
                if (Context.Restriction == string.Empty)
                {
                    StopRestriction();
                    //                    var path = history.Get(modifiers.HasFlag(KeyModifiers.Shift));
                    // if (path != null)
                    //     ChangePath(path, false);
                }
                else
                {
                    Context.Restriction = Context.Restriction[..^1];
                    if (Context.Restriction?.Length == 0)
                        Context.Restriction = string.Empty;
                    controller.FilterChanged(FilterChange.LessStrict);
                }
            }
            else
            {
                if (chr != '\0')
                {
                    var searchKey = Context.Restriction + chr;
                    if (controller.CheckRestriction(searchKey))
                    {
                        Context.Restriction = searchKey;
                        controller.FilterChanged(FilterChange.MoreStrict);
                    }
                }
            }
            return false;
        };
        ColumnView.AddController(keyController);

        OnFinalize(() =>
        {
            controller.Dispose();
        });
    }
    
    public void Initialize()
    {
        var path = Application.Settings.GetString($"path-{id}") ?? "";
        ChangePath(path);
    }

    public void StartEditing() => editablePath.StartEditing();
    public void SelectAll() => controller.SelectAll();
    public void SelectNone() => controller.SelectNone();
    public void SelectAllAbove() => controller.SelectAllAbove();
    public void SelectAllBeneath() => controller.SelectAllBeneath();
    public void ToggleSelection() => controller.ToggleSelection();
    public void ToggleSelection(int pos) => controller.ToggleSelection(pos);
    public void Refresh() => ChangePath(Context.CurrentPath);

    public void SelectionChanged(int pos)
    {
        CurrentPos = pos;
        Context.SelectedPath = controller.GetItemPath(pos);
        Context.ExifData = controller.GetExifData(CurrentPos);
    }

    public void CountsChanged(int dirCount, int fileCount)
    {
        Context.CurrentDirectoryCount = dirCount;
        Context.CurrentFileCount = fileCount;
    } 

    public void OnWidth() => controller.OnWidth(Width);

    public void OnItemsChange(bool start)
    {
        ItemsChange?.Invoke(start);
        if (start == false)
        {
            Context.CurrentFileCount = controller.GetFileCount();
            Context.CurrentDirectoryCount = controller.GetDirectoryCount();
        }
    }

    public void OnItemsGet(bool start) => ItemsSet?.Invoke(start);

    public async void ChangePath(string path)
    {
        try
        {
            StopRestriction();
            controller = Controller.GetFromPath(id, path, controller, this, Context);
            await controller.ChangePathAsync(path);
        }
        catch (DirectoryNotFoundException dnfe)
        {
            Console.Error.WriteLine($"Der Pfad konnte nicht geändert werden: {dnfe}");
            MainContext.Instance.ErrorText = "Verzeichnis nicht vorhanden";
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Der Pfad konnte nicht geändert werden: {e}");
            MainContext.Instance.ErrorText = "Verzeichnis konnte nicht gewechselt werden";
        }
    }

    async void Activate(int position)
    {
        var changePath = await controller.GetChangePath(position);
        ChangePath(changePath);
    }

    void StopRestriction()
        => Gtk.BeginInvoke(200, () =>
            {
                Context.Restriction = string.Empty;
                controller.FilterChanged(FilterChange.LessStrict);
            });

    static void ReplacePlaceHolder(string name, nint parent, nint widget)
    {
        if (name == "folderViewLeft")
            parent.PanedSetStartChild(widget);
        else
            parent.PanedSetEndChild(widget);
    }

    [Widget(Name = "columnview")]
    public readonly ColumnView ColumnView = null!;

    [Widget]
    public readonly EditableLabel editablePath = null!;

    [Widget]
    public readonly SearchBar searchBar = null!;

    [Widget]
    readonly SearchEntry searchEntry = null!;

    Controller controller;

    readonly string id;
}

