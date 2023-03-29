using BNG_CORE;
using MemoryPack;

BNG_CORE.File Test = new BNG_CORE.File();

Test.version = 1;
Test.width = 100;
Test.height = 100;

Frame myFrame = new Frame();
myFrame.layer_data_offsets = new ulong[1] { 0x0 };


Layer myLayer = new Layer();
myLayer.name = "Test jusdwhfjkdsahlfkjasdfh asdf";
myLayer.descr = "asdkljhasd jkfhasdjkfnasdhjkfnasdjk asdjkasdjkjk asdjkasdfh asdfhasdjkfh asdkjlfhaksjdfhasdfkjlsdjkf askdfjkahsdfkj asdjfkölasdf";
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
fs.Write(MemoryPackSerializer.Serialize(Test));
fs.Close();

fs = new FileStream("test.bng", FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.RandomAccess);
byte[] read = new byte[fs.Length];
fs.Read(read);
fs.Close();

var deser = MemoryPackSerializer.Deserialize<BNG_CORE.File>(read);

fs = new FileStream("test1.bng", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 1024 * 1024, FileOptions.RandomAccess);
fs.Write(MemoryPackSerializer.Serialize(deser));
fs.Close();