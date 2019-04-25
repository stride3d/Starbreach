// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Starbreach.Core;
using Xenko.Core.Mathematics;
using Xenko.Audio;
using Xenko.Engine;
using Xenko.Physics;
using Xenko.Rendering;

namespace Starbreach.Drones
{
    class PatrolState : State
    {
        public new const string Name = "Patrol";

        public PatrolState() : base(Name)
        {
        }

        public Waypoint NextWaypoint;
        public Entity ChaseTarget;
        public PhysicsComponent ChaseColliderTarget;
        public IEnumerator<Vector3> MoveOperation = null;
    }

    class ChaseState : State
    {
        public new const string Name = "Chase";

        public ChaseState() : base(Name)
        {
        }

        public Entity ChaseTarget;
        public PhysicsComponent ChaseColliderTarget;
        public IEnumerator<Vector3> MoveOperation = null;
        public Vector3 CurrentChaseTargetPosition = new Vector3();
    }

    public class PatrollingDroneController : DroneControllerBase
    {
        public const string StartState = "Start";
        public const string DeathState = "Death";

        public DronePath PathToFollow;

        private FiniteStateMachine stateMachine;

        private RigidbodyComponent alertZoneTrigger;

        private Vector3 spawnLocation;

        private Tuple<float, float> spawnOrientation;

        public Entity AlertZoneTriggerEntity { get; set; }

        /// <summary>
        /// Alert zone radius when chasing a player
        /// </summary>
        public float ChaseAlertZoneRadius = 20.0f;

        /// <summary>
        /// Alert zone radius when not detecting a player
        /// </summary>
        public float IdleAlertZoneRadius = 10.0f;

        public override void Start()
        {
            base.Start();

            if (Entity.Transform.Parent != null) throw new ArgumentException("DroneRocketController must be root");

            alertZoneTrigger = AlertZoneTriggerEntity.Get<RigidbodyComponent>();

            stateMachine = new FiniteStateMachine("Drone");
            stateMachine.RegisterState(new State(StartState)
            {
                EnterMethod = StartEnter,
                UpdateMethod = () => { stateMachine.SwitchTo(PatrolState.Name); }
            });
            stateMachine.RegisterState(new PatrolState()
            {
                EnterMethod = PatrolEnter,
                UpdateMethod = PatrolUpdate
            });
            stateMachine.RegisterState(new ChaseState()
            {
                EnterMethod = ChaseStart,
                UpdateMethod = ChaseUpdate
            });
            stateMachine.Start(Script, StartState);

            // Exit state machine when drone died
            Drone.Died += drone => stateMachine.Exit();
            Drone.AlertedChanged += DroneOnAlertedChanged;
            UpdateAlertZoneRadius();

            spawnLocation = Entity.Transform.WorldMatrix.TranslationVector;
            spawnOrientation = new Tuple<float, float>(Drone.BodyRotation, Drone.HeadRotation);
        }

        public override void Cancel()
        {
            base.Cancel();
            stateMachine.Exit();
        }

        private async Task StartEnter(State last)
        {
            Drone.Model.Enabled = false;

            await Task.Delay(250);

            Drone.Model.Enabled = true;

            await Task.Delay(250);
        }

        private Task PatrolEnter(State from)
        {
            PatrolState patrolState = stateMachine.GetCurrentState<PatrolState>();
            if (patrolState == null)
                throw new InvalidOperationException("PatrolEnter can only be used with PatrolState");

            if (PathToFollow != null)
            {
                // Select a waypoint to patrol
                patrolState.NextWaypoint =
                    PathToFollow.Path.SelectWaypoint(Entity.Transform.WorldMatrix.TranslationVector);
                patrolState.MoveOperation = Move(patrolState.NextWaypoint.Position);
            }
            else
            {
                // Disable warnings for GDC
                //Log.Warning($"Patrolling Drone {Entity} doesn't have a follow path assigned")
                // Return to spawn
                patrolState.MoveOperation = Move(spawnLocation);
            }

            return Task.FromResult(0);
        }

        private void PatrolUpdate()
        {
            PatrolState patrolState = stateMachine.GetCurrentState<PatrolState>();
            if (patrolState == null)
                throw new InvalidOperationException("PatrolUpdate can only be used with PatrolState");

            // Move the drone on the current path, until the end of the move target is reached
            if (patrolState.MoveOperation != null)
            {
                if (!patrolState.MoveOperation.MoveNext())
                {
                    // Continue on path (if assigned)
                    if (PathToFollow != null)
                    {
                        // Done moving
                        patrolState.NextWaypoint = patrolState.NextWaypoint.Next;
                        if (patrolState.NextWaypoint == null)
                            patrolState.NextWaypoint = PathToFollow.Path.Waypoints[0]; // Loop back to first waypoint
                        patrolState.MoveOperation = Move(patrolState.NextWaypoint.Position);
                    }
                    else
                    {
                        // No move moving, this was a single target move
                        patrolState.MoveOperation = null;
                    }
                }
            }
            else
            {
                // Not moving and no path to follow, reset to spawn rotation
                Drone.UpdateBodyRotation(Drone.RotationToWorldDirection(spawnOrientation.Item1));
                Drone.UpdateHeadRotation(spawnOrientation.Item2);
            }

            // Look in moving direction
            Vector3 dir = Drone.CurrentVelocity;
            dir.Normalize();
            if (dir != Vector3.Zero)
            {
                Drone.UpdateHeadRotation(dir);
            }
            Drone.Alerted = false;

            // Check for enemies
            foreach (var collision in alertZoneTrigger.Collisions)
            {
                var targetCollider = collision.ColliderA.Entity != alertZoneTrigger.Entity
                    ? collision.ColliderA
                    : collision.ColliderB;

                if (Drone.Stunned)
                {
                    if (targetCollider.CollisionGroup != CollisionFilterGroups.CustomFilter3)
                        continue;
                }
                else
                {
                    if (targetCollider.CollisionGroup != CollisionFilterGroups.CharacterFilter &&
                        targetCollider.CollisionGroup != CollisionFilterGroups.CustomFilter1)
                        continue;
                }

                var enemy = Utils.GetDestructible(targetCollider.Entity);
                if (targetCollider.Entity == Entity || enemy == null || enemy.IsDead)
                    continue;

                // Visibility check
                bool hasLineOfSight = CheckLineOfSight(Entity, targetCollider);
                if (hasLineOfSight)
                {
                    // Start chasing state
                    patrolState.ChaseTarget = targetCollider.Entity;
                    patrolState.ChaseColliderTarget = targetCollider;
                    stateMachine.SwitchTo(ChaseState.Name);
                }

                break;
            }
        }

