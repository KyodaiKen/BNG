namespace BNG_CORE {
    using BNG_CORE.Filters;
    using MemoryPack;
    using MemoryPack.Compression;
    using System;
    using System.Diagnostics;
    using System.IO.Compression;
    using System.Text;
    using ZstdSharp;
    using ZstdSharp.Unsafe;

    [Flags]
    public enum Flags : byte {
        STREAMING_OPTIMIZED = 1,
        COMPRESSED_HEADER   = 2
    }
    public enum LayerBlendMode : byte {
        Normal   = 0x00,
        Replace  = 0x00,
        Multiply = 0x01,
        Divide   = 0x02
    }

    public enum CompressionPreFilter : byte {
        None    = 0x00,
        Sub     = 0x10,
        Up      = 0x20,
        Average = 0x30,
        Paeth   = 0x40,
        All     = 0xFF
    }

    [Flags]
    public enum Compression : byte {
        None   = 0,
        Brotli = 8,
        ZSTD   = 16
    }

    public enum PixelFormat : byte {
        GRAY   = 0x10,
        GRAYA  = 0xA1,
        RGB    = 0x0B,
        RGBA   = 0xAB,
        CMYK   = 0x0C,
        CMYKA  = 0xAC,
        YCrCb  = 0x0F,
        YCrCbA = 0xAF
    }

    public enum PixelChannelDataType : byte {
        IntegerUnsigned = 0xA0,
        IntegerSigned   = 0xA1,
        FloatIEEE       = 0xF0
    }

    [MemoryPackable]
    public partial class DataBlock {
        public string? Key { get; set; }
        public uint Type { get; set; }
        public byte[] Value { get; set; }
    }

    [MemoryPackable]
    public partial class Layer {
        [MemoryPackIgnore]
        public string SourceFileName { get; set; } = string.Empty;
        public string Name { get; set; } = "layer";
        public string Description { get; set; } = string.Empty;
        public PixelFormat PixelFormat { get; set; } = PixelFormat.RGB;
        public uint BitsPerChannel { get; set; } = 8;
        public PixelChannelDataType PixelChannelDataType { get; set; } = PixelChannelDataType.IntegerUnsigned;
        [MemoryPackIgnore]
        public int NumChannels { get; set; } = 3;
        [MemoryPackIgnore]
        public int BitsPerPixel { get; set; } = 24;
        [MemoryPackIgnore]
        public int BytesPerPixel { get; set; } = 3;
        public CompressionPreFilter CompressionPreFilter { get; set; } = CompressionPreFilter.None;
        public Compression Compression { get; set; } = Compression.Brotli;
        [MemoryPackIgnore]
        public int CompressionLevel { get; set; } = 3;
        [MemoryPackIgnore]
        public int BrotliWindowSize { get; set; } = 0;
        public LayerBlendMode BlendMode { get; set; } = LayerBlendMode.Normal;
        public double Opacity { get; set; } = 1.0;
        [MemoryPackIgnore]
        public bool Enabled { get; set; } = true;
        public uint OffsetX { get; set; } = 0;
        public uint OffsetY { get; set; } = 0;
        public uint Width { get; set; } = 0;
        public uint Height { get; set; } = 0;
        public List<DataBlock>? ExtraData { get; set; }
        public ulong[,] TileDataLengths { get; set; }
        public (uint w, uint h) BaseTileDimension { get; set; }
        [MemoryPackIgnore]
        public (uint w, uint h)[,] TileDimensions { get; set; }
        [MemoryPackIgnore]
        public ulong[,] TileDataOffsets { get; set; }
    }

    [MemoryPackable]
    public partial class Header {
        [MemoryPackIgnore]
        public Flags Flags { get; set; }
        [MemoryPackIgnore]
        public byte Version { get; set; }
        [MemoryPackIgnore]
        public ulong HeaderOffset { get; set; }
        [MemoryPackIgnore]
        public ulong HeaderLength { get; set; }
        [MemoryPackIgnore]
        public int InitLength { get; set; }
        [MemoryPackIgnore]
        public ulong DataStartOffset { get; set; }
        [MemoryPackIgnore]
        public ulong DataLength { get; set; }
        public uint Width { get; set; } = 0;
        public uint Height { get; set; } = 0;
        public double ResolutionH { get; set; }
        public double ResolutionV { get; set; }
        public double DisplayTime { get; set; } = 1 / 12; // Frame display time in seconds for animations
        public List<DataBlock>? Metadata { get; set; }
        public ulong[] LayerDataLengths { get; set; }
        public List<Layer>? Layers { get; set; }
        [MemoryPackIgnore]
        public ulong[] LayerDataOffsets { get; set; }
        [MemoryPackIgnore]
        public float TileSizeFactor { get; set; } = 1;
        [MemoryPackIgnore]
        public float MaxRepackMemoryPercentage { get; set; } = 80;
    }


    public partial class ImportParameters {
        public PixelFormat TargetPixelFormat { get; set; } = PixelFormat.RGB;
        public uint TargetBitsPerChannel { get; set; } = 8;
        public PixelChannelDataType TargetDataType { get; set; } = PixelChannelDataType.IntegerUnsigned;
        public CompressionPreFilter CompressionPreFilter { get; set; } = CompressionPreFilter.Up;
        public Compression Compression { get; set; } = Compression.Brotli;
        public int CompressionLevel { get; set; } = 6;
        public int BrotliWindowSize { get; set; } = 0;
        public (double h, double v) Resolution { get; set; } = (72.0, 72.0);
        public string LayerName { get; set; } = string.Empty;
        public string LayerDescription { get; set; } = string.Empty;
        public double DisplayTime { get; set; } = 0.0;
        public (uint x, uint y) Offset { get; set; } = (0, 0);
        public double Opacity { get; set; } = 1.0;
        public LayerBlendMode BlendMode { get; set; } = LayerBlendMode.Normal;
        public Flags Flags { get; set;} = 0;
        public float MaxRepackMemoryPercentage { get; set; }
        public float TileSizeFactor { get; set; } = 1;
    }

    public class RAWImportParameters : ImportParameters {
        public (uint w, uint h) SourceDimensions { get; set; }
        public PixelFormat SourcePixelFormat { get; set; } = PixelFormat.RGB;
        public uint SourceBitsPerChannel { get; set; } = 8;
        public PixelChannelDataType SourceDataType { get; set; } = PixelChannelDataType.IntegerUnsigned;
    }

    public class Bitmap : IDisposable {
        private Header BNGFrame;
        private bool disposedValue;

        public bool Strict { get; set; } = false;

        public delegate void dgProgressChanged(double progress);
        public dgProgressChanged ProgressChangedEvent { get; set; }

        public Bitmap() {
            BNGFrame = new Header();
        }

        #region Loading
        public void LoadBNG(ref Stream InputStream, out StringBuilder log) {
            if (InputStream == null) throw new ArgumentNullException(nameof(File));
            if (InputStream.CanSeek == false) throw new AccessViolationException("Stream not seekable");
            if (InputStream.CanRead == false) throw new AccessViolationException("Stream not readable");

            InputStream.Seek(0, SeekOrigin.Begin);

            log = new StringBuilder();

            //Read first 3 byte and check if it's the BNG identifier
            byte[] ident = new byte[3];
            byte[] identCompare = { 0x42, 0x4e, 0x47 };
            InputStream.Read(ident);

            //Read the info byte
            byte infoByte = (byte)InputStream.ReadByte();
            BNGFrame.Flags = new();
            BNGFrame.Version = (byte)(infoByte >> 4);
            BNGFrame.Flags = (Flags)(infoByte & 0x0F);

            if (ident != identCompare) {
                if(Strict) throw new InvalidDataException("This is not a BNG file!");
            }

            //Check if the next 64 bit word which is the pointer to the header is not outside the bounds of the stream
            ulong streamLength = (ulong)InputStream.Length;
            long dataStartPosition = 0;

            //Check for web optimized header
            if (BNGFrame.Flags.HasFlag(Flags.STREAMING_OPTIMIZED)) {
                //Web optimized format
                byte[] readHeaderLength = new byte[4];
                InputStream.Read(readHeaderLength);
                uint headerLength = BitConverter.ToUInt32(readHeaderLength);
                //Try to read the header
                byte[] binaryHeader = new byte[headerLength]; //This is now the header size
                InputStream.Read(binaryHeader);

                using (var decompressor = new BrotliDecompressor()) {
                    var decompressedBuffer = decompressor.Decompress(binaryHeader);
                    BNGFrame = MemoryPackSerializer.Deserialize<Header>(decompressedBuffer);
                }

                BNGFrame.DataStartOffset = (ulong)InputStream.Position; //Data length is in the header
                BNGFrame.HeaderLength = (ulong)binaryHeader.LongLength;
                BNGFrame.HeaderOffset = 8;
                BNGFrame.InitLength = 8;
            }
            else 
            {
                byte[] readHeaderLengthOrOffset = new byte[8];
                InputStream.Read(readHeaderLengthOrOffset);
                ulong headerLengthOrOffset = BitConverter.ToUInt64(readHeaderLengthOrOffset);
                if (headerLengthOrOffset > streamLength) throw new InvalidDataException("The header offset is out of bounds");

                byte[] readHeaderLength = new byte[4];
                InputStream.Read(readHeaderLength);

                dataStartPosition = InputStream.Position; //Data length is in the header

                //Try to read the header
                byte[] binaryHeader = new byte[BitConverter.ToUInt32(readHeaderLength)];
                InputStream.Seek((long)headerLengthOrOffset - 16, SeekOrigin.Current);
                InputStream.Read(binaryHeader);

                if (BNGFrame.Flags.HasFlag(Flags.COMPRESSED_HEADER)) {
                    using (var decompressor = new BrotliDecompressor()) {
                        var decompressedBuffer = decompressor.Decompress(binaryHeader);
                        BNGFrame = MemoryPackSerializer.Deserialize<Header>(decompressedBuffer);
                    }
                } else {
                    BNGFrame = MemoryPackSerializer.Deserialize<Header>(binaryHeader);
                }

                BNGFrame.DataStartOffset = (ulong)dataStartPosition;
                BNGFrame.HeaderLength = BitConverter.ToUInt32(readHeaderLength);
                BNGFrame.HeaderOffset = headerLengthOrOffset;
                BNGFrame.InitLength = 16;
            }

            //Update those fields again as they got overwritten by reading the header
            BNGFrame.Version = (byte)(infoByte >> 4);
            BNGFrame.Flags = (Flags)(infoByte & 0x0F);

            log.AppendLine(string.Format("BNG Frame Version....: {0}", BNGFrame.Version));
            log.AppendLine(string.Format("Width................: {0}", BNGFrame.Width));
            log.AppendLine(string.Format("Height...............: {0}", BNGFrame.Height));
            log.AppendLine(string.Format("Display time (sec)...: {0}", BNGFrame.DisplayTime));
            log.AppendLine(string.Format("Horizontal resolution: {0}", BNGFrame.ResolutionH));
            log.AppendLine(string.Format("Vertical resolution..: {0}", BNGFrame.ResolutionV));
            log.AppendLine(string.Format("Number of layers.....: {0}", BNGFrame.Layers.Count));

            BNGFrame.LayerDataOffsets = new ulong[BNGFrame.Layers.Count];

            for (int layer = 0; layer < BNGFrame.Layers.Count;  layer++) {
                string lyrNum = string.Format("Layer {0}", layer) + " ";
                BNGFrame.LayerDataOffsets[layer] += BNGFrame.LayerDataLengths[layer];
                log.Append(lyrNum);
                log.AppendLine(new string('-', 40 - lyrNum.Length));
                log.AppendLine(string.Format("Layer name...........: {0}", BNGFrame.Layers[layer].Name));
                log.AppendLine(string.Format("Layer description....: {0}", BNGFrame.Layers[layer].Description));
                log.AppendLine(string.Format("Offset X.............: {0}", BNGFrame.Layers[layer].OffsetX));
                log.AppendLine(string.Format("Offset Y.............: {0}", BNGFrame.Layers[layer].OffsetY));
                log.AppendLine(string.Format("Width................: {0}", BNGFrame.Layers[layer].Width));
                log.AppendLine(string.Format("Height...............: {0}", BNGFrame.Layers[layer].Height));
                log.AppendLine(string.Format("Opacity..............: {0}", BNGFrame.Layers[layer].Opacity));
                log.AppendLine(string.Format("Pixel format.........: {0}", BNGFrame.Layers[layer].PixelFormat.ToString()));
                log.AppendLine(string.Format("Bits per channel.....: {0}", BNGFrame.Layers[layer].BitsPerChannel));
                log.AppendLine(string.Format("Channel data format..: {0}", BNGFrame.Layers[layer].PixelChannelDataType.ToString()));
                log.AppendLine(string.Format("Compression..........: {0}", BNGFrame.Layers[layer].Compression.ToString()));
                log.AppendLine(string.Format("Pre-filter...........: {0}", BNGFrame.Layers[layer].CompressionPreFilter.ToString()));
                log.AppendLine(string.Format("Base tile dimension W: {0}", BNGFrame.Layers[layer].BaseTileDimension.w));
                log.AppendLine(string.Format("Base tile dimension H: {0}", BNGFrame.Layers[layer].BaseTileDimension.h));
                log.AppendLine(string.Format("Data offset..........: {0}", BNGFrame.LayerDataOffsets[layer]));
                log.AppendLine(string.Format("Data size (bytes)....: {0}", BNGFrame.LayerDataLengths[layer]));
                log.AppendLine(string.Format("Uncompressed size (\"): {0}", BNGFrame.Layers[layer].Width * BNGFrame.Layers[layer].Height * CalculateBitsPerPixel(BNGFrame.Layers[layer].PixelFormat, BNGFrame.Layers[layer].BitsPerChannel) / 8));
                log.AppendLine(string.Format("Number of tiles......: {0}", BNGFrame.Layers[layer].TileDataLengths.LongLength));

                var txl = BNGFrame.Layers[layer].TileDataLengths.GetLongLength(0);
                var tyl = BNGFrame.Layers[layer].TileDataLengths.GetLongLength(1);
                BNGFrame.Layers[layer].TileDimensions = new (uint w, uint h)[txl, tyl];
                BNGFrame.Layers[layer].TileDataOffsets = new ulong[txl, tyl];

                StringBuilder tileInfo = new StringBuilder();

                for (uint tileY = 0; tileY < tyl; tileY++) {
                    for (uint tileX = 0; tileX < txl; tileX++) {
                        BNGFrame.Layers[layer].TileDataOffsets[tileX, tileY] += BNGFrame.Layers[layer].TileDataLengths[tileX, tileY];
                        string tleNum = string.Format("Tile {0},{1}", tileX, tileY) + " ";
                        log.Append(tleNum);
                        log.AppendLine(new string('-', 40 - tleNum.Length));
                        ulong tleSzPacked = BNGFrame.Layers[layer].TileDataLengths[tileX, tileY];
                        BNGFrame.Layers[layer].TileDataLengths[tileX, tileY] = tleSzPacked;
                        var corrTileSize = CalculateTileDimensionForCoordinate((BNGFrame.Layers[layer].Width, BNGFrame.Layers[layer].Height)
                            , (BNGFrame.Layers[layer].BaseTileDimension.w, BNGFrame.Layers[layer].BaseTileDimension.h), (tileX, tileY));
                        BNGFrame.Layers[layer].TileDimensions[tileX, tileY] = corrTileSize;
                        uint tleWzUnPacked = (uint)(corrTileSize.w * corrTileSize.h * CalculateBitsPerPixel(BNGFrame.Layers[layer].PixelFormat, BNGFrame.Layers[layer].BitsPerChannel) / 8);
                        log.AppendLine(string.Format("Actual tile dim. W...: {0}", corrTileSize.w));
                        log.AppendLine(string.Format("Actual tile dim. H...: {0}", corrTileSize.h));
                        log.AppendLine(string.Format("Data size (bytes)....: {0}", tleSzPacked));
                        log.AppendLine(string.Format("Uncompressed size (\"): {0}", tleWzUnPacked));
                    }
                }

                BNGFrame.DataLength += BNGFrame.LayerDataLengths[layer];
            }

            //Point stream to the start of the tile data
            if (!BNGFrame.Flags.HasFlag(Flags.STREAMING_OPTIMIZED)) {
                InputStream.Position -= (long)(BNGFrame.HeaderOffset + BNGFrame.HeaderLength) - BNGFrame.InitLength;
            }
        }

        public void DecodeFrameToRaw(ref Stream InputStream, ref Stream OutputStream, int FrameID, int LayerID) {
            if (InputStream == null) throw new ArgumentNullException(nameof(File));
            if (InputStream.CanSeek == false) throw new AccessViolationException("Stream not seekable");
            if (InputStream.CanRead == false) throw new AccessViolationException("Stream not readable");
            if (OutputStream == null) throw new ArgumentNullException(nameof(File));
            if (OutputStream.CanSeek == false) throw new AccessViolationException("Stream not seekable");
            if (OutputStream.CanWrite == false) throw new AccessViolationException("Stream not writable");

            var layer = BNGFrame.Layers[LayerID];
            var txl = layer.TileDataOffsets.GetLongLength(0);
            var tyl = layer.TileDataOffsets.GetLongLength(1);
            var bytesPerPixel = CalculateBitsPerPixel(layer.PixelFormat, layer.BitsPerChannel) / 8;


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

            //Point to end of the header data so the next frame can be read (if there are any)
            if (!BNGFrame.Flags.HasFlag(Flags.STREAMING_OPTIMIZED)) {
                InputStream.Seek((long)BNGFrame.HeaderLength, SeekOrigin.Current);
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
        private int CalculateBitsPerPixel(PixelFormat pixelFormat, uint bitsPerChannel) {
            switch (pixelFormat) {
                case PixelFormat.GRAY:
                    return (int)bitsPerChannel;
                case PixelFormat.GRAYA:
                    return (int)bitsPerChannel * 2;
                case PixelFormat.RGB:
                case PixelFormat.YCrCb:
                    return (int)bitsPerChannel * 3;
                case PixelFormat.RGBA:
                case PixelFormat.CMYK:
                    return (int)bitsPerChannel * 4;
                case PixelFormat.CMYKA:
                    return (int)bitsPerChannel * 5;
                default: return (int)bitsPerChannel;
            }
        }

        private (uint w, uint h) CalculateTileDimension(PixelFormat pixelFormat, uint bitsPerChannel, float tileSizeMult) {
            int bytesPerPixel = CalculateBitsPerPixel(pixelFormat, bitsPerChannel) / 8;
            float maxSize = tileSizeMult * 4096f;
            uint size = (uint)(maxSize / bytesPerPixel);
            return (size, size);
        }

        private MemoryMetrics GetCurrentlMemMetrics() {
            return new MemoryMetricsClient().GetMetrics();
        }
        #endregion

        #region Writing
        public Bitmap(string ImportFileName, ImportParameters Options) {
            BNGFrame = new Header();
            AddLayer(ImportFileName, Options);
        }

        public void AddLayer(string ImportFileName, ImportParameters Options) {
            BNGFrame.Layers ??= new();
            PrepareRAWFileToLayer(ImportFileName, (RAWImportParameters)Options);
        }
        public void WriteBNGFrame(ref Stream OutputStream) {
            if (BNGFrame == null) throw new NullReferenceException(nameof(BNGFrame));
            if (OutputStream == null) throw new ArgumentNullException(nameof(File));
            if (OutputStream.CanSeek == false) throw new AccessViolationException("Stream not seekable");
            if (OutputStream.CanWrite == false) throw new AccessViolationException("Stream not writable");

            //Truncate data if something has already been written into it from outside.
            OutputStream.SetLength(0);

            //Write magic word
            OutputStream.Write(Encoding.ASCII.GetBytes("BNG"));

            //Info byte
            BNGFrame.Version = 0;
            byte version = BNGFrame.Version;
            byte flags = (byte)BNGFrame.Flags;
            byte infoByte = (byte)(version << 4 | flags);

            OutputStream.WriteByte(infoByte);

            //Check if stream optimized flag is set
            bool optimizeInMemory = false;
            ulong offsetLengths = 0;
            string TempFileName = "";
            if (BNGFrame.Flags.HasFlag(Flags.STREAMING_OPTIMIZED)) {
                //Determine if in-memory rearrangement should and can be used
                if (BNGFrame.MaxRepackMemoryPercentage > 0) {
                    ulong maxMemoryToUse = (ulong)(BNGFrame.MaxRepackMemoryPercentage * GetCurrentlMemMetrics().Free / 100) * 0x100000;
                    ulong memToBeUsed = 0;
                    for (int layer = 0; layer < BNGFrame.Layers.Count; layer++)
                        memToBeUsed += (ulong)(BNGFrame.Layers[layer].Width * BNGFrame.Layers[layer].Height * (BNGFrame.Layers[layer].BitsPerPixel / 8));
                    if (memToBeUsed + 0x12C00000 < maxMemoryToUse) {
                        optimizeInMemory = true;
                    } else {
                        TempFileName = $@"{Guid.NewGuid()}.bng-temp";
                    }
                } else {
                    TempFileName = $@"{Guid.NewGuid()}.bng-temp";
                }
            } else {
                //Write placeholder 64 bit word:
                offsetLengths = (ulong)OutputStream.Position;
                OutputStream.Write(BitConverter.GetBytes((ulong)0x0));
                OutputStream.Write(BitConverter.GetBytes((uint)0));
            }

            if (BNGFrame.Layers == null) throw new NullReferenceException(string.Format(nameof(BNGFrame.Layers)));
            BNGFrame.LayerDataLengths = new ulong[BNGFrame.Layers.Count];

            Stream oStream = OutputStream; //No optimization, just write through


            for (int LayerID = 0; LayerID < BNGFrame.Layers.Count; LayerID++) {
                var BytesPerPixel = BNGFrame.Layers[LayerID].BitsPerPixel / 8;
                var stride = BNGFrame.Layers[LayerID].Width * BytesPerPixel;
                var numTilesX = BNGFrame.Layers[LayerID].TileDataOffsets.GetLongLength(0);
                var numTilesY = BNGFrame.Layers[LayerID].TileDataOffsets.GetLongLength(1);
                var tileSize = CalculateTileDimension(BNGFrame.Layers[LayerID].PixelFormat, BNGFrame.Layers[LayerID].BitsPerChannel, BNGFrame.TileSizeFactor);
                long bytesWritten = 0;

                BNGFrame.Layers[LayerID].TileDataLengths = new ulong[numTilesX, numTilesY];

                Stream InputStream = new FileStream(BNGFrame.Layers[LayerID].SourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 0x800000, FileOptions.RandomAccess);

                //Determine wether to use memory stream or the file stream provided.
                if (BNGFrame.Flags.HasFlag(Flags.STREAMING_OPTIMIZED) && optimizeInMemory) {
                    oStream = new MemoryStream();
                } else if (BNGFrame.Flags.HasFlag(Flags.STREAMING_OPTIMIZED) && !optimizeInMemory) {
                    oStream = new FileStream(TempFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 0x800000, FileOptions.RandomAccess);
                }

                Stopwatch sw = new();
                sw.Start();
                for (uint y = 0; y < numTilesY; y++) {
                    for (uint x = 0; x < numTilesX; x++) {
                        var corrTileSize = CalculateTileDimensionForCoordinate((BNGFrame.Layers[LayerID].Width, BNGFrame.Layers[LayerID].Height), tileSize, (x, y));
                        byte[] lineBuff = new byte[corrTileSize.w * BytesPerPixel];
                        byte[] prevLineBuff = new byte[corrTileSize.w * BytesPerPixel];
                        MemoryStream iBuff = new MemoryStream();
                        byte[] cBuff = Array.Empty<byte>();
                        long inputOffset = 0;

                        for (int line = 0; line < corrTileSize.h; line++) {
                            inputOffset = tileSize.h * y * stride + line * stride + tileSize.w * x * BytesPerPixel;
                            InputStream.Seek(inputOffset, SeekOrigin.Begin);
                            InputStream.Read(lineBuff);
                            Filter(BNGFrame.Layers[LayerID].CompressionPreFilter, corrTileSize, ref lineBuff, ref prevLineBuff, ref iBuff, BytesPerPixel);
                            Array.Copy(lineBuff, prevLineBuff, lineBuff.LongLength);
                        }

                        Compress(BNGFrame.Layers[LayerID].Compression, BNGFrame.Layers[LayerID].CompressionLevel, BNGFrame.Layers[LayerID].BrotliWindowSize, ref iBuff, ref cBuff);
                        BNGFrame.Layers[LayerID].TileDataLengths[x, y] = (ulong)cBuff.Length;
                        BNGFrame.LayerDataLengths[LayerID] += (ulong)cBuff.Length;

                        bytesWritten += lineBuff.LongLength * corrTileSize.h;
                        var progress = (double)bytesWritten / InputStream.Length * 100.0;
                        if (sw.ElapsedMilliseconds >= 250 || progress == 100.0) {
                            sw.Restart();
                            ProgressChangedEvent?.Invoke(progress);
                        }

                        oStream.Write(cBuff);
                        BNGFrame.DataLength += (ulong)cBuff.LongLength;
                    }
                }
            }
            
            //Serialize the metadata and then compress it using Brotli
            byte[] headerData;
            if (BNGFrame.Flags.HasFlag(Flags.COMPRESSED_HEADER)) {
                var Compressor = new BrotliCompressor(11, 24);
                MemoryPackSerializer.Serialize(Compressor, BNGFrame);
                headerData = Compressor.ToArray();
                Compressor.Dispose();
            }
            else {
                headerData = MemoryPackSerializer.Serialize(BNGFrame);
            }
            BNGFrame.HeaderLength = (ulong)headerData.LongLength;

            ulong dataEndPosition = (ulong)OutputStream.Position;
            if (BNGFrame.Flags.HasFlag(Flags.STREAMING_OPTIMIZED)) {
                dataEndPosition = (ulong)oStream.Length + BNGFrame.HeaderLength;
                OutputStream.Write(BitConverter.GetBytes((uint)BNGFrame.HeaderLength));
                OutputStream.Write(headerData);
                oStream.Flush();
                oStream.Seek(0, SeekOrigin.Begin);
                for (int i = 0; i < oStream.Length; i += 0x800000) {
                    byte[] block = new byte[oStream.Length - oStream.Position > 0x800000 ? 0x800000 : oStream.Length - oStream.Position];
                    oStream.Read(block);
                    OutputStream.Write(block);
                }
                oStream.Close();
                oStream.Dispose();
                if (BNGFrame.Flags.HasFlag(Flags.STREAMING_OPTIMIZED) && !optimizeInMemory)
                    File.Delete(TempFileName);                    
            } else {
                OutputStream.Write(headerData);
                OutputStream.Position = (long)offsetLengths;
                OutputStream.Write(BitConverter.GetBytes(dataEndPosition));
                OutputStream.Write(BitConverter.GetBytes((uint)headerData.Length));
                OutputStream.Position = (long)dataEndPosition + (long)BNGFrame.HeaderLength;
            }

            OutputStream.Flush();

            Array.Clear(headerData);
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

        public ulong PrepareTIFFToLayer(string tiffFileName, int FrameID, ImportParameters importParameters) {
            
            return 0;
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
        public ulong PrepareRAWFileToLayer(string RAWFileName, RAWImportParameters ImportParameters) {
            BNGFrame.Width = ImportParameters.SourceDimensions.w;
            BNGFrame.Height = ImportParameters.SourceDimensions.h;
            BNGFrame.Flags = ImportParameters.Flags;
            BNGFrame.TileSizeFactor = ImportParameters.TileSizeFactor;
            BNGFrame.MaxRepackMemoryPercentage = ImportParameters.MaxRepackMemoryPercentage;

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
            if (ImportParameters.SourceDataType != ImportParameters.TargetDataType) throw new NotImplementedException("Data type conversion is not supported yet");
            newLayer.PixelChannelDataType = ImportParameters.TargetDataType;
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

            var tileSize = CalculateTileDimension(newLayer.PixelFormat, newLayer.BitsPerChannel, BNGFrame.TileSizeFactor);
            var numTilesX = (uint)Math.Floor(newLayer.Width / (double)tileSize.w);
            var numTilesY = (uint)Math.Floor(newLayer.Height / (double)tileSize.h);

            newLayer.TileDataOffsets = new ulong[numTilesX + 1 , numTilesY + 1];
            newLayer.BaseTileDimension = tileSize;

            BNGFrame.Layers.Add(newLayer);

            return (ulong)BNGFrame.Layers.Count - 1;
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