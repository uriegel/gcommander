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
                label.Text = itemIndexes.TryGetValue(item?.Name ?? "", out var newName) ? $"{newName}" : "";
            });

        var col = ColumnViewColumn
            .New("Neuer Name", factory)
            .Expand();
        controller.InsertColumn(1, col);
        controller.SelectNone();
    }

    public void SelectionChanged()
    {
        var idx = 0;
        itemIndexes = controller
            .GetItems()
            .Select(n => (n.Name, n is FileItem fi && fi.IsSelected ? idx++ : -1))
            .ToDictionary(n => n.Name, n => n.Item2);
    }

    readonly DirectoryController controller;

    Dictionary<string, int> itemIndexes = [];

    #region IDisposable

    public void Dispose()
    {
        controller.RemoveColumn(1);
        controller.SelectNone();
    } 
    
    #endregion
}