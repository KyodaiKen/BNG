using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BNGCORE
{
    public static class Helpers
    {
        public static int GetNumChannels(ColorSpace pixelFormat)
        {
            switch (pixelFormat)
            {
                case ColorSpace.GRAY:
                    return 1;
                case ColorSpace.GRAYA:
                    return 2;
                case ColorSpace.RGB:
                case ColorSpace.YCrCb:
                    return 3;
                case ColorSpace.RGBA:
                case ColorSpace.CMYK:
                    return 4;
                case ColorSpace.CMYKA:
                    return 5;
                default: return 1;
            }
        }
    }
}
