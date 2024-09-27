using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.Utilities.Utils
{
    public class IPAddressHelper
    {
        public static async Task<string> GetIPV4AdressAsync()
        {
            IPAddress? address = (await Dns.GetHostAddressesAsync(Dns.GetHostName()))
                .FirstOrDefault(item => item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && item.ToString().StartsWith("192"));
            return address?.ToString() ?? "";
        }

        public static string? GetIPV4Adress() 
        { 
            IPAddress? address = Dns.GetHostAddresses(Dns.GetHostName())
                .FirstOrDefault(item => item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && item.ToString().StartsWith("192"));
            return address?.ToString();
        }
    }
}
