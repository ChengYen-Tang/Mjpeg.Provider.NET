using System;

namespace Mjpeg.Provider.NET
{
    public record BoundingBox(Color Color, float Thickness, PointF[] Points);
    internal record BoundingBoxInfo(SixLabors.ImageSharp.Color Color, float Thickness, SixLabors.ImageSharp.PointF[] Points);
    internal record MjpegParameter(Guid Id, int LongSize = default, bool DrawBoundingBox = false);
    internal record ImageRawData(byte[] RawData, int Length);

    public record PointF(float X, float Y);
    public record Color(byte R, byte G, byte B);
}
