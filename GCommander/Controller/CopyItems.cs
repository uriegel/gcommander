using CsTools.Extensions;

static class CopyItems
{
    public static IEnumerable<ConflictItem> GetConflictItems(CopyItem[] items, string targetPath)
    {
        foreach (var item in items)
        {
            var target = targetPath.AppendPath(item.SubPath).AppendPath(item.Name);
            if (File.Exists(target))
            {
                var targetInfo = new FileInfo(target);
                yield return new(item.Name, item.SubPath, item.Size, targetInfo.Length, item.DateTime, targetInfo.LastWriteTime);
            }
        }
    }

    public static IEnumerable<CopyItem> Get(FolderContext context, SelectableItem[] selected)
    {
        var items = selected
            .SelectFilterNull(n => n is FileItem fi ? new CopyItem(fi.Name, "", fi.Size, fi.DateTime) : null)
            .OrderBy(n => n.Name);
        foreach (var item in items)
            yield return item;

        var dirItems = selected
            .SelectFilterNull(n => n is DirectoryItem di ? di.Name : null)
            .OrderBy(n => n);

        foreach (var item in dirItems)
        {
            foreach (var dirItem in GetCopyItems(item, ""))
                yield return new(dirItem.Name, dirItem.SubPath, dirItem.Size, dirItem.DateTime);
        }

        IEnumerable<CopyItem> GetCopyItems(string directory, string subPath)
        {
            var dirInfo = new DirectoryInfo(context.CurrentPath.AppendPath(subPath).AppendPath(directory));
            foreach (var fileInfo in dirInfo.EnumerateFiles().OrderBy(n => n.Name))
                yield return new(fileInfo.Name, subPath.AppendPath(directory), fileInfo.Length, fileInfo.LastWriteTime);
            foreach (var info in dirInfo.EnumerateDirectories().OrderBy(n => n.Name))
            {
                foreach (var dirItem in GetCopyItems(info.Name, subPath.AppendPath(directory)))
                    yield return new(dirItem.Name, dirItem.SubPath, dirItem.Size, dirItem.DateTime);
            }
        }
    }
}