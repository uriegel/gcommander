using System.ComponentModel;
using CsTools.Extensions;
using Gtk4DotNet;

class DirectoryController : Controller
{
    public static DirectoryController Get(string id, Controller? current, FolderView view, FolderContext context)
        => current is DirectoryController directoryController
            ? directoryController
            : new DirectoryController(id, current, view, context);

    public override async Task ChangePathAsync(string path, bool fromHistory)
    {
        var folderToSelect = path.EndsWith("..") ? context.CurrentPath.SubstringAfterLast('/') : null;
        cancellation.Cancel();
        cancellation = new();
        var items = await Get(path, fromHistory);
        var enableEvents = watcher.Path == "";
        watcher.Path = context.CurrentPath;
        if (enableEvents)
            watcher.EnableRaisingEvents = true;
        view.OnItemsChange(true);
        store.Splice(0, store.ItemsCount(), items);
        StartExifResolving(items.OfType<FileItem>());
        view.OnItemsChange(false);
        int pos = folderToSelect != null
            ? model
                .GetItems<Item>()
                .Select((n, i) => new DirItemPos(Item: n, Pos: i))
                .FirstOrDefault(n => n.Item.Name == folderToSelect)?.Pos
                ?? 0
            : 0;
        SetSelection(pos);

        MainContext.Instance.PropertyChanged -= OnPropertyChanged;
        MainContext.Instance.PropertyChanged += OnPropertyChanged;
    }

    public override async Task<string?> GetChangePath(int pos) => (string?)GetItemPath(pos);

    public override string GetItemPath(int pos)
        => context.CurrentPath.AppendPath(model.GetItem<Item>(pos)?.Name ?? "");

    public override ExifData? GetExifData(int pos)
        => model.GetItem<Item>(pos) is FileItem fileItem ? fileItem.ExifData : null;

