using System.Xml;
using System.Xml.Serialization;
using CsTools.Functional;

record GpxTrack(
    string? Name,
    string? Description,
    float Distance,
    int Duration,
    float AverageSpeed,
    int AverageHeartRate,
    GpxPoint[]? TrackPoints,
    string Date
);

record GpxPoint(
    double Latitude,
    double Longitude,
    double Elevation,
    string? Time,
    int Heartrate,
    float Velocity
);


[XmlRoot(ElementName = "gpx")]
public class XmlTrackInfo
{
    [XmlElement("trk")]
    public XmlTrack? Track;
}

public class Info
{
    [XmlElement("date")]
    public string? Date;
    [XmlElement("distance")]
    public float Distance;
    [XmlElement("duration")]
    public int Duration;
    [XmlElement("averageSpeed")]
    public float AverageSpeed;
}

public class XmlTrack
{
    [XmlElement("name")]
    public string? Name;
    
    [XmlElement("desc")]
    public string? Description;

    [XmlElement("info")]
    public Info? Info;

    [XmlElement("trkseg")]
    public XmlTrackSegment? TrackSegment;
}

public class XmlTrackSegment
{
    [XmlElement("trkpt")]
    public XmlTrackPoint[]? TrackPoints;
}

public class XmlTrackPoint
{
    [XmlAttribute("lat")]
    public double Latitude;
    [XmlAttribute("lon")]
    public double Longitude;

    [XmlElement("ele")]
    public double Elevation;

    [XmlElement("time")]
    public string? Time;

    [XmlElement("speed")]
    public float? Speed;

    [XmlElement("heartrate")]
    public int? HeartRate;
}

public class IgnoreNamespaceXmlTextReader : XmlTextReader
{
    public IgnoreNamespaceXmlTextReader(TextReader reader) : base(reader) { }

    public override string NamespaceURI => "";
}

static class TrackInfo
{
    public static GpxTrack Get(string trkPath)
    {
        var serializer = new XmlSerializer(typeof(XmlTrackInfo));
        using var stream = File.OpenRead(trkPath);
        using var reader = new IgnoreNamespaceXmlTextReader(new StreamReader(stream));
        var xmlTrackInfo = serializer.Deserialize(reader) as XmlTrackInfo;
        var old = xmlTrackInfo?.Track?.Info?.Date != null && DateTime.Parse(xmlTrackInfo?.Track?.Info?.Date!) < new DateTime(2021, 1, 1);
        var trackInfo = new GpxTrack(
            xmlTrackInfo?.Track?.Name,
            xmlTrackInfo?.Track?.Description,
            xmlTrackInfo?.Track?.Info?.Distance ?? 0,
            xmlTrackInfo?.Track?.Info?.Duration ?? 0,
            xmlTrackInfo?.Track?.Info?.AverageSpeed ?? 0,
            (int)(xmlTrackInfo
                ?.Track
                ?.TrackSegment
                ?.TrackPoints
                ?.Select(n => n.HeartRate)
                ?.Average() ?? 0),
            xmlTrackInfo
                ?.Track
                ?.TrackSegment
                ?.TrackPoints
                ?.Select(n => new GpxPoint(n.Latitude, n.Longitude, n.Elevation, n.Time, n.HeartRate ?? 0, old ? n.Speed ?? 0 : (n.Speed ?? 0) * 3.6f))
                    .ToArray(),
            xmlTrackInfo?.Track?.Info?.Date ?? "");
        return trackInfo;
    }
}

