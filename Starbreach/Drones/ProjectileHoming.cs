// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Starbreach.Drones
{
    /// <summary>
    /// Applies projectile homing behaviour to a projectile rigidbody
    /// </summary>
    public class ProjectileHoming : SyncScript
    {
        [DataMemberIgnore]
        public Vector3 TargetPosition { get; set; }

        public float HomingSpeed { get; set; } = 2.0f;

        public override void Update()
        {
            Vector3 targetDir = TargetPosition - Entity.Transform.WorldMatrix.TranslationVector;
            if (targetDir.LengthSquared() < 1.0f)
                return;
            targetDir.Normalize();

            var projectile = Entity.Get<Projectile>();
            var currentDirection = Vector3.Normalize(projectile.Rigidbody.LinearVelocity);

            // Bend towards target direction
            targetDir = currentDirection + Vector3.Normalize(targetDir)*HomingSpeed*(float)Game.UpdateTime.Elapsed.TotalSeconds;
            targetDir.Normalize();

            // Reset velocity based on adjusted direction
            projectile.Rigidbody.LinearVelocity = Vector3.Zero;
            projectile.SetDirection(targetDir);

            projectile.Entity.Transform.Rotation = Quaternion.BetweenDirections(Vector3.UnitZ, targetDir);
        }
    }
}