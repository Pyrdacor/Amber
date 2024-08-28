using Silk.NET.Core.Contexts;
using Silk.NET.Input;
using Silk.NET.Input.Glfw;
using Silk.NET.SDL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using System.Reflection;
using MousePosition = System.Numerics.Vector2;
using WindowDimension = Silk.NET.Maths.Vector2D<int>;
using Amber.Renderer.OpenGL;
using Renderer = Ambermoon.Renderer.OpenGL.Renderer;
using Amber.Common;
using Amber.Serialization;
using Amber.Renderer;
using Color = Amber.Common.Color;
using Amber.Assets.Common;
using Amberstar.GameData.Legacy;
using Amber.IO.FileSystem;
using Amberstar.net;
using Silk.NET.OpenGL;
using Amberstar.Game;
using Amberstar.GameData.Serialization;

namespace Amberstar
{
	class GameWindow : IContextProvider
    {
        string gameVersion = "Amberstar.net";
        Renderer? renderer;
        IWindow? window;
        IKeyboard? keyboard = null;
        IMouse? mouse = null;
        ICursor? cursor = null;
        Game.Game? game = null;
        bool initialized = false;

        public string Identifier { get; }
        public IGLContext? GLContext => window?.GLContext;
        public int Width { get; private set; }
        public int Height { get; private set; }

        public GameWindow(string id = "MainWindow")
        {
            Identifier = id;
        }

        void SetupInput(IInputContext inputContext)
        {
            keyboard = inputContext.Keyboards.FirstOrDefault(k => k.IsConnected);

            if (keyboard != null)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
                keyboard.KeyUp += Keyboard_KeyUp;
                keyboard.KeyChar += Keyboard_KeyChar;
            }

            mouse = inputContext.Mice.FirstOrDefault(m => m.IsConnected);

            if (mouse != null)
            {
                cursor = mouse.Cursor;
                cursor.CursorMode = CursorMode.Hidden;
                mouse.MouseDown += Mouse_MouseDown;
                mouse.MouseUp += Mouse_MouseUp;
                mouse.MouseMove += Mouse_MouseMove;
                mouse.Scroll += Mouse_Scroll;
            }
        }

        List<Game.Key> QueryPressedKeys()
        {
            return keyboard?.SupportedKeys?.Where(key => keyboard.IsKeyPressed(key)).Select(InputConverter.Convert).Where(key => key != Game.Key.Invalid).ToList() ?? [];
        }

        void Keyboard_KeyChar(IKeyboard keyboard, char keyChar)
        {
			game?.KeyChar(keyChar, InputConverter.GetModifiers(keyboard));
		}

        void Keyboard_KeyDown(IKeyboard keyboard, Silk.NET.Input.Key key, int value)
        {
            game?.KeyDown(InputConverter.Convert(key), InputConverter.GetModifiers(keyboard));
		}

        void Keyboard_KeyUp(IKeyboard keyboard, Silk.NET.Input.Key key, int value)
        {
			game?.KeyUp(InputConverter.Convert(key), InputConverter.GetModifiers(keyboard));
		}

        void Mouse_MouseDown(IMouse mouse, MouseButton button)
        {
            game?.MouseDown
            (
                InputConverter.ConvertMousePosition(mouse.Position),
                InputConverter.ConvertMouseButtons(button),
                InputConverter.GetModifiers(keyboard!)
            );
        }

        void Mouse_MouseUp(IMouse mouse, MouseButton button)
        {
			game?.MouseUp
			(
				InputConverter.ConvertMousePosition(mouse.Position),
				InputConverter.ConvertMouseButtons(button),
				InputConverter.GetModifiers(keyboard!)
			);
		}

        void Mouse_MouseMove(IMouse mouse, MousePosition position)
        {
			game?.MouseMove
			(
				InputConverter.ConvertMousePosition(mouse.Position),
				InputConverter.GetMouseButtons(mouse)
			);
		}

        void Mouse_Scroll(IMouse mouse, ScrollWheel wheelDelta)
        {
			game?.MouseWheel
			(
				InputConverter.ConvertMousePosition(mouse.Position),
                wheelDelta.X, wheelDelta.Y,
				InputConverter.GetMouseButtons(mouse)
			);
		}

