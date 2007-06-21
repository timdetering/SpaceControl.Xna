using System;
using System.Collections.Generic;
using System.Text;
using SpaceControl.Utility;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using XNA_GUI.GUIElements;

namespace SpaceControl.GameScreen
{
    public class MainMenuScreen : GameScreen
    {
        private XNA_GUI.GUIElements.GUI_Base guiControl = null;

        public MainMenuScreen(string GuiFilename, GraphicsDevice drawDevice)
            : base(drawDevice)
        {
            guiControl = XNA_GUI.GUIElements.GUI_Base.LoadLayout(GuiFilename, this);       
        }

        public override void Draw(GraphicsDevice drawDevice)
        {
            guiControl.Draw(drawDevice);
        }

        public override void Update(Microsoft.Xna.Framework.GameTime time)
        {
            guiControl.HandleMouse(Mouse.GetState());
           // guiControl.Update(time); 
        }


        public void NewGame(object sender)
        {
            int players = 3;
            int planets = ((SliderBar)guiControl.GetChildByName("Universe Size")).CurrentValue;

            InGameScreen game = new InGameScreen(players, planets, s_drawDevice);
        }

        public void ExitApp(object sender)
        {
           
        }
    }
}
