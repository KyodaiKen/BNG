# BNG

Better Network Graphics / Borg Network Graphics

## Under development
- BNGView GUI for viewing (as well as exporting images to) BNG images
- Layer compositing not implemented yet
- Complete code refactoring and cleanup due
- Low free memory modes utilizing temp files for processing as well as automatically reduced number of processing threads
- Inter frame prediction for animation frames

## Milestones
- Multi threading completed. Will be optimized more, though.
- Core components completed, except layer compositing.
- GUI is available, but very bare and cannot save BNG yet. Only opening BNG files works (and it uses too much memory).

## Supports:
- Pixel formats: Gray/Alpha only, Gray with Alpha, RGB, YCrCb, CMYK, RGBA, YCrCbA, CMYKA
- Bit depths: Integer(8, 16 BE, 16 LE) per channel, IEEE Float(32, 64) per channel
- Animations with up to 2,147,483,648 frames with optional inter-frame prediction
- Up to 2,147,483,648 named layers each frame with description for extended notes or others
- Image data partitioning in tiles for files that don't fit in memory (NASA, hobby astronomy, etc.)
- Compressed header and indexes
- Compressed image data using PNG like predictors / pre-filters and entropy encoders (Brotli, LZW, ZSTD)
- Multi threaded encoding and decoding using tiles
- Fast save and load (depending on the compression used as well as image size, platform and processing power of the platform)
