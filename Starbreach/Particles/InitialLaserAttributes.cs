// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Particles;
using Stride.Particles.Initializers;

namespace Starbreach.Particles
{
    /// <summary>
    /// Initializer which sets the initial velocity AND life for particles based on distance to a target point
    /// </summary>
    [DataContract]
    [Display("Initial Laser Attributes")]
    public class InitialLaserAttributes : ParticleInitializer
    {
        public InitialLaserAttributes()
        {
            RequiredFields.Add(ParticleFields.Velocity);
            RequiredFields.Add(ParticleFields.RandomSeed);
            RequiredFields.Add(ParticleFields.Life);

            DisplayParticleRotation = true;
            DisplayParticleScaleUniform = true;
        }

        public override unsafe void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
        {
            if (!pool.FieldExists(ParticleFields.Velocity) || !pool.FieldExists(ParticleFields.RandomSeed))
                return;

            var velField = pool.GetField(ParticleFields.Velocity);
            var rndField = pool.GetField(ParticleFields.RandomSeed);
            var lifeField = pool.GetField(ParticleFields.Life);

            var targetVelocityAdd = Target?.WorldMatrix.TranslationVector - WorldPosition ?? FallbackTarget;
            var distance = (targetVelocityAdd.Length() + 0.0001f) / MaxParticleLife;
            targetVelocityAdd.Normalize();

            var i = startIdx;
            while (i != endIdx)
            {
                var particle = pool.FromIndex(i);
                var randSeed = particle.Get(rndField);

                var speedFactor = randSeed.GetFloat(RandomOffset.Offset1A + SeedOffset) * (VelocityMax - VelocityMin) + VelocityMin;
                var particleRandVel = speedFactor * targetVelocityAdd;
                (*((Vector3*)particle[velField])) = particleRandVel;

                var particleLife = distance / (particleRandVel.Length() + 0.0001f);
                particleLife = Math.Min(1f, particleLife);
                (*((float*)particle[lifeField])) = particleLife;

                i = (i + 1) % maxCapacity;
            }
        }

        /// <summary>
        /// The seed offset used to match or separate random values
        /// </summary>
        /// <userdoc>
        /// The seed offset used to match or separate random values
        /// </userdoc>
        [DataMember(8)]
        [Display("Random Seed")]
        public uint SeedOffset { get; set; } = 0;

        /// <summary>
        /// An arc initializer needs a second point so that it can position the particles in a line or arc between two locators
        /// </summary>
        /// <userdoc>
        /// An arc initializer needs a second point so that it can position the particles in a line or arc between two locators
        /// </userdoc>
        [DataMember(10)]
        [Display("Target")]
        public TransformComponent Target;

        /// <summary>
        /// In case the <see cref="Target"/> is null, the <see cref="FallbackTarget"/> offset will be used
        /// </summary>
        /// <userdoc>
        /// In case the Target is null, the FallbackTarget offset will be used
        /// </userdoc>
        [DataMember(12)]
        [Display("Fallback Target")]
        public Vector3 FallbackTarget = new Vector3(0, 0, -1);

        /// <summary>
        /// Lower velocity value
        /// </summary>
        /// <userdoc>
        /// Lower velocity value
        /// </userdoc>
        [DataMember(30)]
        [Display("Velocity min")]
        public float VelocityMin { get; set; } = 1;

        /// <summary>
        /// Upper velocity value
        /// </summary>
        /// <userdoc>
        /// Upper velocity value
        /// </userdoc>
        [DataMember(40)]
        [Display("Velocity max")]
        public float VelocityMax { get; set; } = 1;


        public float MaxParticleLife = 10f;
    }
}
