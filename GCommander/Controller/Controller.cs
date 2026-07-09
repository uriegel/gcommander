using Gtk4DotNet;

abstract class Controller : IDisposable
{
    public static Controller GetFromPath(string id, string? path, Controller? current, FolderView view, FolderContext context)
    {
        if (path == null || path == "/.." || path.Length == 0 || path == RootController.Name)
            return RootController.Get(id, current, view, context);
        else 
            return DirectoryController.Get(id, current, view, context);
    }

    public string Id { get; }

    public abstract string GetItemPath(int pos);
    public abstract Task<string> GetChangePath(int pos);
    public abstract Task ChangePathAsync(string path);
    public virtual void SelectAll() { }
    public virtual void SelectNone() { }
    public virtual void SelectAllAbove() { }
    public virtual void SelectAllBeneath() { }
    public virtual void ToggleSelection() { }
    public virtual void ToggleSelection(int pos) { }
    public virtual void OnWidth(int w) { }
    public virtual int GetFileCount() => 0;
    public virtual int GetDirectoryCount() => 0;

    public void SetSelection(int pos)
    {
        view.ColumnView.ScrollTo(pos, ListScrollFlags.ScrollFocus);
        model.Selected = pos;
        view.SelectionChanged(pos);
    }

    public void FilterChanged(FilterChange filterChange) => filter?.Changed(filterChange);

    public virtual bool CheckRestriction(string searchKey) => false;

    protected Controller(string id, FolderView view, FolderContext context)
    {
        Id = id;
        this.view = view;
        this.filter = CreateFilter();
        this.context = context;
        store = ListStore.New();
        sortModel = SortListModel.New(FilterListModel.New(store, filter), null);
        model = SingleSelection.New(sortModel);
        model.OnSelectionChanged += OnSelectionChange;
    }

    protected virtual CustomFilter? CreateFilter() => null;

    protected static int SortSize(long? s1, long? s2)
    {
        var a = s1.HasValue ? s1.Value : 0;
        var b = s2.HasValue ? s2.Value : 0;
        return a - b > 0
            ? 1
            : a - b < 0
            ? -1
            : 0;
    }

    void OnSelectionChange(int _, int __) => view.SelectionChanged(model.Selected);
        
 
    protected SingleSelection model;
    protected SortListModel sortModel;
    protected ListStore store;
    protected CustomFilter? filter;
    readonly protected FolderView view;
 
    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Verwalteten Zustand (verwaltete Objekte) bereinigen
                model.OnSelectionChanged -= OnSelectionChange;
                model.Dispose();
            }

            // Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
            // Große Felder auf NULL setzen
            disposedValue = true;
        }
    }

    // Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
    // ~Controller()
    // {
    //     // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    readonly protected FolderContext context;
    bool disposedValue;

    #endregion
}

static class ControllerExtensions
{
    public static string FormatSize(this long? size)
    {
        if (!size.HasValue || size == -1)
            return "";
        var sizeStr = size.Value.ToString();
        var sep = '.';
        if (sizeStr.Length > 3) 
        {
            var sizePart = sizeStr;
            sizeStr = "";
            for (var j = 3; j < sizePart.Length; j += 3) 
            {
                var extract = sizePart.Substring(sizePart.Length - j, 3);
                sizeStr = sep + extract + sizeStr;
            }
            var strfirst = sizePart[..((sizePart.Length % 3 == 0) ? 3 : (sizePart.Length % 3))];
            sizeStr = strfirst + sizeStr;
        }
        return sizeStr;    
    }
}