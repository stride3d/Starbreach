// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Starbreach.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Animations;
using Stride.Engine;
using Stride.Engine.Events;

namespace Starbreach.Soldier
{
    public class SoldierAnimation : SyncScript
    {
        public static class AnimKeys
        {
            public const string IdleLow = "Idle_Low";
            public const string IdleAim = "Idle_Aim";
            public const string RunLow = "Run_Low";
            public const string RunAim = "Run_Aim";
            public const string WalkForward = "Walk_Forward";
            public const string WalkBackward = "Walk_Backward";
            public const string WalkLeft = "Walk_Left";
            public const string WalkRight = "Walk_Right";
            public const string Death = "Death";
            public const string DrawWeapon = "Additive_Draw_Weapon";
            public const string HolsterWeapon = "Additive_Holster_Weapon";
            public const string FireWeapon = "Additive_Fire_Weapon";
            public const string ReloadWeapon = "Additive_Reload_Weapon";
            public const string TakeDamage = "Additive_Take_Damage";
        }

        public const string LowerIdleState = "Idle";
        public const string LowerRunState = "Run";
        public const string LowerWalkState = "Walk";

        public const string UpperLowState = "Low";
        public const string UpperAimState = "Aim";
        public const string UpperFireState = "Fire";

        private Vector3 moveDirection;
        private Vector3 aimDirection = -Vector3.UnitZ;
        private FiniteStateMachine lowerStateMachine;
        private FiniteStateMachine upperStateMachine;
        private PlayingAnimation lowerAnimation;
        private PlayingAnimation fireAnimation;
        private int additiveAnimations;
        private bool dead;

        public Vector2 WalkAngleThreshold { get; set; } = new Vector2(67.5f, 112.5f);

        public float WalkCrossfadeDuration { get; set; } = 0.2f;

        public float RunAnimSpeed { get; set; } = 0.3f;

        public float WalkAnimSpeed { get; set; } = 0.9f;

        public CameraComponent Camera { get; set; }

        public AnimationComponent AnimationComponent { get; set; }

        private bool IsIdle => lowerStateMachine.CurrentStateName == LowerIdleState;

        private bool IsRun => lowerStateMachine.CurrentStateName == LowerRunState;

        private bool HasAdditiveAnimation => additiveAnimations > 0;

        private int LowerAnimCount { get { var r = AnimationComponent.PlayingAnimations.IndexOf(x => x.BlendOperation != AnimationBlendOperation.LinearBlend); return r > 0 ? r : AnimationComponent.PlayingAnimations.Count; } }

        public SoldierController SoldierController;

        private SoldierWeapon soldierWeapon;

        public override void Start()
        {
            base.Start();
            if (Camera == null) throw new ArgumentException("The camera is not set");
            if (AnimationComponent == null) throw new ArgumentException("The animation component is not set");
            if (SoldierController == null) throw new ArgumentException("SoldierController is not set");

            InitializeLowerStateMachine();
            InitializeUpperStateMachine();

            SoldierController.OnDamageTaken += (soldier, damage) =>
            {
                if(dead)
                    return;
                if(soldier.IsDead)
                {
                    dead = true;
                    AnimationComponent.PlayingAnimations.ForEach(x => { x.WeightTarget = 0.0f; x.CrossfadeRemainingTime = TimeSpan.FromSeconds(0.2); });
                    var death = NewAnimation(AnimKeys.Death);
                    AddLowerAnimation(death);
                    lowerStateMachine?.Exit();
                    upperStateMachine?.Exit();
                }
                else
                {
                    for (var i = AnimationComponent.PlayingAnimations.Count - 1; i >= 0; i--)
                    {
                        if (AnimationComponent.PlayingAnimations[i].Name == AnimKeys.TakeDamage)
                            AnimationComponent.PlayingAnimations.RemoveAt(i);
                    }
                    AnimationComponent.PlayingAnimations.Add(NewAnimation(AnimKeys.TakeDamage, true));
                }
            };

            soldierWeapon = SoldierController.Entity.Get<SoldierWeapon>();
            soldierWeapon.OnReload += weapon =>
            {
                if(!dead) Script.AddTask(() => AddAdditiveAnimation(AnimKeys.ReloadWeapon));
            };
        }

