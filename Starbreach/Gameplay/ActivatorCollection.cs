// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;

namespace Starbreach.Gameplay
{
    /// <summary>
    /// Receiver of one or multiple <see cref="Activator"/>. Utility class to combine multiple activator events into a single state
    /// </summary>
    [DataContract]
    public class ActivatorCollection
    {
        /// <summary>
        /// Combination mode, either logic OR or AND
        /// </summary>
        public ActivationCombinationMode CombinationMode = ActivationCombinationMode.Disjunction;

        /// <summary>
        /// Invert the values of Enabled/Disabled
        /// </summary>
        public bool Inverted { get; set; } = false;

        public List<Activator> Activators { get; private set; } = new List<Activator>();

        /// <summary>
        /// State that was last evaluated by <see cref="Update"/>
        /// </summary>
        [DataMemberIgnore]
        public bool CurrentState { get; private set; } = false;
        
        public delegate void ChangedHandler(bool newState);

        /// <summary>
        /// Called when changed
        /// </summary>
        public event ChangedHandler Changed;
        
        public void Update()
        {
            ReEvaluate();
        }

        private void ReEvaluate()
        {
            bool nextState = false;
            if (CombinationMode == ActivationCombinationMode.Conjunction)
            {
                nextState = true;
                foreach (var activator in Activators)
                {
                    if (!activator.CurrentState)
                    {
                        nextState = false;
                        break;
                    }
                }
            }
            else if (CombinationMode == ActivationCombinationMode.Disjunction)
            {
                foreach (var activator in Activators)
                {
                    if (activator.CurrentState)
                    {
                        nextState = true;
                        break;
                    }
                }
            }

            // Invert
            if(Inverted)
                nextState = !nextState;

            if (nextState != CurrentState)
            {
                CurrentState = nextState;
                Changed?.Invoke(CurrentState);
            }
        }
    }
}