using BNG_CORE;
using CommandLine;

namespace BNG_CLI {
    public enum Task {
        Encode,
        Decode
    }
    public class Options {
        [Option('t', "task", Required = true, HelpText = "Task to be done")]
        public Task Task { get; set; } = Task.Encode;
        [Option('i', "input", Required = true, HelpText = "Input file to be processed")]
        public string InputFile { get; set; } = "";
        [Option('o', "output", Required = true, HelpText = "Output file to be written to")]
        public string OutputFile { get; set; } = "";
        [Option("src-width", Required = true, HelpText = "Source width")]
        public uint SrcWidth { get; set; }
        [Option("src-height", Required = true, HelpText = "Source height")]
        public uint SrcHeight { get; set; }
        [Option('p', "src-pix-fmt", Required = false, HelpText = "Source pixel format")]
        public PixelFormat PixFmt { get; set; } = PixelFormat.RGB;
        [Option('b', "src-bits-per-channel", Required = false, HelpText = "Source bits per channel")]
        public BitsPerChannel BPC { get; set; } = BitsPerChannel.BPC_UInt8;
        [Option("src-res-h", Required = false, HelpText = "Source horizontal resolution in dpi")]
        public double SrcResolutionH { get; set; } = 72;
        [Option("src-res-v", Required = false, HelpText = "Source vertical resolution in dpi")]
        public double SrcResolutionV { get; set; } = 72;
        [Option('c', "compressor", Required = false, HelpText = "Compression algorithm")]
        public Compression Compression { get; set; } = Compression.ZSTD;
        [Option('f', "filter", Required = false, HelpText = "Compression filter")]
        public CompressionPreFilter CompressionFilter { get; set; } = CompressionPreFilter.Paeth;
        [Option('l', "level", Required = false, HelpText = "Compression level")]
        public int CompressionLevel { get; set; } = 8;
    }
    class Program {
        static int Main(string[] args) {
            TextWriter Help = new StringWriter();
            Parser cmdParser = new Parser(why);

            void why(ParserSettings fy) {
                fy.HelpWriter = Help;
            }

            var parms = cmdParser.ParseArguments<Options>(args);
            parms.WithParsed(o => {
                 Bitmap BNG = new Bitmap(o.InputFile, new RAWImportParameters() {
                   SourceDimensions = (o.SrcWidth, o.SrcHeight)
                 , SourcePixelFormat = o.PixFmt
                 , SourceBitsPerChannel = o.BPC
                 , Resolution = (o.SrcResolutionH, o.SrcResolutionV)
                 , CompressionPreFilter = o.CompressionFilter
                 , Compression = o.Compression
                 , CompressionLevel = o.CompressionLevel
                 });


                void pChanged(double progress) {
                    Console.CursorLeft = 0;
                    Console.Write(string.Format("{0:0.00}%", progress));
                }

                BNG.ProgressChangedEvent += pChanged;

                Stream outFile = new FileStream(o.OutputFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 0x800000);
                BNG.WriteBitmapFile(ref outFile);
                outFile.Close();
                outFile.Dispose();
            });

            if (parms.Errors.Count() > 0) Console.Write(Help);
            return 0;
        }
    }
}
