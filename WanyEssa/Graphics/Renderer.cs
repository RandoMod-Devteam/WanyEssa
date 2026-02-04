using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

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
        private int _camPositionLocation;
        private int _albedoTextureLocation;
        private int _normalTextureLocation;
        private int _metallicTextureLocation;
        private int _roughnessTextureLocation;
        private int _aoTextureLocation;
        private int _usePBR;
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
            
            // 3D fragment shader with PBR lighting
            string fragmentShaderSource = @"
                #version 330 core
                
                uniform vec4 color;
                uniform vec3 lightPosition;
                uniform vec3 lightColor;
                uniform vec3 ambientColor;
                uniform vec3 camPosition;
                uniform sampler2D albedoTexture;
                uniform sampler2D normalTexture;
                uniform sampler2D metallicTexture;
                uniform sampler2D roughnessTexture;
                uniform sampler2D aoTexture;
                uniform bool usePBR;
                
                in vec3 fragPosition;
                in vec3 fragNormal;
                in vec2 fragUV;
                
                out vec4 fragColor;
                
                const float PI = 3.14159265359;
                
                // PBR helper functions
                float DistributionGGX(vec3 N, vec3 H, float roughness)
                {
                    float a = roughness*roughness;
                    float a2 = a*a;
                    float NdotH = max(dot(N, H), 0.0);
                    float NdotH2 = NdotH*NdotH;
                    
                    float nom   = a2;
                    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
                    denom = PI * denom * denom;
                    
                    return nom / denom;
                }
                
                float GeometrySchlickGGX(float NdotV, float roughness)
                {
                    float r = (roughness + 1.0);
                    float k = (r*r) / 8.0;
                    
                    float nom   = NdotV;
                    float denom = NdotV * (1.0 - k) + k;
                    
                    return nom / denom;
                }
                
                float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
                {
                    float NdotV = max(dot(N, V), 0.0);
                    float NdotL = max(dot(N, L), 0.0);
                    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
                    float ggx1 = GeometrySchlickGGX(NdotL, roughness);
                    
                    return ggx1 * ggx2;
                }
                
                vec3 fresnelSchlick(float cosTheta, vec3 F0)
                {
                    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
                }
                
                void main()
                {
                    if (usePBR)
                    {
                        // PBR rendering
                        vec3 albedo = pow(texture(albedoTexture, fragUV).rgb, vec3(2.2));
                        float metallic = texture(metallicTexture, fragUV).r;
                        float roughness = texture(roughnessTexture, fragUV).r;
                        float ao = texture(aoTexture, fragUV).r;
                        
                        vec3 N = normalize(fragNormal);
                        vec3 V = normalize(camPosition - fragPosition);
                        
                        // Calculate reflectance at normal incidence
                        vec3 F0 = vec3(0.04);
                        F0 = mix(F0, albedo, metallic);
                        
                        // Lighting
                        vec3 Lo = vec3(0.0);
                        
                        // Directional light
                        vec3 L = normalize(lightPosition - fragPosition);
                        vec3 H = normalize(V + L);
                        float distance = length(lightPosition - fragPosition);
                        float attenuation = 1.0 / (distance * distance);
                        vec3 radiance = lightColor * attenuation;
                        
                        // BRDF
                        float NDF = DistributionGGX(N, H, roughness);
                        float G = GeometrySmith(N, V, L, roughness);
                        vec3 F = fresnelSchlick(max(dot(H, V), 0.0), F0);
                        
                        vec3 numerator = NDF * G * F;
                        float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001;
                        vec3 specular = numerator / denominator;
                        
                        vec3 kS = F;
                        vec3 kD = vec3(1.0) - kS;
                        kD *= 1.0 - metallic;
                        
                        float NdotL = max(dot(N, L), 0.0);
                        Lo += (kD * albedo / PI + specular) * radiance * NdotL;
                        
                        vec3 ambient = ambientColor * albedo * ao;
                        vec3 color = ambient + Lo;
                        
                        // Tone mapping
                        color = color / (color + vec3(1.0));
                        // Gamma correction
                        color = pow(color, vec3(1.0/2.2));
                        
                        fragColor = vec4(color, 1.0);
                    }
                    else
                    {
                        // Traditional lighting
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
            _camPositionLocation = GL.GetUniformLocation(_shaderProgram, "camPosition");
            _albedoTextureLocation = GL.GetUniformLocation(_shaderProgram, "albedoTexture");
            _normalTextureLocation = GL.GetUniformLocation(_shaderProgram, "normalTexture");
            _metallicTextureLocation = GL.GetUniformLocation(_shaderProgram, "metallicTexture");
            _roughnessTextureLocation = GL.GetUniformLocation(_shaderProgram, "roughnessTexture");
            _aoTextureLocation = GL.GetUniformLocation(_shaderProgram, "aoTexture");
            _usePBR = GL.GetUniformLocation(_shaderProgram, "usePBR");
            
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
        
        public void DrawMesh(Mesh mesh, Vector4 color, bool usePBR = false)
        {
            GL.UseProgram(_shaderProgram);
            
            // Set model matrix
            Matrix4 modelMatrix = Matrix4.CreateScale(mesh.Scale.X, mesh.Scale.Y, mesh.Scale.Z) *
                                 Matrix4.CreateRotationX(mesh.Rotation.X) *
                                 Matrix4.CreateRotationY(mesh.Rotation.Y) *
                                 Matrix4.CreateRotationZ(mesh.Rotation.Z) *
                                 Matrix4.CreateTranslation(mesh.Position.X, mesh.Position.Y, mesh.Position.Z);
            
            GL.UniformMatrix4f(_modelLocation, 1, false, ref modelMatrix);
            GL.Uniform4f(_colorLocation, color.X, color.Y, color.Z, color.W);
            GL.Uniform1i(_usePBR, usePBR ? 1 : 0);
            
            // Set texture units
            if (usePBR)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2d, 0); // Default texture
                GL.Uniform1i(_albedoTextureLocation, 0);
                
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2d, 0);
                GL.Uniform1i(_normalTextureLocation, 1);
                
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2d, 0);
                GL.Uniform1i(_metallicTextureLocation, 2);
                
                GL.ActiveTexture(TextureUnit.Texture3);
                GL.BindTexture(TextureTarget.Texture2d, 0);
                GL.Uniform1i(_roughnessTextureLocation, 3);
                
                GL.ActiveTexture(TextureUnit.Texture4);
                GL.BindTexture(TextureTarget.Texture2d, 0);
                GL.Uniform1i(_aoTextureLocation, 4);
            }
            
            // Draw mesh
            mesh.Draw();
        }
        
        public void DrawMesh(Mesh mesh, Vector4 color, int albedoTexture, int normalTexture, int metallicTexture, int roughnessTexture, int aoTexture)
        {
            GL.UseProgram(_shaderProgram);
            
            // Set model matrix
            Matrix4 modelMatrix = Matrix4.CreateScale(mesh.Scale.X, mesh.Scale.Y, mesh.Scale.Z) *
                                 Matrix4.CreateRotationX(mesh.Rotation.X) *
                                 Matrix4.CreateRotationY(mesh.Rotation.Y) *
                                 Matrix4.CreateRotationZ(mesh.Rotation.Z) *
                                 Matrix4.CreateTranslation(mesh.Position.X, mesh.Position.Y, mesh.Position.Z);
            
            GL.UniformMatrix4f(_modelLocation, 1, false, ref modelMatrix);
            GL.Uniform4f(_colorLocation, color.X, color.Y, color.Z, color.W);
            GL.Uniform1i(_usePBR, 1); // Always use PBR for this overload
            
            // Set texture units
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, albedoTexture > 0 ? albedoTexture : 0);
            GL.Uniform1i(_albedoTextureLocation, 0);
            
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2d, normalTexture > 0 ? normalTexture : 0);
            GL.Uniform1i(_normalTextureLocation, 1);
            
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2d, metallicTexture > 0 ? metallicTexture : 0);
            GL.Uniform1i(_metallicTextureLocation, 2);
            
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2d, roughnessTexture > 0 ? roughnessTexture : 0);
            GL.Uniform1i(_roughnessTextureLocation, 3);
            
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2d, aoTexture > 0 ? aoTexture : 0);
            GL.Uniform1i(_aoTextureLocation, 4);
            
            // Draw mesh
            mesh.Draw();
        }
        
        public void DrawRectangle(Vector3 position, Vector2 size, Vector4 color)
        {
            // Create a plane mesh for 2D rendering
            Mesh plane = Mesh.CreatePlane(size.X, size.Y);
            plane.Position = position;
            
            DrawMesh(plane, color);
            plane.Dispose();
        }
        
        public void DrawRectangle(Vector2 position, Vector2 size, Vector4 color)
        {
            DrawRectangle(new Vector3(position.X, position.Y, 0.0f), size, color);
        }
        
        public void DrawCircle(Vector3 center, float radius, Vector4 color)
        {
            // Create a sphere mesh for 2D rendering
            Mesh sphere = Mesh.CreateSphere(radius, 32);
            sphere.Position = center;
            
            DrawMesh(sphere, color);
            sphere.Dispose();
        }
        
        public void DrawCircle(Vector2 center, float radius, Vector4 color)
        {
            DrawCircle(new Vector3(center.X, center.Y, 0.0f), radius, color);
        }
        
        public void Dispose()
        {
            GL.DeleteBuffer(_vertexBuffer);
            GL.DeleteVertexArray(_vertexArray);
            GL.DeleteProgram(_shaderProgram);
        }
    }
}
