using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using static ImGuiNET.ImGuiNative;
using SixLabors.ImageSharp.PixelFormats;

namespace ImGuiNET
{
    class Program
    {
        private static Sdl2Window _window;
        private static GraphicsDevice _gd;
        private static CommandList _cl;
        private static ImGuiController _controller;
        // private static MemoryEditor _memoryEditor;

        // UI state
        private static float _ZoomLevel = 100.0f;
        private static int _counter = 0;
        private static int _dragInt = 0;
        private static Vector3 _clearColor = new Vector3(0, 0, 0);
        private static bool _showGUI = true;
        private static bool _showAnotherWindow = false;
        private static bool _showMemoryEditor = false;
        private static byte[] _memoryEditorData;
        private static uint s_tab_bar_flags = (uint)ImGuiTabBarFlags.Reorderable;
        static bool[] s_opened = { true, true, true, true }; // Persistent user state

        private static string _ImageFileName;
        private static ImageSharpTexture _img;
        private static nint _imgPtr;

        static void SetThing(out float i, float val) { i = val; }

        static void Main(string[] args)
        {
            // Create window, GraphicsDevice, and all resources necessary for the demo.+
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "BNGView"),
                new GraphicsDeviceOptions(false, null, true, ResourceBindingModel.Improved, true, true),
                GraphicsBackend.Vulkan,
                out _window,
                out _gd);
            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _controller.WindowResized(_window.Width, _window.Height);
            };
            _cl = _gd.ResourceFactory.CreateCommandList();
            _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);


            //Pass file name for opening a file
            if(args.Length > 0 )
            {
                _ImageFileName = args[0];
                if (Path.GetExtension(_ImageFileName).ToLower() == "bng")
                {

                }
                else
                {
                    _img = new ImageSharpTexture(_ImageFileName);
                    var dimg = _img.CreateDeviceTexture(_gd, _gd.ResourceFactory);

                    var viewDesc = new TextureViewDescription(dimg, PixelFormat.R8_G8_B8_A8_UNorm); //Pixel Format needed may change, I found UNorm looks closer to the image src then UnormSRGB does 
                    var textureView = _gd.ResourceFactory.CreateTextureView(viewDesc);

                    _imgPtr = _controller.GetOrCreateImGuiBinding(_gd.ResourceFactory, textureView);
                }
            }
            
            var stopwatch = Stopwatch.StartNew();
            float deltaTime = 0f;
            // Main application loop
            while (_window.Exists)
            {
                deltaTime = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
                stopwatch.Restart();
                InputSnapshot snapshot = _window.PumpEvents();
                if (!_window.Exists) { break; }
                _controller.Update(deltaTime, snapshot); // Feed the input events to our ImGui controller, which passes them through to ImGui.

                foreach (var keyEvt in snapshot.KeyEvents)
                {
                    switch (keyEvt.Key)
                    {
                        case Key.Space:
                            if(keyEvt.Down)
                                _showGUI = !_showGUI;
                            break;
                    }
                }

                SubmitUI();

                _cl.Begin();
                _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, new RgbaFloat(_clearColor.X, _clearColor.Y, _clearColor.Z, 1f));
                _controller.Render(_gd, _cl);
                _cl.End();
                _gd.SubmitCommands(_cl);
                _gd.SwapBuffers(_gd.MainSwapchain);
            }

            // Clean up Veldrid resources
            _gd.WaitForIdle();
            _controller.Dispose();
            _cl.Dispose();
            _gd.Dispose();
        }

        private static unsafe void SubmitUI()
        {
            // Demo code adapted from the official Dear ImGui demo program:
            // https://github.com/ocornut/imgui/blob/master/examples/example_win32_directx11/main.cpp#L172

            // 1. Show a simple window.
            // Tip: if we don't call ImGui.BeginWindow()/ImGui.EndWindow() the widgets automatically appears in a window called "Debug".

            bool _showImage = true;
            if (ImGui.Begin("Image", ref _showImage, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus
                    | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.AlwaysHorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar))
            {
                ImGui.SetWindowPos(new Vector2(0, 0));
                ImGui.SetWindowSize(new Vector2(_window.Width, _window.Height));

                if (_imgPtr > 0)
                {
                    ImGui.Image(_imgPtr, new Vector2(_img.Width * _ZoomLevel / 100f, _img.Height * _ZoomLevel / 100f));
                }
                ImGui.End();
            }

            if (_showGUI)
            {
                if (ImGui.Begin("Menu", ref _showGUI, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration))
                {
                    ImGui.SetWindowPos(new Vector2(28, 28));
                    ImGui.SetWindowSize(new Vector2( 100, 16));

                    if (ImGui.BeginMenu("File"))
                    {
                        if (ImGui.MenuItem("Open"))
                        {
                            var dlgResult = NativeFileDialogSharp.Dialog.FileOpen("bng;png;tiff;tif;gif");
                            if(dlgResult.IsOk)
                            {
                                var path = dlgResult.Path;
                                Debug.WriteLine(dlgResult.Path);
                                _img = new ImageSharpTexture(dlgResult.Path);
                                
                                var dimg = _img.CreateDeviceTexture(_gd, _gd.ResourceFactory);

                                var viewDesc = new TextureViewDescription(dimg, PixelFormat.R8_G8_B8_A8_UNorm);
                                var textureView = _gd.ResourceFactory.CreateTextureView(viewDesc);

                                _imgPtr = _controller.GetOrCreateImGuiBinding(_gd.ResourceFactory, textureView); 

                            }
                        }
                        if (ImGui.MenuItem("Export as..."))
                        {
                            var dlgResult = NativeFileDialogSharp.Dialog.FileSave("bng;png;tiff;tif;gif");
                            if (dlgResult.IsOk)
                            {
                                var path = dlgResult.Path;
                                Debug.WriteLine(path);
                            }
                        }
                        ImGui.EndMenu();
                    }
                }
                else
                {
                    ImGui.End();
                }

                if (ImGui.Begin("Navigator", ref _showGUI, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration))
                {
                    ImGui.SetWindowPos(new Vector2(_window.Width - 348, _window.Height - 348));
                    ImGui.SetWindowSize(new Vector2(320, 320));

                    ImGui.DragFloat("Zoom", ref _ZoomLevel, 1f, 1f, 1600.0f, null, ImGuiSliderFlags.AlwaysClamp);
                    if(ImGui.BeginChildFrame(0, new Vector2(304, 278))) {

                        if (_imgPtr > 0)
                        {
                            ImGui.Image(_imgPtr, new Vector2(306, 270));
                        }                        
                        ImGui.EndChildFrame();
                    }
                }
                else
                {
                    ImGui.End();
                }
            }


            /*
            ImGui.Text("Hello, world!");                                        // Display some text (you can use a format string too)
            ImGui.SliderFloat("float", ref _f, 0, 1, _f.ToString("0.000"));  // Edit 1 float using a slider from 0.0f to 1.0f    
            //ImGui.ColorEdit3("clear color", ref _clearColor);                   // Edit 3 floats representing a color

            ImGui.Text($"Mouse position: {ImGui.GetMousePos()}");

            ImGui.Checkbox("ImGui Demo Window", ref _showImGuiDemoWindow);                 // Edit bools storing our windows open/close state
            ImGui.Checkbox("Another Window", ref _showAnotherWindow);
            ImGui.Checkbox("Memory Editor", ref _showMemoryEditor);
            if (ImGui.Button("Button"))                                         // Buttons return true when clicked (NB: most widgets return true when edited/activated)
                _counter++;
            ImGui.SameLine(0, -1);
            ImGui.Text($"counter = {_counter}");

            ImGui.DragInt("Draggable Int", ref _dragInt);

            float framerate = ImGui.GetIO().Framerate;
            ImGui.Text($"Application average {1000.0f / framerate:0.##} ms/frame ({framerate:0.#} FPS)");

            // 2. Show another simple window. In most cases you will use an explicit Begin/End pair to name your windows.
            if (_showAnotherWindow)
            {
                ImGui.Begin("Another Window", ref _showAnotherWindow);
                ImGui.Text("Hello from another window!");
                if (ImGui.Button("Close Me"))
                    _showAnotherWindow = false;
                ImGui.End();
            }

            // 3. Show the ImGui demo window. Most of the sample code is in ImGui.ShowDemoWindow(). Read its code to learn more about Dear ImGui!
            if (_showImGuiDemoWindow)
            {
                // Normally user code doesn't need/want to call this because positions are saved in .ini file anyway.
                // Here we just want to make the demo initial state a bit more friendly!
                ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
                ImGui.ShowDemoWindow(ref _showImGuiDemoWindow);
            }
            
            if (ImGui.TreeNode("Tabs"))
            {
                if (ImGui.TreeNode("Basic"))
                {
                    ImGuiTabBarFlags tab_bar_flags = ImGuiTabBarFlags.None;
                    if (ImGui.BeginTabBar("MyTabBar", tab_bar_flags))
                    {
                        if (ImGui.BeginTabItem("Avocado"))
                        {
                            ImGui.Text("This is the Avocado tab!\nblah blah blah blah blah");
                            ImGui.EndTabItem();
                        }
                        if (ImGui.BeginTabItem("Broccoli"))
                        {
                            ImGui.Text("This is the Broccoli tab!\nblah blah blah blah blah");
                            ImGui.EndTabItem();
                        }
                        if (ImGui.BeginTabItem("Cucumber"))
                        {
                            ImGui.Text("This is the Cucumber tab!\nblah blah blah blah blah");
                            ImGui.EndTabItem();
                        }
                        ImGui.EndTabBar();
                    }
                    ImGui.Separator();
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Advanced & Close Button"))
                {
                    // Expose a couple of the available flags. In most cases you may just call BeginTabBar() with no flags (0).
                    ImGui.CheckboxFlags("ImGuiTabBarFlags_Reorderable", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.Reorderable);
                    ImGui.CheckboxFlags("ImGuiTabBarFlags_AutoSelectNewTabs", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.AutoSelectNewTabs);
                    ImGui.CheckboxFlags("ImGuiTabBarFlags_NoCloseWithMiddleMouseButton", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.NoCloseWithMiddleMouseButton);
                    if ((s_tab_bar_flags & (uint)ImGuiTabBarFlags.FittingPolicyMask) == 0)
                        s_tab_bar_flags |= (uint)ImGuiTabBarFlags.FittingPolicyDefault;
                    if (ImGui.CheckboxFlags("ImGuiTabBarFlags_FittingPolicyResizeDown", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.FittingPolicyResizeDown))
                s_tab_bar_flags &= ~((uint)ImGuiTabBarFlags.FittingPolicyMask ^ (uint)ImGuiTabBarFlags.FittingPolicyResizeDown);
                    if (ImGui.CheckboxFlags("ImGuiTabBarFlags_FittingPolicyScroll", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.FittingPolicyScroll))
                s_tab_bar_flags &= ~((uint)ImGuiTabBarFlags.FittingPolicyMask ^ (uint)ImGuiTabBarFlags.FittingPolicyScroll);

                    // Tab Bar
                    string[] names = { "Artichoke", "Beetroot", "Celery", "Daikon" };

                    for (int n = 0; n < s_opened.Length; n++)
                    {
                        if (n > 0) { ImGui.SameLine(); }
                        ImGui.Checkbox(names[n], ref s_opened[n]);
                    }

                    // Passing a bool* to BeginTabItem() is similar to passing one to Begin(): the underlying bool will be set to false when the tab is closed.
                    if (ImGui.BeginTabBar("MyTabBar", (ImGuiTabBarFlags)s_tab_bar_flags))
                    {
                        for (int n = 0; n < s_opened.Length; n++)
                            if (s_opened[n] && ImGui.BeginTabItem(names[n], ref s_opened[n]))
                            {
                                ImGui.Text($"This is the {names[n]} tab!");
                                if ((n & 1) != 0)
                                    ImGui.Text("I am an odd tab.");
                                ImGui.EndTabItem();
                            }
                        ImGui.EndTabBar();
                    }
                    ImGui.Separator();
                    ImGui.TreePop();
                }
                ImGui.TreePop();
            }

            ImGuiIOPtr io = ImGui.GetIO();
            SetThing(out io.DeltaTime, 2f);

            if (_showMemoryEditor)
            {
                ImGui.Text("Memory editor currently supported.");
                // _memoryEditor.Draw("Memory Editor", _memoryEditorData, _memoryEditorData.Length);
            }
            */
            
        }
    }
}
