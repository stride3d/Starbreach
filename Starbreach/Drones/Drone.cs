// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Starbreach.Core;
using Starbreach.Soldier;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Animations;
using Stride.Audio;
using Stride.Engine;
using Stride.Particles.Components;
using Stride.Physics;
using Stride.Rendering;

namespace Starbreach.Drones
{
    public class DronePostTransformUpdater : TransformOperation
    {
        public Drone Drone;

        public override void Process(TransformComponent transformComponent)
        {
            Drone.PostTransform();
        }
    }

    /// <summary>
    /// The most basic behaviour and movment constraints of a drone
    /// </summary>
    public class Drone : SyncScript, IStunnable, IDestructible
    {
        public delegate void DroneNotificationHandler(Drone drone);
        public delegate void AlertedChangedHandler(Drone drone, bool newState);

        public static readonly string HeadBoneName = "Bone_turret_ring";

        private static readonly float YawThreshold = MathUtil.DegreesToRadians(1);
        
        /// <summary>
        /// Original state of bones on the drone
        /// </summary>
        private readonly Dictionary<string, Bone> bones = new Dictionary<string, Bone>();

        // Body hover
        private float initialHeight;
        private float initialShift = (float)new Random().NextDouble();

        private bool stunned;

        // Sounds for when a drone gets hit
        private AudioEmitterSoundController engineSound;
        private AudioEmitterSoundController deathSound;
        private RandomSoundSelector hitSounds;
        
        private bool alerted;

        public Drone()
        {
            Priority = 4000;
        }

        /// <summary>
        /// The weapon that this drone will shoot
        /// </summary>
        public DroneWeapon Weapon
        {
            get { return weapon; }
            set { weapon = value; }
        }

        public Vector3 CurrentVelocity { get; private set; } = Vector3.Zero;

        /// <summary>
        /// Local rotation of this drone's head(turret) in radians
        /// </summary>
        public float HeadRotation { get; private set; } = 0.0f;

        public Vector3 HeadDirection => RotationToWorldDirection(HeadRotation + BodyRotation);

        /// <summary>
        /// Local rotation of this drone's body in radians
        /// </summary>
        public float BodyRotation { get; private set; }

        public Vector3 BodyDirection => RotationToWorldDirection(BodyRotation);

        /// <summary>
        /// The maxmium speed at which this drone moves
        /// </summary>
        public float MaximumSpeed { get; set; } = 5.0f;

        /// <summary>
        /// The speed at which the bottom part of the drone rotates
        /// </summary>
        [DefaultValue(MathUtil.TwoPi)]
        public float RotationSpeed { get; set; } = MathUtil.TwoPi;

        /// <summary>
        /// The speed at which the upper part of the drone rotates
        /// </summary>
        [DefaultValue(MathUtil.TwoPi)]
        public float HeadRotationSpeed { get; set; } = MathUtil.TwoPi;

        /// <summary>
        /// The drone's model
        /// </summary>
        public ModelComponent Model { get; set; }

        public Material DefaultMaterial { get; set; }

        public Material AlertedMaterial { get; set; }

        /// <summary>
        /// The CharacterComponent for the drone
        /// </summary>
        [DataMemberIgnore]
        public CharacterComponent Character { get; private set; }

        /// <summary>
        /// The CharacterComponent for the drone
        /// </summary>
        [DataMemberIgnore]
        public DroneAnimation Animation { get; private set; }

        /// <summary>
        /// Main drone audio emitter
        /// </summary>
        [DataMemberIgnore]
        public AudioEmitterComponent AudioEmitter { get; private set; }

        /// <summary>
        /// Engine particle for the drone
        /// </summary>
        public ParticleSystemComponent EngineParticle { get; set; }

        /// <summary>
        /// Engine effect of the drone
        /// </summary>
        public AudioEmitterComponent EngineAudioEmitter { get; set; }

        /// <summary>
        /// Prefab of explosion to spawn when this drone is destroyed
        /// </summary>
        public Prefab DroneExplosionPrefab { get; set; }

        /// <summary>
        /// Current HP of the drone
        /// </summary>
        [DataMemberIgnore]
        public int HealthPoints { get; private set; } = 100;

        [DataMemberIgnore]
        public bool Alerted
        {
            get { return alerted; }
            set
            {
                if (alerted != value)
                {
                    alerted = value;
                    if (alerted)
                    {
                        if (AlertedMaterial != null)
                            Model.Materials[1] = AlertedMaterial;
                    }
                    else
                    {
                        if (DefaultMaterial != null)
                            Model.Materials[1] = DefaultMaterial;
                    }
                    AlertedChanged?.Invoke(this, value);
                }
            }
        }

