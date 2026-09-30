using Gtk4DotNet;

Streamer.Init();

var app = new AdwApplication("de.uriegel.gcommander");
app.WithDiagnostics(true);
app.WithWebKit();
app.WebsiteFromResource = true;
app.WithSettings();
app.WithAdditionals();
app.OnActivate += () =>
        app.WindowFromBuilder("mainwindow", "window", p => new MainWindow(p))
            .Show();
app.Run();


