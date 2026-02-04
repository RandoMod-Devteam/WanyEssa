using OpenTK.Mathematics;

namespace WanyEssa.Physics
{
    public abstract class Collider
    {
        public PhysicsBody Body;
        public bool IsTrigger;
        
        protected Collider(PhysicsBody body, bool isTrigger = false)
        {
            Body = body;
            IsTrigger = isTrigger;
        }
        
        public abstract bool CheckCollision(Collider other);
        public abstract void ResolveCollision(Collider other);
    }
    
    public class CircleCollider : Collider
    {
        public float Radius;
        
        public CircleCollider(PhysicsBody body, float radius, bool isTrigger = false)
            : base(body, isTrigger)
        {
            Radius = radius;
        }
        
        public override bool CheckCollision(Collider other)
        {
            if (other is CircleCollider circleOther)
            {
                float distanceSquared = Vector3.DistanceSquared(Body.Position, circleOther.Body.Position);
                float radiusSum = Radius + circleOther.Radius;
                return distanceSquared <= radiusSum * radiusSum;
            }
            
            return false;
        }
        
        public override void ResolveCollision(Collider other)
        {
            if (IsTrigger || other.IsTrigger) return;
            
            if (other is CircleCollider circleOther)
            {
                Vector3 direction = Body.Position - circleOther.Body.Position;
                float distance = direction.Length;
                
                if (distance == 0) return;
                
                Vector3 normal = direction / distance;
                float radiusSum = Radius + circleOther.Radius;
                float penetration = radiusSum - distance;
                
                // Resolve penetration
                if (!Body.IsStatic && !circleOther.Body.IsStatic)
                {
                    Body.Position += normal * (penetration * 0.5f);
                    circleOther.Body.Position -= normal * (penetration * 0.5f);
                }
                else if (!Body.IsStatic)
                {
                    Body.Position += normal * penetration;
                }
                else if (!circleOther.Body.IsStatic)
                {
                    circleOther.Body.Position -= normal * penetration;
                }
                
                // Resolve velocity
                float relativeVelocity = Vector3.Dot(Body.Velocity - circleOther.Body.Velocity, normal);
                if (relativeVelocity > 0) return;
                
                float restitution = 0.8f; // Bounciness
                float impulse = -(1 + restitution) * relativeVelocity;
                
                if (!Body.IsStatic)
                {
                    Body.Velocity += normal * (impulse / Body.Mass);
                }
                if (!circleOther.Body.IsStatic)
                {
                    circleOther.Body.Velocity -= normal * (impulse / circleOther.Body.Mass);
                }
            }
        }
    }
    
    public class BoxCollider : Collider
    {
        public Vector3 Size;
        
        public BoxCollider(PhysicsBody body, Vector3 size, bool isTrigger = false)
            : base(body, isTrigger)
        {
            Size = size;
        }
        
        public override bool CheckCollision(Collider other)
        {
            if (other is CircleCollider circleOther)
            {
                Vector3 closestPoint = new Vector3(
                    Math.Clamp(circleOther.Body.Position.X, Body.Position.X - Size.X / 2, Body.Position.X + Size.X / 2),
                    Math.Clamp(circleOther.Body.Position.Y, Body.Position.Y - Size.Y / 2, Body.Position.Y + Size.Y / 2),
                    Math.Clamp(circleOther.Body.Position.Z, Body.Position.Z - Size.Z / 2, Body.Position.Z + Size.Z / 2)
                );
                
                float distanceSquared = Vector3.DistanceSquared(circleOther.Body.Position, closestPoint);
                return distanceSquared <= circleOther.Radius * circleOther.Radius;
            }
            else if (other is BoxCollider boxOther)
            {
                Vector3 aMin = Body.Position - Size / 2;
                Vector3 aMax = Body.Position + Size / 2;
                Vector3 bMin = boxOther.Body.Position - boxOther.Size / 2;
                Vector3 bMax = boxOther.Body.Position + boxOther.Size / 2;
                
                return aMin.X <= bMax.X && aMax.X >= bMin.X &&
                       aMin.Y <= bMax.Y && aMax.Y >= bMin.Y &&
                       aMin.Z <= bMax.Z && aMax.Z >= bMin.Z;
            }
            
            return false;
        }
        
        public override void ResolveCollision(Collider other)
        {
            // Basic collision resolution for boxes will be implemented later if needed
        }
    }
}