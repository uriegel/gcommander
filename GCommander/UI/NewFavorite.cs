using CsTools.Extensions;
using Gtk4DotNet;

class NewFavorite : AdwAlertDialog
{
    public NewFavorite(Builder builder, string name, string path) : base(builder, name)
    {
        var editable = nameEntry.AsEditable();
        editable.Text = path.SubstringAfterLast('/').WhiteSpaceToNull() ?? path;
        editable = pathEntry.AsEditable();
        editable.Text = path;
    }

    [Widget(Name = "name")]
    public readonly Entry nameEntry = null!;

    [Widget(Name = "path")]
    public readonly Entry pathEntry = null!;
}