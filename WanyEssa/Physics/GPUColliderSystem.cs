using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace WanyEssa.Physics
{
    public class GPUColliderSystem
    {
        private int _computeShader;
        private int _colliderBuffer;
        private int _resultBuffer;
        private int _colliderCount;
        private int _maxColliders;
        private int _colliderDataSize;
        private int _resultDataSize;
        
        public GPUColliderSystem(int maxColliders = 1024)
        {
            _maxColliders = maxColliders;
            _colliderCount = 0;
            _colliderDataSize = maxColliders * 16; // Each collider: position (3 floats) + size/radius (1 float) + type (1 int) + padding (4 bytes)
            _resultDataSize = maxColliders * maxColliders; // Collision matrix
            
            InitializeComputeShader();
            InitializeBuffers();
        }
        
        private void InitializeComputeShader()
        {
            // Compute shader for collision detection
            string computeShaderSource = @"
                #version 430 core
                
                layout(local_size_x = 16, local_size_y = 16) in;
                
                layout(std430, binding = 0) buffer ColliderBuffer
                {
                    vec4 colliders[];
                };
                
                layout(std430, binding = 1) buffer ResultBuffer
                {
                    bool collisions[];
                };
                
                uniform int colliderCount;
                
                // Collider types
                const int COLLIDER_CIRCLE = 0;
                const int COLLIDER_BOX = 1;
                
                void main()
                {
                    int i = int(gl_GlobalInvocationID.x);
                    int j = int(gl_GlobalInvocationID.y);
                    
                    if (i >= colliderCount || j >= colliderCount || i >= j)
                        return;
                    
                    vec4 colliderA = colliders[i];
                    vec4 colliderB = colliders[j];
                    
                    bool collision = false;
                    
                    // Get collider types (stored in w component as integer)
                    int typeA = int(colliderA.w);
                    int typeB = int(colliderB.w);
                    
                    // Circle vs Circle
                    if (typeA == COLLIDER_CIRCLE && typeB == COLLIDER_CIRCLE)
                    {
                        vec3 posA = colliderA.xyz;
                        vec3 posB = colliderB.xyz;
                        float radiusA = colliderA.w;
                        float radiusB = colliderB.w;
                        
                        float distanceSquared = dot(posA - posB, posA - posB);
                        float radiusSum = radiusA + radiusB;
                        collision = distanceSquared <= radiusSum * radiusSum;
                    }
                    // Box vs Circle
                    else if (typeA == COLLIDER_BOX && typeB == COLLIDER_CIRCLE)
                    {
                        vec3 boxPos = colliderA.xyz;
                        vec3 boxSize = colliderA.www; // Assuming size is stored in w component
                        vec3 circlePos = colliderB.xyz;
                        float circleRadius = colliderB.w;
                        
                        vec3 closestPoint = clamp(circlePos, boxPos - boxSize / 2.0, boxPos + boxSize / 2.0);
                        float distanceSquared = dot(circlePos - closestPoint, circlePos - closestPoint);
                        collision = distanceSquared <= circleRadius * circleRadius;
                    }
                    // Circle vs Box
                    else if (typeA == COLLIDER_CIRCLE && typeB == COLLIDER_BOX)
                    {
                        vec3 circlePos = colliderA.xyz;
                        float circleRadius = colliderA.w;
                        vec3 boxPos = colliderB.xyz;
                        vec3 boxSize = colliderB.www;
                        
                        vec3 closestPoint = clamp(circlePos, boxPos - boxSize / 2.0, boxPos + boxSize / 2.0);
                        float distanceSquared = dot(circlePos - closestPoint, circlePos - closestPoint);
                        collision = distanceSquared <= circleRadius * circleRadius;
                    }
                    // Box vs Box
                    else if (typeA == COLLIDER_BOX && typeB == COLLIDER_BOX)
                    {
                        vec3 posA = colliderA.xyz;
                        vec3 sizeA = colliderA.www;
                        vec3 posB = colliderB.xyz;
                        vec3 sizeB = colliderB.www;
                        
                        vec3 aMin = posA - sizeA / 2.0;
                        vec3 aMax = posA + sizeA / 2.0;
                        vec3 bMin = posB - sizeB / 2.0;
                        vec3 bMax = posB + sizeB / 2.0;
                        
                        collision = aMin.x <= bMax.x && aMax.x >= bMin.x &&
                                   aMin.y <= bMax.y && aMax.y >= bMin.y &&
                                   aMin.z <= bMax.z && aMax.z >= bMin.z;
                    }
                    
                    // Store result
                    collisions[i * colliderCount + j] = collision;
                    collisions[j * colliderCount + i] = collision;
                }
            ";
            
            // Create compute shader
            _computeShader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(_computeShader, computeShaderSource);
            GL.CompileShader(_computeShader);
            
            // Check for compilation errors
            int success = GL.GetShaderi(_computeShader, ShaderParameterName.CompileStatus);
            if (success == 0)
            {
                GL.GetShaderInfoLog(_computeShader, out string infoLog);
                Console.WriteLine($"ERROR: Compute shader compilation failed: {infoLog}");
            }
        }
        
        private void InitializeBuffers()
        {
            // Create collider buffer
            _colliderBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _colliderBuffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, _colliderDataSize, IntPtr.Zero, BufferUsage.DynamicDraw);
            GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _colliderBuffer);
            
            // Create result buffer
            _resultBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _resultBuffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, _resultDataSize, IntPtr.Zero, BufferUsage.DynamicRead);
            GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 1, _resultBuffer);
            
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }
        
        public void AddCollider(Collider collider)
        {
            if (_colliderCount >= _maxColliders)
            {
                Console.WriteLine("ERROR: Maximum number of colliders reached!");
                return;
            }
            
            // Get collider data
            float[] colliderData = new float[4];
            int colliderType = 0;
            
            if (collider is CircleCollider circleCollider)
            {
                colliderData[0] = circleCollider.Body.Position.X;
                colliderData[1] = circleCollider.Body.Position.Y;
                colliderData[2] = circleCollider.Body.Position.Z;
                colliderData[3] = circleCollider.Radius;
                colliderType = 0;
            }
            else if (collider is BoxCollider boxCollider)
            {
                colliderData[0] = boxCollider.Body.Position.X;
                colliderData[1] = boxCollider.Body.Position.Y;
                colliderData[2] = boxCollider.Body.Position.Z;
                colliderData[3] = MathF.Max(boxCollider.Size.X, MathF.Max(boxCollider.Size.Y, boxCollider.Size.Z));
                colliderType = 1;
            }
            
            // Upload collider data
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _colliderBuffer);
            IntPtr offset = new IntPtr(_colliderCount * 16); // 4 floats * 4 bytes
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, offset, 16, colliderData);
            
            // Upload collider type
            int[] typeData = new int[1] { colliderType };
            offset = new IntPtr(_colliderCount * 16 + 12); // Position (12 bytes) + size/radius (4 bytes)
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, offset, 4, typeData);
            
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            
            _colliderCount++;
        }
        
        public void ClearColliders()
        {
            _colliderCount = 0;
        }
        
        public List<CollisionPair> DetectCollisions()
        {
            if (_colliderCount == 0)
                return [];
            
            // Use compute shader for collision detection
            GL.UseProgram(_computeShader);
            GL.Uniform1i(GL.GetUniformLocation(_computeShader, "colliderCount"), _colliderCount);
            
            // Dispatch compute shader
            int groupsX = (_colliderCount + 15) / 16;
            int groupsY = (_colliderCount + 15) / 16;
            GL.DispatchCompute((uint)groupsX, (uint)groupsY, 1);
            
            // Wait for compute shader to finish
            GL.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit);
            
            // Read results
            bool[] collisionResults = new bool[_colliderCount * _colliderCount];
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _resultBuffer);
            GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, _colliderCount * _colliderCount, collisionResults);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            
            // Process results
            List<CollisionPair> collisionPairs = [];
            for (int i = 0; i < _colliderCount; i++)
            {
                for (int j = i + 1; j < _colliderCount; j++)
                {
                    if (collisionResults[i * _colliderCount + j])
                    {
                        collisionPairs.Add(new CollisionPair(i, j));
                    }
                }
            }
            
            return collisionPairs;
        }
        
        public void Dispose()
        {
            GL.DeleteShader(_computeShader);
            GL.DeleteBuffer(_colliderBuffer);
            GL.DeleteBuffer(_resultBuffer);
        }
    }
    
    public struct CollisionPair
    {
        public int ColliderA;
        public int ColliderB;
        
        public CollisionPair(int a, int b)
        {
            ColliderA = a;
            ColliderB = b;
        }
    }
}
