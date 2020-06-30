// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Starbreach.Core;
using Stride.Core.Mathematics;
using Stride.Audio;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Particles.Components;
using Stride.Physics;
using Stride.Particles;
using Stride.Particles.Spawners;

namespace Starbreach.Drones
{
    /// <summary>
    /// Controls drone rockets
    /// </summary>
    public class RocketProjectile : Projectile
    {
        public ParticleSystemComponent ExplosionParticle;

        public ParticleSystemComponent SmokeParticle;

        public float AoE { get; set; } = 2.0f;

        public float Damage { get; set; } = 30.0f;

        private AudioEmitterSoundController explodeSound;
        private RigidbodyComponent sensor;

        public override void Cancel()
        {
            ExplosionParticle?.ParticleSystem?.Dispose();
            ExplosionParticle = null;
            SmokeParticle?.ParticleSystem?.Dispose();
            SmokeParticle = null;
        }

        public void SetTarget(Vector3 target)
        {
            var homing = Entity.Get<ProjectileHoming>();
            if (homing == null)
            {
                homing = new ProjectileHoming();
                Entity.Add(homing);
            }
            homing.TargetPosition = target;
        }

        public override async Task Execute()
        {
            sensor = Entity.FindChild("AoESensor").Get<RigidbodyComponent>();
            explodeSound = Entity.Get<AudioEmitterComponent>()["Explode"];

            await base.Execute();
        }

        protected override async Task Explode()
        {
            var model = Entity.Get<ModelComponent>();
            model.Enabled = false;

            ExplosionParticle.Enabled = true;

            //stop spawning smoke particles
            SmokeParticle.ParticleSystem.StopEmitters();

            //find what we hit
            foreach (var collision in sensor.Collisions)
            {
                var collider = collision.ColliderA == sensor ? collision.ColliderB : collision.ColliderA;
                var damagedEntity = Utils.GetDestructible(collider.Entity);
                if (damagedEntity == null || damagedEntity.IsDead) continue;

                // Don't damage other drones
                if (damagedEntity is Drone) continue;

                var firstContact = collision.Contacts.First();

                var damage = (int)((0.25f + 0.75f*Math.Abs(firstContact.Distance)/AoE)*Damage);
                damagedEntity.Damage(damage);
            }

            explodeSound.PlayAndForget();

            // Wait before Destroying the rocket
            await Task.Delay(2000);
        }
    }
}