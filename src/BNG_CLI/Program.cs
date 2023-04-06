using BNG_CORE;
using MemoryPack;
using MemoryPack.Compression;
using System.Text;

Header Test = new Header();

//Test.Metadata = new MetaBlock[2] { new MetaBlock() { key = "internal_id", type = "number", value = "3457892345783489" }, new MetaBlock() { key = "test", type = "string", value = "a test" } };

Test.Version = 1;
Test.Width = 100;
Test.Height = 100;
Test.FrameDataOffsets = new ulong[1] { 0x0 };

Frame myFrame = new Frame();
myFrame.LayerDataOffsets = new ulong[1] { 0x0 };
myFrame.ResolutionH = 100;
myFrame.ResolutionV = 100;


Layer myLayer = new Layer();
myLayer.Name = "Test Layer";
myLayer.Description = "This is only a test";
myLayer.PixelFormat = PixelFormat.RGB;
myLayer.BitsPerChannel = BitsPerChannel.BPC_UInt8;
myLayer.OffsetX = 0;
myLayer.OffsetY = 0;
myLayer.Width = 100;
myLayer.Height = 100;
myLayer.BlendMode = LayerBlendMode.Normal;
myLayer.TileDataOffsets = new ulong[1,1] { { 0x0 } };

Tile myTile = new Tile();
myTile.Width = (ushort)myLayer.Width;
myTile.Height = (ushort)myLayer.Height;
myTile.CompressionAlgo = CompressionAlgorithm.None;
myTile.EntropyEncoding = EntropyEncoding.None;
//var imgdata = new byte[myTile.Width * myTile.Height * 3];

//var rand = new Random();
//rand.NextBytes(imgdata);

myLayer.Tiles = new Tile[1,1] { { myTile } };


myFrame.Layers = new Layer[1] { myLayer };
Test.Frames = new Frame[1] { myFrame };

FileStream fs = new FileStream("test.bng", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 1024 * 1024, FileOptions.RandomAccess);
fs.SetLength(0);
var Compressor = new BrotliCompressor(11, 24);
MemoryPackSerializer.Serialize(Compressor, Test);
var meta = Compressor.ToArray();
Compressor.Dispose();
fs.Write(Encoding.UTF8.GetBytes("BNG!"));
fs.Write(BitConverter.GetBytes((uint)meta.Length));
fs.Write(meta);
//fs.Write(imgdata);
fs.Close();
