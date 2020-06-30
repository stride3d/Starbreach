// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starbreach.Core;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace Starbreach.Drones
{
    public abstract class Projectile : AsyncScript
    {
        public float Impulse { get; set; } = 25.0f;

        public float MaxLifeSpan { get; set; } = 5.0f;

        [DataMemberIgnore]
        public bool Exploding { get; set; }

        public RigidbodyComponent Rigidbody { get; set; }

        private int LifeSpan => (int)(MaxLifeSpan*1000);

        [DataMemberIgnore]
        public Entity Owner { get; set; }

        public void SetDirection(Vector3 direction)
        {
            Rigidbody = Entity.Get<RigidbodyComponent>();
            Rigidbody.AngularFactor = Vector3.Zero;
            Rigidbody.IsTrigger = true;
            Rigidbody.ApplyImpulse(direction*Impulse);
        }

        public override async Task Execute()
        {
            var delay = Game.WaitTime(TimeSpan.FromMilliseconds(LifeSpan));

            // Wait until either the timer expired or a valid collision was found
            while (true)
            {
                var newCollision = NewCollision(Rigidbody);
                await Task.WhenAny(newCollision, delay);
                
                // Was interrupted by delay
                if (delay.IsCompleted)
                    break;

                // Prevent explosion by hitting owner
                if (newCollision.Result.ColliderA.Entity != Owner && newCollision.Result.ColliderB.Entity != Owner)
                    break;

                await Script.NextFrame();
            }

            Rigidbody.ClearForces();
            await Explode();
            Dispose();
        }

        protected abstract Task Explode();

        private static async Task<Collision> NewCollision(RigidbodyComponent component)
        {
            return await component.NewCollision();
        }

        private void Dispose()
        {
            ((Game)Game).SceneSystem.SceneInstance.RootScene.Entities.Remove(Entity);
        }
    }
}