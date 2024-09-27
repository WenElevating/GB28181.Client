using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.Utilities.Models
{
    public class Channel
    {
        // 通道id
        private string _channelId;

        public string PushSource { get; set; }

        public int PushPort { get; set; }

        public string ChannelId { get => _channelId; set => _channelId = value; }

        /// <summary>
        /// 创建通道
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="pushSource"></param>
        /// <param name="pushPort"></param>
        public Channel(string channelId, string pushSource)
        {
            _channelId = channelId;
            PushSource = pushSource;
        }


    }
}
