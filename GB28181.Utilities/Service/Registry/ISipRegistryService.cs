using GB28181.Utilities.Models;
using SIPSorcery.SIP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.Utilities.Service.Registry
{
    public interface ISipRegistryService
    {
        /// <summary>
        /// 注册设备
        /// </summary>
        /// <param name="server">服务端地址</param>
        /// <param name="realm">域</param>
        void RegistryAllDevices(string realm = "");

        /// <summary>
        /// 获取设备注册状态
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        bool GetDeviceRegistryStatusByUsername(string username);
    }
}
