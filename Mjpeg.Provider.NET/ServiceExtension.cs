using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Mjpeg.Provider.NET
{
    public static class ServiceExtension
    {
        /// <summary>
        /// 注入 MJPEGProvider
        /// 整個程式執行的生命週期中止會產生一個 Instance
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="pixelFormat"></param>
        /// <param name="noSignalPicPath"></param>
        /// <returns></returns>
        public static IServiceCollection MjpegProvider(
            [NotNull] this IServiceCollection serviceCollection, PixelFormat pixelFormat, string noSignalPicPath)
        {
            serviceCollection.AddSingleton(sp => new MjpegProvider(pixelFormat, noSignalPicPath));
            return serviceCollection;
        }
    }
}
