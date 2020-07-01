// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Starbreach.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Input;

namespace Starbreach.Soldier
{
    /// <summary>
    /// This script listens to player Input and emits events.
    /// </summary>
    public class SoldierPlayerInput : SyncScript, IPlayerInput
    {
        public bool AimState { get; private set; } = false;
        public bool FireState { get; private set; } = false;
        public bool ReloadState { get; private set; } = false;
        
        public Vector2 MoveDirection { get; private set; } = new Vector2();

        public Vector2 AimDirection { get; private set; } = new Vector2();
        
        public delegate void InputEventHandler(Entity e);

        /// <summary>
        /// Called when the user presses the reload button
        /// </summary>
        public InputEventHandler OnReload;

        /// <summary>
        /// Called when the user presses the start button
        /// </summary>
        public InputEventHandler OnStart;

        /// <summary>
        /// Called when the user presses the interact button
        /// </summary>
        public InputEventHandler OnInteract;

        private GamePadButton lastButtonState = 0;

        /// <summary>
        /// Invert aim Y axis
        /// </summary>
        public bool InvertYAxis = false;

        /// <summary>
        /// Invert aim X axis
        /// </summary>
        public bool InvertXAxis = false;

        /// <summary>
        /// Multiplies move movement by this amount to apply aim rotations
        /// </summary>
        public float MouseSensitivity = 100.0f;

        public List<Keys> KeysLeft { get; } = new List<Keys>();

        public List<Keys> KeysRight { get; } = new List<Keys>();

        public List<Keys> KeysUp { get; } = new List<Keys>();

        public List<Keys> KeysDown { get; } = new List<Keys>();

        public List<Keys> KeysAim { get; } = new List<Keys>();

        public List<Keys> KeysShoot { get; } = new List<Keys>();

        public List<Keys> KeysReload { get; } = new List<Keys>();

        public List<Keys> KeysStart { get; } = new List<Keys>();

        public List<Keys> KeysInteract { get; } = new List<Keys>();

        public int ControllerIndex { get; set; }

        public float DeadZone { get; set; } = 0.5f;

        /// <summary>
        /// Tries to get the player Input component from the player entity
        /// </summary>
        /// <param name="playerEntity"> The entity created from the prefab that is the player </param>
        /// <returns>A valid Player Input object or null</returns>
        public static SoldierPlayerInput FromEntity(Entity playerEntity)
        {
            return playerEntity.Get<SoldierPlayerInput>();
        }

        public override void Start()
        {
            base.Start();
            if (Priority >= 0)
                throw new InvalidOperationException("SoldierPlayerInput must have a priority lower than zero.");
        }

        public override void Update()
        {
            MoveDirection = Vector2.Zero;
            AimDirection = Vector2.Zero;

            // Left stick: movement
            var padDirection = Input.GetLeftThumb(ControllerIndex);
            var isDeadZone = padDirection.Length() < DeadZone;
            if (!isDeadZone)
                MoveDirection = padDirection;
            MoveDirection.Normalize();

            // Right stick: aim
            padDirection = Input.GetRightThumb(ControllerIndex);
            var aimSpeed = padDirection.Length();
            isDeadZone = aimSpeed < DeadZone;
            // Make sure aim starts at 0 when outside deadzone
            aimSpeed = (aimSpeed - DeadZone)/(1.0f - DeadZone);
            // Clamp aim speed
            if (aimSpeed > 1.0f)
                aimSpeed = 1.0f;
            // Curve aim speed
            aimSpeed = (float)Math.Pow(aimSpeed, 1.6);
            if (!isDeadZone)
            {
                AimDirection = padDirection;
                AimDirection.Normalize();
                AimDirection *= aimSpeed;
            }

            // Keyboard move
            if (KeysLeft.Any(key => Input.IsKeyDown(key)))
                MoveDirection += -Vector2.UnitX;
            if (KeysRight.Any(key => Input.IsKeyDown(key)))
                MoveDirection += +Vector2.UnitX;
            if (KeysUp.Any(key => Input.IsKeyDown(key)))
                MoveDirection += +Vector2.UnitY;
            if (KeysDown.Any(key => Input.IsKeyDown(key)))
                MoveDirection += -Vector2.UnitY;

            var isAiming = KeysAim.Any(key => Input.IsKeyDown(key)) || Input.GetLeftTrigger(ControllerIndex) >= DeadZone;
            var isFiring = KeysShoot.Any(key => Input.IsKeyDown(key)) || Input.GetRightTrigger(ControllerIndex) >= DeadZone;
            var isStarting = KeysStart.Any(key => Input.IsKeyPressed(key)) || Input.IsGamePadButtonPressed(ControllerIndex, GamePadButton.Start);
            var isReloading = KeysReload.Any(key => Input.IsKeyPressed(key)) || Input.IsGamePadButtonPressed(ControllerIndex, GamePadButton.Y);
            var isInteracting = KeysInteract.Any(key => Input.IsKeyPressed(key)) || Input.IsGamePadButtonPressed(ControllerIndex, GamePadButton.A);

            if(isStarting)
                OnStart?.Invoke(Entity);
            
            if (isReloading)
                OnReload?.Invoke(Entity);

            if (isInteracting)
                OnInteract?.Invoke(Entity);

            // Mouse aim (after normalization of aim direction)
            // mouse aim is only enabled after you click the screen to lock your cursor, pressing escape cancels this
            if (Input.IsMouseButtonDown(MouseButton.Left))
                Input.LockMousePosition(true);
            if (Input.IsKeyPressed(Keys.Escape))
                Input.UnlockMousePosition();
            if (Input.IsMousePositionLocked)
            {
                // Mouse shooting
                if (Input.IsMouseButtonDown(MouseButton.Left))
                    isFiring = true;

                // Mouse aiming
                if (Input.IsMouseButtonDown(MouseButton.Right))
                    isAiming = true;

                AimDirection += new Vector2(Input.MouseDelta.X, -Input.MouseDelta.Y) * MouseSensitivity;
            }

            if (InvertXAxis)
                AimDirection = new Vector2(-AimDirection.X, AimDirection.Y);
            if (InvertYAxis)
                AimDirection = new Vector2(AimDirection.X, -AimDirection.Y);

            AimState = isAiming;
            FireState = isFiring;
        }

        public override void Cancel()
        {
            base.Cancel();
            Input.UnlockMousePosition();
        }
    }
}
