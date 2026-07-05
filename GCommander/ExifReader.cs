using System.Globalization;
using System.Text;

record ExifData(
    DateTime? DateTime,
    double? Latitude,
    double? Longitude
);

class ExifReader : IDisposable
{
    public static ExifData? GetExifData(string path)
    {
        try
        {
            using var reader = new ExifReader(path);

            var latitude = (double?)null;
            var longitude = (double?)null;
            var dateTime = (DateTime?)null;

            if (reader.GetTagValue<double>(ExifTags.GPSLatitude, out var d))
                latitude = !double.IsNaN(d) ? d : null;
            if (reader.GetTagValue<double>(ExifTags.GPSLongitude, out d))
                longitude = !double.IsNaN(d) ? d : null;

            if (reader.GetTagValue<DateTime>(ExifTags.DateTimeOriginal, out var res))
                dateTime = res;
            else if (reader.GetTagValue(ExifTags.DateTime, out res))
                dateTime = res;
            return latitude == null && longitude == null && dateTime == null
                ? null
                : new ExifData(dateTime, latitude, longitude);
        }
        catch 
        {
            return null;
        }
    }

    public enum ExifTags : ushort
    {
        // IFD0 items
        ImageWidth = 0x100,
        ImageLength = 0x101,
        BitsPerSample = 0x102,
        Compression = 0x103,
        PhotometricInterpretation = 0x106,
        ImageDescription = 0x10E,
        Make = 0x10F,
        Model = 0x110,
        StripOffsets = 0x111,
        Orientation = 0x112,
        SamplesPerPixel = 0x115,
        RowsPerStrip = 0x116,
        StripByteCounts = 0x117,
        XResolution = 0x11A,
        YResolution = 0x11B,
        PlanarConfiguration = 0x11C,
        ResolutionUnit = 0x128,
        TransferFunction = 0x12D,
        Software = 0x131,
        DateTime = 0x132,
        Artist = 0x13B,
        WhitePoint = 0x13E,
        PrimaryChromaticities = 0x13F,
        JPEGInterchangeFormat = 0x201,
        JPEGInterchangeFormatLength = 0x202,
        YCbCrCoefficients = 0x211,
        YCbCrSubSampling = 0x212,
        YCbCrPositioning = 0x213,
        ReferenceBlackWhite = 0x214,
        Copyright = 0x8298,

        // SubIFD items
        ExposureTime = 0x829A,
        FNumber = 0x829D,
        ExposureProgram = 0x8822,
        SpectralSensitivity = 0x8824,
        ISOSpeedRatings = 0x8827,
        OECF = 0x8828,
        ExifVersion = 0x9000,
        DateTimeOriginal = 0x9003,
        DateTimeDigitized = 0x9004,
        ComponentsConfiguration = 0x9101,
        CompressedBitsPerPixel = 0x9102,
        ShutterSpeedValue = 0x9201,
        ApertureValue = 0x9202,
        BrightnessValue = 0x9203,
        ExposureBiasValue = 0x9204,
        MaxApertureValue = 0x9205,
        SubjectDistance = 0x9206,
        MeteringMode = 0x9207,
        LightSource = 0x9208,
        Flash = 0x9209,
        FocalLength = 0x920A,
        SubjectArea = 0x9214,
        MakerNote = 0x927C,
        UserComment = 0x9286,
        SubsecTime = 0x9290,
        SubsecTimeOriginal = 0x9291,
        SubsecTimeDigitized = 0x9292,
        FlashpixVersion = 0xA000,
        ColorSpace = 0xA001,
        PixelXDimension = 0xA002,
        PixelYDimension = 0xA003,
        RelatedSoundFile = 0xA004,
        FlashEnergy = 0xA20B,
        SpatialFrequencyResponse = 0xA20C,
        FocalPlaneXResolution = 0xA20E,
        FocalPlaneYResolution = 0xA20F,
        FocalPlaneResolutionUnit = 0xA210,
        SubjectLocation = 0xA214,
        ExposureIndex = 0xA215,
        SensingMethod = 0xA217,
        FileSource = 0xA300,
        SceneType = 0xA301,
        CFAPattern = 0xA302,
        CustomRendered = 0xA401,
        ExposureMode = 0xA402,
        WhiteBalance = 0xA403,
        DigitalZoomRatio = 0xA404,
        FocalLengthIn35mmFilm = 0xA405,
        SceneCaptureType = 0xA406,
        GainControl = 0xA407,
        Contrast = 0xA408,
        Saturation = 0xA409,
        Sharpness = 0xA40A,
        DeviceSettingDescription = 0xA40B,
        SubjectDistanceRange = 0xA40C,
        ImageUniqueID = 0xA420,

