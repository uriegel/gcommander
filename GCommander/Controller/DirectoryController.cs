using CsTools.Extensions;
using Gtk4DotNet;

class DirectoryController : Controller
{
    public static DirectoryController Get(Controller? current, ColumnView view, FolderViewController folderView)
        => current is DirectoryController directoryController
            ? directoryController
            : new DirectoryController(current, view, folderView);

    public override async void ChangePath(string path)
    {
        var folderToSelect = path.EndsWith("..") ? currentPath.SubstringAfterLast('/') : null;
        var items = await Get(path);
        store.Splice(0, store.ItemsCount(), items);
        int pos = folderToSelect != null
            ? model
                .GetItems<DirectoryItem>()
                .Select((n, i) => new DirItemPos(Item: n, Pos: i))
                .FirstOrDefault(n => n.Item.Name == folderToSelect)?.Pos
                ?? 0
            : 0;
        view.ScrollTo(pos, ListScrollFlags.ScrollFocus);
        folderView.CheckCurrentChanged(pos, true);
    }

    public override string GetChangePath(int pos) => GetItemPath(pos);

    public override string GetItemPath(int pos)
        => currentPath.AppendPath(model.GetItem<DirectoryItem>(pos)?.Name ?? "");

    public override void Refresh()
    {
        throw new NotImplementedException();
    }

