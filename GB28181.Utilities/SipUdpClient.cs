using FFmpeg.AutoGen;
using SIPSorcery.SIP.App;
using SIPSorcery.SIP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using System.Net;
using SIPSorcery.Sys;
using System.Xml;
using System.Reflection.Metadata;
using System.Diagnostics;
using GB28181.Utilities.util;

namespace GB28181.Utilities
{
    /// <summary>
    /// SIP客户端，主要包括设备注册、保活、推流
    /// </summary>
    public class SipUdpClient
    {
        // 日志工厂
        public ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddDebug());

        // 日志
        public ILogger logger;

        public bool IsConnectServer;

        private SIPTransport _transport;

        private SIPUDPChannel _channel;

        private object _lock = new();

        private IPEndPoint? _localEndPoint;

        private IPEndPoint? _remoteEndPoint;

        private string _clientId = "34020000002000001560";

        private readonly string _sipId = "";

        private string _realm = "";

        private CancellationTokenSource _keepLiveToken;

        private Task _keepLiveTask;

        private DeviceManager _deviceManager;

        /// <summary>
        /// 初始化SIP客户端
        /// </summary>
        /// <param name="homeIp">本机IP</param>
        /// <param name="homePort">本机端口</param>
        /// <param name="sipId"></param>
        public SipUdpClient(IPEndPoint? local = null, string sipId = "34020000002000000001")
        {
            logger = factory.CreateLogger("Device");

            var address = util.IPAddressHelper.GetIPV4Adress();
            _localEndPoint = local ?? new IPEndPoint(IPAddress.Parse(address ?? "127.0.0.1") , 50001);
            _sipId = sipId;
            _deviceManager = new DeviceManager();

            _transport = new SIPTransport();
            _transport.SIPRequestOutTraceEvent += Transport_SIPRequestOutTraceEvent;
            _transport.SIPResponseInTraceEvent += Transport_SIPResponseInTraceEvent;
            _transport.SIPTransportRequestReceived += Transport_SIPTransportRequestReceived;

            _channel = new SIPUDPChannel(_localEndPoint.Address, _localEndPoint.Port);
            _transport.AddSIPChannel(_channel);
        }

        public void AddDevice(Device device)
        {
            if (device == null)
            {
                return;
            }
            _deviceManager.AddDevice(device);
        }

        public Channel? GetChannelById(string channelId)
        {
            if (string.IsNullOrEmpty(channelId))
            {
                throw new ApplicationException("通道id不能为空！");
            }

            List<Device>? devices = _deviceManager?.GetAllDevices();

            if (devices == null)
            {
                return null;
            }

            foreach (Device device in devices)
            {
                var channels = device.Channels;
                Channel? channel = channels.FirstOrDefault(c => c.ChannelId.Equals(channelId));
                if (channel != null)
                {
                    return channel;
                }
            }
            return null;
        }