        public bool Stunned => stunned;

        public bool CanBeStunned => true;

        public bool IsDead => HealthPoints <= 0;

        /// <summary>
        /// Called when drone is destroyed (removed from the scene)
        /// </summary>
        public event DroneNotificationHandler Destroyed;

        /// <summary>
        /// Called when drone has died
        /// </summary>
        public event DroneNotificationHandler Died;

        /// <summary>
        /// Called when the state of <see cref="Alerted"/> changed
        /// </summary>
        public event AlertedChangedHandler AlertedChanged;

        public static float WorldDirectionToRotation(Vector3 direction)
        {
            return (float)Math.Atan2(-direction.Z, direction.X) + MathUtil.PiOverTwo;
        }

        public static Vector3 RotationToWorldDirection(float direction)
        {
            direction -= MathUtil.PiOverTwo;
            return new Vector3(
                (float)Math.Cos(direction),
                0.0f,
                -(float)Math.Sin(direction));
        }

        public override void Start()
        {
            base.Start();

            Character = Entity.Get<CharacterComponent>();
            Animation = Entity.Get<DroneAnimation>();
            if (Animation == null)
            {
                Animation = new DroneAnimation {Animation = Model.Entity.Get<AnimationComponent>()};
                Entity.Add(Animation);
            }

            // Store original bone data, so they can be modified later
            for (int i = 0; i < Model.Skeleton.Nodes.Length; ++i)
            {
                var nodeRot = Model.Skeleton.NodeTransformations[i].Transform.Rotation;
                var mat = Matrix.RotationQuaternion(nodeRot);
                float yaw, pitch, roll;
                mat.Decompose(out yaw, out pitch, out roll);
                var node = Model.Model.Skeleton.Nodes[i];
                bones[node.Name] = new Bone
                {
                    EulerAngles = new Vector3(yaw, pitch, roll)
                };
            }

            Entity.Transform.PostOperations.Add(new DronePostTransformUpdater {Drone = this});

            // Engine sound
            engineSound = EngineAudioEmitter["Engine"];
            engineSound.IsLooping = true;
            engineSound.Play();

            // Load sounds
            AudioEmitter = Entity.Get<AudioEmitterComponent>();
            deathSound = AudioEmitter["Death"];

            // Load the variable amount of being hit sounds, "Hit0", "Hit1", etc.
            hitSounds = new RandomSoundSelector(AudioEmitter, "Hit");

            initialHeight = Model.Entity.Transform.Position.Y;
            
            weapon?.Init(this);
        }

        public override void Cancel()
        {
            Destroyed?.Invoke(this);
        }

        public override void Update()
        {
            if (IsDead)
                return;

            if (CurrentVelocity.Length() > 1)
                CurrentVelocity.Normalize();

            // Update speed
            Character.SetVelocity(CurrentVelocity*MaximumSpeed);

            ApplyHeadRotation(HeadRotation);
            ApplyBodyRotation(BodyRotation);

            // Body hover
            const float amp = 0.05f;
            const float freq = 0.5f;
            Model.Entity.Transform.Position.Y = initialHeight +
                                                amp*
                                                (float)
                                                Math.Sin((initialShift + Game.UpdateTime.Total.TotalSeconds)*freq*2*
                                                         Math.PI);
        }

        /// <summary>
        /// Sets the current movement direction
        /// </summary>
        /// <param name="movementDirection"></param>
        public void SetMovement(Vector3 movementDirection)
        {
            CurrentVelocity = movementDirection;
        }

        public bool UpdateBodyRotation(Vector3 direction)
        {
            var targetYaw = WorldDirectionToRotation(direction);
            if (Math.Abs(targetYaw - BodyRotation) < YawThreshold)
                return true;

            BodyRotation = Utils.UpdateYaw(BodyRotation, targetYaw, RotationSpeed,
                (float)Game.UpdateTime.Elapsed.TotalSeconds);
            return false;
        }

        public bool UpdateHeadRotation(Vector3 direction)
        {
            var targetYaw = WorldDirectionToRotation(direction) - BodyRotation;
            if (Math.Abs(targetYaw - HeadRotation) < YawThreshold)
                return true;

            HeadRotation = Utils.UpdateYaw(HeadRotation, targetYaw, HeadRotationSpeed,
                (float)Game.UpdateTime.Elapsed.TotalSeconds);
            return false;
        }

        public bool UpdateHeadRotation(float targetLocalRotation)
        {
            if (Math.Abs(targetLocalRotation - HeadRotation) < YawThreshold)
                return true;

            HeadRotation = Utils.UpdateYaw(HeadRotation, targetLocalRotation, HeadRotationSpeed,
                (float)Game.UpdateTime.Elapsed.TotalSeconds);
            return false;
        }

