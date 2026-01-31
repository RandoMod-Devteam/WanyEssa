using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using Matrix4 = OpenTK.Mathematics.Matrix4;
using Vector2 = WanyEssa.Math.Vector2;
using Vector3 = WanyEssa.Math.Vector3;
using Color = WanyEssa.Math.Color;
using WanyEssa.Math;

namespace WanyEssa.Graphics
{
    public class Renderer
    {
        private int _shaderProgram;
        private int _vertexBuffer;
        private int _vertexArray;
        private int _projectionLocation;
        private int _modelLocation;
        private int _viewLocation;
        private int _colorLocation;
        private int _lightPositionLocation;
        private int _lightColorLocation;
        private int _ambientColorLocation;
        private int _windowWidth;
        private int _windowHeight;
        
        public Renderer(int windowWidth, int windowHeight)
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
            // 3D vertex shader with lighting
            string vertexShaderSource = @"
                #version 330 core
                
                uniform mat4 projection;
                uniform mat4 model;
                uniform mat4 view;
                
                in vec3 position;
                in vec3 normal;
                in vec2 uv;
                
                out vec3 fragPosition;
                out vec3 fragNormal;
                out vec2 fragUV;
                
                void main()
                {
                    fragPosition = vec3(model * vec4(position, 1.0));
                    fragNormal = mat3(transpose(inverse(model))) * normal;
                    fragUV = uv;
                    gl_Position = projection * view * model * vec4(position, 1.0);
                }
            ";
            
            // 3D fragment shader with lighting
            string fragmentShaderSource = @"
                #version 330 core
                
                uniform vec4 color;
                uniform vec3 lightPosition;
                uniform vec3 lightColor;
                uniform vec3 ambientColor;
                
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
            _modelLocation = GL.GetUniformLocation(_shaderProgram, "model");
            _viewLocation = GL.GetUniformLocation(_shaderProgram, "view");
            _colorLocation = GL.GetUniformLocation(_shaderProgram, "color");
            _lightPositionLocation = GL.GetUniformLocation(_shaderProgram, "lightPosition");
            _lightColorLocation = GL.GetUniformLocation(_shaderProgram, "lightColor");
            _ambientColorLocation = GL.GetUniformLocation(_shaderProgram, "ambientColor");
            
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
            
            // Create vertex array
            _vertexArray = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArray);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            
            // Position attribute (3 floats)
            int positionLocation = GL.GetAttribLocation(_shaderProgram, "position");
            GL.EnableVertexAttribArray(positionLocation);
            GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            
            // Normal attribute (3 floats)
            int normalLocation = GL.GetAttribLocation(_shaderProgram, "normal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
            
            // UV attribute (2 floats)
            int uvLocation = GL.GetAttribLocation(_shaderProgram, "uv");
            GL.EnableVertexAttribArray(uvLocation);
            GL.VertexAttribPointer(uvLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
        
        public void SetProjection(int windowWidth, int windowHeight)
        {
            _windowWidth = windowWidth;
            _windowHeight = windowHeight;
            
            // Create perspective projection matrix for 3D rendering
            float aspectRatio = (float)windowWidth / windowHeight;
            Matrix4 projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
                OpenTK.Mathematics.MathHelper.DegreesToRadians(75.0f), aspectRatio, 0.1f, 1000.0f
            );
            
            GL.UseProgram(_shaderProgram);
            GL.UniformMatrix4(_projectionLocation, false, ref projectionMatrix);
        }
        
        public void SetViewMatrix(Matrix4 viewMatrix)
        {
            GL.UseProgram(_shaderProgram);
            GL.UniformMatrix4(_viewLocation, false, ref viewMatrix);
        }
        
        private void SetupLighting()
        {
            GL.UseProgram(_shaderProgram);
            
            // Set light position (above and behind the camera)
            GL.Uniform3(_lightPositionLocation, 0.0f, 10.0f, -10.0f);
            
            // Set light color (white)
            GL.Uniform3(_lightColorLocation, 1.0f, 1.0f, 1.0f);
            
            // Set ambient color (dark gray)
            GL.Uniform3(_ambientColorLocation, 0.2f, 0.2f, 0.2f);
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
        
        public void DrawMesh(Mesh mesh, Color color)
        {
            GL.UseProgram(_shaderProgram);
            
            // Set model matrix
            Matrix4 modelMatrix = Matrix4.CreateScale(mesh.Scale.X, mesh.Scale.Y, mesh.Scale.Z) *
                                 Matrix4.CreateRotationX(mesh.Rotation.X) *
                                 Matrix4.CreateRotationY(mesh.Rotation.Y) *
                                 Matrix4.CreateRotationZ(mesh.Rotation.Z) *
                                 Matrix4.CreateTranslation(mesh.Position.X, mesh.Position.Y, mesh.Position.Z);
            
            GL.UniformMatrix4(_modelLocation, false, ref modelMatrix);
            GL.Uniform4(_colorLocation, color.R, color.G, color.B, color.A);
            
            // Draw mesh
            mesh.Draw();
        }
        
        public void DrawRectangle(WanyEssa.Math.Vector3 position, WanyEssa.Math.Vector2 size, Color color)
        {
            // Create a plane mesh for 2D rendering
            Mesh plane = Mesh.CreatePlane(size.X, size.Y);
            plane.Position = position;
            
            DrawMesh(plane, color);
            plane.Dispose();
        }
        
        public void DrawRectangle(WanyEssa.Math.Vector2 position, WanyEssa.Math.Vector2 size, Color color)
        {
            DrawRectangle(new WanyEssa.Math.Vector3(position.X, position.Y, 0.0f), size, color);
        }
        
        public void DrawCircle(WanyEssa.Math.Vector3 center, float radius, Color color)
        {
            // Create a sphere mesh for 2D rendering
            Mesh sphere = Mesh.CreateSphere(radius, 32);
            sphere.Position = center;
            
            DrawMesh(sphere, color);
            sphere.Dispose();
        }
        
        public void DrawCircle(WanyEssa.Math.Vector2 center, float radius, Color color)
        {
            DrawCircle(new WanyEssa.Math.Vector3(center.X, center.Y, 0.0f), radius, color);
        }
        
        public void Dispose()
        {
            GL.DeleteBuffer(_vertexBuffer);
            GL.DeleteVertexArray(_vertexArray);
            GL.DeleteProgram(_shaderProgram);
        }
    }
}