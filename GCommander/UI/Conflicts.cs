using Gtk4DotNet;

namespace UI;

class Conflicts : AdwAlertDialog
{
    public static async Task<string?> PresentAsync()
    {
        using var builder = Builder.FromDotNetResource("conflicts");
        var dialog = new Conflicts(builder, "dialog");
        var res = await dialog.PresentAsync(MainWindow.Instance);
        return null;
    }

    public Conflicts(Builder builder, string name)
        : base(builder, name)
    {
        conflictBox.SetSizeRequest(MainWindow.Instance.Width - 80, MainWindow.Instance.Height - 300);
        showDirection.RightToLeft = !MainWindow.IsLeftActive();
    }

    [Widget]
    readonly Box conflictBox = null!;
    
    [Widget]
    readonly ShowDirection showDirection = null!;
}

