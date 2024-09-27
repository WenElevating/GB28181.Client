using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.Utilities.Models
{
    public abstract class BaseRegistryAgent
    {
        /// <summary>
        /// 本机地址
        /// </summary>
        public IPEndPoint? LocalEndPoint { get; protected set; }

        /// <summary>
        /// 服务端地址
        /// </summary>
        public IPEndPoint? RemoteEndPoint { get; protected set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string? Username { get; protected set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string? Password { get; protected set; }
    }
}
