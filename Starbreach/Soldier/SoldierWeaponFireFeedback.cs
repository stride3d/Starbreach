// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Xenko.Core.Mathematics;
using Xenko.Audio;
using Xenko.Engine;
using Xenko.Engine.Events;
using Xenko.Particles;
using Xenko.Particles.Components;
using Xenko.Physics;

namespace Starbreach.Soldier
{
    /// <summary>
    /// This script handles all feedback of the soldier's weapon firing.
    /// </summary>
    public class SoldierWeaponFireFeedback : StartupScript
    {
        /// <summary>
        /// Gets or sets the entity from which bullets are fired.
        /// </summary>
        public Entity ShootSource { get; set; }

        /// <summary>
        /// Gets or sets the entity that will be placed at the hit location of the bullet to render particles.
        /// </summary>
        public Entity ShootTarget { get; set; }

        /// <summary>
        /// Gets or sets the light to toggle on when the weapon is firing.
        /// </summary>
        public LightComponent Light { get; set; }

        /// <summary>
        /// Gets or sets the particle system for the bullet trail.
        /// </summary>
        public ParticleSystemComponent ShootTrail { get; set; }

        /// <summary>
        /// Gets or sets the particle system for the bullet impact.
        /// </summary>
        public ParticleSystemComponent ShootImpact { get; set; }

        /// <summary>
        /// Gets or sets the particle system for the bullet holes effect.
        /// </summary>
        public ParticleSystemComponent BulletHole { get; set; }

        /// <summary>
        /// Particle system which controls the muzzle flash
        /// </summary>
        private ParticleSystem muzzleFlashParticles;

        private ParticleSystem laserEffect;
        private ParticleSystem laserImpact;

        private AudioEmitterSoundController[] shootSounds;
        private AudioEmitterSoundController[] impactSounds;
        private Random soundIndexGenerator = new Random();

        public override void Start()
        {
            if (ShootSource == null) throw new ArgumentException("ShootSource not set");
            if (ShootTarget == null) throw new ArgumentException("ShootTarget not set");
            if (Light == null) throw new ArgumentException("Light not set");
            if (ShootTrail == null) throw new ArgumentException("ShootTrail not set");
            if (ShootImpact == null) throw new ArgumentException("ShootImpact not set");
            if (BulletHole == null) throw new ArgumentException("BulletHole not set");

            var muzzleFlashEntity = ShootSource.FindChild("MuzzleFlash");
            muzzleFlashParticles = muzzleFlashEntity?.Get<ParticleSystemComponent>()?.ParticleSystem;
            muzzleFlashParticles?.Stop();

            laserEffect = ShootTrail.ParticleSystem;
            laserEffect.Stop();
            laserImpact = ShootImpact.ParticleSystem;
            laserImpact.Stop();


            BulletHole.ParticleSystem.Enabled = true;
            BulletHole.ParticleSystem.Stop();

            Light.Enabled = false;

            AudioEmitterComponent shootEmitter = Entity.FindChild("ShootSource").Get<AudioEmitterComponent>();
            AudioEmitterComponent shootTargetEmitter = ShootTarget.Get<AudioEmitterComponent>();

            // Load different shoot sound effects
            shootSounds = new AudioEmitterSoundController[4];
            for (int i = 0; i < shootSounds.Length; i++)
                shootSounds[i] = shootEmitter["Shoot" + i];

            // Load different impact sound effects
            impactSounds = new AudioEmitterSoundController[4];
            for (int i = 0; i < impactSounds.Length; i++)
                impactSounds[i] = shootTargetEmitter["Impact" + i];

            SoldierWeapon weapon = Entity.Get<SoldierWeapon>();
            weapon.OnShotFired += (soldierWeapon, result) =>
            {
                Script.AddTask(async() => await FireWeapon(result));
            };
        }

        public async Task FireWeapon(WeaponFiredResult hit)
        {
            
            var hitPoint = hit.HitResult.Succeeded ? hit.HitResult.Point : hit.Target;

            // MORE LOGIC HERE
            //  The bullet holes should be more discriminitive

            var displayBulletHole = hit.HitResult.Succeeded && (hit.HitResult.Collider.CollisionGroup != CollisionFilterGroups.CustomFilter3 &&        // Enemy Drone
                                    hit.HitResult.Collider.CollisionGroup != CollisionFilterGroups.CharacterFilter &&       // Player Character
                                    hit.HitResult.Collider.CollisionGroup != CollisionFilterGroups.CustomFilter1);          // VR Drone

            displayBulletHole = false;

            ShootTarget.Transform.Position = hitPoint;

            var rightVector = Vector3.Cross(new Vector3(0, 1, 0), hit.HitResult.Normal);
            var rightCos = Vector3.Dot(new Vector3(0, 1, 0), hit.HitResult.Normal);
            var rightAngle = (float) Math.Acos(rightCos);
            Quaternion.RotationAxis(ref rightVector, rightAngle, out ShootTarget.Transform.Rotation);

            // Wait for camera to turn
            await Script.NextFrame();

            Light.Enabled = true;
            if (displayBulletHole)
                BulletHole.ParticleSystem.Play();
            laserImpact.Play();
            muzzleFlashParticles?.Play();
            laserEffect.Play();

            shootSounds[soundIndexGenerator.Next(0, shootSounds.Length-1)].PlayAndForget();

            await Script.NextFrame();

            impactSounds[soundIndexGenerator.Next(0, impactSounds.Length - 1)].PlayAndForget();

            Light.Enabled = false;
            if (displayBulletHole)
                BulletHole.ParticleSystem.Timeout(10);
            laserEffect.Timeout(1);
            laserImpact.Timeout(1);
            muzzleFlashParticles?.Timeout(1);
        }
    }
}