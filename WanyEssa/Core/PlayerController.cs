using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using WanyEssa.Graphics;
using WanyEssa.Physics;

namespace WanyEssa.Core
{
    public class PlayerController
    {
        private Camera _camera;
        private PhysicsBody _physicsBody;
        private PhysicsWorld _physicsWorld;
        private Weapon? _currentWeapon = null;
        private List<Weapon> _weapons;
        private int _currentWeaponIndex;
        private float _moveSpeed;
        private float _runSpeed;
        private float _jumpForce;
        private bool _isSprinting;
        private bool _isOnGround;
        private float _mouseSensitivity;
        private bool _invertMouse;
        private bool _isCursorCaptured;
        private float _groundCheckDistance;
        
        public Camera Camera => _camera;
        public PhysicsBody PhysicsBody => _physicsBody;
        public Weapon? CurrentWeapon => _currentWeapon;
        public bool IsOnGround => _isOnGround;
        public bool IsSprinting => _isSprinting;
        public float MoveSpeed => _isSprinting ? _runSpeed : _moveSpeed;
        
        public PlayerController(int windowWidth, int windowHeight, PhysicsWorld physicsWorld, Vector3 position = default(Vector3))
        {
            _camera = new Camera(windowWidth, windowHeight, position == default(Vector3) ? new Vector3(0, 0, 0) : position);
            _physicsBody = new PhysicsBody(position == default(Vector3) ? new Vector3(0, 0, 0) : position, 1.0f, false);
            _physicsWorld = physicsWorld;
            _physicsWorld.AddBody(_physicsBody);
            _weapons = [];
            _currentWeaponIndex = -1;
            _moveSpeed = 5.0f;
            _runSpeed = 8.0f;
            _jumpForce = 500.0f;
            _isSprinting = false;
            _isOnGround = false;
            _mouseSensitivity = 0.1f;
            _invertMouse = false;
            _isCursorCaptured = false;
            _groundCheckDistance = 0.1f;
        }
        
        public void Initialize(GameWindow window)
        {
            // Capture cursor
            window.CursorState = CursorState.Grabbed;
            _isCursorCaptured = true;
        }
        
        public void Update(float deltaTime, GameWindow window)
        {
            // Handle input
            HandleInput(deltaTime, window);
            
            // Check if on ground
            CheckGround();
            
            // Update camera position to match physics body
            _camera.Position = _physicsBody.Position;
            
            // Update current weapon
            if (_currentWeapon != null)
            {
                _currentWeapon.Update(deltaTime);
                
                // Position weapon relative to camera
                if (_currentWeapon.IsAiming)
                {
                    _currentWeapon.Position = _camera.Position + _camera.Forward * 0.5f - _camera.Up * 0.2f;
                    _currentWeapon.Rotation = new Vector3(_camera.Pitch, _camera.Yaw, 0);
                }
                else
                {
                    _currentWeapon.Position = _camera.Position + _camera.Forward * 0.8f + _camera.Right * 0.3f - _camera.Up * 0.4f;
                    _currentWeapon.Rotation = new Vector3(_camera.Pitch + 10, _camera.Yaw, 20);
                }
            }
        }
        
        private void CheckGround()
        {
            // Simple ground check using raycast
            Vector3 rayOrigin = _physicsBody.Position;
            Vector3 rayDirection = new Vector3(0, -1, 0); // Down
            float rayLength = _groundCheckDistance + 0.1f;
            
            // For this example, we'll just check against a flat ground plane at y=0
            // In a real game, you would check against actual geometry
            if (_physicsBody.Position.Y <= 0.0f)
            {
                _isOnGround = true;
                _physicsBody.Position = new Vector3(_physicsBody.Position.X, 0.0f, _physicsBody.Position.Z);
                _physicsBody.Velocity = new Vector3(_physicsBody.Velocity.X, 0.0f, _physicsBody.Velocity.Z);
            }
            else
            {
                _isOnGround = false;
            }
        }
        
