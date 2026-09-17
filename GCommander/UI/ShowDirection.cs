using Gtk4DotNet;

public class ShowDirection : DrawingArea
{
    public ShowDirection(Builder builder, string name) : base(builder, name)
    {
        SetDrawFunction(Draw);

        Arsch();
        async void Arsch()
        {
            for (var i = 0; i < 200; i++)
            {
                await Task.Delay(20);
                x += 10;
                QueueDraw();
            }
        }
    }

    void OnDraw()
    {
        var cpc = ProgressContext.Instance.CopyProgress;
        if (cpc != null)
        {
            QueueDraw();
        }
    }

    void Draw(DrawingArea area, Cairo cairo, int w, int h)
    {
        cairo
            .SourceRgba(255, 0, 0, 1)
            .Rectangle(x, 0, 60, 3)
            .Fill();
    }

    int x = 0;
}