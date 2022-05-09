using Microsoft.AspNetCore.Mvc;
using Mjpeg.Provider.NET;

namespace Mjpeg.Provider.Example.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LiveViewController : ControllerBase
    {
        private readonly MjpegProvider provider;

        public LiveViewController(MjpegProvider provider)
            => this.provider = provider;

        /// <summary>
        /// https://localhost:5001/api/LiveView/Stream?Id=15d07bca-864a-48cc-9c48-6c1734e09f49&fps=15&longSize=720&drawBoundingBox=true
        /// </summary>
        /// <param name="Id"> Stream id </param>
        /// <param name="fps"> Output fps </param>
        /// <param name="longSize"> Set the longest side for equal scaling </param>
        /// <param name="drawBoundingBox"> Whether to draw a bounding box </param>
        /// <returns></returns>
        [HttpGet]
        [Route("Stream")]
        public IActionResult Stream(Guid Id, int fps = 15, int longSize = default, bool drawBoundingBox = false)
            => provider.GetMJPEGActionResult(Id, fps, longSize, drawBoundingBox);
    }
}
