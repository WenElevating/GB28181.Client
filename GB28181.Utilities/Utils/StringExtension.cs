using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GB28181.Utilities.Utils
{
    public static class StringExtension
    {

        public static bool IsEmpty(this string buffer)
        {
            return string.IsNullOrEmpty(buffer) || string.IsNullOrWhiteSpace(buffer);
        }
    }
}
