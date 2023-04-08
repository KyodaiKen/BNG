namespace BNG_CORE.Filters {
    public class Paeth {
        public byte Filter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] - InternalFilter((col - BytesPerPixel < 0) ? (byte)0 : cLine[col], pLine[col], (col - BytesPerPixel < 0) ? (byte)0 : pLine[col]) % (uint)256);
        }
        public byte UnFilter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] + InternalFilter((col - BytesPerPixel < 0) ? (byte)0 : cLine[col], pLine[col], (col - BytesPerPixel < 0) ? (byte)0 : pLine[col]) % (uint)256);
        }
        private byte InternalFilter(byte left, byte above, byte upperLeft) {
            uint p = (uint)left + above + upperLeft;
            var pa = (Math.Abs(p - left)) % 256;
            var pb = (Math.Abs(p - above)) % 256;
            var pc = (Math.Abs(p - upperLeft)) % 256;
            if (pa <= pb && pa <= pc) {
                return left;
            } else if ( pb <= pc ) {
                return above;
            } else {
                return upperLeft;
            }
        }
    }

    public class Sub {
        public byte Filter(ref byte[] line, long col, int BytesPerPixel) {
            return (byte)(line[col] - ((col - BytesPerPixel) < 0 ? 0 : line[col - BytesPerPixel]) % (uint)256);
        }
        public byte UnFilter(ref byte[] line, long col, int BytesPerPixel) {
            return (byte)(line[col] + ((col - BytesPerPixel) < 0 ? 0 : line[col - BytesPerPixel]) % (uint)256);
        }
    }

    public class Up {
        public byte Filter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] - pLine[col] % (uint)256);
        }
        public byte UnFilter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)(cLine[col] + pLine[col] % (uint)256);
        }
    }

    public class Average {
        public byte Filter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)((byte)(cLine[col] - Math.Floor((double)((col - BytesPerPixel) < 0 ? 0 : cLine[col - BytesPerPixel] + (col - BytesPerPixel) < 0 ? 0 : pLine[col - BytesPerPixel] / 2d))) % (uint)256);
        }
        public byte UnFilter(ref byte[] cLine, ref byte[] pLine, long col, int BytesPerPixel) {
            return (byte)((byte)(cLine[col] + Math.Floor((double)((col - BytesPerPixel) < 0 ? 0 : cLine[col - BytesPerPixel] + (col - BytesPerPixel) < 0 ? 0 : pLine[col - BytesPerPixel] / 2d))) % (uint)256);
        }
    }
}
