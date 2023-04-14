﻿namespace BNGCORE.Filters {
    //https://www.w3.org/TR/PNG-Filters.html
    public static class Sub {
        public static byte Filter(ref byte[] line, long col, int BytesPerPixel) {
            return (byte)(line[col] - ((col - BytesPerPixel) < 0 ? 0 : line[col - BytesPerPixel]));
        }
        public static byte UnFilter(ref byte[] line, ref byte[] dLine, long col, int BytesPerPixel) {
            return (byte)(line[col] + ((col - BytesPerPixel) < 0 ? 0 : dLine[col - BytesPerPixel]));
        }
    }

    public static class Up {
        public static byte Filter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] - pLine[col]);
        }
        public static byte UnFilter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] + pLine[col]);
        }
    }

    public static class Average {
        public static byte Filter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] - Predictor(ref cLine, ref pLine, col, BytesPerPixel));
        }
        public static byte UnFilter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] + Predictor(ref cLine, ref pLine, col, BytesPerPixel));
        }
        private static int Predictor(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (int)Math.Floor((((col - BytesPerPixel) < 0 ? 0 : cLine[col - BytesPerPixel]) + pLine[col]) / 2d);
        }
    }

    public static class Paeth {
        public static byte Filter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] - Predictor((col - BytesPerPixel < 0) ? (byte)0 : cLine[col - BytesPerPixel], pLine[col], (col - BytesPerPixel < 0) ? (byte)0 : pLine[col - BytesPerPixel]));
        }
        public static byte UnFilter(ref byte[] cLine, ref byte[] dLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] + Predictor((col - BytesPerPixel < 0) ? (byte)0 : dLine[col - BytesPerPixel], pLine[col], (col - BytesPerPixel < 0) ? (byte)0 : pLine[col - BytesPerPixel]));
        }
        private static int Predictor(byte left, byte above, byte upperLeft) {
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