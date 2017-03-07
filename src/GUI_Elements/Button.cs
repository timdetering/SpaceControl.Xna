using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace XNA_GUI.GUIElements
{
    public class Button : GUI_Base
    {
        #region Attributes
        //Contians the file or resource names for each of the possible button states.
        private string[] buttonImages;

        //Button state options and current state of this control.
        //Default is when the control does not have focus
        //MouseOver is set anytime the mouse enters the mouse
        //Pressed is when a mouse button is down.
        public enum ButtonState { Default = 0, MouseOver = 1, Pressed = 2 }
        private ButtonState currentState;

        //define a function that will be called whenever the control is clicked on.
        public delegate void GUI_BUTTONCLICK(object sender);
        private GUI_BUTTONCLICK onClickFunction;

        //name of the font resource
        private string fontName;

        //Text to display on the button
        private string buttonText;

        //Colors used to tint the text and image.
        private Color backgroundColor, textColor;


        private bool leftButtonDown = false;

        #endregion Attributes

        public Button(XmlNode buttonXml, GUI_Base parent, object owner)
            :base(buttonXml, parent, owner)
        {
            XmlNode fontXml = buttonXml["Font"];
            if (fontXml != null)
                fontName = fontXml.InnerText;
            else   //if no font is specified, assign the default.
                fontName = "ArialFont";

            buttonImages = new string[3];
            XmlNode images = buttonXml["Images"];
            if (images != null)
                LoadImages(images);
            else
                LoadDefaultImages();

            buttonText = buttonXml["DisplayText"].InnerText;

            XmlNode BackgroundColor = buttonXml["BackgroundColor"];
            XmlNode TextColor = buttonXml["TextColor"];
            if (BackgroundColor == null)
                backgroundColor = Color.White;
            else
                backgroundColor = ReadColor32(BackgroundColor);

            if (TextColor == null)
                textColor = Color.White;
            else
                textColor = ReadColor32(TextColor);


            //use reflection to find the function from owner that is the call back function.
            XmlNode clickFn = buttonXml["OnClick"];
            if (clickFn != null)
            {
                string functionName = clickFn.InnerText;
                Type t = owner.GetType();
                MethodInfo m = t.GetMethod(functionName);
                if (m != null)  // just in case the method we're looking for does not exist.
                    onClickFunction = (GUI_BUTTONCLICK)Delegate.CreateDelegate(typeof(GUI_BUTTONCLICK), owner, m.Name, true);
                else
                    onClickFunction = null;
            }
        }

        /// <summary>
        /// Loads the default, mouse over and button pressed images from the xml node.
        /// </summary>
        /// <param name="ImageXml">Xml Node that contains up to three of button images and their
        /// color values and rsource types</param>
        protected void LoadImages(XmlNode ImageXml)
        {
            XmlNode defaultImage=null, mouseOver = null, pressed = null;
            defaultImage = ImageXml["DefaultImage"];
            mouseOver = ImageXml["MouseOverImage"];
            pressed = ImageXml["PressedImage"];

            if (defaultImage == null)
            {
                uint res = MessageBox(new IntPtr(0), string.Format("Default Image not specified in Button {0}",
                    controlName), "Error In button Xml", 0);
                //Load default images in place specified ones because this tag is badly formed.
            }
            else //load the rest of the images.
            {
                buttonImages[(int)ButtonState.Default] = resoursePath + defaultImage.Attributes["Name"].Value;

                if (defaultImage.Attributes["Type"].Value == "Resource")
                    LoadTexureFromResource(buttonImages[(int)ButtonState.Default]);
                else
                    LoadTexutureFromFile(buttonImages[(int)ButtonState.Default]);

                //Load in the mouse over image.
                if (mouseOver == null)
                    buttonImages[(int)ButtonState.MouseOver] = buttonImages[(int)ButtonState.Default];
                else //a mouse over image has been specified
                {
                    buttonImages[(int)ButtonState.MouseOver] = resoursePath + mouseOver.Attributes["Name"].Value;
                    if(mouseOver.Attributes["Type"].Value == "Resource")
                        LoadTexureFromResource(buttonImages[(int)ButtonState.MouseOver]);
                    else
                        LoadTexutureFromFile(buttonImages[(int)ButtonState.MouseOver]);
                }

                //finally repeat for the button pressed images.
                if(pressed == null)
                    buttonImages[(int)ButtonState.Pressed] = buttonImages[(int)ButtonState.Default];
                else
                {
                    buttonImages[(int)ButtonState.Pressed] = resoursePath + pressed.Attributes["Name"].Value;
                    if(pressed.Attributes["Type"].Value == "Resource")
                        LoadTexureFromResource(buttonImages[(int)ButtonState.Pressed]);
                    else
                        LoadTexutureFromFile(buttonImages[(int)ButtonState.Pressed]);
                }
            }
        }

        protected void LoadDefaultImages()
        {
            buttonImages[(int)ButtonState.Default] = resoursePath + "DefaultButton.png";
            buttonImages[(int)ButtonState.MouseOver] = resoursePath + "DefaultButton.png";
            buttonImages[(int)ButtonState.Pressed] = resoursePath + "ButtonPressed.png";

            foreach (string s in buttonImages)
                LoadTexutureFromFile(s);
        }

        protected void OnClick()
        {
            if (onClickFunction != null)
                onClickFunction(this);
        }

        public override void Draw(GraphicsDevice graphics)
        {
            Texture2D t = (Texture2D)GetTexture(buttonImages[(int)currentState]);
            SpriteFont font = GetFont(fontName);

            s_GUISprite.Begin(SpriteBlendMode.AlphaBlend);
            s_GUISprite.Draw(t, drawSapce, backgroundColor);
            Vector2 stringSize = font.MeasureString(buttonText);
            float scale = (sizePixel.Width / stringSize.X) * .8f;
            float xOffset = (sizePixel.Width - (sizePixel.Width * scale)) / 2.0f;
            float yOffset = ((sizePixel.Height - stringSize.Y) * scale) / 2.0f;

            if (buttonText != null && buttonText != string.Empty)
            {
                
                s_GUISprite.DrawString(font, buttonText,
                                        new Microsoft.Xna.Framework.Vector2(posPixel.X + xOffset, posPixel.Y + yOffset),
                                        textColor, 
                                        0.0f, new Vector2(0, 0),
                                        (stringSize.X / sizePixel.Width) * .9f,
                                        SpriteEffects.None, 
                                        0);
            }
            s_GUISprite.End();
            base.Draw(graphics);
        }

        public override void HandleMouse(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            base.HandleMouse(mouse);

            if (mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                currentState = ButtonState.Pressed;
                leftButtonDown = true;
            }
            else if (mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released &&
                leftButtonDown == true)
            {
                //handle on click.
                OnClick();
                currentState = ButtonState.MouseOver;
            }
            
        }

        protected override void MouseEnter(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            leftButtonDown = false;
            currentState = ButtonState.MouseOver;
        }

        protected override void MouseExit(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            leftButtonDown = false;
            currentState = ButtonState.Default;
        }
    }
}
