// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Audio;
using Stride.Engine;

namespace Starbreach.Drones
{
    [DataContract]
    public class MissileDroneWeapon : DroneWeapon
    {
        /// <summary>
        /// Number of missiles to spread over the X/Y direction of the missile spawn area defined by <see cref="ArrayExtent"/>
        /// </summary>
        public Int2 ArraySize = new Int2(4,4);

        /// <summary>
        /// X/Y size in one direction (meaning half) of the rectangle to fire missiles from
        /// </summary>
        public Vector2 ArrayExtent = new Vector2(0.3f, 0.2f);

        public float MaximumRange;

        public Prefab ProjectilePrefab;
        
        /// <summary>
        /// Delay after playing animation to wait before shooting actual projectile
        /// </summary>
        public double AnimationDelay = 0.12f;

        /// <summary>
        /// The rotation that the rockets are randomly distibuted around when launching (around the tangent of the aiming direction)
        /// </summary>
        /// <remarks>This is the half angle of a 2D cone this half cone is then randomly rotated [0;2pi] degrees around the aiming direction</remarks>
        public AngleSingle SpreadAngle = new AngleSingle(0.1f, AngleType.Radian);

        private Random random = new Random((int)DateTime.Now.Ticks);
        private AudioEmitterSoundController shootSound;

        public MissileDroneWeapon()
        {
            ReloadTime = 2.0f;
        }

        /// <summary>
        /// Target position of missiles when they have no specific target
        /// </summary>
        public float ShootingRange { get; set; } = 15.0f;

        public override void Init(Drone drone)
        {
            base.Init(drone);

            shootSound = ProjectileSpawnPoint.Get<AudioEmitterComponent>()["Fire"];
        }

        /// <summary>
        /// Spawns rockets in a grid
        /// </summary>
        protected override async Task Shoot(Entity targetEntity)
        {
            // Play shooting animation
            Drone.Animation.Shoot();

            // Wait a bit
            await Task.Delay(TimeSpan.FromSeconds(AnimationDelay));

            List<Projectile> projectiles = new List<Projectile>();
            
            Vector2 stepOffset = new Vector2(1.0f/ArraySize.X, 1.0f/ArraySize.Y) * ArrayExtent;
            
            // Generate random spawn position
            List<Vector2> spawnOffsets = new List<Vector2>();
            for (int y = 0; y < ArraySize.Y; y++)
            {
                float stepY = (ArraySize.Y == 0) ? 0.0f : (y/(float)(ArraySize.Y-1) * 2.0f - 1.0f);
                for (int x = 0; x < ArraySize.X; x++)
                {
                    float stepX = (ArraySize.X == 0) ? 0.0f : (x / (float)(ArraySize.X-1) * 2.0f - 1.0f);
                    Vector2 spawnOffset = new Vector2((stepOffset.X * stepX), (stepOffset.Y * stepY));
                    spawnOffsets.Add(spawnOffset);
                }
            }

            // Spawn in random order
            while(spawnOffsets.Count > 0)
            {
                // Recalculate directions and start position since drone might have moved
                var position = ProjectileSpawnPoint.Transform.WorldMatrix.TranslationVector;
                Vector3 up = Drone.Entity.Transform.WorldMatrix.Up;
                Vector3 aimDirection = Drone.HeadDirection;
                Vector3 right = Vector3.Normalize(Vector3.Cross(up, aimDirection));

                // Retrieve spawn position for this missile based on the offsets
                int targetPositionIndex = random.Next(0, spawnOffsets.Count-1);
                Vector2 spawnOffset = spawnOffsets[targetPositionIndex];
                Vector3 spawnPosition = position + spawnOffset.X * right + spawnOffset.Y * up;
                spawnPosition += aimDirection * ((float)random.NextDouble() - 0.5f) * 0.1f;
                spawnOffsets.RemoveAt(targetPositionIndex);
                
                // Spawn rocket
                var projectileEntity = ProjectilePrefab.Instantiate().Single();
                var projectile = projectileEntity.Get<Projectile>();
                projectile.Owner = Drone.Entity;

                projectiles.Add(projectile);
                projectileEntity.Transform.Position = spawnPosition;
                projectileEntity.Transform.Rotation = Quaternion.BetweenDirections(Vector3.UnitZ, Drone.HeadDirection);
                
                // Random Pitch/Roll (relative to shooting direction)
                Vector2 randomDeviation = new Vector2((float)random.NextDouble(), (float)random.NextDouble());
                randomDeviation.X = randomDeviation.X * SpreadAngle.Radians; // Pitch
                randomDeviation.Y *= MathUtil.TwoPi; // Roll
                projectileEntity.Transform.Rotation = projectileEntity.Transform.Rotation * Quaternion.RotationAxis(right, randomDeviation.X) * Quaternion.RotationAxis(aimDirection, randomDeviation.Y);

                var rocket = (projectile as RocketProjectile);
                if(targetEntity != null)
                    rocket?.SetTarget(targetEntity.Transform.WorldMatrix.TranslationVector);
                else
                    rocket?.SetTarget(position + aimDirection * ShootingRange);

                Drone.SceneSystem.SceneInstance.RootScene.Entities.Add(projectile.Entity);

                // Set initial direction
                Vector3 initialRocketDirection = Vector3.Transform(Vector3.UnitZ, projectileEntity.Transform.Rotation);
                projectile.SetDirection(initialRocketDirection);
                
                shootSound.PlayAndForget();

                await Task.Delay(2+random.Next(0,8));
            }
        }
    }
}