        private void HandleInput(float deltaTime, GameWindow window)
        {
            var keyboardState = window.KeyboardState;
            var mouseState = window.MouseState;
            
            // Handle mouse input for camera look
            float mouseX = mouseState.Delta.X;
            float mouseY = mouseState.Delta.Y;
            
            if (_isCursorCaptured)
            {
                float sensitivity = _mouseSensitivity;
                float invert = _invertMouse ? -1 : 1;
                _camera.MouseLook(mouseX * sensitivity, mouseY * sensitivity * invert);
            }
            
            // Handle keyboard input for movement
            float speed = _isSprinting ? _runSpeed : _moveSpeed;
            float moveDistance = speed * deltaTime;
            
            if (keyboardState.IsKeyDown(Keys.W))
            {
                _physicsBody.ApplyForce(_camera.Forward * moveDistance * 100);
            }
            if (keyboardState.IsKeyDown(Keys.S))
            {
                _physicsBody.ApplyForce(-_camera.Forward * moveDistance * 100);
            }
            if (keyboardState.IsKeyDown(Keys.A))
            {
                _physicsBody.ApplyForce(-_camera.Right * moveDistance * 100);
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                _physicsBody.ApplyForce(_camera.Right * moveDistance * 100);
            }
            
            // Handle sprinting
            if (keyboardState.IsKeyDown(Keys.LeftShift))
            {
                _isSprinting = true;
            }
            else
            {
                _isSprinting = false;
            }
            
            // Handle jumping
            if (keyboardState.IsKeyPressed(Keys.Space) && _isOnGround)
            {
                _physicsBody.ApplyForce(new Vector3(0, _jumpForce, 0));
                _isOnGround = false;
            }
            
            // Handle weapon switching
            if (keyboardState.IsKeyPressed(Keys.D1))
            {
                SwitchWeapon(0);
            }
            if (keyboardState.IsKeyPressed(Keys.D2))
            {
                SwitchWeapon(1);
            }
            if (keyboardState.IsKeyPressed(Keys.D3))
            {
                SwitchWeapon(2);
            }
            if (keyboardState.IsKeyPressed(Keys.D4))
            {
                SwitchWeapon(3);
            }
            
            // Handle weapon firing
            if (_currentWeapon != null)
            {
                if (mouseState.IsButtonDown(MouseButton.Left))
                {
                    if (_currentWeapon.IsAutomatic)
                    {
                        FireWeapon();
                    }
                }
                if (mouseState.IsButtonPressed(MouseButton.Left))
                {
                    if (!_currentWeapon.IsAutomatic)
                    {
                        FireWeapon();
                    }
                }
                
                // Handle aiming
                if (mouseState.IsButtonDown(MouseButton.Right))
                {
                    _currentWeapon.SetAiming(true);
                }
                else
                {
                    _currentWeapon.SetAiming(false);
                }
                
                // Handle reloading
                if (keyboardState.IsKeyPressed(Keys.R))
                {
                    _currentWeapon.Reload();
                }
            }
            
            // Handle cursor toggle
            if (keyboardState.IsKeyPressed(Keys.Escape))
            {
                _isCursorCaptured = !_isCursorCaptured;
                window.CursorState = _isCursorCaptured ? CursorState.Grabbed : CursorState.Normal;
            }
        }
        
        private void FireWeapon()
        {
            if (_currentWeapon != null && _currentWeapon.CanFire)
            {
                Vector3 hitPoint;
                bool fired = _currentWeapon.Fire(_camera.Position + _camera.Forward * 0.5f, _camera.Forward, out hitPoint);
                
                if (fired)
                {
                    // Apply recoil
                    _camera.Pitch -= _currentWeapon.Recoil;
                    
                    // Here you would add hit detection, particle effects, sound, etc.
                }
            }
        }
        
        public void AddWeapon(Weapon weapon)
        {
            _weapons.Add(weapon);
            if (_currentWeapon == null)
            {
                _currentWeapon = weapon;
                _currentWeaponIndex = _weapons.Count - 1;
            }
        }
        
        public void SwitchWeapon(int index)
        {
            if (index >= 0 && index < _weapons.Count)
            {
                _currentWeaponIndex = index;
                _currentWeapon = _weapons[index];
            }
        }
        
        public void SwitchToNextWeapon()
        {
            if (_weapons.Count > 0)
            {
                _currentWeaponIndex = (_currentWeaponIndex + 1) % _weapons.Count;
                _currentWeapon = _weapons[_currentWeaponIndex];
            }
        }
        
        public void SwitchToPreviousWeapon()
        {
            if (_weapons.Count > 0)
            {
                _currentWeaponIndex = (_currentWeaponIndex - 1 + _weapons.Count) % _weapons.Count;
                _currentWeapon = _weapons[_currentWeaponIndex];
            }
        }
        
        public void Draw(Renderer renderer)
        {
            if (_currentWeapon != null)
            {
                _currentWeapon.Draw(renderer, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            }
        }
    }
}