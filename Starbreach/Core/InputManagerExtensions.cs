// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Input;

namespace Starbreach.Core
{
    public static class InputManagerExtensions
    {
        public static bool IsGamePadButtonDown(this InputManager input, int index, GamePadButton button)
        {
            return input.GamePadCount > index && input.GetGamePadByIndex(index).IsButtonDown(button);
        }

        public static bool IsGamePadButtonPressed(this InputManager input, int index, GamePadButton button)
        {
            return input.GamePadCount > index && input.GetGamePadByIndex(index).IsButtonPressed(button);
        }
        public static bool IsGamePadButtonReleased(this InputManager input, int index, GamePadButton button)
        {
            return input.GamePadCount > index && input.GetGamePadByIndex(index).IsButtonReleased(button);
        }

        public static Vector2 GetLeftThumb(this InputManager input, int index)
        {
            return input.GamePadCount > index ? input.GetGamePadByIndex(index).State.LeftThumb : Vector2.Zero;
        }

        public static Vector2 GetRightThumb(this InputManager input, int index)
        {
            return input.GamePadCount > index ? input.GetGamePadByIndex(index).State.RightThumb : Vector2.Zero;
        }

        public static float GetLeftTrigger(this InputManager input, int index)
        {
            return input.GamePadCount > index ? input.GetGamePadByIndex(index).State.LeftTrigger : 0.0f;
        }

        public static float GetRightTrigger(this InputManager input, int index)
        {
            return input.GamePadCount > index ? input.GetGamePadByIndex(index).State.RightTrigger : 0.0f;
        }

        public static void SetVibration(this InputManager input, int index, float largeMotors, float smallMotors)
        {
            if (input.GamePadCount > index)
                input.GetGamePadByIndex(index).SetVibration(largeMotors, smallMotors);
        }
    }
}