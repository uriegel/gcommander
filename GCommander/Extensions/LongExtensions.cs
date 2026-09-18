namespace Extensions;

static class LongExtensions
{
    public static string FormatSize(this long? size)
    {
        if (!size.HasValue || size == -1)
            return "";
        return size.Value.FormatSize();
    }

    public static string FormatSize(this long size)
    {
        var sizeStr = size.ToString();
        var sep = '.';
        if (sizeStr.Length > 3)
        {
            var sizePart = sizeStr;
            sizeStr = "";
            for (var j = 3; j < sizePart.Length; j += 3)
            {
                var extract = sizePart.Substring(sizePart.Length - j, 3);
                sizeStr = sep + extract + sizeStr;
            }
            var strfirst = sizePart[..((sizePart.Length % 3 == 0) ? 3 : (sizePart.Length % 3))];
            sizeStr = strfirst + sizeStr;
        }
        return sizeStr;
    }
}