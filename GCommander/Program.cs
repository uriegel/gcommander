using Gtk4DotNet;

Application
    .NewAdwaita("de.uriegel.gcommander")
    .WithDiagnostics(true)
    .OnActivate(app => app
        .WindowFromBuilder("mainwindow", "window", p => new MainWindow(p))
        .Show()
    ).Run();


