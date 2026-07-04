using Gtk4DotNet;

class FolderPaned : Paned
{
    public FolderPaned(Builder builder, string name) : base(builder, name)
    {
        this["position"].OnNotify += OnPosition;
        columnviewLeft.ItemsSet += OnItemsSet;
        columnviewRight.ItemsSet += OnItemsSet;
        columnviewLeft.ItemsChange += OnItemsChange;
        columnviewRight.ItemsChange += OnItemsChange;

        activeView = columnviewLeft;    
        MainContext.Instance.ChangeFolderContext(columnviewLeft.Context);

        var leftEvents = FocusEventController.New();
        leftEvents.OnEnter += () =>
        {
            if (onItemsSet)
                return;
            MainContext.Instance.ChangeFolderContext(columnviewLeft.Context);
            activeView = columnviewLeft;
            lastActiveView = columnviewLeft;
        };
        leftEvents.OnLeave += () =>
        {
            lastActiveView = columnviewRight;
            activeView = null;
        };
        var rightEvents = FocusEventController.New();
        rightEvents.OnEnter += () =>
        {
            if (onItemsSet)
                return;
            MainContext.Instance.ChangeFolderContext(columnviewRight.Context);
            activeView = columnviewRight;
            lastActiveView = columnviewRight;
        };
        rightEvents.OnLeave += () =>
        {
            lastActiveView = columnviewLeft;
            activeView = null;
        };
        columnviewLeft.AddController(leftEvents);
        columnviewRight.AddController(rightEvents);

        var kec = KeyEventController.New();
        kec.SetPropagationPhase(PropagationPhase.Capture);
        kec.OnKeyPressed += OnKey;
        AddController(kec);
    }

    public void SetFocus() => activeView?.GrabFocus();

    public void ShowHidden(bool show) {}

    void OnPosition()
    {
        if (columnviewLeft.Width == 0 && columnviewRight.Width == 0)
            return;
        columnviewLeft.OnWidth();
        columnviewRight.OnWidth();
    }
    
    async void OnItemsSet(bool start) => onItemsSet = start;
    
    async void OnItemsChange(bool start)
    {
        if (start)
            lastPosition = Position;
        else if (lastPosition > 0)
        {
            await Task.Delay(10);
            Position = lastPosition;
        }
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

    ColumnView GetInactiveView() => columnviewLeft == lastActiveView ? columnviewRight : columnviewLeft;

    [Widget]
    FolderView columnviewLeft = null!;

    [Widget]
    FolderView columnviewRight = null!;

    ColumnView? activeView;
    ColumnView lastActiveView = null!;

    bool onItemsSet;
    
    int lastPosition;
}