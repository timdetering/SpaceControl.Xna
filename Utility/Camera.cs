using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SpaceControl.Utility
{
    public class Camera
    {
        Vector3 lookAt, eye, up;
        protected enum MoveKeys { MoveUp = 0, MoveDown = 1, MoveLeft = 2, MoveRight = 3, MoveIn = 4, MoveOut = 5 }
        protected delegate void KeyPressAction(float moveDistance);
        protected Hashtable movementKeys = new Hashtable(6);
        protected Matrix projection;
        protected int mouseWheel;
        protected int xOffset = 0, yOffset = 0;
        public int xPosition
        {
            get { return xOffset; }
        }
        public int yPosition
        {
            get { return yOffset; }
        }
        public Camera(Vector3 lookAt, Vector3 eye)
        {
            this.lookAt = lookAt;
            this.eye = eye;
            up = Vector3.Up;

            CreateMoveKeys();
            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f),
                1.33f, 1.0f, 1000.0f);
        }

        virtual protected void CreateMoveKeys()
        {
            movementKeys.Add(Keys.Up, new KeyPressAction(MoveUp));
            movementKeys.Add(Keys.Down, new KeyPressAction(MoveDown));
            movementKeys.Add(Keys.Left, new KeyPressAction(MoveLeft));
            movementKeys.Add(Keys.Right, new KeyPressAction(MoveRight));
            movementKeys.Add(Keys.PageUp, new KeyPressAction(MoveIn));
            movementKeys.Add(Keys.PageDown, new KeyPressAction(MoveOut));
        }


        public void JumpTo(Vector3 destination)
        {
            Vector3 change = destination - lookAt;
            lookAt = destination;
            eye += change;
        }

        public void MoveUp(float MoveDistance)
        {
            lookAt.Z -= MoveDistance;
            eye.Z -= MoveDistance;
            yOffset--;
        }

        public void MoveDown(float MoveDistance)
        {
            lookAt.Z += MoveDistance;
            eye.Z += MoveDistance;
            yOffset++;
        }

        public void MoveLeft(float MoveDistance)
        {
            lookAt.X -= MoveDistance;
            eye.X -= MoveDistance;
            xOffset--;
        }

        public void MoveRight(float MoveDistance)
        {
            lookAt.X += MoveDistance;
            eye.X += MoveDistance;
            xOffset++;
        }

        public void MoveIn(float MoveDistance)
        {
            Vector3 direction = lookAt - eye;
            direction.Normalize();
            direction *= MoveDistance;

            eye += direction;
        }

        public void MoveOut(float MoveDistance)
        {
            Vector3 direction = lookAt - eye;
            direction.Normalize();
            direction *= MoveDistance;
            eye -= direction;
        }

        public void Update(GameTime time)
        {
            KeyboardState keystate = Keyboard.GetState();
            Keys[] pressedKeys = keystate.GetPressedKeys();

            foreach (Keys k in pressedKeys)
            {
                if (movementKeys.ContainsKey(k))
                    ((KeyPressAction)movementKeys[k])((float)(time.ElapsedRealTime.TotalMilliseconds / 20.0));
            }

            MouseState s = Mouse.GetState();
            int scroll = mouseWheel - s.ScrollWheelValue;
            MoveOut((float)scroll/10.0f);
            mouseWheel = s.ScrollWheelValue;
        }

        public Matrix View
        {
            get { return Matrix.CreateLookAt(eye, lookAt, up); }
        }

        public Matrix Projection
        {
            get { return projection; }
            set { projection = value; }
        }

    }
}
