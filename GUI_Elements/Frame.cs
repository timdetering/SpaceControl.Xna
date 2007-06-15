using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Xml;

namespace XNA_GUI.GUIElements
{
    public class Frame : GUI_Base
    {
        string texture = "DefaultFrameBackground.png";

        public Frame(XmlNode FrameXml, GUI_Base parent, object owner)
            : base(FrameXml, parent, owner)
        {
            texture = resoursePath + texture;
            LoadTexutureFromFile(texture);
        }

        protected override void MouseEnter(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            return;
        }

        protected override void MouseExit(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            return;
        }

        public override void Draw(GraphicsDevice graphics)
        {
            Texture2D t = (Texture2D)GetTexture(texture);

            s_GUISprite.Begin(SpriteBlendMode.AlphaBlend);
            s_GUISprite.Draw(t, drawSapce, Color.White);
            s_GUISprite.End();
            base.Draw(graphics);
        }
    }
}