        static void WritePNG(string filename, byte[] rgbData, Size imageSize, bool alpha, bool upsideDown)
        {
            if (File.Exists(filename))
                filename += Guid.NewGuid().ToString();

            filename += ".png";

            int bpp = alpha ? 4 : 3;
            var writer = new DataWriter();

            void WriteChunk(string name, Action<DataWriter>? dataWriter)
            {
                var internalDataWriter = new DataWriter();
                dataWriter?.Invoke(internalDataWriter);
                var data = internalDataWriter.ToArray();

                writer.Write((uint)data.Length);
                writer.WriteWithoutLength(name);
                writer.Write(data);
                var crc = new PngCrc();
                uint headerCrc = crc.Calculate(new byte[] { (byte)name[0], (byte)name[1], (byte)name[2], (byte)name[3] });
                writer.Write(crc.Calculate(headerCrc, data));
            }

            // Header
            writer.Write(0x89);
            writer.Write(0x50);
            writer.Write(0x4E);
            writer.Write(0x47);
            writer.Write(0x0D);
            writer.Write(0x0A);
            writer.Write(0x1A);
            writer.Write(0x0A);

            // IHDR chunk
            WriteChunk("IHDR", writer =>
            {
                writer.Write((uint)imageSize.Width);
                writer.Write((uint)imageSize.Height);
                writer.Write(8); // 8 bits per color
                writer.Write((byte)(alpha ? 6 : 2)); // With alpha (RGBA) or color only (RGB)
                writer.Write(0); // Deflate compression
                writer.Write(0); // Default filtering
                writer.Write(0); // No interlace
            });

            WriteChunk("IDAT", writer =>
            {
                byte[] dataWithFilterBytes = new byte[rgbData.Length + imageSize.Height];
                for (int y = 0; y < imageSize.Height; ++y)
                {
                    int i = upsideDown ? imageSize.Height - y - 1 : y;
                    System.Buffer.BlockCopy(rgbData, y * imageSize.Width * bpp, dataWithFilterBytes, 1 + i + i * imageSize.Width * bpp, imageSize.Width * bpp);
                }
                // Note: Data is initialized with 0 bytes so the filter bytes are already 0.
                using var uncompressedStream = new MemoryStream(dataWithFilterBytes);
                using var compressedStream = new MemoryStream();
                var compressStream = new System.IO.Compression.DeflateStream(compressedStream, System.IO.Compression.CompressionLevel.Optimal, true);
                uncompressedStream.CopyTo(compressStream);
                compressStream.Close();

                // Zlib header
                writer.Write(0x78); // 32k window deflate method
                writer.Write(0xDA); // Best compression, no dict and header is multiple of 31

                uint Adler32()
                {
                    uint s1 = 1;
                    uint s2 = 0;

                    for (int n = 0; n < dataWithFilterBytes.Length; ++n)
                    {
                        s1 = (s1 + dataWithFilterBytes[n]) % 65521;
                        s2 = (s2 + s1) % 65521;
                    }

                    return (s2 << 16) | s1;
                }

                // Compressed data
                writer.Write(compressedStream.ToArray());

                // Checksum
                writer.Write(Adler32());
            });

            // IEND chunk
            WriteChunk("IEND", null);

            using var file = File.Create(filename);
            writer.CopyTo(file);
        }

        void Window_Load()
        {
            if (window!.Native?.Glfw is null)
            {
                Console.WriteLine("WARNING: The current window is not a GLFW window." + Environment.NewLine +
                                  "         Other window systems may be not fully supported!");
            }

            //var windowIcon = new Silk.NET.Core.RawImage(16, 16, new Memory<byte>(Resources.WindowIcon));
            //window.SetWindowIcon(ref windowIcon);

            window.MakeCurrent();

            // Setup input
            SetupInput(window.CreateInput());

            var platform = Silk.NET.Windowing.Window.GetWindowPlatform(false);

            window.Monitor = platform!.GetMainMonitor();
            window.Size = new WindowDimension(320 * 3, 200 * 3);

            var gl = GL.GetApi(GLContext);
            gl.Viewport(new System.Drawing.Size(window.FramebufferSize.X, window.FramebufferSize.Y));
            gl.ClearColor(System.Drawing.Color.Black);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GLContext!.SwapBuffers();

            window.Center();

            renderer = new(this, new Size(Width, Height), new Size(320, 200));

			var fileSystem = FileSystem.FromOperatingSystemPath(@"D:\Projects\Amber\German\AmberfilesST");

			var assetProvider = new AssetProvider(fileSystem.AsReadOnly());

            // setup the layers
            LayerSetup.Run(assetProvider, renderer, out var uiGraphicIndexProvider,
                out var paletteIndexProvider, out var paletteColorProvider, out var fontInfoProvider);

			game = new Game.Game(renderer, assetProvider, uiGraphicIndexProvider, paletteIndexProvider,
                paletteColorProvider, fontInfoProvider, QueryPressedKeys);

			initialized = true;
        }

