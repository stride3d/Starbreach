// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace Starbreach.VFX
{
    public class RotateAxisY : AsyncScript
    {
        public override async Task Execute()
        {
            while (Game.IsRunning)
            {
                await Script.NextFrame();

                var rotationSpeed = 2f;

                var elapsedTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
                Entity.Transform.Rotation *= Quaternion.RotationY(rotationSpeed * elapsedTime);
            }
        }
    }
}
