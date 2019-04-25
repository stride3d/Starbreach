// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xenko.Engine.Processors;

namespace Starbreach.Core
{
    public class FiniteStateMachine
    {
        private readonly Dictionary<string, State> states = new Dictionary<string, State>();

        private State currentState;
        private State nextState;
        private bool exited;
        private ScriptSystem scriptSystem;

        public double TimeInCurrentState { get; private set; } = 0.0f;

        public FiniteStateMachine(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string CurrentStateName => currentState?.Name;

        public State GetCurrentState()
        {
            return currentState;
        }
        public T GetCurrentState<T>() where T : State
        {
            return (T)currentState;
        }

        public void RegisterState(State state)
        {
            states.Add(state.Name, state);
        }

        public void SwitchTo(string stateName)
        {
            if (stateName != CurrentStateName)
            {
                var state = states[stateName];
                if (state != nextState)
                {
                    nextState = state;
                    TimeInCurrentState = 0;
                    //Debug.WriteLine($"FSM: Machine [{Name}] switching from [{CurrentState}] to [{stateName}]");
                }
            }
        }

        public void Start(ScriptSystem script, string initialStateName)
        {
            exited = false;
            scriptSystem = script;
            scriptSystem.AddTask(Run);
            nextState = states[initialStateName];
        }

        public void Exit()
        {
            exited = true;
        }

        public async Task Run()
        {
            while (!exited)
            {
                if (nextState != null)
                {
                    if (currentState != null)
                    {
                        await currentState.Exit(nextState);
                    }
                    var previousState = currentState;
                    currentState = nextState;

                    await currentState.Enter(previousState);
                }
                nextState = null;
                currentState?.Update();
                TimeInCurrentState += scriptSystem.Game.UpdateTime.Elapsed.TotalSeconds ;
                await scriptSystem.NextFrame();
            }
        }
    }
}
