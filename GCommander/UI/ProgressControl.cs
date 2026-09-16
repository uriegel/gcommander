using CsTools.Extensions;
using Gtk4DotNet;

public class ProgressControl : Revealer
{
    public ProgressControl(Builder builder, string name) : base(builder, name)
    {
        DataContext = ProgressContext.Instance;

        this.Binding("reveal-child", nameof(ProgressContext.CopyProgress), BindingFlags.Default, p => p != null);

        //this.Binding("opacity", nameof(ProgressContext.DeleteAction), BindingFlags.Default, hide => (bool?)hide == true ? 0.0 : 1.0);
        titleLabel.Binding("label", nameof(ProgressContext.CopyProgress), BindingFlags.Default, cpc => ((CopyProgress?)cpc)?.Title);
        sizeLabel.Binding("label", nameof(ProgressContext.CopyProgress), BindingFlags.Default, cpc => $"({((CopyProgress?)cpc)?.TotalMaxBytes.ByteCountToString(2)})");
        currentNameLabel.Binding("label", nameof(ProgressContext.CopyProgress), BindingFlags.Default, cpc => ((CopyProgress?)cpc)?.Name);
        totalCountLabel.Binding("label", nameof(ProgressContext.CopyProgress), BindingFlags.Default, cpc => $"{((CopyProgress?)cpc)?.TotalCount}");
        currentCountLabel.Binding("label", nameof(ProgressContext.CopyProgress), BindingFlags.Default, cpc => $"{((CopyProgress?)cpc)?.CurrentCount}");
        durationLabel.Binding("label", nameof(ProgressContext.CopyProgress), BindingFlags.Default, cpc => $"{((CopyProgress?)cpc)?.Duration:hh\\:mm\\:ss}");
        estimatedDurationLabel.Binding("label", nameof(ProgressContext.CopyProgress), BindingFlags.Default, cpc => $"{ProgressContext.GetEstimatedDuration(cpc):hh\\:mm\\:ss}");
        progressbarTotal.Binding("fraction", nameof(ProgressContext.CopyProgress), BindingFlags.Default, ProgressContext.GetTotalFraction);
        progressbarCurrent.Binding("fraction", nameof(ProgressContext.CopyProgress), BindingFlags.Default, ProgressContext.GetFraction);
        // TODO cancelBtn.OnClicked += BackgroundJobs.Cancel;
        _ = progressSpinner;
    }

    public void ShowPopover()
    {
        if (ProgressContext.Instance.CopyProgress != null)
            progressControl.Popup();            
    }

    [Widget]
    Label titleLabel = null!;

    [Widget]
    Label sizeLabel = null!;

    [Widget]
    Label currentNameLabel = null!;

    [Widget]
    Label totalCountLabel = null!;

    [Widget]
    Label currentCountLabel = null!;

    [Widget]
    Label durationLabel = null!;

    [Widget]
    Label estimatedDurationLabel = null!;

    [Widget]
    ProgressBar progressbarTotal = null!;

    [Widget]
    ProgressBar progressbarCurrent = null!;

    [Widget]
    Button cancelBtn = null!;

    [Widget]
    MenuButton progressControl = null!;

    [Widget]
    ProgressSpinner progressSpinner = null!;
}

