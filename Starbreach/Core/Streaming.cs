// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Physics;

namespace Starbreach.Core
{
    public class Streaming : AsyncScript
    {
        public StaticColliderComponent Triggers { get; set; }

        public PhysicsComponent Target { get; set; }

        public Vector3 Offset { get; set; }

        public string SceneUrl { get; set; }

        private Scene scene;

        public override async Task Execute()
        {
            while (true)
            {
                if (Triggers.Collisions.Any(CollisionMatch))
                {
                    await LoadScene();
                }
                else
                {
                    await UnloadScene();
                }
                await Script.NextFrame();
            }
        }

        private async Task LoadScene()
        {
            if (scene == null)
            {
                scene = await Content.LoadAsync<Scene>(SceneUrl);
                var allEntities = SelectDeep(scene.Entities, x => x.Transform.Children.Select(y => y.Entity)).ToList();

                SceneSystem.SceneInstance.RootScene.Children.Add(scene);

                //await Task.Delay(200);
                await Script.NextFrame();

                foreach (var entity in allEntities)
                {
                    var physics = entity.Get<PhysicsComponent>();
                    if (physics?.ColliderShape != null)
                    {
                        entity.Transform.UpdateWorldMatrix();
                        physics.UpdatePhysicsTransformation();
                    }
                }
            }
        }

        private async Task UnloadScene()
        {
            if (scene != null)
            {
                SceneSystem.SceneInstance.RootScene.Children.Remove(scene);
                await Script.NextFrame();
                Content.Unload(scene);
                scene = null;
            }
        }

        private bool CollisionMatch(Collision collision)
        {
            return collision.ColliderA == Target || collision.ColliderB == Target;
        }

        private static IEnumerable<T> SelectDeep<T>(IEnumerable<T> source, Func<T, IEnumerable<T>> childrenSelector)
        {
            if (childrenSelector == null) throw new ArgumentNullException(nameof(childrenSelector));

            var stack = new Stack<IEnumerable<T>>();
            stack.Push(source);
            while (stack.Count != 0)
            {
                var current = stack.Pop();
                if (current == null)
                    continue;

                foreach (var item in current)
                {
                    yield return item;
                    stack.Push(childrenSelector(item));
                }
            }
        }


    }
}
