// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Engine;
using Xenko.Engine.Events;

namespace Starbreach.Core
{
    public class PlayerSpawner : SyncScript
    {
        protected IPlayer Player;

        protected EventReceiver<bool> StartGameEvent;
        protected readonly List<Entity> PlayerEntities = new List<Entity>();

        public Prefab PlayerPrefab { get; set; }

        public float RespawnDelay = 5.0f;
        public float RespawnTimer = 0.0f;

        protected bool Playing => PlayerEntities.Count > 0;

        public override void Start()
        {
        }

        public override void Update()
        {
            if (!Playing)
            {
                if (RespawnTimer <= 0.0f)
                {
                    SpawnPlayer();
                    // Reset respawn time
                    RespawnTimer = RespawnDelay;
                }
                else
                {
                    RespawnTimer -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                    UpdateRespawnTimer(RespawnTimer);
                }
            }
            else if (Player != null && !Player.IsAlive)
            {
                KillPlayer();
            }
        }

        protected virtual void UpdateRespawnTimer(float timeLeft)
        {
        }

        protected virtual void SpawnPlayer()
        {
            PlayerEntities.AddRange(PlayerPrefab.Instantiate());
            foreach (var entity in PlayerEntities)
            {
                entity.Transform.Position = Entity.Transform.Position;
                // Allow user spawn handling before starting scripts
                PreSpawnPlayer(entity);
                // Add to scene
                SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
            }
        }

        protected virtual void KillPlayer()
        {
            foreach (var entity in PlayerEntities)
            {
                SceneSystem.SceneInstance.RootScene.Entities.Remove(entity);
            }

            PlayerEntities.Clear();
        }

        protected virtual void PreSpawnPlayer(Entity playerEntity)
        {   
        }

        /// <summary>
        /// Use to force despawning of spawned players
        /// </summary>
        public void DespawnPlayerIfSpawned()
        {
            KillPlayer();
        }
    }
}