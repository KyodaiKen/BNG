using BNG_CORE;
using System.IO.Compression;

Bitmap test = new Bitmap("N:\\commissions\\Bianca\\The_Artspear\\Ref Sheet\\Bianca Overhaul - Variations\\gauntlets-gigapixel.raw", new RAWImportParameters() {
    SourceDimensions            = (32000, 13956)
  , SourcePixelFormat           = PixelFormat.RGB
  , SourceBitsPerChannel        = BitsPerChannel.BPC_UInt8
  , Resolution                  = (72, 72) //DPI
  , CompressionPreFilter        = CompressionPreFilter.None
  , Compression                 = Compression.Brotli
  , CompressionLevel            = 2
  , CompressionWordSize         = 24
});

void pChanged(double progress) {
    Console.CursorLeft = 0;
    Console.Write(string.Format("{0:0.00}%", progress));
}

test.ProgressChangedEvent += pChanged;

Stream outFile = new FileStream("N:\\commissions\\Bianca\\The_Artspear\\Ref Sheet\\Bianca Overhaul - Variations\\gauntlets-gigapixel.bng", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 0x100000);
test.WriteBitmapFile(ref outFile);
outFile.Close();
outFile.Dispose();