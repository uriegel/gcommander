using Gtk4DotNet;

// TODO FolderViewController as Property in FolderView for keyboard and mouse control, only for MultiSelection

class FolderView : ColumnView
{
    public FolderView(Builder builder, string name) : base(builder, name)
    {
        controller = Controller.GetFromPath(null, null, this)!;
        controller.ChangePath(null);

        OnFinalize(() =>
        {
            controller.Dispose();
        });
    }

    public void ChangePath(string? path)
    {

    }

    Controller controller;
}