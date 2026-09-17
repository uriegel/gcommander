using Gtk4DotNet;

public class ShowDirection : DrawingArea
{
    public bool RightToLeft { get; set; }
    public ShowDirection() : base() => Initialize();

    public ShowDirection(Builder builder, string name) : base(builder, name) => Initialize();

    void Initialize()
    {
        SetSizeRequest(-1, 3);
        AddCssClass("custom-accent");
        SetDrawFunction(Draw);
        OnUnrealize += async () => cancellation.Cancel();
        OnRealize += async () =>
        {
            var step = (Width + indicatorLength) / (duration / delay);
            while (true)
                try
                {
                    await Task.Delay(delay, cancellation.Token);
                    pos += step;
                    if (pos > Width)
                        pos = -indicatorLength;
                    QueueDraw();
                }
                catch
                {
                    break;
                }
        };
    }

    void Draw(DrawingArea area, Cairo cairo, int w, int h)
    {
        var color = GetStyleContext().GetColor().ToSrgb();
        cairo.SourceRgba(color.Red, color.Green, color.Blue, 1);
        if (!RightToLeft)
            cairo.Rectangle(pos, 0, indicatorLength, h);
        else
            cairo.Rectangle(w - indicatorLength - pos, 0, indicatorLength, h);
        cairo.Fill();
    }

    readonly CancellationTokenSource cancellation = new();
    const int indicatorLength = 60;
    const int delay = 10;
    const int duration = 2000;
    double pos = - indicatorLength;
}