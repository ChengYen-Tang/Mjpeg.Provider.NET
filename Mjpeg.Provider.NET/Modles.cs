using Microsoft.IO;
using System;
using System.IO;
using System.Threading;

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
        private readonly SpinLock spinLock;
        private bool disposedValue;
        private volatile int usedCount;
        private volatile bool isCalledDispose;

        public ImageRawData(MemoryStream memoryStream)
        {
            int length = Convert.ToInt32(memoryStream.Length);
            this.memoryStream = memoryStream;
            Length = length;
            Data = new(memoryStream.GetBuffer(), 0, length);

            spinLock = new SpinLock(false);
            usedCount = 0;
            isCalledDispose = false;
        }

        public ImageRawData(RecyclableMemoryStream memoryStream)
        {
            int length = Convert.ToInt32(memoryStream.Length);
            this.memoryStream = memoryStream;
            Length = length;
            Data = memoryStream.GetMemory(length);

            spinLock = new SpinLock(false);
            usedCount = 0;
            isCalledDispose = false;
        }

        public void SetUsed()
        {
            bool lockTacker = false;
            spinLock.Enter(ref lockTacker);
            if (isCalledDispose)
                throw new InvalidOperationException("Object disposed");
            else
                usedCount++;
            if (lockTacker)
                spinLock.Exit();
        }

        public void SetNotUse()
        {
            bool lockTacker = false;
            spinLock.Enter(ref lockTacker);
            if (usedCount > 0)
            {
                usedCount--;
                if (usedCount == 0)
                    Dispose();
            }
            if (lockTacker)
                spinLock.Exit();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                    memoryStream.Dispose();
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
            bool lockTacker = false;
            spinLock.Enter(ref lockTacker);
            if (usedCount <= 0)
            {
                // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
            else
                isCalledDispose = true;
            if (lockTacker)
                spinLock.Exit();
        }
    }


    public record PointF(float X, float Y);
    public record Color(byte R, byte G, byte B);
}