        /// <summary>
        /// Checks if the drone is targeting the given direction
        /// </summary>
        /// <returns>True if facing this world direction, false otherwise</returns>
        public bool IsTargeting(Vector3 direction)
        {
            float targetRotation = WorldDirectionToRotation(direction);
            return IsTargeting(WorldToLocalHeadRotation(targetRotation));
        }

        /// <summary>
        /// Checks if the drone is looking in the given local rotation
        /// </summary>
        /// <returns>True if looking this way, false otherwise</returns>
        public bool IsTargeting(float targetLocalHeadRotation)
        {
            if (Math.Abs(targetLocalHeadRotation - HeadRotation) < YawThreshold)
                return true;
            return false;
        }

        public float WorldToLocalHeadRotation(float angle)
        {
            return angle - BodyRotation;
        }

        public void Stun()
        {
            stunned = true;
        }

        public void CancelStun()
        {
            stunned = false;
        }

        public void Damage(int damage)
        {
            if (IsDead)
                return;

            bool wasAlive = HealthPoints > 0;

            HealthPoints -= damage;

            if (wasAlive && IsDead)
            {
                Died?.Invoke(this);
                OnDie();
            }
            
            hitSounds.StopAll();
            hitSounds.PlayAndForget();
        }

        protected virtual void OnDie()
        {
            Script.AddTask(Death);
        }

        /// <summary>
        /// Default drone death sequence
        /// </summary>
        /// <returns></returns>
        protected async Task Death()
        {
            // Ensure health is 0
            HealthPoints = 0;

            // Stop moving
            Character.SetVelocity(Vector3.Zero);

            deathSound.PlayAndForget();
            engineSound.Stop();
            EngineParticle.ParticleSystem.StopEmitters();

            // Disable collider
            Character.Enabled = false;

            await Task.Delay(250);

            Model.Enabled = false;

            // Spawn exploded drone
            var explosionEntities = DroneExplosionPrefab.Instantiate();
            foreach (var entity in explosionEntities)
            {
                entity.Transform.Position += Entity.Transform.Position;
                // Inherit yaw
                entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(BodyRotation, 0, 0);

                // Add to the 
                Entity.Scene.Entities.Add(entity);

                if (entity.Name.StartsWith("DroneFr"))
                {
                    var comp = entity.Get<RigidbodyComponent>();
                    var dir = Entity.Transform.Position - SoldierController.Instance.Entity.Transform.Position;
                    dir.Normalize();
                    // Angle up
                    dir += Vector3.UnitY / (float)Math.Tan(MathUtil.DegreesToRadians(10));
                    dir.Normalize();
                    comp.ApplyImpulse(dir * 3.0f);
                    Script.AddTask(async () =>
                    {
                        await Task.Delay(4000);
                        if (!entity.IsDisposed)
                            comp.Enabled = false;
                        await Task.Delay(10000);
                        if (!entity.IsDisposed)
                            Entity.Scene.Entities.Remove(entity);
                    });
                }
            }

        await Task.Delay(3000);

            // Despawn after death sequence
            SceneSystem.SceneInstance.RootScene.Entities.Remove(Entity);
        }

        private void ApplyBodyRotation(float bodyRotation)
        {
            Entity.Transform.RotationEulerXYZ = new Vector3(0.0f, bodyRotation, 0.0f);
        }

        private float currentHeadRotation;
        private DroneWeapon weapon;

        /// <summary>
        /// Set local head rotation on the model
        /// </summary>
        /// <param name="angle">The local rotation in radians from the starting position</param>
        private void ApplyHeadRotation(float angle)
        {
            currentHeadRotation = angle;
        }

        internal void PostTransform()
        {
            for (int i = 0; i < Model.Skeleton.Nodes.Length; ++i)
            {
                var name = Model.Skeleton.Nodes[i].Name;

                if (name == HeadBoneName)
                {
                    var originalBone = bones[name];
                    var rotation = Quaternion.RotationYawPitchRoll(
                        currentHeadRotation - MathUtil.PiOverTwo,
                        // 1/4 revolution offset since the head is rotated by this amount (pointing to the right)
                        originalBone.EulerAngles.Y,
                        originalBone.EulerAngles.Z);
                    Model.Skeleton.NodeTransformations[i].Transform.Rotation = rotation;
                }
            }
        }

        struct Bone
        {
            /// <summary>
            /// Yaw, Pitch, Roll (X,Y,Z)
            /// </summary>
            public Vector3 EulerAngles;
        }
    }
}