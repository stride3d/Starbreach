// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Starbreach.Core;
using Xenko.Engine;
using Xenko.Particles.Components;
using Xenko.Physics;

namespace Starbreach.Drones
{
    public class LaserProjectile : Projectile
    {
        public ParticleSystemComponent ExplosionParticle;

        public ParticleSystemComponent TrailParticle;
        
        public int Damage { get; set; } = 30;

        private RandomSoundSelector explosionSoundSelector;

        public override void Cancel()
        {
            ExplosionParticle?.ParticleSystem?.Dispose();
            ExplosionParticle = null;
            TrailParticle?.ParticleSystem?.Dispose();
            TrailParticle = null;
        }

        public override async Task Execute()
        {
            explosionSoundSelector = new RandomSoundSelector(Entity.Get<AudioEmitterComponent>(), "Explode");

            await base.Execute();
        }

        protected override async Task Explode()
        {
            var model = Entity.Get<SpriteComponent>();
            model.Enabled = false;

            ExplosionParticle.Enabled = true;

            //stop spawning smoke particles
            TrailParticle.ParticleSystem.StopEmitters();

            // Damage all hit entities
            foreach (var collision in Rigidbody.Collisions)
            {
                var target = collision.ColliderA;
                if (target == Rigidbody) // Swap
                    target = collision.ColliderB;
                var destructible = Utils.GetDestructible(target.Entity);
                destructible?.Damage(Damage);
            }

            explosionSoundSelector.PlayAndForget();

            // Wait before Destroying the projectile
            await Task.Delay(2000);
        }
    }
}