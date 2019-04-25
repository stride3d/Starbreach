// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Rendering;

namespace Starbreach.VFX
{
    public class Fan : SyncScript
    {
        public float RotationSpeed = 1.0f;
        public ModelComponent VentLightModelComponent;

        public Texture LightTexture;

        private float phase = 0.0f;
        private Vector3 upAxis;
        private Quaternion originalRotation;

        public override void Start()
        {
            Entity.Transform.UpdateLocalMatrix();
            upAxis = Entity.Transform.LocalMatrix.Up;
            originalRotation = Entity.Transform.Rotation;
        }

        public override void Update()
        {
            phase = (float)Game.UpdateTime.Total.TotalSeconds * RotationSpeed;
            var rotate = Quaternion.RotationAxis(upAxis, (float)Math.PI * 2.0f * phase);
            Entity.Transform.Rotation = originalRotation * rotate;
           
            var material = VentLightModelComponent.GetMaterial(0);
            foreach (var pass in material.Passes)
            {
                pass.Parameters.Set(ComputeColorTextureScrollParamKeys.Offset, new Vector2(phase, 0.0f));
                pass.Parameters.Set(ComputeColorTextureScrollParamKeys.MyTexture, LightTexture);
            }
        }
    }
}
