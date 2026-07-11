using System.Text.Json;
using System.Text.Json.Serialization;
using CsTools.Extensions;
using Gtk4DotNet;

using static CsTools.ProcessCmd;

// TODO TrackView: popups
// TODO Video view with Stream
// TODO Video view aspect ratio

// TODO History
// TODO Favorites
// TODO DirectorsyWatcher with Directory changes

// TODO After Actions grabFocus to paned

// TODO public static void RemoveDrive(string mountPoint)

// TODO Percentage as progress?
// TODO DriveType??

class RootController : Controller
{
    public const string Name = "root";

    public override async Task ChangePathAsync(string? path)
    {
        view.OnItemsGet(true);
        var items = await Get();
        context.CurrentPath = "root";
        view.OnItemsGet(false);
        view.OnItemsChange(true);
        store.Splice(0, store.ItemsCount(), items);
        view.OnItemsChange(false);
        view.ColumnView.ScrollTo(0, ListScrollFlags.ScrollFocus);
        view.SelectionChanged(0);
        Application.Settings.SetString($"path-{Id}", "");
    }

    public static RootController Get(string id, Controller? current, FolderView view, FolderContext context)
        => current is RootController rootController
            ? rootController
            : new RootController(id, current, view, context);

    public override string GetItemPath(int pos)
    {
        latestName = model.GetItem<RootItem>(pos)?.Name;
        return latestName?.StartsWith("zzz") == true
            ? latestName[3..]
            : latestName
            ?? "";
    }

    public override int GetDirectoryCount() => model.GetItems<RootItem>().Count();

    public override async Task<string> GetChangePath(int pos)
    {
        var item = model.GetItem<RootItem>(pos);
        if (item == null)
            return "";
        if (item.MountPoint == "")
            return await Mount(item.Name);
        else
            return item.MountPoint;
    }

    public override void OnWidth(int w)
    {
        if (!imploded && w < 320)
        {
            using var cols = view.ColumnView.GetColumns();
            var colArray = cols.ToArray();
            colArray[3].Visible = false;
            colArray[4].Visible = false;
            imploded = true;
        }
        else if (imploded && w > 320)
        {
            using var cols = view.ColumnView.GetColumns();
            var colArray = cols.ToArray();
            colArray[3].Visible = true;
            colArray[4].Visible = true;
            imploded = false;
        }
    }

