﻿using System;
using System.IO;
using SadRogue.Primitives;

namespace SadConsole
{
    /// <summary>
    /// The MonoGame implementation of the SadConsole Game Host.
    /// </summary>
    public class Game : GameHost
    {
        /// <summary>
        /// Temporary variable to store the screen width before going full screen.
        /// </summary>
        protected int _preFullScreenWidth;

        /// <summary>
        /// Temporary variable to store the screen height before going full screen.
        /// </summary>
        protected int _preFullScreenHeight;

        /// <summary>
        /// Temporary variable to store the state of the <see cref="Settings.ResizeMode"/> when it's set to None, before going full screen.
        /// </summary>
        protected bool _handleResizeNone;

        /// <summary>
        /// When <see langword="true"/>, forces the <see cref="OpenStream"/> method to use <code>TitalContainer</code> when creating a stream to read a file.
        /// </summary>
        public bool UseTitleContainer { get; set; } = true;

        /// <summary>
        /// The <see cref="Microsoft.Xna.Framework.Game"/> instance.
        /// </summary>
        public Host.Game MonoGameInstance { get; protected set; }

        /// <summary>
        /// Strongly typed version of <see cref="GameHost.Instance"/>.
        /// </summary>
        public new static Game Instance
        {
            get => (Game)GameHost.Instance;
            protected set => GameHost.Instance = value;
        }

        internal string _font;

        /// <summary>
        /// Creates an instance of the class.
        /// </summary>
        protected Game() { }

        /// <summary>
        /// Creates a new game and assigns it to the <see cref="MonoGameInstance"/> property.
        /// </summary>
        /// <param name="cellCountX"></param>
        /// <param name="cellCountY"></param>
        /// <param name="font"></param>
        /// <param name="monogameCtorCallback"></param>
        public static void Create(int cellCountX, int cellCountY, string font = "", Action<Host.Game> monogameCtorCallback = null)
        {
            var game = new Game();
            game.ScreenCellsX = cellCountX;
            game.ScreenCellsY = cellCountY;
            game._font = font;

            Instance = game;
            game.MonoGameInstance = new Host.Game(monogameCtorCallback, game.MonoGameInit);
        }

        /// <summary>
        /// Method called by the <see cref="Host.Game"/> class for initializing SadConsole specifics. Called prior to <see cref="Host.Game.ResetRendering"/>.
        /// </summary>
        /// <param name="game">The game instance.</param>
        protected void MonoGameInit(Host.Game game)
        {
            LoadDefaultFonts(_font);

            MonoGameInstance.ResizeGraphicsDeviceManager(DefaultFont.GetFontSize(DefaultFontSize).ToMonoPoint(), ScreenCellsX, ScreenCellsY, 0, 0);
            
            SetRenderer(Renderers.Constants.RendererNames.Default, typeof(Renderers.ScreenSurfaceRenderer));

            SetRendererStep(Renderers.Constants.RenderStepNames.ControlHost, typeof(Renderers.ControlHostRenderStep));
            SetRendererStep(Renderers.Constants.RenderStepNames.Cursor, typeof(Renderers.CursorRenderStep));
            SetRendererStep(Renderers.Constants.RenderStepNames.EntityRenderer, typeof(Renderers.EntityLiteRenderStep));
            SetRendererStep(Renderers.Constants.RenderStepNames.Output, typeof(Renderers.OutputSurfaceRenderStep));
            SetRendererStep(Renderers.Constants.RenderStepNames.Surface, typeof(Renderers.SurfaceRenderStep));
            SetRendererStep(Renderers.Constants.RenderStepNames.Tint, typeof(Renderers.TintSurfaceRenderStep));
            SetRendererStep(Renderers.Constants.RenderStepNames.Window, typeof(Renderers.WindowRenderStep));

            LoadMappedColors();

            if (Settings.CreateStartingConsole)
            {
                StartingConsole = new Console(ScreenCellsX, ScreenCellsY);
                StartingConsole.IsFocused = true;
                Screen = StartingConsole;
            }
            else
                Screen = new ScreenObject() { IsFocused = true };

            OnStart?.Invoke();
            SplashScreens.SplashScreenManager.CheckRun();
        }

