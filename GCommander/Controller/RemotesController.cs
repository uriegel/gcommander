using System.Text.Json;
using CsTools.Extensions;
using Gtk4DotNet;

class RemotesController : Controller
{
    public const string Name = "remotes";

    public static RemotesController Get(string id, Controller? current, FolderView view, FolderContext context)
        => current is RemotesController remotesController
            ? remotesController
            : new RemotesController(id, current, view, context);

    public RemotesController(string id, Controller? previous, FolderView view, FolderContext context) : base(id, view, context)
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
                else if (item is NewRemoteItem)
                    iconname?.SetFromIconName("add");
                else
                    iconname?.SetFromIconName("starred");
                var row = iconname?.GetParent()?.GetParent();
                row?.DataContext = item;
                if (item is SelectableItem si)
                    row?.SetBindingToCss("selection", nameof(si.IsSelected));
            });

        var ipfactory = SignalListItemFactory
            .New()
            .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.Start).SetEllipsize(EllipsizeMode.End)))
            .Bind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                var item = listitem.GetItem<Item>();
                label.Text = item is RemoteItem ri ? ri.IP : "";
            });

        view.ColumnView.SetModel(null);
        view.ColumnView.ClearColumns();
        view.ColumnView.SetModel(model);

        previous?.Dispose();

        using var nameSorter = CustomSorter.New<Item>((item1, item2) => (item1?.Name ?? "").CompareTo(item2?.Name ?? ""));
        using var nameMultiSorter = MultiSorter.New().Append(CustomSorter.New<Item>(SortFixedFirst)).Append(nameSorter);
        var firstCol = ColumnViewColumn
            .New("Name", namefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(nameMultiSorter));
        view.ColumnView.AppendColumn(firstCol);
        view.ColumnView.SortByColumn(firstCol);

        using var ipSorter = CustomSorter.New<Item>((item1, item2) => (item1 is FavoriteItem fi ? fi.Path : "").CompareTo(item2 is FavoriteItem fi2 ? fi2.Path : ""));
        using var ipMultiSorter = MultiSorter.New().Append(CustomSorter.New<Item>(SortFixedFirst)).Append(ipSorter);
        view.ColumnView.AppendColumn(ColumnViewColumn
            .New("IP-Adresse", ipfactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(ipMultiSorter))
        );

        using var viewsorter = view.ColumnView.GetSorter();
        viewsorter.OnChanged -= SortOrderChanged;
        viewsorter.OnChanged += SortOrderChanged;
        sortModel.SetSorter(viewsorter);
    }

    public override async Task ChangePathAsync(string path, bool fromHistory = false)
    {
        view.OnItemsGet(true);
        var items = await Get();
        SetNewPath(Name, fromHistory);
        view.OnItemsGet(false);
        view.OnItemsChange(true);
        store.ReplaceAll(items);
        view.OnItemsChange(false);
        view.ColumnView.ScrollTo(0, ListScrollFlags.ScrollFocus);
        view.SelectionChanged(0);
    }

    public override async Task<string?> GetActivationPath(int pos)
    {
        var res = GetItemPath(pos);
        if (res == "")
        {
            var path = MainWindow.GetInactiveView().Context.CurrentPath;

            var result = await NewFavorite.PresentAsync(path, MainWindow.Instance);
            if (result == null)
                return null;

            var favs = Application.Settings.GetString("favorites") is string favstr && favstr.Length > 0 
                 ? JsonSerializer.Deserialize<FavoriteItem[]>(favstr) ?? [] 
                 : [];
            Application.Settings.SetString("favorites", JsonSerializer.Serialize<FavoriteItem[]>([.. favs, result]));
            MainWindow.Refresh();
            MainWindow.FocusActiveView();
            return null;
        }
        return res;
    }

    public override string GetItemPath(int pos)
        => pos == 0
            ? RootController.Name
            : pos == items.Length + 1
            ? ""
            : model.GetItem<Item>(pos) is RemoteItem ri ? ri.IP : "";

     async Task<Item[]> Get()
    {
        items = Application.Settings.GetString("remotes") is string remotes && remotes.Length > 0
                ? JsonSerializer.Deserialize<RemoteItem[]>(remotes) ?? []
                : [];
        return [
            new ParentItem(),
            .. items.Select(n => new RemoteItem(n.Name, n.IP)),
            new NewRemoteItem()
        ];
    }

    int SortFixedFirst(Item? item1, Item? item2)
    {
        var order = item1 is ParentItem
            ? -1
            : item2 is ParentItem
            ? 1
            : item1 is FavoriteItem && item2 is NewFavoriteItem
            ? -1
            : item2 is FavoriteItem && item1 is NewFavoriteItem
            ? 1
            : 0;
        return reverseOrder ? -order : order;
    }

    void SortOrderChanged(bool reverse, ColumnViewColumn? col, SorterChange sc) => reverseOrder = reverse;

    bool reverseOrder;

    RemoteItem[] items = [];
}