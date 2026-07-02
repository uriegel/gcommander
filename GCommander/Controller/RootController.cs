using System.Text.Json;
using System.Text.Json.Serialization;
using CsTools.Extensions;
using Gtk4DotNet;

using static CsTools.ProcessCmd;

// TODO => columnView.SideEffect(cv => Gtk.SignalConnect<ActivateDelegate>(cv, "activate", (_, pos, __) => onActivate(pos)));

// TODO OnActivate
// TODO ChangePath to DirectoryController
// TODO Mount unmounted drive
// TODO Display Error
// TODO Change Controllers ...
// TODO Paned with two cmmanderViews

// TODO public static void RemoveDrive(string mountPoint)

// TODO Percentage as progress?
// TODO DriveType??

class RootController : Controller
{
    public const string Name = "Root";

    public override async void ChangePath(string? path)
    {
        var items = await Get();
        store.Splice(0, store.ItemsCount(), items);
    }

    public static RootController Get(Controller? current, ColumnView view, FolderViewController folderView)
        => current is RootController rootController
            ? rootController
            : new RootController(current, view, folderView);

    public override string GetItemPath(int pos)
    {
        latestName = model.GetItem<RootItem>(pos)?.Name;
        return latestName?.StartsWith("zzz") == true
            ? latestName[3..]
            : latestName
            ?? "";
    }

    public override string GetChangePath(int pos)
        => model.GetItem<RootItem>(pos)?.MountPoint ?? "";
    
    public override async void Refresh()
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
            folderView.CheckCurrentChanged(pos);
            view.ScrollTo(pos, ListScrollFlags.ScrollFocus);
        }
        finally
        {
            locker.Release();
        }
    }

    public override void Activate(int position)
    {
        var item = model.GetItem<RootItem>(position);                

    }

    // public static async Task<string> Mount(string device)
    // {
    //     try
    //     {
    //         var output = await RunAsync("udisksctl", $"mount -b /dev/{device}");
    //         return output.SubstringAfter(" at ").Trim();
    //     }
    //     catch (Exception)
    //     {
    //         // if (e.Message.Contains("already mounted"))
    //         //     throw new AlreadyMountedException();
    //         // else
    //         //     throw new MountException(e.Message);
    //     }
    // }

    public RootController(Controller? previous, ColumnView view, FolderViewController folderView)
        : base(NoSelection.New)
    {
        previous?.Dispose();

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

        view.SetModel(null);
        view.ClearColumns();
        view.SetModel(model);

        using var nameSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.Name ?? "").CompareTo(item2?.Name ?? ""));
        using var nameMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(nameSorter);
        var firstCol = ColumnViewColumn
            .New("Name", namefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(nameMultiSorter));
        view.AppendColumn(firstCol);
        view.SortByColumn(firstCol);

        using var descriptionSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.Description ?? "").CompareTo(item2?.Description ?? ""));
        using var descriptionMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(descriptionSorter);
        view.AppendColumn(ColumnViewColumn
            .New("Bezeichnung", descriptionfactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(descriptionMultiSorter))
        );
        using var mountPointSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.MountPoint ?? "").CompareTo(item2?.MountPoint ?? ""));
        using var mountPointMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(mountPointSorter);
        view.AppendColumn(ColumnViewColumn
            .New("MountPoint", mountPointfactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(mountPointMultiSorter))
        );
        using var useSorter = CustomSorter.New<RootItem>((item1, item2) => SortSize(item1?.Use, item2?.Use));
        using var useMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(useSorter);
        view.AppendColumn(ColumnViewColumn
            .New("%", usefactory)
            .SideEffect(cvc => cvc.SetSorter(useMultiSorter))
        );
        using var sizeSorter = CustomSorter.New<RootItem>((item1, item2) => SortSize(item1?.Size, item2?.Size));
        using var sizeMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(sizeSorter);
        view.AppendColumn(ColumnViewColumn
            .New("Größe", sizefactory)
            .SideEffect(cvc => cvc.SetSorter(sizeMultiSorter))
        );

        using var viewsorter = view.GetSorter();
        viewsorter.OnChanged -= folderView.SortOrderChanged;
        viewsorter.OnChanged += folderView.SortOrderChanged;
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

    void StartMonitoring()
    {
        volumeMonitor = VolumeMonitor.Get();
        volumeMonitor.OnDriveConnected(Refresh);
        volumeMonitor.OnDriveDisconnected(Refresh);
        volumeMonitor.OnMountAdded(Refresh);
        volumeMonitor.OnMountRemoved(Refresh);
        volumeMonitor.OnVolumeRemoved(Refresh);
    }

    int SortMounted(RootItem? item1, RootItem? item2)
    {
        var order = item1?.IsMounted == true && item2?.IsMounted != true
            ? -1
            : item2?.IsMounted == true && item1?.IsMounted != true
            ? 1
            : 0;
        return folderView.ReverseSortOrder ? -order : order;
    }

    readonly FolderViewController folderView;
    readonly ColumnView view;
    readonly SemaphoreSlim locker = new(1, 1);
    VolumeMonitor? volumeMonitor;
    string? latestName;

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