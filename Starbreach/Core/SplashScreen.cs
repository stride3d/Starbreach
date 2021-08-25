// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Starbreach.Camera;
using Starbreach.Soldier;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Particles.Components;
using Stride.Physics;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;
using Stride.UI;
using Stride.UI.Controls;

namespace Starbreach.Core
{
    public class SplashScreen : AsyncScript
    {
        public Gameplay.Activator Activator { get; set; }

        public SoldierController Soldier { get; set; }

        public ModelComponent SoldierModel { get; set; }

        public ParticleSystemComponent StartParticles { get; set; }

        public ParticleSystemComponent FinishParticles { get; set; }

        public override async Task Execute()
        {
            var initialPosition = Soldier.Entity.Transform.Position;
            var initialYaw = 180;
            var initialCamYaw = Soldier.Entity.Get<CameraController>().Yaw;
            while (true)
            {
                // Game start, enable and initialize soldier
                Soldier.IsEnabled = true;
                SoldierModel.Enabled = true;
                Soldier.Entity.Get<CharacterComponent>().Teleport(initialPosition);
                Soldier.Entity.Get<CameraController>().Yaw = initialCamYaw;
                Soldier.Rotation = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(initialYaw), 0f, 0f);

                // Wait to reach the end
                while (true)
                {
                    await Script.NextFrame();
                    if (Activator.CurrentState)
                    {
                        // Disable soldier
                        Soldier.IsEnabled = false;
                        Soldier.Entity.Get<CharacterComponent>().SetVelocity(Vector3.Zero);
                        
                        await Task.Delay(100);

                        // Particle effects and fade
                        SoldierModel.Enabled = false;
                        FinishParticles.Enabled = true;
                        Entity.Get<UIComponent>().Enabled = true;
                        const float initialTime = 2.0f;
                        var time = initialTime;
                        var spl = (ImageElement)Entity.Get<UIComponent>().Page.RootElement.FindName("Spl");
                        spl.Visibility = Visibility.Visible;
                        while (time > 0)
                        {
                            spl.Opacity = 1.0f - time / initialTime;
                            time -= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                            await Script.NextFrame();
                        }
                        spl.Opacity = 1.0f;

                        await Task.Delay(5000);

                        spl.Opacity = 0.0f;
                        Entity.Get<UIComponent>().Enabled = false;
                        break;
                    }
                }
            }
        }
    }
}