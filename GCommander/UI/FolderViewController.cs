using Gtk4DotNet;

class FolderViewController
{
    public bool ReverseSortOrder { get; private set; }
    public FolderViewController(ColumnView view)
    {
        this.view = view;
    }

    public void SortOrderChanged(bool reverse, SorterChange _) => ReverseSortOrder = reverse;

    readonly ColumnView view;
}