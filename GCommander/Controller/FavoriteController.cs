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

    public override async Task<string?> GetChangePath(int pos)
    {
        var res = GetItemPath(pos);
        if (res == "")
        {
            var path = MainWindow.GetInactiveView().Context.CurrentPath;

            var result = await AdwAlertDialog.PresentFromTemplateAsync("new-favorite", "dialog", MainWindow.Instance, (builder, name) => new NewFavorite(builder, name, path));
            if (result == "cancel")
                return null;




            //  var settings = ApplicationData.Current.LocalSettings.Values;
            //  var favs = settings["Favorites"] is string favstr ? JsonSerializer.Deserialize<Favorite[]>(favstr) ?? [] : [];
            //  settings["Favorites"] = JsonSerializer.Serialize<Favorite[]>([ ..favs, res ]);
            //  MainWindow.Refresh();



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

        //  var settings = ApplicationData.Current.LocalSettings.Values;
        //  var favs = settings["Favorites"] is string favstr ? JsonSerializer.Deserialize<Favorite[]>(favstr) ?? [] : [];
        //  items = [ 
        //      new Item("..", "iconFromRes/GoUp", [ "" ]),
        //      .. favs.Select(n => new Item(n.Name, "iconFromRes/Starred", [ n.Path ], IsSelectable: true)).OrderBy(n => n.Text),
        //      new Item("Hinzuf�gen...", "iconFromRes/Plus", [ "" ])
        //  ];
        //  SetNewPath(Name, fromHistory);
        //  return (items, 0, items.Length - 2, 0);        
    }
}


    // public override async Task<bool> DeleteItems(int[] itemsPos)
    // {
    //     var toDelete = items.Where((n, i) => itemsPos.Contains(i)).ToArray();
    //     if (await Dialog.ShowAsync(MainWindow.Content,
    //         "Favoriten l�schen",
    //         textContent: $"M�chtest du {(toDelete.Length == 1 ? "den" : "die")} Favoriten l�schen?"))
    //     {
    //         var settings = ApplicationData.Current.LocalSettings.Values;
    //         var favs = settings["Favorites"] is string favstr ? JsonSerializer.Deserialize<Favorite[]>(favstr) ?? [] : [];
    //         settings["Favorites"] = JsonSerializer.Serialize<Favorite[]>([.. favs.Where(n => !toDelete.Any(m => m.Values[0] == n.Path))]);
    //         MainWindow.Refresh();
    //         return true;
    //     }
    //     else
    //         return false;
    // }

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
