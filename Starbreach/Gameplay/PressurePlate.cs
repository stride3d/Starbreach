// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Engine;
using Xenko.Engine.Events;
using Xenko.Rendering;
using MathUtil = Xenko.Core.Mathematics.MathUtil;

namespace Starbreach.Gameplay
{
    /// <summary>
    /// Handle presure plate smoothing and controlling action receivers
    /// </summary>
    public class PressurePlate : SyncActivator
    {
        /// <summary>
        /// Pressed state of the plate (1.0f is down, 0.0f is all the way up/not pressed).
        /// </summary>
        /// <remarks>State changes will trigger once all the way in one direction</remarks>
        private float currentValue = 0.0f;

        private bool nextState = false;
        private EventReceiver<bool> triggerEventReceiver;
        private bool enabled = true;

        private Material[] materials0 = new Material[4];
        private Material[] materials1 = new Material[4];
        private bool currentState = false;
        private bool toggledState = false;

        /// <summary>
        /// Is the plate going up or down (1.0f is down)
        /// </summary>
        private float currentDirection => nextState ? 1.0f : -1.0f;

        public ModelComponent Model { get; set; }

        [DataMember(1000)]
        public Material Material0DisabledOff
        {
            get { return materials0[0]; }
            set { materials0[0] = value; }
        }

        [DataMember(1000)]
        public Material Material0DisabledOn
        {
            get { return materials0[1]; }
            set { materials0[1] = value; }
        }

        [DataMember(1000)]
        public Material Material0EnabledOff
        {
            get { return materials0[2]; }
            set { materials0[2] = value; }
        }

        [DataMember(1000)]
        public Material Material0EnabledOn
        {
            get { return materials0[3]; }
            set { materials0[3] = value; }
        }

        [DataMember(1000)]
        public Material Material1DisabledOff
        {
            get { return materials1[0]; }
            set { materials1[0] = value; }
        }

        [DataMember(1000)]
        public Material Material1DisabledOn
        {
            get { return materials1[1]; }
            set { materials1[1] = value; }
        }

        [DataMember(1000)]
        public Material Material1EnabledOff
        {
            get { return materials1[2]; }
            set { materials1[2] = value; }
        }

        [DataMember(1000)]
        public Material Material1EnabledOn
        {
            get { return materials1[3]; }
            set { materials1[3] = value; }
        }

        public PressurePlateTrigger Trigger { get; set; }

        public AudioEmitterComponent AudioEmitter { get; set; }

        /// <summary>
        /// Is this pressure plate responding to input, or locked
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
                UpdateVisuals();
            }
        }

        /// <summary>
        /// If <c>true</c> this trigger will toggle every time it goes from disabled to enabled
        /// </summary>
        public bool Toggle { get; set; } = false;

        /// <summary>
        /// Stay enabled once activated
        /// </summary>
        public bool SingleActivation { get; set; } = false;

        /// <summary>
        /// The current toggle state, so the initial value can be set
        /// </summary>
        public bool CurrentToggleState
        {
            get { return toggledState; }
            set
            {
                toggledState = value; 
                UpdateVisuals();
            } 
        }

        public override bool CurrentState => Toggle ? toggledState : currentState;

        /// <summary>
        /// The transition time of the pressure plate from enabled to disabled. 
        /// Used to simulate the button being gradually pressed down until it triggers
        /// </summary>
        public float TransitionTime { get; set; } = 0.15f;

        [DataMemberIgnore]
        public override EventKey<bool> Changed { get; } = new EventKey<bool>("Pressure Plate", "State");

        public override void Start()
        {
            base.Start();

            if (Trigger == null)
                throw new ArgumentException($"{nameof(Trigger)} is not set");

            triggerEventReceiver = new EventReceiver<bool>(Trigger.Changed, EventReceiverOptions.Buffered);

            UpdateVisuals();
        }

        public override void Cancel()
        {
            base.Cancel();
            triggerEventReceiver.Dispose();
        }

        public override void Update()
        {
            // Await pressure plate state changes
            bool newState;
            if (triggerEventReceiver.TryReceive(out newState))
            {
                if (Enabled)
                {
                    if (newState != nextState)
                    {
                        nextState = newState;
                    }
                }
            }

            // Button smoothing
            currentValue += currentDirection*(float)Game.UpdateTime.Elapsed.TotalSeconds;
            currentValue = MathUtil.Clamp(currentValue, 0.0f, TransitionTime);

            // Trigger a new state or a toggle when fully pressed down or released
            if (nextState != currentState)
            {
                currentValue += currentDirection*(float)Game.UpdateTime.Elapsed.TotalSeconds;
                if (nextState)
                {
                    if (currentValue >= TransitionTime)
                    {
                        // Disable after one press if single activation is on
                        if (SingleActivation)
                            Enabled = false;

                        currentState = true;
                        currentValue = TransitionTime;

                        toggledState = !toggledState;

                        Changed.Broadcast(CurrentState);
                        PlayStateSound();
                        UpdateVisuals();
                    }
                }
                else
                {
                    if (currentValue <= 0.0f)
                    {
                        currentState = false;
                        currentValue = 0.0f;

                        // Toggle only changed when activated
                        if (!Toggle)
                        {
                            Changed.Broadcast(CurrentState);
                            PlayStateSound();
                        }

                        UpdateVisuals();
                    }
                }
            }
        }

        private void PlayStateSound()
        {
            if (CurrentState)
                AudioEmitter?["Enable"].PlayAndForget();
            else
                AudioEmitter?["Disable"].PlayAndForget();
        }

        private void UpdateVisuals()
        {
            if (Model == null)
                return;

            int index = Enabled ? 2 : 0;
            if (CurrentState)
                ++index;
            Model.Materials[0] = materials0[index];
            Model.Materials[1] = materials1[index];
        }
    }
}