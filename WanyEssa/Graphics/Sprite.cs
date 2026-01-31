using WanyEssa.Math;

namespace WanyEssa.Graphics
{
    public class Sprite
    {
        public Vector2 Position;
        public Vector2 Size;
        public Color Color;
        public float Rotation;
        
        public Sprite()
        {
            Position = Vector2.Zero;
            Size = new Vector2(1.0f, 1.0f);
            Color = Color.White;
            Rotation = 0.0f;
        }
        
        public Sprite(Vector2 position, Vector2 size, Color color = default(Color))
        {
            Position = position;
            Size = size;
            // Check if color is default by comparing individual components
            bool isDefault = color.R == 0.0f && color.G == 0.0f && color.B == 0.0f && color.A == 0.0f;
            Color = isDefault ? Color.White : color;
            Rotation = 0.0f;
        }
    }
}