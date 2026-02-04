using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace WanyEssa.Graphics
{
    public class PostProcessor
    {
        private int _shaderProgram;
        private int _vertexBuffer;
        private int _vertexArray;
        private int _framebuffer;
        private int _textureColorBuffer;
        private int _renderbufferDepth;
        private int _windowWidth;
        private int _windowHeight;
        private int _screenTextureLocation;
        private int _timeLocation;
        private int _resolutionLocation;
        private int _bloomThresholdLocation;
        private int _bloomStrengthLocation;
        private int _useBloomLocation;
        private int _useGrayscaleLocation;
        private int _useSepiaLocation;
        private int _useVignetteLocation;
        private int _vignetteStrengthLocation;
        
        public bool UseBloom { get; set; } = false;
        public float BloomThreshold { get; set; } = 0.8f;
        public float BloomStrength { get; set; } = 1.0f;
        public bool UseGrayscale { get; set; } = false;
        public bool UseSepia { get; set; } = false;
        public bool UseVignette { get; set; } = false;
        public float VignetteStrength { get; set; } = 0.5f;
        
        public PostProcessor(int windowWidth, int windowHeight)
        {
            _windowWidth = windowWidth;
            _windowHeight = windowHeight;
            InitializeShader();
            InitializeBuffers();
            InitializeFramebuffer();
        }
        
        private void InitializeShader()
        {
            // Vertex shader for fullscreen quad
            string vertexShaderSource = @"
                #version 330 core
                
                in vec2 position;
                in vec2 uv;
                
                out vec2 fragUV;
                
                void main()
                {
                    gl_Position = vec4(position, 0.0, 1.0);
                    fragUV = uv;
                }
            ";
            
            // Fragment shader with post-processing effects
            string fragmentShaderSource = @"
                #version 330 core
                
                uniform sampler2D screenTexture;
                uniform float time;
                uniform vec2 resolution;
                uniform float bloomThreshold;
                uniform float bloomStrength;
                uniform bool useBloom;
                uniform bool useGrayscale;
                uniform bool useSepia;
                uniform bool useVignette;
                uniform float vignetteStrength;
                
                in vec2 fragUV;
                
                out vec4 fragColor;
                
                // Bloom effect helper
                vec3 bloom(vec3 color)
                {
                    // Extract bright areas
                    float brightness = dot(color, vec3(0.2126, 0.7152, 0.0722));
                    if (brightness > bloomThreshold)
                    {
                        return color * bloomStrength;
                    }
                    return vec3(0.0);
                }
                
                // Grayscale effect
                vec3 grayscale(vec3 color)
                {
                    float gray = dot(color, vec3(0.2126, 0.7152, 0.0722));
                    return vec3(gray);
                }
                
                // Sepia effect
                vec3 sepia(vec3 color)
                {
                    float gray = dot(color, vec3(0.2126, 0.7152, 0.0722));
                    return vec3(
                        gray * 1.2,  // Red
                        gray * 0.9,  // Green
                        gray * 0.7   // Blue
                    );
                }
                
                // Vignette effect
                float vignette(vec2 uv)
                {
                    vec2 center = uv - 0.5;
                    float dist = length(center);
                    return 1.0 - smoothstep(0.3, 0.6, dist) * vignetteStrength;
                }
                
                void main()
                {
                    vec3 color = texture(screenTexture, fragUV).rgb;
                    
                    // Apply bloom
                    if (useBloom)
                    {
                        vec3 bloomColor = bloom(color);
                        color = mix(color, bloomColor, 0.5);
                    }
                    
                    // Apply grayscale
                    if (useGrayscale)
                    {
                        color = grayscale(color);
                    }
                    
                    // Apply sepia
                    if (useSepia)
                    {
                        color = sepia(color);
                    }
                    
                    // Apply vignette
                    if (useVignette)
                    {
                        float vignetteValue = vignette(fragUV);
                        color *= vignetteValue;
                    }
                    
                    fragColor = vec4(color, 1.0);
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
            _screenTextureLocation = GL.GetUniformLocation(_shaderProgram, "screenTexture");
            _timeLocation = GL.GetUniformLocation(_shaderProgram, "time");
            _resolutionLocation = GL.GetUniformLocation(_shaderProgram, "resolution");
            _bloomThresholdLocation = GL.GetUniformLocation(_shaderProgram, "bloomThreshold");
            _bloomStrengthLocation = GL.GetUniformLocation(_shaderProgram, "bloomStrength");
            _useBloomLocation = GL.GetUniformLocation(_shaderProgram, "useBloom");
            _useGrayscaleLocation = GL.GetUniformLocation(_shaderProgram, "useGrayscale");
            _useSepiaLocation = GL.GetUniformLocation(_shaderProgram, "useSepia");
            _useVignetteLocation = GL.GetUniformLocation(_shaderProgram, "useVignette");
            _vignetteStrengthLocation = GL.GetUniformLocation(_shaderProgram, "vignetteStrength");
            
            // Clean up shaders
            GL.DetachShader(_shaderProgram, vertexShader);
            GL.DetachShader(_shaderProgram, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }
        
        private void InitializeBuffers()
        {
            // Create vertex buffer for fullscreen quad
            _vertexBuffer = GL.GenBuffer();
            
            // Create vertex array
            _vertexArray = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArray);
            
            // Fullscreen quad vertices with UVs
            float[] quadVertices = {
                // Position   // UV
                -1.0f,  1.0f, 0.0f, 1.0f,  // Top-left
                -1.0f, -1.0f, 0.0f, 0.0f,  // Bottom-left
                 1.0f, -1.0f, 1.0f, 0.0f,  // Bottom-right
                 1.0f,  1.0f, 1.0f, 1.0f   // Top-right
            };
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsage.StaticDraw);
            
            // Position attribute (2 floats)
            int positionLocation = GL.GetAttribLocation(_shaderProgram, "position");
            GL.EnableVertexAttribArray((uint)positionLocation);
            GL.VertexAttribPointer((uint)positionLocation, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)0);
            
            // UV attribute (2 floats)
            int uvLocation = GL.GetAttribLocation(_shaderProgram, "uv");
            GL.EnableVertexAttribArray((uint)uvLocation);
            GL.VertexAttribPointer((uint)uvLocation, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (IntPtr)(2 * sizeof(float)));
            
            GL.BindVertexArray(0);
        }
        
        private void InitializeFramebuffer()
        {
            // Create framebuffer
            _framebuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
            
            // Create texture color buffer
            _textureColorBuffer = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, _textureColorBuffer);
            unsafe
            {
                GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, _windowWidth, _windowHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            }
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, _textureColorBuffer, 0);
            
            // Create renderbuffer for depth
            _renderbufferDepth = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _renderbufferDepth);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent, _windowWidth, _windowHeight);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _renderbufferDepth);
            
            // Check if framebuffer is complete
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.FramebufferComplete)
            {
                Console.WriteLine("ERROR: Framebuffer is not complete!");
            }
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
        
        public void Resize(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
            
            // Recreate framebuffer with new size
            GL.DeleteFramebuffer(_framebuffer);
            GL.DeleteTexture(_textureColorBuffer);
            GL.DeleteRenderbuffer(_renderbufferDepth);
            InitializeFramebuffer();
        }
        
        public void BeginRender()
        {
            // Bind framebuffer
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
            GL.Enable(EnableCap.DepthTest);
            
            // Clear buffers
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }
        
        public void EndRender(float time = 0.0f)
        {
            // Bind back to default framebuffer
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Disable(EnableCap.DepthTest);
            
            // Clear screen
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            
            // Use post-processing shader
            GL.UseProgram(_shaderProgram);
            
            // Set uniforms
            GL.Uniform1i(_screenTextureLocation, 0);
            GL.Uniform1f(_timeLocation, time);
            GL.Uniform2f(_resolutionLocation, (float)_windowWidth, (float)_windowHeight);
            GL.Uniform1f(_bloomThresholdLocation, BloomThreshold);
            GL.Uniform1f(_bloomStrengthLocation, BloomStrength);
            GL.Uniform1i(_useBloomLocation, UseBloom ? 1 : 0);
            GL.Uniform1i(_useGrayscaleLocation, UseGrayscale ? 1 : 0);
            GL.Uniform1i(_useSepiaLocation, UseSepia ? 1 : 0);
            GL.Uniform1i(_useVignetteLocation, UseVignette ? 1 : 0);
            GL.Uniform1f(_vignetteStrengthLocation, VignetteStrength);
            
            // Bind screen texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, _textureColorBuffer);
            
            // Draw fullscreen quad
            GL.BindVertexArray(_vertexArray);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
            GL.BindVertexArray(0);
        }
        
        public void Dispose()
        {
            GL.DeleteFramebuffer(_framebuffer);
            GL.DeleteTexture(_textureColorBuffer);
            GL.DeleteRenderbuffer(_renderbufferDepth);
            GL.DeleteBuffer(_vertexBuffer);
            GL.DeleteVertexArray(_vertexArray);
            GL.DeleteProgram(_shaderProgram);
        }
    }
}
