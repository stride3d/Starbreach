// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Starbreach.VFX
{
    public class FloatingRock : SyncScript
    {
        public Vector2 FrequencyRange = new Vector2(0.05f, 0.2f);
        public Vector2 AmplitudeRange = new Vector2(0.2f, 1.5f);

        private Vector3 startingPosition;
        private float frequency;
        private float amplitude;
        private float timer = 0.0f;

        public override void Start()
        {
            Random random = new Random(Entity.GetHashCode());
            frequency = MathUtil.Lerp(FrequencyRange.X, FrequencyRange.Y, (float)random.NextDouble()); 
            amplitude = MathUtil.Lerp(AmplitudeRange.X, AmplitudeRange.Y, (float)random.NextDouble());

            // Random offset
            timer = (float)random.NextDouble();

            Entity.Transform.UpdateWorldMatrix();
            startingPosition = Entity.Transform.WorldMatrix.TranslationVector;
        }

        public override void Update()
        {
            timer += (float)Game.UpdateTime.Elapsed.TotalSeconds;
            var offset = (float)Math.Sin(timer * Math.PI * 2.0 * frequency) * amplitude;
            Entity.Transform.Position = startingPosition + new Vector3(0.0f, offset, 0.0f);
        }
    }
}
