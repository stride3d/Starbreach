// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Input;
using Xenko.Rendering;
using System.IO;

namespace Starbreach.VFX
{
    /// <summary>
    /// Debug script to save a <see cref="RenderFrame"/> to a file
    /// </summary>
    class SaveRenderFrame : SyncScript
    {
        public override void Update()
        {
            if (Input.IsKeyPressed(Keys.Space))
            {
                // TODO Take a screenshot
            }
        }
    }
}
