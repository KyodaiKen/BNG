using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BNGCORE.Compositing
{
    public static class Normal
    {
        public static void Compose(in FrameHeader frame, in List<Layer> layerInfo, in List<byte[]> tileLayers, out byte[] compoundTile)
        {
            for (int l = 0; l < tileLayers.Count; l++)
            {
                compoundTile = new byte[frame.Width * frame.BytesPerPixel * frame.Height];

                //Convert the tile to the target pixel format first if needed


                byte[] compoundTileData = tileLayers[l];
                int bytesPerChannel = (int)layerInfo[l].BitsPerChannel / 8;

                switch (layerInfo[l].BitsPerChannel)
                {
                    case 8:
                        switch (layerInfo[l].PixelFormat)
                        {
                            case PixelFormat.IntegerUnsigned:
                                for (int i = 0; i < compoundTileData.Length; i++)
                                {
                                    var opacity = (float)layerInfo[l].Opacity;
                                    compoundTileData[i] = (byte)Math.Round(tileLayers[l][i] * opacity + compoundTileData[i] * (1 - opacity));
                                }
                                break;
                            case PixelFormat.IntegerSigned:
                                for (int i = 0; i < compoundTileData.Length; i++)
                                {
                                    var opacity = (float)layerInfo[l].Opacity;
                                    ushort lv = BitConverter.ToChar(new Span<byte>(tileLayers[l], i, 1));
                                    ushort cv = BitConverter.ToChar(new Span<byte>(compoundTileData, i, 1));
                                    compoundTileData[i] = (byte)Math.Round(lv * opacity + cv * (1 - opacity));
                                }
                                break;
                        }
                        break;
                    case 16:
                        switch (layerInfo[l].PixelFormat)
                        {
                            case PixelFormat.IntegerUnsigned:
                                for (int i = 0; i < compoundTileData.Length; i += bytesPerChannel)
                                {
                                    var opacity = (double)layerInfo[l].Opacity;
                                    ushort lv = BitConverter.ToUInt16(new Span<byte>(tileLayers[l], i, bytesPerChannel));
                                    ushort cv = BitConverter.ToUInt16(new Span<byte>(compoundTileData, i, bytesPerChannel));
                                    BitConverter.GetBytes((ushort)Math.Round(lv * opacity + cv * (1 - opacity))).CopyTo(compoundTileData, i);
                                }
                                break;
                        }
                        break;
                    case 32:
                        switch (layerInfo[l].PixelFormat)
                        {
                            case PixelFormat.IntegerUnsigned:
                                for (int i = 0; i < compoundTileData.Length; i += bytesPerChannel)
                                {
                                    var opacity = (double)layerInfo[l].Opacity;
                                    uint lv = BitConverter.ToUInt32(new Span<byte>(tileLayers[l], i, bytesPerChannel));
                                    uint cv = BitConverter.ToUInt32(new Span<byte>(compoundTileData, i, bytesPerChannel));
                                    BitConverter.GetBytes((uint)Math.Round(lv * opacity + cv * (1 - opacity))).CopyTo(compoundTileData, i);
                                }
                                break;
                        }
                        break;
                }
            }
        }
    }
}
