// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Mathematics;

namespace Starbreach.Core
{
    /// <summary>
    /// A single point on a path, use Next to get the next point
    /// </summary>
    public class Waypoint
    {
        private Path path;

        public Vector3 Position { get; private set; }

        /// <summary>
        /// The next point in the path, null if this is the last point
        /// </summary>
        public Waypoint Next
        {
            get
            {
                int nextIndex = path.Waypoints.IndexOf(this) + 1;
                if (nextIndex >= path.Waypoints.Count) return null;
                return path.Waypoints[nextIndex];
            }
        }

        /// <summary>
        /// The previous point, null if this is the first point
        /// </summary>
        public Waypoint Previous
        {
            get
            {
                int nextIndex = path.Waypoints.IndexOf(this) - 1;
                if (nextIndex < 0) return null;
                return path.Waypoints[nextIndex];
            }
        }

        public Waypoint(Path path, Vector3 location)
        {
            this.path = path;
            Position = location;
        }
    }

    public class Path
    {
        List<Waypoint> waypoints = new List<Waypoint>();
        public List<Waypoint> Waypoints => waypoints;

        public Path(Vector3[] points)
        {
            foreach (Vector3 p in points)
            {
                waypoints.Add(new Waypoint(this, p));
            }
        }

        /// <summary>
        /// Selects the next point in the path for the current location
        /// </summary>
        /// <param name="position">Current location</param>
        /// <param name="reverse">true if traversing the reverse path</param>
        /// <returns></returns>
        public Waypoint SelectWaypoint(Vector3 position, bool reverse = false)
        {
            Waypoint candidate = null;
            float distanceToPoint = float.MaxValue;
            foreach (Waypoint waypoint in waypoints)
            {
                float dist = (waypoint.Position - position).LengthSquared();
                if (dist < distanceToPoint)
                {
                    distanceToPoint = dist;
                    candidate = waypoint;
                }
            }
            
            // Skip ahead 1 waypoint if it lies ahead of the current point
            if (candidate != null && ((!reverse && candidate.Next != null) || (reverse && candidate.Previous != null)))
            {
                Vector3 waypointDirection = reverse ?
                    candidate.Previous.Position - candidate.Position :
                    candidate.Next.Position - candidate.Position;
                float projectedProgress = Vector3.Dot(position, waypointDirection);
                float projectedStart = Vector3.Dot(candidate.Position, waypointDirection);
                if (projectedProgress > projectedStart)
                    candidate = reverse ? candidate.Previous : candidate.Next;
            }

            return candidate;
        }
    }
}
