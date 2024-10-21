using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.Utilities.Models
{
    public abstract class IDevice
    {
        private static int _localDeviceID = 0;

        public string DeivceID { get; private set; }

        public IDevice()
        {
            Interlocked.Increment(ref  _localDeviceID);
            DeivceID = _localDeviceID.ToString();
        }

    }
}
