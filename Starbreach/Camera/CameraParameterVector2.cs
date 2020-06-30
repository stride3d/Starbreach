// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;

namespace Starbreach.Camera
{
    /// <summary>
    /// Implementation of <see cref="ICameraParameter"/> for a Vector2 parameter.
    /// </summary>
    public class CameraParameterVector2 : CameraParameterBase<Vector2>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CameraParameterVector2"/> with values for each stance.
        /// </summary>
        /// <param name="valueAtRun">The value for the Run stance.</param>
        /// <param name="valueAtAim">The value for the Aim stance.</param>
        public CameraParameterVector2(Vector2 valueAtRun, Vector2 valueAtAim)
            : base(valueAtRun, valueAtAim)
        {
            ValueAtRun = valueAtRun;
            ValueAtAim = valueAtAim;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraParameterVector2"/>.
        /// </summary>
        public CameraParameterVector2()
        {
        }

        /// <inheritdoc/>
        public override void Update(float dt, float transitionDuration)
        {
            Vector2 currentValue;
            currentValue.X = InterpolateParameter(dt, transitionDuration, ValueAtRun.X, ValueAtAim.X, CurrentValue.X, TargetValue.X);
            currentValue.Y = InterpolateParameter(dt, transitionDuration, ValueAtRun.Y, ValueAtAim.Y, CurrentValue.Y, TargetValue.Y);
            CurrentValue = currentValue;
        }
    }
}