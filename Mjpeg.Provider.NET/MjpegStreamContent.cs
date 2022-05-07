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

                    await outputStream.WriteAsync(header, cancellationToken);
                    await outputStream.WriteAsync(imageDataLength, cancellationToken);
                    await outputStream.WriteAsync(newLine, cancellationToken);
                    await outputStream.WriteAsync(imageData.RawData, cancellationToken);
                    
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
