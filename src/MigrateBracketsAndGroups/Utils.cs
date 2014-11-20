using System;
using System.Collections.Generic;
using System.Linq;

namespace LxTools.Carno
{
    static class MoreExtensionMethods
    {
        public static int TryParseAsInt(this string s, int defaultValue)
        {
            int result;
            if (!int.TryParse(s, out result))
                result = defaultValue;
            return result;
        }
    }
}
