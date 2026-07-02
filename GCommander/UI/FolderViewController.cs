using Gtk4DotNet;

class FolderViewController
{
    public bool ReverseSortOrder { get; private set; }

    public int CurrentPos { get; private set; } = -1;

    public FolderViewController(FolderView view)
    {
        this.view = view;
        var controller = ClickGesture.New();
        controller.OnPressed += (c, _, _) =>
        {
            int pos = view.GetFocusedItemPos();
            CheckCurrentChanged(pos);
        };
        view.AddController(controller);

        var kec = KeyEventController.New();
        kec.SetPropagationPhase(PropagationPhase.Capture);
        kec.OnKeyPressed += OnKey;
        view.AddController(kec);

    }
    
    public void SortOrderChanged(bool reverse, SorterChange _) => ReverseSortOrder = reverse;

    bool OnKey(char key, KeyModifiers mod)
    {
        switch (key)
        {
            case (char)ConsoleKey.UpArrow:
            case (char)ConsoleKey.DownArrow:
                var pos = view.GetFocusedItemPos();
                var newPos = key switch
                {
                    (char)ConsoleKey.UpArrow => Math.Max(pos - 1, 0),
                    (char)ConsoleKey.DownArrow => Math.Min(pos + 1, view.ItemsCount() - 1),
                    _ => 0
                };
                view.ScrollTo(newPos, ListScrollFlags.ScrollFocus);
                CheckCurrentChanged(newPos);
                return true;
            case (char)ConsoleKey.Home:
                view.ScrollTo(0, ListScrollFlags.ScrollFocus);
                CheckCurrentChanged(0);
                return true;
            case (char)ConsoleKey.End:
                var total = view.ItemsCount() - 1;
                view.ScrollTo(total, ListScrollFlags.ScrollFocus);
                CheckCurrentChanged(total);
                return true;
            case (char)ConsoleKey.PageUp:
            case (char)ConsoleKey.PageDown:
                var pageSize = GetNumberOfVisibleRows(view);
                pos = view.GetFocusedItemPos();
                newPos = key switch
                {
                    (char)ConsoleKey.PageUp => Math.Max(pos - pageSize, 0),
                    (char)ConsoleKey.PageDown => Math.Min(pos + pageSize, view.ItemsCount() - 1),
                    _ => 0
                };
                view.ScrollTo(newPos, ListScrollFlags.ScrollFocus);
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