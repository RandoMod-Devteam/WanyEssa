using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace WanyEssa.Graphics
{
    public class OpenGLResourceManager : IDisposable
    {
        private readonly Dictionary<string, VertexArrayObject> _vaos;
        private readonly Dictionary<string, BufferObject> _vbos;
        private readonly Dictionary<string, UniformBufferObject> _ubos;
        private bool _disposed;
        
        public OpenGLResourceManager()
        {
            _vaos = [];
            _vbos = [];
            _ubos = [];
            _disposed = false;
        }
        
        // VAO management
        public VertexArrayObject CreateVAO(string name)
        {
            VertexArrayObject vao = new();
            _vaos[name] = vao;
            return vao;
        }
        
        public VertexArrayObject? GetVAO(string name)
        {
            if (_vaos.TryGetValue(name, out VertexArrayObject? vao))
            {
                return vao;
            }
            return null;
        }
        
        public void RemoveVAO(string name)
        {
            if (_vaos.TryGetValue(name, out VertexArrayObject? vao))
            {
                vao.Dispose();
                _vaos.Remove(name);
            }
        }
        
        // VBO management
        public BufferObject CreateVBO(string name, BufferTarget target, BufferUsage usage)
        {
            BufferObject vbo = new BufferObject(target, usage);
            _vbos[name] = vbo;
            return vbo;
        }
        
        public BufferObject? GetVBO(string name)
        {
            if (_vbos.TryGetValue(name, out BufferObject? vbo))
            {
                return vbo;
            }
            return null;
        }
        
        public void RemoveVBO(string name)
        {
            if (_vbos.TryGetValue(name, out BufferObject? vbo))
            {
                vbo.Dispose();
                _vbos.Remove(name);
            }
        }
        
        // UBO management
        public UniformBufferObject CreateUBO(string name, int binding, int size, BufferUsage usage)
        {
            UniformBufferObject ubo = new UniformBufferObject(binding, size, usage);
            _ubos[name] = ubo;
            return ubo;
        }
        
        public UniformBufferObject? GetUBO(string name)
        {
            if (_ubos.TryGetValue(name, out UniformBufferObject? ubo))
            {
                return ubo;
            }
            return null;
        }
        
        public void RemoveUBO(string name)
        {
            if (_ubos.TryGetValue(name, out UniformBufferObject? ubo))
            {
                ubo.Dispose();
                _ubos.Remove(name);
            }
        }
        
        // Cleanup
        public void Clear()
        {
            foreach (var vao in _vaos.Values)
            {
                vao.Dispose();
            }
            _vaos.Clear();
            
            foreach (var vbo in _vbos.Values)
            {
                vbo.Dispose();
            }
            _vbos.Clear();
            
            foreach (var ubo in _ubos.Values)
            {
                ubo.Dispose();
            }
            _ubos.Clear();
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                Clear();
                _disposed = true;
            }
        }
    }
    
    public class VertexArrayObject : IDisposable
    {
        private int _vao;
        private bool _disposed;
        
        public int VAO => _vao;
        
        public VertexArrayObject()
        {
            _vao = GL.GenVertexArray();
            _disposed = false;
        }
        
        public void Bind()
        {
            if (!_disposed)
            {
                GL.BindVertexArray(_vao);
            }
        }
        
        public void Unbind()
        {
            GL.BindVertexArray(0);
        }
        
        public void EnableVertexAttribArray(int index)
        {
            Bind();
            GL.EnableVertexAttribArray((uint)index);
        }
        
        public void DisableVertexAttribArray(int index)
        {
            Bind();
            GL.DisableVertexAttribArray((uint)index);
        }
        
        public void VertexAttribPointer(int index, int size, VertexAttribPointerType type, bool normalized, int stride, int offset)
        {
            Bind();
            GL.VertexAttribPointer((uint)index, size, type, normalized, stride, (IntPtr)offset);
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteVertexArray(_vao);
                _vao = 0;
                _disposed = true;
            }
        }
    }
    
    public class BufferObject : IDisposable
    {
        private int _buffer;
        private BufferTarget _target;
        private BufferUsage _usage;
        private bool _disposed;
        
        public int Buffer => _buffer;
        public BufferTarget Target => _target;
        public BufferUsage Usage => _usage;
        
        public BufferObject(BufferTarget target, BufferUsage usage)
        {
            _buffer = GL.GenBuffer();
            _target = target;
            _usage = usage;
            _disposed = false;
        }
        
        public void Bind()
        {
            if (!_disposed)
            {
                GL.BindBuffer(_target, _buffer);
            }
        }
        
        public void Unbind()
        {
            GL.BindBuffer(_target, 0);
        }
        
        public void BufferData(int size, IntPtr data)
        {
            if (_disposed)
                return;
            
            Bind();
            GL.BufferData(_target, size, data, _usage);
        }
        
        public void BufferSubData(int offset, int size, IntPtr data)
        {
            if (_disposed)
                return;
            
            Bind();
            GL.BufferSubData(_target, new IntPtr(offset), size, data);
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteBuffer(_buffer);
                _buffer = 0;
                _disposed = true;
            }
        }
    }
    
    public class UniformBufferObject : IDisposable
    {
        private int _buffer;
        private int _binding;
        private int _size;
        private BufferUsage _usage;
        private bool _disposed;
        
        public int Buffer => _buffer;
        public int Binding => _binding;
        public int Size => _size;
        
        public UniformBufferObject(int binding, int size, BufferUsage usage)
        {
            _buffer = GL.GenBuffer();
            _binding = binding;
            _size = size;
            _usage = usage;
            _disposed = false;
            
            GL.BindBuffer(BufferTarget.UniformBuffer, _buffer);
            GL.BufferData(BufferTarget.UniformBuffer, size, IntPtr.Zero, usage);
            GL.BindBufferBase(BufferTarget.UniformBuffer, (uint)binding, _buffer);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }
        
        public void Bind()
        {
            if (!_disposed)
            {
                GL.BindBuffer(BufferTarget.UniformBuffer, _buffer);
            }
        }
        
        public void Unbind()
        {
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }
        
        public void BufferData(int size, IntPtr data)
        {
            if (_disposed)
                return;
            
            Bind();
            GL.BufferData(BufferTarget.UniformBuffer, size, data, _usage);
        }
        
        public void BufferSubData(int offset, int size, IntPtr data)
        {
            if (_disposed)
                return;
            
            Bind();
            GL.BufferSubData(BufferTarget.UniformBuffer, new IntPtr(offset), size, data);
        }
        
        public unsafe void SetUniformBlockBinding(int program, string blockName)
        {
            if (_disposed)
                return;

            uint blockIndex = GL.GetUniformBlockIndex(program, blockName);
            if (blockIndex != 0xFFFFFFFF) // GL_INVALID_INDEX
            {
                GL.UniformBlockBinding(program, blockIndex, (uint)_binding);
            }
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteBuffer(_buffer);
                _buffer = 0;
                _disposed = true;
            }
        }
    }
    
    // Common uniform buffer structures
    public struct PerFrameUniforms
    {
        public OpenTK.Mathematics.Matrix4 ViewMatrix;
        public OpenTK.Mathematics.Matrix4 ProjectionMatrix;
        public OpenTK.Mathematics.Vector3 CameraPosition;
        public float Time;
        public OpenTK.Mathematics.Vector2 Resolution;
        
        public static int Size => sizeof(float) * (16 * 2 + 3 + 1 + 2); // 2 matrices (16 floats each) + camera position (3) + time (1) + resolution (2)
    }
    
    public struct PerObjectUniforms
    {
        public OpenTK.Mathematics.Matrix4 ModelMatrix;
        public OpenTK.Mathematics.Vector4 Color;
        
        public static int Size => sizeof(float) * (16 + 4); // Model matrix (16 floats) + color (4)
    }
    
    public struct LightUniforms
    {
        public OpenTK.Mathematics.Vector3 Position;
        public OpenTK.Mathematics.Vector3 Color;
        public OpenTK.Mathematics.Vector3 AmbientColor;
        public float Intensity;
        
        public static int Size => sizeof(float) * (3 + 3 + 3 + 1); // Position (3) + color (3) + ambient color (3) + intensity (1)
    }
}
