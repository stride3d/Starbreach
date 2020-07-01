// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starbreach.Core;
using Stride.Input;

namespace Starbreach.Drones
{
    public class DronePath : StartupScript
    {
        List<Waypoint> waypoints = new List<Waypoint>();

        [DataMemberIgnore]
        public List<Waypoint> Waypoints => waypoints;

        [DataMemberIgnore]
        public Path Path { get; private set; }

        public override void Start()
        {
            List<Vector3> points = new List<Vector3>();
            foreach (TransformComponent waypoint in Entity.Transform.Children)
            {
                waypoint.UpdateWorldMatrix();
                points.Add(waypoint.WorldMatrix.TranslationVector);
            }
            Path = new Path(points.ToArray());
            Log.Info($"Added {waypoints.Count} waypoints to {Entity}");
        }
    }
}
