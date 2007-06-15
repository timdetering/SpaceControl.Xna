using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna;
using System.Xml;
using System.Drawing;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using System.Reflection;


namespace XNA_GUI.GUIElements
{
    /// <summary>
    /// The GUI Base is the element from which all GUI controls must be derivded. 
    /// </summary>
    abstract public class GUI_Base
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint MessageBox(IntPtr hWnd, string text, string caption, uint type);

        //Attributes that need to be shared and accessed by child classes
        #region Common Elements
        //percent for the purpose of this control is between 0.00 and 1.00, and is the percent of the parent's view port
        //this child control takes up.
        //Pixels are always stored in absolute coordinates from the main window's corner.
        protected SizeF sizePercent, sizePixel;
        public SizeF SizePixels
        {
            get { return sizePixel; }
        }
        public SizeF SizePercent
        {
            get { return sizePercent; }
        }

        protected PointF posPercent, posPixel;
        public PointF PositionPercent
        {
            get { return posPercent; }
        }
        public PointF PositionPixels
        {
            get { return posPixel; }
        }

        protected Microsoft.Xna.Framework.Rectangle drawSapce;

        protected GUI_Base parentObject;

        //This allows searching for controls by string identifier.
        protected string controlName;
        public string ControlName
        {
            get { return controlName; }
            set { controlName = value; }
        }

        //each control has a color rendered at its background.  This is a 32 bit ARGB color value.
        protected int primaryColor;

        //to conserve resources and provide more efficient rendering, a single sprite batch is created for the
        //GUI in the InitializeGUI function.  This must be cleaned up by the UnloadGUI function.
        protected static SpriteBatch s_GUISprite = null;

        protected static ContentManager s_content = null;

        protected static GraphicsDeviceManager s_graphics = null;

        //absolute path to the main GUI reso;
        protected static string resoursePath;
        #endregion

        public GUI_Base(XmlNode GUIControlXml, GUI_Base parent, object owner)
        {
            controlName = GUIControlXml.Attributes["Name"].Value;
            parentObject = parent;
            //Read in the size and position from the XML Node
            ReadSize(GUIControlXml);
            ReadPosition(GUIControlXml);

            if (parent != null)
                Resize(parent);
            else
            {
                sizePixel.Height = sizePercent.Height * s_graphics.PreferredBackBufferHeight;
                sizePixel.Width = sizePercent.Width * s_graphics.PreferredBackBufferWidth;
                posPixel.X = posPercent.X * s_graphics.PreferredBackBufferWidth;
                posPixel.Y = posPercent.Y * s_graphics.PreferredBackBufferHeight;
                drawSapce = new Microsoft.Xna.Framework.Rectangle((int)posPixel.X, (int)posPixel.Y,
                    (int)sizePixel.Width, (int)sizePixel.Height);
            }

            LoadChildren(GUIControlXml, owner);


        }

        /// <summary>
        /// Checks to see if a point falls within the control.  This may be overriden by children that do not wish
        /// to act as a purely rectangular control.
        /// </summary>
        /// <param name="mouseX">X Position of the point to check</param>
        /// <param name="mouseY">Y Position of the point to check</param>
        /// <returns>true if the point is in the control, otherwise false</returns>
        public virtual bool PointIsIn(int mouseX, int mouseY)
        {
            return (mouseX >= posPixel.X && mouseX < posPixel.X + sizePixel.Width &&
                    mouseY >= posPixel.Y && mouseY < posPixel.Y + sizePixel.Height);
        }

        /// <summary>
        /// Reads the Size control attributes from Xml.
        /// </summary>
        /// <param name="GUIControlXml">Master XML node containing all attributes of this GUI element</param>
        private void ReadSize(XmlNode GUIControlXml)
        {
            try
            {
                sizePercent.Width = (float)Convert.ToDouble(GUIControlXml["ControlSize"].Attributes["Width"].Value) / 100.0f;
                sizePercent.Height = (float)Convert.ToDouble(GUIControlXml["ControlSize"].Attributes["Height"].Value) / 100.0f;
            }
            catch (NullReferenceException)
            {
                uint result = MessageBox(new IntPtr(0), string.Format("XML error in Size element for GUI Control {0}." +
                    "Please make sure the XML is properly formated: <Size Height=value Width=value/>", controlName),
                    "Error Loading GUI Control", 0);
            }
        }

        /// <summary>
        /// Reads Position control attributes from Xml
        /// </summary>
        /// <param name="GUIControlXml">Master XML node containing all attributes of this GUI element</param>
        private void ReadPosition(XmlNode GUIControlXml)
        {
            try
            {
                posPercent.X = (float)Convert.ToDouble(GUIControlXml["ControlPosition"].Attributes["X"].Value) / 100.0f;
                posPercent.Y = (float)Convert.ToDouble(GUIControlXml["ControlPosition"].Attributes["Y"].Value) / 100.0f;
                return;
            }
            catch (NullReferenceException)
            {
                uint result = MessageBox(new IntPtr(0), string.Format("XML error in Position element of GUI control {0}" +
                    ".  Please make sure the XML file is formated properly: <Position X=value Y=value/>", controlName),
                    "Error Loading GUI Control", 0);
            }
        }

