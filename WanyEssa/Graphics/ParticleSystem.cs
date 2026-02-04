using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace WanyEssa.Graphics
{
    public class Particle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Acceleration;
        public Vector4 Color;
        public Vector4 EndColor;
        public float Size;
        public float LifeTime;
        public float MaxLifeTime;
        public bool IsAlive;
        public float Rotation;
        public float RotationSpeed;
        
        public Particle(Vector3 position, Vector3 velocity, Vector3 acceleration, Vector4 color, Vector4 endColor, float size, float lifeTime)
        {
            Position = position;
            Velocity = velocity;
            Acceleration = acceleration;
            Color = color;
            EndColor = endColor;
            Size = size;
            LifeTime = lifeTime;
            MaxLifeTime = lifeTime;
            IsAlive = true;
            Rotation = 0.0f;
            RotationSpeed = 0.0f;
        }
        
        public void Update(float deltaTime)
        {
            if (!IsAlive)
                return;
            
            LifeTime -= deltaTime;
            if (LifeTime <= 0)
            {
                IsAlive = false;
                return;
            }
            
            Velocity += Acceleration * deltaTime;
            Position += Velocity * deltaTime;
            Rotation += RotationSpeed * deltaTime;
            
            // Interpolate color
            float t = 1.0f - (LifeTime / MaxLifeTime);
            Color = Vector4.Lerp(Color, EndColor, t);
        }
    }
    
    public class ParticleSystem
    {
        private List<Particle> _particles = [];
        private int _maxParticles;
        private bool _isEmitting;
        private float _emitRate;
        private float _emitTimer;
        private Vector3 _emitPosition;
        private Vector3 _emitDirection;
        private float _emitSpread;
        private Vector3 _particleVelocity;
        private float _velocityVariation;
        private Vector3 _particleAcceleration;
        private Vector4 _particleColor;
        private Vector4 _particleEndColor;
        private float _particleSize;
        private float _sizeVariation;
        private float _particleLifeTime;
        private float _lifeTimeVariation;
        
        public bool IsEmitting => _isEmitting;
        public int ActiveParticles => _particles.Count;
        
        public ParticleSystem(int maxParticles = 1000)
        {
            _particles = [];
            _maxParticles = maxParticles;
            _isEmitting = false;
            _emitRate = 100.0f; // Particles per second
            _emitTimer = 0.0f;
            _emitPosition = Vector3.Zero;
            _emitDirection = new Vector3(0, 0, 1); // Forward
            _emitSpread = 15.0f; // Degrees
            _particleVelocity = new Vector3(0, 1, 0);
            _velocityVariation = 0.5f;
            _particleAcceleration = new Vector3(0, -9.81f, 0); // Gravity
            _particleColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f); // White
            _particleEndColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f); // Black
            _particleSize = 0.1f;
            _sizeVariation = 0.05f;
            _particleLifeTime = 1.0f;
            _lifeTimeVariation = 0.5f;
        }
        
        public void StartEmit(Vector3 position, Vector3 direction = default(Vector3))
        {
            _isEmitting = true;
            _emitPosition = position;
            if (direction != default(Vector3))
            {
                _emitDirection = direction;
            }
        }
        
        public void StopEmit()
        {
            _isEmitting = false;
        }
        
        public void Emit(int count)
        {
            for (int i = 0; i < count; i++)
            {
                CreateParticle();
            }
        }
        
        public void Update(float deltaTime)
        {
            // Emit particles if enabled
            if (_isEmitting)
            {
                _emitTimer += deltaTime;
                float emitInterval = 1.0f / _emitRate;
                
                while (_emitTimer >= emitInterval && _particles.Count < _maxParticles)
                {
                    CreateParticle();
                    _emitTimer -= emitInterval;
                }
            }
            
            // Update particles
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                Particle particle = _particles[i];
                particle.Update(deltaTime);
                
                if (!particle.IsAlive)
                {
                    _particles.RemoveAt(i);
                }
            }
        }
        
        public void Draw(Renderer renderer)
        {
            foreach (Particle particle in _particles)
            {
                if (particle.IsAlive)
                {
                    // In a real implementation, you would draw the particle here
                    // For example, as a billboarded quad or sprite
                    // For now, we'll just draw a small circle
                    renderer.DrawCircle(new Vector3(particle.Position.X, particle.Position.Y, 0.0f), particle.Size, particle.Color);
                }
            }
        }
        
        private void CreateParticle()
        {
            if (_particles.Count >= _maxParticles)
                return;
            
            // Randomize particle properties
            Vector3 direction = RandomDirection(_emitDirection, _emitSpread);
            Vector3 velocity = _particleVelocity + direction * (_particleVelocity.Length * _velocityVariation * (float)(new Random().NextDouble() * 2.0f - 1.0f));
            float size = _particleSize + (float)(new Random().NextDouble() * _sizeVariation * 2.0f - _sizeVariation);
            float lifeTime = _particleLifeTime + (float)(new Random().NextDouble() * _lifeTimeVariation * 2.0f - _lifeTimeVariation);
            
            Particle particle = new(
                _emitPosition,
                velocity,
                _particleAcceleration,
                _particleColor,
                _particleEndColor,
                size,
                lifeTime
            );
            
            particle.RotationSpeed = (float)(new Random().NextDouble() * 360.0f - 180.0f);
            
            _particles.Add(particle);
        }
        
        private Vector3 RandomDirection(Vector3 direction, float spread)
        {
            // Simple implementation for random direction within spread
            float angle = (float)(new Random().NextDouble() * Math.PI * 2.0f);
            float spreadRad = spread * (float)Math.PI / 180.0f;
            float magnitude = (float)(new Random().NextDouble() * Math.Sin(spreadRad));
            
            Vector3 perpendicular = Vector3.Cross(direction, new Vector3(0, 1, 0));
            if (perpendicular.LengthSquared < 0.01f)
            {
                perpendicular = Vector3.Cross(direction, new Vector3(1, 0, 0));
            }
            perpendicular.Normalize();
            
            Vector3 tangent = Vector3.Cross(direction, perpendicular);
            tangent.Normalize();
            
            Vector3 offset = perpendicular * (float)Math.Sin(angle) * magnitude + tangent * (float)Math.Cos(angle) * magnitude;
            return (direction + offset).Normalized();
        }
        
        public void Clear()
        {
            _particles.Clear();
        }
        
        // Setters for particle system properties
        public void SetEmitRate(float rate)
        {
            _emitRate = rate;
        }
        
        public void SetEmitSpread(float spread)
        {
            _emitSpread = spread;
        }
        
        public void SetParticleVelocity(Vector3 velocity)
        {
            _particleVelocity = velocity;
        }
        
        public void SetVelocityVariation(float variation)
        {
            _velocityVariation = variation;
        }
        
        public void SetParticleAcceleration(Vector3 acceleration)
        {
            _particleAcceleration = acceleration;
        }
        
        public void SetParticleColor(Vector4 color, Vector4 endColor)
        {
            _particleColor = color;
            _particleEndColor = endColor;
        }
        
        public void SetParticleSize(float size, float variation)
        {
            _particleSize = size;
            _sizeVariation = variation;
        }
        
        public void SetParticleLifeTime(float lifeTime, float variation)
        {
            _particleLifeTime = lifeTime;
            _lifeTimeVariation = variation;
        }
    }
    
    public static class ParticlePresets
    {
        public static ParticleSystem CreateMuzzleFlash(Vector3 position, Vector3 direction)
        {
            ParticleSystem system = new ParticleSystem(100);
            system.SetEmitRate(500.0f);
            system.SetEmitSpread(30.0f);
            system.SetParticleVelocity(direction * 10.0f);
            system.SetVelocityVariation(0.5f);
            system.SetParticleAcceleration(new Vector3(0, 0, 0));
            system.SetParticleColor(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), new Vector4(1.0f, 0.2f, 0.0f, 0.0f));
            system.SetParticleSize(0.1f, 0.05f);
            system.SetParticleLifeTime(0.1f, 0.05f);
            system.StartEmit(position, direction);
            return system;
        }
        
        public static ParticleSystem CreateImpactEffect(Vector3 position, Vector3 normal)
        {
            ParticleSystem system = new ParticleSystem(200);
            system.SetEmitRate(1000.0f);
            system.SetEmitSpread(60.0f);
            system.SetParticleVelocity(normal * 5.0f);
            system.SetVelocityVariation(0.8f);
            system.SetParticleAcceleration(new Vector3(0, -9.81f, 0));
            system.SetParticleColor(new Vector4(0.8f, 0.8f, 0.8f, 1.0f), new Vector4(0.5f, 0.5f, 0.5f, 0.0f));
            system.SetParticleSize(0.05f, 0.02f);
            system.SetParticleLifeTime(0.5f, 0.2f);
            system.StartEmit(position, normal);
            return system;
        }
        
        public static ParticleSystem CreateExplosionEffect(Vector3 position)
        {
            ParticleSystem system = new ParticleSystem(500);
            system.SetEmitRate(2000.0f);
            system.SetEmitSpread(180.0f);
            system.SetParticleVelocity(new Vector3(0, 10, 0));
            system.SetVelocityVariation(1.0f);
            system.SetParticleAcceleration(new Vector3(0, -9.81f, 0));
            system.SetParticleColor(new Vector4(1.0f, 0.3f, 0.0f, 1.0f), new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
            system.SetParticleSize(0.2f, 0.1f);
            system.SetParticleLifeTime(1.0f, 0.3f);
            system.StartEmit(position);
            return system;
        }
        
        public static ParticleSystem CreateSmokeEffect(Vector3 position)
        {
            ParticleSystem system = new ParticleSystem(300);
            system.SetEmitRate(50.0f);
            system.SetEmitSpread(45.0f);
            system.SetParticleVelocity(new Vector3(0, 2, 0));
            system.SetVelocityVariation(0.5f);
            system.SetParticleAcceleration(new Vector3(0, -2.0f, 0));
            system.SetParticleColor(new Vector4(0.2f, 0.2f, 0.2f, 0.8f), new Vector4(0.1f, 0.1f, 0.1f, 0.0f));
            system.SetParticleSize(0.2f, 0.1f);
            system.SetParticleLifeTime(2.0f, 0.5f);
            system.StartEmit(position);
            return system;
        }
    }
    
    public static class Vector4Extensions
    {
        public static Vector4 Lerp(Vector4 a, Vector4 b, float t)
        {
            t = Math.Clamp(t, 0.0f, 1.0f);
            return new Vector4(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t,
                a.W + (b.W - a.W) * t
            );
        }
    }
}