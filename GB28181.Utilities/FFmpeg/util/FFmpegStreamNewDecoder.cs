using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.Utilities
{
    public unsafe class FFmpegStreamNewDecoder: IDisposable
    {
        private AVFormatContext* _sourceContext = null;

        private int _vedioIndex;

        private string _url;

        public delegate void OnPacketDelegate(ref AVPacket packet);

        public event OnPacketDelegate OnPacket;

        public delegate void OnContextCloseDeletgate();

        public event OnContextCloseDeletgate OnContextClose;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public int FrameRate { get; private set; }

        public int VedioIndex { get; private set; }

        public AVCodecID CodecId { get; private set; }

        public AVRational PktTimebase { get; private set; }

        public IntPtr ExtraData { get; private set; }

        public int Extradata_size { get; private set; }

        public FFmpegStreamNewDecoder(string url) 
        { 
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("The url cannot be null");
            }
            _url = url;
        }

        public void InitDecoder()
        {
            // 初始化网络库
            ffmpeg.avformat_network_init();

            // 设置日志级别
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_VERBOSE);

            // 封装上下文
            _sourceContext = ffmpeg.avformat_alloc_context();
            var format = _sourceContext;

            AVDictionary* options = null;

            if (_url.StartsWith("rtsp://"))
            {
                ffmpeg.av_dict_set(&options, "rtsp_transport", "tcp", 0);
                ffmpeg.av_dict_set(&options, "stimeout", "10000000", 0);
                ffmpeg.av_dict_set(&options, "flvflags", "no_duration_filesize", 0);
            }

            // 打开文件
            if (ffmpeg.avformat_open_input(&format, _url, null, &options) < 0)
            {
                throw new ApplicationException("open file failed !");
            }
            Debug.WriteLine("open file success!");

            // 获取视频数据
            if (ffmpeg.avformat_find_stream_info(format, null) < 0)
            {
                throw new ApplicationException("get stream info failed!");
            }
            Debug.WriteLine("get stream info success!");

            // 视频流位置
            for (int i = 0; i < _sourceContext->nb_streams; i++)
            {
                if (_sourceContext->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    _vedioIndex = i;
                    break;
                }
            }

            ffmpeg.av_dump_format(_sourceContext, 0, _url, 0);

            // 参数
            Width = _sourceContext->streams[_vedioIndex]->codecpar->width;

            Height = _sourceContext->streams[_vedioIndex]->codecpar->height;
            
            FrameRate = (int)ffmpeg.av_q2d(_sourceContext->streams[_vedioIndex]->r_frame_rate);
            
            CodecId = _sourceContext->streams[_vedioIndex]->codecpar->codec_id;

            PktTimebase = _sourceContext->streams[_vedioIndex]->time_base;

            ExtraData = (IntPtr)_sourceContext->streams[_vedioIndex]->codecpar->extradata;

            Extradata_size = _sourceContext->streams[_vedioIndex]->codecpar->extradata_size;

            // 启动解码线程
            Task.Run(RunDecodeLoop);
        }

        /// <summary>
        /// 解码线程
        /// </summary>
        private void RunDecodeLoop()
        {
            while (true) {
                AVPacket* packet = ffmpeg.av_packet_alloc();
                // 读一帧
                int error = ffmpeg.av_read_frame(_sourceContext, packet);
                if (error != 0 || error == ffmpeg.AVERROR_EOF)
                {
                    Debug.WriteLine("read frame end!");
                    return;
                }

                // 复制包
                var newPack = CopyPacket(packet);

                int size = Marshal.SizeOf((IntPtr)packet->data);
                byte[] data = new byte[size];
                Marshal.Copy((IntPtr)newPack.data, data, 0, size);
                var newData = data;

                if (OnPacket != null)
                {
                    OnPacket?.Invoke(ref *packet);
                }
            }
        }

        /// <summary>
        /// 复制包
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        private AVPacket CopyPacket(AVPacket* packet)
        {
            AVPacket* pack = ffmpeg.av_packet_clone(packet);
            return *pack;
        }

        /// <summary>
        /// 转换pts
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="timebase"></param>
        /// <returns></returns>
        public long ConverterPts(long pts, AVRational timebase)
        {
            return ffmpeg.av_rescale_q_rnd(pts, PktTimebase, timebase, AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX);
        }


        public void Dispose()
        {
            ffmpeg.avformat_free_context(_sourceContext);
        }
    }
}