        /// <summary>
        /// Pulls ARGB color info from an xml node.  The node must use Red Green Blue and Alpha attributes
        /// within the XmlNode passed.  The values must be ints between 0 and 255.
        /// </summary>
        /// <param name="ColorNode">XmlNode containing the coler values</param>
        /// <returns>XNA color value described by the XML.</returns>
        protected Microsoft.Xna.Framework.Graphics.Color ReadColor32(XmlNode ColorNode)
        {
            byte red, green, blue, alpha;
            red = Convert.ToByte(ColorNode.Attributes["Red"].Value);
            green = Convert.ToByte(ColorNode.Attributes["Green"].Value);
            blue = Convert.ToByte(ColorNode.Attributes["Blue"].Value);
            alpha = Convert.ToByte(ColorNode.Attributes["Alpha"].Value);
            return new Microsoft.Xna.Framework.Graphics.Color(red, green, blue, alpha);
        }

        #region Xml Loader Helpers
        static private Hashtable s_controlTypes = new Hashtable(5);

        protected void LoadChildren(XmlNode parentNode, object owner)
        {
            foreach (XmlNode node in parentNode.ChildNodes)
            {
                if(node.NodeType == XmlNodeType.Element)
                {
                    object [] parameters = new object[3];
                    parameters[0] = node;
                    parameters[1] = this;
                    parameters[2] = owner;

                    string asmName = "XNA_GUI.GUIElements." + node.Name;
                    Type t = Type.GetType(asmName);
                    if (t != null)
                    {
                        GUI_Base child = (GUI_Base)Activator.CreateInstance(t, parameters);
                        //GUI_Base child = (GUI_Base)Activator.CreateInstance((Type)s_controlTypes[node.Name], parameters);
                        AddChild(child);
                    }
                }
            }
        }

        static public GUI_Base LoadLayout(string fileToLoad, object owners)
        {
            XmlDocument doc = new XmlDocument();
            if (System.IO.File.Exists(fileToLoad) == false)
            {
                uint ret = MessageBox(new IntPtr(0), string.Format("The file {0} does not exist in the current directory", fileToLoad),
                    "Error Loading GUI Layout", 0);
                return null;
            }
            doc.Load(fileToLoad);
            XmlNodeList nodes = doc.GetElementsByTagName("GUIRoot");
            Frame rootGUIControl = new Frame(nodes[0]["Frame"], null, owners);
            return rootGUIControl;
        }

        #endregion Xml Loader Helpers

        #region System Events

        /// <summary>
        /// Recalculates the existing client size of this control based on the new size of the parent.
        /// </summary>
        /// <param name="parent"></param>
        public virtual void Resize(GUI_Base parent)
        {
            SizeF parentSize = parent.sizePixel;
            PointF parentPos = parent.PositionPixels;
            sizePixel.Width = sizePercent.Width * parentSize.Width;
            sizePixel.Height = SizePercent.Height * parentSize.Height;

            posPixel.X = parentPos.X + posPercent.X * parentSize.Width;
            posPixel.Y = parentPos.Y + parentSize.Height * posPercent.Y;

            drawSapce = new Microsoft.Xna.Framework.Rectangle((int)posPixel.X, (int)posPixel.Y,
                (int)sizePixel.Width, (int)sizePixel.Height);

            foreach (GUI_Base child in childList)
                child.Resize(this);
        }

        public virtual void Draw(GraphicsDevice graphics)
        {
            foreach (GUI_Base c in childList)
                c.Draw(graphics);
        }

        public virtual void Update(TimeSpan elapsedTime)
        {
            foreach (GUI_Base c in childList)
                c.Update(elapsedTime);
        }

        
        public static void Initialize(GraphicsDeviceManager graphics, ContentManager content, string resPath)
        {
            if(s_GUISprite == null)
                s_GUISprite = new SpriteBatch(graphics.GraphicsDevice);
            s_TextureResources = new Hashtable(20);
            s_XNAFonts = new Hashtable(1);

            s_graphics = graphics;
            s_content = content;
            resoursePath = resPath;

        }

        /// <summary>
        /// Clears all the resources allocated to the GUI Controls.
        /// </summary>
        public static void UnloadAll()
        {
            foreach (Texture t in s_TextureResources.Values)
                t.Dispose();
            s_TextureResources.Clear();

            s_GUISprite.Dispose();
            s_GUISprite = null;
        }

        public static void DeviceLost()
        {
            foreach (Texture t in s_TextureResources.Values)
                t.Dispose();
            s_GUISprite.Dispose();
        }

