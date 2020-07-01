// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Rendering;

namespace Starbreach.VFX
{
    /// <summary>
    /// A path of blinking lights, to indicate a direction
    /// </summary>
    public class GuidePath : SyncScript
    {
        public Material OnMaterial;

        public Material OffMaterial;

        /// <summary>
        /// Maximum distance between lights
        /// </summary>
        public int MaxDistance = 10;
        
        public int Offset = 0;

        public float BlinkDuration = 0.2f;

        private ModelComponent[] modelChain;

        private float timer;

        public override void Start()
        {
            List<ModelComponent> models = new List<ModelComponent>();
            foreach(TransformComponent c in Entity.Transform.Children)
            {
                ModelComponent modelComponent = c.Children[0].Entity.Get<ModelComponent>();
                modelComponent.Materials[0] = OffMaterial;
                models.Insert(0, modelComponent);
            }
            modelChain = models.ToArray();
        }

        public override void Update()
        {
            timer += (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (timer > BlinkDuration)
            {
                Offset = (Offset + 1) % Math.Min(MaxDistance, modelChain.Length);
                timer -= BlinkDuration;
            }

            for (int i = 0; i < modelChain.Length; i++)
            {
                bool on = (i % MaxDistance) == Offset;
                if(!on && ((i - 1) % MaxDistance) == Offset && (timer > (BlinkDuration * 0.75f)))
                    on = true;
                
                modelChain[i].Materials[0] = on ? OnMaterial : OffMaterial;
            }
        }
    }
}