        /// <inheritdoc/>
        public override void Run()
        {
            MonoGameInstance.Run();
            OnEnd?.Invoke();
            MonoGameInstance.Dispose();
        }

        /// <inheritdoc/>
        public override ITexture GetTexture(string resourcePath) =>
            new Host.GameTexture(resourcePath);

        /// <inheritdoc/>
        public override ITexture GetTexture(Stream textureStream) =>
            new Host.GameTexture(textureStream);

        /// <inheritdoc/>
        public override SadConsole.Input.IKeyboardState GetKeyboardState() =>
            new Host.Keyboard();

        /// <inheritdoc/>
        public override SadConsole.Input.IMouseState GetMouseState() =>
            new Host.Mouse();


        /// <summary>
        /// Opens a read-only stream with MonoGame.
        /// </summary>
        /// <param name="file">The file to open.</param>
        /// <param name="mode">Unused by monogame.</param>
        /// <param name="access">Unused by monogame.</param>
        /// <returns>The stream.</returns>
        public override Stream OpenStream(string file, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read)
        {
            if (mode == FileMode.Create || mode == FileMode.CreateNew || mode == FileMode.OpenOrCreate)
                return System.IO.File.OpenWrite(file);

            return UseTitleContainer ? Microsoft.Xna.Framework.TitleContainer.OpenStream(file) : File.OpenRead(file);
        }

        /// <summary>
        /// Toggles between windowed and fullscreen rendering for SadConsole.
        /// </summary>
        public void ToggleFullScreen()
        {
            Host.Global.GraphicsDeviceManager.ApplyChanges();

            // Coming back from fullscreen
            if (Host.Global.GraphicsDeviceManager.IsFullScreen)
            {
                Host.Global.GraphicsDeviceManager.IsFullScreen = !Host.Global.GraphicsDeviceManager.IsFullScreen;

                Host.Global.GraphicsDeviceManager.PreferredBackBufferWidth = _preFullScreenWidth;
                Host.Global.GraphicsDeviceManager.PreferredBackBufferHeight = _preFullScreenHeight;
                Host.Global.GraphicsDeviceManager.ApplyChanges();
            }

            // Going full screen
            else
            {
                _preFullScreenWidth = Host.Global.GraphicsDevice.PresentationParameters.BackBufferWidth;
                _preFullScreenHeight = Host.Global.GraphicsDevice.PresentationParameters.BackBufferHeight;

                if (Settings.ResizeMode == Settings.WindowResizeOptions.None)
                {
                    _handleResizeNone = true;
                    Settings.ResizeMode = Settings.WindowResizeOptions.Scale;
                }

                Host.Global.GraphicsDeviceManager.PreferredBackBufferWidth = Host.Global.GraphicsDevice.Adapter.CurrentDisplayMode.Width;
                Host.Global.GraphicsDeviceManager.PreferredBackBufferHeight = Host.Global.GraphicsDevice.Adapter.CurrentDisplayMode.Height;

                Host.Global.GraphicsDeviceManager.IsFullScreen = !Host.Global.GraphicsDeviceManager.IsFullScreen;
                Host.Global.GraphicsDeviceManager.ApplyChanges();

                if (_handleResizeNone)
                {
                    _handleResizeNone = false;
                    Settings.ResizeMode = Settings.WindowResizeOptions.None;
                }
            }

            Instance.MonoGameInstance.ResetRendering();
        }
        
        /// <inheritdoc/>
        public override void ResizeWindow(int width, int height)
        {
            Host.Global.GraphicsDeviceManager.PreferredBackBufferWidth = width;
            Host.Global.GraphicsDeviceManager.PreferredBackBufferHeight = height;
            Host.Global.GraphicsDeviceManager.ApplyChanges();

            Instance.MonoGameInstance.ResetRendering();
        }

        internal void InvokeFrameDraw() =>
            OnFrameRender();

        internal void InvokeFrameUpdate() =>
            OnFrameUpdate();
    }
}
