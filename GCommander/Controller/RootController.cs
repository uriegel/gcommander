using CsTools.Extensions;
using Gtk4DotNet;

class RootController : Controller
{
    public const string Name = "Root";

    public override void ChangePath(string? path)
    {
        store.Append(new RootItem("sda1"));
        store.Append(new RootItem("sda2"));
        store.Append(new RootItem("sda3"));
        store.Append(new RootItem("sdb1"));
        store.Append(new RootItem("sdb2"));
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
}