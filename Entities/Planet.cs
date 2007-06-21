using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.IO;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SpaceControl.Utility;

namespace SpaceControl.Entities
{
    public class Planet : BaseEntity
    {
        protected static List<string> s_planetNames = new List<string>();
        protected static AppSettingsReader s_reader = new AppSettingsReader();
        protected static bool init = false;
        protected static RenderTarget2D fontDrawSurface = null;
        float[] cloudRotation;
        float[] cloudRotationRate;

        string modelName;

        float productionRate, maxProduction, timeSinceLastFleet;
        public float Production
        {
            get { return productionRate; }
        }
        int defenseFleets;
        public int DefenseFleets
        {
            get { return defenseFleets; }
        }
        protected string name;
        public string Name
        {
            get { return name; }
        }
        List<float> dispatchRates = new List<float>(1);
        public List<float> DispatchRates
        {
            get { return dispatchRates; }
        }

        public void SetRate(float rate, int index)
        {
            lock(dispatchRates)
                dispatchRates[index] = rate;
        }
        List<int> dispatchHistory = new List<int>(100);
        int dispatchIndex = 1;
        protected Player owner;
        public Player Owner
        {
            get { return owner; }
            set
            {
                owner = value;
                for (int i = 1; i < outgoingDeployments.Count; i++)
                    RemoveRoute(i);
            }
        }

        private List<DeploymentRoute> outgoingDeployments;
        public List<DeploymentRoute> DeploymentRoutes
        {
            get { return outgoingDeployments; }
        }

        public DeploymentRoute GetRouteByIndex(int i)
        {
            if (outgoingDeployments.Count < i)
                return null;

            return outgoingDeployments[i];
        }

        public Planet()
            : base()
        {
            if (init == false)
                Planet.Initialize();
            if (r.Next(100) % 2 == 0)
                modelName = LoadModel("Planet");
            else
                modelName = LoadModel("GasPlanet");

            Model m = GetModel(modelName);
            cloudRotation = new float[m.Meshes.Count];
            cloudRotationRate = new float[m.Meshes.Count];

            for (int i = 0; i < m.Meshes.Count; i++)
            {
                cloudRotation[i] = (float)(r.NextDouble() * 360.0);
                cloudRotationRate[i] = (float)r.NextDouble();
            }
            worldPosition = new Vector3((float)(100 * r.NextDouble()), (float)(10 * r.NextDouble()),
                (float)(200 * r.NextDouble()));
            name = s_planetNames[r.Next(s_planetNames.Count)];
            maxProduction = (float)(120 * r.NextDouble());
            productionRate = (float)(r.NextDouble() * 0.5 * maxProduction + 1);
            timeSinceLastFleet = 0.0f;
            defenseFleets = 0;
            outgoingDeployments = new List<DeploymentRoute>(1);

            for (int i = 0; i < 100; i++)
                dispatchHistory.Add(-1);

            dispatchRates.Add(1.0f);    //defense fleet will always occupy spot 0, and start as 100%
            //of fleet deployments.
            dispatchHistory[0] = 0;

        }

        public Planet(Vector3 position)
            : this()
        {
            worldPosition = position;
        }

        #region Methods

        /// <summary>
        /// Preload planet names and available models.  Automatically called by the constructor
        /// if init == false, can be called before creating planet to make loading more convienient.
        /// </summary>
        public static void Initialize()
        {
            string planetNamesFile = (string)s_reader.GetValue("PlanetNamePath", typeof(string));
            StreamReader sr = new StreamReader(planetNamesFile);
            while (sr.EndOfStream == false)
            {
                string s = sr.ReadLine();
                s_planetNames.Add(s);
            }

        }


