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
        editablePath.DataContext = Context;
        editablePath["editing"].OnNotify += () =>
        {
            Context.IsEditing = editablePath.IsEditing;
            if (!Context.IsEditing)
            {
                ColumnView.GrabFocus();
                controller?.ChangePath(editablePath.Text);
            }
        };
        editablePath.Binding("text", nameof(FolderContext.CurrentPath), BindingFlags.Default);

        FolderViewController = new(this);
        controller = Controller.GetFromPath(null, null, ColumnView, FolderViewController, Context)!;
        controller.ChangePath("");

        ColumnView.OnActivate += Activate;

        OnFinalize(() =>
        {
            controller.Dispose();
        });
    }

    public void ChangePath(string path) { }

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
        controller = Controller.GetFromPath(changePath, controller, ColumnView, FolderViewController, Context);
        controller.ChangePath(changePath);
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
}

