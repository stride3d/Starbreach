// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Starbreach.Camera
{
    /// <summary>
    /// An interface representing a parameter of the camera that changes between the Run and the Aim stances.
    /// </summary>
    public interface ICameraParameter
    {
        /// <summary>
        /// Notifies this parameter to switch to the Aim stance.
        /// </summary>
        void SwitchToAim();

        /// <summary>
        /// Notifies this parameter to switch to the Run stance.
        /// </summary>
        void SwitchToRun();

        /// <summary>
        /// Updates this parameter. This method should be called every frame.
        /// </summary>
        /// <param name="dt">The delta-time for the current frame.</param>
        /// <param name="transitionDuration">The duration of the transition of this parameter between the two stances.</param>
        void Update(float dt, float transitionDuration);
    }
}