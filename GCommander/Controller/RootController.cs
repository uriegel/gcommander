using System.Text.Json;
using System.Text.Json.Serialization;
using CsTools.Extensions;
using Gtk4DotNet;

using static CsTools.ProcessCmd;


// TODO Sorting by size broken
// TODO GtkMultiSorter
// TODO Sorting name? attach name sort index to sort, perhaps group sort? is mounted, is not mounted
// TODO Remotes and Favorites
// TODO with warning css when too large
// TODO Percentage as progress?
// TODO Mount unmounted drive
// TODO VolumeMontior

// TODO GetParent() generic and returns null if no parent

class RootController : Controller
{
    public const string Name = "Root";

    public override async void ChangePath(string? path)
    {
        var items = await Get();
        foreach (var item in items)
            store.Append(item);
    }

    public static RootController? Get(Controller? current, ColumnView view)
        => current is RootController
            ? null
            : new RootController(current, view);

    public RootController(Controller? previous, ColumnView view)
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
                iconname?.Name = item?.Name ?? "";
                if (item?.IconName != null)
                    iconname?.SetFromIconName(item.IconName);
                iconname?.GetParent()?.GetParent().AddCssClass("hiddenItem", item?.IsMounted != true);
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
                label.Text = item?.Use ?? "";
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
        view.AppendColumn(ColumnViewColumn
            .New("Name", namefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(nameSorter))
        );
        using var descriptionSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.Description ?? "").CompareTo(item2?.Description ?? ""));
        view.AppendColumn(ColumnViewColumn
            .New("Bezeichnung", descriptionfactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(descriptionSorter))
        );
        using var mountPointSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.MountPoint ?? "").CompareTo(item2?.MountPoint ?? ""));
        view.AppendColumn(ColumnViewColumn
            .New("MountPoint", mountPointfactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(mountPointSorter))
        );
        using var useSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.Use ?? "").CompareTo(item2?.Use ?? ""));
        view.AppendColumn(ColumnViewColumn
            .New("%", usefactory)
            .SideEffect(cvc => cvc.SetSorter(useSorter))
        );
        using var sizeSorter = CustomSorter.New<RootItem>((item1, item2) => SortSize(item1?.Size, item2?.Size));
        view.AppendColumn(ColumnViewColumn
            .New("Größe", sizefactory)
            .SideEffect(cvc => cvc.SetSorter(sizeSorter))
        );

        using var viewsorter = view.GetSorter();
        sortModel.SetSorter(viewsorter);
    }

    static async Task<RootItem[]> Get()
        => [new RootItem("~", "home", null, CsTools.Directory.GetHomeDir(), true, "user-home", null, DriveType.HOME), ..
            from drive in JsonSerializer.Deserialize<DrivesResult>(
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
                child.Fsuse,
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
