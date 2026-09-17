using Gtk4DotNet;

public class ShowDirection : DrawingArea
{
    public ShowDirection(Builder builder, string name) : base(builder, name)
    {
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
        cairo
            .SourceRgba(255, 0, 0, 1)
            .Rectangle(pos, 0, indicatorLength, 3)
            .Fill();
    }

    readonly CancellationTokenSource cancellation = new();
    const int indicatorLength = 60;
    const int delay = 10;
    const int duration = 2000;
    double pos = - indicatorLength;
}