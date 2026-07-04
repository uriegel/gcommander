using Gtk4DotNet;

class FolderViewController
{
    public int CurrentPos { get; private set; } = -1;

    public FolderViewController(FolderView view)
    {
        this.view = view;
        var controller = ClickGesture.New();
        controller.OnPressed += (c, _, _) =>
        {
            int pos = view.ColumnView.GetFocusedItemPos();
            CheckCurrentChanged(pos);
        };
        view.ColumnView.AddController(controller);

        var kec = KeyEventController.New();
        kec.SetPropagationPhase(PropagationPhase.Capture);
        kec.OnKeyPressed += OnKey;
        view.ColumnView.AddController(kec);
    }
    
    bool OnKey(char key, KeyModifiers mod)
    {
        switch (key)
        {
            case (char)ConsoleKey.UpArrow:
            case (char)ConsoleKey.DownArrow:
                var pos = view.ColumnView.GetFocusedItemPos();
                var newPos = key switch
                {
                    (char)ConsoleKey.UpArrow => Math.Max(pos - 1, 0),
                    (char)ConsoleKey.DownArrow => Math.Min(pos + 1, view.ColumnView.ItemsCount() - 1),
                    _ => 0
                };
                view.ColumnView.ScrollTo(newPos, ListScrollFlags.ScrollFocus);
                CheckCurrentChanged(newPos);
                return true;
            case (char)ConsoleKey.Home:
                view.ColumnView.ScrollTo(0, ListScrollFlags.ScrollFocus);
                CheckCurrentChanged(0);
                return true;
            case (char)ConsoleKey.End:
                var total = view.ColumnView.ItemsCount() - 1;
                view.ColumnView.ScrollTo(total, ListScrollFlags.ScrollFocus);
                CheckCurrentChanged(total);
                return true;
            case (char)ConsoleKey.PageUp:
            case (char)ConsoleKey.PageDown:
                var pageSize = GetNumberOfVisibleRows(view.ColumnView);
                pos = view.ColumnView.GetFocusedItemPos();
                newPos = key switch
                {
                    (char)ConsoleKey.PageUp => Math.Max(pos - pageSize, 0),
                    (char)ConsoleKey.PageDown => Math.Min(pos + pageSize, view.ColumnView.ItemsCount() - 1),
                    _ => 0
                };
                view.ColumnView.ScrollTo(newPos, ListScrollFlags.ScrollFocus);
                CheckCurrentChanged(newPos);
                return true;
        }
        return false;
    }

    public void CheckCurrentChanged(int newPos, bool force = false)
    {
        if (newPos != CurrentPos || force)
        {
            CurrentPos = newPos;
            view.SelectionChanged(CurrentPos);
        }
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