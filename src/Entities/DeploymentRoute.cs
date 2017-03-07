using System;
using System.Collections.Generic;
using System.Text;
using SpaceControl.Utility;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace SpaceControl.Entities
{
    public class Fleet
    {
        public Player owner;
        public float position;
    }

    public class DeploymentRoute : BaseEntity
    {
        private Planet source, destination;
        private string fleetTexture = @".\Textures\FleetIcon.png";
        private const string c_RouteTexuteName = @".\Textures\DepolymentBase.png";
        private const string c_RouteEffect = "DepolymentEffect";
        private static Effect depolymentEffect = null;
        private static bool s_deviceLost = false;
        public Planet Desitnation
        {
            get { return destination; }
            set { destination = value; }
        }

        public Planet Source
        {
            get { return source; }
        }

        private string textureName;
        private Player owner;
        private const double c_movementRate = .15f;
        private VertexPositionColorTexture[] verts;
        private List<Fleet> fleetsInRoute;

        public DeploymentRoute(Planet source)
            :base()
        {
            this.source = source;
            owner = source.Owner;
            fleetsInRoute = new List<Fleet>(1);
            LoadTexture(fleetTexture);
            LoadTexture(c_RouteTexuteName);

            verts = new VertexPositionColorTexture[4];
            verts[0] = new VertexPositionColorTexture(
                source.Position, owner.PlayerColor, new Vector2(0, 0));
            verts[1] = new VertexPositionColorTexture(
                new Vector3(source.Position.X + 1, source.Position.Y + 1, source.Position.Z),
                owner.PlayerColor, new Vector2(1, 0));
        }

        public DeploymentRoute(Planet source, Planet destination)
        {
            this.source = source;
            this.destination = destination;
            fleetsInRoute = new List<Fleet>(1);
            owner = source.Owner;

            CreateVerts();
            LoadTexture(fleetTexture);
            LoadTexture(c_RouteTexuteName);

            if (depolymentEffect == null)
            {
                LoadEffect();
            }
        }

        protected void CreateVerts()
        {
            verts = new VertexPositionColorTexture[4];
            verts[0] = new VertexPositionColorTexture(
                source.Position, owner.PlayerColor, new Vector2(0, 0));
            verts[1] = new VertexPositionColorTexture(
                new Vector3(source.Position.X + 1, source.Position.Y + 1, source.Position.Z),
                owner.PlayerColor, new Vector2(0, 1));
            verts[2] = new VertexPositionColorTexture(
                destination.Position, destination.Owner.PlayerColor, new Vector2(10, 0));
            verts[3] = new VertexPositionColorTexture(
                new Vector3(destination.Position.X + 1, destination.Position.Y + 1, destination.Position.Z),
                destination.Owner.PlayerColor, new Vector2(10, 1));
        }
        protected void LoadEffect()
        {
            depolymentEffect = s_Content.Load<Effect>(c_RouteEffect);
            depolymentEffect.Parameters["xMainTexture"].SetValue(GetTexure(c_RouteTexuteName));

        }

        public override void Update(Microsoft.Xna.Framework.GameTime time)
        {
            Vector3 distance = source.Position - destination.Position;

            //Reposition fleets along the route
            for (int i = 0; i < fleetsInRoute.Count; i++)
            {
                fleetsInRoute[i].position += (float)(c_movementRate * 
                    (time.ElapsedGameTime.TotalMilliseconds) / ( 5.0f * distance.Length()));
                if (fleetsInRoute[i].position > 1.0f)
                    DispatchFleet(fleetsInRoute[i]);
            }


            //Scroll the texture for a simple animated effect.
            float textureOffet = -((float)time.ElapsedGameTime.Milliseconds / 1000.0f) % 1;
            verts[0].TextureCoordinate.X += textureOffet;
            bool wrap = false;
            if (verts[0].TextureCoordinate.X > 1.0f || verts[0].TextureCoordinate.X < -1.0f)
                wrap = true;

            verts[1].TextureCoordinate.X += textureOffet;
            verts[2].TextureCoordinate.X += textureOffet;
            verts[3].TextureCoordinate.X += textureOffet;

            //Don't allow the U,V coordinates to get to large.
            if (wrap)
            {
                verts[0].TextureCoordinate.X += 1.0f;
                verts[1].TextureCoordinate.X += 1.0f;
                verts[2].TextureCoordinate.X += 1.0f;
                verts[3].TextureCoordinate.X += 1.0f;
            }

        }

        
        public override void Draw(GraphicsDevice drawDevice, Camera viewCamera)
        {
            Texture2D t = (Texture2D)GetTexure(fleetTexture);
            Vector3 direction = destination.Position - source.Position;
            float distance = direction.Length();
            direction.Normalize();
            //Draw the background for the route
               
            depolymentEffect.Parameters["xView"].SetValue(viewCamera.View);
                
            depolymentEffect.Parameters["xProjection"].SetValue(viewCamera.Projection);
               
            depolymentEffect.Parameters["xWorld"].SetValue(Matrix.CreateTranslation(0, 0, 0));

            
            drawDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            drawDevice.SamplerStates[0].AddressV = TextureAddressMode.Border;
            drawDevice.RenderState.CullMode = CullMode.None;

            drawDevice.RenderState.AlphaTestEnable = true;
            drawDevice.RenderState.DepthBufferEnable = true;
            drawDevice.RenderState.DepthBufferWriteEnable = true;

            Vector3 cameraLook = new Vector3(viewCamera.View.M13, viewCamera.View.M23, viewCamera.View.M33);
            Vector3 perp = Vector3.Cross(cameraLook, direction);
            perp.Normalize();
            
            verts[1].Position = verts[0].Position + (3 * perp);
            verts[3].Position = verts[2].Position + (3 * perp);
            verts[2].Color = destination.Owner.PlayerColor;
            verts[3].Color = destination.Owner.PlayerColor;
            drawDevice.RenderState.CullMode = CullMode.None;

            depolymentEffect.Begin();
            foreach (EffectPass pass in depolymentEffect.CurrentTechnique.Passes)
            {
                pass.Begin();
                drawDevice.DrawUserPrimitives<VertexPositionColorTexture>(PrimitiveType.TriangleFan, verts, 0, 2);
                pass.End();
            }
            depolymentEffect.End();
            drawDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            //foreach fleet in the route right now, get its position, and draw a fleet icon at that 
            //position.
            drawDevice.RenderState.DepthBufferWriteEnable = true;
            drawDevice.RenderState.DepthBufferEnable = true;

            Vector2 direction2D = new Vector2(direction.X, direction.Z);
            direction2D.Normalize();
            float rotation = (direction2D.X < 0 ? 1 : -1) * (float)(Math.Acos(Vector2.Dot(direction2D, Vector2.UnitY)) + Math.PI);

            
            foreach (Fleet f in fleetsInRoute)
            {
                Vector3 fleetPos = source.Position + direction * (distance * f.position);
                s_sprite.Begin(SpriteBlendMode.AlphaBlend);
                Vector3 screenSpace = drawDevice.Viewport.Project(fleetPos, viewCamera.Projection, viewCamera.View, Matrix.CreateTranslation(0, 0, 0));
                s_sprite.Draw(t, new Vector2(screenSpace.X, screenSpace.Y), null, source.Owner.PlayerColor,
                    rotation, new Vector2(t.Width/2, t.Height/2), 1.0f, SpriteEffects.None, 0);
                s_sprite.End();
            }

        }

        public override void DeviceLostChild()
        {
            depolymentEffect.Dispose();
            s_deviceLost = true;
        }

        public override void DeviceResetChild(GraphicsDevice graphics, Microsoft.Xna.Framework.Content.ContentManager content)
        {
            if (s_deviceLost == true)
            {
                LoadEffect();
                CreateVerts();
                s_deviceLost = false;
            }
        }

        public void UpdateColors(Color sourceColor, Color destColor)
        {
            verts[0].Color = sourceColor;
            verts[1].Color = sourceColor;
            verts[2].Color = destColor;
            verts[3].Color = destColor;
        }

        /// <summary>
        /// Adds a new Fleet to this deployment route
        /// </summary>
        /// <param name="p">Player that owns the new fleet</param>
        public void ReceiveFleet(Player p)
        {
            Fleet f = new Fleet();
            f.position = 0.0f;
            f.owner = p;
            fleetsInRoute.Add(f);
        }

        /// <summary>
        /// A fleet has completed its journey, send it to the desitnation planet.
        /// </summary>
        /// <param name="f">The fleet to be removed from this route and sent to the planet</param>
        public void DispatchFleet(Fleet f)
        {
            fleetsInRoute.Remove(f);
            destination.ReceiveFleet(f);
        }
    }
}
