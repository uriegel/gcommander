using Gtk4DotNet;

namespace UI;

class ExtendedRename : AdwAlertDialog
{
    public static async Task<bool> PresentAsync()
    {
        using var builder = Builder.FromDotNetResource("extendedrename");
        var dialog = new ExtendedRename(builder, "dialog");
        var res = await dialog.PresentAsync(MainWindow.Instance);
        return res != "cancel";
    }

    public ExtendedRename(Builder builder, string name)
        : base(builder, name)
    {
        start.AsEditable().OnInsertText += text =>
        {
            if (text.All(char.IsDigit) == false)
                start.AsEditable().StopInserting();
        };
    }

    [Widget]
    readonly Entry start = null!;
}