    public DirectoryController(Controller? previous, ColumnView view, FolderViewController folderView)
        : base(MultiSelection.New)
    {
        this.view = view;
        this.folderView = folderView;

        var namefactory = SignalListItemFactory
            .New()
            .Setup(listitem =>
            {
                using var builder = Builder.FromDotNetResource("icon-name-item");
                var item = new IconNameItem(builder);
                listitem.SetManagedChild(item);
            })
            .Bind(listitem =>
            {
                var iconname = listitem.GetManagedChild<IconNameItem>();
                var item = listitem.GetItem<DirectoryItem>();
                iconname?.Name = item?.Name ?? "";
                if (item?.Type == DirectoryItemType.Parent)
                    iconname?.SetFromIconName("go-up");
                else if (item?.Type == DirectoryItemType.Directory)
                    iconname?.SetFromIconName("folder-open");
                else
                    iconname?.SetIcon(item?.Name ?? "");
                // var row = iconname?.GetParent()?.GetParent();
                // row?.AddCssClass("hiddenItem", item?.IsMounted != true);
            });

        var datefactory = SignalListItemFactory
            .New()
            .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.Start).SetEllipsize(EllipsizeMode.End)))
            .Bind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                var item = listitem.GetItem<DirectoryItem>();
                label.Text = item?.DateTime.ToString() ?? "";
            });

        // var mountPointfactory = SignalListItemFactory
        //     .New()
        //     .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.Start).SetEllipsize(EllipsizeMode.End)))
        //     .Bind(listitem =>
        //     {
        //         var label = listitem.GetChild<Label>();
        //         var item = listitem.GetItem<RootItem>();
        //         label.Text = item?.MountPoint ?? "";
        //     });

        // var usefactory = SignalListItemFactory
        //     .New()
        //     .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.End).SetEllipsize(EllipsizeMode.End)))
        //     .Bind(listitem =>
        //     {
        //         var label = listitem.GetChild<Label>();
        //         var item = listitem.GetItem<RootItem>();
        //         label.Text = item?.Use != null ? $"{item?.Use}%" : "";
        //         if (item?.Use > 90)
        //             label?.GetParent()?.AddCssClass("warning", item?.IsMounted == true);
        //     });

        // var sizefactory = SignalListItemFactory
        //     .New()
        //     .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.End).SetEllipsize(EllipsizeMode.End)))
        //     .Bind(listitem =>
        //     {
        //         var label = listitem.GetChild<Label>();
        //         var item = listitem.GetItem<RootItem>();
        //         label.Text = item?.Size.FormatSize() ?? "";
        //     });

        view.SetModel(null);
        view.ClearColumns();
        view.SetModel(model);

        previous?.Dispose();

        using var nameSorter = CustomSorter.New<DirectoryItem>((item1, item2) => 0); //(item1?.Name ?? "").CompareTo(item2?.Name ?? ""));
        using var nameMultiSorter = MultiSorter.New().Append(CustomSorter.New<DirectoryItem>(SortDirectoriesFirst)).Append(nameSorter);
        var firstCol = ColumnViewColumn
            .New("Name", namefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(nameMultiSorter));
        view.AppendColumn(firstCol);
        view.SortByColumn(firstCol);

        using var dateSorter = CustomSorter.New<DirectoryItem>((item1, item2) => (item1?.DateTime ?? DateTime.MinValue).CompareTo(item2?.DateTime ?? DateTime.MinValue));
        using var dateMultiSorter = MultiSorter.New().Append(CustomSorter.New<DirectoryItem>(SortDirectoriesFirst)).Append(dateSorter);
        var dateCol = ColumnViewColumn
            .New("Datum", datefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(dateMultiSorter));
        view.AppendColumn(dateCol);
        view.SortByColumn(dateCol);
        // view.AppendColumn(ColumnViewColumn
        //     .New("Bezeichnung", descriptionfactory)
        //     .Expand()
        //     .SideEffect(cvc => cvc.SetSorter(descriptionMultiSorter))
        // );
        // using var mountPointSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.MountPoint ?? "").CompareTo(item2?.MountPoint ?? ""));
        // using var mountPointMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(mountPointSorter);
        // view.AppendColumn(ColumnViewColumn
        //     .New("MountPoint", mountPointfactory)
        //     .Expand()
        //     .SideEffect(cvc => cvc.SetSorter(mountPointMultiSorter))
        // );
        // using var useSorter = CustomSorter.New<RootItem>((item1, item2) => SortSize(item1?.Use, item2?.Use));
        // using var useMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(useSorter);
        // view.AppendColumn(ColumnViewColumn
        //     .New("%", usefactory)
        //     .SideEffect(cvc => cvc.SetSorter(useMultiSorter))
        // );
        // using var sizeSorter = CustomSorter.New<RootItem>((item1, item2) => SortSize(item1?.Size, item2?.Size));
        // using var sizeMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(sizeSorter);
        // view.AppendColumn(ColumnViewColumn
        //     .New("Größe", sizefactory)
        //     .SideEffect(cvc => cvc.SetSorter(sizeMultiSorter))
        // );

        using var viewsorter = view.GetSorter();
        viewsorter.OnChanged -= folderView.SortOrderChanged;
        viewsorter.OnChanged += folderView.SortOrderChanged;
        sortModel.SetSorter(viewsorter);

        // StartMonitoring();
    }
    
    async Task<DirectoryItem[]> Get(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        var dirs = dirInfo
                        .GetDirectories()
                        .Select(DirectoryItem.CreateDirItem)
                        //.Where(n => getFiles.ShowHidden == true || !n.IsHidden == true)
                        .OrderBy(n => n.Name)
                        .ToArray();
        var files = dirInfo
                        .GetFiles()
                        .Select(DirectoryItem.CreateFileItem)
                        //.Where(n => getFiles.ShowHidden == true || !n.IsHidden == true)
                        .ToArray();
        currentPath = dirInfo.FullName;                        
        return [
            new DirectoryItem("..", DirectoryItemType.Parent),
            .. dirs,
            .. files
        ];
    }

    int SortDirectoriesFirst(DirectoryItem? item1, DirectoryItem? item2)
    {
        return 0;
        // var order = item1?.IsMounted == true && item2?.IsMounted != true
        //     ? -1
        //     : item2?.IsMounted == true && item1?.IsMounted != true
        //     ? 1
        //     : 0;
        // return folderView.ReverseSortOrder ? -order : order;
    }

    readonly FolderViewController folderView;
    readonly ColumnView view;

    string currentPath = "";

    #region IDisposable

    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                //                volumeMonitor?.Dispose();
            }

            // Free unmanaged resources owned by DerivedClass

            disposed = true;
        }

        base.Dispose(disposing);
    }
    bool disposed;

    #endregion
}

record DirItemPos(DirectoryItem Item, int Pos);