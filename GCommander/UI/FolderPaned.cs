using Gtk4DotNet;

class FolderPaned : Paned
{
    public FolderPaned(Builder builder, string name) : base(builder, name)
    {
        this["position"].OnNotify += OnPosition;
        folderViewLeft.ItemsSet += OnItemsSet;
        folderViewRight.ItemsSet += OnItemsSet;
        folderViewLeft.ItemsChange += OnItemsChange;
        folderViewRight.ItemsChange += OnItemsChange;

        activeView = folderViewLeft;    
        MainContext.Instance.ChangeFolderContext(folderViewLeft.Context);

        var leftEvents = FocusEventController.New();
        leftEvents.OnEnter += () =>
        {
            if (onItemsSet)
                return;
            MainContext.Instance.ChangeFolderContext(folderViewLeft.Context);
            activeView = folderViewLeft;
            lastActiveView = folderViewLeft;
        };
        leftEvents.OnLeave += () =>
        {
            lastActiveView = folderViewRight;
            activeView = null;
        };
        var rightEvents = FocusEventController.New();
        rightEvents.OnEnter += () =>
        {
            if (onItemsSet)
                return;
            MainContext.Instance.ChangeFolderContext(folderViewRight.Context);
            activeView = folderViewRight;
            lastActiveView = folderViewRight;
        };
        rightEvents.OnLeave += () =>
        {
            lastActiveView = folderViewLeft;
            activeView = null;
        };
        folderViewLeft.AddController(leftEvents);
        folderViewRight.AddController(rightEvents);

        var kec = KeyEventController.New();
        kec.SetPropagationPhase(PropagationPhase.Capture);
        kec.OnKeyPressed += OnKey;
        AddController(kec);
    }

    public void SetFocus() => activeView?.GrabFocus();

    public void ShowHidden(bool show) {}

    void OnPosition()
    {
        if (folderViewLeft.ColumnView.Width == 0 && folderViewRight.ColumnView.Width == 0)
            return;
        folderViewLeft.OnWidth();
        folderViewRight.OnWidth();
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

    FolderView GetInactiveView() => folderViewLeft == lastActiveView ? folderViewRight : folderViewLeft;

    [Widget(Template = "folderview")]
    readonly FolderView folderViewLeft = null!;

    [Widget(Template = "folderview")]
    readonly FolderView folderViewRight = null!;

    FolderView? activeView;
    FolderView lastActiveView = null!;

    bool onItemsSet;
    
    int lastPosition;
}