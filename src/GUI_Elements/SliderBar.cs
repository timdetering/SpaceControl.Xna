using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XNA_GUI.GUIElements
{
    public class SliderBar : GUI_Base
    {
        private bool leftButtonDown;
        private bool dragging;
        private Point lastMousePosition;
        //minValue = smallest value the slider control can be set to, this coresponds to the left or top of the control
        //maxValue = maximum control value, bottom or right of the control.
        //currentValue = where the slider is current set at.
        private int minValue, maxValue, currentValue;
        public int CurrentValue
        {
            get { return currentValue; }
            set
            {
                currentValue = value;
                if (currentValue > maxValue)
                    currentValue = maxValue;
                if (currentValue < minValue)
                    currentValue = minValue;
            }
        }

        public int MaxValue
        {
            get { return maxValue; }
        }

        public int MinValue
        {
            get { return minValue; }
        }

        //abs(maxValue = minValue).  total number of values present for the slider.
        //usefull for determining position on the slider as a percentage of the length of the slider.
        private int sliderLength;

        public enum SliderOrientation { Vertical = 0, Horizontal = 1 }
        private SliderOrientation orientation;

        private string backgroundImage, sliderImage;

        public SliderBar(XmlNode sliderXml, GUI_Base parent, object owner)
            : base(sliderXml, parent, owner)
        {
            //Load in the Image for the slider and the bar
            XmlNode barImageXML = sliderXml["SliderBarImage"];
            XmlNode sliderImageXML = sliderXml["SliderImage"];

            if(barImageXML != null)
                backgroundImage = LoadTextureFromXML(barImageXML);

            if(sliderImageXML != null)
                sliderImage = LoadTextureFromXML(sliderImageXML);

            //Load in the min, max and Starting values for the control.
            try
            {
                minValue = Convert.ToInt32(sliderXml["MinValue"].InnerText);
                maxValue = Convert.ToInt32(sliderXml["MaxValue"].InnerText);
                currentValue = Convert.ToInt32(sliderXml["InitialValue"].InnerText);
            }
            catch (NullReferenceException)
            {
                uint result = MessageBox(new IntPtr(0), string.Format("Error loading in slider values in {0}", controlName),
                    "Error In slider XML", 0);
                minValue = 0;
                maxValue = 100;
                currentValue = 50;
            }

            sliderLength = Math.Abs(maxValue - minValue);
            XmlNode orientationXML = sliderXml["Orientation"];
            if (orientationXML != null && orientationXML.InnerText == "Vertical")
                orientation = SliderOrientation.Vertical;
            else
                orientation = SliderOrientation.Horizontal;

        }

        public override void Draw(GraphicsDevice graphics)
        {
            if (orientation == SliderOrientation.Horizontal)
                DrawHorizontal(graphics);
            else
                DrawVertical(graphics);

        }

        private void DrawVertical(GraphicsDevice graphics)
        {
            Texture2D barTexture, sliderTexture;
            barTexture = (Texture2D)GetTexture(backgroundImage);
            sliderTexture = (Texture2D)GetTexture(sliderImage);

            float sliderImageStartX = posPixel.X + sliderTexture.Width - (Math.Abs(sliderTexture.Width - sizePixel.Width) / 2);
            float sliderImageStartY = ((float)currentValue / (float)sliderLength) * sizePixel.Height + posPixel.Y - sliderTexture.Width / 2;

            Rectangle sliderImageArea = new Rectangle((int)sliderImageStartX, (int)sliderImageStartY, sliderTexture.Width, sliderTexture.Height);

            s_GUISprite.Begin(SpriteBlendMode.AlphaBlend);
            s_GUISprite.Draw(barTexture, drawSapce, null, Color.White,0, new Vector2(0, 0),
                SpriteEffects.None, 1);
            s_GUISprite.Draw(sliderTexture, sliderImageArea, null, Color.White, (float)Math.PI/2.0f, new Vector2(0, 0),
                SpriteEffects.None, 0);
            s_GUISprite.End();
        }


        private void DrawHorizontal(GraphicsDevice graphics)
        {
            Texture2D barTexture, sliderTexture;
            barTexture = (Texture2D)GetTexture(backgroundImage);
            sliderTexture = (Texture2D)GetTexture(sliderImage);

            float sliderImageStartX = ((float)currentValue / (float)sliderLength) * sizePixel.Width + posPixel.X - sliderTexture.Width / 2;
            float sliderImageStartY = posPixel.Y + (sizePixel.Height - sliderTexture.Height) / 2.0f;

            Rectangle sliderImageArea = new Rectangle((int)sliderImageStartX, (int)sliderImageStartY, sliderTexture.Width, sliderTexture.Height);
            s_GUISprite.Begin(SpriteBlendMode.AlphaBlend);
            s_GUISprite.Draw(barTexture, drawSapce, Color.White);
            s_GUISprite.Draw(sliderTexture, sliderImageArea, Color.White);
            s_GUISprite.End();

        }

        public override void HandleMouse(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            int deltaX = lastMousePosition.X - mouse.X;
            int deltaY = lastMousePosition.Y - mouse.Y;

            if (mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                leftButtonDown = true;
                dragging = true;
                CalculateCurrentValue(mouse.X, mouse.Y);
            }
            else if (mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released && leftButtonDown == true)
            {
                leftButtonDown = false;
                dragging = false;
                CalculateCurrentValue(mouse.X, mouse.Y);
            }

            lastMousePosition.X = mouse.X;
            lastMousePosition.Y = mouse.Y;

            base.HandleMouse(mouse);

        }

        protected override void MouseEnter(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            leftButtonDown = false;
            dragging = false;
            lastMousePosition.X = mouse.X;
            lastMousePosition.Y = mouse.Y;
        }

        protected override void MouseExit(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            leftButtonDown = false;
            dragging = false;
        }

        private void CalculateCurrentValue(int mouseX, int mouseY)
        {
            float positionPercentage = 0;
            if (orientation == SliderOrientation.Horizontal) //use the x position and control width 
            {
                positionPercentage = ((float)mouseX - posPixel.X) / sizePixel.Width;
            }
            else //use y position and control height info
            {
                positionPercentage = ((float)mouseY - posPixel.Y) / sizePixel.Height;
            }

            currentValue = (int)(positionPercentage * sliderLength) + minValue;
        }
    }
}
