// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Particles;
using Xenko.Particles.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starbreach.VFX
{
    /// <summary>
    /// The <see cref="UpdaterForceField"/> updates the particles' positions and velocity based on proximity and relative position to a bounding force field
    /// </summary>
    [DataContract]
    [Display("Beam Position")]
    public class BeamPostUpdate : ParticleUpdater
    {
        public override bool IsPostUpdater => true;

        public BeamPostUpdate()
        {
            RequiredFields.Add(ParticleFields.Position);
            RequiredFields.Add(ParticleFields.Velocity);
            RequiredFields.Add(ParticleFields.RandomSeed);
        }

        [DataMember(10)]
        [Display("Target")]
        public TransformComponent Target;

        /// <inheritdoc />
        public override unsafe void Update(float dt, ParticlePool pool)
        {
            if (!pool.FieldExists(ParticleFields.Position) || !pool.FieldExists(ParticleFields.Velocity))
                return;

            var posField = pool.GetField(ParticleFields.Position);
            var velField = pool.GetField(ParticleFields.Velocity);
            var lifeField = pool.GetField(ParticleFields.Life);

            var beamAdd = Target?.WorldMatrix.TranslationVector - WorldPosition ?? new Vector3();

            foreach (var particle in pool)
            {
                var remainingLife = (*((float*)particle[lifeField]));
                var lerp = 1.0f - remainingLife;

                // var particlePosition = WorldPosition + beamAdd * lerp;
                // Force contribution to velocity - conserved energy
                //(*((Vector3*)particle[velField])) = particlePosition;

                var desiredPosition = WorldPosition + beamAdd * lerp;
                var desiredOffset = desiredPosition - (*((Vector3*)particle[posField]));

                (*((Vector3*)particle[velField])) += desiredOffset * 3 * (float)Math.Sqrt(lerp);

                lerp *= lerp;

                (*((Vector3*)particle[posField])) += desiredOffset * lerp * lerp;
            }
        }
    }
}