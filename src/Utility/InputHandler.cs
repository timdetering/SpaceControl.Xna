using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Reflection;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace SpaceControl.Utility
{
    public class InputHandler
    {
        //Posible input events to use
        public enum Events
        {
            LeftClick = 0,
            RightClick = 1,
            MouseMove = 2,
            ScrollWheelMove = 3
        }
       
        //Possible game states the main game may be in right now
        public enum GameState
        {
            Default = 0,
            PlanetSelected = 1,
            CreateDeploymentRoute = 2,
            Menu = 3
        }

        //Callback handler format
        public delegate void InputEventHandler(MouseState mouse, Keys[] pressedKeys, Vector3 mouseChange);

        //The current GameState, this is always maintained by the Input Handler to ensure the proper
        //Action is taken with each event detected.  
        protected GameState currentState = GameState.Default;
        public GameState CurrentGameState
        {
            get { return currentState; }
            set { currentState = value; }
        }

        //Mouse status information, this gets updated every frame.  These are used to determine
        //changes from previous to this frame.
        protected bool leftMouseDown = false, rightMouseDown = false;
        protected int lastMouseX = 0, lastMouseY = 0, lastScrollWheel = 0;

        //handlerMaps[GameState][Event] to call the appropriated handler functions
        protected List<List<InputEventHandler>> handlerMap;

        //Default constructor, requires an XML file that enumrates the available actions to take 
        //on various GameState / Event conditions.
        public InputHandler(string inputEventsFile, object callBackHandler)
        {
            //fill out the handler map and populate the actions as null for now.
            handlerMap = new List<List<InputEventHandler>>(Enum.GetValues(typeof(GameState)).Length);
            for (int i = 0; i < Enum.GetValues(typeof(GameState)).Length; i++)
            {
                handlerMap.Add(new List<InputEventHandler>(Enum.GetValues(typeof(Events)).Length));
                for (int j = 0; j < Enum.GetValues(typeof(Events)).Length; j++)
                    handlerMap[i].Add(null);
            }

            //Get the available options for game state and events as strings so we can compare
            //them to what is specified in XML.
            string[] eventNames = Enum.GetNames(typeof(Events));
            string[] gamestateNames = Enum.GetNames(typeof(GameState));
            List<string> eventList = new List<string>(eventNames);
            List<string> gameStateList = new List<string>(gamestateNames);

            //Load the XML handler informaiton and load each action.
            XmlDocument doc = new XmlDocument();
            doc.Load(inputEventsFile);
            XmlNodeList handlers = doc.GetElementsByTagName("InputEventHandler");
            foreach (XmlNode n in handlers)
            {
                //An action has three components, what game state it is attached to,
                //what input event should trigger it
                //and the name of the function in the callBackHandler that needs to be invoked.
                string gameState = n.Attributes["GameState"].Value;
                string eventType = n.Attributes["Event"].Value;
                string callback = n.Attributes["Action"].Value;
                int gameStateIndex = 0, eventIndex = 0;
                gameStateIndex = gameStateList.IndexOf(gameState);
                eventIndex = eventList.IndexOf(eventType);

                //Create the new delegate and assign it to the appropriate handlerMap index.
                InputEventHandler action = (InputEventHandler)Delegate.CreateDelegate(typeof(InputEventHandler), callBackHandler,
                    callback, true);

                handlerMap[gameStateIndex][eventIndex] = action;
                
            }

        }

        /// <summary>
        /// If an action needs to be changed after initial loading, this function can be used 
        /// to register a new action with an event / gamestate combination.
        /// </summary>
        /// <param name="inputEvent">The event the action is tied to</param>
        /// <param name="state">The GameState that must be active for the action to take place</param>
        /// <param name="handler">The InputEventHandler function to call.</param>
        public void RegisterHandler(Events inputEvent, GameState state, InputEventHandler handler)
        {
            handlerMap[(int)state][(int)inputEvent] = handler;
        }

        /// <summary>
        /// Determines all the input events that have occured since the last frame and calls their
        /// respective handlers, while the gamestate may be changed in the callback functions, all
        /// events will be called for the gamestate we are in before UpdateHandler is called.
        /// </summary>
        /// <param name="time">Elapsed GameTime since the last update</param>
        public void UpdateHandler(GameTime time)
        {
            MouseState mouse = Mouse.GetState();
            KeyboardState keystate = Keyboard.GetState();
            List<int> events = new List<int>();

            int deltaX = lastMouseX - mouse.X;
            int deltaY = lastMouseY - mouse.Y;
            int deltaZ = lastScrollWheel - mouse.ScrollWheelValue;

            Vector3 mouseChange = new Vector3(deltaX, deltaY, deltaZ);
            if (deltaX != 0 || deltaY != 0)
            {
                events.Add((int)Events.MouseMove);
            }

            if (deltaZ != 0)
            {
                events.Add((int)Events.ScrollWheelMove);
            }

            if (leftMouseDown == true && mouse.LeftButton == ButtonState.Released)
            {
                events.Add((int)Events.LeftClick);
                leftMouseDown = false;
            }
            else if (mouse.LeftButton == ButtonState.Pressed)
                leftMouseDown = true;

            if (rightMouseDown == true && mouse.RightButton == ButtonState.Released)
            {
                events.Add((int)Events.RightClick);
                rightMouseDown = false;
            }
            else if (mouse.RightButton == ButtonState.Pressed)
                rightMouseDown = true;

            int state = (int)currentState;

            foreach (int eventFound in events)
            {
                InputEventHandler handler = (InputEventHandler)handlerMap[state][eventFound];
                if (handler != null)
                    handler(mouse, keystate.GetPressedKeys(), mouseChange);
            }


        }
    }
}
