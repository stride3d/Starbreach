// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Starbreach.Core;
using Xenko.Core.Mathematics;
using Xenko.Animations;
using Xenko.Engine;
using Xenko.Engine.Events;
using Xenko.Physics;
using Xenko.Rendering.Compositing;
using Xenko.UI.Controls;

namespace Starbreach.Soldier
{
    public class SoldierSpawner : PlayerSpawner
    {
        private SoldierController currentSoldier;

        private UIComponent spawnUiComponent;
        private TextBlock respawnTimerTextBlock;

        private CameraComponent currentCameraComponent;

        private CameraComponent ActiveCamera
        {
            get
            {
                return (SceneSystem.GraphicsCompositor).Cameras[0].Camera;
            }
            set
            {
                if (currentCameraComponent != null)
                {
                    currentCameraComponent.Slot = new SceneCameraSlotId();
                }
                if (value != null)
                {
                    value.Slot = (SceneSystem.GraphicsCompositor).Cameras[0].ToSlotId();
                }
                currentCameraComponent = value;
            }
        }

        public CameraComponent DefaultCamera { get; set; }

        public override void Start()
        {
            var ipbrGame = Services.GetService<IStarbreach>();
            spawnUiComponent = ipbrGame.PlayerUiEntity.FindChild("RespawnUI").Get<UIComponent>();
            respawnTimerTextBlock = spawnUiComponent.Page.RootElement.FindNameRecursive("CountDown") as TextBlock; 

            base.Start();
        }

        protected override void PreSpawnPlayer(Entity playerEntity)
        {
            var controller = playerEntity.Get<SoldierController>();
            if (controller != null)
            {
                Player = currentSoldier = controller;
                IPlayerInput input = playerEntity.Get<SoldierPlayerInput>();
                Player.Init(input);
            }
        }

        protected override void SpawnPlayer()
        {
            base.SpawnPlayer();

            // Hide Respawn UI
            spawnUiComponent.Enabled = false;

            if (Player == null)
                throw new InvalidOperationException("Could not find the player controller");

            // Set yaw on spawned soldier
            if(currentSoldier != null)
            {
                var rot = Entity.Transform.Rotation;
                rot.X = rot.Z = 0.0f;
                rot.Normalize();
                var yaw = MathUtil.RadiansToDegrees(2 * (float)Math.Acos(rot.W));
                currentSoldier.Yaw = yaw;
                currentSoldier.CameraController.Yaw = yaw;
                ActiveCamera = currentSoldier.Camera;
            }
        }

        protected override void UpdateRespawnTimer(float timeLeft)
        {
            // Show respawn timer (when game has started)
            spawnUiComponent.Enabled = true;

            int timeLeftRounded = (int)Math.Round(timeLeft);
            respawnTimerTextBlock.Text = timeLeftRounded.ToString();
        }

        protected override void KillPlayer()
        {
            ActiveCamera = DefaultCamera;
            currentSoldier = null;

            base.KillPlayer();
        }
    }
}
