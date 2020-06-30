// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Engine;
using Stride.Engine.Events;

namespace Starbreach.Gameplay
{
    /// <summary>
    /// An object like a pressure plate, button, etc.
    /// </summary>
    public abstract class Activator : AsyncScript
    {
        [DataMemberIgnore]
        public abstract EventKey<bool> Changed { get; }

        public virtual bool CurrentState { get; protected set; }
    }

    /// <summary>
    /// Same as <see cref="Activator"/> but acts as a <see cref="SyncScript"/> instead while still inheriting from <see cref="Activator"/>
    /// </summary>
    public abstract class SyncActivator : Activator
    {
        public override async Task Execute()
        {
            Start();
            while (Game.IsRunning)
            {
                Update();

                await Script.NextFrame();
            }
        }

        public virtual void Start()
        {
        }

        public abstract void Update();
    }
}