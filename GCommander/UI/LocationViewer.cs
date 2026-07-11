using System.Text;
using CsTools.Extensions;
using Gtk4DotNet;

class LocationViewer : WebView
{
    public LocationViewer(Builder builder, string name)
        : base(builder, name)
    {
        WebKitWebContext.GetDefault().RegisterUriScheme("res", OnResRequest);
        OnFinalize(WebKitWebContext.DisposeUriSchemes);
        LoadUri("res://location/index.html");

        MainContext.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainContext.ExifData))
            {
                if (MainContext.Instance.ExifData?.Latitude.HasValue == true 
                    && MainContext.Instance.ExifData?.Longitude.HasValue == true)
                RunJavascript(FormattableString.Invariant(
                    $"setLocation({MainContext.Instance.ExifData?.Latitude.Value}, {MainContext.Instance.ExifData?.Longitude.Value})"));
            }
        };
    }

    static void OnResRequest(WebkitUriSchemeRequest request)
    {
        try
        {
            var uri = request.GetUri()[6..].SubstringUntil('?');
            uri = uri.Length > 0 ? uri : "index.html";
            var res = Resources.Get(uri);
            if (res != null)
            {
                var bytes = new byte[res.Length];
                var read = res.Read(bytes, 0, bytes.Length);
                using var gbytes = GBytes.New(bytes);
                using var gstream = MemoryInputStream.New(gbytes);
                request.Finish(gstream, bytes.Length, uri?.GetFileExtension()?.ToMimeType() ?? "text/html");
            }
            else
                SendNotFound(request);
        }
        catch
        {
            SendNotFound(request);
        }
    }

    static void SendNotFound(WebkitUriSchemeRequest request)
        => SendResponse(request, 404, "Not Found", "I can't find what you're looking for!");

    static void SendResponse(WebkitUriSchemeRequest request, int code, string status, string text)
    {
        using var bytes = GBytes.New(Encoding.UTF8.GetBytes(text));
        using var stream = MemoryInputStream.New(bytes);
        using var response = WebKitUriSchemeResponse.New(stream, text.Length);
        using var respondHeaders = SoupMessageHeaders.New(SoupMessageHeaderType.Response);
        respondHeaders.Set([new("Access-Control-Allow-Origin", "*")]);
        response.HttpHeaders(respondHeaders);
        response.Status(code, status);
        request.Finish(response);
    }
}

