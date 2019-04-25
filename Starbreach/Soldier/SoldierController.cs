// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Starbreach.Camera;
using Starbreach.Core;
using Starbreach.Drones;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Engine.Events;
using Xenko.Physics;
using System.Collections.Generic;
using Xenko.Core.Collections;
using Xenko.Audio;
using Xenko.Particles;
using Xenko.UI;
using Xenko.UI.Controls;
using Xenko.UI.Panels;

namespace Starbreach.Soldier
{
    /// <summary>
    /// This script controls the soldier movement and orientation.
    /// </summary>
    public class SoldierController : SyncScript, IDestructible, IPlayer
    {
        public const string IdleState = "Idle";
        public const string RunState = "Run";
        public const string WalkState = "Walk";

        public static SoldierController Instance { get; private set; }

        public delegate void DamageTakenHandler(SoldierController soldier, int damage);

        public DamageTakenHandler OnDamageTaken;

        public Vector3 MoveDirection { get; private set; }
        public Vector2 AimDirection { get; private set; }
        private FiniteStateMachine stateMachine;
        private int controllerIndex;
        private Entity sphereCastOrigin;
        private CharacterComponent character;
        private SphereColliderShape usableOverlapShape = new SphereColliderShape(false, 0.5f);

        // Sounds
        private AudioEmitterSoundController[] hitSounds;
        private AudioEmitterSoundController deathSound;
        private Random soundIndexGenerator = new Random();

        // UI
        //private UIComponent uiComponent;
        
        public bool IsAiming => Input.AimState || Input.FireState;

        public float RunSpeed { get; set; } = 1.0f;

        public float WalkSpeed { get; set; } = 0.5f;

        [DataMemberRange(0, 1, 0.05, 0.1, 3)]
        public float RotationSpeed { get; set; } = 0.2f;

        public float YawSpeedDuringAim { get; set; } = 45.0f;

        public CameraComponent Camera { get; set; }

        public AnimationComponent AnimationComponent { get; set; }

        public CameraController CameraController { get; set; }

        public Entity DroneCrosshair { get; set; }

        public bool IsEnabled { get; set; } = false;

        public new SoldierPlayerInput Input;

        [DataMemberIgnore]
        public float Yaw { get; set; }

        [DataMemberIgnore]
        public bool IsAlive { get; private set; } = true;

        public void Init(IPlayerInput input)
        {
            Input = input as SoldierPlayerInput;
            if(Input == null) throw new ArgumentException("Player Input was not of type SoldierPlayerInput");
        }

        public static readonly int MaxHealthPoints = 250;

        [DataMemberIgnore]
        public int HealthPoints { get; private set; } = MaxHealthPoints;

        public override void Start()
        {
            if (Entity.Transform.Parent != null) throw new ArgumentException("SoldierController must be root");
            if (Camera == null) throw new ArgumentException("Camera is not set");
            if (AnimationComponent == null) throw new ArgumentException("AnimationComponent is not set");
            if (CameraController == null) throw new ArgumentException("CameraController is not set");
            if (DroneCrosshair == null) throw new ArgumentException("DroneCrosshair is not set");
            if (RotationSpeed < 0 || RotationSpeed > 1) throw new ArgumentException("Rotation Speed must be between 0 and 1");

            //this.GetSimulation().ColliderShapesRendering = true;

            // Enable UI
            IStarbreach ipbrGame = Game as IStarbreach;
            //Entity statusBarEntity = ipbrGame.PlayerUiEntity.FindChild("StatusBar");
            //uiComponent = statusBarEntity.Get<UIComponent>();
            //uiComponent.Enabled = true;

            stateMachine = new FiniteStateMachine("SoldierController");
            stateMachine.RegisterState(new State(IdleState) { UpdateMethod = UpdateIdle });
            stateMachine.RegisterState(new State(RunState) { UpdateMethod = UpdateRun });
            stateMachine.RegisterState(new State(WalkState) { EnterMethod = StartWalk, UpdateMethod = UpdateWalk });
            stateMachine.Start(Script, IdleState);
            Instance = this;

            character = Entity.Get<CharacterComponent>();
            sphereCastOrigin = Entity.FindChild("SphereCastOrigin");

            AudioEmitterComponent emitter = Entity.Get<AudioEmitterComponent>();

            // Load 3 different being hit sounds
            hitSounds = new AudioEmitterSoundController[3];
            for (int i = 0; i < 3; i++)
            {
                hitSounds[i] = emitter["Hit" + i];
                hitSounds[i].IsLooping = false;
            }
            deathSound = emitter["Death"];
        }

