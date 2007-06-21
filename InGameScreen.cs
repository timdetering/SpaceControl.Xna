using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using SpaceControl.Utility;
using Microsoft.Xna.Framework;
using SpaceControl.Entities;
using Microsoft.Xna.Framework.Input;

namespace SpaceControl.GameScreen
{
    public class InGameScreen : GameScreen
    {
        private const string c_backgroundTexture = @".\Textures\background.jpg", c_starTexture = @".\Textures\BackgroundStar.png";
        private Texture2D backgroundTexture = null, backgroundStars;
        private Vector2[] starPositions;
        private float scrolableSizeX, scrolableSizeZ;
        private SpriteBatch backgroundSprite = null;
        private InputHandler input = null;
        private XNA_GUI.GUIElements.GUI_Base guiControl = null;
        private Camera drawCamera = new Camera(new Vector3(0, 0, 0),
                                                new Vector3(20, 200, 200));
        private Universe universe = null;

        public InGameScreen(int playerCount, int planetCount, GraphicsDevice drawDevice)
            : base(drawDevice)
        {
            input = new InputHandler("InputHandler.xml", this);
            guiControl = XNA_GUI.GUIElements.GUI_Base.LoadLayout("GUI Layout.xml", this);
            universe = new Universe(playerCount, planetCount);
            drawCamera.JumpTo(universe.Players[1].HomeWorld.Position);

            backgroundTexture = Texture2D.FromFile(drawDevice, c_backgroundTexture);
            backgroundStars = Texture2D.FromFile(drawDevice, c_starTexture);
            scrolableSizeZ = backgroundTexture.Height - drawDevice.Viewport.Height;
            scrolableSizeX = backgroundTexture.Width - drawDevice.Viewport.Width;

            backgroundSprite = new SpriteBatch(drawDevice);
        }

        public override void Draw(GraphicsDevice drawDevice)
        {
            DrawBackground(drawDevice);
            universe.Draw(drawDevice, drawCamera);
            guiControl.Draw(drawDevice);

        }



        private void DrawBackground(GraphicsDevice drawDevice)
        {

            float xPos = (drawCamera.xPosition / scrolableSizeX) * scrolableSizeX + (.5f * scrolableSizeX);
            float yPos = (drawCamera.yPosition / scrolableSizeZ) * scrolableSizeZ + (.5f * scrolableSizeZ);
            if (xPos < 0)
                xPos = 0;
            if (xPos > scrolableSizeX)
                xPos = scrolableSizeX;

            if (yPos < 0)
                yPos = 0;
            if (yPos > scrolableSizeZ)
                yPos = scrolableSizeZ;

            backgroundSprite.Begin();
            backgroundSprite.Draw(backgroundTexture, new Vector2(0, 0),
                new Rectangle((int)xPos, (int)yPos, drawDevice.Viewport.Width, drawDevice.Viewport.Width), Color.White);

            backgroundSprite.End();

        }
        public override void Update(GameTime time)
        {
            universe.Update(time);
            //guiControl.Update(time);
            MouseState s = Mouse.GetState();
            if (guiControl.PointIsIn(s.X, s.Y) == true)
                guiControl.HandleMouse(s);
            else
                input.UpdateHandler(time);
            drawCamera.Update(time);
            Utility.SelectedManager.Update(guiControl, universe.Players[1]);


        }

        public void CreateDeployment(object sender)
        {
            if (input.CurrentGameState == InputHandler.GameState.PlanetSelected &&
                Utility.SelectedManager.Selected.Owner.IsHuman == true)
                input.CurrentGameState = InputHandler.GameState.CreateDeploymentRoute;
        }

        public void DoNothing(MouseState mouse, Keys[] pressedKeys, Vector3 mouseChange)
        {
            return;
        }

        public void DoPick(MouseState mouse, Keys[] pressedKeys, Vector3 mouseChange)
        {

            Planet picked = universe.DoPick(mouse.X, mouse.Y, drawCamera.View, drawCamera.Projection);
            if (picked == null)
                Utility.SelectedManager.ClearSelected(guiControl);
            else
            {
                input.CurrentGameState = InputHandler.GameState.PlanetSelected;
                Utility.SelectedManager.MakeSelected(picked, guiControl, universe.GetHumanPlayer());
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
            Planet destination = universe.DoPick(mouse.X, mouse.Y, drawCamera.View, drawCamera.Projection);
            if (destination != null)
            {
                start.CreateDeploymentRoute(destination);
                input.CurrentGameState = InputHandler.GameState.PlanetSelected;
                Utility.SelectedManager.MakeSelected(start, guiControl, universe.GetHumanPlayer());

            }
            else
                input.CurrentGameState = InputHandler.GameState.Default;


        }

        public override void Dispose()
        {
            backgroundTexture.Dispose();
            backgroundSprite.Dispose();

        }
    }
}
