using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XNA_GUI.GUIElements
{
    public class Window : GUI_Base
    {
        //Make iterrating through the possible textures a bit easier.
        public enum ImageNames { TitleBar = 0, Left = 1, Right = 2, Bottom = 3, Background = 4 }
        
        //These are the Names of the various XML nodes under the <Images> tag.  
        protected string[] c_ImageNodes = { "TitleBar", "LeftSide", "RightSide", "Bottom", "Background" };
        
        //images names.  will be string.empty if the texture is not specified in XML.
        string[] images;

        //if the mouse movement in this update should be translated into repositioning the window.
        bool dragging;
        Rectangle[] imageDrawSpaces;
        int mouseX, mouseY;

        public Window(XmlNode windowXml, GUI_Base parent, object owner)
            :base(windowXml, parent, owner)
        {
            images = new string[5];
            imageDrawSpaces = new Rectangle[5];

            XmlNode imageNode = windowXml["Images"];
            if (imageNode != null)
            {
                for (int i = 0; i < c_ImageNodes.Length; i++)
                {
                    XmlNode currentImage = imageNode[c_ImageNodes[i]];
                    if (currentImage != null)
                    {
                        images[i] = LoadTextureFromXML(currentImage);
                        CreateDrawSpace((ImageNames)i);
                    }
                    else
                    {
                        images[i] = string.Empty;
                        imageDrawSpaces[i] = new Rectangle(0, 0, 0, 0);
                    }
                }
            }
        }

        /// <summary>
        /// sets up the draw rectangle for the images specified.
        /// </summary>
        /// <param name="imageID">Index of the texture to set up.</param>
        private void CreateDrawSpace(ImageNames imageID)
        {
            switch (imageID)
            {
                case ImageNames.TitleBar:
                    {
                        Texture2D titleTexture = (Texture2D)GetTexture(images[(int)imageID]);
                        imageDrawSpaces[(int)imageID] = new Rectangle(0, 0, (int)sizePixel.Width, titleTexture.Height);
                        break;
                    }
                case ImageNames.Background:
                    {
                        imageDrawSpaces[(int)imageID] = new Rectangle(0, 0, (int)sizePixel.Width, (int)sizePixel.Height);
                        break;
                    }
                case ImageNames.Bottom:
                    {
                        Texture2D bottomTexture = (Texture2D)GetTexture(images[(int)imageID]);
                        imageDrawSpaces[(int)imageID] = new Rectangle(0, (int)sizePixel.Height - bottomTexture.Height,
                            (int)sizePixel.Width, bottomTexture.Height);
                        break;
                    }
                case ImageNames.Left:
                    {
                        Texture2D leftTexture = (Texture2D)GetTexture(images[(int)imageID]);
                        imageDrawSpaces[(int)imageID] = new Rectangle(0, 0, leftTexture.Width, (int)sizePixel.Height);
                        break;
                    }
                case ImageNames.Right:
                    {
                        Texture2D rightTextutre = (Texture2D)GetTexture(images[(int)imageID]);
                        imageDrawSpaces[(int)imageID] = new Rectangle((int)sizePixel.Width - rightTextutre.Width,
                            0, rightTextutre.Width, (int)sizePixel.Height);
                        break;
                    }
            }
        }

        /// <summary>
        /// Draws the window and all of it's children to the screen.
        /// </summary>
        /// <param name="graphics"></param>
        public override void Draw(GraphicsDevice graphics)
        {
            base.Draw(graphics);
            Rectangle offsetRect;
            s_GUISprite.Begin(SpriteBlendMode.AlphaBlend);
            for(int i = 0; i < images.Length; i++)
            {
                if (images[i] != string.Empty)
                {
                    Texture2D texture = (Texture2D)GetTexture(images[i]);
                    offsetRect = imageDrawSpaces[i];
                    offsetRect.X = offsetRect.X + (int)posPixel.X;
                    offsetRect.Y = offsetRect.Y + (int)posPixel.Y;
                    s_GUISprite.Draw(texture, offsetRect, null, Color.White);
                }
            }
            s_GUISprite.End();

        }

        /// <summary>
        /// repositions / resizes the window and its children.
        /// </summary>
        /// <param name="parent">The already resized parent of this window</param>
        public override void Resize(GUI_Base parent)
        {
            base.Resize(parent);
            if (images != null)
            {
                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i] != string.Empty)
                        CreateDrawSpace((ImageNames)i);
                }
            }
        }

        protected override void MouseEnter(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            mouseX = mouse.X;
            mouseY = mouse.Y;
            dragging = false;
        }

        protected override void MouseExit(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            dragging = false;
        }

        public override void HandleMouse(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            base.HandleMouse(mouse);

            if(mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                if(dragging == true)
                {
                    posPixel.X += mouse.X - mouseX;
                    posPixel.Y += mouse.Y - mouseY;
                    posPercent.X = posPixel.X / parentObject.SizePixels.Width;
                    posPercent.Y = posPixel.Y / parentObject.SizePixels.Height;
                    Resize(parentObject);
                }
                else if(imageDrawSpaces[(int)ImageNames.TitleBar].Contains(new Point(mouse.X-(int)posPixel.X, mouse.Y-(int)posPixel.Y)))
                {
                    dragging = true;
                }
            }
            else
                dragging = false;

            mouseY = mouse.Y;
            mouseX = mouse.X;
        }
    }
}
