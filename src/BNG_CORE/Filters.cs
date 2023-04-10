namespace BNG_CORE.Filters {
    //https://www.w3.org/TR/PNG-Filters.html
    public class Sub {
        public byte Filter(ref byte[] line, long col, int BytesPerPixel) {
            return (byte)(line[col] - ((col - BytesPerPixel) < 0 ? 0 : line[col - BytesPerPixel]));
        }
        public byte UnFilter(ref byte[] line, ref byte[] dLine, long col, int BytesPerPixel) {
            return (byte)(line[col] + ((col - BytesPerPixel) < 0 ? 0 : dLine[col - BytesPerPixel]));
        }
    }

    public class Up {
        public byte Filter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] - pLine[col]);
        }
        public byte UnFilter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] + pLine[col]);
        }
    }

    public class Average {
        public byte Filter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)((byte)(cLine[col] - Math.Floor((double)((col - BytesPerPixel) < 0 ? 0 : cLine[col - BytesPerPixel] + (col - BytesPerPixel) < 0 ? 0 : pLine[col - BytesPerPixel] / 2d))));
        }
        public byte UnFilter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)((byte)(cLine[col] + Math.Floor((double)((col - BytesPerPixel) < 0 ? 0 : cLine[col - BytesPerPixel] + (col - BytesPerPixel) < 0 ? 0 : pLine[col - BytesPerPixel] / 2d))));
        }
    }

    public class Paeth {
        public byte Filter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] - InternalFilter((col - BytesPerPixel < 0) ? (byte)0 : cLine[col - BytesPerPixel], pLine[col], (col - BytesPerPixel < 0) ? (byte)0 : pLine[col]));
        }
        public byte UnFilter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] + InternalFilter((col - BytesPerPixel < 0) ? (byte)0 : cLine[col - BytesPerPixel], pLine[col], (col - BytesPerPixel < 0) ? (byte)0 : pLine[col]));
        }
        private byte InternalFilter(byte left, byte above, byte upperLeft) {
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
