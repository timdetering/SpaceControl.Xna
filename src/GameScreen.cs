using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using SpaceControl.Utility;
using Microsoft.Xna.Framework;

namespace SpaceControl.GameScreen
{
    public class GameScreen : IDisposable
    {
        #region Stack Operators
        protected static Stack<GameScreen> s_screenList = new Stack<GameScreen>(1);
        public void Push(GameScreen g)
        {
            s_screenList.Push(g);
        }

        public GameScreen Pop()
        {
            return s_screenList.Pop();
        }

        public GameScreen Peek()
        {
            return s_screenList.Peek();
        }
        #endregion Stack Operators

        protected static GraphicsDevice s_drawDevice = null;

        public GameScreen(GraphicsDevice graphics)
        {
            s_drawDevice = graphics;
            Push(this);
        }



        public virtual void Draw(GraphicsDevice drawDevice)
        {
            return;
        }

        public virtual void Update(GameTime time)
        {
            return;
        }

        public static void DrawScreen(GraphicsDevice drawDevice)
        {
            s_screenList.Peek().Draw(drawDevice);
        }

        public static void UpdateScreen(GameTime time)
        {
            s_screenList.Peek().Update(time);
        }

        public virtual void Dispose()
        {
        }
    }
}
