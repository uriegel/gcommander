using System.Diagnostics;
using CsTools.Extensions;
using Gtk4DotNet;

class AppChooser : AdwDialog
{
    public AppChooser(Builder builder, string name, string path, string fileName) : base(builder, name)

    {
        description.Text = $"Wähle eine App, um <b>{fileName}</b> zu öffnen";
        SetDefaultWidget(openBtn);

        using var actiongroup = SimpleActionGroup.New("appchooser");
        actiongroup.AddActions(
            new SimpleAction("openfile", () => StartProcess(listbox, path, fileName)),
            new SimpleAction("cancel", CloseDialog)
        );
        InsertActionGroup("appchooser", actiongroup);

        var keyController = KeyEventController.New();
        keyController.OnKeyPressed += (chr, mod) =>
        {
            if (chr == 13)
            {
                StartProcess(listbox, path, fileName);
                return true;
            }
            return false;
        };
        AddController(keyController);

        listbox.SetHeaderFunc((current, previous) =>
        {
            var currentListitem = current?.GetChild<ListItem>();
            var previousListitem = previous?.GetChild<ListItem>();
            if (previous == null && currentListitem?.IsRecommended == true)
                current?.CreateHeader("Empfohlene Apps");
            if (previousListitem?.IsRecommended == true && currentListitem?.IsRecommended == false)
                current?.CreateHeader("Alle Apps");
        });

        EventController CreatePressed() => ClickGesture.New().SideEffect(c => c.OnPressed += (n, x, y, mod) =>
        {
            if (n == 2)
                StartProcess(listbox, path, fileName);
        });

        var contentType = Gio.GuessContentType(fileName) ?? "none";
        using var defaultApp = GAppInfo.GetDefault(contentType);
        if (defaultApp != null)
            listbox.AppendFromTemplate("appitem", b => new ListItem(b, defaultApp.GetIcon(), defaultApp.Name, true)
                .RegisterWidget()
                .SideEffect(n => AttachData(n, defaultApp.Executable)));
        using var recommendedApps = GAppInfo.GetRecommendedApps(contentType);
        foreach (var appinfo in recommendedApps.OrderBy(n => n.Name).Where(n => n.ShouldShow && n.Name != defaultApp?.Name))
            listbox.AppendFromTemplate("appitem", b => new ListItem(b, appinfo.GetIcon(), appinfo.Name, true)
                .RegisterWidget()
                .SideEffect(n => AttachData(n, appinfo.Executable)));
        using var apps = GAppInfo.GetAllApps();
        foreach (var appinfo in apps.OrderBy(n => n.Name).Where(n => n.ShouldShow))
            listbox.AppendFromTemplate("appitem", b => new ListItem(b, appinfo.GetIcon(), appinfo.Name)
                .RegisterWidget()
                .SideEffect(n => AttachData(n, appinfo.Executable)));

        void AttachData(ListItem listItem, string? executable)
        {
            listItem.AddController(CreatePressed());
            listItem.SetManagedData("data", executable ?? "");
        }

        Focus();
        async void Focus()
        {
            await Task.Delay(200);
            var row = listbox.GetRowAtIndex(0);
            listbox.SelectRow(row);
            row.GrabFocus();
        }
    }

    void StartProcess(ListBox listbox, string path, string fileName)
    {
        try
        {
            var row = listbox.GetSelectedRow().GetChild<Box>();
            var executable = row?.GetManagedData<string>("data");
            new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = $"\"{path.AppendPath(fileName)}\"",
                    CreateNoWindow = true
                }
            }.Start();
            CloseDialog();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Could not start process: {e}");
        }
    }

    [Widget]
    readonly Button openBtn = null!;

    [Widget]
    readonly Label description = null!;

    [Widget]
    readonly ListBox listbox = null!;
}

static class MyWindowExtensions
{
    public static void CreateHeader(this ListBoxRow listBoxRow, string header)
    {
        using var builder = Builder.FromDotNetResource("appitemheader");
        listBoxRow.SetHeader(new ListItemHeader(builder, header));
    }
}

