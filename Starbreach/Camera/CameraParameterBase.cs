// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;

namespace Starbreach.Camera
{
    /// <summary>
    /// Base class for implementations of hte <see cref="ICameraParameter"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of the camera parameter.</typeparam>
    [DataContract(Inherited = true)]
    public abstract class CameraParameterBase<T> : ICameraParameter
    {
        /// <summary>
        /// Indicates if the camera is in Aim stance.
        /// </summary>
        private bool isAiming;

        private T valueAtRun;
        private T valueAtAim;

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraParameterBase{T}"/> with values for each stance.
        /// </summary>
        /// <param name="valueAtRun">The value for the Run stance.</param>
        /// <param name="valueAtAim">The value for the Aim stance.</param>
        protected CameraParameterBase(T valueAtRun, T valueAtAim)
        {
            ValueAtRun = valueAtRun;
            ValueAtAim = valueAtAim;
            CurrentValue = valueAtRun;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraParameterBase{T}"/>.
        /// </summary>
        protected CameraParameterBase()
            : this(default(T), default(T))
        {
        }

        /// <summary>
        /// Gets or sets the value of this parameter in the Run stance.
        /// </summary>
        public T ValueAtRun { get { return valueAtRun; } set { valueAtRun = value; if (!isAiming) CurrentValue = value; } }

        /// <summary>
        /// Gets or sets the value of this parameter in the Aim stance.
        /// </summary>
        public T ValueAtAim { get { return valueAtAim; } set { valueAtAim = value; if (isAiming) CurrentValue = value; } }

        /// <summary>
        /// Gets the current value of this parameter.
        /// </summary>
        [DataMemberIgnore]
        public T CurrentValue { get; protected set; }

        /// <summary>
        /// Gets the target value of this parameter, corresponding either to <see cref="ValueAtRun"/> or to <see cref="ValueAtAim"/>.
        /// </summary>
        protected T TargetValue => isAiming ? ValueAtAim : ValueAtRun;

        /// <Inheritdoc/>
        public void SwitchToAim()
        {
            isAiming = true;
        }

        /// <Inheritdoc/>
        public void SwitchToRun()
        {
            isAiming = false;
        }

        /// <Inheritdoc/>
        public abstract void Update(float dt, float transitionDuration);

        /// <summary>
        /// Interpolates the value of a float parameter between two boundaries according to a delta-time and a duration of transition.
        /// </summary>
        /// <param name="dt">The delta-time for the interpolation.</param>
        /// <param name="transitionDuration">The duration of the transition between the two boundaries.</param>
        /// <param name="boundary1">The first boundary. Order relative to the other boundary does not matter.</param>
        /// <param name="boundary2">The second boundary. Order relative to the other boundary does not matter.</param>
        /// <param name="currentValue">The current value, resulting of the interpolation at the previous frame.</param>
        /// <param name="targetValue">The target value, which should be either <paramref name="boundary1"/> or <paramref name="boundary2"/>.</param>
        /// <returns>The new interpolated value for the float parameter.</returns>
        protected static float InterpolateParameter(float dt, float transitionDuration, float boundary1, float boundary2, float currentValue, float targetValue)
        {
            var step = Math.Abs(boundary1 - boundary2) * dt / transitionDuration;
            return currentValue + Math.Min(step, Math.Abs(targetValue - currentValue)) * Math.Sign(targetValue - currentValue);
        }
    }
}