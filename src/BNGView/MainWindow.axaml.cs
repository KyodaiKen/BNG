using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using HarfBuzzSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BNGView
{
    public class LoadedBitmap
    {
        public Bitmap Bitmap { get; set; }
        public string FileName { get; set; }
    }


    public partial class MainWindow : Window
    {
        private double _zoomLevel;
        private bool _dragging;
        private Vector pointerPosOnDragStart;
        private Vector imageOffsetOnDragStart;

        public List<LoadedBitmap> LoadedBitmaps { get; set; }
        public int activeBitmapID;

        private double ZoomLevel {
            get {
                return _zoomLevel * 100;
            }
            set {
                if (value < 1) value = 1;
                _zoomLevel = value / 100;
                if (ZoomTextBox != null)
                {
                    ZoomTextBox.Text = string.Format("{0:0.###}", value);
                    ZoomLevelSlider.Value = value;
                    BitmapImageObject.Width = LoadedBitmaps[activeBitmapID].Bitmap.PixelSize.Width * _zoomLevel;
                    BitmapImageObject.Height = LoadedBitmaps[activeBitmapID].Bitmap.PixelSize.Height * _zoomLevel;
                }
            }
        }
        public MainWindow()
        {
            InitializeComponent();

            LoadedBitmaps = new();

            _dragging = false;

            //Test load image
            var b = new LoadedBitmap();
            var fileName = @"D:\temp\bng_test\compressed\dragon.bng";
            b.Bitmap = LoadImageFromBNG(fileName);
            b.FileName = fileName;

            LoadedBitmaps.Add(b);
            BitmapImageObject.Source = LoadedBitmaps.First().Bitmap;
            ScrollView.Arrange(new Avalonia.Rect(0, 0, b.Bitmap.PixelSize.Width / 2, b.Bitmap.PixelSize.Height));
            ScrollView.Measure(new Avalonia.Size(b.Bitmap.PixelSize.Width / 2, b.Bitmap.PixelSize.Height));
            Debug.WriteLine(ScrollView.Viewport.ToString());
            activeBitmapID = 0;
        }

        void RebuildTabs()
        {

        }

        unsafe Bitmap LoadImageFromBNG(string fileName)
        {
            Stream file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
            MemoryStream bitmapStream = new();
            BNGCORE.Bitmap bng = new(); BNGCORE.FrameHeader header; StringBuilder log;
            bng.LoadBNG(ref file, out log, out header);
            bng.DecodeLayerToRaw(file, bitmapStream, 0);

            var layer = header.Layers[0];

            SixLabors.ImageSharp.Image<Rgba32> slImg;
            byte[] convBitmapBytes;

            switch (layer.PixelFormat)
            {
                case BNGCORE.PixelFormat.IntegerUnsigned:
                    switch (layer.ColorSpace)
                    {
                        case BNGCORE.ColorSpace.GRAY:
                            switch (layer.BitsPerChannel)
                            {
                                case 8:
                                    {
                                        var tmp = SixLabors.ImageSharp.Image.LoadPixelData<L8>(bitmapStream.ToArray(), (int)layer.Width, (int)layer.Height);
                                        var conv = tmp.CloneAs<Rgba32>();
                                        byte[] convBitmapData = new byte[layer.Width * layer.Height * 4];
                                        conv.CopyPixelDataTo(convBitmapData);
                                        conv.Dispose();
                                        convBitmapBytes = convBitmapData.ToArray();
                                    }
                                    break;
                                case 16:
                                    {
                                        var tmp = SixLabors.ImageSharp.Image.LoadPixelData<L16>(bitmapStream.ToArray(), (int)layer.Width, (int)layer.Height);
                                        var conv = tmp.CloneAs<Rgba32>();
                                        byte[] convBitmapData = new byte[layer.Width * layer.Height * 4];
                                        conv.CopyPixelDataTo(convBitmapData);
                                        conv.Dispose();
                                        convBitmapBytes = convBitmapData.ToArray();
                                    }
                                    break;
                                default:
                                    throw new NotSupportedException("Only 8 bits per channel are currently supported");
                            }
                            break;
                        case BNGCORE.ColorSpace.RGB:
                            switch (layer.BitsPerChannel)
                            {
                                case 8:
                                    var tmp = SixLabors.ImageSharp.Image.LoadPixelData<Rgb24>(bitmapStream.ToArray(), (int)layer.Width, (int)layer.Height);
                                    var conv = tmp.CloneAs<Rgba32>();
                                    byte[] convBitmapData = new byte[layer.Width * layer.Height * 4];
                                    conv.CopyPixelDataTo(convBitmapData);
                                    conv.Dispose();
                                    convBitmapBytes = convBitmapData.ToArray();
                                    break;
                                default:
                                    throw new NotSupportedException("Only 8 bits per channel are currently supported");
                            }
                            break;
                        case BNGCORE.ColorSpace.RGBA:
                            switch (layer.BitsPerChannel)
                            {
                                case 8:
                                    convBitmapBytes = bitmapStream.ToArray();
                                    break;
                                default:
                                    throw new NotSupportedException("Only 8 bits per channel are currently supported");
                            }
                            break;
                        default:
                            throw new NotSupportedException("RGB or RGBA are supported");
                    }
                    break;
                default:
                    throw new NotSupportedException("Only unsigned integer images are supported");
            }

            fixed (byte* p = convBitmapBytes)
            {
                return new Bitmap(Avalonia.Platform.PixelFormat.Rgba8888
                                , Avalonia.Platform.AlphaFormat.Unpremul
                                , (nint)p, new Avalonia.PixelSize((int)layer.Width, (int)layer.Height)
                                , new Avalonia.Vector(header.ResolutionH, header.ResolutionV)
                                , (int)(header.Width * 4));
            }
            
        }

        Bitmap LoadImage(string fileName)
        {
            return new Bitmap(fileName);
        }

        void OnHideUIButtonClick(object sender, RoutedEventArgs args)
        {
            Navigator.IsVisible = false;
            TopBar.IsVisible = false;
        }

        void OnImageSpaceClick(object sender, Avalonia.Input.PointerReleasedEventArgs args)
        {
            switch (args.MouseButton)
            {
                case Avalonia.Input.MouseButton.Middle:
                    Navigator.IsVisible = !Navigator.IsVisible;
                    TopBar.IsVisible = !TopBar.IsVisible;
                    break;
            }
        }

        void OnDragStart(object sender, Avalonia.Input.PointerPressedEventArgs args)
        {
            switch (args.MouseButton)
            {
                case Avalonia.Input.MouseButton.Left:
                    _dragging = true;
                    pointerPosOnDragStart = args.GetPosition(ScrollView);
                    imageOffsetOnDragStart = ScrollView.Offset;
                    break;
            }
        }

        void OnDragging(object sender, Avalonia.Input.PointerEventArgs args)
        {
            if (_dragging)
            {
                var pointerPos = args.GetPosition(ScrollView);
                var lbSize = LoadedBitmaps[activeBitmapID].Bitmap.PixelSize;
                var newOffset = new Vector(pointerPosOnDragStart.X - pointerPos.X + imageOffsetOnDragStart.X, pointerPosOnDragStart.Y - pointerPos.Y + imageOffsetOnDragStart.Y);
                ScrollView.Offset = newOffset;
            }
        }

        void OnDragEnd(object sender, Avalonia.Input.PointerReleasedEventArgs args)
        {
            switch (args.MouseButton)
            {
                case Avalonia.Input.MouseButton.Left:
                    _dragging = false;
                    break;
            }
        }

        void ZoomTextBoxPropertyChanged(object sender, Avalonia.AvaloniaPropertyChangedEventArgs args)
        {
            if (args.NewValue != null)
            {
                switch (args.Property.Name)
                {
                    case "Text":

                        double parsed;
                        if (double.TryParse((string)args.NewValue, out parsed))
                        {
                            if (ZoomLevelSlider != null) ZoomLevelSlider.Value = parsed;
                            ZoomLevel = parsed;
                        }
                        break;
                }
            }
        }

        void ZoomSliderPropertyChanged(object sender, Avalonia.AvaloniaPropertyChangedEventArgs args)
        {
            if (args.NewValue != null)
            {
                switch (args.Property.Name)
                {
                    case "Value":
                        ZoomLevel = (double)args.NewValue;
                        break;
                }
            }
        }
    }
}