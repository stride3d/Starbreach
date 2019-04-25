// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Xenko.Engine;

namespace Starbreach.Core
{
    public class StreamingScript : StartupScript
    {
        // TODO: this is just a placeholder script for streaming
        public List<Scene> Levels { get; set; } = new List<Scene>();

        public override void Start()
        {
            base.Start();
            foreach (var level in Levels)
            {
                SceneSystem.SceneInstance.RootScene.Entities.AddRange(level.Entities.Where(x => x.Name != "(discard)"));
            }
        }
    }
}