# BNG

Better Network Graphics / Borg Network Graphics

## Under development
- Multi threading not developed yet
- Encoding and decoding of raw images (raw pixel data) working! Tests look promising!
- Layer compositing not implemented yet

## Supports:
- Pixel formats: Gray/Alpha only, Gray with Alpha, RGB, YCrCb, CMYK, RGBA, YCrCbA, CMYKA
- Bit depths: Integer(8, 16 BE, 16 LE) per channel, IEEE Float(32, 64) per channel
- Animations with up to 2,147,483,648 frames
- Up to 2,147,483,648 named layers each frame with description for extended notes or others
- Image data partitioning in tiles for files that don't fit in memory (NASA, hobby astronomy, etc.)
- Compressed header and indexes
- Compressed image data using PNG like predictors / pre-filters and entropy encoders (Brotli, LZW, ZSTD)
- Multi threaded encoding and decoding using tiles
- Fast save and load (depending on the compression used as well as image size, platform and processing power of the platform)