        private Task ChaseStart(State from)
        {
            PatrolState patrolState = (PatrolState)from;
            if (patrolState == null)
                throw new InvalidOperationException("ChaseState can only be entered from PatrolState");

            ChaseState chaseState = stateMachine.GetCurrentState<ChaseState>();
            if (chaseState == null) throw new InvalidOperationException("ChaseStart can only be used with ChaseState");


            chaseState.ChaseTarget = patrolState.ChaseTarget;
            chaseState.ChaseColliderTarget = patrolState.ChaseColliderTarget;

            return Task.FromResult(0);
        }

        private void ChaseUpdate()
        {
            Drone.Alerted = true;

            ChaseState chaseState = stateMachine.GetCurrentState<ChaseState>();
            if (chaseState == null) throw new InvalidOperationException("ChaseUpdate can only be used with ChaseState");

            if (chaseState.ChaseTarget == null)
            {
                // Stop chasing
                stateMachine.SwitchTo(PatrolState.Name);
                return;
            }

            IDestructible destructible = Utils.GetDestructible(chaseState.ChaseTarget);
            if (destructible == null) throw new InvalidOperationException("ChaseTarget can only target IDestructibles");
            if (destructible.IsDead)
            {
                // Stop chasing
                stateMachine.SwitchTo(PatrolState.Name);
                return;
            }

            // Check if still overlapping
            bool withinRange = false;
            foreach (var collision in alertZoneTrigger.Collisions)
            {
                var targetCollider = collision.ColliderA.Entity != alertZoneTrigger.Entity
                    ? collision.ColliderA
                    : collision.ColliderB;
                if (targetCollider == chaseState.ChaseColliderTarget)
                {
                    withinRange = true;
                    break;
                }
            }

            if (!withinRange)
            {
                // Stop chasing
                stateMachine.SwitchTo(PatrolState.Name);
                return;
            }

            // Recalculate path to player?
            Vector3 actualTargetPos = chaseState.ChaseTarget.Transform.WorldMatrix.TranslationVector;

            var source = Entity.Transform.WorldMatrix.TranslationVector;
            var target = actualTargetPos;
            source.Y = 1.5f;
            target.Y = 1.5f;
            Vector3 aimDir = target - source;
            float distToTarget = aimDir.Length();
            aimDir.Normalize();
            var playerTargeted = Drone.UpdateHeadRotation(aimDir);

            // Process move step
            if (chaseState.MoveOperation != null)
            {
                if (!chaseState.MoveOperation.MoveNext())
                    chaseState.MoveOperation = null;
            }

            bool hasLineOfSight = CheckLineOfSight(Entity, chaseState.ChaseColliderTarget);
            if (hasLineOfSight)
            {
                    if (distToTarget < 6.0f)
                    {
                    	// No longer need to move, player is in line of sight, and drone is pretty close
                    	chaseState.MoveOperation = null;
                    	Drone.SetMovement(Vector3.Zero);
                	}

                    if (playerTargeted)
                    {
                        // Shoot the player
                        Drone.Weapon?.TryShoot(chaseState.ChaseTarget);
                    }
            }

            // Update path towards player when either not moving or 
            //  the current path would end up too far from the player
            float targetDistance = (actualTargetPos - chaseState.CurrentChaseTargetPosition).Length();
            if (chaseState.MoveOperation == null || targetDistance > 1.0f)
            {
                chaseState.CurrentChaseTargetPosition = chaseState.ChaseTarget.Transform.WorldMatrix.TranslationVector;
                chaseState.MoveOperation = Move(chaseState.CurrentChaseTargetPosition);
            }
        }

        private bool CheckLineOfSight(Entity viewerEntity, PhysicsComponent targetCollider)
        {
            var source = viewerEntity.Transform.WorldMatrix.TranslationVector;
            var target = targetCollider.Entity.Transform.WorldMatrix.TranslationVector;
            source.Y = 0.5f;
            target.Y = 0.5f;

            var simulation = this.GetSimulation();
            var raycast = simulation.Raycast(source, target, CollisionFilterGroups.AllFilter, 
                (CollisionFilterGroupFlags)targetCollider.CollisionGroup | CollisionFilterGroupFlags.StaticFilter | CollisionFilterGroupFlags.DefaultFilter);
            return raycast.Succeeded && raycast.Collider == targetCollider;
        }

        private void DroneOnAlertedChanged(Drone drone, bool newState)
        {
            UpdateAlertZoneRadius();
        }

        private void UpdateAlertZoneRadius()
        {
            alertZoneTrigger.CanScaleShape = false;

            if (Drone.Alerted) // Alerted
            {
                alertZoneTrigger.ColliderShape.Scaling = new Vector3(ChaseAlertZoneRadius);
            }
            else
            {
                alertZoneTrigger.ColliderShape.Scaling = new Vector3(IdleAlertZoneRadius);
            }
        }
    }
}