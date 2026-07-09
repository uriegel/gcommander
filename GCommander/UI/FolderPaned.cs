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
        leftEvents.OnLeave += () => activeView = null;
        var rightEvents = FocusEventController.New();
        rightEvents.OnEnter += () =>
        {
            if (onItemsSet)
                return;
            MainContext.Instance.ChangeFolderContext(folderViewRight.Context);
            activeView = folderViewRight;
            lastActiveView = folderViewRight;
        };
        rightEvents.OnLeave += () => activeView = null;
        
        folderViewLeft.AddController(leftEvents);
        folderViewRight.AddController(rightEvents);

        var kec = KeyEventController.New();
        kec.SetPropagationPhase(PropagationPhase.Capture);
        kec.OnKeyPressed += OnKey;
        AddController(kec);
    }

    public void Initialize(int width)
    {
        Position = width / 2;
        folderViewLeft.Initialize();
        folderViewRight.Initialize();
        SetFocus();
    }

    public void SetFocus() => activeView?.ColumnView.GrabFocus();

    void OnPosition()
    {
        if (folderViewLeft.ColumnView.Width == 0 && folderViewRight.ColumnView.Width == 0)
            return;
        folderViewLeft.OnWidth();
        folderViewRight.OnWidth();
    }

    public void Refresh() => lastActiveView?.Refresh();
    public void SelectAll() => lastActiveView?.SelectAll();
    public void SelectNone() => lastActiveView?.SelectNone();
    public void SelectAllAbove() => lastActiveView?.SelectAllAbove();
    public void SelectAllBeneath() => lastActiveView?.SelectAllBeneath();
    public void ToggleSelection() => lastActiveView?.ToggleSelection();
    public void AdaptPath() => GetInactiveView().ChangePath(lastActiveView.Context.CurrentPath);

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