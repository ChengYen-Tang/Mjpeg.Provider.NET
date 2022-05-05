using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mjpeg.Provider.NET
{
    internal class MjpegStreamContent : IActionResult
    {
        private static readonly string boundary = "frame";
        private static readonly string contentType = "multipart/x-mixed-replace;boundary=" + boundary;
        private static readonly byte[] newLine = Encoding.UTF8.GetBytes("\r\n");

        private readonly Action<MJPEGParameter, MjpegStreamContent> closeAction;
        private readonly Func<CancellationToken, Task<ImageRawData>> onNextImage;
        private readonly MJPEGParameter parameter;

        public MjpegStreamContent(Action<MJPEGParameter, MjpegStreamContent> closeAction, MJPEGParameter parameter, Func<CancellationToken, Task<ImageRawData>> onNextImage)
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

                    string header = $"--{boundary}\r\nContent-Type:image/jpeg\r\nContent-Length:{imageData.Length}\r\n\r\n";
                    byte[] headerData = Encoding.UTF8.GetBytes(header);
                    await outputStream.WriteAsync(headerData, 0, headerData.Length, cancellationToken);
                    await outputStream.WriteAsync(imageData.RawData, 0, imageData.Length, cancellationToken);
                    await outputStream.WriteAsync(newLine, 0, newLine.Length, cancellationToken);

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
    }
}
