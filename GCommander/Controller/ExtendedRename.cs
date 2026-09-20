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
                if (listitem.GetItem<Item>() is FileItem fileItem)
                {
                    label.DataContext = fileItem;
                    label.SetBinding("label", nameof(fileItem.NewName));
                }
            })
            .Unbind(listitem =>
            {
                var label = listitem.GetChild<Label>();
                label.UnsetBinding("label");
                label.Text = "";
                label.DataContext = null;
            });


        var col = ColumnViewColumn
            .New("Neuer Name", factory)
            .Expand();
        controller.InsertColumn(1, col);
        controller.SelectNone();
    }

    public void SelectionChanged()
    {
        int idx = UI.ExtendedRename.Value.Start;
        var fileItems = controller.GetItems().OfType<FileItem>().ToArray();
        foreach (var fileItem in fileItems.Where(n => n.IsSelected))
            fileItem.NewName = $"{UI.ExtendedRename.Value.Prefix}{idx++.ToString().PadLeft(UI.ExtendedRename.Value.Digits, '0')}";
        foreach (var fileItem in fileItems.Where(n => !n.IsSelected))
            fileItem.NewName = "";
    }

    readonly DirectoryController controller;

    #region IDisposable

    public void Dispose()
    {
        controller.RemoveColumn(1);
        controller.SelectNone();
    } 
    
    #endregion
}