        public override void Update()
        {
            // Check movement status and update states
            if (SoldierController.AverageVelocity.LengthSquared() < 0.1f)
            {
                if (IsRun)
                {
                    lowerStateMachine?.SwitchTo(LowerIdleState);
                }
                moveDirection = Vector3.Zero;
            }
            else
            {
                if (IsIdle)
                {
                    lowerStateMachine?.SwitchTo(LowerRunState);
                }
                moveDirection = Vector3.Normalize(SoldierController.AverageVelocity);
            }
            
            bool reloading = soldierWeapon.IsReloading;
            bool firing = SoldierController.Input.FireState;

            // Check aim status and update states
            aimDirection = Camera.Entity.Transform.WorldMatrix.TranslationVector - Entity.Transform.WorldMatrix.TranslationVector;
            aimDirection.Normalize();

            if (SoldierController.IsAiming)
            {
                if (lowerStateMachine != null && lowerStateMachine?.CurrentStateName != LowerWalkState)
                {
                    lowerStateMachine.SwitchTo(LowerWalkState);
                }
                if (!HasAdditiveAnimation)
                {
                    upperStateMachine.SwitchTo(firing && !reloading ? UpperFireState : UpperAimState);
                }
            }
            else
            {
                if (lowerStateMachine != null && lowerStateMachine.CurrentStateName == LowerWalkState)
                {
                    lowerStateMachine?.SwitchTo(LowerIdleState);
                }
                if (!HasAdditiveAnimation)
                {
                    upperStateMachine.SwitchTo(UpperLowState);
                }
            }
        }

