class RootController : Controller
{
    public const string Name = "Root";

    public static RootController Get(Controller? current)
        => current is RootController root
            ? root 
            : new RootController();
}