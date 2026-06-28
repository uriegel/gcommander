using Gtk4DotNet;

// TODO FolderViewController as Property in FolderView for keyboard and mouse control

class FolderView : ColumnView
{
    public FolderView(Builder builder, string name) : base(builder, name)
    {


        // To RootController
        var store = ListStore.New();
        model = NoSelection.New(store);
        AppendColumn(ColumnViewColumn.New("Name", SignalListItemFactory.New()).Expand());
        SetModel(model);
        OnFinalize(() =>
        {
            model.Dispose();
        });



    }



    // To Controller
    SelectionModel model = null!;


    public void ChangePath(string? path)
    {

    }

    Controller Controller = new RootController();
}