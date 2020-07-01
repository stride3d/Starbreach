// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starbreach.Core;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;

namespace Particles
{
    public class CometSpawner : SyncScript
    {
        private Entity comet;
        private Vector3 rotations;
        private Vector3 currentVelocity;

        // Declared public member fields and properties will show in the game studio
        public Prefab CometPrefab { get; set; }

        public Vector3 Direction { get; set; } = -3 * Vector3.UnitX;

        public float Speed { get; set; } = 6; 

        public override void Start()
        {
            // Initialization of the script.
        }

        public override void Update()
        {
            if (Input.IsGamePadButtonDown(0, GamePadButton.Y))
            {
                if (comet != null)
                    SceneSystem.SceneInstance.RootScene.Entities.Remove(comet);

                comet = CometPrefab.Instantiate()[0];
                comet.Transform.Position = Entity.Transform.Position;
                Direction.Normalize();
                currentVelocity = Direction * Speed;
                SceneSystem.SceneInstance.RootScene.Entities.Add(comet);
            }

            if (comet != null)
            {
                currentVelocity.Y += -2.0f * (float)Game.UpdateTime.Elapsed.TotalSeconds;
                comet.Transform.Position = comet.Transform.Position + currentVelocity * (float)Game.UpdateTime.Elapsed.TotalSeconds;
                rotations.X += MathUtil.DegreesToRadians(30) * (float)Game.UpdateTime.Elapsed.TotalSeconds;
                rotations.Y += MathUtil.DegreesToRadians(20) * (float)Game.UpdateTime.Elapsed.TotalSeconds;
                rotations.Z += MathUtil.DegreesToRadians(15) * (float)Game.UpdateTime.Elapsed.TotalSeconds;
                comet.Transform.RotationEulerXYZ = rotations;
            }
        }
    }
}
