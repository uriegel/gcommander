using Gtk4DotNet;

class FolderPaned : Paned
{
    public FolderPaned(Builder builder, string name) : base(builder, name)
    {
        this["position"].OnNotify += OnPosition;
        columnviewLeft.ItemsChange += OnItemsChange;      
        columnviewRight.ItemsChange += OnItemsChange;
    }

    // TODO change to focus active folder
    public void SetInitialFocus() => columnviewLeft.GrabFocus();

    void OnPosition()
    {
        Console.WriteLine($" {columnviewLeft.Width} - {columnviewRight.Width}");
    }

    void OnItemsChange(bool start)
    {
        if (start)
            lastPosition = Position;
        else if (lastPosition > 0)
            Position = lastPosition;
    }

    [Widget]
    FolderView columnviewLeft = null!;

    [Widget]
    FolderView columnviewRight = null!;

    int lastPosition;
}