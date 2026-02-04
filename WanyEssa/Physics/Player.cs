using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

namespace WanyEssa.Physics
{
    public class Player : PhysicsBody
    {
        public float MoveSpeed { get; set; } = 200.0f;
        public float JumpForce { get; set; } = 5000.0f;
        public bool IsGrounded { get; private set; }
        public bool IsJumping { get; private set; }
        public float JumpTime { get; private set; }
        public float MaxJumpTime { get; set; } = 0.2f;
        
        // Animation states
        public enum PlayerState
        {
            Idle,
            Running,
            Jumping,
            Falling
        }
        
        public PlayerState State { get; private set; }
        public float AnimationTime { get; private set; }
        
        public Player(Vector3 position, float mass = 1.0f)
            : base(position, mass, false)
        {
            State = PlayerState.Idle;
            AnimationTime = 0.0f;
        }
        
        public void Update(GameWindow window, float deltaTime)
        {
            // Handle input
            HandleInput(window, deltaTime);
            
            // Update state
            UpdateState(deltaTime);
            
            // Reset IsGrounded each frame
            IsGrounded = false;
        }
        
        private void HandleInput(GameWindow window, float deltaTime)
        {
            var keyboardState = window.KeyboardState;
            
            // Horizontal movement
            float horizontalInput = 0.0f;
            if (keyboardState.IsKeyDown(Keys.A))
            {
                horizontalInput -= 1.0f;
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                horizontalInput += 1.0f;
            }
            
            // Apply horizontal force
            Vector3 moveForce = new Vector3(horizontalInput * MoveSpeed, 0.0f, 0.0f);
            ApplyForce(moveForce);
            
            // Jump handling
            if (keyboardState.IsKeyDown(Keys.Space))
            {
                if (IsGrounded || JumpTime < MaxJumpTime)
                {
                    JumpTime += deltaTime;
                    IsJumping = true;
                    // Apply jump force
                    Velocity = new Vector3(Velocity.X, 0.0f, Velocity.Z);
                    ApplyForce(new Vector3(0.0f, JumpForce, 0.0f));
                }
            }
            else
            {
                IsJumping = false;
                JumpTime = 0.0f;
            }
        }
        
        private void UpdateState(float deltaTime)
        {
            // Update animation time
            AnimationTime += deltaTime;
            
            // Determine player state based on velocity and input
            if (MathF.Abs(Velocity.X) > 0.1f)
            {
                State = PlayerState.Running;
            }
            else
            {
                State = PlayerState.Idle;
            }
            
            if (Velocity.Y > 0.1f)
            {
                State = PlayerState.Jumping;
            }
            else if (Velocity.Y < -0.1f && !IsGrounded)
            {
                State = PlayerState.Falling;
            }
        }
        
        public void OnCollisionWithGround(float groundY, float playerHeight)
        {
            // Simple ground collision detection
            float playerBottomY = Position.Y - playerHeight * 0.5f;
            
            if (playerBottomY <= groundY + 5.0f && Velocity.Y <= 0.0f)
            {
                IsGrounded = true;
                IsJumping = false;
                JumpTime = 0.0f;
            }
        }
        
        // Override PhysicsBody Update to add player-specific behavior
        public new void Update(float deltaTime)
        {
            base.Update(deltaTime);
            
            // Apply friction when grounded
            if (IsGrounded)
            {
                Velocity = new Vector3(Velocity.X * 0.8f, Velocity.Y, Velocity.Z);
            }
        }
    }
}