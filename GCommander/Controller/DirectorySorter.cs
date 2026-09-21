using CsTools.Extensions;
using Gtk4DotNet;

class DirectorySorter
{
    public const string NAME = "Name";
    public const string ERWEITERUNG = "Erweiterung";

    public bool ReverseOrder { get; private set; } 
 
    public DirectorySorter(Action? onChanged = null) 
    {
        this.onChanged = onChanged;
    }
    
    public int NameOrExtensionOrder(Item? item1, Item? item2)
        => extensionSearch
            ? (item1?.Name.GetFileExtension() ?? "").CompareTo(item2?.Name.GetFileExtension() ?? "")
            : (item1?.Name ?? "").CompareTo(item2?.Name ?? "");

    public void SortOrderChanged(bool reverse, ColumnViewColumn? col, SorterChange sc)
    {
        if ((lastSearchTitle == NAME || lastSearchTitle == ERWEITERUNG) && col?.Title == lastSearchTitle && ReverseOrder != reverse && !reverse)
        {
            extensionSearch = lastSearchTitle == NAME;
            nameOrExt = col;
            col?.Title = extensionSearch ? ERWEITERUNG : NAME;
        }
        if (col?.Title != NAME && col?.Title != ERWEITERUNG && extensionSearch)
        {
            extensionSearch = false;
            // This is a little bit dangerous!!
            nameOrExt?.Title = NAME;
        }
        ReverseOrder = reverse;
        lastSearchTitle = col?.Title ?? "";

        if (onChanged != null)
            OnChanged();

        async void OnChanged()
        {
            await Task.Delay(300);
            onChanged?.Invoke();
        }
    }

    public int SortDirectoriesFirst(Item? item1, Item? item2)
    {
        var order = item1 is ParentItem
            ? -1
            : item2 is ParentItem
            ? 1
            : item1 is DirectoryItem && item2 is FileItem
            ? -1
            : item2 is DirectoryItem && item1 is FileItem
            ? 1
            : 0;
        return ReverseOrder ? -order : order;
    }

    public static int SortSize(Item? item1, Item? item2)
    {
        var a = item1 is FileItem fi1 ? fi1.Size : 0;
        var b = item2 is FileItem fi2 ? fi2.Size : 0;
        return a - b > 0
            ? 1
            : a - b < 0
            ? -1
            : 0;
    }

    string lastSearchTitle = "";
    bool extensionSearch;
    ColumnViewColumn? nameOrExt;
    Action? onChanged;
}