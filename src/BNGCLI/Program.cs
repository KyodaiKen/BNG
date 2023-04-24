using BNGCORE;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace BNG_CLI {
    public enum Task {
        Encode,
        Decode
    }

    public struct FileSource {
        public string pathName;
        public RAWImportParameters importParameters;
        public string outputDirectory;
    }
    class ParseArguments {
        public Task Task { get; set; } = Task.Decode;
        public List<FileSource> InputFiles { get; set; }
        public string OutputFile { get; set; } = "";
        public bool ErrorState { get; set; }
        public int VerboseLevel { get; set; } = 1;

        private string[] _args;
        private StringWriter Output { get; set; }
        private StringWriter Help { get; set; }


        private delegate void dgFoundArgCallback(long index);
        private delegate void dgArgNotFoundCallback();
        private delegate void dgEnumNotFound();
        private delegate void dgEnumFound(object enumVal);

        public ParseArguments(string[] args) {
            Output = new();
            Help = new();
            _args = args;
            ErrorState = false;
            Task _Task = Task;

            if (_args.Length == 0) {
                ErrorState = true;
            }

            uint width = 0, height = 0, lox = 0, loy = 0;
            double lsx = 1, lsy = 1;

            InputFiles = new();
            


            Help.WriteLine("Usage ------------------------------------------");
            Help.WriteLine("BNGCLI [-e -i <\"filename=<file>;key=value,filename=<file>;key=value;key=value\" notation> <output file name>] [-i <bng file> <output path>] \n");

            //Scan for encoding task
            Help.Write("-e  Tell the program to go into encode mode. If not set, BNG_CLI will assume a BNG file to be decoded.\n\n");
            FindArg("-e", FoundE);
            Task = Task.Decode;
            void FoundE(long index) {
                _Task = Task.Encode;
            }

            FindArg("-v", FoundV);
            void FoundV(long index)
            {
                if (index + 1 <= _args.Length)
                {
                    int parsedVerboseLevel = 0;
                    int.TryParse(_args[index + 1], out parsedVerboseLevel);
                    VerboseLevel = parsedVerboseLevel;
                }
            }

            if (_Task == Task.Encode) {
                //Encode
                Help.WriteLine("Options for encoding--------------------------------------\n");
                Help.WriteLine("BNGCLI -e -i \"fn=myfile.raw,w=1024,h=768,ensop=80;fn=my other file.raw,w=1280,h=720,ensop=80\" my.bng\n");
                Help.WriteLine("-i  List of input files and parameters. \"fn=<file>,key=value;fn=<file>,key=value,key=value\" notation. SINGLE-Quote things that contain ; or ,. (Required)\n");
                Help.WriteLine("\n  Input file\n");
                Help.WriteLine("    fn=    (Required)                 Input file name               Relative or absolute file path");
                Help.WriteLine("    fsqb=  (Default=None)             File sequence begin number    Integer from 0 to " + long.MaxValue.ToString());
                Help.WriteLine("    fsqe=  (Default=None)             File sequence end number      Integer from 0 to " + long.MaxValue.ToString());

                Help.WriteLine("\n  RAW import\n");
                Help.WriteLine("    w=     (Required)                 RAW image width               Integer from 0 to " + uint.MaxValue.ToString());
                Help.WriteLine("    h=     (Required)                 RAW image height              Integer from 0 to " + uint.MaxValue.ToString());
                Help.WriteLine("    cs=    (Default=RGB)              RAW image Color space         { RGB, RGBA, YCrCb, YCrCbA, CMYK, CMYKA }");
                Help.WriteLine("    pf=    (Default=IntegerUnsigned)  RAW image Pixel format        { IntegerUnsigned, IntegerSigned, FloatIEEE }");
                Help.WriteLine("    bpc=   (Default=8)                RAW image Bits per CHANNEL    { 8, 16, 32, 64 }");

                Help.WriteLine("\n  Frame\n");
                Help.WriteLine("    frnm=  (Default=Empty)            Frame name                    Free text");
                Help.WriteLine("    frdc=  (Default=Empty)            Frame description             Free text");
                Help.WriteLine("    cw=    (Default=Auto)             Frame canvas width            Integer from 0 to " + uint.MaxValue.ToString());
                Help.WriteLine("    ch=    (Default=Auto)             Frame canvas height           Integer from 0 to " + uint.MaxValue.ToString());
                Help.WriteLine("    frdur= (Default=1/15)             Frame display duration        Seconds with decimal places");
                Help.WriteLine("    fropn= (Default=0)                Enter 1 if you want to add more layers to this frame with future inputs");
                Help.WriteLine("    fccs=  (Default=First Layer cs)   Compositing Color space       { RGB, RGBA, YCrCb, YCrCbA, CMYK, CMYKA }");
                Help.WriteLine("    fcpf=  (Default=First Layer cpf)  Compositing Pixel format      { IntegerUnsigned, IntegerSigned, FloatIEEE }");
                Help.WriteLine("    fcbpc= (Default=First Layer bpc)  Compositing Bits per CHANNEL  { 8, 16, 32, 64 }");

                Help.WriteLine("\n  Layer\n");
                Help.WriteLine("    lnm=   (Default=Filename w/o ext) Layer name                    Free text");
                Help.WriteLine("    ldc=   (Default=Empty)            Layer description             Free text");
                Help.WriteLine("    lox=   (Default=0)                Layer offset X                Integer from 0 to " + uint.MaxValue.ToString());
                Help.WriteLine("    loy=   (Default=0)                Layer offset Y                Integer from 0 to " + uint.MaxValue.ToString());
                Help.WriteLine("    lop=   (Default=1)                Layer opacity                 Fraction between 0 and 1");
                Help.WriteLine("    lbm=   (Default=Normal)           Layer blend mode              { Normal, Multiply, Divide, Subtract }");
                Help.WriteLine("    ltc=   (Default=0)                Enter 1 if you want to add this image as a layer to the current OPEN frame");
                Help.WriteLine("    lcf=   (Default=0)                Enter 1 if you want this layer to close the current OPEN frame");

                Help.WriteLine("\n  Compression and file layout\n");
                Help.WriteLine("    flt=   (Default=Average)          Compression pre-filter        { Sub, Up, Average, Median, Median2, Paeth }");
                Help.WriteLine("    compr= (Default=LZW)              Compression algorithm         { Brotli, LZW, ZSTD }");
                Help.WriteLine("    level= (Default=N/A)              Compression level             Brotli: 0...11, ZSTD: 1 ... 22, LZW: N/A");
                Help.WriteLine("    bwnd=  (Default=bpc,max 24)       Brotli window size            1...24");
                Help.WriteLine("    ensop= (Default=80)               Enable streaming optimizer    Value (float) defines the percentage of FREE memory to be used.\n"+
                               "                                                                    If more is needed than set here, a temporary file in the\n"+
                               "                                                                    destination path is used instead.");

                FindArg("-i", FoundMI, NotFoundMI);
                void FoundMI(long index) {
                    if (index + 1 <= _args.Length) {
                        string[] inputs = Split(_args[index + 1], ';');
                        foreach (string input in inputs) {

                            FileSource fileinfo = new();
                            fileinfo.pathName = "";
                            fileinfo.importParameters = new();
                            fileinfo.importParameters.Flags = Flags.COMPRESSED_HEADER;
                            string[] options = Split(input, ',');
                            foreach (string option in options) {
                                string[] tuple = new string[2];
                                tuple[0] = option.Substring(0, option.IndexOf('='));
                                tuple[1] = option.Substring(option.IndexOf('=') + 1);
                                switch (tuple[0]) {
                                    case "fn":
                                        fileinfo.pathName = tuple[1];
                                        break;
                                    case "w":
                                        if (!uint.TryParse(tuple[1], out width)) {
                                            Output.WriteLine("Error: Illegal number for w. Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        break;
                                    case "h":
                                        if (!uint.TryParse(tuple[1], out height)) {
                                            Output.WriteLine("Error: Illegal number for h. Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        break;
                                    case "cs":
                                        AssignEnum(typeof(ColorSpace), tuple[1], CSNotFound, CSFound);
                                        void CSFound(object enumVal) {
                                            fileinfo.importParameters.SourceColorSpace = (ColorSpace)enumVal;
                                        }
                                        void CSNotFound() {
                                            Output.WriteLine("Error: Value for cs is invalid!");
                                            ErrorState = true;
                                        }
                                        break;
                                    case "pf":
                                        AssignEnum(typeof(PixelFormat), tuple[1], PFNotFound, PFFound);
                                        void PFFound(object enumVal) {
                                            fileinfo.importParameters.SourcePixelFormat = (PixelFormat)enumVal;
                                        }
                                        void PFNotFound() {
                                            Output.WriteLine("Error: Value for cs is invalid!");
                                            ErrorState = true;
                                        }
                                        break;
                                    case "bpc":
                                        uint value = 0;
                                        if (!uint.TryParse(tuple[1], out value)) {
                                            Output.WriteLine("Error: Illegal number for bpc. Please enter one of 8, 16, 32, 64.");
                                            ErrorState = true;
                                            break;
                                        }
                                        if (value != 0 || value != 8 || value != 16 || value != 32 | value != 64) {
                                            Output.WriteLine("Error: Illegal number for bpc. Please enter one of 8, 16, 32, 64.");
                                            ErrorState = true;
                                            break;
                                        }
                                        else {
                                            fileinfo.importParameters.SourceBitsPerChannel = value;
                                        }
                                        break;
                                    case "frnm":
                                        fileinfo.importParameters.FrameName = tuple[1];
                                        break;
                                    case "frdc":
                                        fileinfo.importParameters.FrameDescription = tuple[1];
                                        break;
                                    case "cw":
                                        uint cw = 0;
                                        if (!uint.TryParse(tuple[1], out cw)) {
                                            Output.WriteLine("Error: Illegal number for cw. Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        if (cw < 0) {
                                            Output.WriteLine("Error: Illegal number for cw. Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.FrameWidth = cw;
                                        break;
                                    case "ch":
                                        uint ch = 0;
                                        if (!uint.TryParse(tuple[1], out cw)) {
                                            Output.WriteLine("Error: Illegal number for ch. Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        if (cw < 0) {
                                            Output.WriteLine("Error: Illegal number for ch. Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.FrameHeight = ch;
                                        break;
                                    case "frdur":
                                        long fDur = 0;
                                        if (!long.TryParse(tuple[1], out fDur)) {
                                            Output.WriteLine("Error: Illegal number for frdur. Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        if (fDur < 0) {
                                            Output.WriteLine("Error: Illegal number for frdur. Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.FrameDuration = fDur;
                                        break;
                                    case "fropn":
                                        int fropn = 0;
                                        if (!int.TryParse(tuple[1], out fropn)) {
                                            Output.WriteLine("Error: Illegal number for l4f Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        if (fropn < 0) {
                                            Output.WriteLine("Error: Illegal number for l4f Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.OpenFrame = fropn == 1;
                                        break;
                                    case "fccs":
                                        AssignEnum(typeof(ColorSpace), tuple[1], FCCSNotFound, FCCSFound);
                                        void FCCSFound(object enumVal) {
                                            fileinfo.importParameters.CompositingColorSpace = (ColorSpace)enumVal;
                                        }
                                        void FCCSNotFound() {
                                            Output.WriteLine("Error: Value for cs is invalid!");
                                            ErrorState = true;
                                        }
                                        break;
                                    case "fcpf":
                                        AssignEnum(typeof(PixelFormat), tuple[1], FCPFNotFound, FCPFFound);
                                        void FCPFFound(object enumVal) {
                                            fileinfo.importParameters.CompositingPixelFormat = (PixelFormat)enumVal;
                                        }
                                        void FCPFNotFound() {
                                            Output.WriteLine("Error: Value for cs is invalid!");
                                            ErrorState = true;
                                        }
                                        break;
                                    case "fcbpc":
                                        uint fcbpc = 0;
                                        if (!uint.TryParse(tuple[1], out fcbpc)) {
                                            Output.WriteLine("Error: Illegal number for bpc. Please enter one of 8, 16, 32, 64.");
                                            ErrorState = true;
                                            break;
                                        }
                                        if (fcbpc != 0 || fcbpc != 8 || fcbpc != 16 || fcbpc != 32 | fcbpc != 64) {
                                            Output.WriteLine("Error: Illegal number for bpc. Please enter one of 8, 16, 32, 64.");
                                            ErrorState = true;
                                            break;
                                        }
                                        else {
                                            fileinfo.importParameters.CompositingBitsPerChannel = fcbpc;
                                        }
                                        break;
                                    case "ltc":
                                        int ltc = 0;
                                        if (!int.TryParse(tuple[1], out ltc)) {
                                            Output.WriteLine("Error: Illegal number for l4f Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        if (ltc < 0) {
                                            Output.WriteLine("Error: Illegal number for l4f Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.LayerToCurrentFrame = ltc == 1;
                                        break;
                                    case "lnm":
                                        fileinfo.importParameters.LayerName = tuple[1];
                                        break;
                                    case "ldc":
                                        fileinfo.importParameters.LayerDescription = tuple[1];
                                        break;
                                    case "lox":
                                        if (!uint.TryParse(tuple[1], out lox)) {
                                            Output.WriteLine("Error: Illegal number for lox. Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        break;
                                    case "loy":
                                        if (!uint.TryParse(tuple[1], out loy)) {
                                            Output.WriteLine("Error: Illegal number for loy. Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        break;
                                    case "lop":
                                        double lop;
                                        if (!double.TryParse(tuple[1], out lop)) {
                                            Output.WriteLine("Error: Illegal number for lop. Please enter only numbers and a decimal point.");
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.LayerOpacity = lop;
                                        break;
                                    case "lbm":
                                        AssignEnum(typeof(PixelFormat), tuple[1], LBMNotFound, LBMFound);
                                        void LBMFound(object enumVal) {
                                            fileinfo.importParameters.LayerBlendMode = (LayerBlendMode)enumVal;
                                        }
                                        void LBMNotFound() {
                                            Output.WriteLine("Error: Value for lbm is invalid!");
                                            ErrorState = true;
                                        }
                                        break;
                                    case "lcf":
                                        int lcf = 0;
                                        if (!int.TryParse(tuple[1], out lcf)) {
                                            Output.WriteLine("Error: Illegal number for l4f Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        if (lcf < 0) {
                                            Output.WriteLine("Error: Illegal number for l4f Please enter an integer number between 0 and " + uint.MaxValue.ToString());
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.LayerClosesFrame = lcf == 1;
                                        break;
                                    case "flt":
                                        AssignEnum(typeof(CompressionPreFilter), tuple[1], FLTNotFound, FLTFound);
                                        void FLTFound(object enumVal) {
                                            fileinfo.importParameters.CompressionPreFilter = (CompressionPreFilter)enumVal;
                                        }
                                        void FLTNotFound() {
                                            Output.WriteLine("Error: Value for flt is invalid!");
                                            ErrorState = true;
                                        }
                                        break;
                                    case "compr":
                                        AssignEnum(typeof(Compression), tuple[1], ComprNotFound, ComprFound);
                                        void ComprFound(object enumVal) {
                                            fileinfo.importParameters.Compression = (Compression)enumVal;
                                        }
                                        void ComprNotFound() {
                                            Output.WriteLine("Error: Value for flt is invalid!");
                                            ErrorState = true;
                                        }
                                        break;
                                    case "level":
                                        int comprLevel;
                                        if (!int.TryParse(tuple[1], out comprLevel)) {
                                            Output.WriteLine("Error: Illegal number for level. Please enter an integer number depending on the compression algorithm.");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (fileinfo.importParameters.Compression == Compression.Brotli) {
                                            if (comprLevel < 0 || comprLevel > 11) {
                                                Output.WriteLine("Error: Illegal number for Brotli level. Please enter an integer number between 0 and 11");
                                                ErrorState = true;
                                                return;
                                            }
                                        }
                                        else if (fileinfo.importParameters.Compression == Compression.ZSTD) {
                                            if (comprLevel < 0 || comprLevel > 22) {
                                                Output.WriteLine("Error: Illegal number for ZSTD level. Please enter an integer number between 0 and 22");
                                                ErrorState = true;
                                                return;
                                            }
                                        }
                                        fileinfo.importParameters.CompressionLevel = comprLevel;
                                        break;
                                    case "bwnd":
                                        int bwnd;
                                        if (!int.TryParse(tuple[1], out bwnd)) {
                                            Output.WriteLine("Error: Illegal number for bwnd. Please enter an integer number between 0 and 24");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (bwnd < 0 || bwnd > 24) {
                                            Output.WriteLine("Error: Illegal number for bwnd. Please enter an integer number between 0 and 24");
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.BrotliWindowSize = bwnd;
                                        break;
                                    case "ensop":
                                        float ensop;
                                        if (!float.TryParse(tuple[1], out ensop)) {
                                            Output.WriteLine("Error: Illegal number for bwnd. Please enter an integer number between 0 and 100 (float)");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (ensop < 0 || ensop > 100) {
                                            Output.WriteLine("Error: Illegal number for bwnd. Please enter an integer number between 0 and 100 (float)");
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.MaxRepackMemoryPercentage = ensop;
                                        fileinfo.importParameters.Flags |= Flags.STREAMING_OPTIMIZED;
                                        break;
                                }
                            }

                            fileinfo.importParameters.SourceDimensions = (width, height);
                            fileinfo.outputDirectory = "";

                            InputFiles.Add(fileinfo);
                        }
                    }
                    else {
                        Output.WriteLine("Error: No inputs given!");
                        ErrorState = true;
                    }
                }
            } else {
                //Decode
                Help.WriteLine("Options for decoding--------------------------------------\n");
                Help.WriteLine("BNGCLI -i \"myfile.bng;my other file.bng\" n:\\my\\output\\path");
                Help.WriteLine("-i  File names in \"aaa;bbb\" notation");

                FindArg("-i", FoundDI, NotFoundDI);
                void FoundDI(long index) {
                    if (index + 1 <= _args.Length - 1) {
                        string[] inputs = Split(_args[index + 1], ';');
                        foreach (string input in inputs) {
                            FileSource fileinfo = new();
                            fileinfo.pathName = input;
                            fileinfo.outputDirectory = _args[_args.Length - 1];
                            InputFiles.Add(fileinfo);
                        }
                    } else {
                        Output.WriteLine("Error: No inputs given OR output path missing!");
                        ErrorState = true;
                    }
                }
                void NotFoundDI() {
                    Output.WriteLine("Error: No inputs given!");
                    ErrorState = true;
                }
            }
            void NotFoundMI() {
                Output.WriteLine("Error: No inputs given!");
                ErrorState = true;
            }

            if (ErrorState) {
                Output.Write(Help.ToString());
                return;
            }

            OutputFile = _args[_args.LongLength - 1];
            Task = _Task;
        }

        #region Helpers
        private void FindArg(string arg, dgFoundArgCallback foundArgCallback, dgArgNotFoundCallback? notFoundCallback = null) {
            for (long i = 0; i < _args.LongLength; i++) {
                if (_args[i].Equals(arg, StringComparison.OrdinalIgnoreCase)) {
                    foundArgCallback.Invoke(i);
                    return;
                }
            }
            if (notFoundCallback != null) notFoundCallback.Invoke();
        }

        private void AssignEnum(Type T, string enumValue, dgEnumNotFound notFoundCallback, dgEnumFound foundCallback) {
            var elements = Enum.GetValues(T);
            for (long element = 0; element < elements.LongLength; element++) {
                if (elements.GetValue(element).ToString().ToLower().Equals(enumValue.ToLower())) {
                    foundCallback.Invoke(elements.GetValue(element));
                    return;
                }
            }
            notFoundCallback.Invoke();
        }

        private string[] Split(string line, char by) {
            List<string> result = new List<string>();
            StringBuilder currentStr = new StringBuilder("");
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++) // For each character
            {
                if (line[i] == '\'') // Quotes are closing or opening
                    inQuotes = !inQuotes;
                else if (line[i] == by) // Comma
                {
                    if (!inQuotes) // If not in quotes, end of current string, add it to result
                    {
                        result.Add(currentStr.ToString());
                        currentStr.Clear();
                    }
                    else
                        currentStr.Append(line[i]); // If in quotes, just add it 
                }
                else // Add any other character to current string
                    currentStr.Append(line[i]);
            }
            result.Add(currentStr.ToString());
            return result.ToArray(); // Return array of all strings
        }
        #endregion

        public string GetOutput() {
            return Output.ToString();
        }
    }
    class Program {
        static int Main(string[] args) {
            Stopwatch sw = new();
            sw.Start();

            TextWriter Help = new StringWriter();

            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-us");
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-us");
            Console.OutputEncoding = Encoding.UTF8;

            ParseArguments p = new(args);

            if (p.ErrorState) {
                Console.Write(p.GetOutput());
                return 1;
            }

            switch (p.Task) {
                case Task.Encode:
                    Stream outFile = new FileStream(p.OutputFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 0x800000);
                    outFile.SetLength(0);

                    Bitmap BNG = new Bitmap();

                    TimeSpan lastE = new(DateTime.Now.Ticks);
                    void pChanged(Bitmap.progressBean progress) {
                        TimeSpan nowE = new(DateTime.Now.Ticks);
                        if ((nowE - lastE).TotalMilliseconds > 100 || progress.progress == 0.0 || progress.progress == 100.0)
                        {
                            lock (Console.Out)
                            {
                                Console.Write('\r');
                                Console.Write(new string(' ', Console.WindowWidth));
                                Console.Write('\r');
                                if (progress.isMultithreaded)
                                {
                                    Console.Write(string.Format("Layer {1}/{2}: (Processing {3} tiles simultaenously, {4}/{5} in pool), {0:0.00} percent done", progress.progress, progress.currentLayer + 1, progress.numLayers, progress.tilesProcessing, progress.tilesInPool, progress.numTiles));
                                }
                                else
                                {
                                    Console.Write(string.Format("Layer {1}/{2}: {0:0.00} percent done", progress.progress, progress.currentLayer + 1, progress.numLayers));
                                }
                            }
                            lastE = new(DateTime.Now.Ticks);
                        }
                    }

                    BNG.ProgressChangedEvent += pChanged;

                    long frame = 0;
                    Stream nullStream = null;

                    foreach (FileSource f in p.InputFiles) {
                        Stopwatch fsw = new();
                        fsw.Start();
                        
                        if (f.importParameters.OpenFrame && !f.importParameters.LayerClosesFrame && !f.importParameters.LayerToCurrentFrame) {
                            BNG = new Bitmap();
                            BNG.ProgressChangedEvent += pChanged;
                            frame++;
                            Console.WriteLine(string.Format("Creating new frame {0}...", frame));
                            Console.WriteLine("Adding layer " + Path.GetFileName(f.pathName));
                            BNG.AddLayer(f.pathName, f.importParameters, ref nullStream);
                        }
                        if (f.importParameters.LayerToCurrentFrame && !f.importParameters.OpenFrame) {
                            Console.WriteLine("Adding layer " + Path.GetFileName(f.pathName));
                            BNG.AddLayer(f.pathName, f.importParameters, ref nullStream);
                        }
                        if (!f.importParameters.OpenFrame && f.importParameters.LayerClosesFrame && f.importParameters.LayerToCurrentFrame) {
                            Console.WriteLine(string.Format("Writing Frame {0}...", frame));
                            BNG.WriteBNGFrame(ref outFile);
                            BNG.Dispose();
                            Console.Write('\r');
                            Console.Write(new string(' ', Console.WindowWidth));
                            Console.Write('\r');
                            Console.Write(string.Format("Done, processing took {0}", fsw.Elapsed));
                        }
                        if (!f.importParameters.OpenFrame && !f.importParameters.LayerClosesFrame && !f.importParameters.LayerToCurrentFrame) {
                            BNG = new Bitmap();
                            BNG.ProgressChangedEvent += pChanged;
                            frame++;
                            Console.WriteLine("\n" + string.Format("Creating Frame {0}/{1}...", frame, p.InputFiles.Count));
                            Console.WriteLine("Adding layer " + Path.GetFileName(f.pathName));
                            BNG.AddLayer(f.pathName, f.importParameters, ref nullStream);
                            Console.WriteLine(string.Format("Writing Frame {0}/{1}...", frame, p.InputFiles.Count));
                            BNG.WriteBNGFrame(ref outFile);
                            BNG.Dispose();
                            Console.Write('\r');
                            Console.Write(new string(' ', Console.WindowWidth));
                            Console.Write('\r');
                            Console.Write(string.Format("Done, processing took {0}", fsw.Elapsed));
                        }
                        fsw.Stop();
                    }
                    Console.WriteLine();
                    outFile.Close();
                    outFile.Dispose();
                    break;
                case Task.Decode:

                    long fi = 0;
                    foreach (var file in p.InputFiles) {
                        fi++;
                        Console.WriteLine(string.Format("Processing input file {0}/{1}: {2}", fi, p.InputFiles.Count, Path.GetFileName(file.pathName)));

                        Bitmap BNGToDecode = new Bitmap();
                        StringBuilder info = new();
                        BNGToDecode.Strict = true;
                        BNGToDecode.VerboseLevel = p.VerboseLevel;
                        Stream inFile = new FileStream(file.pathName, FileMode.Open, FileAccess.Read, FileShare.Read, 0x800000);

                        TimeSpan last = new(DateTime.Now.Ticks);

                        Stopwatch dsw = new();
                        dsw.Start();

                        long cf = 0;
                        while(inFile.Position < inFile.Length) {
                            cf++;
                            FrameHeader bng;
                            if (!BNGToDecode.LoadBNG(ref inFile, out info, out bng)) {
                                Console.WriteLine(fi > 1 ? "No further BNG frame was found. Finalizing..." : "Error: No BNG frame was found. Aborting...");
                                break;
                            }

                            void pChangedDec(Bitmap.progressBean progress)
                            {
                                TimeSpan now = new(DateTime.Now.Ticks);
                                if ((now - last).TotalMilliseconds > 100 || progress.progress == 0.0 || progress.progress == 100.0)
                                {
                                    lock(Console.Out)
                                    {
                                        Console.Write('\r');
                                        Console.Write(new string(' ', Console.WindowWidth));
                                        Console.Write('\r');
                                        Console.Write(string.Format("Layer {0}/{1} {2:0.00}%", progress.currentLayer + 1, progress.numLayers, progress.progress));
                                    }
                                    last = new(DateTime.Now.Ticks);
                                }
                            }

                            BNGToDecode.ProgressChangedEvent += pChangedDec;

                            Console.Write('\r');
                            Console.Write(new string(' ', Console.WindowWidth));
                            Console.Write('\r');
                            string frTitle = string.Format("\nFound BNG frame {0}", cf);
                            Console.Write(frTitle + new string('=', 40 - frTitle.Length));
                            Console.Write(info.ToString());
                            for (var layer  = 0; layer < bng.Layers.Count; layer++) {
                                Console.WriteLine("\nExtracting layer {0}/{1}...", layer+1, bng.Layers.Count);
                                //Determine output file name
                                string outNamePortion = string.Empty;
                                if (outNamePortion == string.Empty) outNamePortion = bng.Layers[layer].Name;
                                if (outNamePortion == string.Empty) outNamePortion = "_" + cf.ToString();
                                string outFileName = Path.GetFileNameWithoutExtension(file.pathName) + "_bng_export_" + outNamePortion + ".data";

                                Stream outFileDec = new FileStream(Path.TrimEndingDirectorySeparator(file.outputDirectory) + Path.DirectorySeparatorChar + outFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 0x100000);
                                BNGToDecode.DecodeLayerToRaw( inFile,  outFileDec, layer);

                                Console.Write('\r');
                                Console.Write(new string(' ', Console.WindowWidth));
                                Console.Write('\r');
                                Console.WriteLine(string.Format("Done, processing took {0}", dsw.Elapsed));

                                outFileDec.Close();
                                outFileDec.Dispose();
                            }
                        }

                        dsw.Stop();
                        inFile.Dispose();
                    }
                    break;
            }

            sw.Stop();
            Console.Write(string.Format("Overall processing took {0}\n", sw.Elapsed));

            return 0;
        }
    }
}
