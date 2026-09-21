using System.Text.Json;
using CsTools.Extensions;
using Gtk4DotNet;

class RemotesController : Controller
{
    public const string Name = "ext";

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
                else if (item is RemoteDevice ri && ri.IsAndroid)
                    iconname?.SetFromIconName("phone");
                else 
                    iconname?.SetFromIconName("network-server");
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
                label.Text = item is RemoteDevice ri ? ri.IP : "";
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

        using var ipSorter = CustomSorter.New<Item>((item1, item2) => (item1 is RemoteDevice ri ? ri.IP : "").CompareTo(item2 is RemoteDevice ri2 ? ri2.IP : ""));
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
            var result = await NewRemote.PresentAsync();
            if (result == null)
                return null;

            var remotes = Application.Settings.GetString("remotes") is string remote && remote.Length > 0 
                 ? JsonSerializer.Deserialize<RemoteDevice[]>(remote) ?? [] 
                 : [];
            Application.Settings.SetString("remotes", JsonSerializer.Serialize<RemoteDevice[]>([.. remotes, result]));
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
            : model.GetItem<Item>(pos) is RemoteDevice ri ? $"remote/{ri.IP}" : "";

    public override int GetDirectoryCount() => model.GetItems<Item>().OfType<RemoteDevice>().Count();
    
    async Task<Item[]> Get()
    {
        items = Application.Settings.GetString("remotes") is string remotes && remotes.Length > 0
                ? JsonSerializer.Deserialize<RemoteDevice[]>(remotes) ?? []
                : [];
        return [
            new ParentItem(),
            .. items.Select(n => new RemoteDevice(n.Name, n.IP, n.IsAndroid)),
            new NewRemoteItem()
        ];
    }

    public override async Task Delete(int focusedPos)
    {
        var selected = GetSelectedItems(focusedPos).OfType<RemoteDevice>().ToArray();
        if (selected.Length == 0)
            return;
        var dialog = AdwAlertDialog.New("Gerät löschen", $"Möchtest du {(selected.Length > 1 ? "die Geräte" : "das Gerät")} löschen?");
        dialog.SetResponses([
                new("ok", "_OK", Default: true, Appearance: AdwResponseAppearance.Suggested),
                new("cancel", "_Abbrechen", Cancel: true)
            ]);
        var res = await dialog.PresentAsync(MainWindow.Instance);
        if (res == "cancel")
            return;

        var favs = Application.Settings.GetString("remotes") is string favstr && favstr.Length > 0
                ? JsonSerializer.Deserialize<RemoteDevice[]>(favstr) ?? []
                : [];
        var selectedNames = selected.Select(n => n.Name);
        Application.Settings.SetString("remotes", JsonSerializer.Serialize<RemoteDevice[]>([.. favs.Where(n => !selectedNames.Contains(n.Name))]));
        MainWindow.Refresh();
        MainWindow.FocusActiveView();
    }

    int SortFixedFirst(Item? item1, Item? item2)
    {
        var order = item1 is ParentItem
            ? -1
            : item2 is ParentItem
            ? 1
            : item1 is RemoteDevice && item2 is NewRemoteItem
            ? -1
            : item2 is RemoteDevice && item1 is NewRemoteItem
            ? 1
            : 0;
        return reverseOrder ? -order : order;
    }

    void SortOrderChanged(bool reverse, ColumnViewColumn? col, SorterChange sc) => reverseOrder = reverse;

    bool reverseOrder;

    RemoteDevice[] items = [];
}