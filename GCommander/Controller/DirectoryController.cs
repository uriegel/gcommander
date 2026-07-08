using System.ComponentModel;
using CsTools.Extensions;
using Gtk4DotNet;

class DirectoryController : Controller
{
    public static DirectoryController Get(string id, Controller? current, ColumnView view, FolderViewController folderView, FolderContext context)
        => current is DirectoryController directoryController
            ? directoryController
            : new DirectoryController(id, current, view, folderView, context);

    public override async Task ChangePathAsync(string path)
    {
        var folderToSelect = path.EndsWith("..") ? context.CurrentPath.SubstringAfterLast('/') : null;
        cancellation.Cancel();
        cancellation = new();
        var items = await Get(path);
        var enableEvents = watcher.Path == "";
        watcher.Path = context.CurrentPath;
        if (enableEvents)
            watcher.EnableRaisingEvents = true;
        folderView.OnItemsChange(true);
        store.Splice(0, store.ItemsCount(), items);
        StartExifResolving(items);
        folderView.OnItemsChange(false);
        int pos = folderToSelect != null
            ? model
                .GetItems<DirectoryItem>()
                .Select((n, i) => new DirItemPos(Item: n, Pos: i))
                .FirstOrDefault(n => n.Item.Name == folderToSelect)?.Pos
                ?? 0
            : 0;
        SetSelection(pos);

        MainContext.Instance.PropertyChanged -= OnPropertyChanged;
        MainContext.Instance.PropertyChanged += OnPropertyChanged;
    }

    public override Task<string> GetChangePath(int pos) => GetItemPath(pos).ToAsync();

    public override string GetItemPath(int pos)
        => context.CurrentPath.AppendPath(model.GetItem<DirectoryItem>(pos)?.Name ?? "");

    public override void Refresh()
    {
        throw new NotImplementedException();
    }

    public override void SelectAll()
    {
        foreach (var item in store.GetItems<DirectoryItem>())
        {
            if (item.Type != DirectoryItemType.Parent)
                item.IsSelected = true;
        }
    }

    public override void SelectNone()
    {
        foreach (var item in store.GetItems<DirectoryItem>())
        {
            if (item.Type != DirectoryItemType.Parent)
                item.IsSelected = false;
        }
    }

    public override void ToggleSelection()
    {
        var pos = model.Selected;
        var item = model.GetItem<DirectoryItem>(pos);
        item?.IsSelected = item.IsSelected !=true;
        SetSelection(Math.Min(pos + 1, model.GetItems() -1));
        
    }

    public DirectoryController(string id, Controller? previous, ColumnView view, FolderViewController folderView, FolderContext context)
        : base(id, CustomFilter.New<DirectoryItem>(FilterHidden), view, folderView, context)
    {
        watcher.Created += WatchCreated;
        watcher.Deleted += WatchDeleted;
        watcher.Changed += WatchChanged;
        watcher.Renamed += WatchRenamed;
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
                var item = listitem.GetItem<DirectoryItem>();
                iconname?.Name = item?.Name ?? "";
                if (item?.Type == DirectoryItemType.Parent)
                    iconname?.SetFromIconName("go-up");
                else if (item?.Type == DirectoryItemType.Directory)
                    iconname?.SetFromIconName("folder-open");
                else
                    iconname?.SetIcon(item?.Name ?? "");
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
                var dateexif = listitem.GetManagedChild<DateExif>();
                var item = listitem.GetItem<DirectoryItem>();
                dateexif?.DataContext = item;
                dateexif?.SetDateTimeBinding();
                dateexif?.SetExifBinding();
                var row = dateexif?.GetParent()?.GetParent();
                row?.DataContext = item;
                row?.AddCssClass("hiddenItem", item?.IsHidden == true);
                row?.SetBindingToCss("selection", nameof(item.IsSelected));
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
                var item = listitem.GetItem<DirectoryItem>();
                label.DataContext = item;
                label.SetBinding("label", nameof(item.Size), BindingFlags.Default, s => ((long?)s).FormatSize());
            })
            .Unbind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                label.UnsetBinding("label");
                label.DataContext = null;
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
    }

    public static string GetExifDate(ExifData? exif, string altValue)
        => exif != null && exif.DateTime != DateTime.MinValue
            ? exif.DateTime.ToString("g")
            : altValue;

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
        context.CurrentPath = dirInfo.FullName;
        Application.Settings.SetString($"path-{Id}", dirInfo.FullName);
        return [
            new DirectoryItem("..", DirectoryItemType.Parent, false),
            .. dirs,
            .. files
        ];
    }

    public override int GetDirectoryCount() => model.GetItems<DirectoryItem>().Count(n => n.Type == DirectoryItemType.Directory);
    public override int GetFileCount() => model.GetItems<DirectoryItem>().Count(n => n.Type == DirectoryItemType.File);

    void StartExifResolving(DirectoryItem[] items)
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

    void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainContext.ShowHiddenItems))
        {
            FilterChanged(MainContext.Instance.ShowHiddenItems ? FilterChange.LessStrict : FilterChange.MoreStrict);
            folderView.CountsChanged(GetDirectoryCount(), GetFileCount());
        }
    }

    void WatchCreated(object _, FileSystemEventArgs e)
    {
        try
        {
            store.Splice(0, 0, [DirectoryItem.CreateFileItem(new FileInfo(e.FullPath))]);
        }
        catch { }
    }

    void WatchDeleted(object _, FileSystemEventArgs e)
    {
        var pos = store.GetItems<DirectoryItem>().TakeWhile(n => n.Name != e.Name).Count();
        store.Splice<DirectoryItem>(pos, 1, []);
    }
        
    void WatchChanged(object _, FileSystemEventArgs e)
    {
        var fileInfo = new FileInfo(context.CurrentPath.AppendPath(e.Name)); 
        var item = model.GetItems<DirectoryItem>().FirstOrDefault(n => n.Name == e.Name);
        item?.DateTime = fileInfo.LastWriteTime;
        item?.Size = fileInfo.Length;
    }

    void WatchRenamed(object _, RenamedEventArgs e)
    {
        Console.WriteLine($"Renamed: {e.OldName} {e.Name}");
        int focused = model.Selected;
        var pos = model.GetItems<DirectoryItem>().TakeWhile(n => n.Name != e.OldName).Count();
        bool focusNew = pos == focused;

        var posToRemove = store.GetItems<DirectoryItem>().TakeWhile(n => n.Name != e.OldName).Count();
        if (pos != store.GetItems())
            store.Remove(posToRemove);

        var fileInfo = new FileInfo(context.CurrentPath.AppendPath(e.Name)); 
        if (!File.Exists(context.CurrentPath.AppendPath(e.Name)))
            store.Splice(0, 0, [ DirectoryItem.CreateFileItem(fileInfo) ]);
        else
        {
            var item = model.GetItems<DirectoryItem>().FirstOrDefault(n => n.Name == e.Name);
            item?.DateTime = fileInfo.LastWriteTime;
            item?.Size = fileInfo.Length;
        }

        if (focusNew)
        {
            var newPos = model
                .GetItems<DirectoryItem>()
                .Select((n, i) => new DirItemPos(Item: n, Pos: i))
                .FirstOrDefault(n => n.Item.Name == e.Name)?.Pos;
            if (newPos.HasValue)
                SetSelection(newPos.Value);
        }
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

record DirItemPos(DirectoryItem Item, int Pos);