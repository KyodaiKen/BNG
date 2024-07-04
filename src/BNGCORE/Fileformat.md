# KURIF File structure

# Main header
| Data Type | Field Name            | Contents                          | Extended Info |
| ------  | -----------             | --------------------------------- | |
| uint    | MAGIC_WORD              | Byte 0-2: KIF; Byte 3: INFO_BYTE  | |
| UUID    | UUID                    | Universally Unique ID of the file | |
| uint    | WIDTH                   |                                   | |
| uint    | HEIGHT                  |                                   | |
| byte    | COMOPSITING_COLOR_SPACE | Color Space (RGB, RGBA...)        | enum |
| byte    | CHANNEL_DATA_FORMAT     | Channel Data Format               | enum |
| byte    | USAGE                   | Usage (Gallery or Animation)      | enum |
| double  | PPI_RES_H               | Horizontal resolution in pixels/in| |
| double  | PPI_RES_V               | Vertical resolution in pixels/in  | |
| uint    | METADATA_NUM_FIELDS     | Number of metadata fields         | |
|         | **For each field**      |                                   | |
| byte[]  | METADATA_DATA           |                                   | |
|         | **End for each**        |                                   | |
| uint[]  | METADATA_FIELD_LENGTHS  |                                   | |

## Color Space Table
| Name                       | Value |
| -------------------------- | ----- |
| GRAY                       | 1     |
| GRAYA_Straight             | 2     |
| GRAYA_PreMult              | 3     |
| RGB                        | 4     |
| RGBA_Straight              | 5     |
| RGBA_PreMult               | 6     |
| CMYK                       | 7     |
| CMYKA_Straight             | 8     |
| CMYKA_PreMult              | 9     |
| YCrCb                      | 10    |
| YCrCbA_Straight            | 11    |
| YCrCbA_PreMult             | 11    |

## Channel Data Format Table
| Name                       | Value | No. Bits | Data Type    |
| -------------------------- | ----- | -------- | ------------ |
| UINT8                      | 1     | 8        | Unsigned Int |
| UINT16                     | 2     | 16       | Unsigned Int |
| UINT32                     | 3     | 32       | Unsigned Int |
| UINT64                     | 4     | 64       | Unsigned Int |
| FLOAT16                    | 5     | 16       | IEEE Float   |
| FLOAT32                    | 6     | 32       | IEEE Float   |
| FLOAT64                    | 7     | 64       | IEEE Float   |
| FLOAT128                   | 8     | 128      | IEEE Float   |

## Usage table
| Name                       | Value |
| -------------------------- | ----- |
| Gallery                    | 1     |
| Animation                  | 2     |

# Metadata header
| Data Type | Field Name            | Contents                          | Extended Info |
| ------- | -----------             | --------------------------------- | |
| uint    | METADATA_FIELD_ID       | Metadata field ID                 | |
| uint    | METADATA_REF_FIELD_ID   | Field ID referencing to another   | |
| byte[8] | METADATA_TYPE           | Type (8 characters max)           | |
| byte[]  | METADATA                | Metadata bytes, text is UTF-8     | Max size is 65528 |

# Frame header
| Data Type | Field Name          | Contents                             | Extended Info |
| ------  | -----------           | ---------------------------------    | |
| uint    | FRM_MAGIC_WORD        | Byte 0-2: KFR; Byte 3: RESERVED      | |
| ulong   | FRM_SEQ_NBR           | Frame sequence number                | |
| byte    | FRM_NAME_LEN          | Length of frame name in bytes        | |
| byte[]  | FRM_NAME_STR          | Frame name as byte array for UTF-8   | |
| ushort  | FRM_DESCR_LEN         | Length of frame description in bytes | |
| byte[]  | FRM_DESCR_STR         | Frame name as byte array for UTF-8   | |
| uint    | FRM_DISPL_DUR         | Frame display duration in ms         | |
| uint    | METADATA_NUM_FIELDS   | Number of metadata fields            | |
|         | **For each field**    |                                      | |
| byte[]  | METADATA_DATA         |                                      | |
|         | **End for each**      |                                      | |
| uint[]  | METADATA_FIELD_LENGTHS|                                      | |
| ulong   | FRM_NUM_LAYERS        | Frame layer count                    | |
|         | **For each layer**    |                                      | |
| byte[]  | FRM_LAYER_DATA        |                                      | |
|         | **End for each**      |                                      | |
| ulong[] | FRM_LAYER_DATA_LENGTHS|                                      | |

# Layer header (within FRM_LAYER_DATA starting at layer offset 0)
| Data Type | Field Name            | Contents                             | Extended Info |
| ------  | -----------             | ---------------------------------    | |
| byte    | LAYER_NAME_LEN          | Length of layer name in bytes        | |
| byte[]  | LAYER_NAME_STR          | Layer name as byte array for UTF-8   | |
| ushort  | LAYER_DESCR_LEN         | Length of layer description in bytes | |
| byte[]  | LAYER_DESCR_STR         | Layer name as byte array for UTF-8   | |
| uint    | LAYER_WIDRH             |                                      | |
| uint    | LAYER_HEIGHT            |                                      | |
| uint    | LAYER_OFFSET_X          | Pixel offset where layer is placed   | |
| uint    | LAYER_OFFSET_Y          | Pixel offset where layer is placed   | |
| byte    | LAYER_COLOR_SPACE       | Color Space (RGB, RGBA...)           | enum |
| byte    | L_CHANNEL_DATA_FORMAT   | Channel Data Format                  | enum |
| byte    | LAYER_BLEND_MODE        |                                      | enum |
| double  | LAYER_OPACITY           | between 0 and 1                      | |
| uint    | METADATA_NUM_FIELDS     | Number of metadata fields            | |
|         | **For each field**      |                                      | |
| byte[]  | METADATA_DATA           |                                      | |
|         | **End for each**        |                                      | |
| uint[]  | METADATA_FIELD_LENGTHS  |                                   | |
| ulong   | LAYER_NUM_TILES         | Frame layer count                    | |
|         | **For each tile**       |                                      | |
| byte[]  | LAYER_TILE_DATA         |                                      | |
|         | **End for each**        |                                      | |
| ulong[] | LAYER_TILE_DATA_LENGTHS |                                      | |

## Layer blend mode table
| Name                       | Value |
| -------------------------- | ----- |
| Normal                     | 1     |
| Multiply                   | 2     |
| Divide                     | 3     |
| Add                        | 4     |
| Subtract                   | 5     |


# Tile header (within LAYER_TILE_DATA starting at tile offset 0)
| Data Type | Field Name            | Contents                                | Extended Info |
| ------  | -----------             | ---------------------------------       | |
| byte    | TILE_PREDICTOR          | Predictor used to compress this tile    | enum |
| byte    | TILE_COMPRESSION        | Compression algorithm used on this tile | enum |
| byte[]  | TILE_COMPRESSED_DATA    |                                         | |

## Tile predictor table
| Name                       | Value |
| -------------------------- | ----- |
| None                       | 1     |
| Sub                        | 2     |
| Up                         | 3     |
| Average                    | 4     |
| Median                     | 5     |
| Median                     | 6     |
| Paeth                      | 7     |
| JXLPredt                   | 8     |

## Tile compression table
| Name                       | Value |
| -------------------------- | ----- |
| None                       | 1     |
| Brotli                     | 2     |
| ZSTD                       | 3     |
| LZW                        | 4     |
| LZMA                       | 5     |
| XZ                         | 6     |
