using Gtk4DotNet;

class FolderPaned : Paned
{
    public FolderPaned(Builder builder, string name) : base(builder, name)
    {
        this["position"].OnNotify += OnPosition;
        columnviewLeft.ItemsChange += OnItemsChange;
        columnviewRight.ItemsChange += OnItemsChange;

        activeView = columnviewLeft;

        var leftEvents = FocusEventController.New();
        leftEvents.OnEnter += () =>
        {
            activeView = columnviewLeft;
            lastActiveView = columnviewLeft;
        };
        leftEvents.OnLeave += () => activeView = null;

        var rightEvents = FocusEventController.New();
        rightEvents.OnEnter += () =>
        {
            activeView = columnviewRight;
            lastActiveView = columnviewRight;
        };
        rightEvents.OnLeave += () => activeView = null;
        columnviewLeft.AddController(leftEvents);
        columnviewRight.AddController(rightEvents);

        var kec = KeyEventController.New();
        kec.SetPropagationPhase(PropagationPhase.Capture);
        kec.OnKeyPressed += OnKey;
        AddController(kec);
    }

    // TODO change to focus active folder
    public void SetFocus() => columnviewLeft.GrabFocus();

    void OnPosition()
    {
        if (columnviewLeft.Width == 0 && columnviewRight.Width == 0)
            return;
        columnviewLeft.OnWidth();
        columnviewRight.OnWidth();
    }

    void OnItemsChange(bool start)
    {
        if (start)
            lastPosition = Position;
        else if (lastPosition > 0)
            Position = lastPosition;
    }


    bool OnKey(char chr, KeyModifiers key)
    {
        if (chr == (char)ConsoleKey.Tab && !key.HasFlag(KeyModifiers.Shift))
        {
            GetInactiveView()?.GrabFocus();
            return true;
        }
        else
            return false;
    }

    ColumnView GetInactiveView() => columnviewLeft == activeView ? columnviewRight : columnviewLeft;

    [Widget]
    FolderView columnviewLeft = null!;

    [Widget]
    FolderView columnviewRight = null!;

    ColumnView? activeView;
    ColumnView lastActiveView = null!;
    int lastPosition;
}