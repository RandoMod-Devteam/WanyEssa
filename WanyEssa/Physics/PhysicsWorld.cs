using System.Collections.Generic;
using WanyEssa.Math;

namespace WanyEssa.Physics
{
    public class PhysicsWorld
    {
        private List<PhysicsBody> _bodies;
        private List<Collider> _colliders;
        private Vector3 _gravity;
        private float _timeStep;
        
        public Vector3 Gravity
        {
            get => _gravity;
            set => _gravity = value;
        }
        
        public PhysicsWorld(float timeStep = 1.0f / 60.0f)
        {
            _bodies = new List<PhysicsBody>();
            _colliders = new List<Collider>();
            _gravity = new Vector3(0, -9.81f, 0); // Earth gravity
            _timeStep = timeStep;
        }
        
        public void AddBody(PhysicsBody body)
        {
            _bodies.Add(body);
        }
        
        public void RemoveBody(PhysicsBody body)
        {
            _bodies.Remove(body);
        }
        
        public void AddCollider(Collider collider)
        {
            _colliders.Add(collider);
        }
        
        public void RemoveCollider(Collider collider)
        {
            _colliders.Remove(collider);
        }
        
        public void Update(float deltaTime)
        {
            foreach (var body in _bodies)
            {
                if (!body.IsStatic)
                {
                    // Apply gravity
                    body.ApplyForce(_gravity * body.Mass);
                    
                    // Update body
                    body.Update(deltaTime);
                }
            }
            
            // Check for collisions
            CheckCollisions();
        }
        
        private void CheckCollisions()
        {
            // Simple collision detection for now
            // In a real implementation, you would use a more efficient algorithm like spatial partitioning
            for (int i = 0; i < _colliders.Count; i++)
            {
                for (int j = i + 1; j < _colliders.Count; j++)
                {
                    Collider colliderA = _colliders[i];
                    Collider colliderB = _colliders[j];
                    
                    // Skip if both are static
                    if (colliderA.Body.IsStatic && colliderB.Body.IsStatic)
                        continue;
                    
                    // Check collision
                    if (colliderA.CheckCollision(colliderB))
                    {
                        // Resolve collision
                        colliderA.ResolveCollision(colliderB);
                    }
                }
            }
        }
        
        public void Clear()
        {
            _bodies.Clear();
            _colliders.Clear();
        }
    }
}