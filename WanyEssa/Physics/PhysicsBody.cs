using OpenTK.Mathematics;

namespace WanyEssa.Physics
{
    public class PhysicsBody
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Acceleration;
        public float Mass;
        public bool IsStatic;
        public float Drag;
        
        public PhysicsBody(Vector3 position, float mass = 1.0f, bool isStatic = false)
        {
            Position = position;
            Velocity = OpenTK.Mathematics.Vector3.Zero;
            Acceleration = OpenTK.Mathematics.Vector3.Zero;
            Mass = mass;
            IsStatic = isStatic;
            Drag = 0.99f;
        }
        
        public void ApplyForce(Vector3 force)
        {
            if (IsStatic) return;
            
            Acceleration += force / Mass;
        }
        
        public void Update(float deltaTime)
        {
            if (IsStatic) return;
            
            Velocity += Acceleration * deltaTime;
            Velocity *= Drag;
            Position += Velocity * deltaTime;
            
            // Reset acceleration for next frame
            Acceleration = Vector3.Zero;
        }
    }
}