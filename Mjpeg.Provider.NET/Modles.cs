using Microsoft.IO;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mjpeg.Provider.NET
{
    public record BoundingBox(Color Color, float Thickness, PointF[] Points);
    internal record BoundingBoxInfo(SixLabors.ImageSharp.Color Color, float Thickness, SixLabors.ImageSharp.PointF[] Points);
    internal record MjpegParameter(Guid Id, int LongSize = default, bool DrawBoundingBox = false);
    internal class ImageRawData : IDisposable
    {
        public int Length { get; init; }
        public Memory<byte> Data { get; init; }

        public readonly MemoryStream memoryStream;
        private volatile bool disposedValue;
        private volatile int usedCount;

        public ImageRawData(MemoryStream memoryStream)
        {
            int length = Convert.ToInt32(memoryStream.Length);
            this.memoryStream = memoryStream;
            Length = length;
            Data = new(memoryStream.GetBuffer(), 0, length);

            usedCount = 0;
        }

        public ImageRawData(RecyclableMemoryStream memoryStream)
        {
            int length = Convert.ToInt32(memoryStream.Length);
            this.memoryStream = memoryStream;
            Length = length;
            Data = memoryStream.GetMemory(length);

            usedCount = 0;
        }

        public void SetUsed()
        {
            if (disposedValue)
                throw new InvalidOperationException("Object disposed");
            else
                Interlocked.Increment(ref usedCount);
        }

        public void SetNotUse()
            => Interlocked.Decrement(ref usedCount);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Task.Run(() => {
                        SpinWait.SpinUntil(() => usedCount == 0);
                        // TODO: 處置受控狀態 (受控物件)
                        memoryStream.Dispose();
                    });
                }

                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~ImageRawData()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }


    public record PointF(float X, float Y);
    public record Color(byte R, byte G, byte B);
}
