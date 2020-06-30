// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Starbreach.Camera;
using Starbreach.Core;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Audio;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Games;
using Stride.Graphics;
using Stride.Physics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Starbreach.Soldier
{
    public struct WeaponFiredResult
    {
        public HitResult HitResult;
        public Vector3 Target;
    }

    /// <summary>
    /// This script handles the weapon of the solider and makes it fire.
    /// </summary>
    public class SoldierWeapon : SyncScript
    {
        private readonly Random rand = new Random(42);

        private SoldierController soldier;
        private TimeSpan lastBullet;
        private bool isFiring;
        public bool IsReloading { get; private set; }
        
        // Sound
        private AudioEmitterSoundController reloadSound;
        private Entity uiCameraEntity;
        private Entity crosshairEntity;
        private Entity statusBarEntity;
        private StackPanel ammoBarGrid;

        public delegate void ShotFiredHandler(SoldierWeapon weapon, WeaponFiredResult result);
        public ShotFiredHandler OnShotFired;
        
        public delegate void ReloadHandler(SoldierWeapon weapon);
        public ReloadHandler OnReload;

        /// <summary>
        /// Gets or sets the camera that defines the direction of the shooting.
        /// </summary>
        public CameraController Camera { get; set; }

        /// <summary>
        /// Gets or sets the entity from which bullets are fired.
        /// </summary>
        public Entity ShootSource { get; set; }

        /// <summary>
        /// Gets or sets the number of bullets shot per second
        /// </summary>
        public float BulletPerSeconds { get; set; } = 10;

        /// <summary>
        /// Gets or sets the angle of the shooting cone.
        /// </summary>
        public float ShootingConeAngle { get; set; } = 2.0f;

        public int MaxAmmo { get; set; } = 25;

        [DataMemberIgnore]
        public int CurrentAmmo { get; private set; }

        public override void Start()
        {
            if (Camera == null) throw new ArgumentException("Camera is not set");
            if (ShootSource == null) throw new ArgumentException("ShootSource is not set");
            if (BulletPerSeconds <= 0) throw new ArgumentException("BulletPerSeconds must be > 0");
            if (MaxAmmo <= 0) throw new ArgumentException("MaxAmmo must be > 0");
            CurrentAmmo = MaxAmmo;

            soldier = Entity.Get<SoldierController>();
            reloadSound = ShootSource.Get<AudioEmitterComponent>()["Reload"];

            // Handle reload event
            soldier.Input.OnReload += (e) =>
            {
                if(!IsReloading && CurrentAmmo != MaxAmmo) Reload();
            };
            BulletPerSeconds = 40;
        }

        public override void Update()
        {
            // Can't shoot while dead
            if (soldier.IsDead) return;

            isFiring = soldier.Input.FireState;

            var bulletDelta = 1.0f / BulletPerSeconds;
            if ((Game.UpdateTime.Total - lastBullet).TotalSeconds > bulletDelta)
            {
                if (IsReloading)
                {
                    CurrentAmmo = MaxAmmo;
                    IsReloading = false;
                }
                if (isFiring)
                {
                    if (CurrentAmmo > 0)
                    {
                        Shoot();
                    }
                    else
                    {
                        Reload();
                    }
                }
                else if (CurrentAmmo == 0)
                    Reload(); // Auto reload when empty
            }
            //Game.DebugPrint($"Ammo: {CurrentAmmo}");

            UpdateUI();
        }
        
        private void UpdateUI()
        {
            var source = ShootSource.Transform.WorldMatrix.TranslationVector;
            var accuracyVector = new Vector3(0.0f, 0.0f, 1.0f);
            accuracyVector = Vector3.Transform(accuracyVector, Quaternion.RotationYawPitchRoll(
                    MathUtil.DegreesToRadians(Camera.Yaw), 0, 0));
            var target = source + accuracyVector * 100.0f;

            var vrGame = (IStarbreach)Game;
            uiCameraEntity = uiCameraEntity ?? vrGame.PlayerUiEntity.FindChild("Camera");
            if (uiCameraEntity != null)
            {
                crosshairEntity = crosshairEntity ?? vrGame.PlayerUiEntity.FindChild("Crosshair");
                if (soldier.IsAiming)
                {
                    // TEMPORARY Until we have a correct crosshair
                    //crosshairEntity.Get<SpriteComponent>().Enabled = true;

                    // TODO make sure crosshair doesn't lag behind camera movement
                    //if(!crosshairSet)
                    { 
                        var y = uiCameraEntity.Get<CameraComponent>().OrthographicSize;
                        var x = y*uiCameraEntity.Get<CameraComponent>().AspectRatio;

                        var crosshairPosition = Vector3.Project(target, -x*0.5f, -y*0.5f, x, y,
                            Camera.Camera.NearClipPlane, Camera.Camera.FarClipPlane, Camera.Camera.ViewProjectionMatrix);
                        crosshairPosition.Y *= -1.0f;
                    
                        crosshairEntity.Transform.Position = crosshairPosition;
                        crosshairEntity.Transform.Position.Z = -10.0f;
                        crosshairEntity.Transform.UpdateWorldMatrix();
                    }
                }
                else
                {
                    crosshairEntity.Get<SpriteComponent>().Enabled = false;
                }
            }
            
            // Update Ammo indicator
            statusBarEntity = statusBarEntity ?? vrGame.PlayerUiEntity.FindChild("StatusBar");
            //ammoBarGrid = ammoBarGrid ?? (StackPanel)statusBarEntity.Get<UIComponent>().Page.RootElement.FindName("ammobarGrid");
            //for (var i = 0; i < ammoBarGrid.Children.Count; i++)
            //{
            //    ammoBarGrid.Children[i].Visibility = (i < CurrentAmmo) ? Visibility.Visible : Visibility.Hidden;
            //}
        }

        private void Reload()
        {
            var bulletDelta = 1.0f / BulletPerSeconds;
            lastBullet = Game.UpdateTime.Total + TimeSpan.FromSeconds(1.5 - bulletDelta);
            IsReloading = true;
            OnReload?.Invoke(this);

            // Play reload sound
            reloadSound.PlayAndForget();
        }

        private void Shoot()
        {
            var simulation = this.GetSimulation();
            var source = ShootSource.Transform.WorldMatrix.TranslationVector;
            // Compute direction from the angle of the shooting cone
            var accuracyRadius = Math.Tan(MathUtil.DegreesToRadians(ShootingConeAngle) * 0.5) * rand.NextDouble();
            var accuracyAngle = Math.PI * 2 * rand.NextDouble();
            var accuracyVector = new Vector3((float)(accuracyRadius * Math.Cos(accuracyAngle)), (float)(accuracyRadius * Math.Sin(accuracyAngle)), 1.0f);
            accuracyVector = Vector3.Transform(accuracyVector, Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(Camera.Yaw), 0, 0));
            var target = source + accuracyVector * 100.0f;

            // Cast a ray to find the collision
            var hits = new List<HitResult>();
            simulation.RaycastPenetrating(source, target, hits);
            Vector3 normal = (target - source);
            normal.Normalize();
            hits.Sort(Comparer<HitResult>.Create((a,b) => Vector3.Dot(a.Point, normal).CompareTo(Vector3.Dot(b.Point, normal))));

            // Hack to ignore disabled colliders
            HitResult hit = new HitResult();
            hit.Succeeded = false;
            foreach(var testHit in hits)
            {
                if (testHit.Collider.CollisionGroup == CollisionFilterGroups.CustomFilter2) // Alert sphere
                    continue;

                if (testHit.Collider.Enabled)
                {
                    hit = testHit;
                    break;
                }
            }

            if (hit.Succeeded)
            {
                //Damage things that are hit
                var destructible = Utils.GetDestructible(hit.Collider.Entity);
                if(destructible != null && !Utils.IsPlayerEntity(hit.Collider.Entity))
                {
                    destructible?.Damage(25);
                }
            }

            OnShotFired?.Invoke(this, new WeaponFiredResult { HitResult  = hit, Target = target});
            lastBullet = Game.UpdateTime.Total;

            // Debug ammo usage
            CurrentAmmo--;
        }
    }
}