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

    protected Controller(Func<SortListModel, SelectionModel> getModel)
    {
        store = ListStore.New();
        sortModel = SortListModel.New(store, null);
        model = getModel(sortModel);
    }

    protected static int SortSize(long? s1, long? s2)
        => s1 - s2 > 0
            ? 1
            : s1 - s2 < 0
            ? -1
            : 0;

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
                // Verwalteten Zustand (verwaltete Objekte) bereinigen
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