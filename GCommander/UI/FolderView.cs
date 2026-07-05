using Gtk4DotNet;

class FolderView : Box
{
    public event Action<bool>? ItemsChange;
    public event Action<bool>? ItemsSet;

    public FolderContext Context { get; } = new();

    public FolderViewController FolderViewController { get; }

    public FolderView(Builder builder, string name, nint parent)
        : base(builder, "folderview", widget => ReplacePlaceHolder(name, parent, widget))
    {
        id = name == "folderViewLeft" ? "left" : "right";
        editablePath.DataContext = Context;
        editablePath["editing"].OnNotify += () =>
        {
            Context.IsEditing = editablePath.IsEditing;
            if (!Context.IsEditing)
            {
                ColumnView.GrabFocus();
                ChangePath(editablePath.Text);
            }
        };
        editablePath.Binding("text", nameof(FolderContext.CurrentPath), BindingFlags.Default);

        FolderViewController = new(this);
        controller = Controller.GetFromPath(id, null, null, ColumnView, FolderViewController, Context)!;

        ColumnView.OnActivate += Activate;

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

    public void Refresh() => controller.Refresh();

    public void SelectionChanged(int pos)
    {
        Context.SelectedPath = controller.GetItemPath(pos);
        //Context.ExifData = controller.GetExifData(CurrentPos);
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

    void Activate(int position)
    {
        var changePath = controller.GetChangePath(position);
        ChangePath(changePath);
    }

    void ChangePath(string path)
    {
        controller = Controller.GetFromPath(id, path, controller, ColumnView, FolderViewController, Context);
        controller.ChangePath(path);
    }

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

    Controller controller;

    readonly string id;
}

