using System.ComponentModel;
using CsTools;
using CsTools.Extensions;
using CsTools.Functional;
using CsTools.HttpRequest;
using Extensions;
using Gtk4DotNet;
using UI;

using static CsTools.HttpRequest.Core;

class RemoteController : Controller
{
    public const string Name = "remote";

    public static RemoteController Get(string id, Controller? current, FolderView view, FolderContext context)
        => current is RemoteController remoteController
            ? remoteController
            : new RemoteController(id, current, view, context);

    public override string GetItemPath(int pos)
    {
        var path = model.GetItem<Item>(pos)?.Name ?? "";
        return path != ".."
            ? Context.CurrentPath.AppendPath(path.StartsWith('/') ? path[1..] : path)
            : Context.CurrentPath.UpOne();
    }

    public override async Task<string?> GetActivationPath(int pos) => (string?)GetItemPath(pos);
    public override async Task ChangePathAsync(string path, bool fromHistory = false)
    {
        var folderToSelect = path.Length < Context.CurrentPath.Length ? Context.CurrentPath.SubstringAfterLast('/') : null;
        cancellation.Cancel();
        cancellation = new();
        var items = await Get(path, fromHistory);
        view.OnItemsChange(true);
        store.ReplaceAll(items);
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

    public override int GetDirectoryCount() => model.GetItems<Item>().OfType<DirectoryItem>().Count();
    public override int GetFileCount() => model.GetItems<Item>().OfType<FileItem>().Count();

    public override bool CheckRestriction(string searchKey)
        => model
            .GetItems<Item>()
            .Any(n => n.Name.StartsWith(searchKey, StringComparison.CurrentCultureIgnoreCase));

    public override async Task CreateFolder(int focusedPos)
    {
        var item = model.GetItem<Item>(focusedPos) is SelectableItem si ? si : null;
        var newFile = await UI.CreateFolder.PresentAsync(item?.Name, MainWindow.Instance);
        if (newFile == null)
            return;
        await Request.RunAsync(Context.CurrentPath.AppendPath(newFile).GetIpAndPath().PostCreateDirectory(), true);
        MainWindow.GetActiveView().Refresh();
    }

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
            await Request.RunAsync(Context.CurrentPath.AppendPath(item.Name).GetIpAndPath().DeleteItem(), true);
        MainWindow.GetActiveView().Refresh();
    }

    public override async Task Copy(int focusedPos, bool move)
    {
        var targetController = MainWindow.GetInactiveView().GetController();
        if (targetController is not DirectoryController)
            return;

        var selected = GetSelectedItems(focusedPos).OfType<FileItem>().ToArray();
        if (selected.Length == 0 || move)
            return;
        var title = "Kopieren";
        var text = selected.Length == 1
            ? "die Datei"
            : "die Dateien";

        var sourcePath = Context.CurrentPath;
        var targetPath = MainWindow.GetInactiveView().Context.CurrentPath;
        var fromLeft = MainWindow.IsLeftActive();

        var copyItems = CopyItems.Get(Context, selected).ToArray();
        var conflicts = CopyItems.GetConflictItems(copyItems, targetPath).ToArray();
        if (conflicts.Length == 0)
        {
            var dialog = AdwAlertDialog.New(title, $"Möchtest du {text} kopieren?");
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
            void OnProgress(long max, long curr)
                => ProgressContext.Instance.CopyProgress = new(title, item.Name, copyItems.Length, currentCount,
                        totalMaxBytes, totalCurrentBytes, item.Size, curr, DateTime.UtcNow - start, cts);

            await CopyItem(targetPath, item, OnProgress, cancellation.Token);
            totalCurrentBytes += item.Size;
            currentCount++;
        }

        MainWindow.GetInactiveView().Refresh();
    }

