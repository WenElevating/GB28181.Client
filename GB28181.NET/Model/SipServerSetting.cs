using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.NET.Model
{
    public sealed class SipServerSetting
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("realm")]
        public string? Realm { get; set; }

        [JsonProperty("host")]
        public string? Host { get; set; }

        [JsonProperty("port")]
        public int? Port { get; set; }

        [JsonProperty("password")]
        public string? Password { get; set; }
    }
}
