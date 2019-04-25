// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Starbreach.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Input;

namespace Starbreach.Camera
{
    /// <summary>
    /// This script allows to customize camera parameters to fine-tune them.
    /// </summary>
    public class CameraManualControl : SyncScript
    {
        public CameraController CameraController { get; set; }

        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets oe sets the speed at which the pitch can be modified.
        /// </summary>
        public float DebugPitchSpeed { get; set; } = 45.0f;

        /// <summary>
        /// Gets oe sets the speed at which the distance can be modified.
        /// </summary>
        public float DebugDistanceSpeed { get; set; } = 5f;

        /// <summary>
        /// Gets oe sets the speed at which the FOV can be modified.
        /// </summary>
        public float DebugFovSpeed { get; set; } = 5.0f;

        /// <summary>
        /// Gets oe sets the speed at which the pan can be modified.
        /// </summary>
        public float DebugPanSpeed { get; set; } = 0.5f;

        public override void Start()
        {
            if (CameraController == null) throw new ArgumentException("Camera not set");
        }

        public override void Update()
        {
            if (!IsEnabled)
                return;

            var distOffset = 0.0f;
            var fovOffset = 0.0f;
            var panOffset = Vector2.Zero;
            bool changed = false;
            var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (Input.IsGamePadButtonDown(0, GamePadButton.PadUp) || Input.IsKeyDown(Keys.PageUp))
            {
                distOffset += DebugDistanceSpeed * dt;
                changed = true;
            }
            if (Input.IsGamePadButtonDown(0, GamePadButton.PadDown) || Input.IsKeyDown(Keys.PageDown))
            {
                distOffset -= DebugDistanceSpeed * dt;
                changed = true;
            }
            if (Input.IsGamePadButtonDown(0, GamePadButton.PadLeft) || Input.IsKeyDown(Keys.I))
            {
                changed = true;
            }
            if (Input.IsGamePadButtonDown(0, GamePadButton.PadRight) || Input.IsKeyDown(Keys.K))
            {
                changed = true;
            }
            if (Input.IsGamePadButtonDown(0, GamePadButton.LeftShoulder) || Input.IsKeyDown(Keys.End))
            {
                fovOffset += DebugFovSpeed * dt;
                changed = true;
            }
            if (Input.IsGamePadButtonDown(0, GamePadButton.RightShoulder) || Input.IsKeyDown(Keys.Home))
            {
                fovOffset -= DebugFovSpeed * dt;
                changed = true;
            }
            if (Input.IsGamePadButtonDown(0, GamePadButton.X) || Input.IsKeyDown(Keys.G))
            {
                panOffset.X += DebugPanSpeed * dt;
                changed = true;
            }
            if (Input.IsGamePadButtonDown(0, GamePadButton.B) || Input.IsKeyDown(Keys.J))
            {
                panOffset.X -= DebugPanSpeed * dt;
                changed = true;
            }
            if (Input.IsGamePadButtonDown(0, GamePadButton.Y) || Input.IsKeyDown(Keys.Y))
            {
                panOffset.Y += DebugPanSpeed * dt;
                changed = true;
            }
            if (Input.IsGamePadButtonDown(0, GamePadButton.A) || Input.IsKeyDown(Keys.H))
            {
                panOffset.Y -= DebugPanSpeed * dt;
                changed = true;
            }

            if (changed)
            {
                if (CameraController.IsAiming)
                {
                    CameraController.Distance.ValueAtAim += distOffset;
                    CameraController.Fov.ValueAtAim += fovOffset;
                    CameraController.Pan.ValueAtAim += panOffset;
                }
                else
                {
                    CameraController.Distance.ValueAtRun += distOffset;
                    CameraController.Fov.ValueAtRun += fovOffset;
                    CameraController.Pan.ValueAtRun += panOffset;
                }
            }
            Game.DebugPrint($"Dist={CameraController.Distance.CurrentValue}");
            Game.DebugPrint($"FOV={CameraController.Fov.CurrentValue}");
            Game.DebugPrint($"Pan={CameraController.Pan.CurrentValue}");
        }
    }
}