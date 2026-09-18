using Gtk4DotNet;

abstract class Controller : IDisposable
{
    public static Controller GetFromPath(string id, string? path, Controller? current, FolderView view, FolderContext context)
    {
        if (path == null || path == "/.." || path.Length == 0 || path == RootController.Name)
            return RootController.Get(id, current, view, context);
        else if (path == FavoriteController.Name)
            return FavoriteController.Get(id, current, view, context);
        else
            return DirectoryController.Get(id, current, view, context);
    }

    public string Id { get; }

    public abstract string GetItemPath(int pos);
    public virtual ExifData? GetExifData(int pos) => null;
    public abstract Task<string?> GetActivationPath(int pos);
    public abstract Task ChangePathAsync(string path, bool fromHistory = false);
    
    public void SelectAll()
    {
        foreach (var item in store.GetItems<Item>().OfType<SelectableItem>())
            item.IsSelected = true;
    }

    public void SelectNone()
    {
        foreach (var item in store.GetItems<Item>().OfType<SelectableItem>())
            item.IsSelected = false;
    }

    public void ToggleSelection()
    {
        var pos = model.Selected;
        if (model.GetItem<Item>(pos) is SelectableItem item)
            item.IsSelected = item.IsSelected != true;
        SetSelection(Math.Min(pos + 1, model.GetItemsCount() - 1));
    }

    public void ToggleSelection(int pos)
    {
        if (model.GetItem<Item>(pos) is SelectableItem item)
            item?.IsSelected = item.IsSelected != true;
    }

    public virtual void OpenWith(int pos) {}

    public void SelectAllAbove()
    {
        foreach (var item in model.GetItems<Item>().OfType<SelectableItem>().Take(model.Selected))
            item.IsSelected = true;
        foreach (var item in model.GetItems<Item>().OfType<SelectableItem>().Skip(model.Selected))
            item.IsSelected = false;
    }

    public void SelectAllBeneath()
    {
        foreach (var item in model.GetItems<Item>().OfType<SelectableItem>().Take(model.Selected))
            item.IsSelected = false;
        foreach (var item in model.GetItems<Item>().OfType<SelectableItem>().Skip(model.Selected - 1))
            item.IsSelected = true;
    }

    public IEnumerable<Item> GetSelectedItems(int focusedPos = -1)
    {
        var selected = store
            .GetItems<Item>()
            .OfType<SelectableItem>()
            .Where(n => n.IsSelected);
        if (selected.Any())
            return selected;
        if (focusedPos == -1)
            return [];
        return model.GetItem<Item>(focusedPos) is SelectableItem item ? [ item ] : [];
    }

    public virtual Task Delete(int focusedPos) => Task.CompletedTask;
    public virtual Task Copy(int focusedPos, bool move) => Task.CompletedTask;
    public virtual Task Rename(int focusedPos) => Task.CompletedTask;
    public virtual Task RenameAsCopy(int focusedPos) => Task.CompletedTask;
    public virtual Task CreateFolder(int focusedPos) => Task.CompletedTask;
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

    public void Refresh()
    {
        var sorter = sortModel.GetSorter();
        sorter?.Changed(SorterChange.Different);
    }

    public virtual bool CheckRestriction(string searchKey) => false;

    protected Controller(string id, FolderView view, FolderContext context)
    {
        Id = id;
        this.view = view;
        this.filter = CreateFilter();
        this.context = context;
        store = new(item => item.Name);
        sortModel = SortListModel.New(new FilterListModel<Item>(store, filter), null);
        model = SingleSelection.New(sortModel);
        model.OnSelectionChanged += OnSelectionChange;
    }

    protected void SetNewPath(string path, bool fromHistory = false)
    {
        if (!fromHistory)
            context.AddHistory(path);
        context.CurrentPath = path;
    }

    protected virtual CustomFilter? CreateFilter() => null;

    void OnSelectionChange(int _, int __) => view.SelectionChanged(model.Selected);
        
 
    protected SingleSelection model;
    protected SortListModel sortModel;
    protected KeyedListStore<Item, string> store;
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

