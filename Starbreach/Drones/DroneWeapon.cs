// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Engine;

namespace Starbreach.Drones
{
    [DataContract]
    public abstract class DroneWeapon
    {
        public Entity ProjectileSpawnPoint;

        private TimeSpan lastShot;
        
        public double ReloadTime { get; set; } = 1.0f;

        [DataMemberIgnore]
        public Drone Drone { get; internal set; }

        public virtual void Init(Drone drone)
        {
            Drone = drone;
        }

        public bool TryShoot(Entity targetEntity)
        {
            if (!CanShoot(targetEntity))
                return false;

            Drone.Script.AddTask(() => Shoot(targetEntity));
            StartReloading();

            return true;
        }

        public virtual bool CanShoot(Entity targetEntity)
        {
            var currentTime = Drone.Game.UpdateTime.Total;
            if ((currentTime - lastShot) < TimeSpan.FromSeconds(ReloadTime))
                return false;

            return true;
        }

        protected void StartReloading()
        {
            lastShot = Drone.Game.UpdateTime.Total;
        }

        protected abstract Task Shoot(Entity targetEntity);
    }
}