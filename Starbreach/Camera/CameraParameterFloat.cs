// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Starbreach.Camera
{
    /// <summary>
    /// Implementation of <see cref="ICameraParameter"/> for a float parameter.
    /// </summary>
    public class CameraParameterFloat : CameraParameterBase<float>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CameraParameterFloat"/> with values for each stance.
        /// </summary>
        /// <param name="valueAtRun">The value for the Run stance.</param>
        /// <param name="valueAtAim">The value for the Aim stance.</param>
        public CameraParameterFloat(float valueAtRun, float valueAtAim)
            : base(valueAtRun, valueAtAim)
        { 
            ValueAtRun = valueAtRun;
            ValueAtAim = valueAtAim;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraParameterFloat"/>.
        /// </summary>
        public CameraParameterFloat()
        {
        }

        /// <inheritdoc/>
        public override void Update(float dt, float transitionDuration)
        {
            CurrentValue = InterpolateParameter(dt, transitionDuration, ValueAtRun, ValueAtAim, CurrentValue, TargetValue);
        }
    }
}