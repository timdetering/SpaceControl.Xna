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
        GUI_Base base_GUI = null;
        Camera camera;
        Texture2D background = null, deploymentRoute = null;
        SpriteBatch sprite = null;
        InputHandler input;
        Universe universe;
        List<AIControl.AIBasic> AIPlayers = new List<SpaceControl.AIControl.AIBasic>(1);

        protected enum GameState : int { MainMenu = 0, Game = 1, GameOver = 2, Minimized }
        private GameState currentState = GameState.MainMenu;

        public SpaceControlMain()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 768;
            graphics.PreferredBackBufferWidth = 1024;

            content = new ContentManager(Services);
            IsMouseVisible = true;
            camera = new Camera(new Vector3(0, 0, 0), new Vector3(0, 100, 100));
            input = new InputHandler("InputHandler.xml", this);
            
        }

        public void DoNothing(MouseState mouse, Keys[] pressedKeys, Vector3 mouseChange)
        {
            return;
        }

        public void DoPick(MouseState mouse, Keys[] pressedKeys, Vector3 mouseChange)
        {

            Planet picked = universe.DoPick(mouse.X, mouse.Y, camera.View, camera.Projection);
            if (picked == null)
                Utility.SelectedManager.ClearSelected(base_GUI);
            else
            {
                input.CurrentGameState = InputHandler.GameState.PlanetSelected;
                Utility.SelectedManager.MakeSelected(picked, base_GUI, universe.GetHumanPlayer());
            }
        }

        public void CreateDeploymentRoute(MouseState mouse, Keys[] pressedKeys, Vector3 mouseChange)
        {
            if (Utility.SelectedManager.Selected == null)   //can't create a depolyment without a source.
            {
                input.CurrentGameState = InputHandler.GameState.Default;
                return;
            }
            Planet start = Utility.SelectedManager.Selected;
            Planet destination = universe.DoPick(mouse.X, mouse.Y, camera.View, camera.Projection);
            if (destination != null)
            {
                start.CreateDeploymentRoute(destination);
                input.CurrentGameState = InputHandler.GameState.PlanetSelected;
                Utility.SelectedManager.MakeSelected(start, base_GUI, universe.GetHumanPlayer());
                
            }
            else
                input.CurrentGameState = InputHandler.GameState.Default;


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
            universe = new Universe(3, 15);

            for (int i = 2; i < universe.Players.Count; i++)
            {
                AIControl.AIBasic ai = new SpaceControl.AIControl.AIBasic(universe.Players[i], universe);
                AIPlayers.Add(ai);
                System.Threading.Thread t = new System.Threading.Thread(ai.Run);
                t.IsBackground = true;
                t.Start();
            }
            camera.JumpTo(universe.GetHumanPlayer().HomeWorld.Position);
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
                // TODO: Load any ResourceManagementMode.Automatic content
                background = (Texture2D)Texture.FromFile(graphics.GraphicsDevice,
                    @".\Textures\background.jpg");
                sprite = new SpriteBatch(graphics.GraphicsDevice);
                deploymentRoute = (Texture2D)Texture.FromFile(graphics.GraphicsDevice,
                    @".\Textures\Cursor.png");

                GUI_Base.DeviceReset(graphics, content);
                BaseEntity.DeviceReset(graphics.GraphicsDevice, content);
            }

            // TODO: Load any ResourceManagementMode.Manual content
            GUI_Base.Initialize(graphics, content, @".\GUIResources\");
            BaseEntity.Initalize(content, graphics.GraphicsDevice, @".\");
            base_GUI = GUI_Base.LoadLayout("GUI Layout.xml", this);
            


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
                sprite.Dispose();
                background.Dispose();
                
            }

            // TODO: Unload any ResourceManagementMode.Manual content
            GUI_Base.DeviceLost();
            base_GUI = null;
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
                return;

            // TODO: Add your update logic here
            camera.Update(gameTime);
            BaseEntity.UpdateScene(gameTime);
            MouseState s = Mouse.GetState();
            base_GUI.HandleMouse(s);
            if(base_GUI.PointIsIn(s.X, s.Y) == false)
                input.UpdateHandler(gameTime);

            Utility.SelectedManager.Update(base_GUI, universe.GetHumanPlayer());

            base.Update(gameTime);
        }
        
        /// <summary>
        /// Button callback to add a new depolyment route to the currently selected planet.
        /// This This sets the input state to CreateDeploymentRoute so the next planet clicked on
        /// will be set as the desination.
        /// </summary>
        /// <param name="sender"></param>
        public void CreateDeployment(object sender)
        {
            if(input.CurrentGameState == InputHandler.GameState.PlanetSelected &&
                Utility.SelectedManager.Selected.Owner.IsHuman == true)
                input.CurrentGameState = InputHandler.GameState.CreateDeploymentRoute;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            if (currentState == GameState.Minimized)
                return;
        
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
            graphics.GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
            

            // TODO: Add your drawing code here
            
            sprite.Begin();
            sprite.Draw(background, new Rectangle(0, 0, graphics.PreferredBackBufferWidth,
                graphics.PreferredBackBufferHeight), new Rectangle(0,
                                                                    0,
                                                                    background.Width,
                                                                    background.Height),
                                                                    Color.White);
            if (input.CurrentGameState == InputHandler.GameState.CreateDeploymentRoute)
            {
                MouseState s = Mouse.GetState();
                sprite.Draw(deploymentRoute, new Rectangle(s.X - deploymentRoute.Width / 2,
                                                           s.Y - deploymentRoute.Height / 2,
                                                           deploymentRoute.Width,
                                                           deploymentRoute.Height),
                    Color.White);
                IsMouseVisible = false;
            }
            else
                IsMouseVisible = true;
            sprite.End();
            BaseEntity.RenderScene(graphics.GraphicsDevice, camera);
            base_GUI.Draw(graphics.GraphicsDevice);
            base.Draw(gameTime);
        }
    }
}
