using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GB28181.Utilities
{
    public unsafe class FFmpegToLibRtmpDecoder
    {
        private AVFormatContext* _srcFormatContext;

        private AVCodecContext* _codecContext;

        private SwsContext* _wsContext;

        private string _url;

        private int _vedioIndex = -1;

        private byte_ptrArray4 _data;

        private int_array4 _linsize;

        public delegate void OnFrameDelegate(ref AVFrame frame);

        public event OnFrameDelegate OnFrame;

        public string Url { get => _url; }

        public int FrameWidth { get; set; }

        public int FrameHeight { get; set; }

        public int FrameRate { get; set; }

        public AVRational Timebase { get; set; }

        public CancellationTokenSource DecodeToken { get; }

        public FFmpegToLibRtmpDecoder(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ApplicationException("[error] -> Decode url address is cant't null!");
            }

            _url = url;


            AVDictionary* options = null;
            // rtsp设置
            if (_url.StartsWith("rtsp://"))
            {
                ffmpeg.av_dict_set(&options, "rtsp_transport", "tcp", 0);
                ffmpeg.av_dict_set(&options, "stimeout", "10000000", 0);
                ffmpeg.av_dict_set(&options, "flvflags", "no_duration_filesize", 0);
            }

            _srcFormatContext = ffmpeg.avformat_alloc_context();

            var tempFormatContext = _srcFormatContext;
            if (ffmpeg.avformat_open_input(&tempFormatContext, _url, null, &options) != 0)
            {
                throw new ApplicationException("[error] -> this url can't open input!");
            }

            if (ffmpeg.avformat_find_stream_info(tempFormatContext, null) < 0)
            {
                throw new ApplicationException("[error] -> this url can't find stream info!");
            }

            AVCodec* codec = null;
            _vedioIndex = ffmpeg.av_find_best_stream(_srcFormatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &codec, 0);

            if (_vedioIndex < 0 || _vedioIndex == ffmpeg.AVERROR_STREAM_NOT_FOUND)
            {
                throw new ApplicationException("[error] -> this url can't find vedio stream!");
            }

            _codecContext = ffmpeg.avcodec_alloc_context3(codec);

            if (_codecContext == null)
            {
                throw new ApplicationException("[error] -> current can't create codec context!");
            }

            if (ffmpeg.avcodec_parameters_to_context(_codecContext, _srcFormatContext->streams[_vedioIndex]->codecpar) < 0)
            {
                throw new ApplicationException("[error] -> parameter to codec context failed!");
            }

            if (ffmpeg.avcodec_open2(_codecContext, codec, null) != 0)
            {
                throw new ApplicationException("[error] -> this codec can't open!");
            }

            FrameWidth = _srcFormatContext->streams[_vedioIndex]->codecpar->width;

            FrameHeight = _srcFormatContext->streams[_vedioIndex]->codecpar->height;

            FrameRate = (int)ffmpeg.av_q2d(_srcFormatContext->streams[_vedioIndex]->r_frame_rate);

            Timebase = _srcFormatContext->streams[_vedioIndex]->time_base;

            if (!InitConvert(FrameWidth, FrameHeight, _codecContext->pix_fmt, FrameWidth, FrameHeight, _codecContext->pix_fmt))
            {
                ffmpeg.avformat_free_context(_srcFormatContext);
                throw new ApplicationException("[error] -> init convert failed!");
            }

            DecodeToken = new CancellationTokenSource();
            // 启动解码线程
            Task.Run(MainDecodeLoop, DecodeToken.Token);
        }


        public void MainDecodeLoop()
        {
            while (DecodeToken != null && !DecodeToken.IsCancellationRequested)
            {
                AVPacket* packet = ffmpeg.av_packet_alloc();
                AVFrame* frame = ffmpeg.av_frame_alloc();
                try
                {
                    int error = ffmpeg.av_read_frame(_srcFormatContext, packet);

                    if (error < 0 || error == ffmpeg.AVERROR_EOF)
                    {
                        throw new ApplicationException("[info] -> this url read frame end.");
                    }

                    if (packet->stream_index != _vedioIndex)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    error = ffmpeg.avcodec_send_packet(_codecContext, packet);

                    if (error != 0)
                    {
                        throw new ApplicationException($"[error] -> send packet error code: {error}.");
                    }

                    error = ffmpeg.avcodec_receive_frame(_codecContext, frame);

                    if (error != 0)
                    {
                        Debug.WriteLine($"[error] -> receive frame error code: {error}.");
                        continue;
                    }

                    // 回调
                    if (OnFrame != null)
                    {
                        OnFrame?.Invoke(ref *frame);
                    }

                }
                catch (Exception ex)
                {
                    ffmpeg.av_packet_unref(packet);
                    ffmpeg.av_frame_unref(frame);
                    ffmpeg.av_packet_free(&packet);
                    ffmpeg.av_frame_free(&frame);
                    DecodeToken.Cancel();
                    Dispose();
                }
            }
        }

        public bool InitConvert(int srcFrameWidth, int srcFrameHeight, AVPixelFormat srcPixelFormat, int dstFrameWidth, int dstFrameHeight, AVPixelFormat dstPixelFormat)
        {
            _wsContext = ffmpeg.sws_getContext(srcFrameWidth, srcFrameHeight, srcPixelFormat, dstFrameWidth, dstFrameHeight, dstPixelFormat, ffmpeg.SWS_FAST_BILINEAR, null, null, null);
            if (_wsContext == null)
            {
                return false;
            }

            int bufferSize = ffmpeg.av_image_get_buffer_size(dstPixelFormat, dstFrameWidth, dstFrameHeight, 1);

            IntPtr dataPointer = Marshal.AllocHGlobal(bufferSize);

            _data = new byte_ptrArray4();

            _linsize = new int_array4();

            ffmpeg.av_image_fill_arrays(ref _data, ref _linsize, (byte*)dataPointer, dstPixelFormat, dstFrameWidth, dstFrameHeight, 1);

            return true;
        }

        public byte[] FrameConvertArray(AVFrame frame)
        {
            var tempFrame = &frame;

            ffmpeg.sws_scale(_wsContext, tempFrame->data, tempFrame->linesize, 0, tempFrame->height, _data, _linsize);

            var data = new byte_ptrArray8();

            data.UpdateFrom(_data);

            var linsize = new int_array8();

            linsize.UpdateFrom(_linsize);

            byte[] result = new byte[(int)(FrameWidth * FrameHeight * 1.5)];

            Marshal.Copy((IntPtr)data[0], result, 0, result.Length);

            return result;
        }

        public void Dispose()
        {
            if (_srcFormatContext != null)
            {
                ffmpeg.avformat_free_context(_srcFormatContext);
            }

            //if (_codecContext != null)
            //{
            //    var tempCodecContext = &_codecContext;
            //    ffmpeg.avcodec_free_context(tempCodecContext);
            //}

        }
    }
}
