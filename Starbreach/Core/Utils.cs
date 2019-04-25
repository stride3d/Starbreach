// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Engine.Events;
using Xenko.Games;
using Xenko.UI;

namespace Starbreach.Core
{
    public static class Utils
    {
        private static bool debugPrintRegistered;
        private static int lineCount;

        public static Vector3 LogicDirectionToWorldDirection(Vector2 logicDirection, Entity cameraEntity)
        {
            var pivotRotation = cameraEntity.GetParent().Transform.Rotation;
            var forward = Vector3.Transform(-cameraEntity.Transform.Position, pivotRotation);
            forward.Y = 0;
            forward.Normalize();
            var right = Vector3.Cross(forward, Vector3.UnitY);
            var worldDirection = forward * logicDirection.Y + right * logicDirection.X;
            worldDirection.Normalize();
            return worldDirection;
        }

        public static float LerpYaw(float currentYaw, float targetYaw, float factor)
        {
            // TODO: this lerp is framerate dependent
            // Compute target yaw from the movement direction
            var deltaYaw = targetYaw - currentYaw;
            // Avoid interpolation to be done on the wrong arc
            if (Math.Abs(deltaYaw) > MathUtil.Pi)
                currentYaw = currentYaw + Math.Sign(deltaYaw) * MathUtil.TwoPi;

            return MathUtil.Lerp(currentYaw, targetYaw, factor);
        }

        public static float UpdateYaw(float currentYaw, float targetYaw, float degreesPerSecond, float dt)
        {
            // TODO: this update is not smoothing
            // Compute target yaw from the movement direction
            var deltaYaw = targetYaw - currentYaw;
            // Avoid interpolation to be done on the wrong arc
            if (Math.Abs(deltaYaw) > MathUtil.Pi)
                currentYaw = currentYaw + Math.Sign(deltaYaw) * MathUtil.TwoPi;

            var newYaw = currentYaw + Math.Min(Math.Abs(deltaYaw), degreesPerSecond * dt) * Math.Sign(deltaYaw);
            return newYaw;
        }

        public static void DebugPrint(this IGame gameInterface, string message)
        {
            var game = (Game)gameInterface;
            if (!debugPrintRegistered)
            {
                game.Script.AddTask(() => DebugPrintNextFrame(game), int.MinValue);
                debugPrintRegistered = true;
            }
            ++lineCount;
            var position = new Int2(16, 16 * lineCount);
#if DEBUG
            game.DebugTextSystem.Print(message, position + Int2.One, Color4.Black);
            game.DebugTextSystem.Print(message, position);
#endif
        }

        public static void RetrieveWeaponState(EventReceiver<bool> toggleAimEvent, EventReceiver<bool> toggleFireEvent, out bool aiming)
        {
            bool firing;
            RetrieveWeaponState(toggleAimEvent,  toggleFireEvent, out aiming, out firing);
        }

        public static void RetrieveWeaponState(EventReceiver<bool> toggleAimEvent, EventReceiver<bool> toggleFireEvent, out bool aiming, out bool firing)
        {
            toggleAimEvent.TryReceive(out aiming);
            toggleFireEvent.TryReceive(out firing);
            aiming = aiming || firing;
        }

        private static async Task DebugPrintNextFrame(Game game)
        {
            while (game.IsRunning)
            {
                lineCount = 0;
                await game.Script.NextFrame();
            }
        }

        public static bool IsPlayerEntity(Entity entity)
        {
            return entity.Any(x => x is IPlayer);
        }

        public static IDestructible GetDestructible(Entity entity)
        {
            return (IDestructible)entity.FirstOrDefault(x => x is IDestructible);
        }

        public static IStunnable GetStunnable(Entity entity)
        {
            return (IStunnable)entity.FirstOrDefault(x => x is IStunnable);
        }

        public static IUsable GetUsable(Entity entity)
        {
            return (IUsable)entity.FirstOrDefault(x => x is IUsable);
        }

        public static async Task WaitTime(this IGame game, TimeSpan time)
        {
            var g = (Game) game;
            var goal = game.UpdateTime.Total + time;
            while(game.UpdateTime.Total < goal)
            {
                await g.Script.NextFrame();
            }
        }

        public static void VibratorSmooth(this IGame game, int controllerIndex, Vector2 from, Vector2 to, TimeSpan time)
        {
            var g = (Game)game;
            g.Script.AddTask(async () =>
            {
                var length = time.TotalSeconds;
                var now = 0.0f;
                for (;;)
                {
                    if (now > length) break;
                    var vib = Vector2.Lerp(from, to, (float)(now / length));
                    g.Input.SetVibration(controllerIndex, vib.X, vib.Y);
                    await g.Script.NextFrame();
                    now += (float)g.UpdateTime.Elapsed.TotalSeconds;
                }
                g.Input.SetVibration(controllerIndex, 0, 0);
            });
        }

        public static UIElement FindNameRecursive(this UIElement element, string name)
        {
            foreach (UIElement elem in element.VisualChildren)
            {
                if (elem.Name == name)
                    return elem;
                UIElement subResult = elem.FindNameRecursive(name);
                if (subResult != null)
                    return subResult;
            }
            return null;
        }
    }
}