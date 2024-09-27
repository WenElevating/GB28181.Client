using GB28181.Utilities.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.Utilities.Utils
{
    public class DeviceManager
    {
        private readonly ConcurrentDictionary<string, Device> s_deivce_list = new();

        public DeviceManager() { }

        /// <summary>
        /// 获取设备数量
        /// </summary>
        /// <returns></returns>
        public int GetDeviceCount()
        {
            return s_deivce_list.Count;
        }


        /// <summary>
        /// 添加设备
        /// </summary>
        /// <param name="device">设备</param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public void AddDevice(Device device)
        {
            if (device == null)
            {
                throw new ApplicationException("设备未初始化！");
            }

            if (device.Username.IsEmpty() || s_deivce_list.ContainsKey(device.Username))
            {
                Debug.WriteLine("设备已存在！");
                return;
            }

            s_deivce_list.TryAdd(device.Username, device);
        }

        /// <summary>
        /// 获取设备
        /// </summary>
        /// <param name="username">设备标识符</param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public Device? GetDevice(string? username)
        {
            if (username.IsEmpty() || !s_deivce_list.ContainsKey(username))
            {
                throw new ApplicationException("设备不存在！");
            }

            s_deivce_list.TryGetValue(username, out Device? device);

            return device;
        }


        /// <summary>
        /// 删除设备
        /// </summary>
        /// <param name="channelId">设备标识符</param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public bool RemoveDevice(string channelId)
        {
            if (channelId.IsEmpty() || !s_deivce_list.ContainsKey(channelId))
            {
                throw new ApplicationException("设备不存在！");
            }

            Device? device = GetDevice(channelId);

            if (device == null)
            {
                return true;
            }

            return s_deivce_list.TryRemove(new KeyValuePair<string, Device>(channelId, device));
        }

        /// <summary>
        /// 更新设备，如果设备存在则更新，不存在则新增
        /// </summary>
        /// <param name="device">设备</param>
        /// <returns></returns>
        public bool UpdateOrAddDevice(Device device)
        {
            if (device == null)
            {
                throw new ApplicationException("设备未初始化！");
            }

            s_deivce_list.AddOrUpdate(device.Username, device, (key, value) => device);

            return true;
        }

        /// <summary>
        /// 获取所有设备
        /// </summary>
        /// <returns></returns>
        public List<Device>? GetAllDevices()
        {
            if (s_deivce_list.IsEmpty)
            {
                return null;
            }
            return [.. s_deivce_list.Values];
        }
    }
}
