using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace WanyEssa.Graphics
{
    public class InstanceRenderer
    {
        private int _shaderProgram;
        private int _vertexBuffer;
        private int _instanceBuffer;
        private int _vertexArray;
        private int _projectionLocation;
        private int _viewLocation;
        private int _colorLocation;
        private int _lightPositionLocation;
        private int _lightColorLocation;
        private int _ambientColorLocation;
        private int _camPositionLocation;
        private int _windowWidth;
        private int _windowHeight;
        
        public InstanceRenderer(int windowWidth, int windowHeight)
        {
            _windowWidth = windowWidth;
            _windowHeight = windowHeight;
            InitializeShader();
            InitializeBuffers();
            SetProjection(windowWidth, windowHeight);
            SetupLighting();
        }
        
        private void InitializeShader()
        {
            // Vertex shader with instancing
            string vertexShaderSource = @"
                #version 330 core
                
                uniform mat4 projection;
                uniform mat4 view;
                
                in vec3 position;
                in vec3 normal;
                in vec2 uv;
                in mat4 instanceMatrix;
                
                out vec3 fragPosition;
                out vec3 fragNormal;
                out vec2 fragUV;
                
                void main()
                {
                    vec4 worldPosition = instanceMatrix * vec4(position, 1.0);
                    fragPosition = vec3(worldPosition);
                    fragNormal = mat3(transpose(inverse(instanceMatrix))) * normal;
                    fragUV = uv;
                    gl_Position = projection * view * worldPosition;
                }
            ";
            
            // Fragment shader
            string fragmentShaderSource = @"
                #version 330 core
                
                uniform vec4 color;
                uniform vec3 lightPosition;
                uniform vec3 lightColor;
                uniform vec3 ambientColor;
                uniform vec3 camPosition;
                
                in vec3 fragPosition;
                in vec3 fragNormal;
                in vec2 fragUV;
                
                out vec4 fragColor;
                
                void main()
                {
                    // Ambient lighting
                    vec3 ambient = ambientColor * 0.5;
                    
                    // Diffuse lighting
                    vec3 normal = normalize(fragNormal);
                    vec3 lightDir = normalize(lightPosition - fragPosition);
                    float diff = max(dot(normal, lightDir), 0.0);
                    vec3 diffuse = lightColor * diff;
                    
                    // Combine lighting
                    vec3 result = (ambient + diffuse) * color.rgb;
                    fragColor = vec4(result, color.a);
                }
            ";
            
            // Create vertex shader
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);
            
            // Create fragment shader
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);
            
            // Create shader program
            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, vertexShader);
            GL.AttachShader(_shaderProgram, fragmentShader);
            GL.LinkProgram(_shaderProgram);
            
            // Get uniform locations
            _projectionLocation = GL.GetUniformLocation(_shaderProgram, "projection");
            _viewLocation = GL.GetUniformLocation(_shaderProgram, "view");
            _colorLocation = GL.GetUniformLocation(_shaderProgram, "color");
            _lightPositionLocation = GL.GetUniformLocation(_shaderProgram, "lightPosition");
            _lightColorLocation = GL.GetUniformLocation(_shaderProgram, "lightColor");
            _ambientColorLocation = GL.GetUniformLocation(_shaderProgram, "ambientColor");
            _camPositionLocation = GL.GetUniformLocation(_shaderProgram, "camPosition");
            
            // Clean up shaders
            GL.DetachShader(_shaderProgram, vertexShader);
            GL.DetachShader(_shaderProgram, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }
        
        private void InitializeBuffers()
        {
            // Create vertex buffer
            _vertexBuffer = GL.GenBuffer();
            
            // Create instance buffer
            _instanceBuffer = GL.GenBuffer();
            
            // Create vertex array
            _vertexArray = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArray);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            
            // Position attribute (3 floats)
            int positionLocation = GL.GetAttribLocation(_shaderProgram, "position");
            GL.EnableVertexAttribArray((uint)positionLocation);
            GL.VertexAttribPointer((uint)positionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), (IntPtr)0);
            
            // Normal attribute (3 floats)
            int normalLocation = GL.GetAttribLocation(_shaderProgram, "normal");
            GL.EnableVertexAttribArray((uint)normalLocation);
            GL.VertexAttribPointer((uint)normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), (IntPtr)(3 * sizeof(float)));
            
            // UV attribute (2 floats)
            int uvLocation = GL.GetAttribLocation(_shaderProgram, "uv");
            GL.EnableVertexAttribArray((uint)uvLocation);
            GL.VertexAttribPointer((uint)uvLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), (IntPtr)(6 * sizeof(float)));
            
            // Instance matrix attributes (4x4 matrix = 4 vec4 attributes)
            int instanceMatrixLocation = GL.GetAttribLocation(_shaderProgram, "instanceMatrix");
            GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceBuffer);
            
            // Matrix is stored as 4 separate vec4 attributes
            for (int i = 0; i < 4; i++)
            {
                int attribLocation = instanceMatrixLocation + i;
                GL.EnableVertexAttribArray((uint)attribLocation);
                GL.VertexAttribPointer((uint)attribLocation, 4, VertexAttribPointerType.Float, false, 4 * 4 * sizeof(float), (IntPtr)(i * 4 * sizeof(float)));
                GL.VertexAttribDivisor((uint)attribLocation, 1); // Update per instance
            }
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
        
        public void SetProjection(int windowWidth, int windowHeight)
        {
            _windowWidth = windowWidth;
            _windowHeight = windowHeight;
            
            // Create perspective projection matrix
            float aspectRatio = (float)windowWidth / windowHeight;
            Matrix4 projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
                OpenTK.Mathematics.MathHelper.DegreesToRadians(75.0f), aspectRatio, 0.1f, 1000.0f
            );
            
            GL.UseProgram(_shaderProgram);
            GL.UniformMatrix4f(_projectionLocation, 1, false, ref projectionMatrix);
        }
        
        public void SetViewMatrix(Matrix4 viewMatrix)
        {
            GL.UseProgram(_shaderProgram);
            GL.UniformMatrix4f(_viewLocation, 1, false, ref viewMatrix);
        }
        
        public void SetCameraPosition(Vector3 position)
        {
            GL.UseProgram(_shaderProgram);
            GL.Uniform3f(_camPositionLocation, position.X, position.Y, position.Z);
        }
        
        private void SetupLighting()
        {
            GL.UseProgram(_shaderProgram);
            
            // Set light position (above and behind the camera)
            GL.Uniform3f(_lightPositionLocation, 0.0f, 10.0f, -10.0f);
            
            // Set light color (white)
            GL.Uniform3f(_lightColorLocation, 1.0f, 1.0f, 1.0f);
            
            // Set ambient color (dark gray)
            GL.Uniform3f(_ambientColorLocation, 0.2f, 0.2f, 0.2f);
        }
        
        public void Begin()
        {
            GL.UseProgram(_shaderProgram);
            GL.Enable(EnableCap.DepthTest);
        }
        
        public void End()
        {
            GL.Disable(EnableCap.DepthTest);
        }
        
        public void DrawInstances(Mesh mesh, List<Matrix4> instanceMatrices, Vector4 color)
        {
            if (instanceMatrices.Count == 0)
                return;
            
            GL.UseProgram(_shaderProgram);
            GL.Uniform4f(_colorLocation, color.X, color.Y, color.Z, color.W);
            
            // Upload instance data
            GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, instanceMatrices.Count * 4 * 4 * sizeof(float), instanceMatrices.ToArray(), BufferUsage.DynamicDraw);
            
            // Bind mesh vertices
            GL.BindVertexArray(_vertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            
            // Get mesh vertex data
            List<float> vertexData = new();
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                var vertex = mesh.GetVertex(i);
                var normal = mesh.GetNormal(i);
                var uv = mesh.GetUV(i);
                
                vertexData.Add(vertex.X);
                vertexData.Add(vertex.Y);
                vertexData.Add(vertex.Z);
                vertexData.Add(normal.X);
                vertexData.Add(normal.Y);
                vertexData.Add(normal.Z);
                vertexData.Add(uv.X);
                vertexData.Add(uv.Y);
            }
            
            GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Count * sizeof(float), vertexData.ToArray(), BufferUsage.StaticDraw);
            
            // Draw instances
            if (mesh.IndexCount > 0)
            {
                // Use element array buffer for indexed drawing
                int indexBuffer = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
                GL.BufferData(BufferTarget.ElementArrayBuffer, mesh.IndexCount * sizeof(int), mesh.GetIndices(), BufferUsage.StaticDraw);
                
                GL.DrawElementsInstanced(PrimitiveType.Triangles, mesh.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, instanceMatrices.Count);
                
                GL.DeleteBuffer(indexBuffer);
            }
            else
            {
                GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, mesh.VertexCount, instanceMatrices.Count);
            }
            
            GL.BindVertexArray(0);
        }
        
        public void DrawInstances(Mesh mesh, List<Vector3> positions, List<Vector3> scales, Vector4 color)
        {
            if (positions.Count == 0)
                return;
            
            // Generate instance matrices
            List<Matrix4> instanceMatrices = new List<Matrix4>();
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 scale = i < scales.Count ? scales[i] : Vector3.One;
                Matrix4 instanceMatrix = Matrix4.CreateScale(scale.X, scale.Y, scale.Z) *
                                        Matrix4.CreateTranslation(positions[i].X, positions[i].Y, positions[i].Z);
                instanceMatrices.Add(instanceMatrix);
            }
            
            DrawInstances(mesh, instanceMatrices, color);
        }
        
        public void Dispose()
        {
            GL.DeleteBuffer(_vertexBuffer);
            GL.DeleteBuffer(_instanceBuffer);
            GL.DeleteVertexArray(_vertexArray);
            GL.DeleteProgram(_shaderProgram);
        }
    }
}
