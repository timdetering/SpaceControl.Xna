using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml;

namespace XNA_GUI.GUIElements
{
    class TextLabel : GUI_Base
    {
        #region Attributes

        private const string c_defaultBackground = "LabelBackground";
        private const string c_defaultFont = "ArialFont";
        private string displayText;
        public string DisplayText
        {
            get { return displayText; }
            set { displayText = value; }
        }

        private string backgroundImage;
        private string fontName;
        private float textPaddingVertical;
        Color backgroundColor, textColor;
        
        #endregion Attributes
        public TextLabel(XmlNode TextLabelXml, GUI_Base parent, object owner)
            : base(TextLabelXml, parent, owner)
        {
            displayText = TextLabelXml["DisplayText"].InnerText;
            XmlNode background = TextLabelXml["BackgroundImage"];

            if (background != null)
            {
                backgroundImage = LoadTextureFromXML(background);
            }

            XmlNode fontInfo = TextLabelXml["Font"];
            if (fontInfo != null)
            {
                fontName = fontInfo.InnerText;
            }
            else
                fontName = c_defaultFont;

            XmlNode ImageColorInfo = TextLabelXml["ImageColor"];
            if (ImageColorInfo != null)
                backgroundColor = this.ReadColor32(ImageColorInfo);
            else
                backgroundColor = Color.White;

            XmlNode TextColorInfo = TextLabelXml["TextColor"];
            if (TextColorInfo != null)
                textColor = ReadColor32(TextColorInfo);
            else
                textColor = Color.White;

            LoadFont(fontName);
            Resize(parent);
        }

        public override void Draw(GraphicsDevice graphics)
        {
            SpriteFont font = GetFont(fontName);
            s_GUISprite.Begin(SpriteBlendMode.AlphaBlend);
            if (backgroundImage != null && backgroundImage != string.Empty)
            {
                Texture2D t = (Texture2D)GetTexture(backgroundImage);
                s_GUISprite.Draw(t, drawSapce, backgroundColor);
            }
            Vector2 stringSize = font.MeasureString(displayText);
            float scale = 1.0f;
            if (stringSize.X > sizePixel.Width)
                scale = sizePixel.Width / stringSize.X;

            s_GUISprite.DrawString(font, displayText, new Vector2(posPixel.X, posPixel.Y + textPaddingVertical), textColor,
                0.0f, Vector2.Zero, scale, SpriteEffects.None, 0);
            s_GUISprite.End();
            base.Draw(graphics);
        }

        protected override void MouseEnter(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            return;
        }

        protected override void MouseExit(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            return;
        }

        public override void Resize(GUI_Base parent)
        {
            base.Resize(parent);

            //we'll also want to cacluate where to position the text in relation to the 
            //upper left corner of the control
            if (fontName != null && fontName != String.Empty)
            {
                SpriteFont font = GetFont(fontName);
                textPaddingVertical = 0.5f * (sizePixel.Height - font.LineSpacing);
            }
        }
    }
}
