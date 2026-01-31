using System;
using WanyEssa.Math;
using Matrix4 = OpenTK.Mathematics.Matrix4;
using MathHelper = OpenTK.Mathematics.MathHelper;

namespace WanyEssa.Graphics
{
    public class Camera
    {
        private WanyEssa.Math.Vector3 _position;
        private WanyEssa.Math.Vector3 _forward;
        private WanyEssa.Math.Vector3 _up;
        private WanyEssa.Math.Vector3 _right;
        private float _yaw;
        private float _pitch;
        private float _fov;
        private float _nearClip;
        private float _farClip;
        private Matrix4 _viewMatrix;
        private Matrix4 _projectionMatrix;
        private bool _viewMatrixDirty;
        private bool _projectionMatrixDirty;
        private int _windowWidth;
        private int _windowHeight;
        
        public WanyEssa.Math.Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                _viewMatrixDirty = true;
            }
        }
        
        public float Yaw
        {
            get => _yaw;
            set
            {
                _yaw = value;
                UpdateVectors();
            }
        }
        
        public float Pitch
        {
            get => _pitch;
            set
            {
                // Clamp pitch to avoid gimbal lock
                _pitch = System.Math.Clamp(value, -89.0f, 89.0f);
                UpdateVectors();
            }
        }
        
        public float Fov
        {
            get => _fov;
            set
            {
                _fov = System.Math.Clamp(value, 1.0f, 179.0f);
                _projectionMatrixDirty = true;
            }
        }
        
        public WanyEssa.Math.Vector3 Forward => _forward;
        public WanyEssa.Math.Vector3 Up => _up;
        public WanyEssa.Math.Vector3 Right => _right;
        public Matrix4 ViewMatrix => GetViewMatrix();
        public Matrix4 ProjectionMatrix => GetProjectionMatrix();
        
        public Camera(int windowWidth, int windowHeight, WanyEssa.Math.Vector3 position = default)
        {
            _windowWidth = windowWidth;
            _windowHeight = windowHeight;
            _position = position == default(WanyEssa.Math.Vector3) ? new WanyEssa.Math.Vector3(0, 0, 0) : position;
            _forward = WanyEssa.Math.Vector3.Forward;
            _up = WanyEssa.Math.Vector3.Up;
            _right = WanyEssa.Math.Vector3.Right;
            _yaw = 0.0f;
            _pitch = 0.0f;
            _fov = 75.0f;
            _nearClip = 0.1f;
            _farClip = 1000.0f;
            _viewMatrixDirty = true;
            _projectionMatrixDirty = true;
        }
        
        public void UpdateWindowSize(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
            _projectionMatrixDirty = true;
        }
        
        public void LookAt(WanyEssa.Math.Vector3 target)
        {
            _forward = (target - _position).Normalized;
            _right = WanyEssa.Math.Vector3.Cross(_forward, WanyEssa.Math.Vector3.Up).Normalized;
            _up = WanyEssa.Math.Vector3.Cross(_right, _forward).Normalized;
            
            // Calculate yaw and pitch from forward vector
            _yaw = (float)System.Math.Atan2(_forward.X, _forward.Z);
            _pitch = (float)System.Math.Asin(_forward.Y);
            
            _viewMatrixDirty = true;
        }
        
        public void MouseLook(float deltaX, float deltaY, float sensitivity = 0.1f)
        {
            _yaw += deltaX * sensitivity;
            _pitch -= deltaY * sensitivity;
            
            // Clamp pitch to avoid gimbal lock
            _pitch = System.Math.Clamp(_pitch, -89.0f, 89.0f);
            
            UpdateVectors();
        }
        
        private void UpdateVectors()
        {
            // Calculate new forward vector
            float yawRad = OpenTK.Mathematics.MathHelper.DegreesToRadians(_yaw);
            float pitchRad = OpenTK.Mathematics.MathHelper.DegreesToRadians(_pitch);
            
            _forward = new WanyEssa.Math.Vector3(
                (float)(System.Math.Cos(yawRad) * System.Math.Cos(pitchRad)),
                (float)System.Math.Sin(pitchRad),
                (float)(System.Math.Sin(yawRad) * System.Math.Cos(pitchRad))
            ).Normalized;
            
            // Calculate right and up vectors
            _right = WanyEssa.Math.Vector3.Cross(_forward, WanyEssa.Math.Vector3.Up).Normalized;
            _up = WanyEssa.Math.Vector3.Cross(_right, _forward).Normalized;
            
            _viewMatrixDirty = true;
        }
        
        public void MoveForward(float distance)
        {
            _position += _forward * distance;
            _viewMatrixDirty = true;
        }
        
        public void MoveBackward(float distance)
        {
            _position -= _forward * distance;
            _viewMatrixDirty = true;
        }
        
        public void MoveLeft(float distance)
        {
            _position -= _right * distance;
            _viewMatrixDirty = true;
        }
        
        public void MoveRight(float distance)
        {
            _position += _right * distance;
            _viewMatrixDirty = true;
        }
        
        public void MoveUp(float distance)
        {
            _position += _up * distance;
            _viewMatrixDirty = true;
        }
        
        public void MoveDown(float distance)
        {
            _position -= _up * distance;
            _viewMatrixDirty = true;
        }
        
        private Matrix4 GetViewMatrix()
        {
            if (_viewMatrixDirty)
            {
                _viewMatrix = Matrix4.LookAt(
                    _position.X, _position.Y, _position.Z,
                    _position.X + _forward.X, _position.Y + _forward.Y, _position.Z + _forward.Z,
                    _up.X, _up.Y, _up.Z
                );
                _viewMatrixDirty = false;
            }
            return _viewMatrix;
        }
        
        private Matrix4 GetProjectionMatrix()
        {
            if (_projectionMatrixDirty)
            {
                float aspectRatio = (float)_windowWidth / _windowHeight;
                _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
                    OpenTK.Mathematics.MathHelper.DegreesToRadians(_fov),
                    aspectRatio,
                    _nearClip,
                    _farClip
                );
                _projectionMatrixDirty = false;
            }
            return _projectionMatrix;
        }
        
        public void Update(float deltaTime)
        {
            // Update logic can be added here if needed
        }
    }
}