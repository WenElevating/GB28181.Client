using GB28181.Utilities.Models;
using GB28181.Utilities.Service.System;
using GB28181.Utilities.Utils;
using SIPSorcery.SIP.App;
using SIPSorcery.SIP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;
using SIPSorcery.Net;

namespace GB28181.Utilities.Service.Registry
{
    public class SipRegistryService : AbstractRegistryService
    {
        /// <summary>
        /// This is a device management service
        /// </summary>
        private readonly IDeviceService _deviceService;

        /// <summary>
        /// This is a data transport
        /// </summary>
        private readonly SIPTransport _transport;

        /// <summary>
        /// This is a user agent cache queue
        /// </summary>
        private readonly ConcurrentDictionary<string, SIPRegistrationUserAgent> _agentDic;

        /// <summary>
        /// This is a server address
        /// </summary>
        private readonly IPEndPoint _server;

        /// <summary>
        /// heart beat token
        /// </summary>
        private CancellationTokenSource _deviceHeartBeatTokenSource;

        private Task _deviceHeartBeatTask;

        private bool disposedValue;

        public SipRegistryService(SIPTransport transport, IPEndPoint server, bool isAuto = false) : base(Encoding.UTF8)
        {
            DestinationAddress = server.Address;
            DesinationPort = server.Port;
            _deviceService = new DeviceService();
            _agentDic = [];
            _transport = transport;
            _server = server;

            // 启动心跳服务
            _deviceHeartBeatTokenSource = new CancellationTokenSource();
            _deviceHeartBeatTask = Task.Run(HeartBeatLoop);

            // 自动注册
            if (IsAutoRegister)
            {

            }
        }

        private async Task HeartBeatLoop()
        {
            try
            {
                var rad = new Random();
                while (!_deviceHeartBeatTokenSource.IsCancellationRequested)
                {
                    var devceList = _deviceService.GetAllDevices();

                    if (devceList is null || devceList.Count == 0)
                    {
                        await Task.Delay(1);
                        continue;
                    }

                    SIPURI srcUri;
                    SIPURI dstUri;
                    devceList.ForEach(async (device) =>
                    {
                        srcUri = new(device.Username, $"{device.HomeIp}:{device.HomePort}", null, SIPSchemesEnum.sip, SIPProtocolsEnum.udp);

                        dstUri = new("34020000002000000001", $"{_server?.Address}:{_server?.Port}", null);

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
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }

        private SIPRegistrationUserAgent CreateRegisterAgent(SIPTransport transport, Device? device, string realm)
        {
            return new SIPRegistrationUserAgent(
                        transport,
                        null,
                        new SIPURI(device?.Username, $"{device?.HomeIp}:{device?.HomePort}", null, SIPSchemesEnum.sip, SIPProtocolsEnum.udp),
                        null,
                        device?.Password,
                        realm,
                        $"{_server.Address}:{_server.Port}",
                        new SIPURI(SIPSchemesEnum.sip, _server.Address, _server.Port),
                        device?.Expiry ?? 120,
                        null);
        }

        public override void RegistryAllDevice(string realm = "")
        {
            try
            {
                var deviceList = _deviceService.GetAllDevices();

                if (deviceList is null || deviceList.Count == 0)
                {
                    return;
                }

                var serverAddress = _server.Address;
                var serverPort = _server.Port;
                
                deviceList.ForEach((device) =>
                {
                    if (!_agentDic.TryGetValue(device.Username, out SIPRegistrationUserAgent? value))
                    {
                        var userAgent = CreateRegisterAgent(_transport, device, realm);
                        _agentDic.TryAdd(device.Username, userAgent);
                        userAgent.Start();
                    }
                    else
                    {
                        if (value != null && !value.IsRegistered) 
                        {
                            value.Start();
                        }
                    }
                });
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine(ex);
                throw;
            }
        }

        public override bool CheckRegistryStatus(string username)
        {
            if (username.IsEmpty())
            { 
                throw new ArgumentNullException("username is null!");
            }

            if (!_agentDic.TryGetValue(username, out SIPRegistrationUserAgent? value))
            {
                throw new ArgumentNullException("this device is not exist!");
            }

            return value.IsRegistered;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    var deviceList = _deviceService.GetAllDevices();

                    // 停止注册服务
                    if (deviceList is not null && deviceList.Count > 0)
                    {
                        deviceList.ForEach((device) => 
                        {
                            if (device is null || string.IsNullOrEmpty(device.Username))
                            {
                                return;
                            }
                            _agentDic.TryGetValue(device.Username, out var userAgent);
                            userAgent?.Stop();
                        });
                    }
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