        public override void Update(GameTime time)
        {
            base.Update(time);
            if(productionRate < maxProduction)
                productionRate += ((float)time.ElapsedGameTime.TotalMilliseconds / 1000.0f) * 0.015f;
            if (timeSinceLastFleet > (60.0f / productionRate))
            {
                timeSinceLastFleet = 0;
                DispatchFleet();
            }
            else
                timeSinceLastFleet += (float)(time.ElapsedGameTime.TotalMilliseconds / 1000.0);

            for (int i = 0; i < cloudRotationRate.Length; i++)
                cloudRotation[i] = (cloudRotation[i] + cloudRotationRate[i] * ((float)time.ElapsedGameTime.Milliseconds / 100.0f));

            foreach (DeploymentRoute r in outgoingDeployments)
                r.Update(time);
        }

        /// <summary>
        /// Setting this as a homeworld ensure a predefined starting and maximum production.  This
        /// ensures each player starts on a equal footing. 
        /// </summary>
        public void MakeHomeworld()
        {
            productionRate = 20.0f;
            maxProduction = 120.0f;
        }

        /// <summary>
        /// Sends a newly created fleet to one of the available dispatchRoutes (this includes the
        /// defense fleets as an option).
        /// </summary>
        public void DispatchFleet()
        {
            if (outgoingDeployments.Count == 0)
            {
                defenseFleets++;
                return;
            }

            NormalizeRoutes();

            //count up the history information.
            int[] historyData = new int[dispatchRates.Count];
            //clear couters for each dispatch queue to 0
            for(int i =0; i < historyData.Length; i++)
                historyData[i] = 0;
            int count = 0;

            //
            lock (dispatchHistory)
            {
                foreach (int i in dispatchHistory)
                {
                    if (i != -1)
                    {
                        count++;
                        historyData[i]++;
                    }
                }
            }

            float[] actualRates = new float[dispatchRates.Count];
            for (int i = 0; i < dispatchRates.Count; i++)
            {
                actualRates[i] = (float)historyData[i] / (float)count;
            }

            float[] delta = new float[dispatchRates.Count];
            int maxIndex = 0;
            delta[0] = actualRates[0] - dispatchRates[0];
            for (int i = 1; i < dispatchRates.Count; i++)
            {
                delta[i] = actualRates[i] - dispatchRates[i];
                if (delta[i] < delta[maxIndex])
                    maxIndex = i;
            }

            if (maxIndex == 0)
                defenseFleets++;
            else
                outgoingDeployments[maxIndex - 1].ReceiveFleet(owner);

            dispatchHistory[dispatchIndex] = maxIndex;
            dispatchIndex = (dispatchIndex+1) % 100;
      
        }

