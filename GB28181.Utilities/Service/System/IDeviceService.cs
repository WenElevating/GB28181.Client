using GB28181.Utilities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.Utilities.Service.System
{
    public interface IDeviceService
    {
        /// <summary>
        /// 添加设备
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        int AddDevice(Device device);
        
        /// <summary>
        /// 更新设备
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        int UpdateDevice(Device device);
        
        /// <summary>
        /// 根据id获取设备
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Device? GetDeviceById(string id);


        /// <summary>
        /// 根据id删除设备
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        int DeleteDeviceById(string id);

        /// <summary>
        /// 获取所有设备
        /// </summary>
        /// <returns></returns>
        List<Device>? GetAllDevices();
    }
}
