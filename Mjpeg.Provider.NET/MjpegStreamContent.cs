using Microsoft.AspNetCore.Mvc;
using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mjpeg.Provider.NET
{
    internal class MjpegStreamContent : IActionResult
    {
        private static readonly string boundary = "frame";
        private static readonly string contentType = "multipart/x-mixed-replace;boundary=" + boundary;
        private static readonly byte[] header = Encoding.UTF8.GetBytes($"\r\n--{boundary}\r\nContent-Type:image/jpeg\r\nContent-Length:");
        private static readonly byte[] newLine = Encoding.UTF8.GetBytes("\r\n\r\n");

        private readonly Action<MjpegParameter, MjpegStreamContent> closeAction;
        private readonly Func<CancellationToken, Task<ImageRawData>> onNextImage;
        private readonly MjpegParameter parameter;

        public MjpegStreamContent(Action<MjpegParameter, MjpegStreamContent> closeAction, MjpegParameter parameter, Func<CancellationToken, Task<ImageRawData>> onNextImage)
            => (this.closeAction, this.parameter, this.onNextImage) = (closeAction, parameter, onNextImage);

        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.ContentType = contentType;

            var outputStream = context.HttpContext.Response.Body;
            var cancellationToken = context.HttpContext.RequestAborted;

            try
            {
                while (true)
                {
                    ImageRawData imageData = await onNextImage(cancellationToken);
                    byte[] imageDataLength = Encoding.UTF8.GetBytes(imageData.Length.ToString());
                    int totalBufferLength = header.Length + imageDataLength.Length + newLine.Length;
                    var buffer = Combine(header, imageDataLength, newLine, totalBufferLength);
                    await outputStream.WriteAsync(buffer.data, cancellationToken);
                    await outputStream.WriteAsync(imageData.Data, cancellationToken);
                    ArrayPool<byte>.Shared.Return(buffer.arrayPoolSpace);
                    imageData.SetNotUse();
                    if (cancellationToken.IsCancellationRequested)
                        break;
                }
            }
            catch { }
            finally
            {
                closeAction?.Invoke(parameter, this);
            }
        }

        // 合併3個 byte array 成一個 byte array
        private static (Memory<byte> data, byte[] arrayPoolSpace) Combine(byte[] a, byte[] b, byte[] c, int totalLength)
        {
            byte[] ret = ArrayPool<byte>.Shared.Rent(totalLength);
            Buffer.BlockCopy(a, 0, ret, 0, a.Length);
            Buffer.BlockCopy(b, 0, ret, a.Length, b.Length);
            Buffer.BlockCopy(c, 0, ret, a.Length + b.Length, c.Length);
            return (ret.AsMemory(0, totalLength), ret);
        }
    }
}