        public override void Draw(GraphicsDevice drawDevice, Camera drawCamera)
        {

            Model m = GetModel(modelName);
            Matrix[] transforms = new Matrix[m.Bones.Count];
            m.CopyAbsoluteBoneTransformsTo(transforms);

            drawDevice.RenderState.DepthBufferEnable = true;
            drawDevice.RenderState.DepthBufferWriteEnable = true;

            ModelMesh ground = m.Meshes["Ground"];

            if (ground != null)
            {
                foreach (BasicEffect be in ground.Effects)
                {
                    be.World = transforms[ground.ParentBone.Index] * Matrix.CreateTranslation(worldPosition);
                    be.Projection = drawCamera.Projection;
                    be.View = drawCamera.View;
                    be.EnableDefaultLighting();


                }
                ground.Draw();

            }
            
            //support more than 1 cloud layer.
            drawDevice.RenderState.AlphaBlendEnable = true;
            drawDevice.RenderState.AlphaTestEnable = true;
            drawDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;
            drawDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            drawDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            drawDevice.RenderState.DepthBufferWriteEnable = false;

            

            ModelMesh clouds = m.Meshes["Clouds"];

                foreach (BasicEffect be in clouds.Effects)
                {
                    be.World = transforms[clouds.ParentBone.Index] *
                                Matrix.CreateRotationY(MathHelper.ToRadians(cloudRotation[m.Meshes.IndexOf(clouds)])) *
                                Matrix.CreateTranslation(worldPosition);
                    be.Projection = drawCamera.Projection;
                    be.View = drawCamera.View;


                }
            clouds.Draw();
            drawDevice.RenderState.DepthBufferWriteEnable = true;
            drawDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            
            Vector3 drawPosition = worldPosition + (clouds.BoundingSphere.Radius * Vector3.Cross(drawCamera.View.Forward, Vector3.Left));

            Vector3 unprojected = drawDevice.Viewport.Project(drawPosition,
                drawCamera.Projection, drawCamera.View,
                Matrix.CreateTranslation(Vector3.Zero));
            Vector3 maxContraints = drawDevice.Viewport.Project(new Vector3(drawPosition.X, drawPosition.Y - clouds.BoundingSphere.Radius * 2, drawPosition.Z),
                drawCamera.Projection, drawCamera.View, Matrix.CreateTranslation(Vector3.Zero));
            float height = Math.Abs(unprojected.Y - maxContraints.Y);

            //Draw the text info over the planet.
           
            s_sprite.Begin(SpriteBlendMode.AlphaBlend);
            Vector2 stringSize = s_font.MeasureString(string.Format("Defense Fleets: ", defenseFleets));
            float scale = height / stringSize.Y;
            if (scale > 2.0f)
                scale = 2.0f;
            if (scale < 0.33f)
                scale = 0.33f;

            Vector2 position = new Vector2(unprojected.X, unprojected.Y - 3 * stringSize.Y * scale);
            //s_sprite.DrawString(s_font, string.Format("{1}\nDefense Fleets: {0}", defenseFleets, name),
            //    new Vector2(unprojected.X - (0.5f * stringSize.X), unprojected.Y-stringSize.Y * 2), owner.PlayerColor);
            s_sprite.DrawString(s_font, string.Format("{0}\nDefense Fleets: {1}", name, defenseFleets),
                position, owner.PlayerColor,
                0.0f, Vector2.Zero, scale, SpriteEffects.None, 0.0f);
            s_sprite.End();
            


            for (int i = 0; i < outgoingDeployments.Count; i++)
                outgoingDeployments[i].Draw(drawDevice, drawCamera);

        }

