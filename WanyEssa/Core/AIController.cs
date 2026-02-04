using OpenTK.Mathematics;
using WanyEssa.Physics;
using WanyEssa.Graphics;

namespace WanyEssa.Core
{
    public enum AIState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Dead
    }
    
    public class AIController
    {
        private PhysicsBody _physicsBody;
        private Weapon _weapon;
        private Camera _camera;
        private PlayerController _player;
        private AIState _currentState;
        private List<Vector3> _patrolPoints;
        private int _currentPatrolIndex;
        private float _moveSpeed;
        private float _rotationSpeed;
        private float _aggroRange;
        private float _attackRange;
        private float _fieldOfView;
        private bool _isDead;
        private float _health;
        
        public PhysicsBody PhysicsBody => _physicsBody;
        public Weapon Weapon => _weapon;
        public Camera Camera => _camera;
        public AIState CurrentState => _currentState;
        public bool IsDead => _isDead;
        public float Health => _health;
        
        public AIController(Vector3 position, PlayerController player, PhysicsWorld physicsWorld)
        {
            _physicsBody = new PhysicsBody(position, 1.0f, false);
            physicsWorld.AddBody(_physicsBody);
            _camera = new Camera(800, 600, position);
            _player = player;
            _currentState = AIState.Idle;
            _patrolPoints = new List<Vector3>();
            _currentPatrolIndex = 0;
            _moveSpeed = 2.0f;
            _rotationSpeed = 5.0f;
            _aggroRange = 20.0f;
            _attackRange = 10.0f;
            _fieldOfView = 90.0f;
            _isDead = false;
            _health = 100.0f;
            
            // Give AI a basic weapon
            _weapon = Weapon.CreatePistol();
        }
        
        public void AddPatrolPoint(Vector3 point)
        {
            _patrolPoints.Add(point);
            if (_currentState == AIState.Idle && _patrolPoints.Count > 0)
            {
                _currentState = AIState.Patrol;
            }
        }
        
        public void Update(float deltaTime)
        {
            if (_isDead)
            {
                _currentState = AIState.Dead;
                return;
            }
            
            // Check player distance
            float distanceToPlayerSquared = Vector3.DistanceSquared(_physicsBody.Position, _player.PhysicsBody.Position);
            
            // Check if player is in field of view
            bool playerInSight = IsPlayerInSight();
            
            // State machine
            switch (_currentState)
            {
                case AIState.Idle:
                    HandleIdleState(deltaTime);
                    break;
                case AIState.Patrol:
                    HandlePatrolState(deltaTime);
                    break;
                case AIState.Chase:
                    HandleChaseState(deltaTime);
                    break;
                case AIState.Attack:
                    HandleAttackState(deltaTime);
                    break;
            }
            
            // Transition states based on player distance and visibility
            if (playerInSight && distanceToPlayerSquared <= _aggroRange * _aggroRange)
            {
                if (distanceToPlayerSquared <= _attackRange * _attackRange)
                {
                    _currentState = AIState.Attack;
                }
                else
                {
                    _currentState = AIState.Chase;
                }
            }
            else if (_currentState == AIState.Chase || _currentState == AIState.Attack)
            {
                if (_patrolPoints.Count > 0)
                {
                    _currentState = AIState.Patrol;
                }
                else
                {
                    _currentState = AIState.Idle;
                }
            }
            
            // Update weapon
            if (_weapon != null)
            {
                _weapon.Update(deltaTime);
                
                // Position weapon relative to AI
                _weapon.Position = _physicsBody.Position + _camera.Forward * 0.5f - _camera.Up * 0.2f;
                _weapon.Rotation = new Vector3(_camera.Pitch, _camera.Yaw, 0);
            }
        }
        
        private void HandleIdleState(float deltaTime)
        {
            // Do nothing in idle state
        }
        
        private void HandlePatrolState(float deltaTime)
        {
            if (_patrolPoints.Count == 0)
            {
                _currentState = AIState.Idle;
                return;
            }
            
            Vector3 targetPoint = _patrolPoints[_currentPatrolIndex];
            Vector3 direction = (targetPoint - _physicsBody.Position).Normalized();
            float distanceSquared = Vector3.DistanceSquared(_physicsBody.Position, targetPoint);
            
            if (distanceSquared > 0.5f)
            {
                // Move towards patrol point
                _physicsBody.ApplyForce(direction * _moveSpeed * 100);
                
                // Rotate towards patrol point
                RotateTowards(direction, deltaTime);
            }
            else
            {
                // Move to next patrol point
                _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Count;
            }
        }
        
        private void HandleChaseState(float deltaTime)
        {
            Vector3 direction = (_player.PhysicsBody.Position - _physicsBody.Position).Normalized();
            
            // Move towards player
            _physicsBody.ApplyForce(direction * _moveSpeed * 1.5f * 100);
            
            // Rotate towards player
            RotateTowards(direction, deltaTime);
        }
        
        private void HandleAttackState(float deltaTime)
        {
            Vector3 direction = (_player.PhysicsBody.Position - _physicsBody.Position).Normalized();
            
            // Rotate towards player
            RotateTowards(direction, deltaTime);
            
            // Fire weapon if in range and can fire
            if (_weapon != null && _weapon.CanFire)
            {
                Vector3 hitPoint;
                _weapon.Fire(_physicsBody.Position + _camera.Forward * 0.5f, _camera.Forward, out hitPoint);
            }
        }
        
        private void RotateTowards(Vector3 direction, float deltaTime)
        {
            // Calculate desired yaw and pitch
            float desiredYaw = MathF.Atan2(direction.X, direction.Z) * 180.0f / MathF.PI;
            float desiredPitch = MathF.Asin(direction.Y) * 180.0f / MathF.PI;
            
            // Smoothly rotate towards desired angles
            _camera.Yaw = OpenTK.Mathematics.MathHelper.Lerp(_camera.Yaw, desiredYaw, _rotationSpeed * deltaTime);
            _camera.Pitch = OpenTK.Mathematics.MathHelper.Lerp(_camera.Pitch, desiredPitch, _rotationSpeed * deltaTime);
        }
        
        private bool IsPlayerInSight()
        {
            Vector3 directionToPlayer = (_player.PhysicsBody.Position - _physicsBody.Position).Normalized();
            float angle = MathF.Acos(Vector3.Dot(_camera.Forward, directionToPlayer)) * 180.0f / MathF.PI;
            
            return angle <= _fieldOfView / 2.0f;
        }
        
        public void TakeDamage(float damage)
        {
            _health -= damage;
            if (_health <= 0)
            {
                _isDead = true;
                _currentState = AIState.Dead;
            }
        }
        
        public void Draw(Renderer renderer)
        {
            if (!_isDead && _weapon != null)
            {
                _weapon.Draw(renderer, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            }
        }
    }
    

}