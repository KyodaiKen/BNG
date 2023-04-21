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

            string methodName = "To" + typeof(T).ToString().Split('.')[1]; // System.Half -> ToHalf
            if (methodName == "ToSByte") methodName = "ToChar";

            bool isFloat;
            switch (sourceFormat)
            {
                case "System.Half":
                case "System.Single":
                case "System.Double":
                    isFloat = true;
                    break;
                default:
                    isFloat = false;
                    break;
            }

            int size = Marshal.SizeOf(typeof(T));
            bool switchEndian = targetToBeBigEndian != sourceIsBigEndian;
            MethodInfo method = typeof(BitConverter).GetMethod(methodName);

            var targetNumChannels = Helpers.GetNumChannels(targetColorSpace);
            var sourceNumChannels = Helpers.GetNumChannels(sourceColorSpace);

            object[] sChValues = new object[sourceNumChannels];

            byte[] outputBytes = new byte[sourceData.Length * targetNumChannels];
            if (sourceFormat == "System.SByte")
            {
                for (int i = 0; i < sourceData.Length; i ++)
                {
                    sChValues[j] = Rescale<T>(System.Convert.ToSByte(sourceData[i]));
                    ConvertColorSpace(sChValues, outputBytes, i, size, sourceColorSpace, targetColorSpace, isFloat);
                }
            }
            else
            {
                for (int i = 0; i < sourceData.Length; i ++)
                {
                    
                    for (int j = 0; j < sourceNumChannels; j++)
                    {
                        if (switchEndian) Array.Reverse(sourceData, (i * sourceNumChannels + j) * size, size);
                        sChValues[j] = Rescale<T>(method.Invoke(null, new object[] { sourceData, (i * sourceNumChannels + j) * size }));
                    }
                    ConvertColorSpace(sChValues, outputBytes, i * targetNumChannels, size, sourceColorSpace, targetColorSpace, isFloat);
                }
            }
            return outputBytes;
        }

        private static T Rescale<T>(object inValue) {
            object maxValue = inValue.GetType().InvokeMember("MaxValue", System.Reflection.BindingFlags.GetField, null, inValue, null);
            object newMaxValue = typeof(T).GetType().InvokeMember("MaxValue", System.Reflection.BindingFlags.GetField, null, typeof(T), null);
            return (dynamic)inValue / (decimal)maxValue * newMaxValue;
        }

        private static byte[] ConvertColorSpace(object[] inChValues, byte[] outputBytes, int i, int size, ColorSpace sourceColorSpace, ColorSpace targetColorSpace, bool isFloat)
        {
            if (isFloat)
            {

            }
            else
            {
                switch (targetColorSpace)
                {
                    case ColorSpace.GRAY:
                        {
                            var opacityFrac = 0f;
                            switch (sourceColorSpace)
                            {
                                case ColorSpace.GRAYA:
                                    outputBytes[i] = (byte)Math.Round((float)inChValues[0] * ((float)inChValues[1] / 256f));
                                    break;
                                case ColorSpace.RGB:
                                    outputBytes[i] = (byte)Math.Round((float)inChValues[0] * .3 + (float)inChValues[1] * .59 + (float)inChValues[2] * .11);
                                    break;
                                case ColorSpace.RGBA:
                                    opacityFrac = (float)inChValues[3] / 256f;
                                    outputBytes[i] = (byte)Math.Round((float)inChValues[0] * .3 + (float)inChValues[1] * .59 + (float)inChValues[2] * .11 * (float)inChValues[3]);
                                    break;
                                case ColorSpace.YCrCb:
                                    outputBytes[i] = (byte)inChValues[0]; // CRUDE! Just return Y
                                    break;
                                case ColorSpace.YCrCbA:
                                    outputBytes[i] = (byte)Math.Round((float)inChValues[0] * ((float)inChValues[1] / 256f)); // CRUDE! Just return Y
                                    break;
                                case ColorSpace.CMYK:
                                    outputBytes[i] = (byte)Math.Round((float)inChValues[0] * .25 + (float)inChValues[1] * .25 + (float)inChValues[2] * .25 + (float)inChValues[3] * .25); //CRUDE!
                                    break;
                                case ColorSpace.CMYKA:
                                    opacityFrac = (float)inChValues[4] / 256f;
                                    outputBytes[i] = (byte)Math.Round((float)inChValues[0] * .25 + (float)inChValues[1] * .25 + (float)inChValues[2] * .25 + (float)inChValues[3] * .25 * ); //CRUDE!
                                    break;
                            }
                        }
                        break;
                    case ColorSpace.GRAYA:
                        {
                            switch (sourceColorSpace)
                            {
                                case ColorSpace.GRAY:
                                    outputBytes[i] = (byte)inChValues[0];
                                    outputBytes[i + 1] = 0xFF; // Opacity cranked
                                    break;
                                case ColorSpace.RGB:
                                    outputBytes[i] = (byte)Math.Round((float)inChValues[0] * .3 + (float)inChValues[1] * .59 + (float)inChValues[2] * .11);
                                    outputBytes[i + 1] = 0xFF; // Opacity cranked
                                    break;
                                case ColorSpace.RGBA:
                                    outputBytes[i] = (byte)Math.Round((float)inChValues[0] * .3 + (float)inChValues[1] * .59 + (float)inChValues[2] * .11 * (float)inChValues[3]);
                                    outputBytes[i + 1] = (byte)inChValues[3]; //Copy alpha channel
                                    break;
                                case ColorSpace.YCrCb:
                                    outputBytes[i] = (byte)inChValues[0]; // CRUDE! Just return Y
                                    outputBytes[i + 1] = 0xFF; // Opacity cranked
                                    break;
                                case ColorSpace.YCrCbA:
                                    outputBytes[i] = (byte)Math.Round((float)inChValues[0] * ((float)inChValues[1] / 256f)); // CRUDE! Just return Y
                                    break;
                                case ColorSpace.CMYK:
                                    outputBytes[i] = (byte)Math.Round((float)inChValues[0] * .25 + (float)inChValues[1] * .25 + (float)inChValues[2] * .25 + (float)inChValues[3] * .25); //CRUDE!
                                    break;
                                case ColorSpace.CMYKA:
                                    opacityFrac = (float)inChValues[4] / 256f;
                                    outputBytes[i] = (byte)Math.Round((float)inChValues[0] * .25 + (float)inChValues[1] * .25 + (float)inChValues[2] * .25 + (float)inChValues[3] * .25 * ); //CRUDE!
                                    break;
                            }
                        }
                        break;
                    case ColorSpace.RGB:

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
