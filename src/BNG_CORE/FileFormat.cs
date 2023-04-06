namespace BNG_CORE {
    using MemoryPack;
    using MemoryPack.Compression;
    using System.ComponentModel;
    using System.Text;

    public enum LayerBlendMode : byte {
        Normal = 0x00,
        Replace = 0x00,
        Multiply = 0x01,
        Divide = 0x02
    }

    public enum CompressionAlgorithm : byte {
        None = 0x00,
        LinePredictor = 0x80,
        Predictor3D = 0x85
    }

    public enum EntropyEncoding : byte {
        None = 0x00,
        GZIP = 0x80,
        Brotli = 0x85,
        ZSTD = 0x90,
        LZMA = 0x95,
        ArithmeticOrder0 = 0xA0
    }

    public enum PixelFormat : byte {
        GRAY = 0x10,
        RGB = 0x0B,
        RGBA = 0xAB,
        CMYK = 0x0C,
        CMYKA = 0xAC,
        YCrCb = 0x0F,
        YCrCbA = 0xAF
    }

    public enum BitsPerChannel : byte {
        BPC_UInt8 = 0xA0,
        BPC_UInt16_LE = 0xA1,
        BPC_UInt16_BE = 0xA2,
        BPC_UInt32_LE = 0xA3,
        BPC_UInt32_BE = 0xA4,
        BPC_UInt64_LE = 0xA5,
        BPC_UInt64_BE = 0xA6,
        BPC_IEEE_FLOAT32 = 0xF1, // float
        BPC_IEEE_FLOAT64 = 0xF2, // double

        //YCrCb
        BPP_YCrCbPacked_9 = 0x30, //4:1:0
        BPP_YCrCbPacked_12 = 0x31, //4:2:0
        BPP_YCrCbPacked_16 = 0x32, //4:2:2
        BPP_YCrCbPacked_24 = 0x33, //4:4:4
    }

    public enum MetaBlockType : byte {
        TypeString = 0x00,
        TypeNumer = 0x01,
        TypeBinary = 0x03
    }

    [MemoryPackable]
    public partial class MetaBlock {
        public string? Key { get; set; }
        public MetaBlockType? Type { get; set; }
        public byte[]? Value { get; set; }
    }

    [MemoryPackable]
    public partial class Tile {
        public CompressionAlgorithm CompressionAlgo { get; set; }
        public EntropyEncoding EntropyEncoding { get; set; }
        public ushort Width { get; set; }
        public ushort Height { get; set; }
    }

    [MemoryPackable]
    public partial class Layer {
        public string Name { get; set; } = "layer";
        public string Description { get; set; } = "";
        public PixelFormat PixelFormat { get; set; }
        public BitsPerChannel BitsPerChannel { get; set; }
        public LayerBlendMode BlendMode { get; set; }
        public double Opacity { get; set; } = 1;
        public uint OffsetX { get; set; }
        public uint OffsetY { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public ulong[,]? TileDataOffsets { get; set; }
        public Tile[,]? Tiles { get; set; }
    }

    [MemoryPackable]
    public partial class Frame {
        public double DisplayTime { get; set; } = 1 / 12; // Frame display time in seconds for animations
        public double ResolutionH { get; set; }
        public double ResolutionV { get; set; }
        public ulong[]? LayerDataOffsets { get; set; }
        public Layer[]? Layers { get; set; }
    }
    [MemoryPackable]
    public partial class Header {
        public byte Version { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public MetaBlock[]? Metadata { get; set; }
        public ulong[]? FrameDataOffsets { get; set; }
        public Frame[] Frames { get; set; }
    }

    public class Bitmap : IDisposable {
        private Header Info;
        private bool disposedValue;

        public Bitmap() {
            Info = new Header();
            Info.Frames = new Frame[0];
        }

        public Bitmap(string ImportFileName, ushort PageIndex = 0, double DisplayTime = 0, uint OffsetX = 0, uint OffsetY = 0) {
            Info = new Header();
            Info.Frames = new Frame[0];
            AddFrame(ImportFileName, PageIndex, DisplayTime, OffsetX, OffsetY);
        }

        public void AddFrame(string ImportFileName, ushort PageIndex = 0, double DisplayTime = 0, uint OffsetX = 0, uint OffsetY = 0) {
            Info.Frames.Append(new Frame() { DisplayTime = DisplayTime, Layers = null, LayerDataOffsets = null });
            AddLayer(Info.Frames.LongLength, ImportFileName, PageIndex, OffsetX, OffsetY);
        }
        public void AddLayer(long FrameID, string ImportFileName, ushort PageIndex = 0, uint OffsetX = 0, uint OffsetY = 0) {
            if (Info.Frames == null) throw new NullReferenceException(nameof(Info.Frames));

            Info.Frames[FrameID].Layers ??= new Layer[0];

            Layer myNewLayer = new Layer();

        }

        public Header GetMetadata() {
            return Info;
        }

        public byte[] PackData() {
            //TODO
            return new byte[0];
        }

        private (uint w, uint h) CalculateTileDimension(long FrameID, long LayerID) {
            if (Info.Frames == null) throw new NullReferenceException(nameof(Info.Frames));
            if (FrameID > Info.Frames.LongLength) throw new IndexOutOfRangeException(nameof(FrameID));
            if (FrameID < 0) throw new IndexOutOfRangeException(nameof(FrameID));
            if (Info.Frames[FrameID].Layers == null) throw new NullReferenceException("Layers");
            if (LayerID > Info.Frames[FrameID].Layers.LongLength) throw new IndexOutOfRangeException(nameof(LayerID));
            if (LayerID < 0) throw new IndexOutOfRangeException(nameof(LayerID));

            switch (Info.Frames[FrameID].Layers[LayerID].PixelFormat) {
                case PixelFormat.GRAY:
                    switch (Info.Frames[FrameID].Layers[LayerID].BitsPerChannel) {
                        case BitsPerChannel.BPC_UInt8:
                            return (512, 512); // 256 KByte tiles
                        case BitsPerChannel.BPC_UInt16_LE:
                        case BitsPerChannel.BPC_UInt16_BE:
                            return (724, 724); // ~512 KByte tiles
                        case BitsPerChannel.BPC_UInt32_LE:
                        case BitsPerChannel.BPC_UInt32_BE:
                        case BitsPerChannel.BPC_IEEE_FLOAT32:
                            return (1024, 1024); // 1 MByte tiles
                        case BitsPerChannel.BPC_UInt64_LE:
                        case BitsPerChannel.BPC_UInt64_BE:
                        case BitsPerChannel.BPC_IEEE_FLOAT64:
                            return (1448, 1448); // ~2 MByte tiles
                    }
                    break;
                case PixelFormat.RGB:
                case PixelFormat.YCrCb:
                    switch (Info.Frames[FrameID].Layers[LayerID].BitsPerChannel) {
                        case BitsPerChannel.BPC_UInt8:
                        case BitsPerChannel.BPP_YCrCbPacked_9:
                            return (1024, 1024); // 1 MByte tiles
                        case BitsPerChannel.BPC_UInt16_LE:
                        case BitsPerChannel.BPC_UInt16_BE:
                        case BitsPerChannel.BPP_YCrCbPacked_12:
                        case BitsPerChannel.BPP_YCrCbPacked_16:
                            return (1448, 1448); // ~2 MByte tiles
                        case BitsPerChannel.BPC_UInt32_LE:
                        case BitsPerChannel.BPC_UInt32_BE:
                        case BitsPerChannel.BPC_IEEE_FLOAT32:
                        case BitsPerChannel.BPP_YCrCbPacked_24:
                            return (2048, 2048); // 4 MByte tiles
                        case BitsPerChannel.BPC_UInt64_LE:
                        case BitsPerChannel.BPC_UInt64_BE:
                        case BitsPerChannel.BPC_IEEE_FLOAT64:
                            return (2896, 2896); // ~8 MByte
                    }
                    break;
                case PixelFormat.CMYK:
                case PixelFormat.RGBA:
                case PixelFormat.YCrCbA:
                    switch (Info.Frames[FrameID].Layers[LayerID].BitsPerChannel) {
                        case BitsPerChannel.BPC_UInt8:
                        case BitsPerChannel.BPP_YCrCbPacked_9:
                            return (1280, 1280); // 1.56 MByte tiles
                        case BitsPerChannel.BPC_UInt16_LE:
                        case BitsPerChannel.BPC_UInt16_BE:
                            return (1920, 1920); // 3.5 MByte tiles
                        case BitsPerChannel.BPC_UInt32_LE:
                        case BitsPerChannel.BPC_UInt32_BE:
                        case BitsPerChannel.BPC_IEEE_FLOAT32:
                            return (3840, 3840); // 11.5 MByte tiles
                        case BitsPerChannel.BPC_UInt64_LE:
                        case BitsPerChannel.BPC_UInt64_BE:
                        case BitsPerChannel.BPC_IEEE_FLOAT64:
                            return (4096, 4096); // 16 MByte
                    }
                    break;
                case PixelFormat.CMYKA:
                    switch (Info.Frames[FrameID].Layers[LayerID].BitsPerChannel) {
                        case BitsPerChannel.BPC_UInt8:
                            return (1536, 1536); // 2.25 MByte tiles
                        case BitsPerChannel.BPC_UInt16_LE:
                        case BitsPerChannel.BPC_UInt16_BE:
                            return (2300, 2300); // ~5 MByte tiles
                        case BitsPerChannel.BPC_UInt32_LE:
                        case BitsPerChannel.BPC_UInt32_BE:
                        case BitsPerChannel.BPC_IEEE_FLOAT32:
                            return (3238, 3238); // ~10 MByte tiles
                        case BitsPerChannel.BPC_UInt64_LE:
                        case BitsPerChannel.BPC_UInt64_BE:
                        case BitsPerChannel.BPC_IEEE_FLOAT64:
                            return (4580, 4580); // ~20 MByte
                    }
                    break;
            }
            return (1024, 1024);
        }

        private (uint w, uint h) CalculateTileDimensionForCoordinate(long FrameID, long LayerID, (uint w, uint h) TileSize, (uint x, uint y) TileIndex) {
            if (Info.Frames == null) throw new NullReferenceException(nameof(Info.Frames));
            if (FrameID > Info.Frames.LongLength) throw new IndexOutOfRangeException(nameof(FrameID));
            if (FrameID < 0) throw new IndexOutOfRangeException(nameof(FrameID));
            if (Info.Frames[FrameID].Layers == null) throw new NullReferenceException("Layers");
            if (LayerID > Info.Frames[FrameID].Layers.LongLength) throw new IndexOutOfRangeException(nameof(LayerID));
            if (LayerID < 0) throw new IndexOutOfRangeException(nameof(LayerID));

            (uint w, uint h) LayerDimension = (Info.Frames[FrameID].Layers[LayerID].Width, Info.Frames[FrameID].Layers[LayerID].Height);

            if (TileIndex.x * TileSize.w > LayerDimension.w) throw new IndexOutOfRangeException(nameof(TileIndex));
            if (TileIndex.y * TileSize.h > LayerDimension.h) throw new IndexOutOfRangeException(nameof(TileIndex));

            uint NewTileWidth;
            uint NewTileHeight;

            if ((TileIndex.x + 1) * TileSize.w > LayerDimension.w) {
                NewTileWidth = LayerDimension.w % TileSize.w;
            } else {
                NewTileWidth = TileSize.w;
            }

            if ((TileIndex.y + 1) * TileSize.h > LayerDimension.h) {
                NewTileHeight = LayerDimension.h % TileSize.h;
            }
            else {
                NewTileHeight = TileSize.h;
            }

            return (NewTileWidth, NewTileHeight);
        }

        #region IDisposable implementation
        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ImageObject()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    public class FileWriter {
        public delegate void ProgressChanged(object sender, ProgressChangedEventArgs args);
        public FileWriter() {

        }

        public void WriteFile(ref Stream File, ref Bitmap Bitmap) {
            if (File == null) throw new ArgumentNullException(nameof(File));
            if (Bitmap == null) throw new ArgumentNullException(nameof(Bitmap));
            if (File.CanSeek == false) throw new AccessViolationException("Stream not seekable");
            if (File.CanWrite == false) throw new AccessViolationException("Stream not writable");

            //Truncate data if something has already been written into it from outside.
            File.SetLength(0);

            //Write magic word
            File.Write(Encoding.ASCII.GetBytes("BNG!"));

            //Build compressed bitmap data, updates the metadata (header)
            var BitmapData = Bitmap.PackData();

            //Serialize the metadata and then compress it using Brotli
            var Compressor = new BrotliCompressor(11, 24);
            MemoryPackSerializer.Serialize(Compressor, Bitmap.GetMetadata());
            var Metadata = Compressor.ToArray();
            Compressor.Dispose();
            
            //Write metadata length
            File.Write(BitConverter.GetBytes((uint)Metadata.Length));
            File.Write(Metadata);
            Array.Clear(Metadata);

            //Finally, write the actual compressed bitmap data
            File.Write(BitmapData);
            File.Flush();
        }
    }
}