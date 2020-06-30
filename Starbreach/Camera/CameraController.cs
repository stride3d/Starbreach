// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starbreach.Core;
using Starbreach.Soldier;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;

namespace Starbreach.Camera
{
    /// <summary>
    /// This script controls the camera parameters and switches between Run and Aim stances.
    /// </summary>
    public class CameraController : SyncScript
    {
        /// <summary>
        /// Gets or sets a reference to the camera.
        /// </summary>
        public CameraComponent Camera { get; set; }

        /// <summary>
        /// Gets or sets a reference to the pivot entity containing the camera.
        /// </summary>
        public Entity Pivot { get; set; }

        /// <summary>
        /// Gets or sets a reference to the model of the character targeted by the camera.
        /// </summary>
        public ModelComponent Model { get; set; }

        /// <summary>
        /// Gets or sets the duration of the switch between the two stances.
        /// </summary>
        public float SwitchDuration { get; set; } = 0.2f;

        /// <summary>
        /// Gets the parameter controlling the distance of the camera to its target.
        /// </summary>
        public CameraParameterFloat Distance { get; } = new CameraParameterFloat(10.0f, 10.0f);

        /// <summary>
        /// Gets the parameter controlling the field of view of the camera.
        /// </summary>
        public CameraParameterFloat Fov { get; } = new CameraParameterFloat(45.0f, 45.0f);

        /// <summary>
        /// Gets the parameter controlling the panof the camera.
        /// </summary>
        public CameraParameterVector2 Pan { get; } = new CameraParameterVector2();

        /// <summary>
        /// Gets or sets the pitch of the camera.
        /// </summary>
        public float Pitch { get; set; } = 20.0f;

        /// <summary>
        /// Gets or sets the yaw of the camera.
        /// </summary>
        public float Yaw { get; set; } = 45.0f;

        /// <summary>
        /// Gets or sets the speed at which the yaw is controlled by the player, in degrees per second.
        /// </summary>
        public float PitchSpeed { get; set; } = 60.0f;

        /// <summary>
        /// Gets or sets the speed at which the yaw is controlled by the player, in degrees per second.
        /// </summary>
        public float YawSpeed { get; set; } = 90.0f;

        /// <summary>
        /// Gets or sets the range of pitch the player can rotate in.
        /// </summary>
        public Vector2 PitchRange { get; set; } = new Vector2(10.0f, 80.0f);

        /// <summary>
        /// Gets whether the camera is currenly in the aiming stance.
        /// </summary>
        public bool IsAiming => input.AimState || input.FireState;

        private SoldierPlayerInput input;

        private IEnumerable<ICameraParameter> Parameters
        {
            get
            {
                yield return Distance;
                yield return Fov;
                yield return Pan;
            }
        }

        public override void Start()
        {
            input = SoldierPlayerInput.FromEntity(Entity);
            if (input == null) throw new ArgumentException("Input is not set");
            if (Camera == null) throw new ArgumentException("Camera is not set");
            if (Model == null) throw new ArgumentException("Model is not set");
            if (Pivot == null) throw new ArgumentException("Pivot is not set");
        }

        public override void Update()
        {
            ProcessInputs();
            UpdateParameters();
            ApplyParameters();
        }

        private void ProcessInputs()
        {
            Vector2 aimDir = input.AimDirection;
            if (!IsAiming)
            {
                var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
                Pitch = MathUtil.Clamp(Pitch - aimDir.Y * PitchSpeed * dt, PitchRange.X, PitchRange.Y);
                Yaw += -aimDir.X * YawSpeed * dt;
            }
        }

        private void UpdateParameters()
        {
            var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            foreach (var parameter in Parameters)
            {
                parameter.Update(dt, SwitchDuration);
            }
        }

        private void ApplyParameters()
        {
            // Update pitch
            Pivot.Transform.Rotation = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(Yaw), MathUtil.DegreesToRadians(Pitch), 0);
            // Update distance
            Camera.Entity.Transform.Position.Z = -Distance.CurrentValue;
            // Update FOV
            Camera.VerticalFieldOfView = Fov.CurrentValue;
            // Update pan
            Pivot.Transform.Position = Vector3.Transform(new Vector3(Pan.CurrentValue.X, Pan.CurrentValue.Y, 0), Model.Entity.Transform.Rotation);
        }
    }
}