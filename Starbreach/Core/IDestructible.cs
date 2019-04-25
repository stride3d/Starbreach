// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Starbreach.Core
{
    public interface IStunnable
    {
        void Stun();

        void CancelStun();

        bool Stunned { get; }

        bool CanBeStunned { get; }
    }

    public interface IDestructible
    {
        void Damage(int damage);

        int HealthPoints { get; }

        bool IsDead { get; }
    }
}