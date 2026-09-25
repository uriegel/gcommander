using CsTools.Extensions;

static class DeleteEmptyDirectories
{
    public static void Delete(SelectableItem[] items, string path)
    {
        foreach (var dir in items.OfType<DirectoryItem>())
        {
            var dirToCheck = path.AppendPath(dir.Name);
            if (dirToCheck.IsDirectoryEmpty())
            {
                try
                {
                    Directory.Delete(dirToCheck, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Konnte Verzeichnis nicht löschen: {e}");
                }
            }
        }
    }

    static bool IsDirectoryEmpty(this string dir)
    {
        var info = new DirectoryInfo(dir);
        if (info.GetFiles().Length != 0)
            return false;

        var dirs = info
            .GetDirectories()
            .Select(n => n.FullName);
        foreach (var subDir in dirs)
        {
            if (!subDir.IsDirectoryEmpty())
                return false;
        }
        return true;
    }
}