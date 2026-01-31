using System;

namespace WanyEssa.Math
{
    public struct Color
    {
        public float R;
        public float G;
        public float B;
        public float A;
        
        public Color(float r, float g, float b, float a = 1.0f)
        {
            // Initialize fields with default values first
            R = 0.0f;
            G = 0.0f;
            B = 0.0f;
            A = 0.0f;
            
            // Then assign the clamped values
            R = Clamp(r, 0.0f, 1.0f);
            G = Clamp(g, 0.0f, 1.0f);
            B = Clamp(b, 0.0f, 1.0f);
            A = Clamp(a, 0.0f, 1.0f);
        }
        
        public static Color White => new(1.0f, 1.0f, 1.0f);
        public static Color Black => new(0.0f, 0.0f, 0.0f);
        public static Color Red => new(1.0f, 0.0f, 0.0f);
        public static Color Green => new(0.0f, 1.0f, 0.0f);
        public static Color Blue => new(0.0f, 0.0f, 1.0f);
        public static Color Yellow => new(1.0f, 1.0f, 0.0f);
        public static Color Magenta => new(1.0f, 0.0f, 1.0f);
        public static Color Cyan => new(0.0f, 1.0f, 1.0f);
        public static Color Gray => new(0.5f, 0.5f, 0.5f);
        public static Color Orange => new(1.0f, 0.647f, 0.0f);
        
        public static Color FromRGBA(byte r, byte g, byte b, byte a = 255)
        {
            return new Color(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
        }
        
        public static Color operator +(Color a, Color b)
        {
            return new Color(
                a.R + b.R,
                a.G + b.G,
                a.B + b.B,
                a.A + b.A
            );
        }
        
        public static Color operator -(Color a, Color b)
        {
            return new Color(
                a.R - b.R,
                a.G - b.G,
                a.B - b.B,
                a.A - b.A
            );
        }
        
        public static Color operator *(Color a, float scalar)
        {
            return new Color(
                a.R * scalar,
                a.G * scalar,
                a.B * scalar,
                a.A * scalar
            );
        }
        
        public override string ToString()
        {
            return $"RGBA({R:F2}, {G:F2}, {B:F2}, {A:F2})";
        }
        
        public static Color Lerp(Color a, Color b, float t)
        {
            t = Clamp(t, 0.0f, 1.0f);
            return new Color(
                a.R + (b.R - a.R) * t,
                a.G + (b.G - a.G) * t,
                a.B + (b.B - a.B) * t,
                a.A + (b.A - a.A) * t
            );
        }
        
        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}