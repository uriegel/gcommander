using System.Text.Json;
using System.Text.Json.Serialization;
using CsTools.Extensions;
using Gtk4DotNet;

using static CsTools.ProcessCmd;

// TODO Mount unmounted drive
// TODO VolumeMonitor

// TODO Percentage as progress?
// TODO DriveType??

class RootController : Controller
{
    public const string Name = "Root";

    public override async void ChangePath(string? path)
    {
        var items = await Get();
        foreach (var item in items)
            store.Append(item);
    }

    public static RootController? Get(Controller? current, ColumnView view, FolderViewController folderView)
        => current is RootController
            ? null
            : new RootController(current, view, folderView);

    public RootController(Controller? previous, ColumnView view, FolderViewController folderView)
        : base(NoSelection.New)
    {
        previous?.Dispose();

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
                    label.GetParent().AddCssClass("warning", item?.IsMounted == true);
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

        var sorterIsMounted = CustomSorter.New<RootItem>((item1, item2) =>
        {
            var order = item1?.IsMounted == true && item2?.IsMounted != true
                ? -1
                : item2?.IsMounted == true && item1?.IsMounted != true
                ? 1
                : 0;
            return folderView.ReverseSortOrder ? -order : order;
        });

        using var nameSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.Name ?? "").CompareTo(item2?.Name ?? ""));
        using var nameMultiSorter = MultiSorter.New().Append(sorterIsMounted).Append(nameSorter);
        var firstCol = ColumnViewColumn
            .New("Name", namefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(nameMultiSorter));
        view.AppendColumn(firstCol);
        view.SortByColumn(firstCol);

        using var descriptionSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.Description ?? "").CompareTo(item2?.Description ?? ""));
        using var descriptionMultiSorter = MultiSorter.New().Append(sorterIsMounted).Append(descriptionSorter);
        view.AppendColumn(ColumnViewColumn
            .New("Bezeichnung", descriptionfactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(descriptionMultiSorter))
        );
        using var mountPointSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.MountPoint ?? "").CompareTo(item2?.MountPoint ?? ""));
        using var mountPointMultiSorter = MultiSorter.New().Append(sorterIsMounted).Append(mountPointSorter);
        view.AppendColumn(ColumnViewColumn
            .New("MountPoint", mountPointfactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(mountPointMultiSorter))
        );
        using var useSorter = CustomSorter.New<RootItem>((item1, item2) => SortSize(item1?.Use, item2?.Use));
        using var useMultiSorter = MultiSorter.New().Append(sorterIsMounted).Append(useSorter);
        view.AppendColumn(ColumnViewColumn
            .New("%", usefactory)
            .SideEffect(cvc => cvc.SetSorter(useMultiSorter))
        );
        using var sizeSorter = CustomSorter.New<RootItem>((item1, item2) => SortSize(item1?.Size, item2?.Size));
        using var sizeMultiSorter = MultiSorter.New().Append(sorterIsMounted).Append(sizeSorter);
        view.AppendColumn(ColumnViewColumn
            .New("Größe", sizefactory)
            .SideEffect(cvc => cvc.SetSorter(sizeMultiSorter))
        );

        using var viewsorter = view.GetSorter();
        viewsorter.OnChanged -= folderView.SortOrderChanged;
        viewsorter.OnChanged += folderView.SortOrderChanged;
        sortModel.SetSorter(viewsorter);
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
