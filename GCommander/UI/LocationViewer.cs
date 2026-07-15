using System.Text;
using CsTools.Extensions;
using Gtk4DotNet;

class LocationViewer : Box
{
    public LocationViewer(Builder builder, string name)
        : base(builder, name)
    {
        locationView.LoadUri("res://location/index.html");

        this["visible"].OnNotify += () =>
        {
            if (Visible
                && MainContext.Instance.ExifData?.Latitude.HasValue == true
                && MainContext.Instance.ExifData?.Longitude.HasValue == true)
            {
                locationView.Visible = true;
                locationView.RunJavascript(FormattableString.Invariant(
                    $"setLocation({MainContext.Instance.ExifData?.Latitude.Value}, {MainContext.Instance.ExifData?.Longitude.Value})"));
            }
            else
                locationView.Visible = false;
        };

        MainContext.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainContext.ExifData))
            {
                if (Visible && MainContext.Instance.ExifData?.Latitude.HasValue == true
                    && MainContext.Instance.ExifData?.Longitude.HasValue == true)
                {
                    locationView.Visible = true;
                    locationView.RunJavascript(FormattableString.Invariant(
                        $"setLocation({MainContext.Instance.ExifData?.Latitude.Value}, {MainContext.Instance.ExifData?.Longitude.Value})"));
                }
                else
                    locationView.Visible = false;
            }
        };
    }

    [Widget]
    readonly WebView locationView = null!;
}