        private void InitializeLowerStateMachine()
        {
            lowerStateMachine = new FiniteStateMachine("SoldierAnimationLower");

            Action<State, string, string> startIdleOrRun = (from, newAnimNameLow, newAnimNameAim) =>
            {
                // Select the proper animation to play
                lowerAnimation = NewAnimation(HasAdditiveAnimation ? newAnimNameAim : newAnimNameLow);
                if (from != null)
                {
                    // Blend in if needed
                    lowerAnimation.Weight = 0.0f;
                    lowerAnimation.WeightTarget = 1.0f;
                    lowerAnimation.CrossfadeRemainingTime = TimeSpan.FromSeconds(0.2);
                }
                // Add the animation
                InsertLowerAnimation(0, lowerAnimation);
            };

            Action updateLowerIdleOrRun = () =>
            {
                // Swap between the low animation (used if no additive animation) and the aim animation (used only for additive animation)
                if (HasAdditiveAnimation)
                {
                    SwapLowerAnimation(AnimKeys.IdleLow, AnimKeys.IdleAim, false);
                    SwapLowerAnimation(AnimKeys.RunLow, AnimKeys.RunAim, false);
                }
                else
                {
                    SwapLowerAnimation(AnimKeys.IdleAim, AnimKeys.IdleLow, true);
                    SwapLowerAnimation(AnimKeys.RunAim, AnimKeys.RunLow, true);
                }
                foreach (var anim in AnimationComponent.PlayingAnimations.Take(LowerAnimCount))
                {
                    if (anim.Name == AnimKeys.RunLow || anim.Name == AnimKeys.RunAim)
                        anim.TimeFactor = SoldierController.DistanceTravelledAverage * RunAnimSpeed;
                }
            };

            Action<State> exitLowerIdleOrRun = to =>
            {
                // Blend away the remaining animation
                lowerAnimation.WeightTarget = 0.0f;
                lowerAnimation.CrossfadeRemainingTime = TimeSpan.FromSeconds(0.2);
            };

            Action<State> startLowerWalk = from =>
            {
                // Add all the animations used for walking, enable only Idle at start
                AddLowerAnimation(NewAnimation(AnimKeys.IdleAim));
                AddLowerAnimation(NewAnimation(AnimKeys.WalkForward, 0.0f));
                AddLowerAnimation(NewAnimation(AnimKeys.WalkBackward, 0.0f));
                AddLowerAnimation(NewAnimation(AnimKeys.WalkLeft, 0.0f));
                AddLowerAnimation(NewAnimation(AnimKeys.WalkRight, 0.0f));
            };

            Action updateLowerWalk = () =>
            {
                var facingDirection = Vector3.Transform(-Vector3.UnitZ, AnimationComponent.Entity.Transform.Rotation);
                var dot = Vector3.Dot(moveDirection, facingDirection);
                string lowerAnimToPlay;
                var forwardThreshold = Math.Cos(MathUtil.DegreesToRadians(WalkAngleThreshold.X));
                var backwardThreshold = Math.Cos(MathUtil.DegreesToRadians(WalkAngleThreshold.Y));
                var crossfadeFactor = Game.UpdateTime.Elapsed.TotalSeconds / WalkCrossfadeDuration;
                // Find out which anim should be played
                if (moveDirection.LengthSquared() < 0.1f)
                {
                    lowerAnimToPlay = AnimKeys.IdleAim;
                }
                else if (dot > forwardThreshold)
                {
                    lowerAnimToPlay = AnimKeys.WalkForward;
                }
                else if (dot < backwardThreshold)
                {
                    lowerAnimToPlay = AnimKeys.WalkBackward;
                }
                else
                {
                    var cross = Vector3.Cross(aimDirection, moveDirection);
                    lowerAnimToPlay = cross.Y <= 0 ? AnimKeys.WalkLeft : AnimKeys.WalkRight;
                }

                foreach (var anim in AnimationComponent.PlayingAnimations.Take(LowerAnimCount))
                {
                    anim.TimeFactor = SoldierController.DistanceTravelledAverage * WalkAnimSpeed;
                    if (anim.Name == lowerAnimToPlay)
                        anim.Weight = (float)Math.Min(anim.Weight + crossfadeFactor, 1.0f);
                    else
                        anim.Weight = (float)Math.Max(anim.Weight - crossfadeFactor, 0.0f);
                }
            };

            Action<State> exitWalk = to =>
            {
                PlayingAnimation crossfadeAnim = null;
                var maxWeight = float.MinValue;

                foreach (var anim in AnimationComponent.PlayingAnimations.Take(LowerAnimCount).ToList())
                {
                    if (anim.Weight > maxWeight)
                    {
                        crossfadeAnim = anim;
                        maxWeight = anim.Weight;
                    }
                    // Remove all walking animation...
                    AnimationComponent.PlayingAnimations.Remove(anim);
                }
                // ... except the current one that will be blended away
                if (crossfadeAnim != null)
                {
                    InsertLowerAnimation(0, crossfadeAnim);
                    crossfadeAnim.WeightTarget = 0.0f;
                    crossfadeAnim.CrossfadeRemainingTime = TimeSpan.FromSeconds(0.2);
                }
            };

            var idleState = new State(LowerIdleState)
            {
                EnterMethod = State.ToTask(from => startIdleOrRun(from, AnimKeys.IdleLow, AnimKeys.IdleAim)),
                UpdateMethod = updateLowerIdleOrRun,
                ExitMethod = State.ToTask(to => exitLowerIdleOrRun(to))
            };

            var runState = new State(LowerRunState)
            {
                EnterMethod = State.ToTask(from => startIdleOrRun(from, AnimKeys.RunLow, AnimKeys.RunAim)),
                UpdateMethod = updateLowerIdleOrRun,
                ExitMethod = State.ToTask(to => exitLowerIdleOrRun(to))
            };

            var walkState = new State(LowerWalkState)
            {
                EnterMethod = State.ToTask(startLowerWalk),
                UpdateMethod = updateLowerWalk,
                ExitMethod = State.ToTask(to => exitWalk(to))
            };

            lowerStateMachine.RegisterState(idleState);
            lowerStateMachine.RegisterState(runState);
            lowerStateMachine.RegisterState(walkState);
            lowerStateMachine.Start(Script, LowerIdleState);
        }

