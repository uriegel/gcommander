using Gtk4DotNet;
using Extensions;

namespace UI;

class Conflicts : AdwAlertDialog, IDisposable
{
    public static async Task<string?> PresentAsync(ConflictItem[] items)
    {
        using var builder = Builder.FromDotNetResource("conflicts");
        using var dialog = new Conflicts(builder, "dialog", items);
        var res = await dialog.PresentAsync(MainWindow.Instance);
        return null;
    }

    public Conflicts(Builder builder, string name, ConflictItem[] items)
        : base(builder, name)
    {
        conflictBox.SetSizeRequest(MainWindow.Instance.Width - 80, MainWindow.Instance.Height - 300);
        showDirection.RightToLeft = !MainWindow.IsLeftActive();

        var store = new ListStore<ConflictItem>();
        store.Initialize(items);
        model = SingleSelection.New(store);
        columnView.SetModel(model);

        var namefactory = SignalListItemFactory.New();
        namefactory.Setup(n =>
        {
            using var builder = Builder.FromDotNetResource("conflictnameitem");
            var item = new ConflictNameItem(builder);
            n.SetManagedChild(item);
        });
        namefactory.Bind(listitem =>
        {
            var iconname = listitem.GetManagedChild<ConflictNameItem>();
            var item = listitem.GetItem<ConflictItem>();
            iconname?.Name = item?.Name ?? "";
            iconname?.SubPath = item?.SubPath ?? "";
            iconname?.SetIcon(item?.Name ?? "");
        });
        var dateTimeFactory = SignalListItemFactory.New();
        dateTimeFactory.Setup(n =>
        {
            using var builder = Builder.FromDotNetResource("conflictitem");
            var item = new ConflictColumnItem(builder);
            n.SetManagedChild(item);
        });
        dateTimeFactory.Bind(listitem =>
        {
            var iconname = listitem.GetManagedChild<ConflictColumnItem>();
            var item = listitem.GetItem<ConflictItem>();
            iconname?.Name = item?.DateTime.ToString("g") ?? "";
            iconname?.Name2 = item?.TargetDateTime.ToString("g") ?? "";
        });
        var sizeFactory = SignalListItemFactory.New();
        sizeFactory.Setup(n =>
        {
            using var builder = Builder.FromDotNetResource("conflictitem");
            var item = new ConflictColumnItem(builder)
            {
                RightAligned = true
            };
            n.SetManagedChild(item);
        });
        sizeFactory.Bind(listitem =>
        {
            var iconname = listitem.GetManagedChild<ConflictColumnItem>();
            var item = listitem.GetItem<ConflictItem>();
            iconname?.Name = item?.Size.FormatSize() ?? "";
            iconname?.Name2 = item?.TargetSize.FormatSize() ?? "";
        });
        columnView.AppendColumn(ColumnViewColumn.New("Name", namefactory).Expand());
        columnView.AppendColumn(ColumnViewColumn.New("Datum", dateTimeFactory).Expand());
        columnView.AppendColumn(ColumnViewColumn.New("Größe", sizeFactory).Expand());
    }

    [Widget]
    readonly Box conflictBox = null!;

    [Widget]
    readonly ShowDirection showDirection = null!;

    [Widget]
    readonly ColumnView columnView = null!;

    readonly SelectionModel model = null!;

    #region IDisposable

    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
                model.Dispose();

            // Free unmanaged resources owned by DerivedClass
            disposed = true;
        }

        base.Dispose(disposing);
    }
    bool disposed;

    #endregion 
}

