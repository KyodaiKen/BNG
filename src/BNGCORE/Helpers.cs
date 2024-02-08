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
                case ColorSpace.GRAYA_Straight:
                case ColorSpace.GRAYA_PreMult:
                    return 2;
                case ColorSpace.RGB:
                case ColorSpace.YCrCb:
                    return 3;
                case ColorSpace.RGBA_Straight:
                case ColorSpace.RGBA_PreMult:
                case ColorSpace.CMYK:
                    return 4;
                case ColorSpace.CMYKA_Straight:
                case ColorSpace.CMYKA_PreMult:
                    return 5;
                default: return 1;
            }
        }
    }
}
