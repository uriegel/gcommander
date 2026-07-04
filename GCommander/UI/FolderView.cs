using Gtk4DotNet;

class FolderView : ColumnView
{
    public event Action<bool>? ItemsChange;

    public FolderViewController FolderViewController { get; }
    
    public FolderView(Builder builder, string name) : base(builder, name)
    {
        FolderViewController = new(this);
        controller = Controller.GetFromPath(null, null, this, FolderViewController)!;
        controller.ChangePath("");

        OnActivate += Activate;

        OnFinalize(() =>
        {
            controller.Dispose();
        });
    }

    public void ChangePath(string path) { }

    public void Refresh() => controller.Refresh();

    public void SelectionChanged(int pos)
    {
        MainContext.Instance.SelectedPath = controller.GetItemPath(pos);
        //Context.ExifData = controller.GetExifData(CurrentPos);
    }

    public void OnWidth() => controller.OnWidth(Width);

    public void OnItemsChange(bool start) => ItemsChange?.Invoke(start);

    void Activate(int position)
    {
        var changePath = controller.GetChangePath(position);
        controller = Controller.GetFromPath(changePath, controller, this, FolderViewController);
        controller.ChangePath(changePath);
    }

    Controller controller;
}