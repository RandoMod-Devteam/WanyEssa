using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using WanyEssa.Core;

namespace WanyEssa
{
    public class Game : GameWindow
    {
        private bool _isRunning;
        private float _deltaTime;
        private Core.Console _console;
        private MapEditor _mapEditor;
        private bool _showSettings;
        private bool _showMapEditor;
        
        public Core.Console Console => _console;
        public MapEditor MapEditor => _mapEditor;
        public bool ShowSettings => _showSettings;
        public bool ShowMapEditor => _showMapEditor;
        
        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            _isRunning = false;
            _deltaTime = 0.0f;
            _console = new Core.Console();
            _mapEditor = new MapEditor();
            _showSettings = false;
            _showMapEditor = false;
        }
        
        protected override void OnLoad()
        {
            base.OnLoad();
            
            Initialize();
            _isRunning = true;
        }
        
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            
            if (!_isRunning)
                return;
            
            // Handle global input
            HandleGlobalInput();
            
            // Update console
            _console.Update(this);
            
            // Update map editor if visible
            if (_showMapEditor)
            {
                _mapEditor.Update(this, _deltaTime);
            }
            
            _deltaTime = (float)args.Time;
            
            Update(_deltaTime);
        }
        
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            
            if (!_isRunning)
                return;
            
            Render();
            
            // Draw console
            _console.Draw();
            
            // Draw settings menu
            if (_showSettings)
            {
                DrawSettingsMenu();
            }
            
            // Draw map editor
            if (_showMapEditor)
            {
                _mapEditor.Draw();
            }
            
            SwapBuffers();
        }
        
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            
            OnWindowResize(Size.X, Size.Y);
        }
        
        protected override void OnUnload()
        {
            base.OnUnload();
            
            _isRunning = false;
            Cleanup();
        }
        
        private void HandleGlobalInput()
        {
            var keyboardState = KeyboardState;
            
            // Toggle console with ` key
            if (keyboardState.IsKeyPressed(Keys.GraveAccent))
            {
                _console.Toggle();
            }
            
            // Toggle settings with ESC key
            if (keyboardState.IsKeyPressed(Keys.Escape))
            {
                _showSettings = !_showSettings;
                // If showing settings, close console and map editor
                if (_showSettings)
                {
                    _showMapEditor = false;
                }
            }
            
            // Toggle map editor with M key
            if (keyboardState.IsKeyPressed(Keys.M))
            {
                _showMapEditor = !_showMapEditor;
                // If showing map editor, close settings
                if (_showMapEditor)
                {
                    _showSettings = false;
                }
            }
        }
        
        private void DrawSettingsMenu()
        {
            // Draw settings menu background
            GL.ClearColor(0.1f, 0.1f, 0.1f, 0.9f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            
            // In a real implementation, this would draw the settings menu
            // with options for MSAA, FSA, FPS Limit, console enabling
        }
        
        // Virtual methods for game implementation
        protected virtual void Initialize() {}
        protected virtual void Update(float deltaTime) {}
        protected virtual void Render() {}
        protected virtual void OnWindowResize(int width, int height) {}
        protected virtual void Cleanup() {}
        
        public float DeltaTime => _deltaTime;
        public bool IsRunning => _isRunning;
    }
}