        public override void Update()
        {
            UpdateUI();

            if (!IsEnabled)
                return;

            // Do nothing if we're dead
            if (HealthPoints <= 0)
                return;
            
            AimDirection = Input.AimDirection;

            // Update movement direction
            MoveDirection = Utils.LogicDirectionToWorldDirection(Input.MoveDirection, Camera.Entity);

            // Check for state transition from movement Input
            if (MoveDirection == Vector3.Zero && stateMachine?.CurrentStateName == RunState)
            {
                // Currently in Run, but movement stopped. Switch to Idle.
                stateMachine?.SwitchTo(IdleState);
            }
            else if (MoveDirection != Vector3.Zero && stateMachine?.CurrentStateName == IdleState)
            {
                // Currently in Idle, but movement started. Switch to Run.
                stateMachine?.SwitchTo(RunState);
            }

            // Check for state transition from aim Input
            if (IsAiming && stateMachine != null && stateMachine.CurrentStateName != WalkState)
            {
                // Currently in Run or Idle, aim toggled. Switch to Walk.
                stateMachine?.SwitchTo(WalkState);
            }
            else if (!IsAiming && stateMachine != null && stateMachine.CurrentStateName == WalkState)
            {
                // Currently in Walk, aim toggled. Switch to Idle.
                stateMachine?.SwitchTo(IdleState);
            }

            // Check which nearby objects soldier can interact with
            Vector3 sweepDirection = -AnimationComponent.Entity.Transform.WorldMatrix.Forward;
            Matrix sweepStart = sphereCastOrigin.Transform.WorldMatrix;
            Matrix sweepEnd = sphereCastOrigin.Transform.WorldMatrix * Matrix.Translation(sweepDirection * 2.0f);
            var sphereHits = this.GetSimulation().ShapeSweepPenetrating(usableOverlapShape, sweepStart, sweepEnd);
            float closestUsableDistance = float.MaxValue;
            IUsable closestUsable = null;
            Vector3 castOriginPosition = sphereCastOrigin.Transform.WorldMatrix.TranslationVector;
            foreach (HitResult hit in sphereHits)
            {
                IUsable usable = Utils.GetUsable(hit.Collider.Entity);
                if (usable != null)
                {
                    Vector3 otherPosition = hit.Collider.Entity.Transform.WorldMatrix.TranslationVector;
                    float dist = (otherPosition - castOriginPosition).Length();
                    if (dist < closestUsableDistance)
                    {
                        closestUsableDistance = dist;
                        closestUsable = usable;
                    }
                }
            }

            // TODO move to interact event handler
            // Use usable object if there is one nearby
            //if(closestUsable != null && closestUsable.CanBeUsed)
            //{
            //    bool interacting = false;
            //
            //    closestUsable.Use();
            //
            //    Game.DebugPrint($"Press to use [{closestUsable.Name}]");
            //}
        }
        
        public override void Cancel()
        {
            // Disable UI
            //uiComponent.Enabled = false;
            stateMachine.Exit();
        }

        private void SetUIBar(string barGridName, float value)
        {
            //Grid barGrid = uiComponent.Page.RootElement.FindNameRecursive(barGridName) as Grid;
            float healthPercent = value;
            healthPercent *= 100.0f;
            if (healthPercent < 4.0f && healthPercent > 0.0f)
                healthPercent = 4.0f;
            //barGrid.ColumnDefinitions[0].SizeValue = healthPercent;
            //barGrid.ColumnDefinitions[1].SizeValue = (100.0f - healthPercent);
        }

        private void UpdateUI()
        {
            //SetUIBar("lifebarGrid", (float) HealthPoints/(float) MaxHealthPoints);
            //TextBlock timerText = uiComponent.Page.RootElement.FindNameRecursive("timer") as TextBlock;
        }

        private void UpdateIdle()
        {
            // Stop moving
            Move(0.0f);
            AnimationComponent.Entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(Yaw), 0, 0);
        }

        private void UpdateRun()
        {
            // Apply movement
            Move(RunSpeed);
            // Apply character rotation
            SmoothRotate(RotationSpeed);
        }

        private Task StartWalk(State arg)
        {
            // Reset the yaw of the SoldierController to match the camera yaw
            Yaw = CameraController.Yaw;
            return Task.FromResult(0);
        }

        private void UpdateWalk()
        {
            // Apply movement
            Move(WalkSpeed);

            // Update yaw from aim direction
            var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            Yaw += -AimDirection.X * YawSpeedDuringAim * dt;

            AnimationComponent.Entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(Yaw), 0, 0);

            CameraController.Yaw = Yaw;
            // TODO: make this customizable
            CameraController.Pitch = 10.0f;
        }

        private void SmoothRotate(float speed)
        {
            // Compute target yaw from the movement direction
            var targetYaw = (float)Math.Atan2(-MoveDirection.Z, MoveDirection.X) + MathUtil.PiOverTwo;
            // Update the orientation of the soldier (lower pass filter to smooth rotation)
            float yawRadians = Utils.LerpYaw(MathUtil.DegreesToRadians(Yaw), targetYaw, speed);
            // Update the soldier rotation according to the yaw
            AnimationComponent.Entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(Yaw), 0, 0);

            Yaw = MathUtil.RadiansToDegrees(yawRadians);
        }

        private void Move(float speed)
        {
            // Use the delta time from physics
            character.SetVelocity(MoveDirection * speed);
        }

        public void Damage(int damage)
        {
            bool wasDead = IsDead;

            // Let's keep the godmode for now
            //HealthPoints = Math.Max(0, HealthPoints - damage);
            if (HealthPoints > 0)
            {
                Game.VibratorSmooth(controllerIndex, new Vector2(0.50f, 0.45f), new Vector2(0.95f, 0.90f), TimeSpan.FromSeconds(0.5));

                // Stop other hit sounds
                for (int i = 0; i < hitSounds.Length; i++)
                    hitSounds[i].Stop();

                // Play being hit sound
                hitSounds[soundIndexGenerator.Next(0, hitSounds.Length - 1)].PlayAndForget();
            }
            else
            {
                // Only do this once, when dying
                if (!wasDead)
                {
                    // Play dying sound
                    deathSound.PlayAndForget();

                    Game.VibratorSmooth(controllerIndex, new Vector2(0.90f, 0.99f), new Vector2(0.0f, 0.0f), TimeSpan.FromSeconds(3));
                    Move(0.0f); //stop any motion
                    stateMachine?.SwitchTo(IdleState);
                    stateMachine?.Exit();
                    Script.AddTask(async () =>
                    {
                        await Game.WaitTime(TimeSpan.FromSeconds(3));
                        IsAlive = false;
                    });
                }
            }

            // Fire event
            OnDamageTaken?.Invoke(this, damage);
        }

        public bool IsDead => HealthPoints <= 0;
    }
}
