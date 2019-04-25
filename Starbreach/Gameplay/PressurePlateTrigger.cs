// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Engine;
using Xenko.Engine.Events;
using Xenko.Physics;

namespace Starbreach.Gameplay
{
    /// <summary>
    /// Handles sending out trigger events when a valid collider enters or leaves the trigger area of the pressure plate
    /// </summary>
    public class PressurePlateTrigger : Activator
    {
        /// <summary>
        /// The pressure plate trigger collider
        /// </summary>
        public RigidbodyComponent Trigger { get; set; }
        
        [DataMemberIgnore]
        public override EventKey<bool> Changed { get; } = new EventKey<bool>("Pressure Plate", "Internal Trigger");

        /// <summary>
        /// The collision groups to filter triggering entities by
        /// </summary>
        public CollisionFilterGroupFlags CollisionFilterGroup;

        public override async Task Execute()
        {
            while (Game.IsRunning)
            {
                // Wait until the collision state changes
                await Task.WhenAny(NewCollision(), EndCollision());

                bool nextState = false;
                foreach (var collision in Trigger.Collisions)
                {
                    var otherCollider = (collision.ColliderA == Trigger) ? collision.ColliderB : collision.ColliderA;

                    // Filter by group
                    if(((CollisionFilterGroupFlags)otherCollider.CollisionGroup & CollisionFilterGroup) != 0)
                        nextState = true;
                }

                // Send out state change
                if (nextState != CurrentState)
                {
                    CurrentState = nextState;
                    Changed.Broadcast(nextState);
                }
            }
        }

        private async Task<Collision> NewCollision()
        {
            return await Trigger.NewCollision();
        }

        private async Task<Collision> EndCollision()
        {
            return await Trigger.CollisionEnded();
        }
    }
}