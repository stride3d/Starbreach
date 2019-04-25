// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Mathematics;
using Xenko.Audio;
using Xenko.Engine;
using Xenko.Physics;

namespace Starbreach.VFX
{
    public class DroneExplosion : AsyncScript
    {
        protected AudioEmitterSoundController explosionSound;

        public override async Task Execute()
        {
            // Play explosion sound
            AudioEmitterComponent audioEmitter = Entity.Get<AudioEmitterComponent>();
            explosionSound = audioEmitter["Explosion"];
            explosionSound.PlayAndForget();

            Entity.Transform.UpdateWorldMatrix();

            var model = Entity.Get<ModelComponent>();

            // Scatter all the Rigidbody parts of the scattered drone model
            var fracturedRigidBodies = Entity.GetAll<RigidbodyComponent>();
            Vector3 explosionCenter = Entity.Transform.WorldMatrix.TranslationVector;
            Random explosionRandom = new Random();
            foreach (var fragment in fracturedRigidBodies)
            {
                Vector3 dir = fragment.PhysicsWorldTransform.TranslationVector - explosionCenter;
                dir.Normalize();

                fragment.IsKinematic = false;
                if (model.Skeleton.Nodes[fragment.BoneIndex].Name != "Drone_D_part_015")
                {
                    fragment.ApplyTorqueImpulse(-dir*(float) (explosionRandom.NextDouble()*0.2f));
                    fragment.ApplyImpulse(dir * (float)(explosionRandom.NextDouble() * 2.5f + 2.5f));
                }
            }

            await Task.Delay(30000);

            // Despawn after a while to clean up drone parts
            SceneSystem.SceneInstance.RootScene.Entities.Remove(Entity);
        }
    }
}
