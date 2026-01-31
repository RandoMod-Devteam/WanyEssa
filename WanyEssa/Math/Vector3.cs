using System;

namespace WanyEssa.Math
{
    public struct Vector3
    {
        public float X;
        public float Y;
        public float Z;
        
        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public static Vector3 Zero => new(0.0f, 0.0f, 0.0f);
        public static Vector3 One => new(1.0f, 1.0f, 1.0f);
        public static Vector3 Up => new(0.0f, 1.0f, 0.0f);
        public static Vector3 Down => new(0.0f, -1.0f, 0.0f);
        public static Vector3 Left => new(-1.0f, 0.0f, 0.0f);
        public static Vector3 Right => new(1.0f, 0.0f, 0.0f);
        public static Vector3 Forward => new(0.0f, 0.0f, 1.0f);
        public static Vector3 Backward => new(0.0f, 0.0f, -1.0f);
        
        public float Length => (float)System.Math.Sqrt(X * X + Y * Y + Z * Z);
        public float LengthSquared => X * X + Y * Y + Z * Z;
        
        public Vector3 Normalized
        {
            get
            {
                float length = Length;
                if (length > 0.0f)
                {
                    return new Vector3(X / length, Y / length, Z / length);
                }
                return Zero;
            }
        }
        
        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
        
        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }
        
        public static Vector3 operator *(Vector3 a, float scalar)
        {
            return new Vector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
        }
        
        public static Vector3 operator /(Vector3 a, float scalar)
        {
            if (scalar != 0.0f)
            {
                return new Vector3(a.X / scalar, a.Y / scalar, a.Z / scalar);
            }
            return Zero;
        }
        
        public static Vector3 operator -(Vector3 a)
        {
            return new Vector3(-a.X, -a.Y, -a.Z);
        }
        
        public static float Dot(Vector3 a, Vector3 b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }
        
        public static Vector3 Cross(Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X
            );
        }
        
        public static float Distance(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        
        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
        
        public static Vector3 RandomDirection(Vector3 direction, float spreadDegrees)
        {
            Random random = new Random();
            float spreadRadians = (float)System.Math.PI * spreadDegrees / 180.0f;
            
            // Generate random angles
            float theta = (float)random.NextDouble() * 2.0f * (float)System.Math.PI;
            float phi = (float)System.Math.Asin((float)random.NextDouble() * 2.0f - 1.0f);
            
            // Scale by spread
            phi *= spreadRadians;
            
            // Create random direction vector
            Vector3 randomDir = new(
                (float)(System.Math.Cos(theta) * System.Math.Cos(phi)),
                (float)System.Math.Sin(phi),
                (float)(System.Math.Sin(theta) * System.Math.Cos(phi))
            );
            
            // Create a rotation matrix to align with the original direction
            Vector3 up = Vector3.Up;
            if (System.Math.Abs(Vector3.Dot(direction, up)) > 0.999f)
            {
                up = Vector3.Forward;
            }
            
            Vector3 right = Vector3.Cross(direction, up).Normalized;
            up = Vector3.Cross(right, direction).Normalized;
            
            // Rotate random direction to align with original direction
            Vector3 rotatedDir = new Vector3(
                randomDir.X * right.X + randomDir.Y * direction.X + randomDir.Z * up.X,
                randomDir.X * right.Y + randomDir.Y * direction.Y + randomDir.Z * up.Y,
                randomDir.X * right.Z + randomDir.Y * direction.Z + randomDir.Z * up.Z
            );
            
            return rotatedDir.Normalized;
        }
        
        public static Vector3 RotateAround(Vector3 vector, Vector3 axis, float angleDegrees)
        {
            float angleRadians = (float)System.Math.PI * angleDegrees / 180.0f;
            float cos = (float)System.Math.Cos(angleRadians);
            float sin = (float)System.Math.Sin(angleRadians);
            float oneMinusCos = 1.0f - cos;
            
            Vector3 normalizedAxis = axis.Normalized;
            
            // Rodrigues' rotation formula
            float x = vector.X * (cos + normalizedAxis.X * normalizedAxis.X * oneMinusCos) +
                      vector.Y * (normalizedAxis.X * normalizedAxis.Y * oneMinusCos - normalizedAxis.Z * sin) +
                      vector.Z * (normalizedAxis.X * normalizedAxis.Z * oneMinusCos + normalizedAxis.Y * sin);
            
            float y = vector.X * (normalizedAxis.Y * normalizedAxis.X * oneMinusCos + normalizedAxis.Z * sin) +
                      vector.Y * (cos + normalizedAxis.Y * normalizedAxis.Y * oneMinusCos) +
                      vector.Z * (normalizedAxis.Y * normalizedAxis.Z * oneMinusCos - normalizedAxis.X * sin);
            
            float z = vector.X * (normalizedAxis.Z * normalizedAxis.X * oneMinusCos - normalizedAxis.Y * sin) +
                      vector.Y * (normalizedAxis.Z * normalizedAxis.Y * oneMinusCos + normalizedAxis.X * sin) +
                      vector.Z * (cos + normalizedAxis.Z * normalizedAxis.Z * oneMinusCos);
            
            return new Vector3(x, y, z);
        }
        
        public static bool operator ==(Vector3 a, Vector3 b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }
        
        public static bool operator !=(Vector3 a, Vector3 b)
        {
            return !(a == b);
        }
        
        public override bool Equals(object? obj)
        {
            if (obj is Vector3 vector)
            {
                return this == vector;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return (X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode());
        }
    }
}