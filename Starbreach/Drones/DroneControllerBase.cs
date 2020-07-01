// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Starbreach.Core;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Audio;
using Stride.Engine;
using Stride.Navigation;
using Stride.Particles.Components;
using Stride.Physics;
using Stride.Rendering;

namespace Starbreach.Drones
{
    /// <summary>
    /// A basic drone controller, that contains functionality for moving a drone using a navigation component with <see cref="Move"/>
    /// </summary>
    public class DroneControllerBase : SyncScript
    {
        [DataMemberIgnore]
        public Drone Drone { get; private set; }

        private float MoveThreshold { get; set; } = 0.2f;

        public override void Start()
        {
            Drone = Entity.Get<Drone>();
        }

        public override void Update()
        {
        }

        /// <summary>
        /// Tries to move towards the target position using the attached NavigationComponent asynchronously
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        protected IEnumerator<Vector3> Move(Vector3 targetPosition)
        {
            NavigationComponent navigationComponent = Entity.Get<NavigationComponent>();

            List<Vector3> pathPoints = new List<Vector3>();
            if (navigationComponent == null || navigationComponent.NavigationMesh == null)
            {
                pathPoints = new List<Vector3> {Entity.Transform.WorldMatrix.TranslationVector, targetPosition};  
            }
            else
            {     
                if (!navigationComponent.TryFindPath(targetPosition, pathPoints))
                    yield break;
            }

            Path navigationPath = new Path(pathPoints.ToArray());
            Waypoint nextWaypoint = navigationPath.Waypoints[0];
            while (nextWaypoint != null)
            {
                Vector3 targetSpeed = Vector3.Zero;
                if (!Drone.Stunned)
                {
                    // Move towards target when having a waypoint
                    Vector3 dir = nextWaypoint.Position - Entity.Transform.WorldMatrix.TranslationVector;
                    dir.Y = 0;
                    var dist = dir.Length();

                    if (dist < MoveThreshold)
                    {
                        nextWaypoint = nextWaypoint.Next;
                        continue;
                    }

                    dir.Normalize();
                    Drone.UpdateBodyRotation(dir);

                    targetSpeed = dir*Drone.MaximumSpeed;
                    float dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
                    var estimatedDist = targetSpeed.Length()*dt;
                    if (estimatedDist > dist)
                    {
                        targetSpeed = dir*(dist/dt);
                    }
                }
                Drone.SetMovement(targetSpeed);
                yield return nextWaypoint.Position;
            }

            // Stop when done moving
            Drone.SetMovement(Vector3.Zero);
        }
    }
}