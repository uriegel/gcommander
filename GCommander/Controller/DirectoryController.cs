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
        folderView.OnItemsChange(true);
        store.Splice(0, store.ItemsCount(), items);
        folderView.OnItemsChange(false);
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
        : base(CustomFilter.New<DirectoryItem>(FilterHidden), MultiSelection.New)
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
                var row = iconname?.GetParent()?.GetParent();
                row?.AddCssClass("hiddenItem", item?.IsHidden == true);
            });

        var datefactory = SignalListItemFactory
            .New()
            .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.Start).SetEllipsize(EllipsizeMode.End)))
            .Bind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                var item = listitem.GetItem<DirectoryItem>();
                label.Text = item != null && item.DateTime.HasValue ? item.DateTime.Value.ToString("g") : "";
            });

        var sizefactory = SignalListItemFactory
            .New()
            .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.End).SetEllipsize(EllipsizeMode.End)))
            .Bind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                var item = listitem.GetItem<DirectoryItem>();
                label.Text = item?.Size.FormatSize() ?? "";
            });

        view.SetModel(null);
        view.ClearColumns();
        view.SetModel(model);

        previous?.Dispose();

        using var nameSorter = CustomSorter.New<DirectoryItem>(NameOrExtensionOrder);
        using var nameMultiSorter = MultiSorter.New().Append(CustomSorter.New<DirectoryItem>(SortDirectoriesFirst)).Append(nameSorter);
        var firstCol = ColumnViewColumn
            .New(NAME, namefactory)
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

        using var sizeSorter = CustomSorter.New<DirectoryItem>((item1, item2) => SortSize(item1?.Size, item2?.Size));
        using var sizeMultiSorter = MultiSorter.New().Append(CustomSorter.New<DirectoryItem>(SortDirectoriesFirst)).Append(sizeSorter);
        var sizeCol = ColumnViewColumn
            .New("Größe", sizefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(sizeMultiSorter));
        view.AppendColumn(sizeCol);

        using var viewsorter = view.GetSorter();
        viewsorter.OnChanged -= SortOrderChanged;
        viewsorter.OnChanged += SortOrderChanged;
        sortModel.SetSorter(viewsorter);

        //StartMonitoring();
    }

    async Task<DirectoryItem[]> Get(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        var dirs = dirInfo
                        .GetDirectories()
                        .Select(DirectoryItem.CreateDirItem)
                        .OrderBy(n => n.Name)
                        .ToArray();
        var files = dirInfo
                        .GetFiles()
                        .Select(DirectoryItem.CreateFileItem)
                        .ToArray();
        currentPath = dirInfo.FullName;
        return [
            new DirectoryItem("..", DirectoryItemType.Parent, false),
            .. dirs,
            .. files
        ];
    }

    public override int GetDirectoryCount() => model.GetItems<DirectoryItem>().Count(n => n.Type == DirectoryItemType.Directory);
    public override int GetFileCount() => model.GetItems<DirectoryItem>().Count(n => n.Type == DirectoryItemType.File);

    static bool FilterHidden(DirectoryItem? item) 
        => MainContext.Instance.ShowHiddenItems || item?.IsHidden != true;

    int NameOrExtensionOrder(DirectoryItem? item1, DirectoryItem? item2)
        => extensionSearch
            ? (item1?.Name.GetFileExtension() ?? "").CompareTo(item2?.Name.GetFileExtension() ?? "")
            : (item1?.Name ?? "").CompareTo(item2?.Name ?? "");
  
    void SortOrderChanged(bool reverse, ColumnViewColumn? col, SorterChange sc)
    {

        if ((lastSearchTitle == NAME || lastSearchTitle == ERWEITERUNG) && col?.Title == lastSearchTitle && reverseOrder != reverse && !reverse)
        {
            extensionSearch = lastSearchTitle == NAME;
            nameOrExt = col;
            col?.Title = extensionSearch ? ERWEITERUNG : NAME;
        }
        if (col?.Title != NAME && col?.Title != ERWEITERUNG && extensionSearch)
        {
            extensionSearch = false;
            // This is a little bit dangerous!!
            nameOrExt?.Title = NAME;
        }
        reverseOrder = reverse;
        lastSearchTitle = col?.Title ?? "";
    }
        
    int SortDirectoriesFirst(DirectoryItem? item1, DirectoryItem? item2)
    {
        var order = item1?.Type == DirectoryItemType.Parent
            ? 1
            : item2?.Type == DirectoryItemType.Parent
            ? 1
            : item1?.Type == DirectoryItemType.Directory && item2?.Type == DirectoryItemType.File
            ? -1
            : item2?.Type == DirectoryItemType.Directory && item1?.Type == DirectoryItemType.File
            ? 1
            : 0;
        return reverseOrder ? -order : order;
    }

    readonly FolderViewController folderView;
    readonly ColumnView view;
    string currentPath = "";
    bool reverseOrder;
    bool extensionSearch;
    string lastSearchTitle = "";
    ColumnViewColumn? nameOrExt;

    const string NAME = "Name";
    const string ERWEITERUNG = "Erweiterung";

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