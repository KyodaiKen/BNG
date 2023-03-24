# Format definition

## Magic word

```
42 4e 47 00   BNG.
```

```c
struct magic {
    char[3] magic = {0x42, 0x4e, 0x47};
    char    version = 0;
}
```

The number at the end is the version number.

## Header structure
```c

enum<char> compr {
    none =          0x0; //  0, 0000

    //The following are only for data
    lzw =           0x1; //  1, 0001 
    lzma =          0x2; //  2, 0010
    zstd =          0x3; //  3, 0011
    arithmetic =    0x4; //  4, 0100
    lzma2 =         0x5; //  5, 0101
    lz4 =           0x6; //  6, 0110
    lzo =           0x7; //  7, 0111
    
    //All others are reserved for later use
}

enum<b4bit> colorfmt {
    mono  =     0x0; //  0, 0000
    RGB =       0x1; //  1, 0001
    RGB555 =    0x2; //  2, 0010
    RGB565 =    0x3; //  3, 0011
    YCrCb444 =  0x4; //  4, 0100
    YCrCb422 =  0x5; //  5, 0101
    YCrCb420 =  0x6; //  6, 0110
    CMYK =      0x7; //  7, 0111
    //Rest is invalid / reserved
}

enum<b4bit> bit_depth {
    bd_8  =             0x0; //  0, 0000
    bd_uint16le =       0x1; //  1, 0001
    bd_uint16be =       0x2; //  2, 0010
    bd_uint24le =       0x3; //  3, 0011
    bd_uint24be =       0x4; //  4, 0100
    bd_uint32le =       0x5; //  5, 0101
    bd_uint32be =       0x6; //  6, 0110
    bd_uint64le =       0x5; //  7, 0111
    bd_uint64be =       0x6; //  8, 1000
    bd_i3e16    =       0x7; //  9, 1001
    bd_i3e32    =       0x8; // 10, 1010
    bd_i3e64    =       0x9; // 11, 1011
    bd_custom   =       0xF; // 15, 1111
}

struct<char> colors {
    colorfmt    color_format
    bit_depth   bits_per_channel
}

struct header {
    char                header_version;
    compr               index_compression;
    compr               data_compression;
    uint32              num_frames;
    uint32              index_size;

    //The following are from the index and can be compressed as one block!
    colors[num_frames]  idx_frame_color_info;
    uint32[num_frames]  idx_frame_data_size_compressed;
    uint32[num_frames]  idx_frame_width;
    uint32[num_frames]  idx_frame_height;
    frame[num_frames]   frames;
}
```

## Frame structure

```c
enum<char> layer_blend_mode {
    replace =       0x00
    alpha_layer =   0x01
    subtract =      0x02
    add =           0x03
    multiply =      0x04
    //More to come
}

struct frame {
    char                    frame_version;
    double                  display_time_seconds;
    uint32                  num_layers;
    uint32                  index_size;

    //The following are from the index and can be compressed as one block!
    uint32[num_layers]                  idx_layer_offset_x;
    uint32[num_layers]                  idx_layer_offset_y;
    uint32[num_layers]                  idx_layer_width;
    uint32[num_layers]                  idx_layer_height;
    layer_blend_mode[num_layers]        idx_layer_blend_mode;
    double[num_layers]                  idx_layer_opacity;
    compr[num_layers]                   idx_layer_compression;
    uint32[num_layers]                  idx_data_size;
    char[num_layers]                    idx_name_lengths
    char[num_layers * idx_name_lengths] idx_names; //utf-8!
}
```

## Data

The data is stored as denoted in the index one frame and their layers after another.
The data offset for each layer is derived from the header length and the data sizes.