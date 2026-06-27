using Silk.NET.Core.Contexts;
using Silk.NET.Input;
using Silk.NET.Input.Glfw;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using System.Reflection;
using MousePosition = System.Numerics.Vector2;
using WindowDimension = Silk.NET.Maths.Vector2D<int>;
using Amber.Renderer.OpenGL;
using Amber.Common;
using Amber.IO.FileSystem;
using AmberIsland.Game;
using Key = Silk.NET.Input.Key;

namespace AmberIsland
{
	class GameWindow(string id = "MainWindow") : IContextProvider
    {
        const string CharacterSpriteSheetPath = @"D:\Projects\Amber\AmberIsland\assets\character_base\char_a_p1\char_a_p1_0bas_humn_v00.png";

        string gameVersion = "AmberIsland";
        Renderer? renderer;
        IWindow? window;
        IKeyboard? keyboard = null;
        IMouse? mouse = null;
        ICursor? cursor = null;
        Game.Game? game = null;

        public string Identifier { get; } = id;
        public IGLContext? GLContext => window?.GLContext;
        public int Width { get; private set; }
        public int Height { get; private set; }

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
                cursor.CursorMode = CursorMode.Normal;
                mouse.MouseDown += Mouse_MouseDown;
                mouse.MouseUp += Mouse_MouseUp;
                mouse.MouseMove += Mouse_MouseMove;
                mouse.Scroll += Mouse_Scroll;
            }
        }

		MousePosition WindowToVirtualScreen(MousePosition position)
        {
            float x = position.X * Game.Game.VirtualScreenWidth / Width;
			float y = position.Y * Game.Game.VirtualScreenHeight / Height;

            return new MousePosition(x, y);
		}

        HashSet<Game.Key> QueryPressedKeys()
        {
            return keyboard?.SupportedKeys?.Where(key => keyboard.IsKeyPressed(key)).Select(InputConverter.Convert).Where(key => key != Game.Key.Invalid).ToHashSet() ?? [];
        }

        void Keyboard_KeyChar(IKeyboard keyboard, char keyChar)
        {
			game?.KeyChar(keyChar, InputConverter.GetModifiers(keyboard));
		}

        void Keyboard_KeyDown(IKeyboard keyboard, Key key, int value)
        {
            game?.KeyDown(InputConverter.Convert(key), InputConverter.GetModifiers(keyboard));
		}

        void Keyboard_KeyUp(IKeyboard keyboard, Key key, int value)
        {
			game?.KeyUp(InputConverter.Convert(key), InputConverter.GetModifiers(keyboard));
		}

        void Mouse_MouseDown(IMouse mouse, MouseButton button)
        {
            game?.MouseDown
            (
                InputConverter.ConvertMousePosition(WindowToVirtualScreen(mouse.Position)),
                InputConverter.ConvertMouseButtons(button),
                InputConverter.GetModifiers(keyboard!)
            );
        }

        void Mouse_MouseUp(IMouse mouse, MouseButton button)
        {
			game?.MouseUp
			(
				InputConverter.ConvertMousePosition(WindowToVirtualScreen(mouse.Position)),
				InputConverter.ConvertMouseButtons(button),
				InputConverter.GetModifiers(keyboard!)
			);
		}

        void Mouse_MouseMove(IMouse mouse, MousePosition position)
        {
			game?.MouseMove
			(
				InputConverter.ConvertMousePosition(WindowToVirtualScreen(mouse.Position)),
				InputConverter.GetMouseButtons(mouse)
			);
		}

        void Mouse_Scroll(IMouse mouse, ScrollWheel wheelDelta)
        {
			game?.MouseWheel
			(
				InputConverter.ConvertMousePosition(WindowToVirtualScreen(mouse.Position)),
                wheelDelta.X, wheelDelta.Y,
				InputConverter.GetMouseButtons(mouse)
			);
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

            var platform = Window.GetWindowPlatform(false);

            window.Monitor = platform!.GetMainMonitor();
            window.Size = new WindowDimension(Game.Game.VirtualScreenWidth * 2, Game.Game.VirtualScreenHeight * 2);

            var gl = GL.GetApi(GLContext);
            gl.Viewport(new System.Drawing.Size(window.FramebufferSize.X, window.FramebufferSize.Y));
            gl.ClearColor(System.Drawing.Color.Black);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GLContext!.SwapBuffers();

            window.Center();

            renderer = new(this, new Size(Width, Height), new Size(Game.Game.VirtualScreenWidth, Game.Game.VirtualScreenHeight));

			//var fileSystem = FileSystem.FromOperatingSystemPath(@"D:\Projects\Amber\German\AmberfilesST");

            /*var assetProvider = new AssetProvider(fileSystem.AsReadOnly());

            // setup the layers
            LayerSetup.Run(assetProvider, renderer, out var uiGraphicIndexProvider,
                out var paletteIndexProvider, out var paletteColorProvider, out var fontInfoProvider);

            var audioOuput = new AudioOutput();

			game = new Game.Game(renderer, assetProvider, audioOuput, uiGraphicIndexProvider,
                paletteIndexProvider, paletteColorProvider, fontInfoProvider, QueryPressedKeys);*/

            game = new Game.Game(new GameData.GameData(@"D:\Projects\Amber\AmberIsland\assets"), renderer, QueryPressedKeys);
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
            gameVersion = $"AmberIsland v{version.Major}.{version.Minor}.{version.Build}";
            var videoMode = new VideoMode(60);
            var options = new WindowOptions(true, new WindowDimension(100, 100),
                new WindowDimension(Width, Height), 60.0, 120.0, api, gameVersion,
                WindowState.Normal, WindowBorder.Fixed, true, false, videoMode, 24);
            options.WindowClass = "AmberIsland";

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
