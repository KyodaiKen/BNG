using BNG_CORE;
using MemoryPack;
using System.IO.Compression;
using System.Text;

BNG_CORE.File Test = new BNG_CORE.File();

Test.version = 1;
Test.width = 100;
Test.height = 100;
Test.frame_data_offsets = new ulong[1] { 0x0 };

Frame myFrame = new Frame();
myFrame.layer_data_offsets = new ulong[1] { 0x0 };


Layer myLayer = new Layer();
myLayer.name = "Test Layer";
myLayer.descr = "This is just a test";
myLayer.pixel_format = PixFmt.RGB;
myLayer.bits = Bits.BPC_BYTE;
myLayer.offset_x = 0;
myLayer.offset_y = 0;
myLayer.width = 100;
myLayer.height = 100;
myLayer.blend_mode = LayerBlendMode.Normal;
myLayer.tile_data_offsets = new ulong[1,1] { { 0x0 } };

Tile myTile = new Tile();
myTile.width = 100;
myTile.height = 100;
myTile.compression_algo = CompressionAlgorithm.None;
myTile.entropy_encoding = EntropyEncoding.None;
myTile.data = new byte[myTile.width * myTile.height * 3];

var rand = new Random();
rand.NextBytes(myTile.data);

myLayer.tiles = new Tile[1,1] { { myTile } };


myFrame.layers = new Layer[1] { myLayer };
Test.frames = new Frame[1] { myFrame };

FileStream fs = new FileStream("test.bng", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 1024 * 1024, FileOptions.RandomAccess);
var meta = MemoryPackSerializer.Serialize(Test);
fs.Write(Encoding.UTF8.GetBytes("BNG!"));
var zm = new MemoryStream();
var z = new ZLibStream(zm, CompressionLevel.SmallestSize);
z.Write(meta);
z.Flush();
zm.Flush();

fs.Write(BitConverter.GetBytes((uint) zm.Length));
byte[] zmeta = new byte[zm.Length];
zm.Position = 0;
zm.Read(zmeta);
fs.Write(zmeta);
fs.Write(Test.frames[0].layers[0].tiles[0, 0].data);
fs.Close();
