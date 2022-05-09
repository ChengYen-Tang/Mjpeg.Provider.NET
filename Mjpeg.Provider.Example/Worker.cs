using Emgu.CV;
using Emgu.CV.CvEnum;
using Mjpeg.Provider.NET;

namespace Mjpeg.Provider.Example
{
    public class Worker : IHostedService
    {
        private readonly ILogger<Worker> _logger;
        private readonly MjpegProvider provider;
        private readonly VideoCapture capture;
        private readonly Mat frame;
        private readonly Guid streamId;

        private Task task1;
        private Task task2;
        private bool taskSwitch = true;

        public Worker(ILogger<Worker> logger, MjpegProvider provider)
        {
            _logger = logger;
            this.provider = provider;
            streamId = Guid.Parse("15d07bca-864a-48cc-9c48-6c1734e09f49");
            provider.CreateChannel(streamId);
            capture = new VideoCapture(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resouce", "Pexels Videos 2527932.mp4"));
            frame = new();
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            task1 = Task.Factory.StartNew(PushImageAsync, TaskCreationOptions.LongRunning);
            task2 = Task.Factory.StartNew(PushBoundingBoxAsync, TaskCreationOptions.LongRunning);
            _logger.LogInformation("worker service is started.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            taskSwitch = false;
            task1.Wait();
            task2.Wait();

            capture.Dispose();
            frame.Dispose();

            _logger.LogInformation("worker service is stoped.");
            return Task.CompletedTask;
        }

        private void PushImageAsync()
        {
            while (taskSwitch)
            {
                bool hasFrame = capture.Read(frame);
                if (hasFrame)
                    provider.UpdateChannelImage(streamId, frame.GetRawData(), frame.Width, frame.Height);
                else
                    capture.Set(CapProp.PosAviRatio, 0);
            }
        }

        private async Task PushBoundingBoxAsync()
        {
            Random rnd = new(Guid.NewGuid().GetHashCode());

            while (taskSwitch)
            {
                await Task.Delay(100);

                BoundingBox[] boundinxBoxInfos = new BoundingBox[] {
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(79, 334), new PointF(101, 334), new PointF(101, 350), new PointF(79, 350), new PointF(79, 334) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                    new(new(255, 0, 0), 2, new PointF[] { new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)), new PointF(rnd.Next(2, 500), rnd.Next(2, 500)) }),
                };

                provider.SetBoundingBox(streamId, boundinxBoxInfos);
            }
        }
    }
}