    public RemoteController(string id, Controller? previous, FolderView view, FolderContext context)
        : base(id, view, context)
    {
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
                var row = iconname?.GetParent()?.GetParent();
                row?.DataContext = item;
                if (item is SelectableItem si)
                    row?.SetBindingToCss("selection", nameof(si.IsSelected));
            })
            .Unbind(listitem =>
            {
                var iconname = listitem.GetManagedChild<IconNameItem>();
                var row = iconname?.GetParent()?.GetParent();
                row?.UnsetBindingToCss("selection");
                row?.DataContext = null;
            });

        var datefactory = SignalListItemFactory
            .New()
            .Setup(listitem => listitem.SetChild(Label.New().SetEllipsize(EllipsizeMode.End)))
            .Bind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                var item = listitem.GetItem<Item>();
                label.Text = item is FileItem fileItem ? fileItem.DateTime.ToString("g") : "";
            });

        var sizefactory = SignalListItemFactory
            .New()
            .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.End).MarginEnd(5).SetEllipsize(EllipsizeMode.End)))
            .Bind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                var item = listitem.GetItem<Item>();
                label.Text = item is FileItem fileItem ? fileItem.Size.FormatSize() : "";
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

    protected override CustomFilter? CreateFilter() => CustomFilter.New<Item>(Filter);

    async Task<Item[]> Get(string path, bool fromHistory)
    {
        var result = await path
            .GetIpAndPath()
            .Pipe(ipPath =>
                ipPath
                    .GetRequest()
                    .GetAsync<RemoteItem[]>($"getfiles{ipPath.Path}")
                    .AsAsyncEnumerable()
                    .OrderByDescending(n => n.IsDirectory)
                    .ThenBy(n => n.Name)
                    .Select(n => n.IsDirectory
                        ? new DirectoryItem(n.Name, n.IsHidden) as Item
                        : new FileItem(n.Name, n.IsHidden, n.Time.FromUnixTime(), n.Size)))
                    .ToArrayAwait();

        SetNewPath(path, fromHistory);
        Application.Settings.SetString($"path-{Id}", path);
        return [
            new ParentItem(),
            .. result
        ];
    }

    void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainContext.ShowHiddenItems))
        {
            FilterChanged(MainContext.Instance.ShowHiddenItems ? FilterChange.LessStrict : FilterChange.MoreStrict);
            view.CountsChanged(GetDirectoryCount(), GetFileCount());
        }
    }

    async Task CopyItem(string targetPath, CopyItem item, Action<long, long> onProgress, CancellationToken cancellation)
    {
        var newFileName = targetPath.AppendPath(item.Name);
        var tmpNewFileName = targetPath.AppendPath(item.Name + TMP_POSTFIX);
        long? lastWrite = null;
        await Task.Run(async () =>
        {
            var msg = await Request.RunAsync(Context.CurrentPath.GetIpAndPath().GetFile(item.Name), true);
            var len = msg.Content.Headers.ContentLength;
            try
            {
                using var target =
                    File
                        .Create(tmpNewFileName.EnsureFileDirectoryExists())
                        .WithProgress((t, c) => onProgress(len ?? t, c));
                await msg.Content.ReadAsStream().CopyToAsync(target, cancellation);
                lastWrite = msg.GetHeaderLongValue("x-file-date");
            }
            catch
            {
                try
                {
                    File.Delete(tmpNewFileName);
                }
                catch { }
                throw;
            }
        }, CancellationToken.None);
        if (lastWrite.HasValue)
            File.SetLastWriteTime(tmpNewFileName, lastWrite.Value.FromUnixTime());
        using var gsf = GFile.New(Context.CurrentPath.AppendPath(item.Name));
        using var gtf = GFile.New(tmpNewFileName);
        gsf.CopyAttributes(gtf, FileCopyFlags.Overwrite);
        File.Move(tmpNewFileName, newFileName, true);
    }
    
    bool Filter(Item? item)
        => (MainContext.Instance.ShowHiddenItems || item is not FileSystemItem fsi || !fsi.IsHidden)
            && (view.Context.Restriction == null
                || item?.Name.StartsWith(view.Context.Restriction, StringComparison.CurrentCultureIgnoreCase) == true);

    CancellationTokenSource cancellation = new();
    readonly DirectorySorter directorySorter = new();

    const string TMP_POSTFIX = "-tmp-commander";

    #region IDisposable

    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                cancellation.Cancel();
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

static partial class RemoteControllerExtensions
{
    public static IpAndPath GetIpAndPath(this string url)
        => new(url.StringBetween('/', '/'),
            url[7..].Contains('/')
            ? "/" + url.SubstringAfter('/').SubstringAfter('/')
            : "");

    public static JsonRequest GetRequest(this IpAndPath ipAndPath)
        => new($"http://{ipAndPath.Ip}:8080");

    public static Settings GetFile(this IpAndPath ipAndPath, string name)
        => DefaultSettings with
        {
            Method = HttpMethod.Get,
            BaseUrl = $"http://{ipAndPath.Ip}:8080",
            Url = $"/downloadfile/{ipAndPath.Path.AppendPath(name)}",
        };

    public static Settings PutFile(this Stream streamToPost, IpAndPath ipAndPath, string name, DateTime lastWrite)
        => DefaultSettings with
        {
            Method = HttpMethod.Put,
            BaseUrl = $"http://{ipAndPath.Ip}:8080",
            Url = $"/putfile/{ipAndPath.Path.AppendPath(name)}",
            Timeout = 100_000_000,
            AddContent = () => new StreamContent(streamToPost, 15000)
                                    .SideEffect(n => n.Headers
                                                        .TryAddWithoutValidation(
                                                            "x-file-date",
                                                            new DateTimeOffset(lastWrite).ToUnixTimeMilliseconds().ToString()))
        };        

    public static Settings PostCreateDirectory(this IpAndPath ipAndPath) 
        => DefaultSettings with
        {
            Method = HttpMethod.Post,
            BaseUrl = $"http://{ipAndPath.Ip}:8080",
            Url = $"/createdirectory/{ipAndPath.Path}",
        };

    public static Settings DeleteItem(this IpAndPath ipAndPath) 
        => DefaultSettings with
        {
            Method = HttpMethod.Delete,
            BaseUrl = $"http://{ipAndPath.Ip}:8080",
            Url = $"/deletefile/{ipAndPath.Path}",
        };    

    public static string UpOne(this string path)
        => path[7..].Contains('/')
            ? path.SubstringUntilLast('/')
            : RemotesController.Name;
}

record RemoteItem(
    string Name,
    long Size,
    bool IsDirectory,
    bool IsHidden,
    long Time
);

record IpAndPath(string Ip, string Path);    