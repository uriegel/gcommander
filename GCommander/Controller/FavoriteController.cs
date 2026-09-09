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
                var item = listitem.GetItem<FavoriteItem>();
                iconname?.Name = item?.Name ?? "";
                if (item?.Name == "..")
                    iconname?.SetFromIconName("go-up");
                else 
                    iconname?.SetFromIconName("add");
                var row = iconname?.GetParent()?.GetParent();
            });

        var pathfactory = SignalListItemFactory
            .New()
            .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.Start).SetEllipsize(EllipsizeMode.End)))
            .Bind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                var item = listitem.GetItem<FavoriteItem>();
                label.Text = item?.Path ?? "";
            });

        view.ColumnView.SetModel(null);
        view.ColumnView.ClearColumns();
        view.ColumnView.SetModel(model);

        previous?.Dispose();

        using var nameSorter = CustomSorter.New<FavoriteItem>((item1, item2) => (item1?.Name ?? "").CompareTo(item2?.Name ?? ""));
        var firstCol = ColumnViewColumn
            .New("Name", namefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(nameSorter));
        view.ColumnView.AppendColumn(firstCol);
        view.ColumnView.SortByColumn(firstCol);

        using var pathSorter = CustomSorter.New<FavoriteItem>((item1, item2) => (item1?.Path ?? "").CompareTo(item2?.Path ?? ""));
        view.ColumnView.AppendColumn(ColumnViewColumn
            .New("Path", pathfactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(pathSorter))
        );

        using var viewsorter = view.ColumnView.GetSorter();
        // viewsorter.OnChanged -= SortOrderChanged;
        // viewsorter.OnChanged += SortOrderChanged;
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

    public override async Task<string> GetChangePath(int pos)
    {
        var res = GetItemPath(pos);
        if (res == "")
        {
            var dialog = AdwAlertDialog.New("Neuer Favorit", "Möchtest du einen neuen Favoriten hinzufügen?");
            dialog.SetResponses([
                new("ok", "_OK", Default: true, Appearance: AdwResponseAppearance.Suggested),
                new("cancel", "_Abbrechen", Cancel: true)
            ]);
            await dialog.PresentAsync(MainWindow.Widget);            
        }
        return res;
    }

    public override string GetItemPath(int pos)
    {
        return pos == 0
        ? RootController.Name
        : "";
    }

    static async Task<FavoriteItem[]> Get()
    {
        return [
            new FavoriteItem("..", ""),
            new FavoriteItem("Favoriten hinzufügen", "")
        ];
    }
}