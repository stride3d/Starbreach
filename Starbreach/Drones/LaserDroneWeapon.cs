// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Starbreach.Core;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Audio;
using Stride.Engine;

namespace Starbreach.Drones
{
    [DataContract]
    public class LaserDroneWeapon : DroneWeapon
    {
        public Prefab ProjectilePrefab;

        /// <summary>
        /// Delay after playing animation to wait before shooting actual projectile
        /// </summary>
        public double AnimationDelay = 0.5f;

        private RandomSoundSelector shootSoundSelector;

        public LaserDroneWeapon()
        {
            ReloadTime = 1.0f;
        }

        public override void Init(Drone drone)
        {
            base.Init(drone);

            shootSoundSelector = new RandomSoundSelector(ProjectileSpawnPoint.Get<AudioEmitterComponent>(), "Fire");
        }
        
        protected override async Task Shoot(Entity targetEntity)
        {
            // Play shooting animation
            Drone.Animation.Shoot();

            // Wait a bit
            await Task.Delay(TimeSpan.FromSeconds(AnimationDelay));

            // Spawn projectile
            var projectileEntity = ProjectilePrefab.Instantiate().Single();
            var projectile = projectileEntity.Get<Projectile>();
            projectile.Owner = Drone.Entity;

            projectileEntity.Transform.Position = ProjectileSpawnPoint.Transform.WorldMatrix.TranslationVector;
            projectileEntity.Transform.Rotation = Quaternion.BetweenDirections(Vector3.UnitZ, Drone.HeadDirection);

            Drone.SceneSystem.SceneInstance.RootScene.Entities.Add(projectile.Entity);
            projectile.SetDirection(Drone.HeadDirection);

            shootSoundSelector.PlayAndForget();
        }
    }
}