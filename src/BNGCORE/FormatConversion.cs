using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BNGCORE.FormatConversion
{
    public class BitmapDataFormat
    {
        public ColorSpace   ColorSpace { get; set; }
        public int          BitsPerChannel { get; set; }
        public PixelFormat  PixelFormat { get; set; }
        public bool         IsBigEndian { get; set; }
    }
    public static class BitmapConverter
    {
        public static bool TryConvert(in byte[] sourceData, BitmapDataFormat sourceFormat, out byte[] targetData, BitmapDataFormat targetFormat)
        {
            try
            {
                targetData = Convert(in sourceData, sourceFormat, targetFormat);
                return true;
            } catch
            {
                targetData = sourceData;
                return false;
            }
        }

        public static byte[] Convert(in byte[] sourceData, BitmapDataFormat sourceFormat, BitmapDataFormat targetFormat)
        {
            return Convert(sourceData
                         , sourceFormat.ColorSpace
                         , FormatToStringType(sourceFormat)
                         , sourceFormat.IsBigEndian
                         , targetFormat.ColorSpace
                         , FormatToStringType(targetFormat)
                         , targetFormat.IsBigEndian
            );
        }
        public static byte[] Convert(in byte[] sourceData, ColorSpace sourceColorSpace, string sourceFormat, bool sourceIsBigEndian, ColorSpace targetColorSpace, string targetFormat, bool targetToBeBigEndian)
        {
            if (sourceFormat == "failed" || targetFormat == "failed")
                throw new Exception("Unknown error ocurred!");

            switch (targetFormat)
            {
                case "System.SByte":
                    return Convert<SByte>(sourceData, sourceColorSpace, sourceFormat, sourceIsBigEndian, targetColorSpace, targetToBeBigEndian);
                case "System.Int16":
                    return Convert<Int16>(sourceData, sourceColorSpace, sourceFormat, sourceIsBigEndian, targetColorSpace, targetToBeBigEndian);
                case "System.Int32":
                    return Convert<Int32>(sourceData, sourceColorSpace, sourceFormat, sourceIsBigEndian, targetColorSpace, targetToBeBigEndian);
                case "System.Int64":
                    return Convert<Int64>(sourceData, sourceColorSpace, sourceFormat, sourceIsBigEndian, targetColorSpace, targetToBeBigEndian);
                case "System.Byte":
                    return Convert<Byte>(sourceData, sourceColorSpace, sourceFormat, sourceIsBigEndian, targetColorSpace, targetToBeBigEndian);
                case "System.UInt16":
                    return Convert<UInt16>(sourceData, sourceColorSpace, sourceFormat, sourceIsBigEndian, targetColorSpace, targetToBeBigEndian);
                case "System.UInt32":
                    return Convert<UInt32>(sourceData, sourceColorSpace, sourceFormat, sourceIsBigEndian, targetColorSpace, targetToBeBigEndian);
                case "System.UInt64":
                    return Convert<UInt64>(sourceData, sourceColorSpace, sourceFormat, sourceIsBigEndian, targetColorSpace, targetToBeBigEndian);
                case "System.Half":
                    return Convert<Half>(sourceData, sourceColorSpace, sourceFormat, sourceIsBigEndian, targetColorSpace, targetToBeBigEndian);
                case "System.Single":
                    return Convert<Single>(sourceData, sourceColorSpace, sourceFormat, sourceIsBigEndian, targetColorSpace, targetToBeBigEndian);
                case "System.Double":   
                    return Convert<Double>(sourceData, sourceColorSpace, sourceFormat, sourceIsBigEndian, targetColorSpace, targetToBeBigEndian);
            }
            return null;
        }

        public static byte[] Convert<T>(in byte[] sourceData, ColorSpace sourceColorSpace, string sourceFormat, bool sourceIsBigEndian, ColorSpace targetColorSpace, bool targetToBeBigEndian)
        {
            //Check if conversion is needed
            if (sourceFormat == typeof(T).ToString() && sourceIsBigEndian == targetToBeBigEndian)
                throw new ArgumentException("No format change detected");

            bool convertColorSpace = sourceColorSpace != targetColorSpace;

            string methodName = "To" + typeof(T).ToString().Split('.')[1]; // System.Half -> ToHalf
            if (methodName == "ToSByte") methodName = "ToChar";

            bool isFloat = false;
            int targetSize = 0;
            ulong maxVal = 0;
            switch (sourceFormat)
            {
                case "System.Half":
                    isFloat = true;
                    break;
                case "System.Single":
                    isFloat = true;
                    break;
                case "System.Double":
                    isFloat = true;
                    break;
            }

            switch (typeof(T).ToString())
            {
                case "System.SByte":
                    targetSize = 1;
                    maxVal = byte.MaxValue;
                    break;
                case "System.Int16":
                    targetSize = 2;
                    maxVal = (ulong)short.MaxValue;
                    break;
                case "System.Int32":
                    targetSize = 4;
                    maxVal = int.MaxValue;
                    break;
                case "System.Int64":
                    targetSize = 8;
                    maxVal = long.MaxValue;
                    break;
                case "System.Byte":
                    targetSize = 1;
                    maxVal = (ulong)sbyte.MaxValue;
                    break;
                case "System.UInt16":
                    targetSize = 2;
                    maxVal = ushort.MaxValue;
                    break;
                case "System.UInt32":
                    targetSize = 4;
                    maxVal = uint.MaxValue;
                    break;
                case "System.UInt64":
                    targetSize = 8;
                    maxVal = ulong.MaxValue;
                    break;
                case "System.Half":
                    targetSize = 2;
                    maxVal = 1;
                    isFloat = true;
                    break;
                case "System.Single":
                    targetSize = 4;
                    maxVal = 1;
                    isFloat = true;
                    break;
                case "System.Double":
                    targetSize = 8;
                    maxVal = 1;
                    isFloat = true;
                    break;
            }

            int size = Marshal.SizeOf(typeof(T));
            bool switchEndian = targetToBeBigEndian != sourceIsBigEndian;
            MethodInfo method = typeof(BitConverter).GetMethod(methodName);

            var targetNumChannels = Helpers.GetNumChannels(targetColorSpace);
            var sourceNumChannels = Helpers.GetNumChannels(sourceColorSpace);

            object[] sChValues = new object[sourceNumChannels];

            byte[] outputBytes = new byte[sourceData.Length * targetNumChannels * size];
            if (sourceFormat == "System.SByte")
            {
                for (int i = 0; i < sourceData.Length; i++)
                {
                    sChValues[0] = Rescale<T>(System.Convert.ToSByte(sourceData[i]));
                    if (convertColorSpace)
                        ConvertColorSpace(sChValues, outputBytes, i, size, sourceColorSpace, targetColorSpace, isFloat);
                    else
                        Array.Copy(sChValues, i, outputBytes, i , 1);
                }
            }
            else
            {
                for (int i = 0; i < sourceData.Length; i++)
                {
                    
                    for (int j = 0; j < sourceNumChannels; j++)
                    {
                        if (switchEndian) Array.Reverse(sourceData, (i * sourceNumChannels + j) * size, size);
                        sChValues[j] = Rescale<T>(method.Invoke(null, new object[] { sourceData, (i * sourceNumChannels + j) * size }));
                    }
                    ConvertColorSpace(sChValues, outputBytes, i * targetNumChannels * targetSize, size, sourceColorSpace, targetColorSpace, isFloat);
                }
            }
            return outputBytes;
        }

        private static T Rescale<T>(object inValue) {
            object maxValue = inValue.GetType().InvokeMember("MaxValue", System.Reflection.BindingFlags.GetField, null, inValue, null);
            object newMaxValue = typeof(T).GetType().InvokeMember("MaxValue", System.Reflection.BindingFlags.GetField, null, typeof(T), null);
            return (dynamic)inValue / (decimal)maxValue * newMaxValue;
        }


        public static T[] ConvertColorSpace<T>(T[] pixelIn, ColorSpace sourceColorSpace, ColorSpace targetColorSpace, bool isFloat, ulong maxValue)
        {
            T[] outPixel = new T[Helpers.GetNumChannels(targetColorSpace)];

            if (isFloat)
            {

            }
            else
            {
                switch (targetColorSpace)
                {
                    case ColorSpace.GRAY:
                        {
                            decimal opacityFrac = 0;
                            switch (sourceColorSpace)
                            {
                                case ColorSpace.GRAYA:
                                    outPixel[0] = (dynamic)Math.Round((decimal)(dynamic)pixelIn[0] * ((dynamic)pixelIn[1] / (decimal)maxValue));
                                    break;
                                case ColorSpace.RGB:
                                    outPixel[0] = (dynamic)Math.Round((decimal)(dynamic)pixelIn[0] * (decimal).3 + (dynamic)pixelIn[1] * (decimal).59 + (dynamic)pixelIn[2] * (decimal).11);
                                    break;
                                case ColorSpace.RGBA:
                                    opacityFrac = (dynamic)pixelIn[3] / maxValue;
                                    outPixel[0] = (dynamic)Math.Round((decimal)(dynamic)pixelIn[0] * (decimal).3 + (dynamic)pixelIn[1] * (decimal).59 + (dynamic)pixelIn[2] * (decimal).11 * (dynamic)pixelIn[3]);
                                    break;
                                case ColorSpace.YCrCb:
                                    outPixel[0] = (dynamic)pixelIn[0]; // CRUDE! Just return Y
                                    break;
                                case ColorSpace.YCrCbA:
                                    outPixel[0] = (dynamic)Math.Round((decimal)(dynamic)pixelIn[0] * ((dynamic)pixelIn[1] / maxValue)); // CRUDE! Just return Y
                                    break;
                                case ColorSpace.CMYK:
                                    outPixel[0] = (dynamic)Math.Round((decimal)(dynamic)pixelIn[0] * (decimal).25 + (dynamic)pixelIn[1] * (decimal).25 + (dynamic)pixelIn[2] * (decimal).25 + (dynamic)pixelIn[3] * (decimal).25); //CRUDE!
                                    break;
                                case ColorSpace.CMYKA:
                                    opacityFrac = (dynamic)pixelIn[4] / maxValue;
                                    outPixel[0] = (dynamic)Math.Round((decimal)(dynamic)pixelIn[0] * (decimal).25 + (dynamic)pixelIn[1] * (decimal).25 + (dynamic)pixelIn[2] * (decimal).25 + (dynamic)pixelIn[3] * (decimal).25 * opacityFrac); //CRUDE!
                                    break;
                            }
                        }
                        break;
                    case ColorSpace.GRAYA:
                        {
                            switch (sourceColorSpace)
                            {
                                case ColorSpace.GRAY:
                                    outPixel[0] = (dynamic)pixelIn[0];
                                    outPixel[1] = (dynamic)maxValue; // Opacity cranked
                                    break;
                                case ColorSpace.RGB:
                                    outPixel[0] = (dynamic)Math.Round((dynamic)pixelIn[0] * (decimal).3 + (dynamic)pixelIn[1] * (decimal).59 + (dynamic)pixelIn[2] * (decimal).11);
                                    outPixel[1] = (dynamic)maxValue; // Opacity cranked
                                    break;
                                case ColorSpace.RGBA:
                                    outPixel[0] = (dynamic)Math.Round((dynamic)pixelIn[0] * (decimal).3 + (dynamic)pixelIn[1] * (decimal).59 + (dynamic)pixelIn[2] * (decimal).11 * (dynamic)pixelIn[3]);
                                    outPixel[1] = (dynamic)pixelIn[3]; //Copy alpha channel
                                    break;
                                case ColorSpace.YCrCb:
                                    outPixel[0] = (dynamic)pixelIn[0]; // CRUDE! Just return Y
                                    outPixel[1] = (dynamic)maxValue; // Opacity cranked
                                    break;
                                case ColorSpace.YCrCbA:
                                    outPixel[0] = (dynamic)Math.Round((dynamic)pixelIn[0] * ((dynamic)pixelIn[1] / maxValue)); // CRUDE! Just return Y
                                    outPixel[1] = (dynamic)pixelIn[3]; //Copy alpha channel
                                    break;
                                case ColorSpace.CMYK:
                                    outPixel[0] = (dynamic)Math.Round((dynamic)pixelIn[0] * (decimal).25 + (dynamic)pixelIn[1] * (decimal).25 + (dynamic)pixelIn[2] * (decimal).25 + (dynamic)pixelIn[3] * (decimal).25); //CRUDE!
                                    outPixel[1] = (dynamic)maxValue; // Opacity cranked
                                    break;
                                case ColorSpace.CMYKA:
                                    outPixel[0] = (dynamic)Math.Round((dynamic)pixelIn[0] * (decimal).25 + (dynamic)pixelIn[1] * (decimal).25 + (dynamic)pixelIn[2] * (decimal).25 + (dynamic)pixelIn[3] * (decimal).25); //CRUDE!
                                    outPixel[1] = (dynamic)pixelIn[4]; //Copy alpha channel
                                    break;
                            }
                        }
                        break;
                    case ColorSpace.RGB:
                        {
                            decimal opacityFrac = 0;
                            switch (sourceColorSpace)
                            {
                                case ColorSpace.GRAY:
                                    outPixel[0] = (dynamic)pixelIn[0];
                                    outPixel[1] = (dynamic)pixelIn[0];
                                    outPixel[2] = (dynamic)pixelIn[0];
                                    break;
                                case ColorSpace.GRAYA:
                                    opacityFrac = (decimal)(dynamic)pixelIn[1] / maxValue;
                                    outPixel[0] = (dynamic)Math.Round((decimal)(dynamic)pixelIn[0] * opacityFrac);
                                    outPixel[1] = (dynamic)Math.Round((decimal)(dynamic)pixelIn[0] * opacityFrac);
                                    outPixel[2] = (dynamic)Math.Round((decimal)(dynamic)pixelIn[0] * opacityFrac);
                                    break;
                                case ColorSpace.RGBA:
                                    outPixel[0] = (dynamic)pixelIn[0];
                                    outPixel[1] = (dynamic)pixelIn[1];
                                    outPixel[2] = (dynamic)pixelIn[2];
                                    outPixel[3] = (dynamic)maxValue; // Opacity cranked
                                    break;
                                case ColorSpace.YCrCb:
                                    throw new Exception("TODO");
                                case ColorSpace.YCrCbA:
                                    throw new Exception("TODO");
                                    break;
                                case ColorSpace.CMYK:
                                    outPixel[0] = (dynamic)Math.Round(maxValue * (maxValue - (decimal)(dynamic)pixelIn[0]) * (maxValue - (decimal)(dynamic)pixelIn[3]));
                                    outPixel[1] = (dynamic)maxValue; // Opacity cranked
                                    break;
                                case ColorSpace.CMYKA:
                                    outPixel[0] = (dynamic)Math.Round((dynamic)pixelIn[0] * (dynamic).25 + (dynamic)pixelIn[1] * (dynamic).25 + (dynamic)pixelIn[2] * (dynamic).25 + (dynamic)pixelIn[3] * (dynamic).25 * ); //CRUDE!
                                    outPixel[1] = (dynamic)pixelIn[4]; //Copy alpha channel
                                    break;
                            }
                        }
                        break;
                    case ColorSpace.RGBA:

                        break;
                    case ColorSpace.YCrCb:

                        break;
                    case ColorSpace.YCrCbA:

                        break;
                    case ColorSpace.CMYK:

                        break;
                    case ColorSpace.CMYKA:

                        break;
                }
            }

            throw new NotImplementedException();
        }

        private static string FormatToStringType(BitmapDataFormat format)
        {
            switch (format.PixelFormat)
            {
                case PixelFormat.IntegerUnsigned:
                    if (format.BitsPerChannel != 8 || format.BitsPerChannel != 16 || format.BitsPerChannel != 32 || format.BitsPerChannel != 64)
                        throw new ArgumentException(string.Format("Only 8, 16, 32 or 64 bit are allowed for {0}!", format.PixelFormat.ToString()));

                    if (format.BitsPerChannel == 8)
                        return "System.Byte";

                    return "System.UInt" + format.BitsPerChannel;
                case PixelFormat.IntegerSigned:
                    if (format.BitsPerChannel != 8 || format.BitsPerChannel != 16 || format.BitsPerChannel != 32 || format.BitsPerChannel != 64)
                        throw new ArgumentException(string.Format("Only 8, 16, 32 or 64 bit are allowed for {0}!", format.PixelFormat.ToString()));

                    if (format.BitsPerChannel == 8)
                        return "System.SByte";

                    return "System.Int" + format.BitsPerChannel;
                case PixelFormat.FloatIEEE:
                    switch (format.BitsPerChannel)
                    {
                        case 16:
                            return "System.Half";
                        case 32:
                            return "System.Single";
                        case 64:
                            return "System.Double";
                        default:
                            throw new ArgumentException(string.Format("Only 8, 16, 32 or 64 bit are allowed for {0}!", format.PixelFormat.ToString()));
                    }
            }
            return "failed";
        }
    }
}
