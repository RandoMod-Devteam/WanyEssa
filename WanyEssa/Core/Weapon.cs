using System;
using System.Collections.Generic;
using WanyEssa.Math;
using WanyEssa.Graphics;
using WanyEssa.Audio;

namespace WanyEssa.Core
{
    public class Weapon
    {
        public string Name { get; set; }
        public float Damage { get; set; }
        public float FireRate { get; set; } // Shots per second
        public float ReloadTime { get; set; }
        public int MagazineSize { get; set; }
        public int AmmoCount { get; set; }
        public float Accuracy { get; set; } // 0.0 - 1.0, where 1.0 is perfect accuracy
        public float Range { get; set; }
        public float Recoil { get; set; }
        public float AimDownSightsFov { get; set; }
        public bool IsAutomatic { get; set; }
        
        private float _timeSinceLastShot;
        private bool _isReloading;
        private float _reloadTimer;
        private bool _isAiming;
        private List<ParticleSystem> _particleSystems;
        
        public Mesh? Mesh { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; } = new Vector3(1, 1, 1);
        
        public bool CanFire => !_isReloading && AmmoCount > 0 && _timeSinceLastShot >= 1.0f / FireRate;
        public bool IsReloading => _isReloading;
        public bool IsAiming => _isAiming;
        
        public Weapon(string name, float damage, float fireRate, float reloadTime, int magazineSize, float accuracy, float range, float recoil)
        {
            Name = name;
            Damage = damage;
            FireRate = fireRate;
            ReloadTime = reloadTime;
            MagazineSize = magazineSize;
            AmmoCount = magazineSize;
            Accuracy = accuracy;
            Range = range;
            Recoil = recoil;
            AimDownSightsFov = 45.0f;
            IsAutomatic = false;
            
            _timeSinceLastShot = 0.0f;
            _isReloading = false;
            _reloadTimer = 0.0f;
            _isAiming = false;
            _particleSystems = new List<ParticleSystem>();
            
            // Initialize Mesh with default value
            Mesh = null;
        }
        
        public void Update(float deltaTime)
        {
            _timeSinceLastShot += deltaTime;
            
            if (_isReloading)
            {
                _reloadTimer += deltaTime;
                if (_reloadTimer >= ReloadTime)
                {
                    AmmoCount = MagazineSize;
                    _isReloading = false;
                    _reloadTimer = 0.0f;
                }
            }
            
            // Update particle systems
            for (int i = _particleSystems.Count - 1; i >= 0; i--)
            {
                ParticleSystem system = _particleSystems[i];
                system.Update(deltaTime);
                
                if (!system.IsEmitting && system.ActiveParticles == 0)
                {
                    _particleSystems.RemoveAt(i);
                }
            }
        }
        
        public bool Fire(Vector3 origin, Vector3 direction, out Vector3 hitPoint)
        {
            hitPoint = Vector3.Zero;
            
            if (!CanFire)
                return false;
            
            AmmoCount--;
            _timeSinceLastShot = 0.0f;
            
            // Apply accuracy
            Vector3 fireDirection = direction;
            if (Accuracy < 1.0f)
            {
                float spread = (1.0f - Accuracy) * 5.0f; // 5 degrees max spread
                fireDirection = Vector3.RandomDirection(fireDirection, spread);
            }
            
            // Calculate hit point (simple raycast for now)
            hitPoint = origin + fireDirection * Range;
            
            // Play fire sound
            SoundManager.Instance.PlaySound($"{Name}_fire");
            
            // Create muzzle flash particle system
            ParticleSystem muzzleFlash = ParticlePresets.CreateMuzzleFlash(origin, direction);
            _particleSystems.Add(muzzleFlash);
            
            // Create impact effect if we hit something
            if (true) // In a real game, you would check if we actually hit something
            {
                ParticleSystem impact = ParticlePresets.CreateImpactEffect(hitPoint, direction);
                _particleSystems.Add(impact);
            }
            
            // Check if we need to reload
            if (AmmoCount <= 0)
            {
                Reload();
            }
            
            return true;
        }
        
