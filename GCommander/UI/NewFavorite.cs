using System.Runtime.InteropServices;
using CsTools.Extensions;
using Gtk4DotNet;

class NewFavorite : AdwAlertDialog
{
    public static async Task<FavoriteItem?> PresentAsync(string path, Widget parent)
    {
        using var builder = Builder.FromDotNetResource("new-favorite");
        var dialog = new NewFavorite(builder, "dialog", path);
        var res = await dialog.PresentAsync(parent);
        if (res == "cancel")
            return null;
        return dialog.Result;
    }

    public FavoriteItem? Result
    {
        get
        {
            var editable = nameEntry.AsEditable();
            var text = editable.Text;
            editable = pathEntry.AsEditable();
            var path = editable.Text;
            return new(text, path   );
        }
    }
    
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





    [DllImport("libadwaita-1.so.0",
    CallingConvention = CallingConvention.Cdecl)]
    private static extern void adw_alert_dialog_response(
    nint self,
    [MarshalAs(UnmanagedType.LPUTF8Str)] string response);
}