    public DirectoryController(string id, Controller? previous, FolderView view, FolderContext context)
        : base(id, view, context)
    {
        // watcher.Created += WatchCreated;
        // watcher.Deleted += WatchDeleted;
        // watcher.Changed += WatchChanged;
        // watcher.Renamed += WatchRenamed;
        watcher.NotifyFilter = NotifyFilters.CreationTime
                    | NotifyFilters.DirectoryName
                    | NotifyFilters.FileName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Size;

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
                var item = listitem.GetItem<Item>();
                iconname?.Name = item?.Name ?? "";
                if (item is ParentItem)
                    iconname?.SetFromIconName("go-up");
                else if (item is DirectoryItem dirItem)
                    iconname?.SetFromIconName("folder-open");
                else if (item is FileItem fileItem)
                    iconname?.SetIcon(fileItem.Name);
            });

        var datefactory = SignalListItemFactory
            .New()
            .Setup(listitem =>
            {
                using var builder = Builder.FromDotNetResource("date-exif");
                var item = new DateExif(builder);
                listitem.SetManagedChild(item);
            })
            .Bind(listitem =>
            {
                var item = listitem.GetItem<Item>();
                var dateexif = listitem.GetManagedChild<DateExif>();
                var row = dateexif?.GetParent()?.GetParent();
                row?.DataContext = item;
                row?.AddCssClass("hiddenItem", item is FileSystemItem fsi && fsi.IsHidden);
                if (item is SelectableItem si)
                    row?.SetBindingToCss("selection", nameof(si.IsSelected));
                if (item is FileItem fileItem)
                {
                    dateexif?.DataContext = fileItem;
                    dateexif?.SetDateTimeBinding();
                    dateexif?.SetExifBinding();
                }
            })
            .Unbind(listitem =>
            {
                var dateexif = listitem.GetManagedChild<DateExif>();
                dateexif?.UnsetDateTimeBinding();
                dateexif?.UnsetExifBinding();
                var row = dateexif?.GetParent()?.GetParent();
                row?.UnsetBindingToCss("selection");
                row?.DataContext = null;
                dateexif?.DataContext = null;
            });

        var sizefactory = SignalListItemFactory
            .New()
            .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.End).SetEllipsize(EllipsizeMode.End)))
            .Bind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                if (listitem.GetItem<Item>() is FileItem fileItem)
                {
                    label.DataContext = fileItem;
                    label.SetBinding("label", nameof(fileItem.Size), BindingFlags.Default, s => ((long?)s).FormatSize());
                }
            })
            .Unbind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                label.UnsetBinding("label");
                label.DataContext = null;
            });

        view.ColumnView.SetModel(null);
        view.ColumnView.ClearColumns();
        view.ColumnView.SetModel(model);

        previous?.Dispose();

        using var nameSorter = CustomSorter.New<Item>(NameOrExtensionOrder);
        using var nameMultiSorter = MultiSorter.New().Append(CustomSorter.New<Item>(SortDirectoriesFirst)).Append(nameSorter);
        var firstCol = ColumnViewColumn
            .New(NAME, namefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(nameMultiSorter));
        view.ColumnView.AppendColumn(firstCol);
        view.ColumnView.SortByColumn(firstCol);

        using var dateSorter = CustomSorter.New<Item>((item1, item2) 
            => (item1 is FileSystemItem fsi1 ? fsi1.DateTime : DateTime.MinValue).CompareTo(item2 is FileSystemItem fsi2 ? fsi2.DateTime : DateTime.MinValue));
        using var dateMultiSorter = MultiSorter.New().Append(CustomSorter.New<Item>(SortDirectoriesFirst)).Append(dateSorter);
        var dateCol = ColumnViewColumn
            .New("Datum", datefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(dateMultiSorter));
        view.ColumnView.AppendColumn(dateCol);

        using var sizeSorter = CustomSorter.New<Item>(SortSize);
        using var sizeMultiSorter = MultiSorter.New().Append(CustomSorter.New<Item>(SortDirectoriesFirst)).Append(sizeSorter);
        var sizeCol = ColumnViewColumn
            .New("Größe", sizefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(sizeMultiSorter));
        view.ColumnView.AppendColumn(sizeCol);

        using var viewsorter = view.ColumnView.GetSorter();
        viewsorter.OnChanged -= SortOrderChanged;
        viewsorter.OnChanged += SortOrderChanged;
        sortModel.SetSorter(viewsorter);
    }

    public static string GetExifDate(ExifData? exif, string altValue)
        => exif != null && exif.DateTime != DateTime.MinValue
            ? exif.DateTime.ToString("g")
            : altValue;

    async Task<Item[]> Get(string path, bool fromHistory)
    {
        var dirInfo = new DirectoryInfo(path);
        var dirs = dirInfo
                        .GetDirectories()
                        .Select(DirectoryItem.New)
                        .OrderBy(n => n.Name)
                        .ToArray();
        var files = dirInfo
                        .GetFiles()
                        .Select(FileItem.New)
                        .ToArray();
        SetNewPath(dirInfo.FullName, fromHistory);
        Application.Settings.SetString($"path-{Id}", dirInfo.FullName);
        return [
            new ParentItem(),
            .. dirs,
            .. files
        ];
    }

    public override int GetDirectoryCount() => model.GetItems<Item>().OfType<DirectoryItem>().Count();
    public override int GetFileCount() => model.GetItems<Item>().OfType<FileItem>().Count();

    public override bool CheckRestriction(string searchKey)
        => model
            .GetItems<Item>()
            .Any(n => n.Name.StartsWith(searchKey, StringComparison.CurrentCultureIgnoreCase));

    protected override CustomFilter? CreateFilter() => CustomFilter.New<Item>(Filter);

    void StartExifResolving(IEnumerable<FileItem> items)
    {
        var taskId = BackgroundTasks.GetId();
        var token = BackgroundTasks.GetCancellationToken(cancellation.Token);
        BackgroundTasks.Add(taskId, Task.Run(() =>
        {
            context.BackgroundAction = BackgroundAction.ExifDatas;
            try
            {
                foreach (var item in items
                        .Where(item => !token.IsCancellationRequested &&
                            (item.Name.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase)
                                || item.Name.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase)
                                || item.Name.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))))
                    item.ExifData = ExifReader.GetExifData(context.CurrentPath.AppendPath(item.Name));
            }
            finally
            {
                BackgroundTasks.Remove(taskId);
                context.BackgroundAction = BackgroundAction.None;
            }
        }));
    }

    bool Filter(Item? item)
        => (MainContext.Instance.ShowHiddenItems || item is not FileSystemItem fsi || !fsi.IsHidden)
            && (view.Context.Restriction == null
                || item?.Name.StartsWith(view.Context.Restriction, StringComparison.CurrentCultureIgnoreCase) == true);


    int NameOrExtensionOrder(Item? item1, Item? item2)
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

    int SortDirectoriesFirst(Item? item1, Item? item2)
    {
        var order = item1 is ParentItem
            ? -1
            : item2 is ParentItem
            ? 1
            : item1 is DirectoryItem && item2 is FileItem
            ? -1
            : item2 is DirectoryItem && item1 is FileItem
            ? 1
            : 0;
        return reverseOrder ? -order : order;
    }

    void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainContext.ShowHiddenItems))
        {
            FilterChanged(MainContext.Instance.ShowHiddenItems ? FilterChange.LessStrict : FilterChange.MoreStrict);
            view.CountsChanged(GetDirectoryCount(), GetFileCount());
        }
    }

    // void WatchCreated(object _, FileSystemEventArgs e)
    // {
    //     try
    //     {
    //         store.Splice(0, 0, [FileItem.New(new FileInfo(e.FullPath))]);
    //         view.CountsChanged(GetDirectoryCount(), GetFileCount());
    //     }
    //     catch { }
    // }

    // void WatchDeleted(object _, FileSystemEventArgs e)
    // {
    //     var pos = store.GetItems<DirectoryItem>().TakeWhile(n => n.Name != e.Name).Count();
    //     store.Splice<DirectoryItem>(pos, 1, []);
    //     view.CountsChanged(GetDirectoryCount(), GetFileCount());
    // }
        
    // void WatchChanged(object _, FileSystemEventArgs e)
    // {
    //     var fileInfo = new FileInfo(context.CurrentPath.AppendPath(e.Name)); 
    //     var item = model.GetItems<DirectoryItem>().FirstOrDefault(n => n.Name == e.Name);
    //     item?.DateTime = fileInfo.LastWriteTime;
    //     item?.Size = fileInfo.Length;
    // }

    // void WatchRenamed(object _, RenamedEventArgs e)
    // {
    //     Console.WriteLine($"Renamed: {e.OldName} {e.Name}");
    //     int focused = model.Selected;
    //     var pos = model.GetItems<DirectoryItem>().TakeWhile(n => n.Name != e.OldName).Count();
    //     bool focusNew = pos == focused;

    //     var posToRemove = store.GetItems<DirectoryItem>().TakeWhile(n => n.Name != e.OldName).Count();
    //     if (pos != store.GetItemsCount())
    //         store.Remove(posToRemove);

    //     var fileInfo = new FileInfo(context.CurrentPath.AppendPath(e.Name));
    //     if (!File.Exists(context.CurrentPath.AppendPath(e.Name)))
    //         store.Splice(0, 0, [DirectoryItem.CreateFileItem(fileInfo)]);
    //     else
    //     {
    //         var item = model.GetItems<DirectoryItem>().FirstOrDefault(n => n.Name == e.Name);
    //         item?.DateTime = fileInfo.LastWriteTime;
    //         item?.Size = fileInfo.Length;
    //     }
    //     view.CountsChanged(GetDirectoryCount(), GetFileCount());

    //     if (focusNew)
    //     {
    //         var newPos = model
    //             .GetItems<DirectoryItem>()
    //             .Select((n, i) => new DirItemPos(Item: n, Pos: i))
    //             .FirstOrDefault(n => n.Item.Name == e.Name)?.Pos;
    //         if (newPos.HasValue)
    //             SetSelection(newPos.Value);
    //     }
    // }

    static int SortSize(Item? item1, Item? item2)
    {
        var a = item1 is FileItem fi1 ? fi1.Size : 0;
        var b = item2 is FileItem fi2 ? fi2.Size : 0;
        return a - b > 0
            ? 1
            : a - b < 0
            ? -1
            : 0;
    }
        
    readonly FileSystemWatcher watcher = new();
    bool reverseOrder;
    bool extensionSearch;
    string lastSearchTitle = "";
    ColumnViewColumn? nameOrExt;
    const string NAME = "Name";
    const string ERWEITERUNG = "Erweiterung";

    CancellationTokenSource cancellation = new();

    #region IDisposable

    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                cancellation.Cancel();
                watcher.Dispose();
                MainContext.Instance.PropertyChanged -= OnPropertyChanged;
            }

            // Free unmanaged resources owned by DerivedClass
            disposed = true;
        }

        base.Dispose(disposing);
    }
    bool disposed;

    #endregion
}

record DirItemPos(Item Item, int Pos);