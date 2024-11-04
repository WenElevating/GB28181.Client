using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.NET.Model
{
    public sealed class MediaServerSetting
    {
        [JsonProperty("host")]
        public string? Host {  get; set; }
    }
}
