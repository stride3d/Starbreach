// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;

namespace Starbreach.Core
{
    public class State
    {
        public State(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public Func<State, Task> EnterMethod { get; set; }

        public Action UpdateMethod { get; set; }

        public Func<State, Task> ExitMethod { get; set; }

        public void Update()
        {
            UpdateMethod?.Invoke();
        }   

        public Task Enter(State from)
        {
            return EnterMethod?.Invoke(from) ?? Task.FromResult(0);
        }

        public Task Exit(State to)
        {
            return ExitMethod?.Invoke(to) ?? Task.FromResult(0);
        }

        public static Func<State, Task> ToTask(Action<State> action)
        {
            return x =>
            {
                action(x);
                return Task.FromResult(0);
            };
        }
    }
}