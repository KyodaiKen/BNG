using BNG_CORE;
using CommandLine;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace BNG_CLI {
    public enum Task {
        Encode,
        Decode
    }
    public class Options {
        [Option('t', "task", Required = true, HelpText = "Task to be done: Encode, Decode")]
        public Task Task { get; set; } = Task.Encode;
        [Option('i', "input", Required = true, HelpText = "Input file to be processed (RAW Bitmap only)")]
        public string InputFile { get; set; } = "";
        [Option('o', "output", Required = true, HelpText = "Output file to be written to")]
        public string OutputFile { get; set; } = "";
        [Option('w', "src-width", Required = false, HelpText = "Source width", Default = (uint)1920)]
        public uint SrcWidth { get; set; } = 0;
        [Option('h', "src-height", Required = false, HelpText = "Source height", Default = (uint)1080)]
        public uint SrcHeight { get; set; } = 0;
        [Option('p', "src-pix-fmt", Required = false, HelpText = "Source pixel format", Default = PixelFormat.RGB)]
        public PixelFormat PixFmt { get; set; } = PixelFormat.RGB;
        [Option('b', "src-bits-per-channel", Required = false, HelpText = "Source bits per channel", Default = (uint)8)]
        public uint BPC { get; set; } = 8;
        [Option('d', "src-channel-data-format", Required = false, HelpText = "Source channel data format (IntegerUnsigned, IntegerSigned, FloatIEEE)", Default = PixelChannelDataType.IntegerUnsigned)]
        public PixelChannelDataType PixelChannelDataType { get; set; } = PixelChannelDataType.IntegerUnsigned;
        [Option("src-res-h", Required = false, HelpText = "Source horizontal resolution in dpi", Default = 72)]
        public double SrcResolutionH { get; set; } = 72;
        [Option("src-res-v", Required = false, HelpText = "Source vertical resolution in dpi", Default = 72)]
        public double SrcResolutionV { get; set; } = 72;
        [Option('c', "compressor", Required = false, HelpText = "Compression algorithm: None, GZIP, ZLIB, Brotli, ZSTD, ArithmeticOrder0, LZ4, LZMA", Default = Compression.ZSTD)]
        public Compression Compression { get; set; } = Compression.ZSTD;
        [Option('f', "filter", Required = false, HelpText = "Compression filter: None, Sub, Up, Average, Paeth", Default = CompressionPreFilter.Paeth)]
        public CompressionPreFilter CompressionFilter { get; set; } = CompressionPreFilter.Paeth;
        [Option('l', "level", Required = false, HelpText = "Compression level", Default = 8)]
        public int CompressionLevel { get; set; } = 8;
    }
    class Program {
        static int Main(string[] args) {
            Stopwatch sw = new();
            sw.Start();

            TextWriter Help = new StringWriter();
            Parser cmdParser = new Parser(why);

            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-us");
            Console.OutputEncoding = Encoding.UTF8;

            void why(ParserSettings fy) {
                fy.HelpWriter = Help;
                fy.CaseSensitive = false;
            }

            var parms = cmdParser.ParseArguments<Options>(args);
            parms.WithParsed(o => {
                switch (o.Task) {
                    case Task.Encode:
                        Bitmap BNG = new Bitmap(o.InputFile, new RAWImportParameters() {
                          SourceDimensions = (o.SrcWidth, o.SrcHeight)
                        , SourcePixelFormat = o.PixFmt
                        , TargetPixelFormat = o.PixFmt
                        , SourceBitsPerChannel = o.BPC
                        , TargetBitsPerChannel = o.BPC
                        , SourceDataType = o.PixelChannelDataType
                        , TargetDataType = o.PixelChannelDataType
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
                        BNG.WriteBNGFrame(ref outFile);
                        outFile.Close();
                        outFile.Dispose();
                        break;
                    case Task.Decode:
                        Bitmap BNGToDecode = new Bitmap();
                        Stream inFile = new FileStream(o.InputFile, FileMode.Open, FileAccess.Read, FileShare.Read, 0xFF00000);
                        StringBuilder log;
                        BNGToDecode.LoadBNG(ref inFile, out log);
                        Console.WriteLine(log.ToString());

                        Stream outFileDec = new FileStream(o.OutputFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 0x100000);
                        void pChangedDec(double progress) {
                            Console.CursorLeft = 0;
                            Console.Write(string.Format("{0:0.00}%", progress));
                        }

                        BNGToDecode.ProgressChangedEvent += pChangedDec;
                        BNGToDecode.DecodeFrameToRaw(ref inFile, ref outFileDec, 0, 0);

                        outFileDec.Flush();
                        outFileDec.Close();
                        outFileDec.Dispose();

                        inFile.Close();
                        inFile.Dispose();
                        break;
                }
            });

            Console.WriteLine();
            Console.Write(string.Format("Processing took {0}", sw.Elapsed));

            if (parms.Errors.Count() > 0) Console.Write(Help);
            return 0;
        }
    }
}
