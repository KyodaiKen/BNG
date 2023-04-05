namespace BNG_CORE {
    using MemoryPack;
    using System.Collections.Immutable;

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
        public double DisplayTime { get; set; } = 1/12; // Frame display time in seconds for animations
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
        public Frame[]? Frames { get; set; }
    }

    public class Bitmap : IDisposable {
        private Header Info;
        private bool disposedValue;

        public Bitmap() {
            Info = new Header();
        }
        public Bitmap(Stream Input, double DisplayTime = 0, uint OffsetX = 0, uint OffsetY = 0) {
            Info = new Header();
            AddFrame(Input, DisplayTime, OffsetX, OffsetY);
        }
        public void AddFrame(Stream Input, double DisplayTime = 0, uint OffsetX = 0, uint OffsetY = 0) {
            Info.Frames ??= new Frame[0];
            Info.Frames.Append(new Frame() { DisplayTime = DisplayTime, Layers = null, LayerDataOffsets = null });
            AddLayer(Info.Frames.LongLength, Input, OffsetX, OffsetY);
        }

        public void AddLayer(long FrameID, Stream Input, uint OffsetX = 0, uint OffsetY = 0) {
            if (Info.Frames == null) throw new ArgumentNullException(nameof(Info.Frames));

            Info.Frames[FrameID].Layers ??= new Layer[0];

            Layer myNewLayer = new Layer();

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
    }