        /// <summary>
        /// 连接服务端
        /// </summary>
        /// <param name="server"></param>
        public void Connect(IPEndPoint? server)
        {
            try
            {
                _remoteEndPoint = server ?? new IPEndPoint(address: _localEndPoint?.Address ?? IPAddress.Any, 15060);

                // 注册所有设备
                var deviceList = _deviceManager.GetAllDevices();

                if (_remoteEndPoint == null)
                {
                    throw new("服务端地址不正确，请重试!");
                }

                deviceList?.ForEach((device) =>
                {
                    var serverAddress = _remoteEndPoint.Address;
                    var serverPort = _remoteEndPoint.Port;

                    var userAgent = new SIPRegistrationUserAgent(
                    _transport,
                    null,
                    new SIPURI(device?.Username, $"{device?.HomeIp}:{device?.HomePort}", null, SIPSchemesEnum.sip, SIPProtocolsEnum.udp),
                    null,
                    device?.Password,
                    _realm,
                    $"{serverAddress}:{serverPort}",
                    new SIPURI(SIPSchemesEnum.sip, serverAddress, serverPort),
                    device?.Expiry ?? 120,
                    null);

                    userAgent.Start();
                    userAgent.Stop();
                });

                // 注册完成后启动心跳服务
                if (_keepLiveToken == null)
                {
                    _keepLiveToken = new CancellationTokenSource();
                    _keepLiveTask = Task.Factory.StartNew(MainKeepLiveLoop, _keepLiveToken.Token);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }


        /// <summary>
        /// 获取设备目录?
        /// </summary>
        public async void GetDeviceCatalog()
        {
            SIPURI srcUri = new(_clientId, $"{_localEndPoint?.Address}:{_localEndPoint?.Port}", null, SIPSchemesEnum.sip, SIPProtocolsEnum.udp);

            SIPURI dstUri = new(_sipId, $"{_remoteEndPoint?.Address}:{_remoteEndPoint?.Port}", null);
            // 构建Message消息
            SIPRequest request = SIPRequest.GetRequest(SIPMethodsEnum.MESSAGE, dstUri, new SIPToHeader(null, dstUri, null), new SIPFromHeader(null, srcUri, CallProperties.CreateNewTag()));

            string body = "<?xml version=\"1.0\"?>" + SDP.CRLF
                + "<Query>" + SDP.CRLF
                + "<CmdType>Catalog</CmdType>" + SDP.CRLF
                + "<SN>1</SN>" + SDP.CRLF
                + $"<DeviceID>{_sipId}</DeviceID>" + SDP.CRLF 
                + "</Query>";

            // 发送消息
            request.Body = body;

            await _transport.SendRequestAsync(request);
        }

        /// <summary>
        /// 查询设备是否存在
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public bool CheckDeviceExistStatus(string username)
        {

            return false;
        }

        /// <summary>
        /// 保活机制
        /// </summary>
        /// <returns></returns>
        public async Task MainKeepLiveLoop()
        {
            try
            {
                var rad = new Random();
                while (_keepLiveToken != null && !_keepLiveToken.IsCancellationRequested)
                {
                    foreach (var item in _deviceManager.GetAllDevices())
                    {
                        var device = item;

                        SIPURI srcUri = new(device.Username, $"{device.HomeIp}:{device.HomePort}", null, SIPSchemesEnum.sip, SIPProtocolsEnum.udp);

                        SIPURI dstUri = new("34020000002000000001", $"{_remoteEndPoint?.Address}:{_remoteEndPoint?.Port}", null);

                        SIPRequest request = SIPRequest.GetRequest(SIPMethodsEnum.MESSAGE, dstUri, new SIPToHeader(null, dstUri, null), new SIPFromHeader(null, srcUri, CallProperties.CreateNewTag()));

                        string body = "<?xml version=\"1.0\"?>" + SDP.CRLF
                            + "<Notify>" + SDP.CRLF
                            + "<CmdType>Keepalive</CmdType>" + SDP.CRLF
                            + $"<SN>{rad.Next(10000)}</SN>" + SDP.CRLF
                            + $"<DeviceID>{device.Username}</DeviceID>" + SDP.CRLF
                            + "<Status>OK</Status>" + SDP.CRLF
                            + "</Notify>";

                        request.Body = body;

                        await _transport.SendRequestAsync(request);
                    }
                    Thread.Sleep(60 * 1000);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"[{System.Reflection.MethodBase.GetCurrentMethod()?.Name}]：" + ex.Message);
                Debug.WriteLine(ex.Message);
                _keepLiveToken.Cancel();
                await _keepLiveTask;
            }
        }

        /// <summary>
        /// 处理服务端发送的请求
        /// </summary>
        /// <param name="localSIPEndPoint"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="sipRequest"></param>
        /// <returns></returns>
        private async Task Transport_SIPTransportRequestReceived(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest sipRequest)
        {
            Debug.WriteLine($"receive message: {sipRequest}");

            if (sipRequest.Method == SIPMethodsEnum.MESSAGE)
            {
                XmlDocument document = new();
                document.LoadXml(sipRequest.Body);
                XmlElement? root = document.DocumentElement;

                // 命令类型
                XmlNode? cmdType = document.GetElementsByTagName("CmdType").Item(0);

                if (cmdType == null)
                {
                    return;
                }

                if (root != null && root.Name.Equals(SIPMessageXMLType.QUERY) && cmdType.InnerText.Equals(SIPMessageCMDType.CATALOG))
                {
                    // 发送设备信息
                    await SendDeviceInfoAsync(document, sipRequest);
                }
            }
            else if (sipRequest.Method == SIPMethodsEnum.INVITE)
            {
                // 接受推流邀请
                await AcceptPushStreamInviteAsync(sipRequest);
            }

            return;
        }

        /// <summary>
        /// 发送设备信息
        /// </summary>
        /// <param name="document"></param>
        /// <param name="sipRequest"></param>
        /// <returns></returns>
        private async Task SendDeviceInfoAsync(XmlDocument document, SIPRequest sipRequest)
        {
            try
            {
                XmlNodeList node = document.GetElementsByTagName("DeviceID");

                Device? device;

                if (node == null) return;

                string? deviceId = node.Item(0)?.InnerText;

                device = _deviceManager.GetDevice(deviceId);

                if (device == null) return;

                var nodeList = document.GetElementsByTagName("SN");

                var SN = nodeList.Item(0)?.InnerText;

                var srcUri = new SIPURI(device.Username, $"{device.HomeIp}:{device.HomePort}", null, SIPSchemesEnum.sip, SIPProtocolsEnum.udp);

                var dstUri = new SIPURI("34020000002000000001", $"{_remoteEndPoint.Address} : {_remoteEndPoint.Port}", null);

                var messageRequest = SIPRequest.GetRequest(SIPMethodsEnum.MESSAGE, dstUri, new SIPToHeader(null, dstUri, null), new SIPFromHeader(null, srcUri, CallProperties.CreateNewTag()));

                messageRequest.Body = "<?xml version=\"1.0\"?>" + SDP.CRLF
                    + "<Response>" + SDP.CRLF
                    + "<CmdType>Catalog</CmdType>" + SDP.CRLF
                    + $"<SN>{SN}</SN>" + SDP.CRLF
                    + $"<DeviceID>{device.Username}</DeviceID>" + SDP.CRLF
                    + "<SumNum>1</SumNum>" + SDP.CRLF
                    + "<DeviceList Num=\"1\">" + SDP.CRLF;

                List<Channel> channels = device.Channels;

                if (channels == null || channels.Count == 0)
                {
                    throw new ApplicationException("设备尚未初始化通道！");
                }

                foreach ( Channel channel in channels )
                {
                    messageRequest.Body += "<Item>" + SDP.CRLF
                    + $"<DeviceID>{channel.ChannelId}</DeviceID>" + SDP.CRLF
                    + "<Name>IPC</Name>" + SDP.CRLF
                    + "<Manufacturer>ABCD</Manufacturer>" + SDP.CRLF
                    + "<Model>TEST001</Model>" + SDP.CRLF
                    + "<Owner>Owner1</Owner>" + SDP.CRLF
                    + "<CivilCode>CivilCode1</CivilCode>" + SDP.CRLF
                    + "<Block>Block1</Block>" + SDP.CRLF
                    + "<Address>Address1</Address>" + SDP.CRLF
                    + "<Parental>0</Parental>" + SDP.CRLF
                    + $"<ParentID>{device.Username}</ParentID>" + SDP.CRLF
                    + "<SafetyWay>0</SafetyWay>" + SDP.CRLF
                    + "<RegisterWay>1</RegisterWay>" + SDP.CRLF
                    + "<CertNum>CertNum1</CertNum>" + SDP.CRLF
                    + "<Certifiable>0</Certifiable>" + SDP.CRLF
                    + "<ErrCode>400</ErrCode>" + SDP.CRLF
                    + "<EndTime>2050-12-31T23:59:59</EndTime>" + SDP.CRLF
                    + "<Secrecy>0</Secrecy>" + SDP.CRLF
                    + $"<IPAddress>192.168.201.166</IPAddress>" + SDP.CRLF
                    + $"<Port>{sipRequest.LocalSIPEndPoint.Port}</Port>" + SDP.CRLF
                    + $"<Password>{device.Password}</Password>" + SDP.CRLF
                    + "<Status>OK</Status>" + SDP.CRLF
                    + "<Longitude></Longitude>" + SDP.CRLF
                    + "<Latitude></Latitude>" + SDP.CRLF
                    + "</Item>" + SDP.CRLF;
                }

                messageRequest.Body += "</DeviceList>" + SDP.CRLF
                    + "</Response>";

                await _transport.SendRequestAsync(messageRequest);
            }
            catch (Exception ex)
            {
                logger.LogError($"[{System.Reflection.MethodBase.GetCurrentMethod}]：" + ex.Message);
            }
        }

        private async Task AcceptPushStreamInviteAsync(SIPRequest sipRequest)
        {
            try
            {
                var responseSDP = SDP.ParseSDPDescription(sipRequest.Body);

                Channel? channel = GetChannelById(responseSDP.Username);

                if (channel == null)
                {
                    throw new ApplicationException("通道不存在！");
                }

                var media = responseSDP.Media;

                channel.PushPort = media[0].Port;

                var dstUri = new SIPURI("34020000002000000001", $"{_remoteEndPoint?.Address} : {_remoteEndPoint?.Port}", null);

                SIPResponse response = SIPResponse.GetResponse(sipRequest, SIPResponseStatusCodesEnum.Ok, null);

                var sdp = new SDP
                {
                    Version = SDP.SDP_PROTOCOL_VERSION,
                    Username = responseSDP.Username,
                    NetworkType = "IN",
                    AddressType = SDP.ADDRESS_TYPE_IPV4,
                    URI = _localEndPoint?.Address + ":" + _localEndPoint?.Port,
                    SessionId = Crypto.GetRandomInt(5).ToString(),
                    Connection = new SDPConnectionInformation(_remoteEndPoint?.Address),
                    SessionDescription = "My Video Conference",
                    SessionName = "Play",
                    Media = media
                };

                response.Body = sdp.ToString();

                await _transport.SendResponseAsync(dstUri.ToSIPEndPoint(), response);

                if (string.IsNullOrEmpty(channel.PushSource))
                {
                    throw new ApplicationException("设备推流地址尚未初始化！");
                }

                FFmpegHelper.RegisterFFmpegBinaries();
                Pusher pusher = new();
                pusher.InitDecoder(channel.PushSource);
                await pusher.PushStreamAsync($"rtp://{_remoteEndPoint.Address}:{channel.PushPort}");
                //await pusher.PushPsStreamAsync(_serverIp, channel.PushPort);
            }
            catch (Exception ex)
            {
                logger.LogError($"[{System.Reflection.MethodBase.GetCurrentMethod}]：" + ex.Message);
            }
        }

        #region 传输(请求/响应)追踪
        /// <summary>
        /// 传输响应跟踪（用于追溯）
        /// </summary>
        /// <param name="localSIPEndPoint"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="sipResponse"></param>
        private void Transport_SIPResponseInTraceEvent(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPResponse sipResponse)
        {
            Debug.WriteLine($"收到响应：{remoteEndPoint}->{localSIPEndPoint}: {sipResponse.Status}");
        }

        /// <summary>
        /// 传输请求跟踪（用于追溯）
        /// </summary>
        /// <param name="localSIPEndPoint"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="sipRequest"></param>
        private void Transport_SIPRequestOutTraceEvent(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest sipRequest)
        {
            Debug.WriteLine($"发出请求：{localSIPEndPoint}->{remoteEndPoint}: {sipRequest.StatusLine}");
        }
        #endregion

        #region 注册相关回调，用于跟踪注册流程
        /// <summary>
        /// 注册成功回调
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="response"></param>
        private void UserAgent_RegistrationSuccessful(SIPURI uri, SIPResponse response)
        {
            //GenerateChannel();
            logger.LogInformation($"{response}");
            Debug.WriteLine($"{response}");
        }

        /// <summary>
        /// 注册已删除回调
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="response"></param>
        private void UserAgent_RegistrationRemoved(SIPURI uri, SIPResponse response)
        {
            Debug.WriteLine($"{uri} registration failed.");
        }

        /// <summary>
        /// 注册失败
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="response"></param>
        /// <param name="msg"></param>
        private void UserAgent_RegistrationTemporaryFailure(SIPURI uri, SIPResponse response, string msg)
        {
            Debug.WriteLine($"{uri}: {msg}");
        }

        /// <summary>
        /// 注册失败
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="response"></param>
        /// <param name="err"></param>
        private void UserAgent_RegistrationFailed(SIPURI uri, SIPResponse response, string err)
        {
            logger.LogError($"{uri}: {response},{err}");
            Debug.WriteLine($"{uri}: {response},{err}");
        }
        #endregion
    }
}
