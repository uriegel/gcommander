using CsTools.Extensions;
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

    public async Task<bool> Rename()
    {
        var fileItems = controller
            .GetItems()
            .OfType<FileItem>()
            .Where(n => n.IsSelected)
            .ToArray();
        if (fileItems.Length == 0)
            return false;
        var dialog = AdwAlertDialog.New("Erweitertes Umbenennen", $"Möchtest du die Dateien umbenennen?");
        dialog.SetResponses([
                new("ok", "_OK", Default: true, Appearance: AdwResponseAppearance.Suggested),
                new("cancel", "_Abbrechen", Cancel: true)
            ]);
        var res = await dialog.PresentAsync(MainWindow.Instance);
        if (res == "cancel")
            return true;

        foreach (var item in fileItems)
            Directory.Move(controller.Context.CurrentPath.AppendPath(item.Name), controller.Context.CurrentPath.AppendPath("__RENAMING__" + item.NewName));
        foreach (var item in fileItems)
            Directory.Move(controller.Context.CurrentPath.AppendPath("__RENAMING__" + item.NewName), controller.Context.CurrentPath.AppendPath(item.NewName));
        Refresh();
        async void Refresh()
        {
            await Task.Delay(100);
            MainWindow.Refresh();    
        }    
        
        return true;
    }

    public void SelectionChanged()
    {
        int idx = UI.ExtendedRename.Value.Start;
        var fileItems = controller.GetItems().OfType<FileItem>().ToArray();
        foreach (var fileItem in fileItems.Where(n => n.IsSelected))
            fileItem.NewName = $"{UI.ExtendedRename.Value.Prefix}{idx++.ToString().PadLeft(UI.ExtendedRename.Value.Digits, '0')}{fileItem.Name.GetFileExtension()}";
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