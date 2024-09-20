using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GB28181.Utilities
{
    public unsafe class FFmepgStreamNewEnocder : IDisposable
    {
        private string _url;

        private string _dstFormatType;

        private AVFormatContext* _outputContext = null;

        private CancellationTokenSource _ctsForInfoTask;

        private AVCodecID _codecID;

        private long _streamBytes;

        private long _framesSended;

        private static object _lock = new object();

        public string Url => _url;

        public bool IsInitial = false;

        public FFmepgStreamNewEnocder(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("sourceUrl or targetUrl can't null!");
            }

            _dstFormatType = GetFormatType(url);

            if (string.IsNullOrEmpty(_dstFormatType))
            {
                throw new NotSupportedException("Not support this url:" + url);
            }

            _url = url;

            if (_ctsForInfoTask == null)
            {
                _ctsForInfoTask = new CancellationTokenSource();
            }
        }

        /// <summary>
        /// 初始化输出上下文
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="timebase"></param>
        /// <param name="extraData"></param>
        /// <param name="extraDataSize"></param>
        /// <param name="frameRate"></param>
        /// <param name="codecID"></param>
        public void InitEnocder(int width, int height, IntPtr extraData, int extraDataSize, int frameRate = 25, AVCodecID codecID = AVCodecID.AV_CODEC_ID_H264)
        {
            _outputContext = ffmpeg.avformat_alloc_context();
            var tempFormat = _outputContext;
            // 握手
            if (ffmpeg.avformat_alloc_output_context2(&tempFormat, null, _dstFormatType, _url) < 0)
            {
                throw new ApplicationException("alloc output context failed!");
            }

            _outputContext = tempFormat;

            var oFormat = _outputContext->oformat;

            AVCodec* codec = ffmpeg.avcodec_find_encoder(codecID);

            AVCodecContext* inputCodecContext = ffmpeg.avcodec_alloc_context3(codec);
            inputCodecContext->width = width;//视频宽度
            inputCodecContext->height = height;//视频高度
            inputCodecContext->framerate = new AVRational() { num = frameRate, den = 1 };
            inputCodecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUVJ420P;//像素格式
            inputCodecContext->codec_id = codecID;//编码器id
            inputCodecContext->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
            inputCodecContext->bit_rate = 2000000;
            inputCodecContext->extradata = (byte*)extraData;
            inputCodecContext->extradata_size = extraDataSize;

            if (codecID == AVCodecID.AV_CODEC_ID_H264)
            {
                // 高版本的ffmpeg中会失败
                ffmpeg.av_opt_set(inputCodecContext->priv_data, "preset", "veryslow", 0);
                ffmpeg.av_opt_set(inputCodecContext->priv_data, "tune", "zerolatency", 0);
            }
            else if ((codecID == AVCodecID.AV_CODEC_ID_VP8) || (codecID == AVCodecID.AV_CODEC_ID_VP9))
            {
                ffmpeg.av_opt_set(inputCodecContext->priv_data, "quality", "realtime", 0);
            }

            _codecID = codecID;

            if ((inputCodecContext->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
            {
                inputCodecContext->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            }

            // 复制输入流配置并创建输出流
            AVStream* outStream = ffmpeg.avformat_new_stream(_outputContext, codec);
            if (outStream == null)
            {
                ffmpeg.avformat_free_context(_outputContext);
                throw new ApplicationException("create new output stream failed!");
            }


            // 复制编解码上下文
            if (ffmpeg.avcodec_parameters_from_context(outStream->codecpar, inputCodecContext) < 0)
            {
                ffmpeg.avformat_free_context(_outputContext);
                throw new ApplicationException("copy parameters data failed!");
            }

            outStream->codecpar->codec_tag = 0;
            if ((oFormat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
            {
                _outputContext->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            }

            ffmpeg.av_dump_format(_outputContext, 0, _url, 1);

            if ((oFormat->flags & ffmpeg.AVFMT_NOFILE) == 0)
            {
                // 连接
                if (ffmpeg.avio_open(&_outputContext->pb, _url, ffmpeg.AVIO_FLAG_WRITE) < 0)
                {
                    ffmpeg.avformat_free_context(_outputContext);
                    throw new ApplicationException("connect rtmp server failed!");
                }
            }

            // 写入文件头
            if (ffmpeg.avformat_write_header(_outputContext, null) < 0)
            {
                ffmpeg.avformat_free_context(_outputContext);
                throw new ApplicationException("write header failed!");
            }

        }

        /// <summary>
        /// 推送帧
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="lastPts"></param>
        /// <param name="timebase"></param>
        /// <param name="vedioIndex"></param>
        /// <param name="FrameRate"></param>
        public unsafe void Push_H264(ref AVPacket packet, ref long lastPts, AVRational timebase)
        {
            //byte[] data = new byte[packet.size];
            //Marshal.Copy((IntPtr)packet.data, data, 0, packet.size);
            //if (GetNalCount(data) == -1)
            //{
            //    return;
            //}

            AVStream* outputStream;
            AVPacket pack = packet;
            try
            {
                lock (_lock)
                {
                    outputStream = _outputContext->streams[pack.stream_index];

                    // copy packet
                    // 转换PTS/DTS（Convert PTS/DTS）
                    pack.pts = ffmpeg.av_rescale_q_rnd(pack.pts, timebase, outputStream->time_base, AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX);
                    pack.dts = ffmpeg.av_rescale_q_rnd(pack.dts, timebase, outputStream->time_base, AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX);
                    pack.duration = ffmpeg.av_rescale_q(pack.duration, timebase, outputStream->time_base);
                    pack.pos = -1;
                    pack.flags = 1;

                    if (lastPts > pack.pts)
                    {
                        return;
                    }
                    else
                    {
                        lastPts = pack.pts;
                    }

                    _framesSended++;
                    _streamBytes += pack.size;

                    int error = ffmpeg.av_interleaved_write_frame(_outputContext, &pack);

                    if (error < 0)
                    {
                        throw new ApplicationException("Error muxing packet! error: " + error);
                    }
                }
            }
            catch (Exception ex)
            {
                ffmpeg.av_packet_unref(&pack);
                Dispose();
            }

            Thread.Sleep(1);
        }

        private int GetNalCount(byte[] data)
        {
            int count = -1;
            for (int i = 0; i < data.Length; ++i)
            {
                if (data[i] == 0x00 && data[i + 1] == 0x00)
                {
                    if (data[i + 2] == 0x01)
                    {
                        count++;
                    }
                    else if (data[i + 2] == 0x00 && data[i + 3] == 0x01)
                    {
                        count++;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }
            }
            return count;
        }

        private string GetFormatType(string url)
        {
            if (url.StartsWith("rtmp://"))
            {
                return "flv";
            }

            if (url.StartsWith("rtsp://"))
            {
                return "rtsp";
            }

            if (url.StartsWith("udp://"))
            {
                return "h264";
            }

            if (url.StartsWith("rtp://"))
            {
                //return "rtp_mpegts";
                return "rtp_mpegts";
            }

            return null;
        }

        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        public void Dispose()
        {
            // 写入文件尾
            try
            {
                _ctsForInfoTask?.Cancel();
                _ctsForInfoTask = null;

                var pFormatContext = _outputContext;

                if (pFormatContext != null)
                {
                    ffmpeg.av_write_trailer(pFormatContext);
                    ffmpeg.avformat_close_input(&pFormatContext);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[VideoStreamEncoder] " + ex.ToString());
            }

            GC.SuppressFinalize(this);
        }
    }
}