        /// <summary>
        /// Checks if a screen coordinate falls within the planet's model on the screen.
        /// Assumes (0, 0) is the uppler left corner of the screen
        /// </summary>
        /// <param name="x">Horizontal position of the point</param>
        /// <param name="y">Vertical position of the point</param>
        /// <param name="view">View matrix being used to display the model</param>
        /// <param name="projection"Projection matrix used to display the model></param>
        /// <returns>True if the x, y, coordinate is in the model, otherwise false</returns>
        public override bool PointIsIn(float x, float y, Matrix view, Matrix projection)
        {
            Ray r = SpaceControl.Utility.PickingHelper.GetRay(x, y, view, projection, Matrix.CreateTranslation(worldPosition), s_graphics.Viewport);
            Model m = GetModel(modelName);

            foreach (ModelMesh mesh in m.Meshes)
            {
                Nullable<float> result = mesh.BoundingSphere.Intersects(r);
                if (result != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a new depolyment route to the planet's inventory.  
        /// It also resets the the dispatch history to avoid biasing towards the new 
        /// dispatch route.  
        /// </summary>
        /// <param name="destination">Planet the fleets will be sent to</param>
        public void CreateDeploymentRoute(Planet destination)
        {
            
            outgoingDeployments.Add(new DeploymentRoute(this, destination));
            
            dispatchRates.Add(1.0f / (float)(dispatchRates.Count + 1));
            //adding a new depolyment route means we need to reset the counters for all the 
            //fleet depolyment informaiton, otherwise the newest route has a substantial dissadvantage
            //and will get a large number of fleets sent to it before the history balances out.
            for (int i = 0; i < 100; i++)
                dispatchHistory[i] = -1;
            dispatchHistory[0] = 0;
            dispatchIndex = 1;
        }

        /// <summary>
        /// Creates a new depolyment route with a specific depolyment rate.  
        /// </summary>
        /// <param name="desitnation"></param>
        /// <param name="production"></param>
        public void CreateDeploymentRoute(Planet desitnation, float production)
        {
            CreateDeploymentRoute(desitnation);
            NormalizeRoutes();
            dispatchRates[dispatchRates.Count] = production;

        }

        /// <summary>
        /// A new fleet has arrived at this planet. This may come from the planet's own production
        /// or a depolyment route has deposited a fleet as this planet.
        /// 
        /// </summary>
        /// <param name="f">The fleet arrving</param>
        public void ReceiveFleet(Fleet f)
        {
            //if it's the same player's fleet, send it on its way.
            if (f.owner == this.owner)
                DispatchFleet();
            else
            {
                if (defenseFleets > 0)
                    defenseFleets--;
                else // this planet has been conqured.  set the new owner
                {
                    owner.RemovePlanet(this);
                    owner = f.owner;
                    foreach (DeploymentRoute r in outgoingDeployments)
                        r.UpdateColors(r.Source.Owner.PlayerColor, r.Desitnation.Owner.PlayerColor);
                    owner.AddPlanet(this);
                }
            }
        }

        public bool CanSeePlanet(Planet p)
        {
            foreach (DeploymentRoute route in outgoingDeployments)
                if (route.Desitnation == p)
                    return true;

            return false;
        }

        /// <summary>
        /// A callback for a GUI_Button on Click event.  This function reads the index in the
        /// Button's name (passed in the sender object parameter and removes the route of that
        /// index for the planet.
        /// </summary>
        /// <param name="sender">GUI_Button object that was clicked</param>
        public void CancelRoute(object sender)
        {
            int removeIndex = -1;
            //The object sender is a GUI_Button, the name contains the index of the route to cancel
            XNA_GUI.GUIElements.Button b = sender as XNA_GUI.GUIElements.Button;
            if (b != null)
            {
                int stringIndex = b.ControlName.IndexOfAny(new char[] { '0','1','2','3','4','5','6','7','8','9' });
                if (stringIndex != -1)
                {
                    removeIndex = Convert.ToInt32(b.ControlName.Substring(stringIndex, 1));
                    RemoveRoute(removeIndex);
                }
                Utility.SelectedManager.RemoveSingeRoute(removeIndex);
                ClearDispatchHistory();
            }
        }

        public void RemoveRoute(int index)
        {
            dispatchRates.RemoveAt(index);
            s_entityList.Remove(outgoingDeployments[index - 1]);
            outgoingDeployments.RemoveAt(index-1); //-1 used because the outgoingDepolyments list does
                                                   //not include an entry for disptachRates[0] which sends
                                                    //fleets to the planet's deffense count.
            NormalizeRoutes();
        }

        private void ClearDispatchHistory()
        {
            for (int i = 0; i < dispatchHistory.Count; i++)
                dispatchHistory[i] = -1;

            dispatchHistory[0] = 0;
            dispatchIndex = 1;

        }

        /// <summary>
        /// Normalizes values to DispatchRates[].  This is called after adding or removing a route
        /// or changing the depolyment rates on a route.
        /// </summary>
        public void NormalizeRoutes()
        {
            float totalCommitted = 0.0f;

            //Normalize the array.
            foreach (float f in dispatchRates)
                totalCommitted += f;

            if (totalCommitted == 0.0f)
            {
                dispatchRates[0] = 1.0f;
                return;
            }
            lock (dispatchRates)
            {
                for (int i = 0; i < dispatchRates.Count; i++)
                    dispatchRates[i] = dispatchRates[i] / totalCommitted;
            }
        }

        /// <summary>
        /// Gets the index of the route that has the specified planet as its destination.
        /// </summary>
        /// <param name="p">The Destination planet to search for</param>
        /// <returns>The index of the route from outGoingDepolyments, -1 if no
        /// route with that destination is found</returns>
        public int GetRouteByDestination(Planet p)
        {
            for (int i = 0; i < outgoingDeployments.Count; i++)
                if (outgoingDeployments[i].Desitnation == p)
                    return i;

            return -1;
        }
        #endregion Methods
    }
}
