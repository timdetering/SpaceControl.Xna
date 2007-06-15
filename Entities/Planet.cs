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
            if (r.Next(100)%2 == 0)
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

            Vector3 unprojected = drawDevice.Viewport.Project(new Vector3(worldPosition.X, worldPosition.Y + 3.0f * m.Meshes[0].BoundingSphere.Radius, worldPosition.Z),
                drawCamera.Projection, drawCamera.View,
                Matrix.CreateTranslation(Vector3.Zero));
            //Draw the text info over the planet.
            s_sprite.Begin(SpriteBlendMode.AlphaBlend);
            Vector2 stringSize = s_font.MeasureString(string.Format("Defense Fleets: ", defenseFleets));
            s_sprite.DrawString(s_font, string.Format("Defense Fleets: {0}", defenseFleets),
                new Vector2(unprojected.X - (0.5f * stringSize.X), unprojected.Y), owner.PlayerColor);
            s_sprite.End();

            lock (outgoingDeployments)
            {
                for (int i = 0; i < outgoingDeployments.Count; i++)
                    outgoingDeployments[i].Draw(drawDevice, drawCamera);
            }

        }

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

        public void CreateDeploymentRoute(Planet destination)
        {
            lock (outgoingDeployments)
            {
                outgoingDeployments.Add(new DeploymentRoute(this, destination));
            }
            lock (dispatchRates)
            {
                dispatchRates.Add(1.0f / (float)(dispatchRates.Count + 1));
            }
            //adding a new depolyment route means we need to reset the counters for all the 
            //fleet depolyment informaiton, otherwise the newest route has a substantial dissadvantage
            for (int i = 0; i < 100; i++)
                dispatchHistory[i] = -1;
            dispatchHistory[0] = 0;
            dispatchIndex = 1;
        }

        public void CreateDeploymentRoute(Planet desitnation, float production)
        {
            CreateDeploymentRoute(desitnation);
            NormalizeRoutes();
            dispatchRates[dispatchRates.Count] = production;

        }

        public void ReceiveFleet(Fleet f)
        {
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
