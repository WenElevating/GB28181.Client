using Microsoft.Extensions.Logging;
using SIPSorcery.SIP.App;
using SIPSorcery.SIP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using SIPSorcery.Net;
using SIPSorcery.Sys;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using FFmpeg.AutoGen;
using System.Runtime.InteropServices;
using SIPSorceryMedia.Abstractions;
using SIPSorcery.net.RTP;
using System.Xml;

namespace GB28181.Utilities
{
    public class Device
    {
        // 本机IP
        private string _homeIp;

        // 本机端口
        private int _homePort;

        // 授权信息：设备id
        private string _username;

        // 授权信息：密码（由服务端设置）
        private string _password;

        // sip域
        private string _realm;

        // 有效期
        private int _expiry;

        // ssrc
        public string ssrc = "12345678";

        public string Username { get => _username; }

        public string Password { get => _password; }

        public string HomeIp { get => _homeIp; }

        public int HomePort { get => _homePort; }

        public int Expiry { get => _expiry; }

        public List<Channel> Channels { get; set; }

        private Device(string username, string password, string homeIp, int homePort,string realm = "3402000000", int expiry = 120)
        {
            _username = username;
            _password = password;
            _homeIp = homeIp;
            _homePort = homePort;
            _realm = realm;
            _expiry = expiry;
            Channels = new List<Channel>();
        }

        private Device(string username, string password, string homeIp, int homePort, List<Channel> channels, string realm = "3402000000", int expiry = 120)
        {
            _username = username;
            _password = password;
            _homeIp = homeIp;
            _homePort = homePort;
            _realm = realm;
            _expiry = expiry;
            Channels = channels;
        }

        /// <summary>
        /// 创建设备
        /// </summary>
        /// <param name="username">设备名</param>
        /// <param name="password">密码</param>
        /// <param name="homeIp">本机IP</param>
        /// <param name="homePort">本机端口</param>
        /// <param name="serverIp">服务端IP</param>
        /// <param name="serverPort">服务端端口</param>
        /// <param name="channelId">通道Id</param>
        /// <param name="realm">域</param>
        /// <param name="expiry">有效期</param>
        /// <returns></returns>
        public static Device CreateDevice(string username, string password, string homeIp, int homePort,string realm = "3402000000", int expiry = 120)
        {
            return new Device(username, password, homeIp, homePort, realm ,expiry);
        }

        public static Device CreateDevice(string username, string password, string homeIp, int homePort, List<Channel> channels, string realm = "3402000000", int expiry = 120)
        {
            return new Device(username, password, homeIp, homePort, channels, realm, expiry);
        }

        /// <summary>
        /// 添加通道
        /// </summary>
        /// <param name="ChannelId"></param>
        /// <param name="PushSource"></param>
        public void AddChannel(string ChannelId, string PushSource)
        {
            if (string.IsNullOrEmpty(ChannelId) || string.IsNullOrEmpty(PushSource))
            {
                throw new ApplicationException("添加通道失败!");
            }

            Channels.Add(new Channel(ChannelId, PushSource));
        }
    }
}
