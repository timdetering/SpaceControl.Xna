using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace XNA_GUI.GUIElements
{
    public class ListBox : GUI_Base
    {
        //if there are more items in the list than can be displayed within the 
        //control at one time, 
        bool scrollable;    
        uint scrollIndex;

        bool leftButtonDown;
        //index of the current selected items, -1 indicates no item is selected.
        int selected;
        
        //Ordered list of all the items within this list box.
        List<string> itemNames;
        Color selectedColor;

        Rectangle listItemSpace;

        private int itemHeight;
        private int itemsToDraw;
        private string fontName;

        private string borderTexture, itemTexture;

        public ListBox(XmlNode listBoXml, GUI_Base parent, object owner)
            : base(listBoXml, parent, owner)
        {
            scrollIndex = 0;
            selected = -1;
            scrollable = false;
            leftButtonDown = false;

            XmlNode listBoxFrameXml = listBoXml["BorderImage"];
            XmlNode listBoxItemImageXml = listBoXml["ItemBackgroundImage"];
            if (listBoxFrameXml != null)
            {
                borderTexture = LoadTextureFromXML(listBoxFrameXml);
            }
            if (listBoxItemImageXml != null)
            {
                itemTexture = LoadTextureFromXML(listBoxItemImageXml);
            }
                

            XmlNode selectedColorXml = listBoXml["SelectedColor"];
            selectedColor = ReadColor32(selectedColorXml);
            itemNames = new List<string>(1);

            foreach (XmlNode c in listBoXml.ChildNodes)
            {
                if (c.NodeType == XmlNodeType.Element && c.Name == "ListItem")
                {
                    itemNames.Add(c.InnerText);
                }
            }

            XmlNode fontXml = listBoXml["Font"];
            if (fontXml == null)
                fontName = "ArialFont";
            else
                fontName = fontXml.InnerText;

            LoadFont(fontName);

            SpriteFont font = GetFont(fontName);
            int minItemHeight = font.LineSpacing + 4;

            int maxItemsWithoutScroll = (int)(sizePixel.Height / (float)minItemHeight);
            if (itemNames.Count > maxItemsWithoutScroll)
            {
                //implement a scrolling list box to accomidate 

                itemHeight = (int)Math.Floor((double)(sizePixel.Height / (float)maxItemsWithoutScroll));
                itemsToDraw = maxItemsWithoutScroll;
                scrollable = true;
                CreateScrollButtons();
            }
            else
            {
                itemHeight = (int)(sizePixel.Height / (float)itemNames.Count);
                itemsToDraw = itemNames.Count;
            }


        }

        /// <summary>
        /// If required, this function will create the scrolling buttons for this list box.
        /// It also maps their call back functions to the ScrollUp and ScrollDown methods in the listbox control
        /// </summary>
        protected void CreateScrollButtons()
        {
            string topButton = "<Button Name=\"ScrollBarButtonTop\"><ControlSize Height=\"15\"" +
                " Width=\"100\"/><ControlPosition X=\"0\" Y=\"0\"/><DisplayText></DisplayText><Images>" +
                "<DefaultImage Type=\"File\" Name=\"UpArrowDef.png\"/><PressedImage Type=\"File\" " +
                "Name=\"UpArrowPressed.png\"/></Images><OnClick>ScrollUp</OnClick></Button>";

            string bottomButton = "<Button Name=\"ScrollBarButtonTop\"><ControlSize Height=\"15\"" +
                " Width=\"100\"/><ControlPosition X=\"0\" Y=\"90\"/><DisplayText></DisplayText><Images>" +
                "<DefaultImage Type=\"File\" Name=\"UpArrowDef.png\"/><PressedImage Type=\"File\" " +
                "Name=\"UpArrowPressed.png\"/></Images><OnClick>ScrollDown</OnClick></Button>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(topButton);
            XmlNode topXml = doc.GetElementsByTagName("Button")[0];
            doc.LoadXml(bottomButton);
            XmlNode bottomXml = doc.GetElementsByTagName("Button")[0];

            Button top = new Button(topXml, this, this);
            Button bottom = new Button(bottomXml, this, this);

            AddChild(top);
            AddChild(bottom);
        }

        public override void Draw(GraphicsDevice graphics)
        {
            Texture2D border = (Texture2D)GetTexture(borderTexture);
            Texture2D item = (Texture2D)GetTexture(itemTexture);
            SpriteFont font = GetFont(fontName);
            

            s_GUISprite.Begin(SpriteBlendMode.AlphaBlend);
            s_GUISprite.Draw(border, drawSapce, Color.White);
            for (int i =0; i < itemsToDraw; i++)
            {
                Rectangle itemDrawSpace = new Rectangle((int)posPixel.X, (int)(posPixel.Y + i * itemHeight),
                                            (int)sizePixel.Width, itemHeight);
                s_GUISprite.Draw(item, itemDrawSpace, Color.White);
                Vector2 stringSize = font.MeasureString(itemNames[i + (int)scrollIndex]);
                Vector2 position = new Vector2();
                position.X = posPixel.X;
                position.Y = (itemHeight - stringSize.Y) * 0.5f + posPixel.Y + itemHeight * i;
                
                if(stringSize.X > sizePixel.Width)
                    stringSize.X = sizePixel.Width/stringSize.X;
                else
                    stringSize.X = 1;
                stringSize.Y = 1;

                s_GUISprite.DrawString(font, itemNames[i + (int)scrollIndex], position, Color.White, 0.0f, new Vector2(0, 0),
                    stringSize, SpriteEffects.None, 0);
                
            }
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

        /// <summary>
        /// Decreases scroll index by one
        /// </summary>
        /// <param name="sender"></param>
        public void ScrollUp(object sender)
        {
            if(scrollIndex > 0)
                scrollIndex--;
        }

        /// <summary>
        /// Increases scroll index by one.
        /// </summary>
        /// <param name="sender"></param>
        public void ScrollDown(object sender)
        {
            if (scrollIndex < (itemNames.Count - itemsToDraw))
                scrollIndex++;
        }

        public override void HandleMouse(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            //first check if a button was pressed
            base.HandleMouse(mouse);

            //now, if the click occured anywhere in the list items
            //make that the selected item.
            if (mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                leftButtonDown = true;
            else
            {
                if (leftButtonDown == true) //a click has occured
                {
                    int oldselected = selected;
                    selected = (int)(((float)mouse.Y - posPixel.Y) / (float)itemHeight) + (int)scrollIndex;
                    if (selected == oldselected)    //clicking again should deselect the item.
                        selected = -1;
                    leftButtonDown = false;
                }
            }
        }

        public string Selected
        {
            get
            {
                if (selected != -1)
                    return itemNames[selected];
                else
                    return string.Empty;
            }
        }
    }


}