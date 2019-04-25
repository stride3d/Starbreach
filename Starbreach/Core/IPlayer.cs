// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Starbreach.Core
{
    public interface IPlayer
    {
        bool IsAlive { get; }

        void Init(IPlayerInput input);
    }
}