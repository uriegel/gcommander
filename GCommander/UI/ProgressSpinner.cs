using Gtk4DotNet;

public class ProgressSpinner : DrawingArea
{
    public ProgressSpinner(Builder builder, string name) : base(builder, name)
    {
        AddCssClass("custom-accent");
        SetDrawFunction(Draw);
        ProgressContext.Instance.PropertyChanged += (s, e) => OnDraw();
    }

    void OnDraw()
    {
        var cpc = ProgressContext.Instance.CopyProgress;
        if (cpc != null)
        {
            progress = (cpc.TotalBytes + cpc.CurrentBytes) / (float)cpc.TotalMaxBytes;
            QueueDraw();
        }
    }

    void Draw(DrawingArea area, Cairo cairo, int w, int h)
    {
        var color = ProgressContext.Instance.CopyProgress?.IsRunning == true
            ? GetStyleContext().GetColor().ToSrgb()
            : new GtkRgba() { Red = 0, Green = 0, Blue = 0, Alpha = 0 };
        cairo
            .AntiAlias(CairoAntialias.Best)
            .LineCap(LineCap.Round)
            .LineWidth(3.0)
            .SourceRgba(color.Red, color.Green, color.Blue, 0.2)
            .Arc(w / 2.0, h / 2.0, (w < h ? w : h) / 2.0 - 2.0, -Math.PI / 2.0, -Math.PI / 2.0 + Math.PI * 2)
            .Stroke()
            .AntiAlias(CairoAntialias.Best)
            .LineCap(LineCap.Round)
            .LineWidth(3.0)
            .SourceRgba(color.Red, color.Green, color.Blue, color.Alpha)
            .Arc(w / 2.0, h / 2.0, (w < h ? w : h) / 2.0 - 2.0, -Math.PI / 2.0, -Math.PI / 2.0 + progress * Math.PI * 2)
            .Stroke();
    }

    float progress;
}
