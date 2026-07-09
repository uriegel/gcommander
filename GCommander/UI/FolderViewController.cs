using Gtk4DotNet;

class FolderViewController
{
    public int CurrentPos { get; private set; } = -1;

    public FolderViewController(FolderView view)
    {
        this.view = view;
        var controller = ClickGesture.New();
        controller.OnPressed += (c, x, y, keys) =>
        {
            int pos = view.ColumnView.GetFocusedItemPos();
            SelectionChanged(pos);
            if (keys.HasFlag(KeyModifiers.Control))
                view.ToggleSelection(pos);
        };
        view.ColumnView.AddController(controller);
    }
    
    public void SelectionChanged(int newPos)
    {
        CurrentPos = newPos;
        view.SelectionChanged(CurrentPos);
    }

    public void OnItemsChange(bool start) => view.OnItemsChange(start);

    public void OnItemsGet(bool start) => view.OnItemsGet(start);

    public void CountsChanged(int dirCount, int fileCount)
    {
        view.Context.CurrentDirectoryCount = dirCount;  
        view.Context.CurrentFileCount = fileCount;
    } 

    static int GetNumberOfVisibleRows(ColumnView? view)
    {
        if (view == null)
            return 0;
        var row = view.GetRoot<Window>()?.GetFocus<Widget>();
        if (row != null && !row.IsInvalid && row.WidgetName == "GtkColumnViewRowWidget")
            return (view.Height / (row.Height + 1)) - 4;
        else
            return 0;
    }

    readonly FolderView view;
}