        // GPS subifd items
        GPSVersionID = 0x0,
        GPSLatitudeRef = 0x1,
        GPSLatitude = 0x2,
        GPSLongitudeRef = 0x3,
        GPSLongitude = 0x4,
        GPSAltitudeRef = 0x5,
        GPSAltitude = 0x6,
        GPSTimeStamp = 0x7,
        GPSSatellites = 0x8,
        GPSStatus = 0x9,
        GPSMeasureMode = 0xA,
        GPSDOP = 0xB,
        GPSSpeedRef = 0xC,
        GPSSpeed = 0xD,
        GPSTrackRef = 0xE,
        GPSTrack = 0xF,
        GPSImgDirectionRef = 0x10,
        GPSImgDirection = 0x11,
        GPSMapDatum = 0x12,
        GPSDestLatitudeRef = 0x13,
        GPSDestLatitude = 0x14,
        GPSDestLongitudeRef = 0x15,
        GPSDestLongitude = 0x16,
        GPSDestBearingRef = 0x17,
        GPSDestBearing = 0x18,
        GPSDestDistanceRef = 0x19,
        GPSDestDistance = 0x1A,
        GPSProcessingMethod = 0x1B,
        GPSAreaInformation = 0x1C,
        GPSDateStamp = 0x1D,
        GPSDifferential = 0x1E
    }

    public ExifReader(string fileName)
    {
        isLittleEndian = false;
        try
        {
            fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            reader = new BinaryReader(fileStream);

            if (!Initialize())
                Dispose();
        }
        catch
        {
            Dispose();
        }
    }

    public bool GetTagValue<T>(ExifTags tag, out T result)
        where T: struct
        => GetTagValue((ushort)tag, out result);

