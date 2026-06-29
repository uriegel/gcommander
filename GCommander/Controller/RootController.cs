using System.Text.Json;
using System.Text.Json.Serialization;
using CsTools.Extensions;
using Gtk4DotNet;

using static CsTools.ProcessCmd;

class RootController : Controller
{
    public const string Name = "Root";

    public override async void ChangePath(string? path)
    {
        // var items = await Get();
        // foreach (var item in items)
        //     store.Append(item);
    }

    public static RootController? Get(Controller? current, ColumnView view)
        => current is RootController
            ? null
            : new RootController(current, view);

    public RootController(Controller? previous, ColumnView view)
        : base(NoSelection.New)
    {
        previous?.Dispose();

        var namefactory = SignalListItemFactory.New();
        namefactory.Setup(listitem =>
        {
            using var builder = Builder.FromDotNetResource("icon-name-item");
            var item = new IconNameItem(builder);
            listitem.SetManagedChild(item);
        });
        namefactory.Bind(listitem =>
        {
            var iconname = listitem.GetManagedChild<IconNameItem>();
            var item = listitem.GetItem<RootItem>();
            iconname?.Name = item?.Name ?? "";
            // if (item?.IconName != null)
            //     iconname?.SetFromIconName(item.IconName);
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
        using var viewsorter = view.GetSorter();
        sortModel.SetSorter(viewsorter);
    }

    static async Task<RootItem[]> Get()
        => [new RootItem("~", "home", 0, CsTools.Directory.GetHomeDir(), true, "user-home", null, DriveType.HOME), ..
            from drive in JsonSerializer.Deserialize<DrivesResult>(
                                        await RunAsync("lsblk", "--json --bytes -o NAME,UUID,LABEL,FSTYPE,MOUNTPOINT,SIZE,TRAN,RM,FSUSE%"), Json.Defaults
                                    )?.Blockdevices
            where drive.Fstype != "squashfs"
            from child in drive.Children ?? [drive]
            orderby child.Mountpoint == null
            select new RootItem(
                child.Name,
                child.Label,
                child.Size,
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
