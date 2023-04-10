namespace BNG_CORE {
    using BNG_CORE.Filters;
    using MemoryPack;
    using MemoryPack.Compression;
    using System;
    using System.Diagnostics;
    using System.IO.Compression;
    using System.Text;
    using ZstdSharp;

    public enum LayerBlendMode : byte {
        Normal = 0x00,
        Replace = 0x00,
        Multiply = 0x01,
        Divide = 0x02
    }

    public enum CompressionPreFilter : byte {
        None = 0x00,
        Sub = 0x10,
        Up = 0x20,
        Average = 0x30,
        Paeth = 0x40,
        All = 0xFF
    }

    [Flags]
    public enum Compression : byte {
        None = 0,
        Brotli = 8,
        ZSTD = 16
    }

    public enum PixelFormat : byte {
        GRAY = 0x10,
        GRAYA = 0xA1,
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
    }

    public enum DataBlockType : byte {
        TypeString = 0x00,
        TypeNumer = 0x01,
        TypeBinary = 0x03
    }

    [MemoryPackable]
    public partial class DataBlock {
        public string? Key { get; set; }
        public DataBlockType? Type { get; set; }
        public byte[]? Value { get; set; }
    }

    [MemoryPackable]
    public partial class Layer {
        [MemoryPackIgnore]
        public string SourceFileName { get; set; } = string.Empty;
        public string Name { get; set; } = "layer";
        public string Description { get; set; } = string.Empty;
        public PixelFormat PixelFormat { get; set; } = PixelFormat.RGB;
        public BitsPerChannel BitsPerChannel { get; set; } = BitsPerChannel.BPC_UInt8;
        [MemoryPackIgnore]
        public int BitsPerPixel { get; set; } = 0;
        public CompressionPreFilter CompressionPreFilter { get; set; } = CompressionPreFilter.None;
        public Compression Compression { get; set; } = Compression.Brotli;
        [MemoryPackIgnore]
        public int CompressionLevel { get; set; } = 3;
        [MemoryPackIgnore]
        public int BrotliWindowSize { get; set; } = 0;
        public LayerBlendMode BlendMode { get; set; } = LayerBlendMode.Normal;
        public double Opacity { get; set; } = 1.0;
        public uint OffsetX { get; set; } = 0;
        public uint OffsetY { get; set; } = 0;
        public uint Width { get; set; } = 0;
        public uint Height { get; set; } = 0;
        public List<DataBlock>? ExtraData { get; set; }
        public ulong[,] TileDataOffsets { get; set; }
        public (uint w, uint h) BaseTileDimension { get; set; }
        [MemoryPackIgnore]
        public ulong[,] TileDataLengths { get; set; }
        [MemoryPackIgnore]
        public (uint w, uint h)[,] TileDimensions { get; set; }
    }

    [MemoryPackable]
    public partial class Frame {
        public double DisplayTime { get; set; } = 1 / 12; // Frame display time in seconds for animations
        public double ResolutionH { get; set; }
        public double ResolutionV { get; set; }
        public List<ulong>? LayerDataOffsets { get; set; }
        public List<Layer>? Layers { get; set; }
    }
    [MemoryPackable]
    public partial class Header {
        public byte Version { get; set; }
        [MemoryPackIgnore]
        public ulong HeaderLength { get; set; }
        [MemoryPackIgnore]
        public ulong DataStartOffset { get; set; }
        [MemoryPackIgnore]
        public ulong DataLength { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public List<DataBlock>? Metadata { get; set; }
        public List<ulong> FrameDataOffsets { get; set; }
        public List<Frame> Frames { get; set; }
    }


    public partial class ImportParameters {
        public PixelFormat TargetPixelFormat { get; set; } = PixelFormat.RGB;
        public BitsPerChannel TargetBitsPerChannel { get; set; } = BitsPerChannel.BPC_UInt8;
        public CompressionPreFilter CompressionPreFilter { get; set; }
        public Compression Compression { get; set; }
        public int CompressionLevel { get; set; } = (int)System.IO.Compression.CompressionLevel.SmallestSize;
        public int BrotliWindowSize { get; set; } = 0;
        public (double h, double v) Resolution { get; set; } = (72.0, 72.0);
        public string LayerName { get; set; } = string.Empty;
        public string LayerDescription { get; set; } = string.Empty;
        public double DisplayTime { get; set; } = 0.0;
        public (uint x, uint y) Offset { get; set; } = (0, 0);
        public double Opacity { get; set; } = 1.0;
        public LayerBlendMode BlendMode { get; set; } = LayerBlendMode.Normal;
    }

    public class RAWImportParameters : ImportParameters {
        public (uint w, uint h) SourceDimensions { get; set; }
        public PixelFormat SourcePixelFormat { get; set; }
        public BitsPerChannel SourceBitsPerChannel { get; set; }
    }

    public class Bitmap : IDisposable {
        private Header Info;
        private bool disposedValue;

        public bool Strict { get; set; } = false;

        public delegate void dgProgressChanged(double progress);
        public dgProgressChanged ProgressChangedEvent { get; set; }

        public Bitmap() {
            Info = new Header();
            Info.Frames = new List<Frame>();
        }

        #region Loading
        public void LoadBNG(ref Stream InputStream, out StringBuilder log) {
            if (InputStream == null) throw new ArgumentNullException(nameof(File));
            if (InputStream.CanSeek == false) throw new AccessViolationException("Stream not seekable");
            if (InputStream.CanRead == false) throw new AccessViolationException("Stream not readable");

            InputStream.Seek(0, SeekOrigin.Begin);

            log = new StringBuilder();

            //Read first 4 byte and check if it's the BNG! identifier
            byte[] ident = new byte[4];
            InputStream.Read(ident);
            uint intIdent = BitConverter.ToUInt32(ident);

            if (intIdent != 0x21744e42 || intIdent != 0x53744e42) {
                if(Strict) throw new InvalidDataException("This is not a BNG file!");
            }

            //Check if the next 64 bit word which is the pointer to the header is not outside the bounds of the stream
            ulong streamLength = (ulong)InputStream.Length;
            byte[] baHeaderOffset = new byte[8];
            InputStream.Read(baHeaderOffset);
            ulong headerOffset = BitConverter.ToUInt64(baHeaderOffset);
            if (headerOffset > streamLength) throw new InvalidDataException("The header offset is out of bounds");
            long dataOffset = 0;

            //If the identifier ends with S (BNGS) then the header has been moved to the beginning of the file for streaming.
            if (intIdent == 0x53744e42) {
                //Try to read the header
                byte[] binaryHeader = new byte[headerOffset]; //This is now the header size
                InputStream.Read(binaryHeader);

                using (var decompressor = new BrotliDecompressor()) {
                    var decompressedBuffer = decompressor.Decompress(binaryHeader);
                    Info = MemoryPackSerializer.Deserialize<Header>(decompressedBuffer);
                }
                dataOffset = InputStream.Position;
                Info.HeaderLength = (ulong)binaryHeader.LongLength;
            }
            else 
            {
                //Try to read the header
                byte[] binaryHeader = new byte[streamLength - headerOffset];
                dataOffset = InputStream.Position;

                InputStream.Seek((long)headerOffset, SeekOrigin.Begin);
                InputStream.Read(binaryHeader);

                using (var decompressor = new BrotliDecompressor()) {
                    var decompressedBuffer = decompressor.Decompress(binaryHeader);
                    Info = MemoryPackSerializer.Deserialize<Header>(decompressedBuffer);
                }
                Info.HeaderLength = (ulong)binaryHeader.LongLength;
            }

            Info.DataLength = (ulong)InputStream.Length - Info.HeaderLength - 12;
            Info.DataStartOffset = (ulong)dataOffset;

            log.AppendLine(string.Format("BNG Version..........: {0}", Info.Version));
            log.AppendLine(string.Format("Width................: {0}", Info.Width));
            log.AppendLine(string.Format("Height...............: {0}", Info.Height));
            log.AppendLine(string.Format("Number of frames.....: {0}", Info.Frames.Count));
            log.AppendLine();
            for (int frame = 0; frame < Info.Frames.Count; frame++) {
                string frmNum = string.Format("Frame {0}", frame) + " ";
                log.Append(frmNum);
                log.AppendLine(new string('=', 40 - frmNum.Length));
                log.AppendLine(string.Format("Display time (sec)...: {0}", Info.Frames[frame].DisplayTime));
                log.AppendLine(string.Format("Horizontal resolution: {0}", Info.Frames[frame].ResolutionH));
                log.AppendLine(string.Format("Vertical resolution..: {0}", Info.Frames[frame].ResolutionV));
                log.AppendLine(string.Format("Number of layers.....: {0}", Info.Frames[frame].Layers.Count));

                for (int layer = 0; layer < Info.Frames[frame].Layers.Count;  layer++) {
                    string lyrNum = string.Format("Layer {0}", layer) + " ";
                    log.Append(lyrNum);
                    log.AppendLine(new string('-', 40 - lyrNum.Length));
                    log.AppendLine(string.Format("Layer name...........: {0}", Info.Frames[frame].Layers[layer].Name));
                    log.AppendLine(string.Format("Layer description....: {0}", Info.Frames[frame].Layers[layer].Description));
                    log.AppendLine(string.Format("Offset X.............: {0}", Info.Frames[frame].Layers[layer].OffsetX));
                    log.AppendLine(string.Format("Offset Y.............: {0}", Info.Frames[frame].Layers[layer].OffsetY));
                    log.AppendLine(string.Format("Width................: {0}", Info.Frames[frame].Layers[layer].Width));
                    log.AppendLine(string.Format("Height...............: {0}", Info.Frames[frame].Layers[layer].Height));
                    log.AppendLine(string.Format("Opacity..............: {0}", Info.Frames[frame].Layers[layer].Opacity));
                    log.AppendLine(string.Format("Pixel format.........: {0}", Info.Frames[frame].Layers[layer].PixelFormat.ToString()));
                    log.AppendLine(string.Format("Bits per channel.....: {0}", Info.Frames[frame].Layers[layer].BitsPerChannel.ToString()));
                    log.AppendLine(string.Format("Compression..........: {0}", Info.Frames[frame].Layers[layer].Compression.ToString()));
                    log.AppendLine(string.Format("Pre-filter...........: {0}", Info.Frames[frame].Layers[layer].CompressionPreFilter.ToString()));
                    log.AppendLine(string.Format("Base tile dimension W: {0}", Info.Frames[frame].Layers[layer].BaseTileDimension.w));
                    log.AppendLine(string.Format("Base tile dimension H: {0}", Info.Frames[frame].Layers[layer].BaseTileDimension.h));
                    log.AppendLine(string.Format("Data size (bytes)....: {0}", (layer + 1 >= Info.Frames[frame].Layers.Count ? (ulong)InputStream.Length - Info.HeaderLength - 12 : Info.Frames[frame].LayerDataOffsets[layer+1]) - Info.Frames[frame].LayerDataOffsets[layer]));
                    log.AppendLine(string.Format("Uncompressed size (\"): {0}", Info.Frames[frame].Layers[layer].Width * Info.Frames[frame].Layers[layer].Height * CalculateBitsPerPixel(Info.Frames[frame].Layers[layer].PixelFormat, Info.Frames[frame].Layers[layer].BitsPerChannel) / 8));
                    log.AppendLine(string.Format("Number of tiles......: {0}", Info.Frames[frame].Layers[layer].TileDataOffsets.LongLength));

                    var txl = Info.Frames[frame].Layers[layer].TileDataOffsets.GetLongLength(0);
                    var tyl = Info.Frames[frame].Layers[layer].TileDataOffsets.GetLongLength(1);
                    Info.Frames[frame].Layers[layer].TileDimensions = new (uint w, uint h)[txl, tyl];
                    Info.Frames[frame].Layers[layer].TileDataLengths = new ulong[txl, tyl];

                    for (uint tileY = 0; tileY < tyl; tileY++) {
                        for (uint tileX = 0; tileX < txl; tileX++) {
                            string tleNum = string.Format("Tile {0},{1}", tileX, tileY) + " ";
                            log.Append(tleNum);
                            log.AppendLine(new string('-', 40 - tleNum.Length));
                            ulong tleWzUnPacked = 0;
                            ulong tleSzPacked = 0;
                            if (tileX == txl-1 && tileY != tyl-1) {
                                tleSzPacked = Info.Frames[frame].Layers[layer].TileDataOffsets[0, tileY + 1] - Info.Frames[frame].Layers[layer].TileDataOffsets[tileX, tileY];
                            } else if (tileX == txl-1 && tileY == tyl-1) {
                                tleSzPacked = (ulong)InputStream.Length - Info.HeaderLength - Info.Frames[frame].Layers[layer].TileDataOffsets[tileX, tileY];
                            } else {
                                tleSzPacked = Info.Frames[frame].Layers[layer].TileDataOffsets[tileX + 1, tileY] - Info.Frames[frame].Layers[layer].TileDataOffsets[tileX, tileY];
                            }
                            Info.Frames[frame].Layers[layer].TileDataLengths[tileX, tileY] = tleSzPacked;
                            var corrTileSize = CalculateTileDimensionForCoordinate((Info.Frames[frame].Layers[layer].Width, Info.Frames[frame].Layers[layer].Height)
                                , (Info.Frames[frame].Layers[layer].BaseTileDimension.w, Info.Frames[frame].Layers[layer].BaseTileDimension.h), (tileX, tileY));
                            Info.Frames[frame].Layers[layer].TileDimensions[tileX, tileY] = corrTileSize;
                            tleWzUnPacked = (ulong)(corrTileSize.w * corrTileSize.h * CalculateBitsPerPixel(Info.Frames[frame].Layers[layer].PixelFormat, Info.Frames[frame].Layers[layer].BitsPerChannel) / 8);
                            log.AppendLine(string.Format("Actual tile dim. W...: {0}", corrTileSize.w));
                            log.AppendLine(string.Format("Actual tile dim. H...: {0}", corrTileSize.h));
                            log.AppendLine(string.Format("Data size (bytes)....: {0}", tleSzPacked));
                            log.AppendLine(string.Format("Uncompressed size (\"): {0}", tleWzUnPacked));
                        }
                    }
                }
            }
        }

        public void DecodeToRaw(ref Stream InputStream, ref Stream OutputStream, int FrameID, int LayerID) {
            if (InputStream == null) throw new ArgumentNullException(nameof(File));
            if (InputStream.CanSeek == false) throw new AccessViolationException("Stream not seekable");
            if (InputStream.CanRead == false) throw new AccessViolationException("Stream not readable");
            if (OutputStream == null) throw new ArgumentNullException(nameof(File));
            if (OutputStream.CanSeek == false) throw new AccessViolationException("Stream not seekable");
            if (OutputStream.CanWrite == false) throw new AccessViolationException("Stream not writable");

            var layer = Info.Frames[FrameID].Layers[LayerID];
            var txl = layer.TileDataOffsets.GetLongLength(0);
            var tyl = layer.TileDataOffsets.GetLongLength(1);
            var bytesPerPixel = CalculateBitsPerPixel(layer.PixelFormat, layer.BitsPerChannel) / 8;

            InputStream.Seek((long)Info.DataStartOffset, SeekOrigin.Begin);
            OutputStream.SetLength(0);
            long bytesWrittenForProgress = 0;

            Stopwatch sw = new();
            sw.Start();
            for (uint Y = 0; Y < tyl; Y++) {
                for (uint X = 0; X < txl; X++) {
                    UnpackTileToStream(ref layer, (X, Y), ref InputStream, ref OutputStream, bytesPerPixel, ref bytesWrittenForProgress);
                    var progress = (double)bytesWrittenForProgress / (layer.Width * layer.Height * bytesPerPixel) * 100.0;
                    if (sw.ElapsedMilliseconds >= 250 || progress == 100.0) {
                        sw.Restart();
                        ProgressChangedEvent?.Invoke(progress);
                    }
                }
            }
        }

        private void UnpackTileToStream(ref Layer layer, (uint x, uint y) tileIndex, ref Stream inStream, ref Stream outStream, int bytesPerPixel, ref long bytesWritten) {
            //Read and decompress tile
            byte[] compressedTileBuffer = new byte[layer.TileDataLengths[tileIndex.x, tileIndex.y]];
            byte[] tileBuffer = new byte[layer.TileDimensions[tileIndex.x, tileIndex.y].w * layer.TileDimensions[tileIndex.x, tileIndex.y].h * bytesPerPixel];
            inStream.Read(compressedTileBuffer);
            DeCompress(layer.Compression, tileBuffer.Length, ref compressedTileBuffer, ref tileBuffer);

            var tileRows = layer.TileDimensions[tileIndex.x, tileIndex.y].h;
            var tileRowRawLength = (int)layer.TileDimensions[tileIndex.x, tileIndex.y].w * bytesPerPixel;
            byte[] prevRow = new byte[tileRowRawLength]; //This also doubles as the unfiltered row for writing to the stream!
            byte[] row = new byte[tileRowRawLength];
            var stride = layer.Width * bytesPerPixel;

            for (uint r = 0; r < tileRows; r++) {
                Array.Copy(tileBuffer, row.Length * r, row, 0, row.Length);
                prevRow = DecodeFilter4TileScanline(layer.CompressionPreFilter, tileRowRawLength, bytesPerPixel, ref row, ref prevRow);
                outStream.Seek(layer.BaseTileDimension.h * tileIndex.y * stride + r * stride + layer.BaseTileDimension.w * tileIndex.x * bytesPerPixel, SeekOrigin.Begin);
                outStream.Write(prevRow);
                bytesWritten += prevRow.LongLength;
            }
        }

        private byte[] DecodeFilter4TileScanline(CompressionPreFilter compressionPreFilter, int rowLength, int bytesPerPixel, ref byte[] lineBuff, ref byte[] prevLineBuff) {
            byte[] unfilteredLine = new byte[rowLength];
            switch (compressionPreFilter) {
                case CompressionPreFilter.Paeth:
                    for (long col = 0; col < lineBuff.LongLength; col++) {
                        unfilteredLine[col] = Paeth.UnFilter(ref lineBuff,ref unfilteredLine, ref prevLineBuff, col, bytesPerPixel);
                    }
                    break;
                case CompressionPreFilter.Sub:
                    for (long col = 0; col < lineBuff.LongLength; col++) {
                        unfilteredLine[col] = Sub.UnFilter(ref lineBuff, ref unfilteredLine, col, bytesPerPixel);
                    }
                    break;
                case CompressionPreFilter.Up:
                    for (long col = 0; col < lineBuff.LongLength; col++) {
                        unfilteredLine[col] = Up.UnFilter(ref lineBuff, ref prevLineBuff, col, bytesPerPixel);
                    }
                    break;
                case CompressionPreFilter.Average:
                    for (long col = 0; col < lineBuff.LongLength; col++) {
                        unfilteredLine[col] = Average.UnFilter(ref lineBuff, ref prevLineBuff, col, bytesPerPixel);
                    }
                    break;
                default:
                    lineBuff.CopyTo(unfilteredLine, 0);
                    break;
            }
            return unfilteredLine;
        }

        private void DeCompress(Compression compression, int uncompressedSize, ref byte[] compressedBuffer, ref byte[] decompressedBuffer) {

            switch (compression) {
                case Compression.Brotli:
                    var bd = new BrotliDecoder();
                    int consumed, written;
                    bd.Decompress(compressedBuffer.ToArray(), decompressedBuffer, out consumed, out written);
                    break;
                case Compression.ZSTD:
                    ZstdSharp.Decompressor zstdDeCompressor = new ZstdSharp.Decompressor();
                    decompressedBuffer = zstdDeCompressor.Unwrap(compressedBuffer.ToArray()).ToArray();
                    break;
                case Compression.None:
                    decompressedBuffer = compressedBuffer;
                    break;
            }
        }
        #endregion

        #region Helpers
        private int CalculateBitsPerPixel(PixelFormat pixelFormat, BitsPerChannel bitsPerChannel) {
            switch (bitsPerChannel) {
                case BitsPerChannel.BPC_UInt8:
                    switch (pixelFormat) {
                        case PixelFormat.GRAY:
                            return 8;
                        case PixelFormat.GRAYA:
                            return 16;
                        case PixelFormat.RGB:
                        case PixelFormat.YCrCb:
                            return 24;
                        case PixelFormat.RGBA:
                        case PixelFormat.CMYK:
                            return 32;
                        case PixelFormat.CMYKA:
                            return 40;
                    }
                    break;
                case BitsPerChannel.BPC_UInt16_BE:
                case BitsPerChannel.BPC_UInt16_LE:
                    switch (pixelFormat) {
                        case PixelFormat.GRAY:
                            return 16;
                        case PixelFormat.GRAYA:
                            return 32;
                        case PixelFormat.RGB:
                        case PixelFormat.YCrCb:
                            return 48;
                        case PixelFormat.RGBA:
                        case PixelFormat.CMYK:
                            return 64;
                        case PixelFormat.CMYKA:
                            return 80;
                    }
                    break;
                case BitsPerChannel.BPC_UInt32_LE:
                case BitsPerChannel.BPC_UInt32_BE:
                case BitsPerChannel.BPC_IEEE_FLOAT32:
                    switch (pixelFormat) {
                        case PixelFormat.GRAY:
                            return 32;
                        case PixelFormat.GRAYA:
                            return 64;
                        case PixelFormat.RGB:
                        case PixelFormat.YCrCb:
                            return 96;
                        case PixelFormat.RGBA:
                        case PixelFormat.CMYK:
                            return 128;
                        case PixelFormat.CMYKA:
                            return 160;
                    }
                    break;
                case BitsPerChannel.BPC_UInt64_LE:
                case BitsPerChannel.BPC_UInt64_BE:
                case BitsPerChannel.BPC_IEEE_FLOAT64:
                    switch (pixelFormat) {
                        case PixelFormat.GRAY:
                            return 64;
                        case PixelFormat.GRAYA:
                            return 128;
                        case PixelFormat.RGB:
                        case PixelFormat.YCrCb:
                            return 192;
                        case PixelFormat.RGBA:
                        case PixelFormat.CMYK:
                            return 256;
                        case PixelFormat.CMYKA:
                            return 320;
                    }
                    break;
            }
            return 0;
        }

        private (uint w, uint h) CalculateTileDimension(PixelFormat pixelFormat, BitsPerChannel bitsPerChannel) {
            int numChannels = 0;
            switch (pixelFormat) {
                case PixelFormat.GRAY:
                    numChannels = 1;
                    break;
                case PixelFormat.GRAYA:
                    numChannels = 2;
                    break;
                case PixelFormat.RGB:
                case PixelFormat.YCrCb:
                case PixelFormat.CMYK:
                    numChannels = 3;
                    break;
                case PixelFormat.RGBA:
                case PixelFormat.YCrCbA:
                    numChannels = 4;
                    break;
                case PixelFormat.CMYKA:
                    numChannels = 5;
                    break;
            }
            int bytesPerPixel = CalculateBitsPerPixel(pixelFormat, bitsPerChannel) / 8;
            uint size = (uint)(4096 / bytesPerPixel);
            return (size, size);

            /*
            switch (pixelFormat) {
                case PixelFormat.GRAY:
                case PixelFormat.GRAYA:
                    switch (bitsPerChannel) {
                        case BitsPerChannel.BPC_UInt8:
                            return (3027, 3027);
                        case BitsPerChannel.BPC_UInt16_LE:
                        case BitsPerChannel.BPC_UInt16_BE:
                            return (1536, 1536);
                        case BitsPerChannel.BPC_UInt32_LE:
                        case BitsPerChannel.BPC_UInt32_BE:
                        case BitsPerChannel.BPC_IEEE_FLOAT32:
                            return (768, 768);
                        case BitsPerChannel.BPC_UInt64_LE:
                        case BitsPerChannel.BPC_UInt64_BE:
                        case BitsPerChannel.BPC_IEEE_FLOAT64:
                            return (384, 384);
                    }
                    break;
                case PixelFormat.RGB:
                case PixelFormat.YCrCb:
                case PixelFormat.CMYK:
                case PixelFormat.RGBA:
                case PixelFormat.YCrCbA:
                case PixelFormat.CMYKA:
                    switch (bitsPerChannel) {
                        case BitsPerChannel.BPC_UInt8:
                            return (1024, 1024);
                        case BitsPerChannel.BPC_UInt16_LE:
                        case BitsPerChannel.BPC_UInt16_BE:
                            return (512, 512);
                        case BitsPerChannel.BPC_UInt32_LE:
                        case BitsPerChannel.BPC_UInt32_BE:
                        case BitsPerChannel.BPC_IEEE_FLOAT32:
                            return (256, 256);
                        case BitsPerChannel.BPC_UInt64_LE:
                        case BitsPerChannel.BPC_UInt64_BE:
                        case BitsPerChannel.BPC_IEEE_FLOAT64:
                            return (128, 128);
                    }
                    break;
            }
            return (1024, 1024);
            */
        }
        #endregion

        #region Writing
        public Bitmap(string ImportFileName, ImportParameters Options) {
            Info = new Header();
            Info.Frames = new List<Frame>();
            AddFrame(ImportFileName, Options);
        }

        public void AddFrame(string ImportFileName, ImportParameters Options) {
            Info.Frames.Add(new Frame() { DisplayTime = Options.DisplayTime, Layers = new List<Layer>(), LayerDataOffsets = new List<ulong>() });
            AddLayer(Info.Frames.Count - 1, ImportFileName, Options);
        }
        public void AddLayer(int FrameID, string ImportFileName, ImportParameters Options) {
            if (Info.Frames == null) throw new NullReferenceException(nameof(Info.Frames));
            PrepareRAWFileToLayer(ImportFileName, FrameID, (RAWImportParameters)Options);
        }
        public void WriteBitmapFile(ref Stream OutputStream) {
            if (Info == null) throw new NullReferenceException(nameof(Info));
            if (Info.Frames == null) throw new NullReferenceException(nameof(Info.Frames));
            if (OutputStream == null) throw new ArgumentNullException(nameof(File));
            if (OutputStream.CanSeek == false) throw new AccessViolationException("Stream not seekable");
            if (OutputStream.CanWrite == false) throw new AccessViolationException("Stream not writable");

            //Truncate data if something has already been written into it from outside.
            OutputStream.SetLength(0);

            //Write magic word
            OutputStream.Write(Encoding.ASCII.GetBytes("BNG!"));

            //Write placeholder 64 bit word:
            OutputStream.Write(BitConverter.GetBytes((ulong)0x0));

            for (int FrameID = 0; FrameID < Info.Frames.Count; FrameID++) {
                if (Info.Frames[FrameID].Layers == null) throw new NullReferenceException(string.Format("Info.Frames[{0}].Layers", FrameID));
                for (int LayerID = 0; LayerID < Info.Frames[FrameID].Layers.Count; LayerID++) {
                    var BytesPerPixel = Info.Frames[FrameID].Layers[LayerID].BitsPerPixel / 8;
                    var stride = Info.Frames[FrameID].Layers[LayerID].Width * BytesPerPixel;
                    var numTilesX = Info.Frames[FrameID].Layers[LayerID].TileDataOffsets.GetLongLength(0);
                    var numTilesY = Info.Frames[FrameID].Layers[LayerID].TileDataOffsets.GetLongLength(1);
                    var tileSize = CalculateTileDimension(Info.Frames[FrameID].Layers[LayerID].PixelFormat, Info.Frames[FrameID].Layers[LayerID].BitsPerChannel);
                    long bytesWritten = 0;
                    Stream InputStream = new FileStream(Info.Frames[FrameID].Layers[LayerID].SourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 0x800000, FileOptions.RandomAccess);
                    Info.Frames[FrameID].LayerDataOffsets[LayerID] = (ulong)OutputStream.Position;

                    Stopwatch sw = new();
                    sw.Start();
                    for (uint y = 0; y < numTilesY; y++) {
                        for (uint x = 0; x < numTilesX; x++) {
                            var corrTileSize = CalculateTileDimensionForCoordinate((Info.Frames[FrameID].Layers[LayerID].Width, Info.Frames[FrameID].Layers[LayerID].Height), tileSize, (x, y));
                            byte[] lineBuff = new byte[corrTileSize.w * BytesPerPixel];
                            byte[] prevLineBuff = new byte[corrTileSize.w * BytesPerPixel];
                            MemoryStream iBuff = new MemoryStream();
                            byte[] cBuff = Array.Empty<byte>();
                            long inputOffset = 0;

                            for (int line = 0; line < corrTileSize.h; line++) {
                                inputOffset = tileSize.h * y * stride + line * stride + tileSize.w * x * BytesPerPixel;
                                InputStream.Seek(inputOffset, SeekOrigin.Begin);
                                InputStream.Read(lineBuff);
                                Filter(Info.Frames[FrameID].Layers[LayerID].CompressionPreFilter, corrTileSize, ref lineBuff, ref prevLineBuff, ref iBuff, BytesPerPixel);
                                Array.Copy(lineBuff, prevLineBuff, lineBuff.LongLength);
                            }

                            bytesWritten += lineBuff.LongLength * corrTileSize.h;
                            var progress = (double)bytesWritten / InputStream.Length * 100.0;
                            if (sw.ElapsedMilliseconds >= 250 || progress == 100.0) {
                                sw.Restart();
                                ProgressChangedEvent?.Invoke(progress);
                            }

                            Info.Frames[FrameID].Layers[LayerID].TileDataOffsets[x, y] = (ulong)OutputStream.Position;

                            Compress(Info.Frames[FrameID].Layers[LayerID].Compression, Info.Frames[FrameID].Layers[LayerID].CompressionLevel, Info.Frames[FrameID].Layers[LayerID].BrotliWindowSize, ref iBuff, ref cBuff);

                            OutputStream.Write(cBuff);
                        }
                    }
                }
            }

            //Write header position
            var dataEndPosition = OutputStream.Position;
            OutputStream.Seek(4, SeekOrigin.Begin);
            OutputStream.Write(BitConverter.GetBytes((ulong)dataEndPosition));
            OutputStream.Position = dataEndPosition;

            //Serialize the metadata and then compress it using Brotli
            byte[] headerData;
            if (1 == 1) {
                var Compressor = new MemoryPack.Compression.BrotliCompressor(11, 24);
                MemoryPackSerializer.Serialize(Compressor, Info);
                headerData = Compressor.ToArray();
                Compressor.Dispose();
            } else {
                headerData = MemoryPackSerializer.Serialize(Info);
            }
            OutputStream.Write(headerData);
            Array.Clear(headerData);
            OutputStream.Close();

        }

        void Filter(CompressionPreFilter compressionPreFilter, (uint w, uint h) corrTileSize, ref byte[] lineBuff, ref byte[] prevLineBuff, ref MemoryStream iBuff, int BytesPerPixel) {
            switch (compressionPreFilter) {
                case CompressionPreFilter.Paeth:
                    byte[] paethLineBuff = new byte[corrTileSize.w * BytesPerPixel];
                    for (long col = 0; col < lineBuff.LongLength; col++) {
                        paethLineBuff[col] = Paeth.Filter(ref lineBuff, ref prevLineBuff, col, BytesPerPixel);
                    }
                    iBuff.Write(paethLineBuff);
                    break;
                case CompressionPreFilter.Sub:
                    byte[] subLineBuff = new byte[corrTileSize.w * BytesPerPixel];
                    for (long col = 0; col < lineBuff.LongLength; col++) {
                        subLineBuff[col] = Sub.Filter(ref lineBuff, col, BytesPerPixel);
                    }
                    iBuff.Write(subLineBuff);
                    break;
                case CompressionPreFilter.Up:
                    byte[] upLineBuff = new byte[corrTileSize.w * BytesPerPixel];
                    for (long col = 0; col < lineBuff.LongLength; col++) {
                        upLineBuff[col] = Up.Filter(ref lineBuff, ref prevLineBuff, col, BytesPerPixel);
                    }
                    iBuff.Write(upLineBuff);
                    break;
                case CompressionPreFilter.Average:
                    byte[] avgLineBuff = new byte[corrTileSize.w * BytesPerPixel];
                    for (long col = 0; col < lineBuff.LongLength; col++) {
                        avgLineBuff[col] = Average.Filter(ref lineBuff, ref prevLineBuff, col, BytesPerPixel);
                    }
                    iBuff.Write(avgLineBuff);
                    break;
                default:
                    iBuff.Write(lineBuff);
                    break;
            }
        }

        void Compress(Compression compression, int compressionLevel, int brotliWindowSize, ref MemoryStream iBuff, ref byte[] cBuff) {
            switch (compression) {
                case Compression.Brotli:
                    cBuff = new byte[iBuff.Length];
                    using (var be = new BrotliEncoder(compressionLevel, brotliWindowSize > 24 ? 24 : brotliWindowSize)) {
                        int consumed, written;
                        be.Compress(iBuff.ToArray(), cBuff, out consumed, out written, true);
                        Array.Resize(ref cBuff, written);
                    }
                    break;
                case Compression.ZSTD:
                    Compressor zstdCompressor = new Compressor(compressionLevel);
                    cBuff = zstdCompressor.Wrap(iBuff.ToArray()).ToArray();
                    break;
                case Compression.None:
                    cBuff = iBuff.ToArray();
                    break;
            }
        }

        /// <summary>
        /// Prepares the headers for a new layer from a raw image file
        /// </summary>
        /// <param name="RAWFileName">The file path to the raw image file</param>
        /// <param name="FrameID">The frame ID to attach the layer to</param>
        /// <param name="ImportParameters">Raw image import parameters</param>
        /// <returns>The new Layer ID</returns>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public ulong PrepareRAWFileToLayer(string RAWFileName, int FrameID, RAWImportParameters ImportParameters) {
            if (Info.Frames == null) throw new NullReferenceException(nameof(Info.Frames));
            Info.Width = ImportParameters.SourceDimensions.w;
            Info.Height = ImportParameters.SourceDimensions.h;

            var newLayer = new Layer();

            newLayer.SourceFileName = RAWFileName;
            newLayer.Name = ImportParameters.LayerName;
            newLayer.Description = ImportParameters.LayerDescription;
            newLayer.Width = ImportParameters.SourceDimensions.w;
            newLayer.Height = ImportParameters.SourceDimensions.h;
            if (ImportParameters.SourcePixelFormat != ImportParameters.TargetPixelFormat) throw new NotImplementedException("Color space conversion is not supported yet");
            newLayer.PixelFormat = ImportParameters.TargetPixelFormat;
            if (ImportParameters.SourceBitsPerChannel != ImportParameters.TargetBitsPerChannel) throw new NotImplementedException("Bit depth conversion is not supported yet");
            newLayer.BitsPerChannel = ImportParameters.TargetBitsPerChannel;
            newLayer.Compression = ImportParameters.Compression;
            newLayer.CompressionLevel = ImportParameters.CompressionLevel;
            newLayer.BrotliWindowSize = ImportParameters.BrotliWindowSize;
            newLayer.CompressionPreFilter = ImportParameters.CompressionPreFilter;
            newLayer.OffsetX = ImportParameters.Offset.x;
            newLayer.OffsetY = ImportParameters.Offset.y;
            newLayer.Opacity = ImportParameters.Opacity;
            newLayer.BlendMode = ImportParameters.BlendMode;

            newLayer.BitsPerPixel = CalculateBitsPerPixel(newLayer.PixelFormat, newLayer.BitsPerChannel);

            if (ImportParameters.BrotliWindowSize == 0) newLayer.BrotliWindowSize = newLayer.BitsPerPixel;

            var tileSize = CalculateTileDimension(newLayer.PixelFormat, newLayer.BitsPerChannel);
            var numTilesX = (uint)Math.Floor(newLayer.Width / (double)tileSize.w);
            var numTilesY = (uint)Math.Floor(newLayer.Height / (double)tileSize.h);

            newLayer.TileDataOffsets = new ulong[numTilesX + 1 , numTilesY + 1];
            newLayer.BaseTileDimension = tileSize;

            Info.Frames[FrameID].LayerDataOffsets?.Add(0);
            Info.Frames[FrameID].Layers?.Add(newLayer);

            return (ulong)Info.Frames[FrameID].Layers.Count - 1;
        }

        private (uint w, uint h) CalculateTileDimensionForCoordinate((uint w, uint h) LayerDimension, (uint w, uint h) TileSize, (uint x, uint y) TileIndex) {
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
        #endregion

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