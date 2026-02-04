using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Matrix4 = OpenTK.Mathematics.Matrix4;
using Vector2 = OpenTK.Mathematics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace WanyEssa.Graphics
{
    public class GPUParticleSystem
    {
        private int _computeShader;
        private int _particleShader;
        private int _particleBuffer;
        private int _vertexArray;
        private readonly int _maxParticles;
        private int _activeParticles;
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
        
        // Uniform locations
        private int _deltaTimeLocation;
        private int _emitPositionLocation;
        private int _emitDirectionLocation;
        private int _emitSpreadLocation;
        private int _particleVelocityLocation;
        private int _velocityVariationLocation;
        private int _particleAccelerationLocation;
        private int _particleColorLocation;
        private int _particleEndColorLocation;
        private int _particleSizeLocation;
        private int _sizeVariationLocation;
        private int _particleLifeTimeLocation;
        private int _lifeTimeVariationLocation;
        private int _maxParticlesLocation;
        private int _activeParticlesLocation;
        private int _emitCountLocation;
        
        // Particle data structure
        // 注意：这些字段由GPU计算着色器读写，C#端仅分配内存
#pragma warning disable CS0649
        private struct ParticleData
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
        }
#pragma warning restore CS0649
        
        public bool IsEmitting => _isEmitting;
        public int ActiveParticles => _activeParticles;
        
        public GPUParticleSystem(int maxParticles = 10000)
        {
            _maxParticles = maxParticles;
            _activeParticles = 0;
            _isEmitting = false;
            _emitRate = 100.0f;
            _emitTimer = 0.0f;
            _emitPosition = Vector3.Zero;
            _emitDirection = new Vector3(0, 0, -1); // Forward
            _emitSpread = 15.0f;
            _particleVelocity = new Vector3(0, 1, 0);
            _velocityVariation = 0.5f;
            _particleAcceleration = new Vector3(0, -9.81f, 0);
            _particleColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f); // White
            _particleEndColor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f); // Black (transparent)
            _particleSize = 0.1f;
            _sizeVariation = 0.05f;
            _particleLifeTime = 1.0f;
            _lifeTimeVariation = 0.5f;
            
            InitializeShaders();
            InitializeBuffers();
        }
        
        private void InitializeShaders()
        {
            // Compute shader for particle updates
            string computeShaderSource = @"
                #version 430 core
                
                layout(local_size_x = 128) in;
                
                layout(std430, binding = 0) buffer ParticleBuffer
                {
                    struct Particle
                    {
                        vec3 position;
                        vec3 velocity;
                        vec3 acceleration;
                        vec4 color;
                        vec4 endColor;
                        float size;
                        float lifeTime;
                        float maxLifeTime;
                        bool isAlive;
                        float rotation;
                        float rotationSpeed;
                    } particles[];
                };
                
                uniform float deltaTime;
                uniform vec3 emitPosition;
                uniform vec3 emitDirection;
                uniform float emitSpread;
                uniform vec3 particleVelocity;
                uniform float velocityVariation;
                uniform vec3 particleAcceleration;
                uniform vec4 particleColor;
                uniform vec4 particleEndColor;
                uniform float particleSize;
                uniform float sizeVariation;
                uniform float particleLifeTime;
                uniform float lifeTimeVariation;
                uniform int maxParticles;
                uniform int activeParticles;
                uniform int emitCount;
                
                // Random number generator
                float rand(vec2 co)
                {
                    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
                }
                
                // Random direction within spread
                vec3 randomDirection(vec3 direction, float spread)
                {
                    float angleX = rand(gl_GlobalInvocationID.xy) * spread * 3.14159 / 180.0;
                    float angleY = rand(gl_GlobalInvocationID.yx) * spread * 3.14159 / 180.0;
                    
                    mat3 rotX = mat3(
                        1.0, 0.0, 0.0,
                        0.0, cos(angleX), -sin(angleX),
                        0.0, sin(angleX), cos(angleX)
                    );
                    
                    mat3 rotY = mat3(
                        cos(angleY), 0.0, sin(angleY),
                        0.0, 1.0, 0.0,
                        -sin(angleY), 0.0, cos(angleY)
                    );
                    
                    return normalize(rotY * rotX * direction);
                }
                
                void main()
                {
                    int i = int(gl_GlobalInvocationID.x);
                    
                    // Emit new particles
                    if (i < emitCount && activeParticles + i < maxParticles)
                    {
                        int particleIndex = activeParticles + i;
                        Particle particle = particles[particleIndex];
                        
                        // Initialize particle
                        particle.position = emitPosition;
                        particle.velocity = particleVelocity + randomDirection(emitDirection, emitSpread) * length(particleVelocity) * velocityVariation * (rand(gl_GlobalInvocationID.xy) * 2.0 - 1.0);
                        particle.acceleration = particleAcceleration;
                        particle.color = particleColor;
                        particle.endColor = particleEndColor;
                        particle.size = particleSize + sizeVariation * (rand(gl_GlobalInvocationID.yx) * 2.0 - 1.0);
                        particle.lifeTime = particleLifeTime + lifeTimeVariation * (rand(gl_GlobalInvocationID.xy) * 2.0 - 1.0);
                        particle.maxLifeTime = particle.lifeTime;
                        particle.isAlive = true;
                        particle.rotation = 0.0;
                        particle.rotationSpeed = (rand(gl_GlobalInvocationID.xy) * 360.0 - 180.0) * 3.14159 / 180.0;
                        
                        particles[particleIndex] = particle;
                    }
                    
                    // Update existing particles
                    if (i < activeParticles + emitCount && i < maxParticles)
                    {
                        Particle particle = particles[i];
                        
                        if (particle.isAlive)
                        {
                            particle.lifeTime -= deltaTime;
                            
                            if (particle.lifeTime <= 0.0)
                            {
                                particle.isAlive = false;
                            }
                            else
                            {
                                // Update position and velocity
                                particle.velocity += particle.acceleration * deltaTime;
                                particle.position += particle.velocity * deltaTime;
                                particle.rotation += particle.rotationSpeed * deltaTime;
                                
                                // Interpolate color
                                float t = 1.0 - (particle.lifeTime / particle.maxLifeTime);
                                particle.color = mix(particle.color, particle.endColor, t);
                            }
                            
                            particles[i] = particle;
                        }
                    }
                }
            ";
            
            // Particle vertex shader
            string particleVertexShaderSource = @"
                #version 330 core
                
                uniform mat4 projection;
                uniform mat4 view;
                uniform vec3 cameraRight;
                uniform vec3 cameraUp;
                
                layout(std430, binding = 0) buffer ParticleBuffer
                {
                    struct Particle
                    {
                        vec3 position;
                        vec3 velocity;
                        vec3 acceleration;
                        vec4 color;
                        vec4 endColor;
                        float size;
                        float lifeTime;
                        float maxLifeTime;
                        bool isAlive;
                        float rotation;
                        float rotationSpeed;
                    } particles[];
                };
                
                in int particleIndex;
                in vec2 vertex;
                
                out vec4 fragColor;
                out float particleSize;
                
                void main()
                {
                    Particle particle = particles[particleIndex];
                    
                    if (!particle.isAlive)
                    {
                        gl_Position = vec4(0.0, 0.0, 2.0, 1.0);
                        return;
                    }
                    
                    fragColor = particle.color;
                    particleSize = particle.size;
                    
                    // Billboards: align quad to camera
                    vec3 cameraPosition = vec3(inverse(view)[3]);
                    vec3 toCamera = normalize(cameraPosition - particle.position);
                    vec3 right = normalize(cross(toCamera, vec3(0.0, 1.0, 0.0)));
                    vec3 up = normalize(cross(right, toCamera));
                    
                    // Calculate particle position
                    vec3 particlePosition = particle.position + right * vertex.x * particle.size + up * vertex.y * particle.size;
                    
                    gl_Position = projection * view * vec4(particlePosition, 1.0);
                }
            ";
            
            // Particle fragment shader
            string particleFragmentShaderSource = @"
                #version 330 core
                
                in vec4 fragColor;
                in float particleSize;
                
                out vec4 color;
                
                void main()
                {
                    // Simple circular particle
                    vec2 center = gl_PointCoord - 0.5;
                    float distance = length(center);
                    
                    if (distance > 0.5)
                        discard;
                    
                    color = fragColor;
                }
            ";
            
            // Create compute shader
            _computeShader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(_computeShader, computeShaderSource);
            GL.CompileShader(_computeShader);
            
            // Create particle shader program
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, particleVertexShaderSource);
            GL.CompileShader(vertexShader);
            
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, particleFragmentShaderSource);
            GL.CompileShader(fragmentShader);
            
            _particleShader = GL.CreateProgram();
            GL.AttachShader(_particleShader, vertexShader);
            GL.AttachShader(_particleShader, fragmentShader);
            GL.LinkProgram(_particleShader);
            
            // Clean up
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            
            // Get uniform locations
            _deltaTimeLocation = GL.GetUniformLocation(_computeShader, "deltaTime");
            _emitPositionLocation = GL.GetUniformLocation(_computeShader, "emitPosition");
            _emitDirectionLocation = GL.GetUniformLocation(_computeShader, "emitDirection");
            _emitSpreadLocation = GL.GetUniformLocation(_computeShader, "emitSpread");
            _particleVelocityLocation = GL.GetUniformLocation(_computeShader, "particleVelocity");
            _velocityVariationLocation = GL.GetUniformLocation(_computeShader, "velocityVariation");
            _particleAccelerationLocation = GL.GetUniformLocation(_computeShader, "particleAcceleration");
            _particleColorLocation = GL.GetUniformLocation(_computeShader, "particleColor");
            _particleEndColorLocation = GL.GetUniformLocation(_computeShader, "particleEndColor");
            _particleSizeLocation = GL.GetUniformLocation(_computeShader, "particleSize");
            _sizeVariationLocation = GL.GetUniformLocation(_computeShader, "sizeVariation");
            _particleLifeTimeLocation = GL.GetUniformLocation(_computeShader, "particleLifeTime");
            _lifeTimeVariationLocation = GL.GetUniformLocation(_computeShader, "lifeTimeVariation");
            _maxParticlesLocation = GL.GetUniformLocation(_computeShader, "maxParticles");
            _activeParticlesLocation = GL.GetUniformLocation(_computeShader, "activeParticles");
            _emitCountLocation = GL.GetUniformLocation(_computeShader, "emitCount");
        }
        
        private void InitializeBuffers()
        {
            // Create particle buffer
            _particleBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _particleBuffer);
            
            // Allocate buffer for max particles
            int particleSize = 16 * sizeof(float) + sizeof(bool); // Position (3) + Velocity (3) + Acceleration (3) + Color (4) + EndColor (4) + Size (1) + LifeTime (1) + MaxLifeTime (1) + IsAlive (1) + Rotation (1) + RotationSpeed (1)
            int bufferSize = _maxParticles * particleSize;
            GL.BufferData(BufferTarget.ShaderStorageBuffer, bufferSize, IntPtr.Zero, BufferUsage.DynamicDraw);
            GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _particleBuffer);
            
            // Create vertex array for particles
            _vertexArray = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArray);
            
            // Create vertex buffer for quad
            int vertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            
            // Quad vertices
            float[] quadVertices = {
                -1.0f,  1.0f,
                -1.0f, -1.0f,
                 1.0f, -1.0f,
                 1.0f,  1.0f
            };
            
            GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsage.StaticDraw);
            
            // Vertex attribute
            int vertexLocation = GL.GetAttribLocation(_particleShader, "vertex");
            GL.EnableVertexAttribArray((uint)vertexLocation);
            GL.VertexAttribPointer((uint)vertexLocation, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            
            GL.BindVertexArray(0);
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
            if (_activeParticles + count > _maxParticles)
            {
                count = _maxParticles - _activeParticles;
            }
            
            if (count > 0)
            {
                UpdateComputeShader(0.0f, count);
                _activeParticles += count;
            }
        }
        
        public void Update(float deltaTime)
        {
            int emitCount = 0;
            
            // Emit particles if enabled
            if (_isEmitting)
            {
                _emitTimer += deltaTime;
                float emitInterval = 1.0f / _emitRate;
                
                while (_emitTimer >= emitInterval && _activeParticles < _maxParticles)
                {
                    emitCount++;
                    _emitTimer -= emitInterval;
                }
            }
            
            // Update particles using compute shader
            if (emitCount > 0 || _activeParticles > 0)
            {
                UpdateComputeShader(deltaTime, emitCount);
                _activeParticles = Math.Min(_activeParticles + emitCount, _maxParticles);
            }
        }
        
        private void UpdateComputeShader(float deltaTime, int emitCount)
        {
            // Use compute shader
            GL.UseProgram(_computeShader);
            
            // Set uniforms
            GL.Uniform1f(_deltaTimeLocation, deltaTime);
            GL.Uniform3f(_emitPositionLocation, _emitPosition.X, _emitPosition.Y, _emitPosition.Z);
            GL.Uniform3f(_emitDirectionLocation, _emitDirection.X, _emitDirection.Y, _emitDirection.Z);
            GL.Uniform1f(_emitSpreadLocation, _emitSpread); 
            GL.Uniform3f(_particleVelocityLocation, _particleVelocity.X, _particleVelocity.Y, _particleVelocity.Z);
            GL.Uniform1f(_velocityVariationLocation, _velocityVariation);
            GL.Uniform3f(_particleAccelerationLocation, _particleAcceleration.X, _particleAcceleration.Y, _particleAcceleration.Z);
            GL.Uniform4f(_particleColorLocation, _particleColor.X, _particleColor.Y, _particleColor.Z, _particleColor.W);
            GL.Uniform4f(_particleEndColorLocation, _particleEndColor.X, _particleEndColor.Y, _particleEndColor.Z, _particleEndColor.W);
            GL.Uniform1f(_particleSizeLocation, _particleSize);
            GL.Uniform1f(_sizeVariationLocation, _sizeVariation);
            GL.Uniform1f(_particleLifeTimeLocation, _particleLifeTime);
            GL.Uniform1f(_lifeTimeVariationLocation, _lifeTimeVariation);   
            GL.Uniform1i(_maxParticlesLocation, _maxParticles);
            GL.Uniform1i(_activeParticlesLocation, _activeParticles);
            GL.Uniform1i(_emitCountLocation, emitCount);
            
            // Dispatch compute shader
            int workGroupSize = 128;
            int workGroups = (Math.Max(_activeParticles + emitCount, workGroupSize) + workGroupSize - 1) / workGroupSize;
            GL.DispatchCompute((uint)workGroups, 1, 1);
            
            // Wait for compute shader to finish
            GL.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);
        }
        
        public void Draw(Renderer renderer, Camera camera)
        {
            if (_activeParticles == 0)
                return;
            
            // Use particle shader
            GL.UseProgram(_particleShader);
            
            // Set view and projection matrices
            Matrix4 viewMatrix = camera.ViewMatrix;
            Matrix4 projectionMatrix = camera.ProjectionMatrix;
            
            int viewLocation = GL.GetUniformLocation(_particleShader, "view");
            int projectionLocation = GL.GetUniformLocation(_particleShader, "projection");
            int cameraRightLocation = GL.GetUniformLocation(_particleShader, "cameraRight");
            int cameraUpLocation = GL.GetUniformLocation(_particleShader, "cameraUp");
            
            // Set matrices
            GL.UniformMatrix4f(viewLocation, 1, false, ref viewMatrix);
            GL.UniformMatrix4f(projectionLocation, 1, false, ref projectionMatrix);
            GL.Uniform3f(cameraRightLocation, camera.Right.X, camera.Right.Y, camera.Right.Z);
            GL.Uniform3f(cameraUpLocation, camera.Up.X, camera.Up.Y, camera.Up.Z);
            
            // Bind vertex array
            GL.BindVertexArray(_vertexArray);
            
            // Draw particles using instanced rendering
            for (int i = 0; i < _activeParticles; i++)
            {
                // Set particle index
                int particleIndexLocation = GL.GetAttribLocation(_particleShader, "particleIndex");
                GL.VertexAttribI1i((uint)particleIndexLocation, i);
                
                // Draw quad
                GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
            }
            
            GL.BindVertexArray(0);
        }
        
        public void Clear()
        {
            _activeParticles = 0;
            _isEmitting = false;
            _emitTimer = 0.0f;
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
        
        public void Dispose()
        {
            GL.DeleteBuffer(_particleBuffer);
            GL.DeleteVertexArray(_vertexArray);
            GL.DeleteShader(_computeShader);
            GL.DeleteProgram(_particleShader);
        }
    }
}
