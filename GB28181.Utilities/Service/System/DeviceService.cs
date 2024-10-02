using GB28181.Utilities.Models;
using GB28181.Utilities.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.Utilities.Service.System
{
    public class DeviceService : IDeviceService
    {
        private readonly DeviceManager _deviceManager;

        public DeviceService()
        {
            _deviceManager = DeviceManager.GetInstance();
        }

        public int AddDevice(Device device)
        {
            try
            {
                _deviceManager.AddDevice(device);
                return 1;
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine(ex);
            }
            return 0;
        }

        public int DeleteDeviceById(string id)
        {
            try
            {
                _deviceManager.RemoveDevice(id);
                return 1;
            }
            catch (Exception ex) 
            {
                Debug.WriteLine(ex);
            }
            return 0;
        }

        public List<Device>? GetAllDevices()
        {

            return _deviceManager.GetAllDevices();
        }

        public Device? GetDeviceById(string id)
        {
            return _deviceManager.GetDevice(id);
        }

        public int UpdateDevice(Device device)
        {
            throw new NotImplementedException();
        }
    }
}
