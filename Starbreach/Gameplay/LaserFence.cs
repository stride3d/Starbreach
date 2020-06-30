// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Starbreach.Gameplay;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Particles;
using Stride.Particles.Components;
using Stride.Particles.Initializers;
using Stride.Physics;

namespace Starbreach.Gameplay
{
    public class LaserFence : AsyncScript
    {
        /// <summary>
        /// Particle emitters assigned to this fence
        /// </summary>
        public List<ParticleSystemComponent> ParticleSystemComponents = new List<ParticleSystemComponent>();

        /// <summary>
        /// Laser models assigned to this fence
        /// </summary>
        public List<ModelComponent> ModelComponents = new List<ModelComponent>();

        public List<StaticColliderComponent> LasetBlockade = new List<StaticColliderComponent>();

        public ActivatorCollection Triggers { get; } = new ActivatorCollection();

        private Task task;
        private bool enabled = true;
        private IEnumerable<ParticleEmitter> emitters;
        private InitialSizeSeed[] initialSizeSeeds;
        private Vector2[] initialRandomSizes;
        private IEnumerable<ParticleEmitter> radiationEmitters;
        private IEnumerable<ParticleEmitter> beamEmitters;

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (value != enabled)
                {
                    enabled = value;
                    if (value)
                        OnEnable();
                    else
                        OnDisable();
                }
            }
        }

        private async Task AnimationLoop(float length, Action<float> action)
        {
            var startTime = Game.UpdateTime.Total;
            while (true)
            {
                var elapsedTime = Game.UpdateTime.Total - startTime;

                float t = (float)elapsedTime.TotalSeconds/length;
                t = MathUtil.Clamp(t, 0.0f, 1.0f);
                action.Invoke(t);

                if (t >= 1.0)
                    break;

                await Script.NextFrame();
            }
        }

        private void OnEnable()
        {
            task = Enable();
        }

        private void OnDisable()
        {
            task = Disable();
        }

        private async Task Enable()
        {
            foreach (var emitter in emitters)
            {
                emitter.CanEmitParticles = true;
            }

            foreach (var modelComponent in ModelComponents)
            {
                if (modelComponent != null)
                    modelComponent.Enabled = true;
            }

            foreach (var staticColliderComponent in LasetBlockade)
            {
                if (staticColliderComponent != null)
                    staticColliderComponent.Enabled = true;
            }

            await AnimationLoop(0.1f, (t) =>
            {
                for (int i = 0; i < initialSizeSeeds.Length; i++)
                {
                    initialSizeSeeds[i].RandomSize = initialRandomSizes[i]*(t);
                }
            });
        }

        private async Task Disable()
        {
            await AnimationLoop(0.15f, (t) =>
            {
                for (int i = 0; i < initialSizeSeeds.Length; i++)
                {
                    initialSizeSeeds[i].RandomSize = initialRandomSizes[i] * (1.0f + t * 1.5f);
                }
            });

            foreach (var emitter in radiationEmitters)
            {
                emitter.CanEmitParticles = false;
            }

            foreach (var modelComponent in ModelComponents)
            {
                if (modelComponent != null)
                    modelComponent.Enabled = false;
            }

            foreach (var staticColliderComponent in LasetBlockade)
            {
                if (staticColliderComponent != null)
                    staticColliderComponent.Enabled = false;
            }

            await AnimationLoop(0.08f, (t) =>
            {
                for (int i = 0; i < initialSizeSeeds.Length; i++)
                {
                    initialSizeSeeds[i].RandomSize = initialRandomSizes[i]*(1.0f-t);
                }
            });

            foreach (var emitter in beamEmitters)
            {
                emitter.CanEmitParticles = false;
            }
        }

        public override async Task Execute()
        {
            emitters = ParticleSystemComponents.SelectMany(x => x.ParticleSystem.Emitters);
            radiationEmitters = ParticleSystemComponents.Select(x => x.ParticleSystem.Emitters[0]);
            beamEmitters = ParticleSystemComponents.Select(x => x.ParticleSystem.Emitters[1]);
            initialSizeSeeds = beamEmitters.Select(x => x.Initializers.OfType<InitialSizeSeed>().First()).ToArray();
            initialRandomSizes = initialSizeSeeds.Select(x => x.RandomSize).ToArray();
            
            while (Game.IsRunning)
            {
                Triggers.Update();

                if (Triggers.CurrentState != Enabled)
                    Enabled = Triggers.CurrentState;

                if (task != null)
                {
                    await task;
                    task = null;
                }

                await Script.NextFrame();
            }
        }
    }
}