using Gtk4DotNet;

abstract class Controller : IDisposable
{
    public static Controller? GetFromPath(string? path, Controller? current, ColumnView view)
    {
        if (path == null || path == RootController.Name)
            return RootController.Get(current, view);

        return new RootController(current, view);
    }

    public abstract void ChangePath(string? path);
    
    protected Controller(Func<ListStore, SelectionModel> getModel)
    {
        store = ListStore.New();
        sortModel = SortListModel.New(store, null);
        model = getModel(store);
    }

    protected SelectionModel model;
    protected SortListModel sortModel;
    protected ListStore store;

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: Verwalteten Zustand (verwaltete Objekte) bereinigen
                model.Dispose();
            }

            // TODO: Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
            // TODO: Große Felder auf NULL setzen
            disposedValue = true;
        }
    }

    // // TODO: Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
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

    bool disposedValue;

    #endregion
}