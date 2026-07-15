using System.Text;
using System.Text.Json;
using CsTools.Extensions;
using Gtk4DotNet;

class TrackViewer : WebView
{
    public string? GpxTrack
    {
        get;
        set
        {
            field = value;
            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                    var gpx = TrackInfo.Get(value);
                    if (!initial)
                        RunJavascript($"setTrack({JsonSerializer.Serialize(gpx, Json.Defaults)})");
                    else
                    {
                        Gtk.BeginInvoke(200, async () =>
                        {
                            await Task.Delay(50);
                            RunJavascript($"setTrack({JsonSerializer.Serialize(gpx, Json.Defaults)})");
                        });
                        initial = false;
                    } 
                        
                }
                catch
                {
                    Visible = false;    
                }
            }
            else
                Visible = false;
        }
    }
    public TrackViewer(Builder builder, string name)
        : base(builder, name)
    {
        GetSettings().EnableDeveloperExtras = true;
        LoadUri("res://track/index.html");
    }

    bool initial = true;
}