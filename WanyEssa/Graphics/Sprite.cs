using OpenTK.Mathematics;

namespace WanyEssa.Graphics
{
    public class Sprite
    {
        public Vector2 Position;
        public Vector2 Size;
        public Vector4 Color;
        public float Rotation;
        
        public Sprite()
        {
            Position = Vector2.Zero;
            Size = new Vector2(1.0f, 1.0f);
            Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f); // White
            Rotation = 0.0f;
        }
        
        public Sprite(Vector2 position, Vector2 size, Vector4 color = default)
        {
            Position = position;
            Size = size;
            // Check if color is default by comparing individual components
            bool isDefault = color.X == 0.0f && color.Y == 0.0f && color.Z == 0.0f && color.W == 0.0f;
            Color = isDefault ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f) : color;
            Rotation = 0.0f;
        }
    }
}