        public static void DeviceReset(GraphicsDeviceManager graphics, ContentManager content)
        {
            s_content = content;
            s_graphics = graphics;
            if (s_TextureResources == null)
                return;

            List<string> reload = new List<string>(s_TextureResources.Keys.Count);
            foreach (string s in s_TextureResources.Keys)
                reload.Add(s);
            s_TextureResources.Clear();
            foreach (string s in reload)
            {
                Texture t = Texture.FromFile(graphics.GraphicsDevice, s);
                s_TextureResources.Add(s, t);
            }
            s_GUISprite = new SpriteBatch(graphics.GraphicsDevice);
        }
        #endregion 

        #region Child Managment
        private List<GUI_Base> childList = new List<GUI_Base>();

        /// <summary>
        /// Adds a new child element to this control.
        /// </summary>
        /// <param name="child">Class derived from GUI Base that falls within this control</param>
        public void AddChild(GUI_Base child)
        {
            if(childList.Contains(child) == false)
                childList.Add(child);
        }

        /// <summary>
        /// Removes and existing child control.
        /// </summary>
        /// <param name="child">Control that needs to be removed</param>
        public void RemoveChild(GUI_Base child)
        {
            childList.Remove(child);
        }

        /// <summary>
        /// Gets the child object who's controlName matches the string passed.
        /// </summary>
        /// <param name="controlName">Name to search for</param>
        /// <returns>The control of the specified name, or null if no control by that name is found</returns>
        public GUI_Base GetChildByName(string controlName)
        {
            foreach (GUI_Base child in childList)
            {
                if (child.ControlName == controlName)
                    return child;
            }

            return null;
        }

        #endregion Child Management

        #region Input Events
        protected GUI_Base s_controlWithFocus = null;

        /// <summary>
        /// The main entry point for mouse informaiton to be passed to the GUI controls.
        /// The mouse state is typically passed to the GUI by the engine's update() function every frame;
        /// though there is no requirement to do so.  The GUI will only respond to input events using this 
        /// function.
        /// </summary>
        /// <param name="mouse"></param>
        public virtual void HandleMouse(MouseState mouse)
        {
            foreach (GUI_Base c in childList)
            {
                if (c.PointIsIn(mouse.X, mouse.Y) == true)
                {
                    if (c != s_controlWithFocus)
                    {
                        c.MouseEnter(mouse);
                        if (s_controlWithFocus != null)
                            s_controlWithFocus.MouseExit(mouse);
                        s_controlWithFocus = c;
                    }
                    c.HandleMouse(mouse);
                    return;
                }
            }

            //if no child gets focus, child with focus must be set to null
            if(s_controlWithFocus != null)
            {
                s_controlWithFocus.MouseExit(mouse);
                s_controlWithFocus = null;
            }
            return;
        }

        /// <summary>
        /// Special operations may need to be handled when the mouse enters of leaves a control.
        /// All GUI_Base child classes are required to implement these functions.
        /// </summary>
        /// <param name="mouse">The current mouse state as polled by the XNA.Input.Mouse</param>
        protected abstract void MouseEnter(MouseState mouse);

        protected abstract void MouseExit(MouseState mouse);
        #endregion Input Events

        #region Resource Manageer
        private static Hashtable s_TextureResources;
        private static Hashtable s_XNAFonts;

        public string LoadTextureFromXML(XmlNode image)
        {
            string imageName = resoursePath + image.Attributes["Name"].Value;
            try
            {
                if (image.Attributes["Type"].Value == "Resource")
                    LoadTexureFromResource(imageName);
                else
                    LoadTexutureFromFile(imageName);
            }
            catch (Exception)
            {
                //we will want to log the loading error and return a default string.  
                string def = resoursePath + "DefaultButton";
                return def;
            }

            return imageName;
        }
        public void LoadTexutureFromFile(string aboslutePath)
        {
            if (s_TextureResources.ContainsKey(aboslutePath))
                return;

            Texture t = Texture.FromFile(s_graphics.GraphicsDevice, aboslutePath);
            s_TextureResources.Add(aboslutePath, t);
        }

        public void LoadTexureFromResource(string resourceName)
        {
            if (s_TextureResources.ContainsKey(resourceName) == true)
                return;

            Texture t = s_content.Load<Texture>(resourceName);
            s_TextureResources.Add(resourceName, t);
        }

        public Texture GetTexture(string resource)
        {
            return (Texture)s_TextureResources[resource];
        }

        public void LoadFont(string resourceName)
        {
            if (s_XNAFonts.ContainsKey(resourceName) == true)
                return;
            SpriteFont font = s_content.Load<SpriteFont>(resourceName);
            s_XNAFonts.Add(resourceName, font);
        }

        public SpriteFont GetFont(string resourceName)
        {
            return (SpriteFont)s_XNAFonts[resourceName];
        }

        #endregion Resource Manager
    }
}
