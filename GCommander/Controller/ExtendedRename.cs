using Gtk4DotNet;

class ExtendedRename : IDisposable
{
    public ExtendedRename(DirectoryController controller) 
    {
        this.controller = controller;

        var factory = SignalListItemFactory
            .New()
            .Setup(listitem => listitem.SetChild(Label.New().HAlign(Align.Start).SetEllipsize(EllipsizeMode.End)))
            .Bind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                var item = listitem.GetItem<Item>();
                label.Text = "Neu"; //item?.Description ?? "";
            });

        var col = ColumnViewColumn
            .New("Neuer Name", factory)
            .Expand();
        controller.InsertColumn(1, col);
    }

    readonly DirectoryController controller;

    #region IDisposable

    public void Dispose()
    {
        controller.RemoveColumn(1);
    }
    
    #endregion
}