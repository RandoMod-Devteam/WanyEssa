using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace WanyEssa.Graphics
{
    public class ComputeShaderManager
    {
        private readonly Dictionary<string, ComputeShader> _shaders;
        
        public ComputeShaderManager()
        {
            _shaders = [];
        }
        
        public ComputeShader CreateShader(string name, string shaderSource)
        {
            ComputeShader shader = new ComputeShader(name, shaderSource);
            _shaders[name] = shader;
            return shader;
        }
        
        public ComputeShader? LoadShader(string name, string filePath)
        {
            if (!File.Exists(filePath))
            {
                System.Console.WriteLine($"ERROR: Compute shader file not found: {filePath}");
                return null;
            }
            
            try
            {
                string shaderSource = File.ReadAllText(filePath);
                return CreateShader(name, shaderSource);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"ERROR: Failed to load compute shader: {ex.Message}");
                return null;
            }
        }
        
        public ComputeShader? GetShader(string name)
        {
            if (_shaders.TryGetValue(name, out ComputeShader? shader))
            {
                return shader;
            }
            return null;
        }
        
        public void RemoveShader(string name)
        {
            if (_shaders.TryGetValue(name, out ComputeShader? shader))
            {
                shader?.Dispose();
                _shaders.Remove(name);
            }
        }
        
        public void Clear()
        {
            foreach (var shader in _shaders.Values)
            {
                shader.Dispose();
            }
            _shaders.Clear();
        }
        
        public void Dispose()
        {
            Clear();
        }
    }
    
    public class ComputeShader : IDisposable
    {
        private string _name;
        private int _shaderProgram;
        private readonly Dictionary<string, int> _uniformLocations;
        private readonly Dictionary<int, int> _bufferBindings;
        private bool _disposed;
        
        public string Name => _name;
        public int Program => _shaderProgram;
        
        public ComputeShader(string name, string shaderSource)
        {
            _name = name;
            _uniformLocations = [];
            _bufferBindings = [];
            _disposed = false;
            
            InitializeShader(shaderSource);
        }
        
        private void InitializeShader(string shaderSource)
        {
            // Create compute shader
            int computeShader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(computeShader, shaderSource);
            GL.CompileShader(computeShader);
            
            // Check for compilation errors
            int success = GL.GetShaderi(computeShader, ShaderParameterName.CompileStatus);
            if (success == 0)
            {
                GL.GetShaderInfoLog(computeShader, out string infoLog);
                System.Console.WriteLine($"ERROR: Compute shader compilation failed for {_name}: {infoLog}");
                GL.DeleteShader(computeShader);
                return;
            }

            // Create shader program
            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, computeShader);
            GL.LinkProgram(_shaderProgram);

            // Check for linking errors
            success = GL.GetProgrami(_shaderProgram, ProgramProperty.LinkStatus);
            if (success == 0)
            {
                GL.GetProgramInfoLog(_shaderProgram, out string infoLog);
                System.Console.WriteLine($"ERROR: Compute shader linking failed for {_name}: {infoLog}");
                GL.DeleteProgram(_shaderProgram);
                GL.DeleteShader(computeShader);
                return;
            }
            
            // Clean up shader
            GL.DeleteShader(computeShader);
        }
        
        public void Use()
        {
            if (_disposed || _shaderProgram == 0)
                return;
            
            GL.UseProgram(_shaderProgram);
        }
        
        public void Dispatch(uint workGroupsX, uint workGroupsY = 1, uint workGroupsZ = 1)
        {
            if (_disposed || _shaderProgram == 0)
                return;
            
            GL.DispatchCompute(workGroupsX, workGroupsY, workGroupsZ);
        }
        
        public void MemoryBarrier(MemoryBarrierMask flags)
        {
            GL.MemoryBarrier(flags);
        }
        
        // Uniform setters
        public void SetUniform(string name, int value)
        {
            int location = GetUniformLocation(name);
            if (location >= 0)
            {
                GL.Uniform1i(location, value);
            }
        }
        
        public void SetUniform(string name, float value)
        {
            int location = GetUniformLocation(name);
            if (location >= 0)
            {
                GL.Uniform1f(location, value);
            }
        }
        
        public void SetUniform(string name, Vector2 value)
        {
            int location = GetUniformLocation(name);
            if (location >= 0)
            {
                GL.Uniform2f(location, value.X, value.Y);
            }
        }
        
        public void SetUniform(string name, Vector3 value)
        {
            int location = GetUniformLocation(name);
            if (location >= 0)
            {
                GL.Uniform3f(location, value.X, value.Y, value.Z);
            }
        }
        
        public void SetUniform(string name, Vector4 value)
        {
            int location = GetUniformLocation(name);
            if (location >= 0)
            {
                GL.Uniform4f(location, value.X, value.Y, value.Z, value.W);
            }
        }
        
        public unsafe void SetUniform(string name, float[] value, int count)
        {
            int location = GetUniformLocation(name);
            if (location >= 0)
            {
                fixed (float* ptr = value)
                {
                    GL.Uniform1fv(location, count, ptr);
                }
            }
        }
        
        public unsafe void SetUniform(string name, Vector3[] value, int count)
        {
            int location = GetUniformLocation(name);
            if (location >= 0)
            {
                float[] data = new float[count * 3];
                for (int i = 0; i < count; i++)
                {
                    data[i * 3] = value[i].X;
                    data[i * 3 + 1] = value[i].Y;
                    data[i * 3 + 2] = value[i].Z;
                }
                fixed (float* ptr = data)
                {
                    GL.Uniform3fv(location, count, ptr);
                }
            }
        }
        
        // Buffer management
        public void BindBuffer(int binding, int buffer)
        {
            if (!_bufferBindings.ContainsKey(binding))
            {
                _bufferBindings[binding] = buffer;
            }
            else
            {
                _bufferBindings[binding] = buffer;
            }
            
            GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, (uint)binding, buffer);
        }

        public void UnbindBuffer(int binding)
        {
            if (_bufferBindings.ContainsKey(binding))
            {
                _bufferBindings.Remove(binding);
            }

            GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, (uint)binding, 0);
        }

        public void UnbindAllBuffers()
        {
            foreach (var binding in _bufferBindings.Keys)
            {
                GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, (uint)binding, 0);
            }
            _bufferBindings.Clear();
        }
        
        // Helper methods
        private int GetUniformLocation(string name)
        {
            if (_uniformLocations.TryGetValue(name, out int location))
            {
                return location;
            }
            
            location = GL.GetUniformLocation(_shaderProgram, name);
            _uniformLocations[name] = location;
            return location;
        }
        
        // Dispose
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_shaderProgram != 0)
                {
                    GL.DeleteProgram(_shaderProgram);
                    _shaderProgram = 0;
                }
                
                _uniformLocations.Clear();
                _bufferBindings.Clear();
                _disposed = true;
            }
        }
    }
    
    public class ComputeBuffer<T> : IDisposable where T : unmanaged
    {
        private int _buffer;
        private int _size;
        private readonly BufferUsage _usage;
        private bool _disposed;
        
        public int Buffer => _buffer;
        public int Size => _size;
        
        public ComputeBuffer(int size, BufferUsage usage)
        {
            _size = size;
            _usage = usage;
            _disposed = false;
            
            _buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _buffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, size, IntPtr.Zero, usage);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }
        
        public ComputeBuffer(int count)
        {
            _size = count * System.Runtime.InteropServices.Marshal.SizeOf<T>();
            _usage = BufferUsage.DynamicDraw;
            _disposed = false;
            
            _buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _buffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, _size, IntPtr.Zero, _usage);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }
        
        public void SetData(T[] data)
        {
            if (_disposed)
                return;
            
            int dataSize = data.Length * System.Runtime.InteropServices.Marshal.SizeOf<T>();
            if (dataSize > _size)
            {
                System.Console.WriteLine("ERROR: Data size exceeds buffer size!");
                return;
            }
            
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _buffer);
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, dataSize, data);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }
        
        public void SetData(T[] data, int offset, int count)
        {
            if (_disposed)
                return;
            
            int dataSize = count * System.Runtime.InteropServices.Marshal.SizeOf<T>();
            int dataOffset = offset * System.Runtime.InteropServices.Marshal.SizeOf<T>();
            
            if (dataOffset + dataSize > _size)
            {
                System.Console.WriteLine("ERROR: Data size exceeds buffer size!");
                return;
            }
            
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _buffer);
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, new IntPtr(dataOffset), dataSize, data);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }
        
        public void GetData(T[] data)
        {
            if (_disposed)
                return;
            
            int dataSize = data.Length * System.Runtime.InteropServices.Marshal.SizeOf<T>();
            if (dataSize > _size)
            {
                System.Console.WriteLine("ERROR: Data size exceeds buffer size!");
                return;
            }
            
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _buffer);
            GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, dataSize, data);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }
        
        public void GetData(T[] data, int offset, int count)
        {
            if (_disposed)
                return;
            
            int dataSize = count * System.Runtime.InteropServices.Marshal.SizeOf<T>();
            int dataOffset = offset * System.Runtime.InteropServices.Marshal.SizeOf<T>();
            
            if (dataOffset + dataSize > _size)
            {
                System.Console.WriteLine("ERROR: Data size exceeds buffer size!");
                return;
            }
            
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _buffer);
            GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, new IntPtr(dataOffset), dataSize, data);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_buffer != 0)
                {
                    GL.DeleteBuffer(_buffer);
                    _buffer = 0;
                }

                _size = 0;
                _disposed = true;
            }
        }
    }
}
