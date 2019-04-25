// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Starbreach.Core;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Animations;
using Xenko.Engine;

namespace Starbreach.Drones
{
    public class DroneAnimation : SyncScript
    {
        public const string IdleState = "Idle";
        public const string MoveState = "Move";
        public const string FireState = "Fire";

        private FiniteStateMachine stateMachine;
        private PlayingAnimation shootingAnimation;

        public AnimationComponent Animation { get; set; }

        public Drone Drone { get; set; }

        public float IdleSpeedTreshold { get; set; } = 0.2f;

        public override void Start()
        {
            base.Start();

            Drone = Entity.Get<Drone>();

            stateMachine = new FiniteStateMachine("DroneAnimation");
            stateMachine.RegisterState(new State(IdleState) {EnterMethod = StartIdle});
            stateMachine.RegisterState(new State(MoveState) {EnterMethod = StartMove});
            stateMachine.Start(Script, IdleState);
        }

        public override void Cancel()
        {
            base.Cancel();
            stateMachine?.Exit();
        }

        public void Shoot()
        {
            if (shootingAnimation != null)
                Animation.PlayingAnimations.Remove(shootingAnimation);
            shootingAnimation = Animation.Blend(FireState, 1.0f, TimeSpan.Zero);
            shootingAnimation.BlendOperation = AnimationBlendOperation.Add;
            shootingAnimation.RepeatMode = AnimationRepeatMode.PlayOnce;
        }

        private Task StartIdle(State arg)
        {
            Animation.Crossfade(IdleState, TimeSpan.FromSeconds(0.2f));
            return Task.FromResult(0);
        }
        
        private Task StartMove(State arg)
        {
            Animation.Crossfade(MoveState, TimeSpan.FromSeconds(0.2f));
            return Task.FromResult(0);
        }
        
        public override void Update()
        {
            if (Drone.CurrentVelocity.Length() < IdleSpeedTreshold)
            {
                if (stateMachine.CurrentStateName == MoveState)
                {
                    stateMachine.SwitchTo(IdleState);
                }
            }
            else
            {
                if (stateMachine.CurrentStateName == IdleState)
                {
                    stateMachine.SwitchTo(MoveState);
                }
            }
        }
    }
}