using Gtk4DotNet;

class MediaPlayer : Overlay
{
    public string? FileName
    {
        set
        {
            if (value != null)
            {
                Visible = true;
                using var discoverer = new Discoverer(TimeSpan.FromSeconds(10));
                using var info = discoverer.DiscoverUri($"file://{value}");
                using var videos = info!.GetVideoStreams();
                var videoInfo = videos.FirstOrDefault();
                var dar = videoInfo?.DisplayAspectRatio ?? 1;
                videoContainer.AspectRatio = dar;
                mediaFile?.Dispose();
                mediaFile = MediaFile.New(value);
                var asp = (mediaFile as IPaintable).IntrinsicAspectRatio;
                var w = (mediaFile as IPaintable).IntrinsicWidth;
                var h = (mediaFile as IPaintable).IntrinsicHeight;
                mediaControls.SetMediaStream(mediaFile);
                video.SetPaintable(mediaFile);
                (mediaFile as IMediaStream).IsPlaying = true;

                OnFinalize(() => mediaFile?.Dispose());
            }
            else
            {
                mediaFile?.Dispose();
                mediaFile = null;
                video.SetPaintable(null);
                mediaControls.SetMediaStream(null);
                Visible = false;
            }
        }
    }

    public MediaPlayer(Builder builder, string name)
        : base(builder, name)
    {

    }
    
    [Widget]
    readonly Picture video = null!;

    [Widget]
    readonly MediaControls mediaControls = null!;

    [Widget]
    readonly AspectContainer videoContainer = null!;

    MediaFile? mediaFile;
}
