namespace BNGCORE.Filters {
    //https://www.w3.org/TR/PNG-Filters.html
    public static class Sub {
        public static byte Filter(in byte[] line, long col, int BytesPerPixel) {
            return (byte)(line[col] - ((col - BytesPerPixel) < 0 ? 0 : line[col - BytesPerPixel]));
        }
        public static byte UnFilter(in byte[] line, in byte[] dLine, long col, int BytesPerPixel) {
            return (byte)(line[col] + ((col - BytesPerPixel) < 0 ? 0 : dLine[col - BytesPerPixel]));
        }
    }

    public static class Up {
        public static byte Filter(in byte[] cLine, in byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] - pLine[col]);
        }
        public static byte UnFilter(in byte[] cLine, in byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] + pLine[col]);
        }
    }

    public static class Average {
        public static byte Filter(in byte[] cLine, in byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] - Predictor((col - BytesPerPixel) < 0 ? 0 : cLine[col - BytesPerPixel], pLine[col]));
        }
        public static byte UnFilter(in byte[] cLine, in byte[] dLine, in byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] + Predictor((col - BytesPerPixel) < 0 ? 0 : dLine[col - BytesPerPixel], pLine[col]));
        }

        private static int Predictor(int left, int above)
        {
            return (int)Math.Floor((left + above) / 2f);
        }
    }

    public static class Median
    {
        public static byte Filter(in byte[] cLine, in byte[] pLine, long col, int BytesPerPixel)
        {
            return (byte)(cLine[col] - Predictor((col - BytesPerPixel) < 0 ? 0 : cLine[col - BytesPerPixel], pLine[col]));
        }
        public static byte UnFilter(in byte[] cLine, in byte[] dLine, in byte[] pLine, long col, int BytesPerPixel)
        {
            return (byte)(cLine[col] + Predictor((col - BytesPerPixel) < 0 ? 0 : dLine[col - BytesPerPixel], pLine[col]));
        }

        private static int Predictor(int left, int up)
        {
            return Helpers.Median(new List<int> { left, up });
        }
    }

    public static class Median2
    {
        public static byte Filter(in byte[] cLine, in byte[] pLine, long col, int BytesPerPixel)
        {
            return (byte)(cLine[col] - Predictor((col - BytesPerPixel < 0) ? 0 : cLine[col - BytesPerPixel], pLine[col], (col - BytesPerPixel < 0) ? 0 : pLine[col - BytesPerPixel]));
        }
        public static byte UnFilter(in byte[] cLine, in byte[] dLine, in byte[] pLine, long col, int BytesPerPixel)
        {
            return (byte)(cLine[col] + Predictor((col - BytesPerPixel < 0) ? 0 : dLine[col - BytesPerPixel], pLine[col], (col - BytesPerPixel < 0) ? 0 : pLine[col - BytesPerPixel]));
        }
        private static int Predictor(int left, int above, int upperLeft)
        {
            return Helpers.Median(new List<int> { left, above, upperLeft });
        }
    }

    public static class Paeth {
        public static byte Filter(in byte[] cLine, in byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] - Predictor((col - BytesPerPixel < 0) ? 0 : cLine[col - BytesPerPixel], pLine[col], (col - BytesPerPixel < 0) ? 0 : pLine[col - BytesPerPixel]));
        }
        public static byte UnFilter(in byte[] cLine, in byte[] dLine, in byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] + Predictor((col - BytesPerPixel < 0) ? 0 : dLine[col - BytesPerPixel], pLine[col], (col - BytesPerPixel < 0) ? 0 : pLine[col - BytesPerPixel]));
        }
        private static int Predictor(int left, int above, int upperLeft) {
            var p = left + above - upperLeft;
            var pa = Math.Abs(p - left);
            var pb = Math.Abs(p - above);
            var pc = Math.Abs(p - upperLeft);
            if (pa <= pb && pa <= pc) {
                return left;
            }
            else if (pb <= pc) {
                return above;
            }
            else {
                return upperLeft;
            }
        }
    }
}