    public bool GetTagValue<T>(ushort tagID, out T result)
        where T: struct
    {
        var tagData = GetTagBytes(tagID, out ushort tiffDataType, out uint numberOfComponents);
        if (tagData == null)
        {
            result = default;
            return false;
        }

        byte fieldLength = GetTIFFFieldLength(tiffDataType);

        switch (tiffDataType)
        {
            case 1:
                // unsigned byte
                if (numberOfComponents == 1)
                    result = (T)(object)tagData[0];
                else
                    result = (T)(object)tagData;
                return true;
            case 2:
                // ascii string
                string str = Encoding.ASCII.GetString(tagData);

                int nullCharIndex = str.IndexOf('\0');
                if (nullCharIndex != -1)
                    str = str[..nullCharIndex];

                if (typeof(T) == typeof(DateTime))
                {
                    result =
                        (T)(object)DateTime.ParseExact(str, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
                    return true;
                }

                result = (T)(object)str;
                return true;
            case 3:
                if (numberOfComponents == 1)
                    result = (T)(object)ToUShort(tagData);
                else
                    result = (T)(object)GetArray(tagData, fieldLength, ToUShort);
                return true;
            case 4:
                if (numberOfComponents == 1)
                    result = (T)(object)ToUint(tagData);
                else
                    result = (T)(object)GetArray(tagData, fieldLength, ToUint);
                return true;
            case 5:
                if (numberOfComponents == 1)
                    result = (T)(object)ToURational(tagData);
                else
                    result = (T)(object)GetDoubleFromArray(tagData, fieldLength, ToURational);
                return true;
            case 6:
                if (numberOfComponents == 1)
                    result = (T)(object)ToSByte(tagData);
                else
                    result = (T)(object)GetArray(tagData, fieldLength, ToSByte);
                return true;
            case 7:
                if (numberOfComponents == 1)
                    result = (T)(object)ToUint(tagData);
                else
                    result = (T)(object)GetArray(tagData, fieldLength, ToUint);
                return true;
            case 8:
                if (numberOfComponents == 1)
                    result = (T)(object)ToShort(tagData);
                else
                    result = (T)(object)GetArray(tagData, fieldLength, ToShort);
                return true;
            case 9:
                if (numberOfComponents == 1)
                    result = (T)(object)ToInt(tagData);
                else
                    result = (T)(object)GetArray(tagData, fieldLength, ToInt);
                return true;
            case 10:
                if (numberOfComponents == 1)
                    result = (T)(object)ToRational(tagData);
                else
                    result = (T)(object)GetArray(tagData, fieldLength, ToRational);
                return true;
            case 11:
                if (numberOfComponents == 1)
                    result = (T)(object)ToSingle(tagData);
                else
                    result = (T)(object)GetArray(tagData, fieldLength, ToSingle);
                return true;
            case 12:
                if (numberOfComponents == 1)
                    result = (T)(object)ToDouble(tagData);
                else
                    result = (T)(object)GetArray(tagData, fieldLength, ToDouble);
                return true;
            default:
                result = default;
                return false;
        }
    }

    byte[]? GetTagBytes(ushort tagID, out ushort tiffDataType, out uint numberOfComponents)
    {
        if (fileStream == null || reader == null || catalogue == null || !catalogue.ContainsKey(tagID))
        {
            tiffDataType = 0;
            numberOfComponents = 0;
            return null;
        }

        long tagOffset = catalogue[tagID];
        fileStream.Position = tagOffset;
        ushort currentTagID = ReadUShort();
        if (currentTagID != tagID)
            throw new Exception("Tag number not at expected offset");

        tiffDataType = ReadUShort();
        numberOfComponents = ReadUint();
        byte[] tagData = ReadBytes(4);

        var dataSize = (int)(numberOfComponents * GetTIFFFieldLength(tiffDataType));
        if (dataSize > 4)
        {
            ushort offsetAddress = ToUShort(tagData);
            return ReadBytes(offsetAddress, dataSize);
        }

        Array.Resize(ref tagData, dataSize);
        return tagData;
    }

    void CatalogueIFD()
    {
        catalogue ??= [];
        ushort entryCount = ReadUShort();

        if (fileStream != null && reader != null)
            for (ushort currentEntry = 0; currentEntry < entryCount; currentEntry++)
            {
                ushort currentTagNumber = ReadUShort();
                catalogue[currentTagNumber] = fileStream.Position - 2;
                reader.BaseStream.Seek(10, SeekOrigin.Current);
            }
    }

    bool Initialize()
    {
        try
        {
            return !(ReadUShort() != 0xFFD8 || !ReadToExifStart() || !CreateTagIndex());
        }
        catch (Exception)
        {
            return false;
        }
    }

    static byte GetTIFFFieldLength(ushort tiffDataType)
        => tiffDataType switch
            {
                1 or 2 or 6 => 1,
                3 or 8 => 2,
                4 or 7 or 9 or 11 => 4,
                5 or 10 or 12 => 8,
                _ => throw new Exception(string.Format("Unknown TIFF datatype: {0}", tiffDataType)),
            };

    ushort ReadUShort() => ToUShort(ReadBytes(2));
    uint ReadUint() => ToUint(ReadBytes(4));
    string ReadString(int chars) => Encoding.ASCII.GetString(ReadBytes(chars));
    byte[] ReadBytes(int byteCount) => reader?.ReadBytes(byteCount) ?? [];

    byte[] ReadBytes(ushort tiffOffset, int byteCount)
    {
        long originalOffset = fileStream?.Position ?? 0;
        fileStream?.Seek(tiffOffset + tiffHeaderStart, SeekOrigin.Begin);
        byte[] data = reader?.ReadBytes(byteCount) ?? [];
        if (fileStream != null)
            fileStream.Position = originalOffset;
        return data;
    }

    ushort ToUShort(byte[] data)
    {
        if (isLittleEndian != BitConverter.IsLittleEndian)
            Array.Reverse(data);
        if (data.Length == 0)
            return 0;
        return BitConverter.ToUInt16(data, 0);
    }

    double ToURational(byte[] data)
    {
        var numeratorData = new byte[4];
        var denominatorData = new byte[4];
        Array.Copy(data, numeratorData, 4);
        Array.Copy(data, 4, denominatorData, 0, 4);
        uint numerator = ToUint(numeratorData);
        uint denominator = ToUint(denominatorData);
        return numerator / (double)denominator;
    }

    double ToRational(byte[] data)
    {
        var numeratorData = new byte[4];
        var denominatorData = new byte[4];
        Array.Copy(data, numeratorData, 4);
        Array.Copy(data, 4, denominatorData, 0, 4);
        int numerator = ToInt(numeratorData);
        int denominator = ToInt(denominatorData);
        return numerator / (double)denominator;
    }

    uint ToUint(byte[] data)
    {
        if (isLittleEndian != BitConverter.IsLittleEndian)
            Array.Reverse(data);
        return BitConverter.ToUInt32(data, 0);
    }

    int ToInt(byte[] data)
    {
        if (isLittleEndian != BitConverter.IsLittleEndian)
            Array.Reverse(data);
        return BitConverter.ToInt32(data, 0);
    }

    double ToDouble(byte[] data)
    {
        if (isLittleEndian != BitConverter.IsLittleEndian)
            Array.Reverse(data);
        return BitConverter.ToDouble(data, 0);
    }

    float ToSingle(byte[] data)
    {
        if (isLittleEndian != BitConverter.IsLittleEndian)
            Array.Reverse(data);
        return BitConverter.ToSingle(data, 0);
    }

    short ToShort(byte[] data)
    {
        if (isLittleEndian != BitConverter.IsLittleEndian)
            Array.Reverse(data);
        return BitConverter.ToInt16(data, 0);
    }

    sbyte ToSByte(byte[] data) => (sbyte)(data[0] - byte.MaxValue);

    static Array GetArray<T>(byte[] data, int elementLengthBytes, ConverterMethod<T> converter)
    {
        Array convertedData = Array.CreateInstance(typeof(T), data.Length / elementLengthBytes);
        var buffer = new byte[elementLengthBytes];
        for (int elementCount = 0; elementCount < data.Length / elementLengthBytes; elementCount++)
        {
            Array.Copy(data, elementCount * elementLengthBytes, buffer, 0, elementLengthBytes);
            convertedData.SetValue(converter(buffer), elementCount);
        }
        return convertedData;
    }

    static double GetDoubleFromArray<T>(byte[] data, int elementLengthBytes, ConverterMethod<T> converter)
    {
        var convertedData = Array.CreateInstance(typeof(T), data.Length / elementLengthBytes);
        var buffer = new byte[elementLengthBytes];
        for (int elementCount = 0; elementCount < data.Length / elementLengthBytes; elementCount++)
        {
            Array.Copy(data, elementCount * elementLengthBytes, buffer, 0, elementLengthBytes);
            convertedData.SetValue(converter(buffer), elementCount);
        }
        return convertedData is double[] da
            ? da[0] + da[1]/60 + da[2]/3600
            : 0;
    }

    delegate T ConverterMethod<out T>(byte[] data);

    bool ReadToExifStart()
    {
        byte markerStart = 0;
        byte markerNumber = 0;
        while (reader != null && ((markerStart = reader.ReadByte()) == 0xFF) && (markerNumber = reader.ReadByte()) != 0xE1)
        {
            ushort dataLength = ReadUShort();
            reader.BaseStream.Seek(dataLength - 2, SeekOrigin.Current);
        }
        return (markerStart != 0xFF || markerNumber != 0xE1) == false;
    }

    bool CreateTagIndex()
    {
        ReadUShort();
        if (ReadString(4) != "Exif" || ReadUShort() != 0 || reader == null || fileStream == null)
            return false;

        tiffHeaderStart = reader.BaseStream.Position;
        isLittleEndian = ReadString(2) == "II";
        if (ReadUShort() != 0x002A)
            return false;
        uint ifdOffset = ReadUint();
        fileStream.Position = ifdOffset + tiffHeaderStart;
        CatalogueIFD();
        if (!GetTagValue(0x8769, out uint offset))
            return false;
        fileStream.Position = offset + tiffHeaderStart;
        CatalogueIFD();
        if (GetTagValue(0x8825, out offset))
        {
            fileStream.Position = offset + tiffHeaderStart;
            CatalogueIFD();
        }
        return true;
    }

    readonly FileStream? fileStream;
    readonly BinaryReader? reader;
    Dictionary<ushort, long>? catalogue;
    bool isLittleEndian;
    long tiffHeaderStart;

    #region IDisposable Members

    public void Dispose()
    {
        // Make sure the file handle is released
        reader?.Close();
        fileStream?.Close();
    }

    #endregion
}