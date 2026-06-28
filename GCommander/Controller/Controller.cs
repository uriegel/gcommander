abstract class Controller
{
    public static Controller GetFromPath(string? path, Controller? current)
    {
        if (path == null || path == RootController.Name)
            return RootController.Get(current);

        return new EmptyController();
    }
}