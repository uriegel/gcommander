using CsTools.Extensions;
using Gtk4DotNet;

class DirectoryController : Controller
{
    public override void Activate(int position)
    {
    }

    public override void ChangePath(string? path)
    {
    }

    public override string GetChangePath(int pos) => GetItemPath(pos);

    public override string GetItemPath(int pos)
    {
        throw new NotImplementedException();
    }

    public override void Refresh()
    {
        throw new NotImplementedException();
    }

    public DirectoryController(Controller? previous, ColumnView view, FolderViewController folderView)
        //: base(MultiSelection.New)
        : base(NoSelection.New)
    {
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
                var item = listitem.GetItem<DirectoryItem>();
                iconname?.Name = item?.Name ?? "";
                // if (item?.IconName != null)
                //     iconname?.SetFromIconName(item.IconName);
                // var row = iconname?.GetParent()?.GetParent();
                // row?.AddCssClass("hiddenItem", item?.IsMounted != true);
            });

        // var descriptionfactory = SignalListItemFactory
        //     .New()
        //     .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.Start).SetEllipsize(EllipsizeMode.End)))
        //     .Bind(listitem =>
        //     {
        //         var label = listitem.GetChild<Label>();
        //         var item = listitem.GetItem<RootItem>();
        //         label.Text = item?.Description ?? "";
        //     });

        // var mountPointfactory = SignalListItemFactory
        //     .New()
        //     .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.Start).SetEllipsize(EllipsizeMode.End)))
        //     .Bind(listitem =>
        //     {
        //         var label = listitem.GetChild<Label>();
        //         var item = listitem.GetItem<RootItem>();
        //         label.Text = item?.MountPoint ?? "";
        //     });

        // var usefactory = SignalListItemFactory
        //     .New()
        //     .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.End).SetEllipsize(EllipsizeMode.End)))
        //     .Bind(listitem =>
        //     {
        //         var label = listitem.GetChild<Label>();
        //         var item = listitem.GetItem<RootItem>();
        //         label.Text = item?.Use != null ? $"{item?.Use}%" : "";
        //         if (item?.Use > 90)
        //             label?.GetParent()?.AddCssClass("warning", item?.IsMounted == true);
        //     });

        // var sizefactory = SignalListItemFactory
        //     .New()
        //     .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.End).SetEllipsize(EllipsizeMode.End)))
        //     .Bind(listitem =>
        //     {
        //         var label = listitem.GetChild<Label>();
        //         var item = listitem.GetItem<RootItem>();
        //         label.Text = item?.Size.FormatSize() ?? "";
        //     });

        view.SetModel(null);
        view.ClearColumns();
        view.SetModel(model);
        
        previous?.Dispose();

        using var nameSorter = CustomSorter.New<DirectoryItem>((item1, item2) => 0); //(item1?.Name ?? "").CompareTo(item2?.Name ?? ""));
        using var nameMultiSorter = MultiSorter.New().Append(CustomSorter.New<DirectoryItem>(SortDirectoriesFirst)).Append(nameSorter);
        var firstCol = ColumnViewColumn
            .New("Name", namefactory)
            .Expand()
            .SideEffect(cvc => cvc.SetSorter(nameMultiSorter));
        view.AppendColumn(firstCol);
        view.SortByColumn(firstCol);

        // using var descriptionSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.Description ?? "").CompareTo(item2?.Description ?? ""));
        // using var descriptionMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(descriptionSorter);
        // view.AppendColumn(ColumnViewColumn
        //     .New("Bezeichnung", descriptionfactory)
        //     .Expand()
        //     .SideEffect(cvc => cvc.SetSorter(descriptionMultiSorter))
        // );
        // using var mountPointSorter = CustomSorter.New<RootItem>((item1, item2) => (item1?.MountPoint ?? "").CompareTo(item2?.MountPoint ?? ""));
        // using var mountPointMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(mountPointSorter);
        // view.AppendColumn(ColumnViewColumn
        //     .New("MountPoint", mountPointfactory)
        //     .Expand()
        //     .SideEffect(cvc => cvc.SetSorter(mountPointMultiSorter))
        // );
        // using var useSorter = CustomSorter.New<RootItem>((item1, item2) => SortSize(item1?.Use, item2?.Use));
        // using var useMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(useSorter);
        // view.AppendColumn(ColumnViewColumn
        //     .New("%", usefactory)
        //     .SideEffect(cvc => cvc.SetSorter(useMultiSorter))
        // );
        // using var sizeSorter = CustomSorter.New<RootItem>((item1, item2) => SortSize(item1?.Size, item2?.Size));
        // using var sizeMultiSorter = MultiSorter.New().Append(CustomSorter.New<RootItem>(SortMounted)).Append(sizeSorter);
        // view.AppendColumn(ColumnViewColumn
        //     .New("Größe", sizefactory)
        //     .SideEffect(cvc => cvc.SetSorter(sizeMultiSorter))
        // );

        using var viewsorter = view.GetSorter();
        viewsorter.OnChanged -= folderView.SortOrderChanged;
        viewsorter.OnChanged += folderView.SortOrderChanged;
        sortModel.SetSorter(viewsorter);

        // StartMonitoring();
    }

    int SortDirectoriesFirst(DirectoryItem? item1, DirectoryItem? item2)
    {
        return 0;
        // var order = item1?.IsMounted == true && item2?.IsMounted != true
        //     ? -1
        //     : item2?.IsMounted == true && item1?.IsMounted != true
        //     ? 1
        //     : 0;
        // return folderView.ReverseSortOrder ? -order : order;
    }

    readonly FolderViewController folderView;
    readonly ColumnView view;

    #region IDisposable

    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                //                volumeMonitor?.Dispose();
            }

            // Free unmanaged resources owned by DerivedClass

            disposed = true;
        }

        base.Dispose(disposing);
    }
    bool disposed;

    #endregion
}

