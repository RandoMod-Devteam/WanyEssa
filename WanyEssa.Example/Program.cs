using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using WanyEssa;
using WanyEssa.Math;
using WanyEssa.Physics;
using WanyEssa.Graphics;
using WanyEssa.Core;
using System;

namespace WanyEssa.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            // Load settings
            Settings.Instance.Load();
            
            var gameWindowSettings = new GameWindowSettings
            {
                UpdateFrequency = Settings.Instance.FpsLimit
            };
            
            var nativeWindowSettings = new NativeWindowSettings
            {
                ClientSize = new OpenTK.Mathematics.Vector2i(800, 600),
                Title = "WanyEssa Game Engine - Example",
                WindowBorder = WindowBorder.Resizable,
                API = ContextAPI.OpenGL,
                APIVersion = new Version(4, 6),
                Profile = ContextProfile.Core
            };
            
            using var game = new ExampleGame(gameWindowSettings, nativeWindowSettings);
            game.Run();
        }
    }
    
    class ExampleGame : Game
    {
        private PhysicsWorld _physicsWorld;
        private Player _player;
        private CircleCollider _playerCollider;
        private PhysicsBody _groundBody;
        private BoxCollider _groundCollider;
        private Renderer? _renderer = null;
        
        private float _playerRadius = 20.0f;
        private Vector3 _groundSize = new Vector3(800.0f, 40.0f, 1.0f);
        
        public ExampleGame(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            // Initialize physics world with default settings
            _physicsWorld = new PhysicsWorld();
            _physicsWorld.Gravity = new Vector3(0.0f, -9.8f * 100.0f, 0.0f);
            
            // Create player
            _player = new Player(new Vector3(400.0f, 300.0f, 0.0f), 1.0f);
            _playerCollider = new CircleCollider(_player, _playerRadius);
            
            // Create ground
            _groundBody = new PhysicsBody(new Vector3(400.0f, _groundSize.Y * 0.5f, 0.0f), 1.0f, true);
            _groundCollider = new BoxCollider(_groundBody, _groundSize);
        }
        
        protected override void Initialize()
        {
            base.Initialize();
            
            System.Console.WriteLine("WanyEssa Example Game Initialized!");
            System.Console.WriteLine("Controls:");
            System.Console.WriteLine("A/D - Move left/right");
            System.Console.WriteLine("Space - Jump");
            System.Console.WriteLine("ESC - Open settings");
            System.Console.WriteLine("M - Toggle map editor");
            System.Console.WriteLine("` - Open console");
            
            // Add physics objects to world
            _physicsWorld.AddBody(_player);
            _physicsWorld.AddBody(_groundBody);
            
            // Initialize renderer
            _renderer = new Renderer(Size.X, Size.Y);
        }
        
        protected override void Update(float deltaTime)
        {
            // Update player input and state
            _player.Update(this, deltaTime);
            
            // Update physics
            _physicsWorld.Update(deltaTime);
            
            // Check if player is on ground
            float groundY = _groundBody.Position.Y + _groundSize.Y * 0.5f;
            _player.OnCollisionWithGround(groundY, _playerRadius * 2.0f);
            
            // Keep player within window bounds
            if (_player.Position.X - _playerRadius < 0)
            {
                _player.Position = new Vector3(_playerRadius, _player.Position.Y, 0.0f);
                _player.Velocity = new Vector3(0.0f, _player.Velocity.Y, 0.0f);
            }
            else if (_player.Position.X + _playerRadius > Size.X)
            {
                _player.Position = new Vector3(Size.X - _playerRadius, _player.Position.Y, 0.0f);
                _player.Velocity = new Vector3(0.0f, _player.Velocity.Y, 0.0f);
            }
        }
        
        protected override void Render()
        {
            // Clear the screen
            OpenTK.Graphics.OpenGL4.GL.Clear(OpenTK.Graphics.OpenGL4.ClearBufferMask.ColorBufferBit);
            
            // Begin rendering
            _renderer?.Begin();
            
            // Draw ground
            _renderer?.DrawRectangle(new Vector3(_groundBody.Position.X, _groundBody.Position.Y, 0.0f), new Vector2(_groundSize.X, _groundSize.Y), Color.Green);
            
            // Draw player with appropriate color based on state
            Color playerColor = Color.Blue;
            switch (_player.State)
            {
                case Player.PlayerState.Jumping:
                    playerColor = Color.Yellow;
                    break;
                case Player.PlayerState.Falling:
                    playerColor = Color.Orange;
                    break;
                case Player.PlayerState.Running:
                    playerColor = Color.Green;
                    break;
                default:
                    playerColor = Color.Blue;
                    break;
            }
            
            _renderer?.DrawCircle(new Vector3(_player.Position.X, _player.Position.Y, 0.0f), _playerRadius, playerColor);
            
            // End rendering
            _renderer?.End();
        }
        
        protected override void OnWindowResize(int width, int height)
        {
            base.OnWindowResize(width, height);
            
            System.Console.WriteLine($"Window resized to: {width}x{height}");
            
            // Update viewport
            OpenTK.Graphics.OpenGL4.GL.Viewport(0, 0, width, height);
            
            // Update renderer projection
            _renderer?.SetProjection(width, height);
            
            // Update ground position to stay at bottom
            _groundBody.Position = new Vector3(width * 0.5f, _groundSize.Y * 0.5f, 0.0f);
            _groundSize.X = width;
        }
        
        protected override void Cleanup()
        {
            System.Console.WriteLine("WanyEssa Example Game Cleaned Up!");
            
            // Save settings
            Settings.Instance.Save();
            
            // Dispose renderer
            _renderer?.Dispose();
        }
    }
}