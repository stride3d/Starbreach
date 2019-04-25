// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Mathematics;
using Xenko.Input;

namespace Starbreach.Drones
{
    public class TestDroneController : DroneControllerBase
    {
        public override void Start()
        {
            base.Start();
        }

        public override void Update()
        {
            base.Update();

            Vector2 logicalMovement = Vector2.Zero;
            if (Input.IsKeyDown(Keys.A))
                logicalMovement.X = -1.0f;
            if (Input.IsKeyDown(Keys.D))
                logicalMovement.X = 1.0f;
            if (Input.IsKeyDown(Keys.W))
                logicalMovement.Y = 1.0f;
            if (Input.IsKeyDown(Keys.S))
                logicalMovement.Y = -1.0f;

            Vector3 worldMovement = Vector3.UnitX*logicalMovement.X + Vector3.UnitZ*-logicalMovement.Y;
            worldMovement.Normalize();

            Drone.SetMovement(worldMovement);

            if (worldMovement != Vector3.Zero)
            {
                Drone.UpdateBodyRotation(worldMovement);
            }

            if (Input.IsMousePositionLocked)
            {
                Size3 backbufferSize = Game.GraphicsDevice.Presenter.BackBuffer.Size;
                Vector2 screenSize = new Vector2(backbufferSize.Width, backbufferSize.Height);
                Vector2 logicalHeadMovement = Input.MouseDelta * screenSize;
                logicalHeadMovement.Y = -logicalHeadMovement.Y;

                float headRotationDelta = -logicalHeadMovement.X*MathUtil.Pi/500.0f;
                Drone.UpdateHeadRotation(Drone.HeadRotation + headRotationDelta);

                if (Input.IsKeyPressed(Keys.Space) || Input.IsMouseButtonPressed(MouseButton.Left))
                {
                    Drone.Weapon?.TryShoot(null);
                }

                if (Input.IsKeyPressed(Keys.Escape))
                {
                    Input.UnlockMousePosition();
                }
            }
            else
            {
                if (Input.IsMouseButtonPressed(MouseButton.Left))
                {
                    Input.LockMousePosition(true);
                }
            }

        }
    }
}