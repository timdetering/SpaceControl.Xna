#region Using directives
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System.IO;
using SpaceControl.Utility;

#endregion Using Directives


namespace SpaceControl.Entities
{
    public class BaseEntity
    {
        protected static List<BaseEntity> s_entityList = new List<BaseEntity>();

        protected Vector3 worldPosition;
        public Vector3 Position
        {
            get { return worldPosition; }
        }

        protected Vector3 scale;
        protected Vector3 rotation;
        protected static Random r = new Random();
        protected static SpriteBatch s_sprite;
        protected static SpriteFont s_font;

        public static void Initalize(ContentManager content, GraphicsDevice graphics, string resourcePath)
        {
            s_ResourcePath = resourcePath;
            s_graphics = graphics;
            s_Content = content;
            s_sprite = new SpriteBatch(graphics);
            s_font = content.Load<SpriteFont>("ArialFont");
        }

        public BaseEntity()
        {
            worldPosition = new Vector3(0, 0, 0);
            scale = new Vector3(1, 1, 1);
            rotation = new Vector3(0, 0, 0);
            s_entityList.Add(this);
        }

        public static void RenderScene(GraphicsDevice drawDevice, Camera viewCamera)
        {
            lock (s_entityList)
            {
                for (int i = 0; i < s_entityList.Count; i++)
                    s_entityList[i].Draw(drawDevice, viewCamera);
            }
        }

        public static void UpdateScene(GameTime time)
        {
            for (int i = 0; i < s_entityList.Count; i++)
                s_entityList[i].Update(time);
        }

        virtual public void Draw(GraphicsDevice drawDevice, Camera viewCamera)
        {

        }

        virtual public void Update(GameTime time)
        {
        }

        virtual public bool PointIsIn(float x, float y, Matrix view, Matrix projection)
        {
            return false;
        }


        #region Resource Manager
        protected static string s_ResourcePath = string.Empty;
        public static string ResourcePath
        {
            get { return s_ResourcePath; }
            set
            {
                s_ResourcePath = value;
                if (s_ResourcePath[s_ResourcePath.Length - 1] != '\\')
                    s_ResourcePath = s_ResourcePath + "\\";
            }
        }

        protected static Hashtable s_Models = new Hashtable(1);
        protected static Hashtable s_Textures = new Hashtable(10);
        protected static ContentManager s_Content = null;
        protected static GraphicsDevice s_graphics = null;
        
        protected string LoadModel(string resourceName)
        {
            if (s_Models.ContainsKey(resourceName))
                return resourceName;
            Model m = s_Content.Load<Model>(resourceName);
            if (m == null)
                return string.Empty;
            s_Models.Add(resourceName, m);
            return resourceName;
        }

        public virtual void DeviceLostChild()
        {

        }

        public virtual void DeviceResetChild(GraphicsDevice graphics, ContentManager content)
        {

        }

        public static void DeviceLost()
        {
            s_graphics = null;
            
            foreach (Texture t in s_Textures.Values)
                t.Dispose();

            foreach (BaseEntity b in s_entityList)
                b.DeviceLostChild();
        }

        public static void DeviceReset(GraphicsDevice graphics, ContentManager content)
        {
            s_graphics = graphics;
            s_Content = content;
            if (s_Textures == null)
                return;

            List<string> textures = new List<string>(s_Textures.Values.Count);
            foreach(string s in s_Textures.Keys)
                textures.Add(s);

            s_Textures.Clear();
            foreach(string s in textures)
            {
                Texture t;

                if(s.IndexOf(".",1) >  2)
                    t = Texture.FromFile(graphics, s);
                else
                    t = (Texture)content.Load<Texture2D>(s);
                s_Textures.Add(s, t);
            }

            List<string> meshes = new List<string>(s_Models.Count);
            foreach (string s in s_Models.Keys)
                meshes.Add(s);

            s_Models.Clear();
            foreach (string s in meshes)
            {
                Model m = s_Content.Load<Model>(s);
                s_Models.Add(s, m);
            }

            foreach (BaseEntity b in s_entityList)
                b.DeviceResetChild(graphics, content);
        }

        protected Model GetModel(string resourceName)
        {
            return (Model)s_Models[resourceName];
        }

        protected string LoadTexture(string resourceName)
        {
            
            if (File.Exists(resourceName) == false)
            {
                //try to append the aboslute path to our resource directory.
                resourceName = s_ResourcePath + resourceName;
                if (File.Exists(resourceName) == false) //if we still can't find it, return failure.
                    return string.Empty;
            }

            if (s_Textures.ContainsKey(resourceName))
                return resourceName;

            Texture t = Texture.FromFile(s_graphics, resourceName);
            if (t == null)
                return string.Empty;

            s_Textures.Add(resourceName, t);
            return resourceName;
        }

        protected Texture GetTexure(string resourceName)
        {
            if (s_Textures.ContainsKey(resourceName))
                return (Texture)s_Textures[resourceName];
            else
            {
                string result = LoadTexture(resourceName);
                if (result == string.Empty)
                    return null;

                return (Texture)s_Textures[result];
            }
        }

        #endregion Resource Manager
    }
}
