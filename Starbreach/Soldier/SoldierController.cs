// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Starbreach.Camera;
using Starbreach.Core;
using Starbreach.Drones;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Physics;
using System.Collections.Generic;
using Stride.Core.Collections;
using Stride.Audio;
using Stride.Particles;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

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
        /// <summary>
        /// Velocity per second averaged, the averaging window is 0.25 of a second.
        /// </summary>
        public Vector3 AverageVelocity { get; private set; }
        /// <summary>
        /// Average distance travelled scaled to a second,
        /// difference with <see cref="AverageVelocity"/> is that
        /// going back and forth still increases this value.
        /// </summary>
        public float DistanceTravelledAverage { get; private set; }
        public Vector2 AimDirection { get; private set; }
        
        private FiniteStateMachine stateMachine;
        private int controllerIndex;
        private Entity sphereCastOrigin;
        private CharacterComponent character;
        private SphereColliderShape usableOverlapShape = new SphereColliderShape(false, 0.5f);
        private Queue<(Vector3 vel, float dt)> rollingVelocities = new Queue<(Vector3, float)>();
        private Vector3 lastFramePos;
        private float currentRoll;

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

        public float RollMax { get; set; } = 0.35f;

        public float RollAccel { get; set; } = 160f;

        public float RollDecreaseSpeed { get; set; } = 10f;

        public CameraComponent Camera { get; set; }

        public AnimationComponent AnimationComponent { get; set; }

        public CameraController CameraController { get; set; }

        public Entity DroneCrosshair { get; set; }

        public bool IsEnabled { get; set; } = false;

        public new SoldierPlayerInput Input;

        [DataMemberIgnore]
        public Quaternion Rotation
        {
            get
            {
                return AnimationComponent.Entity.Transform.Rotation;
            }
            set
            {
                AnimationComponent.Entity.Transform.Rotation = value;
            }
        }

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
            var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            var deltaPos = Entity.Transform.Position - lastFramePos;
            lastFramePos = Entity.Transform.Position;
            rollingVelocities.Enqueue( (deltaPos, dt) );
            
            // Recompute every frame instead of storing to avoid values slowly drifting 
            // through floating point imprecision
            float totalDt = 0f;
            Vector3 aggregatedVel = default;
            Vector3 aggregatedAbsVel = default;
            foreach (var (v, oldDt) in rollingVelocities)
            {
                totalDt += oldDt;
                aggregatedVel += v;
                aggregatedAbsVel += new Vector3(MathF.Abs(v.X), MathF.Abs(v.Y), MathF.Abs(v.Z));
            }

            // We average over 0.25 of a second as if the data was for a full second,
            // Provides smoothed out but still useful data.
            while (totalDt > .25f)
            {
                // Remove data out of that time frame.
                var (v, oldDt) = rollingVelocities.Dequeue();
                totalDt -= oldDt;
                aggregatedVel -= v;
                aggregatedAbsVel -= new Vector3(MathF.Abs(v.X), MathF.Abs(v.Y), MathF.Abs(v.Z));
            }

            // Division by totalDt scales the result as if it was a full second,
            // easier to work with those kinds of ranges and easier to maintain if the averaging window changes 
            AverageVelocity = totalDt == 0f || aggregatedVel == default ? default : aggregatedVel / totalDt;
            DistanceTravelledAverage = totalDt == 0f || aggregatedAbsVel == default ? default : aggregatedAbsVel.Length() / totalDt;
            
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
            var sphereHits = new List<HitResult>();
            this.GetSimulation().ShapeSweepPenetrating(usableOverlapShape, sweepStart, sweepEnd, sphereHits);
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
            
            SmoothRotate(0f);
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
            return Task.FromResult(0);
        }

        private void UpdateWalk()
        {
            // Apply movement
            Move(WalkSpeed);

            // Update yaw from aim direction
            AnimationComponent.Entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(CameraController.Yaw), 0, 0);
        }
        
        private void SmoothRotate(float speed)
        {
            float dt = (float) Game.UpdateTime.Elapsed.TotalSeconds;
            float perFrameChange = speed * dt * 20f;

            // Increase rotation rate based on velocity to avoid hysteresis for tiny movement
            perFrameChange *= AverageVelocity.Length();
            perFrameChange = perFrameChange > 1f ? 1f : perFrameChange;
            
            // Compute target direction from actual movement direction
            var targetDir = AverageVelocity;
            targetDir.Y = 0f; // Ignore gravity
            targetDir = Vector3.Normalize(targetDir);

            Vector3 axis = Vector3.UnitZ;
            var currentDir = Vector3.Transform(axis, AnimationComponent.Entity.Transform.Rotation);
            var currentRot = Quaternion.BetweenDirections(axis, currentDir);
            var targetRot = Quaternion.BetweenDirections(axis, targetDir);
            var newRot = Quaternion.Slerp(currentRot, targetRot, perFrameChange);
            
            // Roll based on sharpness of turn (i.e.: amount of change from current to new)
            var roll = Quaternion.Dot(currentRot, newRot);
            roll = 1f - (roll*0.5f+0.5f); // remap dot [-1,1] -> [1,0]
            roll *= RollAccel;
            // Roll either way based on direction
            roll = Vector3.Dot(Vector3.Cross(targetDir, currentDir), Vector3.UnitY) > 0f ? roll : -roll;
            
            currentRoll += roll;
            currentRoll = MathUtil.Lerp(currentRoll, 0f, MathF.Min(dt*RollDecreaseSpeed, 1f));
            currentRoll = MathUtil.Clamp(currentRoll, -RollMax, RollMax);

            AnimationComponent.Entity.Transform.Rotation = Quaternion.RotationAxis(Vector3.UnitZ, currentRoll) * newRot;
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