        public void Reload()
        {
            if (!_isReloading && AmmoCount < MagazineSize)
            {
                _isReloading = true;
                _reloadTimer = 0.0f;
                // Play reload sound
                SoundManager.Instance.PlaySound($"{Name}_reload");
            }
        }
        
        public void ToggleAim()
        {
            _isAiming = !_isAiming;
        }
        
        public void SetAiming(bool aiming)
        {
            _isAiming = aiming;
        }
        
        public void Draw(Renderer renderer, Color color)
        {
            if (Mesh != null)
            {
                Mesh.Position = Position;
                Mesh.Rotation = Rotation;
                Mesh.Scale = Scale;
                renderer.DrawMesh(Mesh, color);
            }
            
            // Draw particle systems
            foreach (ParticleSystem system in _particleSystems)
            {
                system.Draw(renderer);
            }
        }
        
        // Static weapon presets
        public static Weapon CreatePistol()
        {
            Weapon pistol = new Weapon(
                "Pistol",
                25.0f,
                4.0f, // 4 shots per second
                1.5f, // 1.5 second reload
                12,
                0.95f, // 95% accuracy
                50.0f,
                0.5f
            );
            pistol.IsAutomatic = false;
            pistol.AimDownSightsFov = 60.0f;
            return pistol;
        }
        
        public static Weapon CreateAssaultRifle()
        {
            Weapon rifle = new Weapon(
                "Assault Rifle",
                30.0f,
                8.0f, // 8 shots per second
                2.0f, // 2 second reload
                30,
                0.85f, // 85% accuracy
                100.0f,
                1.0f
            );
            rifle.IsAutomatic = true;
            rifle.AimDownSightsFov = 50.0f;
            return rifle;
        }
        
        public static Weapon CreateShotgun()
        {
            Weapon shotgun = new Weapon(
                "Shotgun",
                80.0f,
                1.0f, // 1 shot per second
                2.5f, // 2.5 second reload
                8,
                0.75f, // 75% accuracy
                30.0f,
                2.0f
            );
            shotgun.IsAutomatic = false;
            shotgun.AimDownSightsFov = 65.0f;
            return shotgun;
        }
        
        public static Weapon CreateSniperRifle()
        {
            Weapon sniper = new Weapon(
                "Sniper Rifle",
                100.0f,
                0.5f, // 1 shot every 2 seconds
                3.0f, // 3 second reload
                5,
                0.99f, // 99% accuracy
                200.0f,
                1.5f
            );
            sniper.IsAutomatic = false;
            sniper.AimDownSightsFov = 30.0f;
            return sniper;
        }
        
        public static Weapon CreateSubmachineGun()
        {
            Weapon smg = new Weapon(
                "Submachine Gun",
                20.0f,
                10.0f, // 10 shots per second
                1.8f, // 1.8 second reload
                40,
                0.80f, // 80% accuracy
                75.0f,
                0.8f
            );
            smg.IsAutomatic = true;
            smg.AimDownSightsFov = 55.0f;
            return smg;
        }
        
        public static Weapon CreateGrenadeLauncher()
        {
            Weapon launcher = new Weapon(
                "Grenade Launcher",
                150.0f,
                1.0f, // 1 shot per second
                2.5f, // 2.5 second reload
                6,
                0.75f, // 75% accuracy
                100.0f,
                2.0f
            );
            launcher.IsAutomatic = false;
            launcher.AimDownSightsFov = 60.0f;
            return launcher;
        }
        
        public static Weapon CreateKnife()
        {
            Weapon knife = new Weapon(
                "Knife",
                50.0f,
                2.0f, // 2 swings per second
                0.0f, // No reload
                int.MaxValue, // Infinite ammo
                1.0f, // Perfect accuracy
                2.0f,
                0.2f
            );
            knife.IsAutomatic = false;
            knife.AimDownSightsFov = 75.0f; // No aim down sights
            return knife;
        }
    }
}