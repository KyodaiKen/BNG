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
            return (left + above) >> 1;
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
            return left < up ? left : up;
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
            return Math.Max(Math.Min(left, above), Math.Min(Math.Max(left, above), upperLeft));
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

    public static class JXL_Pred
    {
        public static byte Filter(in byte[] cLine, in byte[] pLine, long col, int BytesPerPixel)
        {
            return Predictor(cLine[col], (col - BytesPerPixel < 0) ? 0 : cLine[col - BytesPerPixel], pLine[col], (col - BytesPerPixel < 0) ? 0 : pLine[col - BytesPerPixel], false);
        }
        public static byte UnFilter(in byte[] cLine, in byte[] dLine, in byte[] pLine, long col, int BytesPerPixel)
        {
            return Predictor(cLine[col], (col - BytesPerPixel < 0) ? 0 : dLine[col - BytesPerPixel], pLine[col], (col - BytesPerPixel < 0) ? 0 : pLine[col - BytesPerPixel], true);
        }
        private static byte Predictor(int px, int left, int top, int topleft, bool reverse)
        {
            var ac = left - topleft;
            var ab = left - top;
            var bc = top - topleft;
            var grad = ac + top;
            var d = ab ^ bc;
            var zero = 0;
            var clamp = zero > d ? top : left;
            var s = ac ^ bc;
            var pred = zero > s ? grad : clamp;
            return reverse ? (byte)(px + pred) : (byte)(px - pred);
        }
    }
}
