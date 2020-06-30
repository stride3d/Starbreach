// Copyright (c) Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Starbreach;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;

namespace Starbreach
{
    public class StarbreachGame : Game, IStarbreach
    {
        public Entity PlayerUiEntity { get; private set; }
        

        public StarbreachGame()
        {
            IsFixedTimeStep = true;
            IsDrawDesynchronized = true;
        }

        public void SaveTexture(Texture texture, string path, ImageFileType fileType)
        {
            using (var stream = File.Create(path))
            {
                texture.Save(GraphicsContext.CommandList, stream, fileType);
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            Services.AddService<IStarbreach>(this);
        }

        protected override Task LoadContent()
        {
            return base.LoadContent();
        }

        protected override void BeginRun()
        {
            //// TODO remove this hack to create a seperate UI scene when post effects no longer get applied to UI
            //var compositor = (SceneGraphicsCompositorLayers)SceneSystem.SceneInstance.Scene.Settings.GraphicsCompositor;
            //Entity uiEntity;
            //SceneChildRenderer childRend1;
            
            Scene uiScene = Content.Load<Scene>("UI/UISceneSoldier");

            //            SceneSystem.SceneInstance.Scene.Entities.Add(uiEntity);
            //            childRend1 = new SceneChildRenderer(uiEntity.Get<ChildSceneComponent>());
            //            compositor.Master.Add(childRend1);

            // TODO Hack the HUD
            PlayerUiEntity = uiScene.Entities.First(x => x.Name == "UI");

            base.BeginRun();
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }
        
        protected override void Destroy()
        {
            base.Destroy();
        }
    }
}
