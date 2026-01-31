using System;

namespace WanyEssa.Math
{
    public struct Vector2
    {
        public float X;
        public float Y;
        
        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }
        
        public static Vector2 Zero => new(0.0f, 0.0f);
        public static Vector2 One => new(1.0f, 1.0f);
        public static Vector2 Up => new(0.0f, 1.0f);
        public static Vector2 Down => new(0.0f, -1.0f);
        public static Vector2 Left => new(-1.0f, 0.0f);
        public static Vector2 Right => new(1.0f, 0.0f);
        
        public float Length => (float)System.Math.Sqrt(X * X + Y * Y);
        public float LengthSquared => X * X + Y * Y;
        public float Magnitude => Length; // Alias for Length
        public float MagnitudeSquared => LengthSquared; // Alias for LengthSquared
        
        public Vector2 Normalized
        {
            get
            {
                float length = Length;
                if (length > 0.0f)
                {
                    return new Vector2(X / length, Y / length);
                }
                return Zero;
            }
        }
        
        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X + b.X, a.Y + b.Y);
        }
        
        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X - b.X, a.Y - b.Y);
        }
        
        public static Vector2 operator *(Vector2 a, float scalar)
        {
            return new Vector2(a.X * scalar, a.Y * scalar);
        }
        
        public static Vector2 operator /(Vector2 a, float scalar)
        {
            if (scalar != 0.0f)
            {
                return new Vector2(a.X / scalar, a.Y / scalar);
            }
            return Zero;
        }
        
        public static bool operator ==(Vector2 a, Vector2 b)
        {
            return a.X == b.X && a.Y == b.Y;
        }
        
        public static bool operator !=(Vector2 a, Vector2 b)
        {
            return a.X != b.X || a.Y != b.Y;
        }
        
        public static float Dot(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }
        
        public static float Distance(Vector2 a, Vector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)System.Math.Sqrt(dx * dx + dy * dy);
        }
        
        public static float DistanceSquared(Vector2 a, Vector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }
        
        public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max)
        {
            return new Vector2(
                System.Math.Clamp(value.X, min.X, max.X),
                System.Math.Clamp(value.Y, min.Y, max.Y)
            );
        }
        
        public override bool Equals(object? obj)
        {
            if (obj is Vector2 other)
            {
                return this == other;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
        
        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}