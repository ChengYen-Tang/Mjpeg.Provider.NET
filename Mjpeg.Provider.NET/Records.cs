using System;

namespace Mjpeg.Provider.NET
{
    internal record MJPEGParameter(Guid Id, int LongSize = default, bool DrawBoundingBox = false);
    internal record ImageRawData(byte[] RawData, int Length);
}