        private void InitializeUpperStateMachine()
        {
            upperStateMachine = new FiniteStateMachine("SoldierAnimationLower");

            var lowState = new State(UpperLowState);
            var aimState = new State(UpperAimState)
            {
                EnterMethod = async from =>
                {
                    // Display the draw animation only if we come from the low state
                    if (!HasAdditiveAnimation && @from.Name == UpperLowState)
                    {
                        var animation = NewAnimation(AnimKeys.DrawWeapon, true);
                        AnimationComponent.PlayingAnimations.Add(animation);
                        await AnimationComponent.Ended(animation);
                    }
                },
                ExitMethod = async to =>
                {
                    // Display the draw animation only if we're going to the low state
                    if (!HasAdditiveAnimation && to.Name == UpperLowState)
                    {
                        var animation = NewAnimation(AnimKeys.HolsterWeapon, true);
                        AnimationComponent.PlayingAnimations.Add(animation);
                        await AnimationComponent.Ended(animation);
                    }
                }
            };
            var fireState = new State(UpperFireState)
            {
                EnterMethod = State.ToTask(from =>
                {
                    fireAnimation = NewAnimation(AnimKeys.FireWeapon, true);
                    AnimationComponent.PlayingAnimations.Add(fireAnimation);
                }),
                ExitMethod = State.ToTask(to => AnimationComponent.PlayingAnimations.Remove(fireAnimation))
            };
            upperStateMachine.RegisterState(lowState);
            upperStateMachine.RegisterState(aimState);
            upperStateMachine.RegisterState(fireState);
            upperStateMachine.Start(Script, UpperLowState);
        }

        private void SwapLowerAnimation(string source, string target, bool crossfade)
        {
            if (lowerAnimation.Name == source)
            {
                var sourceAnim = lowerAnimation;
                var i = AnimationComponent.PlayingAnimations.IndexOf(sourceAnim);
                lowerAnimation = NewAnimation(target, lowerAnimation.Weight);
                lowerAnimation.WeightTarget = sourceAnim.WeightTarget;
                lowerAnimation.CrossfadeRemainingTime = sourceAnim.CrossfadeRemainingTime;
                lowerAnimation.CurrentTime = sourceAnim.CurrentTime;
                lowerAnimation.TimeFactor = sourceAnim.TimeFactor;
                if (crossfade)
                {
                    sourceAnim.WeightTarget = 0.0f;
                    sourceAnim.CrossfadeRemainingTime = TimeSpan.FromSeconds(0.2f);
                    lowerAnimation.Weight = 0.0f;
                    lowerAnimation.WeightTarget = 1.0f;
                    lowerAnimation.CrossfadeRemainingTime = TimeSpan.FromSeconds(0.2f);
                    InsertLowerAnimation(i, lowerAnimation);
                }
                else
                {
                    AnimationComponent.PlayingAnimations[i] = lowerAnimation;
                }
            }
        }

        private void AddLowerAnimation(PlayingAnimation anim)
        {
            AnimationComponent.PlayingAnimations.Insert(LowerAnimCount, anim);
        }

        private void InsertLowerAnimation(int index, PlayingAnimation anim)
        {
            if (index > LowerAnimCount) throw new ArgumentOutOfRangeException();
            AnimationComponent.PlayingAnimations.Insert(index, anim);
        }

        private async Task AddAdditiveAnimation(string animName)
        {
            var animation = NewAnimation(animName, true);
            AnimationComponent.PlayingAnimations.Add(animation);
            ++additiveAnimations;
            await AnimationComponent.Ended(animation);
            --additiveAnimations;
        }

        private PlayingAnimation NewAnimation(string name, bool additive)
        {
            return NewAnimation(name, 1.0f, additive);
        }

        private PlayingAnimation NewAnimation(string name, float weight = 1.0f, bool additive = false)
        {
            var animation = AnimationComponent.NewPlayingAnimation(name);
            animation.Weight = weight;
            if (additive)
            {
                animation.BlendOperation = AnimationBlendOperation.Add;
            }
            return animation;
        }
    }
}