    public RootController(string id, Controller? previous, FolderView view, FolderContext context)
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
                var item = listitem.GetItem<RootItem>();
                iconname?.Name = item?.Name.RemoveZzz() ?? "";
                if (item?.IconName != null)
                    iconname?.SetFromIconName(item.IconName);
                var row = iconname?.GetParent()?.GetParent();
                row?.AddCssClass("hiddenItem", item?.IsMounted != true);
            });

        var descriptionfactory = SignalListItemFactory
            .New()
            .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.Start).SetEllipsize(EllipsizeMode.End)))
            .Bind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                var item = listitem.GetItem<RootItem>();
                label.Text = item?.Description ?? "";
            });

        var mountPointfactory = SignalListItemFactory
            .New()
            .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.Start).SetEllipsize(EllipsizeMode.End)))
            .Bind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                var item = listitem.GetItem<RootItem>();
                label.Text = item?.MountPoint ?? "";
            });

        var usefactory = SignalListItemFactory
            .New()
            .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.End).SetEllipsize(EllipsizeMode.End)))
            .Bind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                var item = listitem.GetItem<RootItem>();
                label.Text = item?.Use != null ? $"{item?.Use}%" : "";
                if (item?.Use > 90)
                    label?.GetParent()?.AddCssClass("warning", item?.IsMounted == true);
            });

        var sizefactory = SignalListItemFactory
            .New()
            .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.End).SetEllipsize(EllipsizeMode.End)))
            .Bind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                var item = listitem.GetItem<RootItem>();
                label.Text = item?.Size.FormatSize() ?? "";
            });

        view.ColumnView.SetModel(null);
        view.ColumnView.ClearColumns();
        view.ColumnView.SetModel(model);

        previous?.Dispose();

        using var nameSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.Name ?? "").CompareTo(item2?.Name ?? ""));
        using var nameMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(nameSorter);
        var firstCol = ColumnViewColumn
            .New("Name", namefactory)
            //.New("N", namefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(nameMultiSorter));
        view.ColumnView.AppendColumn(firstCol);
        view.ColumnView.SortByColumn(firstCol);

        using var descriptionSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.Description ?? "").CompareTo(item2?.Description ?? ""));
        using var descriptionMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(descriptionSorter);
        view.ColumnView.AppendColumn(ColumnViewColumn
            .New("Bez.", descriptionfactory)
            //.New("B", descriptionfactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(descriptionMultiSorter))
        );
        using var mountPointSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.MountPoint ?? "").CompareTo(item2?.MountPoint ?? ""));
        using var mountPointMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(mountPointSorter);
        view.ColumnView.AppendColumn(ColumnViewColumn
            .New("Mount", mountPointfactory)
            //.New("M", mountPointfactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(mountPointMultiSorter))
        );
        using var useSorter = CustomSorter.New<RootItem>((item1, item2) => SortSize(item1?.Use, item2?.Use));
        using var useMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(useSorter);
        view.ColumnView.AppendColumn(ColumnViewColumn
            .New("%", usefactory)
            .SideEffect(cvc => cvc.SetSorter(useMultiSorter))
        );
        using var sizeSorter = CustomSorter.New<RootItem>((item1, item2) => SortSize(item1?.Size, item2?.Size));
        using var sizeMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(sizeSorter);
        view.ColumnView.AppendColumn(ColumnViewColumn
            .New("Größe", sizefactory)
            //.New("G", sizefactory)
            .SideEffect(cvc => cvc.SetSorter(sizeMultiSorter))
        );

        using var viewsorter = view.ColumnView.GetSorter();
        viewsorter.OnChanged -= SortOrderChanged;
        viewsorter.OnChanged += SortOrderChanged;
        sortModel.SetSorter(viewsorter);

        StartMonitoring();
    }

    static async Task<RootItem[]> Get()
        => [new RootItem("~", "home", null, CsTools.Directory.GetHomeDir(), true, "user-home", null, DriveType.HOME),
            new RootItem("zzzfav", "Favoriten", null, "fav", true, "starred", null, DriveType.HOME),
            new RootItem("zzzremotes", "Zugriff auf entfernte Geräte", null, "remotes", true, "network-server", null, DriveType.HOME),
            .. from drive in JsonSerializer.Deserialize<DrivesResult>(
                                        await RunAsync("lsblk", "--json --bytes -o NAME,UUID,LABEL,FSTYPE,MOUNTPOINT,SIZE,TRAN,RM,FSUSE%"), Json.Defaults
                                    )?.Blockdevices
            where drive.Fstype != "squashfs"
            from child in drive.Children ?? [drive]
            orderby child.Mountpoint == null
            select new RootItem(
                child.Name,
                child.Label,
                child.Size != 0 ? child.Size : null,
                child.Mountpoint ?? "",
                child.Mountpoint?.Length > 0,
                (child.Tran ?? drive.Tran).GetIconName(child.Rm),
                child.Uuid,
                (child.Tran ?? drive.Tran).GetDriveType(child.Rm),
                child.Fsuse?.Length > 0 ? int.Parse(child.Fsuse[..^1]) : null,
                child.Rm) ];

    async void Refresh()
    {
        await locker.WaitAsync();
        try
        {
            var items = await Get();
            store.Splice(0, model.ItemsCount(), items);
            int pos = model.GetItems<RootItem>()
                .Select((n, i) => new ItemPos(Item: n, Pos: i))
                .FirstOrDefault(n => n.Item.Name == latestName)?.Pos
                    ?? 0;
            view.SelectionChanged(pos);
            view.ColumnView.ScrollTo(pos, ListScrollFlags.ScrollFocus);
        }
        finally
        {
            locker.Release();
        }
    }

    static async Task<string> Mount(string device)
    {
        try
        {
            var output = await RunAsync("udisksctl", $"mount -b /dev/{device}");
            return output.SubstringAfter(" at ").Trim();
        }
        catch (Exception)
        {
            throw;
            // if (e.Message.Contains("already mounted"))
            //     throw new AlreadyMountedException();
            // else
            //     throw new MountException(e.Message);
        }
    }

    void StartMonitoring()
    {
        volumeMonitor = VolumeMonitor.Get();
        volumeMonitor.OnDriveConnected(Refresh);
        volumeMonitor.OnDriveDisconnected(Refresh);
        volumeMonitor.OnMountAdded(Refresh);
        volumeMonitor.OnMountRemoved(Refresh);
        volumeMonitor.OnVolumeRemoved(Refresh);
    }

    void SortOrderChanged(bool reverse, ColumnViewColumn? _, SorterChange __)
        => reverseSortOrder = reverse;

    int SortMounted(RootItem? item1, RootItem? item2)
    {
        var order = item1?.IsMounted == true && item2?.IsMounted != true
            ? -1
            : item2?.IsMounted == true && item1?.IsMounted != true
            ? 1
            : 0;
        return reverseSortOrder ? -order : order;
    }

    readonly SemaphoreSlim locker = new(1, 1);
    VolumeMonitor? volumeMonitor;
    string? latestName;
    bool reverseSortOrder;

    bool imploded;

    #region IDisposable

    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                volumeMonitor?.Dispose();
            }

            // Free unmanaged resources owned by DerivedClass

            disposed = true;
        }

        base.Dispose(disposing);
    }
    bool disposed;

    #endregion
}

static class RootItemExtensions
{
    public static string GetIconName(this string? tran, bool removable)
        => (tran, removable) switch
        {
            ("sata", _) => "drive-harddisk-solidstate",
            ("usb", false) => "drive-harddisk-usb",
            ("usb", true) => "drive-removable-media-usb",
            _ => "drive-harddisk"
        };

    public static string GetDriveType(this string? tran, bool removable)
        => (tran, removable) switch
        {
            ("sata", _) => DriveType.SATA,
            ("usb", false) => DriveType.HARDDRIVE_USB,
            ("usb", true) => DriveType.REMOVABLE_USB,
            _ => DriveType.HARDDRIVE
        };

    public static string RemoveZzz(this string name)
        => name.StartsWith("zzz")
            ? name[3..]
            : name;
}

record DrivesResult(Device[] Blockdevices);
record Device(
    Device[]? Children,
    string Name,
    string? Uuid,
    string Fstype,
    string Label,
    string? Mountpoint,
    long Size,
    string? Tran,
    [property: JsonPropertyName("fsuse%")]
    string? Fsuse,
    bool Rm);

record ItemPos(RootItem Item, int Pos);

