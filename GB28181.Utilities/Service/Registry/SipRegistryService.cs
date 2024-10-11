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
    public class SipRegistryService : ISipRegistryService, IDisposable
    {
        /// <summary>
        /// 设备管理服务
        /// </summary>
        private readonly IDeviceService _deviceService;

        private readonly SIPTransport _transport;

        private readonly ConcurrentDictionary<string, SIPRegistrationUserAgent> _agentDic;

        private readonly IPEndPoint _server;

        private CancellationTokenSource _deviceHeartBeatTokenSource;

        private Task _deviceHeartBeatTask;

        private bool disposedValue;

        public SipRegistryService(SIPTransport transport, IPEndPoint server)
        {
            _deviceService = new DeviceService();
            _agentDic = [];
            _transport = transport;
            _server = server;

            // 启动心跳服务
            _deviceHeartBeatTokenSource = new CancellationTokenSource();
            _deviceHeartBeatTask = Task.Run(HeartBeatLoop);
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

                    devceList.ForEach(async (device) =>
                    {
                        SIPURI srcUri = new(device.Username, $"{device.HomeIp}:{device.HomePort}", null, SIPSchemesEnum.sip, SIPProtocolsEnum.udp);

                        SIPURI dstUri = new("34020000002000000001", $"{_server?.Address}:{_server?.Port}", null);

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

        public void RegisterDevices(string realm = "")
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
                    if (!_agentDic.ContainsKey(device.Username))
                    {
                        var userAgent = new SIPRegistrationUserAgent(
                        _transport,
                        null,
                        new SIPURI(device?.Username, $"{device?.HomeIp}:{device?.HomePort}", null, SIPSchemesEnum.sip, SIPProtocolsEnum.udp),
                        null,
                        device?.Password,
                        realm,
                        $"{serverAddress}:{serverPort}",
                        new SIPURI(SIPSchemesEnum.sip, serverAddress, serverPort),
                        device?.Expiry ?? 120,
                        null);

                        userAgent.Start();
                        _agentDic.TryAdd(device.Username, userAgent);
                    }
                    else
                    {
                        var agent = _agentDic[device.Username];
                        if (agent != null && !agent.IsRegistered) 
                        { 
                            agent.Start();
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

        public bool GetDeviceRegistryStatusByUsername(string username)
        {
            if (username.IsEmpty())
            { 
                throw new ArgumentNullException("username is null!");
            }

            if (!_agentDic.ContainsKey(username))
            {
                throw new ArgumentNullException("this device is not exist!");
            }

            return _agentDic[username].IsRegistered;
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

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~SipRegistryService()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


    }
}
