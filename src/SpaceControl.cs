#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

using XNA_GUI.GUIElements;
using SpaceControl.Entities;
using SpaceControl.Utility;
using System.Xml;

#endregion

namespace SpaceControl
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class SpaceControlMain : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        ContentManager content;
        int framesLastSecond = 0, frameCount = 0;
        float timeSinceLastFrameInfoUpdate = 0.0f;

        protected enum GameState : int { MainMenu = 0, Game = 1, GameOver = 2, Minimized }
        private GameState currentState = GameState.MainMenu;

        public SpaceControlMain()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 768;
            graphics.PreferredBackBufferWidth = 1024;

            content = new ContentManager(Services);
            IsMouseVisible = true;
        }


        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
            graphics.DeviceReset += new EventHandler(DeviceReset);
            graphics.DeviceResetting +=new EventHandler(DeviceLost);
            graphics.DeviceDisposing +=new EventHandler(DeviceDisposed);
            GameScreen.MainMenuScreen s = new SpaceControl.GameScreen.MainMenuScreen("MainMenu.xml", graphics.GraphicsDevice);

        }

        void DeviceReset(object sender, EventArgs e)
        {
            LoadGraphicsContent(true);
            currentState = GameState.Game;
        }


        protected void DeviceLost(object sender, EventArgs e)
        {
            UnloadGraphicsContent(true);
            currentState = GameState.Minimized;
        }

        void DeviceDisposed(object sender, EventArgs e)
        {
            UnloadGraphicsContent(true);
        }

        /// <summary>
        /// Load your graphics content.  If loadAllContent is true, you should
        /// load content from both ResourceManagementMode pools.  Otherwise, just
        /// load ResourceManagementMode.Manual content.
        /// </summary>
        /// <param name="loadAllContent">Which type of content to load.</param>
        protected override void LoadGraphicsContent(bool loadAllContent)
        {
            graphics.PreferMultiSampling = true;
            if (loadAllContent)
            {
                GUI_Base.Initialize(graphics, content, @".\GUIResources\");
                BaseEntity.Initalize(content, graphics.GraphicsDevice, @".\");
                
            }

            // TODO: Load any ResourceManagementMode.Manual content

            currentState = GameState.MainMenu;
            GUI_Base.DeviceReset(graphics, content);
            BaseEntity.DeviceReset(graphics.GraphicsDevice, content);
            
        }


        /// <summary>
        /// Unload your graphics content.  If unloadAllContent is true, you should
        /// unload content from both ResourceManagementMode pools.  Otherwise, just
        /// unload ResourceManagementMode.Manual content.  Manual content will get
        /// Disposed by the GraphicsDevice during a Reset.
        /// </summary>
        /// <param name="unloadAllContent">Which type of content to unload.</param>
        protected override void UnloadGraphicsContent(bool unloadAllContent)
        {
            if (unloadAllContent)
            {
                // TODO: Unload any ResourceManagementMode.Automatic content
                content.Unload();
                
            }

            // TODO: Unload any ResourceManagementMode.Manual content
            GUI_Base.DeviceLost();
            BaseEntity.DeviceLost();
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            if (currentState == GameState.Minimized)
            {
                System.Threading.Thread.Sleep(100);
                return;
            }

            // TODO: Add your update logic here

            timeSinceLastFrameInfoUpdate += gameTime.ElapsedGameTime.Milliseconds;
            if (timeSinceLastFrameInfoUpdate > 1000.0f)
            {
                timeSinceLastFrameInfoUpdate = 0;
                framesLastSecond = frameCount++;
                frameCount = 0;
            }
            else
                frameCount++;

            GameScreen.GameScreen.UpdateScreen(gameTime);
            base.Update(gameTime);
        }
        
        /// <summary>
        /// Button callback to add a new depolyment route to the currently selected planet.
        /// This This sets the input state to CreateDeploymentRoute so the next planet clicked on
        /// will be set as the desination.
        /// </summary>
        /// <param name="sender"></param>

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            if (currentState == GameState.Minimized)
                return;
        
            graphics.GraphicsDevice.Clear(Color.Black);
            graphics.GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
            

            // TODO: Add your drawing code here
            GameScreen.GameScreen.DrawScreen(graphics.GraphicsDevice);            
        }
    }
}
