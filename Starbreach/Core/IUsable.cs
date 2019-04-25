// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starbreach.Core
{
    public interface IUsable
    {
        bool CanBeUsed { get; }

        string Name { get; }

        void Use();

    }
}
