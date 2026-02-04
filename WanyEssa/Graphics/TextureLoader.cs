using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using TextureTarget = OpenTK.Graphics.OpenGL.TextureTarget;
using InternalFormat = OpenTK.Graphics.OpenGL.InternalFormat;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
using PixelType = OpenTK.Graphics.OpenGL.PixelType;
using TextureParameterName = OpenTK.Graphics.OpenGL.TextureParameterName;
using TextureWrapMode = OpenTK.Graphics.OpenGL.TextureWrapMode;
using TextureMinFilter = OpenTK.Graphics.OpenGL.TextureMinFilter;
using TextureMagFilter = OpenTK.Graphics.OpenGL.TextureMagFilter;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace WanyEssa.Graphics
{
    public static class TextureLoader
    {
        public static int LoadTexture(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Texture file not found: {filePath}");
                return -1;
            }

            try
            {
                using var image = Image.Load<Rgba32>(filePath);
                // Generate texture ID
                int textureId = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2d, textureId);

                // Flip image vertically
                image.Mutate(x => x.Flip(FlipMode.Vertical));

                // Create pixel data array
                int width = image.Width;
                int height = image.Height;
                var pixels = new byte[width * height * 4];

                // Copy pixel data
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var pixel = image[x, y];
                        int index = (y * width + x) * 4;
                        pixels[index] = pixel.R;
                        pixels[index + 1] = pixel.G;
                        pixels[index + 2] = pixel.B;
                        pixels[index + 3] = pixel.A;
                    }
                }

                // Upload to GPU
                GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                try
                {
                    IntPtr pixelsPtr = handle.AddrOfPinnedObject();
                    GL.TexImage2D(
                        TextureTarget.Texture2d,
                        0,
                        InternalFormat.Rgba,
                        width,
                        height,
                        0,
                        PixelFormat.Rgba,
                        PixelType.UnsignedByte,
                        pixelsPtr
                    );
                }
                finally
                {
                    handle.Free();
                }

                // Generate mipmaps
                GL.GenerateMipmap(TextureTarget.Texture2d);

                // Set texture parameters
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                // Unbind texture
                GL.BindTexture(TextureTarget.Texture2d, 0);

                Console.WriteLine($"Texture loaded successfully: {filePath}");
                return textureId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading texture: {ex.Message}");
                return -1;
            }
        }

        public static int LoadCubeMap(string[] facePaths)
        {
            if (facePaths.Length != 6)
            {
                Console.WriteLine("Cube map requires exactly 6 faces");
                return -1;
            }

            try
            {
                int textureId = GL.GenTexture();
                GL.BindTexture(TextureTarget.TextureCubeMap, textureId);

                for (int i = 0; i < 6; i++)
                {
                    if (!File.Exists(facePaths[i]))
                    {
                        Console.WriteLine($"Cube map face not found: {facePaths[i]}");
                        GL.DeleteTexture(textureId);
                        return -1;
                    }

                    using (var image = Image.Load<Rgba32>(facePaths[i]))
                    {
                        // Flip image vertically
                        image.Mutate(x => x.Flip(FlipMode.Vertical));

                        int width = image.Width;
                        int height = image.Height;
                        var pixels = new byte[width * height * 4];

                        // Copy pixel data
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                var pixel = image[x, y];
                                int index = (y * width + x) * 4;
                                pixels[index] = pixel.R;
                                pixels[index + 1] = pixel.G;
                                pixels[index + 2] = pixel.B;
                                pixels[index + 3] = pixel.A;
                            }
                        }

                        // Upload to GPU
                        GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                        try
                        {
                            IntPtr pixelsPtr = handle.AddrOfPinnedObject();
                            GL.TexImage2D(
                                (TextureTarget)((int)TextureTarget.TextureCubeMapPositiveX + i),
                                0,
                                InternalFormat.Rgba,
                                width,
                                height,
                                0,
                                PixelFormat.Rgba,
                                PixelType.UnsignedByte,
                                pixelsPtr
                            );
                        }
                        finally
                        {
                            handle.Free();
                        }
                    }
                }

                // Set texture parameters
                GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                // Generate mipmaps
                GL.GenerateMipmap(TextureTarget.TextureCubeMap);

                // Unbind texture
                GL.BindTexture(TextureTarget.TextureCubeMap, 0);

                Console.WriteLine("Cube map loaded successfully");
                return textureId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading cube map: {ex.Message}");
                return -1;
            }
        }

        public static void UnloadTexture(int textureId)
        {
            if (textureId != -1)
            {
                GL.DeleteTexture(textureId);
            }
        }
    }
}
