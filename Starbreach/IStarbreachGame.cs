// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Engine;
using Stride.Graphics;

namespace Starbreach
{
    public interface IStarbreach
    {
        Entity PlayerUiEntity { get; }

        void SaveTexture(Texture texture, String path, ImageFileType fileType);
    }
}