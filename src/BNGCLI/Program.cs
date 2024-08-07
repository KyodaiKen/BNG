using BNGCORE;
using MemoryPack.Formatters;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace BNG_CLI {
    public enum Task {
        Encode,
        Decode,
        Analyze
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

        private bool IsPowerOfTwo(ulong x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }

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
            Help.WriteLine("BNGCLI [-e -i <\"filename=<file>;key=value,filename=<file>;key=value;key=value\" notation> <output file name>] [-a] [-i <bng file> <output path>] \n");

            Help.Write("-e  Tell the program to go into encode mode. If not set, BNG_CLI will assume a BNG file to be decoded.\n");
            Help.Write("-a  Tell the program to analyze a BNG file and output info about it. Can be combined with -v 2 for full output.\n");
            Help.Write("-i  <file name> is for decoding bng files.\n\n");

            //Scan for encoding task
            FindArg("-e", FoundE);
            Task = Task.Decode;
            void FoundE(long index) {
                _Task = Task.Encode;
            }

            //Scan for analysis task
            FindArg("-a", FoundA);
            Task = Task.Decode;
            void FoundA(long index)
            {
                _Task = Task.Analyze;
            }

            //Verbose level
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
                Help.WriteLine("BNGCLI -e -i \"fn=myfile.raw,w=1024,h=768;fn=my other file.raw,w=1280,h=720\" my.bng\n");
                Help.WriteLine("-i  List of input files and parameters. \"fn=<file>,key=value;fn='<fi;le>',key=value.`val,ue`,key=value\" notation. SINGLE-Quote things that contain ; or ,. (Required)\n");
                Help.WriteLine("\n  Input file\n");
                Help.WriteLine("    fn=    (Required)                 Input file name               Relative or absolute file path");
                //Help.WriteLine("    fsqb=  (Default=None)             File sequence begin number    Integer from 0 to " + long.MaxValue.ToString());
                //Help.WriteLine("    fsqe=  (Default=None)             File sequence end number      Integer from 0 to " + long.MaxValue.ToString());
                Help.WriteLine("    maxt=  (Default=-1)               Maximum number of threads     Integer from -1 (auto) to " + int.MaxValue.ToString());

                Help.WriteLine("\n  RAW import\n");
                Help.WriteLine("    w=     (Required)                 RAW image width               Integer from 0 to " + uint.MaxValue.ToString());
                Help.WriteLine("    h=     (Required)                 RAW image height              Integer from 0 to " + uint.MaxValue.ToString());
                Help.WriteLine("    cs=    (Default=RGB)              RAW image Color space         { RGB, RGBA, YCrCb, YCrCbA, CMYK, CMYKA }");
                Help.WriteLine("    am=    (Default=Straight)         Alpha mode (png and tif only) { Straight, PreMult }");
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
                Help.WriteLine("    lts=   (Default=Auto)             Layer tile size override      Auto or number between 32 to " + uint.MaxValue.ToString());
                Help.WriteLine("    ltc=   (Default=0)                Enter 1 if you want to add this image as a layer to the current OPEN frame");
                Help.WriteLine("    lcf=   (Default=0)                Enter 1 if you want this layer to close the current OPEN frame");
                Help.WriteLine("    preset=(Default=7)                Compression effort preset.    (fastest) 1 ... 10 (slowest)");
                Help.WriteLine("    flt=   (Default=from preset)      Dot (.) separated list of compression pre-filters to try\n" +
                               "                                      Possible values:              { None, Sub, Up, Average, Median, Median2, Paeth, JXL_Pred }");
                Help.WriteLine("    compr= (Default=from preset)      Dot (.) separated list of compression algorithms to try\n" +
                               "                                      Possible values:              { None, Brotli, LZW, ZSTD, LZMA, XZ }");
                Help.WriteLine("    clvlb= (Default=from preset)      Brotli ompression level       1 ... 11");
                Help.WriteLine("    bwnd=  (Default=24)               Brotli window size            10 ... 24");
                Help.WriteLine("    clvlz= (Default=from preset)      ZSTD Compression level        1 ... 22");

                Help.WriteLine("\n  File layout\n");
                Help.WriteLine("    ensop= (Default=80)               Enable streaming optimizer    Value (float) defines the percentage of FREE memory to be used.\n"+
                               "                                                                    If more is needed than set here, a temporary file in the\n"+
                               "                                                                    destination path is used instead.");
                Help.WriteLine("    uch=   (Default=0)                Use uncompressed headers      1 = enabled, 0 = disabled");

                int bwnd = 24; // Brotli window default
                int uch = 0; //Uncompressed headers default
                float ensop = 80f; //Set enable stream optimization default to enabled with 80% free memory util.

                FindArg("-i", FoundMI, NotFoundMI);
                void FoundMI(long index) {
                    if (index + 1 <= _args.Length) {
                        string[] inputs = Split(_args[index + 1], ';');
                        foreach (string input in inputs) {

                            FileSource fileinfo = new();
                            fileinfo.pathName = "";
                            fileinfo.importParameters = new();
                            fileinfo.importParameters.Flags = 0; //Flags.COMPRESSED_HEADER;
                            fileinfo.importParameters.OverrideTileSize = 0;
                            string[] options = Split(input, ',', '`');
                            foreach (string option in options) {
                                string[] tuple = new string[2];
                                tuple[0] = option.Substring(0, option.IndexOf('='));
                                tuple[1] = option.Substring(option.IndexOf('=') + 1);
                                switch (tuple[0]) {
                                    case "fn":
                                    case "filename":
                                        fileinfo.pathName = tuple[1];
                                        break;
                                    case "maxt":
                                        int maxt = -1;
                                        if (!int.TryParse(tuple[1], out maxt))
                                        {
                                            Output.WriteLine("Error: Illegal number for maxt. Please enter an integer number between -1 and " + int.MaxValue.ToString() + " except 0.");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (maxt < -1 || maxt == 0)
                                        {
                                            Output.WriteLine("Error: Illegal number for maxt. Please enter an integer number between -1 and " + int.MaxValue.ToString() + " except 0.");
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.MaxThreadCount = maxt;
                                        break;
                                    case "w":
                                        if (!uint.TryParse(tuple[1], out width)) {
                                            Output.WriteLine("Error: Illegal number for w. Please enter an integer number between 0 and " + uint.MaxValue.ToString() + ".");
                                            ErrorState = true;
                                            return;
                                        }
                                        break;
                                    case "h":
                                        if (!uint.TryParse(tuple[1], out height)) {
                                            Output.WriteLine("Error: Illegal number for h. Please enter an integer number between 0 and " + uint.MaxValue.ToString() + ".");
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
                                    case "am":
                                        AssignEnum(typeof(AlphaMode), tuple[1], AMNotFound, AMFound);
                                        void AMFound(object enumVal)
                                        {
                                            fileinfo.importParameters.AlphaMode = (AlphaMode)enumVal;
                                        }
                                        void AMNotFound()
                                        {
                                            Output.WriteLine("Error: Value for alph is invalid!");
                                            ErrorState = true;
                                        }
                                        break;
                                    case "pf":
                                        AssignEnum(typeof(PixelFormat), tuple[1], PFNotFound, PFFound);
                                        void PFFound(object enumVal) {
                                            fileinfo.importParameters.SourcePixelFormat = (PixelFormat)enumVal;
                                        }
                                        void PFNotFound() {
                                            Output.WriteLine("Error: Value for pf is invalid!");
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
                                        if (!IsPowerOfTwo(value)) {
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
                                            Output.WriteLine("Error: Illegal number for cw. Please enter an integer number between 0 and " + uint.MaxValue.ToString() + ".");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (cw < 0) {
                                            Output.WriteLine("Error: Illegal number for cw. Please enter an integer number between 0 and " + uint.MaxValue.ToString() + ".");
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.FrameWidth = cw;
                                        break;
                                    case "ch":
                                        uint ch = 0;
                                        if (!uint.TryParse(tuple[1], out cw)) {
                                            Output.WriteLine("Error: Illegal number for ch. Please enter an integer number between 0 and " + uint.MaxValue.ToString() + ".");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (cw < 0) {
                                            Output.WriteLine("Error: Illegal number for ch. Please enter an integer number between 0 and " + uint.MaxValue.ToString() + ".");
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.FrameHeight = ch;
                                        break;
                                    case "frdur":
                                        double fDur = 0;
                                        if (!double.TryParse(tuple[1], out fDur)) {
                                            Output.WriteLine("Error: Illegal number for frdur. Please enter an integer number between 0 and " + double.MaxValue.ToString() + ".");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (fDur < 0) {
                                            Output.WriteLine("Error: Illegal number for frdur. Please enter an integer number between 0 and " + double.MaxValue.ToString() + ".");
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.FrameDuration = fDur;
                                        break;
                                    case "fropn":
                                        int fropn = 0;
                                        if (!int.TryParse(tuple[1], out fropn)) {
                                            Output.WriteLine("Error: Illegal number for fropn. Please enter 1");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (fropn < 0) {
                                            Output.WriteLine("Error: Illegal number for fropn. Please enter 1");
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
                                            Output.WriteLine("Error: Value for fccs is invalid!");
                                            ErrorState = true;
                                        }
                                        break;
                                    case "fcpf":
                                        AssignEnum(typeof(PixelFormat), tuple[1], FCPFNotFound, FCPFFound);
                                        void FCPFFound(object enumVal) {
                                            fileinfo.importParameters.CompositingPixelFormat = (PixelFormat)enumVal;
                                        }
                                        void FCPFNotFound() {
                                            Output.WriteLine("Error: Value for fcpf is invalid!");
                                            ErrorState = true;
                                        }
                                        break;
                                    case "fcbpc":
                                        uint fcbpc = 0;
                                        if (!uint.TryParse(tuple[1], out fcbpc)) {
                                            Output.WriteLine("Error: Illegal number for fcbpc. Please enter one of 8, 16, 32, 64.");
                                            ErrorState = true;
                                            break;
                                        }
                                        if (fcbpc != 0 || fcbpc != 8 || fcbpc != 16 || fcbpc != 32 | fcbpc != 64) {
                                            Output.WriteLine("Error: Illegal number for fcbpc. Please enter one of 8, 16, 32, 64.");
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
                                            Output.WriteLine("Error: Illegal number for ltc Please enter an integer number between 0 and " + uint.MaxValue.ToString() + ".");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (ltc < 0) {
                                            Output.WriteLine("Error: Illegal number for ltc Please enter an integer number between 0 and " + uint.MaxValue.ToString() + ".");
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
                                            Output.WriteLine("Error: Illegal number for lox. Please enter an integer number between 0 and " + uint.MaxValue.ToString() + ".");
                                            ErrorState = true;
                                            return;
                                        }
                                        break;
                                    case "loy":
                                        if (!uint.TryParse(tuple[1], out loy)) {
                                            Output.WriteLine("Error: Illegal number for loy. Please enter an integer number between 0 and " + uint.MaxValue.ToString() + ".");
                                            ErrorState = true;
                                            return;
                                        }
                                        break;
                                    case "lop":
                                        double lop;
                                        if (!double.TryParse(tuple[1], out lop))
                                        {
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
                                    case "lts":
                                        uint lts = 0;
                                        if (!uint.TryParse(tuple[1], out lts))
                                        {
                                            Output.WriteLine("Error: Illegal number for lts. Please enter a number between 32 and " + uint.MaxValue.ToString() + ".");
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.OverrideTileSize = lts;
                                        break;
                                    case "lcf":
                                        int lcf = 0;
                                        if (!int.TryParse(tuple[1], out lcf)) {
                                            Output.WriteLine("Error: Illegal number for lcf Please enter an integer number between 0 and " + uint.MaxValue.ToString() + ".");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (lcf < 0) {
                                            Output.WriteLine("Error: Illegal number for lcf Please enter an integer number between 0 and " + uint.MaxValue.ToString() + ".");
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.LayerClosesFrame = lcf == 1;
                                        break;
                                    case "preset":
                                        byte cpreset = 0;
                                        void prntPresetValError()
                                        {
                                            Output.WriteLine("Error: Illegal number for preset Please enter an integer number between 0 and 10.");
                                        }
                                        if (!byte.TryParse(tuple[1], out cpreset))
                                        {
                                            prntPresetValError();
                                            ErrorState = true;
                                            return;
                                        }
                                        if (cpreset > 10)
                                        {
                                            prntPresetValError();
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.CompressionPreset = cpreset;
                                        break;
                                    case "flt":
                                        var listFltrs = tuple[1].Split('.');
                                        List<CompressionPreFilter> filters = new List<CompressionPreFilter>();
                                        foreach (var fltr in listFltrs)
                                        {
                                            
                                            AssignEnum(typeof(CompressionPreFilter), fltr, FLTNotFound, FLTFound);
                                            void FLTFound(object enumVal)
                                            {
                                                filters.Add((CompressionPreFilter)enumVal);
                                            }
                                            void FLTNotFound()
                                            {
                                                Output.WriteLine("Error: Value for flt is invalid!");
                                                ErrorState = true;
                                            }
                                        }
                                        
                                        if(filters.Count == 0)
                                        {
                                            Output.WriteLine("Error: Value for flt is invalid!");
                                            ErrorState = true;
                                        }
                                        else
                                        {
                                            fileinfo.importParameters.CompressionPreFilters = filters;
                                        }
                                        break;
                                    case "compr":
                                        var listCompr = tuple[1].Split('.');
                                        List<Compression> comprs = new List<Compression>();
                                        foreach (string compr in listCompr)
                                        {

                                            AssignEnum(typeof(Compression), compr, COMPRNotFound, COMPRFound);
                                            void COMPRFound(object enumVal)
                                            {
                                                comprs.Add((Compression)enumVal);
                                            }
                                            void COMPRNotFound()
                                            {
                                                Output.WriteLine("Error: Value for compr is invalid!");
                                                ErrorState = true;
                                            }
                                        }

                                        if (comprs.Count == 0)
                                        {
                                            Output.WriteLine("Error: Value for compr is invalid!");
                                            ErrorState = true;
                                        }
                                        else
                                        {
                                            fileinfo.importParameters.CompressionPreset = 0;
                                            fileinfo.importParameters.Compressions = comprs;
                                        }
                                        break;
                                    case "clvlb":
                                        int comprLevelBrotli;
                                        if (!int.TryParse(tuple[1], out comprLevelBrotli)) {
                                            Output.WriteLine("Error: Illegal number for Brotli level. Please enter an integer number depending on the compression algorithm.");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (comprLevelBrotli < 0 || comprLevelBrotli > 11) {
                                            Output.WriteLine("Error: Illegal number for Brotli level. Please enter an integer number between 0 and 11");
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.CompressionLevel ??= new();
                                        fileinfo.importParameters.CompressionLevel.Brotli = comprLevelBrotli;
                                        fileinfo.importParameters.CompressionPreset = 0;
                                        break;
                                    case "bwnd":
                                        if (!int.TryParse(tuple[1], out bwnd)) {
                                            Output.WriteLine("Error: Illegal number for bwnd. Please enter an integer number between 10 and 24");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (bwnd < 10 || bwnd > 24) {
                                            Output.WriteLine("Error: Illegal number for bwnd. Please enter an integer number between 10 and 24");
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.CompressionPreset = 0;
                                        break;
                                    case "clvlz":
                                        int comprLevelZSTD;
                                        if (!int.TryParse(tuple[1], out comprLevelZSTD))
                                        {
                                            Output.WriteLine("Error: Illegal number for ZSTD level. Please enter an integer number depending on the compression algorithm.");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (comprLevelZSTD < 0 || comprLevelZSTD > 22)
                                        {
                                            Output.WriteLine("Error: Illegal number for ZSTD level. Please enter an integer number between 0 and 22");
                                            ErrorState = true;
                                            return;
                                        }
                                        fileinfo.importParameters.CompressionLevel ??= new();
                                        fileinfo.importParameters.CompressionLevel.ZSTD = comprLevelZSTD;
                                        fileinfo.importParameters.CompressionPreset = 0;
                                        break;
                                    case "ensop":
                                        if (!float.TryParse(tuple[1], out ensop)) {
                                            Output.WriteLine("Error: Illegal number for ensop. Please enter an integer number between 0 and 100 (float)");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (ensop < 0 || ensop > 100) {
                                            Output.WriteLine("Error: Illegal number for ensop. Please enter an integer number between 0 and 100 (float)");
                                            ErrorState = true;
                                            return;
                                        }
                                        break;
                                    case "uch":
                                        if (!int.TryParse(tuple[1], out uch))
                                        {
                                            Output.WriteLine("Error: Illegal number for uch. Parameter 1 is accepted. For keeping header compression enabled, leave this parameter out.");
                                            ErrorState = true;
                                            return;
                                        }
                                        if (uch < 0 || uch > 1)
                                        {
                                            Output.WriteLine("Error: Illegal number for uch. Parameter 1 is accepted. For keeping header compression enabled, leave this parameter out.");
                                            ErrorState = true;
                                            return;
                                        }
                                        break;
                                }
                            }

                            //Set header compression flag
                            if (uch == 1)
                            {
                                fileinfo.importParameters.Flags &= ~Flags.COMPRESSED_HEADER;
                            }
                            else
                            {
                                fileinfo.importParameters.Flags |= Flags.COMPRESSED_HEADER;
                            }

                            //Set stream optimization flag according to ensop value
                            if (ensop > 0)
                            {
                                fileinfo.importParameters.MaxRepackMemoryPercentage = ensop;
                                fileinfo.importParameters.Flags |= Flags.STREAMING_OPTIMIZED;
                            }
                            else
                            {
                                fileinfo.importParameters.MaxRepackMemoryPercentage = 0;
                                fileinfo.importParameters.Flags &= ~Flags.STREAMING_OPTIMIZED;
                            }

                            //Set Brotli window
                            fileinfo.importParameters.BrotliWindowSize = bwnd;

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
            }
            else if (_Task == Task.Analyze)
            {
                //Decode
                Help.WriteLine("Options for analysis--------------------------------------\n");
                Help.WriteLine("BNGCLI -v 2 -a -i \"myfile.bng;my other file.bng\"");
                Help.WriteLine("-i           File names in \"aaa;bbb\" notation");
                Help.WriteLine("-v  <number> Verbosity level 0 ... 2. 0 doesn't output anything, 1 only outputs the frame and layer info,\n"+
                              "             2 outputs frame, layer and individual tile info.");

                FindArg("-i", FoundAI, NotFoundAI);
                void FoundAI(long index)
                {
                    string[] inputs = Split(_args[index + 1], ';');
                    foreach (string input in inputs)
                    {
                        FileSource fileinfo = new();
                        fileinfo.pathName = input;
                        InputFiles.Add(fileinfo);
                    }
                }
                void NotFoundAI()
                {
                    Output.WriteLine("Error: No inputs given!");
                    ErrorState = true;
                }
            }
            else {
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

        private string[] Split(string line, char by, char q = '\'') {
            List<string> result = new List<string>();
            StringBuilder currentStr = new StringBuilder("");
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++) // For each character
            {
                if (line[i] == q) // Quotes are closing or opening
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
        private static string humanReadibleSize(long fSize)
        {
            string[] sizes = { "bytes", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "YiB" };
            decimal len = fSize;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return String.Format("{0:0.00} {1}", len, sizes[order]);
        }
        static int Main(string[] args) {
            Stopwatch sw = new();
            sw.Start();

            Console.WriteLine("\x1b[94;1m{0}\x1b[93;1m{1}\x1b[0m\nUsing:"
                , Assembly.GetExecutingAssembly().GetName().Name.PadRight(25, ' '), Assembly.GetExecutingAssembly().GetName().Version.ToString());

            foreach (var assyname in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
                if (!assyname.Name.Contains("System"))
                    Console.WriteLine("\x1b[92;1m{0}\x1b[93;1m{1}\x1b[0m", assyname.Name.PadRight(25,' '), assyname.Version.ToString());

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

                                //Console.Write(new string(' ', Console.WindowWidth));
                                //Console.Write('\r');
                                var strp = "";
                                int strlen = 0;
                                if (progress.isMultithreaded)
                                {
                                    strp = string.Format("Layer \x1b[93;1m{1}/{2}\x1b[0m: (Processing \x1b[97;1m{3}\x1b[0m tiles simultaenously, \x1b[94;1m{4}/{5}\x1b[0m in pool), \x1b[95;1m{0:0.00}\x1b[0m percent done", progress.progress, progress.currentLayer + 1, progress.numLayers, progress.tilesProcessing, progress.tilesInPool, progress.numTiles);
                                    strlen = 44;
                                }
                                else
                                {
                                    strp = string.Format("Layer \x1b[93;1m{1}/{2}\x1b[0m: \x1b[95;1m{0:0.00}\x1b[0m percent done", progress.progress, progress.currentLayer + 1, progress.numLayers);
                                    strlen = 22;
                                }
                                Console.Write('\r');
                                Console.Write(strp + new string(' ', Console.WindowWidth - strp.Length + strlen));
                            }
                            lastE = new(DateTime.Now.Ticks);
                        }
                    }

                    BNG.ProgressChangedEvent += pChanged;

                    long frame = 0;
                    long uncompressedSize = 0;
                    long pixelCount = 0;
                    long inputFilesSize = 0;
                    double timeTaken = 0;
                    Stream nullStream = null;
                    Stream decodedForeignFormatImageStream = null;


                    foreach (FileSource f in p.InputFiles) {


                        inputFilesSize += new System.IO.FileInfo(f.pathName).Length;

                        void internalAddLayer()
                        {
                            Console.WriteLine("\u001b[94;1mAdding layer \u001b[96m" + Path.GetFileName(f.pathName) + "\u001b[0m");
                            if (Path.GetExtension(f.pathName).ToLower() == ".png" || Path.GetExtension(f.pathName).ToLower() == ".tiff" || Path.GetExtension(f.pathName).ToLower() == ".tif")
                            {
                                decodedForeignFormatImageStream = getForeignFormatPixelData(f.pathName, f.importParameters);
                                Console.WriteLine("\u001b[93m{0}x{1}, {2}, {3} bits per channel, {4}\u001b[0m", f.importParameters.FrameWidth, f.importParameters.FrameHeight, f.importParameters.SourceColorSpace, f.importParameters.SourceBitsPerChannel, f.importParameters.SourcePixelFormat);
                                BNG.AddLayer("", f.importParameters, ref decodedForeignFormatImageStream);
                            }
                            else
                            {
                                BNG.AddLayer(f.pathName, f.importParameters, ref nullStream);
                            }
                        }

                        Stopwatch fsw = new();

                        if (f.importParameters.FrameName == "") f.importParameters.FrameName = f.pathName;
                        if (f.importParameters.LayerName == "") f.importParameters.LayerName = f.pathName;
                        if (f.importParameters.OpenFrame && !f.importParameters.LayerClosesFrame && !f.importParameters.LayerToCurrentFrame) {
                            BNG = new Bitmap();
                            BNG.ProgressChangedEvent += pChanged;
                            frame++;
                            Console.WriteLine(string.Format("\x1b[95;1mCreating new frame {0}...\u001b[0m", frame));
                            internalAddLayer();
                        }
                        if (f.importParameters.LayerToCurrentFrame && !f.importParameters.OpenFrame) {
                            internalAddLayer();
                        }
                        if (!f.importParameters.OpenFrame && f.importParameters.LayerClosesFrame && f.importParameters.LayerToCurrentFrame) {
                            Console.WriteLine(string.Format("\x1b[95;1mWriting Frame {0}...\u001b[0m", frame));
                            fsw.Start();
                            BNG.WriteBNGFrame(ref outFile);
                            fsw.Stop();
                            BNG.Dispose();
                            if (decodedForeignFormatImageStream != null)
                                uncompressedSize += decodedForeignFormatImageStream.Length;
                            else
                                uncompressedSize += (f.importParameters.SourceDimensions.w * f.importParameters.SourceDimensions.h * (BNG.CalculateBitsPerPixel(f.importParameters.SourceColorSpace, f.importParameters.SourceBitsPerChannel) / 8));
                            pixelCount = f.importParameters.FrameWidth * f.importParameters.FrameHeight;
                            timeTaken += fsw.Elapsed.TotalSeconds;
                            Console.Write('\r');
                            Console.Write(new string(' ', Console.WindowWidth));
                            Console.Write('\r');
                            Console.Write(string.Format("Done, processing took {0}", fsw.Elapsed));
                        }
                        if (!f.importParameters.OpenFrame && !f.importParameters.LayerClosesFrame && !f.importParameters.LayerToCurrentFrame) {
                            BNG = new Bitmap();
                            BNG.ProgressChangedEvent += pChanged;
                            frame++;
                            Console.WriteLine("\n" + string.Format("\u001b[95;1mCreating Frame {0}/{1}...\u001b[0m", frame, p.InputFiles.Count));
                            internalAddLayer();
                            Console.WriteLine(string.Format("\u001b[95;1mWriting Frame {0}/{1}...\u001b[0m", frame, p.InputFiles.Count));
                            fsw.Start();
                            BNG.WriteBNGFrame(ref outFile);
                            fsw.Stop();
                            BNG.Dispose();
                            if (decodedForeignFormatImageStream != null)
                                uncompressedSize += decodedForeignFormatImageStream.Length;
                            else
                                uncompressedSize += (f.importParameters.SourceDimensions.w * f.importParameters.SourceDimensions.h * (BNG.CalculateBitsPerPixel(f.importParameters.SourceColorSpace, f.importParameters.SourceBitsPerChannel) / 8));
                            pixelCount += f.importParameters.FrameWidth * f.importParameters.FrameHeight;
                            timeTaken += fsw.Elapsed.TotalSeconds;
                            Console.Write('\r');
                            Console.Write(new string(' ', Console.WindowWidth));
                            Console.Write('\r');
                            Console.Write("\u001b[92mDone, processing took {0}\u001b[0m, processing speed: {1:0.000000} MP/s", fsw.Elapsed, f.importParameters.FrameWidth * f.importParameters.FrameHeight / fsw.Elapsed.TotalSeconds / 1000000);
                        }
                    }
                    Console.WriteLine();
                    Console.WriteLine("\x1b[96mSize of all input files: \x1b[93m{0}", humanReadibleSize(inputFilesSize).Replace(" ", "\x1b[0m "));
                    Console.WriteLine("\x1b[96mUncompressed size .... : \x1b[93m{0}", humanReadibleSize(uncompressedSize).Replace(" ", "\x1b[0m "));
                    Console.WriteLine("\x1b[96mOutput file size ..... : \x1b[93m{0}", humanReadibleSize(outFile.Length).Replace(" ", "\x1b[0m "));
                    Console.WriteLine("\x1b[96mCompression ratio .... : \x1b[93m{0:0.00}\x1b[0m % of input file, \u001b[93m{1:0.00}\u001b[0m % of uncompressed image data", (decimal)outFile.Length / inputFilesSize * 100, (decimal)outFile.Length / uncompressedSize * 100);
                    Console.WriteLine("\x1b[96mProcessing speed ..... : \x1b[93m{0:0.00000}\u001b[0m MP/s", pixelCount / timeTaken / 1000000);
                    outFile.Close();
                    outFile.Dispose();
                    break;
                case Task.Decode:
                case Task.Analyze:
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
                                Console.WriteLine(cf > 1 ? "No further BNG frame was found. Finalizing..." : "Error: No BNG frame was found. Aborting...");
                                break;
                            }

                            void pChangedDec(Bitmap.progressBean progress)
                            {
                                TimeSpan now = new(DateTime.Now.Ticks);
                                if ((now - last).TotalMilliseconds > 100 || progress.progress == 0.0 || progress.progress == 100.0)
                                {
                                    lock (Console.Out)
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

                            if (p.Task != Task.Analyze)
                            {
                                for (var layer = 0; layer < bng.Layers.Count; layer++)
                                {
                                    Console.WriteLine("\nExtracting layer {0}/{1}...", layer + 1, bng.Layers.Count);
                                    //Determine output file name
                                    string outNamePortion = string.Empty;
                                    if (outNamePortion == string.Empty) outNamePortion = bng.Layers[layer].Name;
                                    if (outNamePortion == string.Empty) outNamePortion = "_" + cf.ToString();
                                    string outFileName = Path.GetFileNameWithoutExtension(file.pathName) + "_bng_export_" + outNamePortion + ".data";

                                    Stream outFileDec = new FileStream(Path.TrimEndingDirectorySeparator(file.outputDirectory) + Path.DirectorySeparatorChar + outFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 0x4000000, FileOptions.RandomAccess);
                                    BNGToDecode.DecodeLayerToRaw(inFile, outFileDec, layer);

                                    Console.Write('\r');
                                    Console.Write(new string(' ', Console.WindowWidth));
                                    Console.Write('\r');
                                    Console.WriteLine(string.Format("Done, processing took {0}", dsw.Elapsed));

                                    outFileDec.Close();
                                    outFileDec.Dispose();
                                }
                            }
                            else
                                BNGToDecode.JumpToEndOfFrame(ref inFile);
                        }

                        Console.WriteLine(string.Format("Done, processing took {0}", dsw.Elapsed));
                        dsw.Stop();
                        inFile.Dispose();
                    }
                    break;
            }

            sw.Stop();
            Console.Write(string.Format("\x1b[97;1mOverall processing took {0}\x1b[0m\n", sw.Elapsed));

            return 0;
        }

        static MemoryStream getForeignFormatPixelData(string pathName, RAWImportParameters importParameters)
        {
            MemoryStream pixelData = null;


            var info = Image.Identify(pathName);
            var fmt = info.Metadata.DecodedImageFormat;
            byte[] data = new byte[info.Width * info.Height * (info.PixelType.BitsPerPixel / 8)];
            Span<byte> pixelDataSpan = new Span<byte>(data);

            switch (fmt.DefaultMimeType)
            {
                case "image/png":
                    switch (info.Metadata.GetPngMetadata().ColorType)
                    {
                        case SixLabors.ImageSharp.Formats.Png.PngColorType.Grayscale:
                            importParameters.SourceColorSpace = ColorSpace.GRAY;
                            importParameters.SourcePixelFormat = PixelFormat.IntegerUnsigned;
                            importParameters.SourceBitsPerChannel = (uint)info.PixelType.BitsPerPixel;

                            switch (info.PixelType.BitsPerPixel)
                            {
                                case 8:
                                    using (var img = Image.Load<L8>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                    break;
                                case 16:
                                    using (var img = Image.Load<L16>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                    break;
                            }
                            break;
                        case SixLabors.ImageSharp.Formats.Png.PngColorType.GrayscaleWithAlpha:
                            switch (importParameters.AlphaMode)
                            {
                                case AlphaMode.Straight:
                                    importParameters.SourceColorSpace = ColorSpace.GRAYA_Straight;
                                    break;
                                case AlphaMode.PreMultiplied:
                                    importParameters.SourceColorSpace = ColorSpace.GRAYA_PreMult;
                                    break;
                            }
                            importParameters.SourcePixelFormat = PixelFormat.IntegerUnsigned;
                            importParameters.SourceBitsPerChannel = (uint)info.PixelType.BitsPerPixel / 2;
                            switch (info.PixelType.BitsPerPixel)
                            {
                                case 16:
                                    using (var img = Image.Load<La16>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                    break;
                                case 32:
                                    using (var img = Image.Load<La32>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                    break;
                            }
                            break;
                        case SixLabors.ImageSharp.Formats.Png.PngColorType.Rgb:
                            importParameters.SourceColorSpace = ColorSpace.RGB;
                            importParameters.SourcePixelFormat = PixelFormat.IntegerUnsigned;
                            importParameters.SourceBitsPerChannel = (uint)info.PixelType.BitsPerPixel / 3;
                            switch (info.PixelType.BitsPerPixel)
                            {
                                case 24:
                                    using (var img = Image.Load<Rgb24>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                    break;
                                case 48:
                                    using (var img = Image.Load<Rgb48>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                    break;
                            }
                            break;
                        case SixLabors.ImageSharp.Formats.Png.PngColorType.RgbWithAlpha:
                            switch (importParameters.AlphaMode)
                            {
                                case AlphaMode.Straight:
                                    importParameters.SourceColorSpace = ColorSpace.RGBA_Straight;
                                    break;
                                case AlphaMode.PreMultiplied:
                                    importParameters.SourceColorSpace = ColorSpace.RGBA_PreMult;
                                    break;
                            }
                            importParameters.SourcePixelFormat = PixelFormat.IntegerUnsigned;
                            importParameters.SourceBitsPerChannel = (uint)info.PixelType.BitsPerPixel / 4;
                            switch (info.PixelType.BitsPerPixel)
                            {
                                case 32:
                                    using (var img = Image.Load<Rgba32>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                    break;
                                case 64:
                                    using (var img = Image.Load<Rgba64>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                    break;
                            }
                            break;
                    }
                    break;
                case "image/tiff":
                case "image/tiff-fx":

                    switch (info.FrameMetadataCollection.First().GetTiffMetadata().PhotometricInterpretation)
                    {
                        case SixLabors.ImageSharp.Formats.Tiff.Constants.TiffPhotometricInterpretation.BlackIsZero:
                            if (info.PixelType.AlphaRepresentation == PixelAlphaRepresentation.None)
                            {
                                importParameters.SourceColorSpace = ColorSpace.GRAY;
                                importParameters.SourcePixelFormat = PixelFormat.IntegerUnsigned;
                                importParameters.SourceBitsPerChannel = (uint)info.PixelType.BitsPerPixel;

                                switch (info.PixelType.BitsPerPixel)
                                {
                                    case 8:
                                        using (var img = Image.Load<L8>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                        break;
                                    case 16:
                                        using (var img = Image.Load<L16>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                        break;
                                }
                            }
                            else
                            {
                                switch (importParameters.AlphaMode)
                                {
                                    case AlphaMode.Straight:
                                        importParameters.SourceColorSpace = ColorSpace.GRAYA_Straight;
                                        break;
                                    case AlphaMode.PreMultiplied:
                                        importParameters.SourceColorSpace = ColorSpace.GRAYA_PreMult;
                                        break;
                                }
                                importParameters.SourcePixelFormat = PixelFormat.IntegerUnsigned;
                                importParameters.SourceBitsPerChannel = (uint)info.PixelType.BitsPerPixel / 2;
                                switch (info.PixelType.BitsPerPixel)
                                {
                                    case 16:
                                        using (var img = Image.Load<La16>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                        break;
                                    case 32:
                                        using (var img = Image.Load<La32>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                        break;
                                }
                            }

                            break;
                        case SixLabors.ImageSharp.Formats.Tiff.Constants.TiffPhotometricInterpretation.Rgb:
                            bool hasAlpha = false;
                            foreach (var frame in info.FrameMetadataCollection)
                            {
                                if(frame.GetTiffMetadata().PhotometricInterpretation == SixLabors.ImageSharp.Formats.Tiff.Constants.TiffPhotometricInterpretation.TransparencyMask)
                                {
                                    hasAlpha = true;
                                }
                            }
                            if (!hasAlpha)
                            {
                                importParameters.SourceColorSpace = ColorSpace.RGB;
                                importParameters.SourcePixelFormat = PixelFormat.IntegerUnsigned;
                                importParameters.SourceBitsPerChannel = (uint)info.PixelType.BitsPerPixel / 3;
                                switch (info.PixelType.BitsPerPixel)
                                {
                                    case 24:
                                        using (var img = Image.Load<Rgb24>(pathName))
                                            img.CopyPixelDataTo(pixelDataSpan);
                                        break;
                                    case 48:
                                        using (var img = Image.Load<Rgb48>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                        break;
                                }
                            }
                            else
                            {
                                switch (importParameters.AlphaMode)
                                {
                                    case AlphaMode.Straight:
                                        importParameters.SourceColorSpace = ColorSpace.RGBA_Straight;
                                        break;
                                    case AlphaMode.PreMultiplied:
                                        importParameters.SourceColorSpace = ColorSpace.RGBA_PreMult;
                                        break;
                                }
                                
                                importParameters.SourcePixelFormat = PixelFormat.IntegerUnsigned;
                                importParameters.SourceBitsPerChannel = (uint)info.PixelType.BitsPerPixel / 4;
                                switch (info.PixelType.BitsPerPixel)
                                {
                                    case 32:
                                        using (var img = Image.Load<Rgba32>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                        break;
                                    case 64:
                                        using (var img = Image.Load<Rgba64>(pathName)) img.CopyPixelDataTo(pixelDataSpan);
                                        break;
                                }
                            }
                            break;
                    }
                    break;
            }

            if (pixelDataSpan != null)
            {
                importParameters.FrameWidth = (uint)info.Width;
                importParameters.FrameHeight = (uint)info.Height;
                importParameters.SourceDimensions = ((uint)info.Width, (uint)info.Height);
                pixelData = new MemoryStream(data);
                return pixelData;
            }
            else
                throw new Exception("Could not load foreign image using ImageSharp");
        }
    }
}
