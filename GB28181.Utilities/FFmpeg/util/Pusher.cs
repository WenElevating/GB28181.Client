using FFmpeg.AutoGen;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GB28181.Utilities
{
    public class Pusher
    {
        private ConcurrentQueue<AVPacket> _vedioPacketQueue = new ConcurrentQueue<AVPacket>();

        private ConcurrentQueue<AVFrame> _vedioFrameQueue = new ConcurrentQueue<AVFrame>();

        private CancellationTokenSource _sendPacketToken;

        private CancellationTokenSource _sendH264FrameToken;

        private FFmepgStreamNewEnocder _pushStream;

        private FFmpegStreamNewDecoder _streamDecoder;

        private FFmpegToLibRtmpDecoder _srsDecoder;

        private static object _lock = new object(); 

        public void InitDecoder(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            // 解码器
            _streamDecoder = new FFmpegStreamNewDecoder(url);
            _streamDecoder.InitDecoder();
            _streamDecoder.OnPacket += OnPacket;
            _streamDecoder.OnContextClose += OnContextClose;
        }

        public void InitSrsDecoder(string url)
        {
            _srsDecoder = new FFmpegToLibRtmpDecoder(url);
            _srsDecoder.OnFrame += OnFrame;
        }

        /// <summary>
        /// rtsp拉流读取结束回调
        /// </summary>
        private void OnContextClose()
        {
            _streamDecoder.Dispose();
        }

        private void OnPacket(ref AVPacket packet)
        {
            if (_vedioPacketQueue != null)
            {
                _vedioPacketQueue.Enqueue(packet);
            }
        }

        private unsafe void OnFrame(ref AVFrame frame)
        {
            if (_vedioFrameQueue != null)
            {
                _vedioFrameQueue.Enqueue(frame);
            }
        }

        /// <summary>
        /// 推流
        /// </summary>
        public async Task PushStreamAsync(string url)
        {
            await Task.Run(() =>
            {
                _sendPacketToken = new CancellationTokenSource();

                // 编码器
                _pushStream = new FFmepgStreamNewEnocder(url);
                _pushStream.InitEnocder(_streamDecoder.Width, _streamDecoder.Height, _streamDecoder.ExtraData, _streamDecoder.Extradata_size, _streamDecoder.FrameRate);

                long lastPts = -1;
                while (!_sendPacketToken.IsCancellationRequested)
                {
                    if (_vedioPacketQueue.TryDequeue(out var pack))
                    {
                        if (_streamDecoder.VedioIndex != pack.stream_index)
                        {
                            continue;
                        }
                        _pushStream.Push_H264(ref pack, ref lastPts, _streamDecoder.PktTimebase);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }

                }
            });
        }

        /// <summary>
        /// 推送PS流，（封装有问题，待解决）
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task PushPsStreamAsync(string ip, int port)
        {
            await Task.Run(() =>
            {
                _sendPacketToken = new CancellationTokenSource();

                // 编码器
                UdpClient client = new UdpClient();
                IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

                long lastPts = -1;
                while (!_sendPacketToken.IsCancellationRequested)
                {
                    if (_vedioPacketQueue.TryDequeue(out var pack))
                    {
                        if (_streamDecoder.VedioIndex != pack.stream_index)
                        {
                            continue;
                        }
                        //byte[] data = RTPHelper.GetPsPacket(ref pack);

                        // 解析
                        //var util = new PsToH264Util();
                        //util.Write(data);
                        //util.ExecuteParsing();
                        //client.SendAsync(data, data.Length, iPEndPoint);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }

                }
            });
        }

        /// <summary>
        /// SRS推流
        /// </summary>
        //public async Task PushSRS(string url)
        //{
        //    await Task.Run(() =>
        //    {
        //        _sendH264FrameToken = new CancellationTokenSource();

        //        // 编码器
        //        var stream = new LibRtmpPushStream(url);

        //        lock (_lock)
        //        {
        //            while (_sendH264FrameToken != null && !_sendH264FrameToken.IsCancellationRequested)
        //            {
        //                if (_vedioPacketQueue.TryDequeue(out var packet))
        //                {
        //                    byte[] data = new byte[packet.size];
        //                    long Pts = 0;
        //                    unsafe
        //                    {
        //                        Marshal.Copy((IntPtr)packet.data, data, 0, data.length);
        //                        Pts = _streamDecoder.ConverterPts(packet.Pts, new AVRational() { num = 1, den = _streamDecoder.FrameRate});
        //                    }
        //                    stream.Push_H264_Data(data, packet.Pts, packet.Dts);
        //                    Thread.Sleep(TimeSpan.FromMilliseconds(_streamDecoder.FrameRate / 1000));
        //                }
        //                else
        //                {
        //                    Thread.Sleep(1);
        //                }
        //            }
        //        }
        //    });
        //}

        /// <summary>
        /// 停止推流
        /// </summary>
        public void StopPushStream()
        {
            _sendPacketToken.Cancel();
            _pushStream.Dispose();
            _streamDecoder.Dispose();
        }

    }
}