        void Window_Render(double delta)
        {
            if (window != null && window.WindowState != WindowState.Minimized)
            {
				var gl = GL.GetApi(GLContext);
				gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

				game?.Render(delta);
				renderer?.Render();

                window.SwapBuffers();
            }
        }

        void Window_Update(double delta)
        {
            game?.Update(delta);
        }

        void Window_Resize(WindowDimension size)
        {
            if (size.X != Width || size.Y != Height)
            {
                Width = size.X;
                Height = size.Y;
                window!.Size = new WindowDimension(Width, Height);
                EnsureWindowOnMonitor();

                renderer?.Resize(new Size(window!.FramebufferSize.X, window.FramebufferSize.Y));
            }
        }

        void Window_FramebufferResize(WindowDimension size)
        {
            renderer?.Resize(new Size(size.X, size.Y));
        }

        void Window_StateChanged(WindowState state)
        {
            /*if (state == WindowState.Minimized)
                Game?.PauseGame();
            else
                Game?.ResumeGame();*/
        }

        void Window_Move(WindowDimension position)
        {

        }

        void WindowMoved()
        {

        }

        void EnsureWindowOnMonitor()
        {
            var bounds = window!.Monitor?.Bounds;
            WindowDimension upperLeft = bounds?.Origin ?? new WindowDimension(0, 0);
            int? newX = null;
            int? newY = null;

            if (window.Position.X - window.BorderSize.Origin.X < upperLeft.X)
            {
                newX = upperLeft.X + window.BorderSize.Origin.X;
            }
            else if (bounds != null && window.Position.X >= upperLeft.X + bounds.Value.Size.X)
            {
                newX = Math.Max(upperLeft.X + window.BorderSize.Origin.X, upperLeft.X + bounds.Value.Size.X - window.Size.X - window.BorderSize.Origin.X - window.BorderSize.Size.X);
            }

            if (window.Position.Y - window.BorderSize.Origin.Y < upperLeft.Y)
            {
                newY = upperLeft.Y + window.BorderSize.Origin.Y;
            }
            else if (bounds != null && window.Position.Y >= upperLeft.Y + bounds.Value.Size.Y)
            {
                newY = Math.Max(upperLeft.Y + window.BorderSize.Origin.Y, upperLeft.Y + bounds.Value.Size.Y - window.Size.Y - window.BorderSize.Origin.Y - window.BorderSize.Size.Y);
            }

            if (newX != null || newY != null)
            {
                window.Position = new WindowDimension(newX ?? window.Position.X, newY ?? window.Position.Y);
            }
        }

        public void Run()
        {
            Width = 6 * 320;
            Height = 6 * 200;

#if GLES
            var api = new GraphicsAPI
                (ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(2, 0));
#else
            var api = GraphicsAPI.Default;
#endif
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            gameVersion = $"Amberstar.net v{version.Major}.{version.Minor}.{version.Build}";
            var videoMode = new VideoMode(60);
            var options = new WindowOptions(true, new WindowDimension(100, 100),
                new WindowDimension(Width, Height), 60.0, 120.0, api, gameVersion,
                WindowState.Normal, WindowBorder.Fixed, true, false, videoMode, 24);
            options.WindowClass = "Amberstar.net";

            GlfwWindowing.RegisterPlatform();
            GlfwInput.RegisterPlatform();
            GlfwWindowing.Use();
            window = (IWindow)Silk.NET.Windowing.Window.GetView(new ViewOptions(options));
            window.Title = options.Title;
            window.Size = options.Size;
            window.WindowBorder = options.WindowBorder;
            window.Load += Window_Load;
            window.Render += Window_Render;
            window.Update += Window_Update;
            window.Resize += Window_Resize;
            window.FramebufferResize += Window_FramebufferResize;
            window.Move += Window_Move;
            window.StateChanged += Window_StateChanged;
            window.Run();
        }
    }
}
