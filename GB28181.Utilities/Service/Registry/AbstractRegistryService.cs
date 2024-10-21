using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.Utilities.Service.Registry
{
    public abstract class AbstractRegistryService : ISipRegistryService, IDisposable
    {
        /// <summary>
        /// 服务端的地址
        /// The address of the server
        /// </summary>
        public IPAddress DestinationAddress { get; protected set; }

        /// <summary>
        /// 服务端端口
        /// </summary>
        public int DesinationPort { get; protected set; }

        public IPEndPoint DesinationEndPoint 
        { 
            get
            {
                return new IPEndPoint(DestinationAddress, DesinationPort);
            }
        }

        /// <summary>
        /// 是否自动注册
        /// </summary>
        public bool IsAutoRegister { get; protected set; }

        public long AutoRegisterInterval { get; protected set; }

        public event SIPMessageTrace SIPRegisterMessageTrace;

        public Encoding DataEncoding { get; private set; }

        protected AbstractRegistryService(Encoding encoding)
        {
            DataEncoding = encoding ?? Encoding.UTF8;
        }

        protected AbstractRegistryService(): this(Encoding.UTF8)
        {
            
        }

        public abstract bool CheckRegistryStatus(string username);

        public abstract void RegistryAllDevice(string realm = "");

        public abstract void Dispose();
    }
}
