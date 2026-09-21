using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Channels;
using CsTools.Extensions;
using Gtk4DotNet;
using UI;
using Extensions;

class DirectoryController : Controller
{
    public static DirectoryController Get(string id, Controller? current, FolderView view, FolderContext context)
        => current is DirectoryController directoryController
            ? directoryController
            : new DirectoryController(id, current, view, context);

    public override async Task ChangePathAsync(string path, bool fromHistory)
    {
        var folderToSelect = path.EndsWith("..") ? Context.CurrentPath.SubstringAfterLast('/') : null;
        cancellation.Cancel();
        cancellation = new();
        var items = await Get(path, fromHistory);
        var enableEvents = watcher.Path == "";
        watcher.Path = Context.CurrentPath;
        if (enableEvents)
            watcher.EnableRaisingEvents = true;
        metaFileData?.Dispose();
        metaFileData = new();
        view.OnItemsChange(true);
        store.ReplaceAll(items);
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

        StartRefreshing();
    }

    public override async Task<string?> GetActivationPath(int pos)
    {
        if (extendedRename != null && await extendedRename.Rename() == true)
            return null;
        var item = model.GetItem<Item>(pos);
        if (item is FileItem fi)
        {
            using var proc = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{Context.CurrentPath.AppendPath(fi.Name)}\"",
                },
            };
            proc.Start();
            return null;
        }
        else
            return (string?)GetItemPath(pos);
    } 

    public override string GetItemPath(int pos)
        => Context.CurrentPath.AppendPath(model.GetItem<Item>(pos)?.Name ?? "");

    public override ExifData? GetExifData(int pos)
        => model.GetItem<Item>(pos) is FileItem fileItem ? fileItem.ExifData : null;

    public override void SelectAll()
    {
        base.SelectAll();
        extendedRename?.SelectionChanged();
    }

    public override void SelectNone()
    {
        base.SelectNone();
        extendedRename?.SelectionChanged();
    }

    public override void SelectAllAbove()
    {
        base.SelectAllAbove();
        extendedRename?.SelectionChanged();
    }

    public override void SelectAllBeneath()
    {
        base.SelectAllBeneath();
        extendedRename?.SelectionChanged();
    }

    public override void ToggleSelection()
    {
        base.ToggleSelection();
        extendedRename?.SelectionChanged();
    }

    public override void ToggleSelection(int pos)
    {
        base.ToggleSelection(pos);        
        extendedRename?.SelectionChanged();
    }

    public DirectoryController(string id, Controller? previous, FolderView view, FolderContext context)
        : base(id, view, context)
    {
        directorySorter = new(ExtendedRenameChanged);
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
                else
                {
                    dateexif?.UnsetDateTimeBinding();
                    dateexif?.UnsetExifBinding();
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
            .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.End).MarginEnd(5).SetEllipsize(EllipsizeMode.End)))
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
                label.Text = "";
                label.DataContext = null;
            });

        view.ColumnView.SetModel(null);
        view.ColumnView.ClearColumns();
        view.ColumnView.SetModel(model);

        previous?.Dispose();

        using var nameSorter = CustomSorter.New<Item>(directorySorter.NameOrExtensionOrder);
        using var nameMultiSorter = MultiSorter.New().Append(CustomSorter.New<Item>(directorySorter.SortDirectoriesFirst)).Append(nameSorter);
        var firstCol = ColumnViewColumn
            .New(DirectorySorter.NAME, namefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(nameMultiSorter));
        view.ColumnView.AppendColumn(firstCol);
        view.ColumnView.SortByColumn(firstCol);

        using var dateSorter = CustomSorter.New<Item>((item1, item2) 
            => (item1 is FileItem fi ? fi.ExifData?.DateTime ?? fi.DateTime : item1 is FileSystemItem fsi1 ? fsi1.DateTime : DateTime.MinValue)
                .CompareTo(item2 is FileItem fi2 ? fi2.ExifData?.DateTime ?? fi2.DateTime : item2 is FileSystemItem fsi2 ? fsi2.DateTime : DateTime.MinValue));
        using var dateMultiSorter = MultiSorter.New().Append(CustomSorter.New<Item>(directorySorter.SortDirectoriesFirst)).Append(dateSorter);
        var dateCol = ColumnViewColumn
            .New("Datum", datefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(dateMultiSorter));
        view.ColumnView.AppendColumn(dateCol);

        using var sizeSorter = CustomSorter.New<Item>(DirectorySorter.SortSize);
        using var sizeMultiSorter = MultiSorter.New().Append(CustomSorter.New<Item>(directorySorter.SortDirectoriesFirst)).Append(sizeSorter);
        var sizeCol = ColumnViewColumn
            .New("Größe", sizefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(sizeMultiSorter));
        view.ColumnView.AppendColumn(sizeCol);

        using var viewsorter = view.ColumnView.GetSorter();
        viewsorter.OnChanged -= directorySorter.SortOrderChanged;
        viewsorter.OnChanged += directorySorter.SortOrderChanged;
        sortModel.SetSorter(viewsorter);
    }

    public static string GetExifDate(ExifData? exif, string altValue)
        => exif != null && exif.DateTime != DateTime.MinValue
            ? exif.DateTime.ToString("g")
            : altValue;

    public override int GetDirectoryCount() => model.GetItems<Item>().OfType<DirectoryItem>().Count();
    public override int GetFileCount() => model.GetItems<Item>().OfType<FileItem>().Count();

    public override async Task Delete(int focusedPos)
    {
        var selected = GetSelectedItems(focusedPos).OfType<SelectableItem>().ToArray();
        if (selected.Length == 0)
            return;
        var dirs = selected.Count(n => n is DirectoryItem);
        var files = selected.Count(n => n is FileItem);
        var text = dirs == 0 && files == 1
            ? "die Datei"
            : dirs == 1 && files == 0
            ? "das Verzeichnis"
            : dirs == 0 && files > 1
            ? "die Dateien"
            : dirs > 1 && files == 0
            ? "die Verzeichnisse"
            : "die Einträge";
        var dialog = AdwAlertDialog.New("Löschen", $"Möchtest du {text} löschen?");
        dialog.SetResponses([
                new("ok", "_OK", Default: true, Appearance: AdwResponseAppearance.Suggested),
                new("cancel", "_Abbrechen", Cancel: true)
            ]);
        var res = await dialog.PresentAsync(MainWindow.Instance);
        if (res == "cancel")
            return;

        foreach (var item in selected)
        {
            using var file = GFile.New(Context.CurrentPath.AppendPath(item.Name));
            await file.TrashAsync();
        }
    }

    public override async Task Copy(int focusedPos, bool move)
    {
        var selected = GetSelectedItems(focusedPos).OfType<SelectableItem>().ToArray();
        if (selected.Length == 0)
            return;
        var title = move ? "Verschieben" : "Kopieren";
        var dirs = selected.Count(n => n is DirectoryItem);
        var files = selected.Count(n => n is FileItem);
        var text = dirs == 0 && files == 1
            ? "die Datei"
            : dirs == 1 && files == 0
            ? "das Verzeichnis"
            : dirs == 0 && files > 1
            ? "die Dateien"
            : dirs > 1 && files == 0
            ? "die Verzeichnisse"
            : "die Einträge";

        var sourcePath = Context.CurrentPath;
        var targetPath = MainWindow.GetInactiveView().Context.CurrentPath;
        var fromLeft = MainWindow.IsLeftActive();

        var copyItems = CopyItems.Get(Context, selected).ToArray();
        var conflicts = CopyItems.GetConflictItems(copyItems, targetPath).ToArray();
        if (conflicts.Length == 0)
        {
            var dialog = AdwAlertDialog.New(title, $"Möchtest du {text} {(move ? "verschieben" : "kopieren")}?");
            dialog.SetResponses([
                    new("ok", "_OK", Default: true, Appearance: AdwResponseAppearance.Suggested),
                    new("cancel", "_Abbrechen", Cancel: true)
                ]);
            dialog.SetExtraChild(new ShowDirection().SideEffect(d => d.RightToLeft = !fromLeft));
            var res = await dialog.PresentAsync(MainWindow.Instance);
            if (res == "cancel")
                return;
        }
        else
        {
            var res = await Conflicts.PresentAsync(conflicts);
            if (!res.HasValue)
                return;
            if (res == false)
            {
                var excludes = conflicts.Select(n => new CopyItem(n.Name, n.SubPath, n.Size, n.DateTime));
                copyItems = [.. copyItems.Except(excludes)];
            }
        }
        var currentCount = 1;
        var totalMaxBytes = copyItems.Sum(n => n.Size);
        var totalCurrentBytes = 0L;
        var start = DateTime.UtcNow;
        var cts = new CancellationTokenSource();

        foreach (var item in copyItems)
        {
            if (cts.Token.IsCancellationRequested)
                break;
            void OnProgress(long curr, long max)
                => ProgressContext.Instance.CopyProgress = new(title, item.Name, copyItems.Length, currentCount,
                        totalMaxBytes, totalCurrentBytes, item.Size, curr, DateTime.UtcNow - start, cts);

            using var file = GFile.New(sourcePath.AppendPath(item.SubPath).AppendPath(item.Name));
            var target = targetPath.AppendPath(item.SubPath).AppendPath(item.Name);
            if (move)
                await file.MoveAsync(target, FileCopyFlags.Overwrite, true, OnProgress);
            else
                await file.CopyAsync(target, FileCopyFlags.Overwrite, true, OnProgress);
            totalCurrentBytes += item.Size;
            currentCount++;
        }

        MainWindow.GetInactiveView().Refresh();
    }

    public override async Task CreateFolder(int focusedPos)
    {
        var item = model.GetItem<Item>(focusedPos) is SelectableItem si ? si : null;
        var res = await UI.CreateFolder.PresentAsync(item?.Name, MainWindow.Instance);
        if (res == null)
            return;
        Directory.CreateDirectory(Context.CurrentPath.AppendPath(res));
    }
    
    public override async Task Rename(int focusedPos)
    {
        if (GetSelectedItems().OfType<SelectableItem>().Any())
            return;
        var item = model.GetItem<Item>(focusedPos);

        var body = item is FileItem ? "die Datei" : item is DirectoryItem ? "das Verzeichnis" : null;
        if (item == null || body == null)
            return;
        
        var res = await UI.Rename.PresentAsync($"Möchtest du {body} umbenennen?", item.Name, MainWindow.Instance);
        if (res == null)
            return;
        Directory.Move(Context.CurrentPath.AppendPath(item.Name), Context.CurrentPath.AppendPath(res));
        MainWindow.FocusActiveView();
    }

    public override async Task RenameAsCopy(int focusedPos)
    {
        if (GetSelectedItems().OfType<SelectableItem>().Any())
            return;
        var item = model.GetItem<Item>(focusedPos);
        var body = item is FileItem ? "der Datei" : item is DirectoryItem ? "des Verzeichnisses" : null;
        if (item == null || body == null)
            return;

        var res = await UI.Rename.PresentAsync($"Möchtest eine Kopie {body} erstellen?", item.Name, MainWindow.Instance, "Kopie erstellen");
        if (res == null)
            return;
        if (File.Exists(Context.CurrentPath.AppendPath(item.Name)))
            File.Copy(Context.CurrentPath.AppendPath(item.Name), Context.CurrentPath.AppendPath(res));
        MainWindow.FocusActiveView();
    }
    
    public override async void ExtendedRename()
    {
        var res = await UI.ExtendedRename.PresentAsync();
        if (res != null)
        {
            if (extendedRename == null)
                extendedRename = new(this);
            extendedRename?.SelectionChanged();
        }
        else
        {
            extendedRename?.Dispose();
            extendedRename = null;
        }
    }

    public override bool CheckRestriction(string searchKey)
        => model
            .GetItems<Item>()
            .Any(n => n.Name.StartsWith(searchKey, StringComparison.CurrentCultureIgnoreCase));

    public override void OpenWith(int pos)
    {
        var item = model.GetItem<Item>(pos);
        if (item is FileItem fi)
        {
            AdwDialog.PresentFromTemplate("appchooser", "dialog", MainWindow.Instance, (builder, name)
                        => new AppChooser(builder, name, Context.CurrentPath, fi.Name));
        }
    }

    public void InsertColumn(int pos, ColumnViewColumn col) => view.ColumnView.InsertColumn(pos, col);
    public void RemoveColumn(int pos) => view.ColumnView.RemoveColumn(pos);

    public IEnumerable<Item> GetItems() => model.GetItems<Item>();

    protected override CustomFilter? CreateFilter() => CustomFilter.New<Item>(Filter);

    async void StartRefreshing()
    {
        try
        {
            while (true)
            {
                await refreshes.Reader.ReadAsync(cancellation.Token);
                Refresh();
                await Task.Delay(500, cancellation.Token);
            }
        }
        catch (OperationCanceledException) { }
    }

    void StartExifResolving(IEnumerable<FileItem> items)
    {
        var taskId = BackgroundTasks.GetId();
        var token = BackgroundTasks.GetCancellationToken(cancellation.Token);
        BackgroundTasks.Add(taskId, Task.Run(() =>
        {
            Context.BackgroundAction = BackgroundAction.ExifDatas;
            try
            {
                foreach (var item in items
                        .Where(item => !token.IsCancellationRequested &&
                            (item.Name.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase)
                                || item.Name.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase)
                                || item.Name.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))))
                    item.ExifData = ExifReader.GetExifData(Context.CurrentPath.AppendPath(item.Name));
                refreshes.Writer.TryWrite(true);
            }
            finally
            {
                BackgroundTasks.Remove(taskId);
                Context.BackgroundAction = BackgroundAction.None;
            }
        }));
    }

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

    bool Filter(Item? item)
        => (MainContext.Instance.ShowHiddenItems || item is not FileSystemItem fsi || !fsi.IsHidden)
            && (view.Context.Restriction == null
                || item?.Name.StartsWith(view.Context.Restriction, StringComparison.CurrentCultureIgnoreCase) == true);

    void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainContext.ShowHiddenItems))
        {
            FilterChanged(MainContext.Instance.ShowHiddenItems ? FilterChange.LessStrict : FilterChange.MoreStrict);
            view.CountsChanged(GetDirectoryCount(), GetFileCount());
        }
    }

    void WatchCreated(object _, FileSystemEventArgs e)
    {
        Gtk.InvokeAsync(() =>
        {
            try
            {
                if (File.Exists(e.FullPath))
                {
                    var fi = new FileInfo(e.FullPath);
                    var item = FileItem.New(fi);
                    if (store.Append(item))
                        metaFileData?.QueueMetadata(item, fi.FullName);
                }
                else if (Directory.Exists(e.FullPath))
                    store.Append(DirectoryItem.New(new DirectoryInfo(e.FullPath)));
                else
                {
                    Console.WriteLine($"Not created: {e.FullPath}");
                    return;
                }
                view.CountsChanged(GetDirectoryCount(), GetFileCount());
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Exception in Watcher created: {e}");
            }
        });
    }

    void WatchDeleted(object _, FileSystemEventArgs e)
    {
        Gtk.InvokeAsync(() =>
        {
            Console.WriteLine("Deleted");
            store.Delete(e.Name ?? "");
            view.CountsChanged(GetDirectoryCount(), GetFileCount());
        });
    }
        
    void WatchChanged(object _, FileSystemEventArgs e)
    {
        Gtk.InvokeAsync(async () =>
        {
            try
            {
                Console.WriteLine("Changed");
                var fileInfo = new FileInfo(Context.CurrentPath.AppendPath(e.Name));
                var itemBase = store.GetValue(e.Name ?? "");
                if (itemBase is FileItem item)
                {
                    item?.DateTime = fileInfo.LastWriteTime;
                    item?.Size = fileInfo.Length;
                    if (item != null)
                        metaFileData?.QueueMetadata(item, fileInfo.FullName);
                }
                refreshes.Writer.TryWrite(true);
            }
            catch (Exception)
            {
                int focused = model.Selected;
                SetSelection(focused);
            }   
        });
    }

    void WatchRenamed(object _, RenamedEventArgs e)
    {
        Gtk.InvokeAsync(() =>
        {
            try
            {
                Console.WriteLine($"-----------------------------------------------Renamed: {e.OldName} {e.Name}");
                int focused = model.Selected;
                var pos = model.GetItems<Item>().TakeWhile(n => n.Name != e.OldName).Count();
                bool focusNew = pos == focused;

                var item = model.GetItems<Item>().FirstOrDefault(n => n.Name == e.OldName);
                if (e.OldName != null)
                    store.Delete(e.OldName);

                if (item != null && e.Name != null)
                {
                    item?.Name = e.Name;
                    var newItem = item is FileItem fi
                        ? new FileItem(fi) as Item
                        : item is DirectoryItem di
                        ? new DirectoryItem(di)
                        : null;
                    if (newItem != null)
                        store.Append(newItem);
                }
                else
                {
                    var fileInfo = new FileInfo(Context.CurrentPath.AppendPath(e.Name));
                    var itemBase = store.GetValue(e.Name ?? "");
                    if (itemBase is FileItem fi)
                    {
                        fi?.DateTime = fileInfo.LastWriteTime;
                        fi?.Size = fileInfo.Length;
                    }
                    refreshes.Writer.TryWrite(true);
                }
                view.CountsChanged(GetDirectoryCount(), GetFileCount());

                if (focusNew)
                {
                    var newPos = model
                        .GetItems<Item>()
                        .Select((n, i) => new DirItemPos(Item: n, Pos: i))
                        .FirstOrDefault(n => n.Item.Name == e.Name)?.Pos;
                    if (newPos.HasValue)
                        SetSelection(newPos.Value);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"-----------------------------------------------Watcher renamed: {e}");
            }
        });
    }

    readonly FileSystemWatcher watcher = new();
    MetaFileData? metaFileData;
    ExtendedRename? extendedRename;
    CancellationTokenSource cancellation = new();
    void ExtendedRenameChanged()
    {
        if (extendedRename != null)
            extendedRename?.SelectionChanged();
    } 
    readonly DirectorySorter directorySorter;

    readonly Channel<bool> refreshes = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        SingleReader = true,
        SingleWriter = true
    });

    #region IDisposable

    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                cancellation.Cancel();
                metaFileData?.Dispose();
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

record CopyItem(string Name, string SubPath, long Size, DateTime DateTime);
