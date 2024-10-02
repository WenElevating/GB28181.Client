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

namespace GB28181.Utilities.Service.Registry
{
    public class SipRegistryService : ISipRegistryService
    {
        /// <summary>
        /// 设备管理服务
        /// </summary>
        private readonly IDeviceService _deviceService;

        //private bool _isEnable

        public SipRegistryService()
        {
            _deviceService = new DeviceService();
        }

        public void RegitryAllDevices(SIPTransport transport, IPEndPoint server, string realm = "")
        {
            try
            {
                var deviceList = _deviceService.GetAllDevices();

                // 校验设备是否已注册，检查在线状态

                deviceList?.ForEach((device) =>
                {
                    var serverAddress = server.Address;
                    var serverPort = server.Port;

                    var userAgent = new SIPRegistrationUserAgent(
                    transport,
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
                    userAgent.Stop();
                });
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine(ex);
                throw;
            }
        }
    }
}
