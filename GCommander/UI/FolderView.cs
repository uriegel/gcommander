using Gtk4DotNet;


// TODO EmptyController
class FolderView : ColumnView
{
    public FolderView(Builder builder, string name) : base(builder, name)
    {


        // Initial Empty Controller
        var store = ListStore.New();
        model = NoSelection.New(store);
        AppendColumn(ColumnViewColumn.New("Name", SignalListItemFactory.New()).Expand());
        SetModel(model);
        OnFinalize(() =>
        {
            model.Dispose();
        });



    }



    // Initial Empty Controller
    SelectionModel model = null!;


    public void ChangePath(string? path)
    {

    }

    Controller Controller = new EmptyController();
}