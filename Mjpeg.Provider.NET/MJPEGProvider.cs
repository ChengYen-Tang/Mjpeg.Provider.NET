﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mjpeg.Provider.NET
{
    public class MjpegProvider
    {
        private readonly ConcurrentDictionary<Guid, Channel> channels;
        private readonly ConcurrentDictionary<MjpegParameter, ImageRawData> jpegs;
        private readonly ConcurrentDictionary<MjpegParameter, StreamContent> Contents;
        private readonly Guid noSignalId;
        private readonly ImageRawData noSignalImage;
        private readonly Func<byte[], int, int, Image> loadImage;

        /// <summary>
        /// 初始化實體
        /// </summary>
        /// <param name="noSignalPicPath"> 無訊號圖片位置 </param>
        public MjpegProvider(PixelFormat pixelFormat, string noSignalPicPath)
        {
            loadImage = SetLoadImageMethod(pixelFormat);
            channels = new();
            jpegs = new();
            Contents = new();
            Image image = Image.Load(noSignalPicPath);
            noSignalImage = ConvertToJpegArray(image);
            noSignalId = Guid.NewGuid();
            channels.TryAdd(noSignalId, new() { Image = image });
        }

        /// <summary>
        /// 新增串流通道
        /// </summary>
        /// <param name="Id"> 串流 Id </param>
        /// <returns></returns>
        public bool CreateChannel(Guid Id)
        {
            channels.TryGetValue(noSignalId, out var noSignalChannel);
            Channel channel = new() { Image = noSignalChannel.Image.Clone(process => { }) };

            bool lockTaken = false;
            noSignalChannel.ChannelLock.Enter(ref lockTaken);
            ICollection<MjpegParameter> parameters = noSignalChannel.MJPEGParameters.Where(item => item.Id == Id).ToArray();
            if (parameters.Any())
            {
                foreach (MjpegParameter parameter in parameters)
                    noSignalChannel.MJPEGParameters.Remove(parameter);

                channel.MJPEGParameters.AddRange(parameters);
            }
            if (lockTaken)
                noSignalChannel.ChannelLock.Exit();

            return channels.TryAdd(Id, channel);
        }

        /// <summary>
        /// 移除串流通道
        /// </summary>
        /// <param name="Id"> 串流 Id </param>
        public void RemoveChannel(Guid Id)
            => channels.TryRemove(Id, out _);

        /// <summary>
        /// 更新串流通道中的影像
        /// </summary>
        /// <param name="Id"> 串流 Id </param>
        /// <param name="image"> JPeg byte array </param>
        public void UpdateChannelImage(Guid Id, byte[] imageData, int width, int height)
        {
            Image image = loadImage(imageData, width, height);
            SetImage(Id, image);
        }

        /// <summary>
        /// 將串流影像重設為預設影像
        /// </summary>
        /// <param name="Id"> 串流 Id </param>
        public void SetDefaultImage(Guid Id)
        {
            if (channels.TryGetValue(noSignalId, out var noSignalChannel))
                SetImage(Id, noSignalChannel.Image.Clone(process => { }));
        }

        /// <summary>
        /// 取得指定串流通動的 MJPEG Content
        /// </summary>
        /// <param name="Id"> 串流 Id </param>
        /// <param name="fps"> 刷新率 </param>
        /// <param name="longSize"> 最長邊的長度 </param>
        /// <param name="drawBoundingBox"> 是否要化BoundingBox，如果有BoundingBox的話 </param>
        /// <returns></returns>
        public IActionResult GetMJPEGActionResult(Guid Id, int fps, int longSize = default, bool drawBoundingBox = false)
        {
            MjpegParameter parameter = new(Id, longSize, drawBoundingBox);
            int delayTime = 1000 / fps;
            MjpegStreamContent content = new(RemoveContent, parameter, async cancellationToken => {
                await Task.Delay(delayTime, cancellationToken); // 2 second delay between image updates
                ImageRawData channelImage = noSignalImage;
                if (Contents.TryGetValue(parameter, out var streamContent))
                {
                    bool lockTacker = false;
                    streamContent.ImageChangeLock.Enter(ref lockTacker);
                    if (jpegs.TryGetValue(parameter, out ImageRawData channelImageByDict))
                    {
                        channelImageByDict.SetUsed();
                        channelImage = channelImageByDict;
                    }
                    if (lockTacker)
                    streamContent.ImageChangeLock.Exit();
                }
                return channelImage;
            });

            AddContents(parameter, content);
            return content;
        }

        /// <summary>
        /// 設定指定串流的 BoundingBox
        /// </summary>
        /// <param name="Id">  串流 I </param>
        /// <param name="boundingBoxInfos"> boundingBoxInfos </param>
        public void SetBoundingBox(Guid Id, IEnumerable<BoundingBox> boundingBoxInfos)
        {
            if (channels.TryGetValue(Id, out var channelImage))
            {
                if (boundingBoxInfos != null)
                    channelImage.BoundingBoxInfos = boundingBoxInfos.Select(item => new BoundingBoxInfo(SixLabors.ImageSharp.Color.FromRgb(item.Color.R, item.Color.G, item.Color.B), item.Thickness, item.Points.Select(point => new SixLabors.ImageSharp.PointF(point.X, point.Y)).ToArray()));
                else
                    channelImage.BoundingBoxInfos = null;
            }
        }

        private void SetImage(Guid Id, Image image)
        {
            if (channels.TryGetValue(Id, out var channelImage))
            {
                Image oldImage = channelImage.Image;
                channelImage.Image = image;

                bool lockTaken = false;
                channelImage.ChannelLock.Enter(ref lockTaken);
                Parallel.ForEach(channelImage.MJPEGParameters, (provider) =>
                {
                    if (Contents.TryGetValue(provider, out var content))
                        content.GenerateMJPEGAction.Invoke();
                });
                if (lockTaken)
                    channelImage.ChannelLock.Exit();

                oldImage.Dispose();
            }
        }

        /// <summary>
        /// 紀錄存在的 MJPEG 內容通道
        /// </summary>
        /// <param name="parameter"> mjpeg 參數 </param>
        /// <param name="content"> MJPEG 內容通道 </param>
        private void AddContents(MjpegParameter parameter, MjpegStreamContent content)
        {
            Channel channel;
            bool isChannelExist = channels.ContainsKey(parameter.Id);
            if (isChannelExist)
                channels.TryGetValue(parameter.Id, out channel);
            else
                channels.TryGetValue(noSignalId, out channel);

            bool lockTaken = false;
            channel.ChannelLock.Enter(ref lockTaken);
            if (!Contents.ContainsKey(parameter))
            {
                jpegs.TryAdd(parameter, null);
                StreamContent streamContents = new();
                streamContents.GenerateMJPEGAction = new Action(() => GenerateMJPEG(parameter, streamContents.MemoryStreamManager));
                streamContents.Contents.Add(content);
                Contents.TryAdd(parameter, streamContents);
                channel.MJPEGParameters.Add(parameter);
                if (!isChannelExist)
                    GenerateMJPEG(parameter, streamContents.MemoryStreamManager, false);
                else
                    GenerateMJPEG(parameter, streamContents.MemoryStreamManager);
            }
            else
            {
                Contents.TryGetValue(parameter, out var streamContents);
                streamContents.Contents.Add(content);
            }
            if (lockTaken)
                channel.ChannelLock.Exit();
        }

        /// <summary>
        /// 刪除紀錄的 MJPEG 內容通道
        /// </summary>
        /// <param name="parameter"> mjpeg 參數 </param>
        /// <param name="content"> MJPEG 內容通道 </param>
        private void RemoveContent(MjpegParameter parameter, MjpegStreamContent content)
        {
            Channel channel;
            if (!channels.TryGetValue(parameter.Id, out channel))
                channels.TryGetValue(noSignalId, out channel);

            bool lockTaken = false;
            channel.ChannelLock.Enter(ref lockTaken);
            Contents.TryGetValue(parameter, out var streamContents);
            streamContents.Contents.Remove(content);
            if (!streamContents.Contents.Any())
            {
                channel.MJPEGParameters.Remove(parameter);
                Contents.TryRemove(parameter, out _);
                jpegs.TryRemove(parameter, out ImageRawData image);
                if (image != null)
                    image.Dispose();
            }
            if (lockTaken)
                channel.ChannelLock.Exit();
        }

        /// <summary>
        /// 圖片 轉 JPEG
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="isValidStream"></param>
        private async void GenerateMJPEG(MjpegParameter parameter,
            RecyclableMemoryStreamManager memoryStreamManager,
            bool isValidStream = true)
        {
            if (channels.TryGetValue(parameter.Id, out var channel))
            {
                Image sourceImage;
                if (isValidStream)
                    sourceImage = channel.Image;
                else
                {
                    channels.TryGetValue(noSignalId, out var noSignaChannel);
                    sourceImage = noSignaChannel.Image;
                }

                Image image;

                if (parameter.LongSize == default)
                {
                    if (!isValidStream || channel.BoundingBoxInfos == null || !parameter.DrawBoundingBox)
                        image = sourceImage.Clone(process => { });
                    else
                    {
                        image = sourceImage.Clone(process => {
                            foreach (BoundingBoxInfo BoundingBox in channel.BoundingBoxInfos)
                            {
                                process.DrawLines(BoundingBox.Color, BoundingBox.Thickness, BoundingBox.Points);
                            }
                        });
                    }
                }
                else
                {
                    int width;
                    int height;
                    if (sourceImage.Width > sourceImage.Height)
                    {
                        double scale = (double)sourceImage.Width / (double)parameter.LongSize;
                        width = parameter.LongSize;
                        height = (int)(sourceImage.Height / scale);
                    }
                    else
                    {
                        double scale = (double)sourceImage.Height / (double)parameter.LongSize;
                        width = (int)(sourceImage.Width / scale);
                        height = parameter.LongSize;
                    }

                    if (!isValidStream || channel.BoundingBoxInfos == null || !parameter.DrawBoundingBox)
                        image = sourceImage.Clone(process => process.Resize(width, height, KnownResamplers.Bicubic));
                    else
                    {
                        image = sourceImage.Clone(process => {
                            foreach (BoundingBoxInfo BoundingBox in channel.BoundingBoxInfos)
                                process.DrawLines(BoundingBox.Color, BoundingBox.Thickness, BoundingBox.Points);
                            process.Resize(width, height, KnownResamplers.Bicubic);
                        });
                    }
                }

                if (Contents.TryGetValue(parameter, out StreamContent streamContent))
                {
                    bool lockTacker = false;
                    streamContent.ImageChangeLock.Enter(ref lockTacker);
                    if (jpegs.TryGetValue(parameter, out var imageData))
                    {
                        jpegs.TryUpdate(parameter, ConvertToJpegArray(image, memoryStreamManager), imageData);
                        if (imageData != null)
                            imageData.Dispose();
                    }
                    if (lockTacker)
                        streamContent.ImageChangeLock.Exit();
                }
                image.Dispose();
            }
        }

        private static ImageRawData ConvertToJpegArray(Image image)
        {
            MemoryStream memoryStream = new();
            image.SaveAsJpeg(memoryStream);
            return new(memoryStream);
        }

        private static ImageRawData ConvertToJpegArray(Image image, RecyclableMemoryStreamManager memoryStreamManager)
        {
            MemoryStream memoryStream = memoryStreamManager.GetStream();
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.Position = 0;
            image.SaveAsJpeg(memoryStream);
            return new(memoryStream);
        }

        private static Func<byte[], int, int, Image> SetLoadImageMethod(PixelFormat pixelFormat)
            => pixelFormat switch
            {
                PixelFormat.RGB => Image.LoadPixelData<Rgb24>,
                _ => Image.LoadPixelData<Bgr24>
            };
    }

    internal class Channel
    {
        public Channel()
            => (ChannelLock, MJPEGParameters) = (new(false), new());

        public Image Image { get; set; }
        public IEnumerable<BoundingBoxInfo> BoundingBoxInfos { get; set; }
        public SpinLock ChannelLock { get; init; }
        public List<MjpegParameter> MJPEGParameters { get; set; }
    }

    internal class StreamContent
    {
        public StreamContent()
            => (Contents, ImageChangeLock, MemoryStreamManager) = (new(), new(false), new());

        public List<MjpegStreamContent> Contents { get; }
        public SpinLock ImageChangeLock { get; init; }
        public Action GenerateMJPEGAction { get; set; }
        public RecyclableMemoryStreamManager MemoryStreamManager { get; }
    }

    public enum PixelFormat
    {
        RGB,
        BGR
    }
}
