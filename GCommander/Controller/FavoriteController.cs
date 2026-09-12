using System.Text.Json;
using CsTools.Extensions;
using Gtk4DotNet;

class FavoriteController : Controller
{
    public const string Name = "fav";

    public static FavoriteController Get(string id, Controller? current, FolderView view, FolderContext context)
        => current is FavoriteController favoriteController
            ? favoriteController
            : new FavoriteController(id, current, view, context);

    public FavoriteController(string id, Controller? previous, FolderView view, FolderContext context) : base(id, view, context)
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
                else if (item is NewFavoriteItem)
                    iconname?.SetFromIconName("add");
                else
                    iconname?.SetFromIconName("starred");
                var row = iconname?.GetParent()?.GetParent();
                row?.DataContext = item;
                if (item is SelectableItem si)
                    row?.SetBindingToCss("selection", nameof(si.IsSelected));
            });

        var pathfactory = SignalListItemFactory
            .New()
            .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.Start).SetEllipsize(EllipsizeMode.End)))
            .Bind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                var item = listitem.GetItem<Item>();
                label.Text = item is FavoriteItem fi ? fi.Path : "";
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

        using var pathSorter = CustomSorter.New<Item>((item1, item2) => (item1 is FavoriteItem fi ? fi.Path : "").CompareTo(item2 is FavoriteItem fi2 ? fi2.Path : ""));
        using var pathMultiSorter = MultiSorter.New().Append(CustomSorter.New<Item>(SortFixedFirst)).Append(pathSorter);
        view.ColumnView.AppendColumn(ColumnViewColumn
            .New("Path", pathfactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(pathMultiSorter))
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
        store.Splice(0, store.ItemsCount(), items);
        view.OnItemsChange(false);
        view.ColumnView.ScrollTo(0, ListScrollFlags.ScrollFocus);
        view.SelectionChanged(0);
    }

    public override async Task<string?> GetChangePath(int pos)
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
            : model.GetItem<Item>(pos) is FavoriteItem fi ? fi.Path : "";
    
    public override int GetDirectoryCount() => model.GetItems<Item>().OfType<FavoriteItem>().Count();
    public override int GetFileCount() => 0;

    public override async void Delete(int focusedPos)
    {
        var selected = GetSelectedItems(focusedPos).OfType<FavoriteItem>().ToArray();
        if (selected.Length == 0)
            return;
        var dialog = AdwAlertDialog.New("Favoriten löschen", $"Möchtest du {(selected.Length > 1 ? "die" : "den")} Favoriten löschen?");
        dialog.SetResponses([
                new("ok", "_OK", Default: true, Appearance: AdwResponseAppearance.Suggested),
                new("cancel", "_Abbrechen", Cancel: true)
            ]);
        var res = await dialog.PresentAsync(MainWindow.Instance);
        if (res == "cancel")
            return;

        var favs = Application.Settings.GetString("favorites") is string favstr && favstr.Length > 0
                ? JsonSerializer.Deserialize<FavoriteItem[]>(favstr) ?? []
                : [];
        var selectedNames = selected.Select(n => n.Name);
        Application.Settings.SetString("favorites", JsonSerializer.Serialize<FavoriteItem[]>([.. favs.Where(n => !selectedNames.Contains(n.Name))]));
        MainWindow.Refresh();
        MainWindow.FocusActiveView();
    }

    public override async void Rename(int focusedPos)
    {
        var name = model.GetItem<Item>(focusedPos) is SelectableItem item ? item.Name : null;
        if (name == null)
            return;
        var res = UI.Rename.PresentAsync(name, MainWindow.Instance);
        if (res == null)
            return;
    }

    async Task<Item[]> Get()
    {
        items = Application.Settings.GetString("favorites") is string favstr && favstr.Length > 0
                ? JsonSerializer.Deserialize<FavoriteItem[]>(favstr) ?? []
                : [];
        return [
            new ParentItem(),
            .. items.Select(n => new FavoriteItem(n.Name, n.Path)),
            new NewFavoriteItem()
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
    FavoriteItem[] items = [];
}


    // public override async Task<bool> Rename(int pos, bool asCopy)
    // {
    //     var item = items[pos];
    //     var newName = await Dialog.ShowAsync(MainWindow.Content, "Umbenennen",
    //         d => (d.Content as RenameDialog)?.FileName ?? "",
    //         new RenameDialog()
    //         {
    //             Description = "M�chtest du den Favoriten umbenennen?",
    //             FileName = item.Text
    //         });
    //     if (newName != null)
    //     {
    //         var settings = ApplicationData.Current.LocalSettings.Values;
    //         var favs = settings["Favorites"] is string favstr ? JsonSerializer.Deserialize<Favorite[]>(favstr) ?? [] : [];
    //         settings["Favorites"] = JsonSerializer.Serialize<Favorite[]>(
    //             [.. favs.Select(n => n.Path == item.Values[0] ? new Favorite(newName, item.Values[0]) : n)]
    //         );
    //         MainWindow.Refresh();
    //         return true;
    //     }
    //     else
    //         return false;
    // }
