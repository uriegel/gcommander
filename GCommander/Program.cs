using Gtk4DotNet;

Streamer.Init();

Application
    .NewAdwaita("de.uriegel.gcommander")
    .WithDiagnostics(true)
    .WithWebKit()
    .WithWebsiteFromResource()
    .WithSettings()
    .WithAdditionals()
    .OnActivate(app => app
        .WindowFromBuilder("mainwindow", "window", p => new MainWindow(p))
        .Show()
    ).Run();


