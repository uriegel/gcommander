using Gtk4DotNet;

class FolderPaned : Paned
{
    public FolderPaned(Builder builder, string name) : base(builder, name)
    {
        this["position"].OnNotify += OnPosition;

        folderViewLeft.ItemsSet += OnItemsSet;
        folderViewRight.ItemsSet += OnItemsSet;

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

        setPositionSetLater();

        async void setPositionSetLater()
        {
            await Task.Delay(600);
            SetBool("position-set", true);
        }
    }

    public void SetFocus() => activeView?.ColumnView.GrabFocus();

    public void ShowHidden(bool show) { }

    void OnPosition()
    {
        if (folderViewLeft.ColumnView.Width == 0 && folderViewRight.ColumnView.Width == 0)
            return;
        folderViewLeft.OnWidth();
        folderViewRight.OnWidth();
    }

    async void OnItemsSet(bool start) => onItemsSet = start;

    bool OnKey(char chr, KeyModifiers key)
    {
        if (chr == (char)ConsoleKey.Tab)
        {
            if (!key.HasFlag(KeyModifiers.Shift))
                GetInactiveView()?.ColumnView.GrabFocus();
            else
                activeView?.StartEditing();
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
}