using System.ComponentModel;
using CsTools.Extensions;
using CsTools.Functional;
using CsTools.HttpRequest;
using Extensions;
using Gtk4DotNet;

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

    CancellationTokenSource cancellation = new();
    readonly DirectorySorter directorySorter = new();

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