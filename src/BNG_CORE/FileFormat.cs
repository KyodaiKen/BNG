namespace BNG_CORE {
    using MemoryPack;

    public enum PixFmt : byte {
        GREY = 0x00,
        GRAY = 0x00,
        ALPHA_ONLY = 0x00,
        RGB = 0x7F,
        RGBA = 0x70,
        YCbCr = 0xF0,
        YCbCrA = 0xF7,
        CMYK = 0xA0,
        CMYKA = 0xA7
    }

    public enum Bits : byte {
        //YCrCb
        BPP_YCrCbPacked_9 = 0x30, //4:1:0
        BPP_YCrCbPacked_12 = 0x31, //4:2:0
        BPP_YCrCbPacked_16 = 0x32, //4:2:2
        BPP_YCrCbPacked_24 = 0x33, //4:4:4

        //RGB Legacy
        BPP_RGB16_555 = 0xA0,
        BPP_RGB16_565 = 0xA1,

        //RGB(A), CMYK(A), YCrCbA, Greyscale, Alpha only
        BPC_BYTE = 0xB0,
        BPC_INT16LE = 0xB1,
        BPC_INT16BE = 0xB2,
        BPC_INT32LE = 0xB3,
        BPC_IEEE32 = 0xB4,
        BPC_IEEE64 = 0xB5
    }

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
    [MemoryPackable]
    public partial class Tile {
        public CompressionAlgorithm compression_algo { get; set; }
        public EntropyEncoding entropy_encoding { get; set; }
        public ushort width { get; set; }
        public ushort height { get; set; }
        public byte[]? data { get; set; }
    }

    [MemoryPackable]
    public partial class Layer {
        public string name { get; set; } = "layer";
        public string descr { get; set; } = "";
        public PixFmt pixel_format { get; set; }
        public Bits bits { get; set; }
        public LayerBlendMode blend_mode { get; set; }
        public double opacity { get; set; } = 1;
        public uint offset_x { get; set; }
        public uint offset_y { get; set; }
        public uint width { get; set; }
        public uint height { get; set; }
        public ulong[,]? tile_data_offsets { get; set; } //To be differential encoded and compressed using ZSTD
        public Tile[,]? tiles { get; set; }
    }

    [MemoryPackable]
    public partial class Frame {
        public double display_time { get; set; } = 1/12; // Frame display time in seconds for animations
        public ulong[]? layer_data_offsets { get; set; } //To be differential encoded and compressed using ZSTD
        public Layer[]? layers { get; set; }
    }
    [MemoryPackable]
    public partial class File {
        public byte version { get; set; }
        public uint width { get; set; }
        public uint height { get; set; }
        public ulong[]? frame_data_offsets { get; set; } //To be differential encoded and compressed using ZSTD
        public Frame[]? frames { get; set; }
    }
}