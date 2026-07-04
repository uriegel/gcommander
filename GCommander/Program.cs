using Gtk4DotNet;

Application
    .NewAdwaita("de.uriegel.gcommandertestetste")
    .WithDiagnostics(true)
    .OnActivate(app => app
        .WindowFromBuilder("mainwindow", "window", p => new MainWindow(p))
        .Show()
    ).Run();


