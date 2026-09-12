using Gtk4DotNet;

class DateExif : Box
{
    public void SetDateTimeBinding()
    {
        datetime.SetBinding("label", nameof(FileItem.DateTime), BindingFlags.Default,
            v => (DateTime?)v != null ? ((DateTime)v).ToString("g") : "");
        datetime.SetBinding("visible", nameof(FileItem.ExifData), BindingFlags.Default,
            v => (v as ExifData) == null || (v as ExifData)?.DateTime == DateTime.MinValue);
    }

    public void SetExifBinding()
    {
        exif.SetBinding("label", nameof(FileItem.ExifData), BindingFlags.Default,
            v => DirectoryController.GetExifDate((ExifData?)v, ""));
        exif?.SetBindingToCss("exif", nameof(FileItem.ExifData), v => (v as ExifData) != null && (v as ExifData)?.DateTime != DateTime.MinValue);
    }

    public void UnsetDateTimeBinding()
    {
        datetime.UnsetBinding("label");
        datetime.UnsetBinding("visible");
    }

    public void UnsetExifBinding()
    {
        exif.UnsetBinding("label");
        exif?.UnsetBindingToCss("exif");
    }

    public DateExif() : base() { }
    public DateExif(Builder builder) : base(builder, "listitem") { }

    [Widget]
    readonly Label datetime = null!;

    [Widget]
    readonly Label exif = null!;
}

