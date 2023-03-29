# BNG

Under development

## Supports:
- Pixel formats: Gray, Alpha only, RGB, YCrCb, CMYK, RGBA, YCrCbA, CMYKA
- Bit depths: Integer(8, 16 BE, 16 LE) per channel, IEEE Float(32, 64) per channel, legacy 16bit RGB (555,565), YCrCb packed subsampling (4:1:0 9bit, 4:2:0 12bit, 4:2:2 16bit, 4:4:4 24 bit)
- Animations with up to 2³² frames
- Up to 65536 named layers with description both in UTF-8, 255 character long names, 64KB long descriptions for extended notes or others
- Image data partitioning in tiles for files that don't fit in memory (NASA, hobby astronomy, etc.)
- Compressed indexes
- Compressed image data using predictors, pre-filters and entropy encoders (ZSTD, Brotli, LZMA, GZIP, Order0 Arithmetic)
- Multi threaded encoding and decoding using tiles
- Fast save and loa (depending on the compression used as well as image size, platform and processing power of the platform)