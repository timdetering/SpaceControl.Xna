using System;
using System.Collections.Generic;
using System.Text;
using SpaceControl.Utility;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace SpaceControl.Entities
{
    public class Universe : BaseEntity
    {
        
        List<Player> players;
        public List<Player> Players
        {
            get { return players; }
        }

        public Player getPlayer(int index)
        {
            return players[index];
        }

        List<Planet> planets;
        public List<Planet> Planets
        {
            get { return planets; }
        }

        public List<Planet> PlanetsControledByPlaer(Player p)
        {
            List<Planet> results = new List<Planet>(1);
            foreach (Planet planet in planets)
            {
                if (planet.Owner == p)
                    results.Add(planet);
            }

            return results;
        }
        Color[] defaultPlayerColors = { Color.White, Color.Red, Color.Blue, Color.Yellow, Color.Green,
                                        Color.HotPink };

        float xmin, xmax, zmin, zmax;
        public float XMin
        {
            get { return xmin; }
        }

        public float XMax
        {
            get { return xmax; }
        }

        public float ZMin
        {
            get { return zmin; }
        }
        public float ZMax
        {
            get { return zmax; }
        }

        public Universe(int numberOfPlayers, int numberOfPlanets)
        {
            players = new List<Player>(numberOfPlayers);
            planets = new List<Planet>(numberOfPlanets);

            for (int i = 0; i < numberOfPlayers; i++)
            {
                if(i == 1)
                    players.Add(new Player(defaultPlayerColors[i], true));
                else
                 players.Add(new Player(defaultPlayerColors[i], false));
            }

            float xSize = 25.0f * numberOfPlanets;
            float zSize = 30.0f * numberOfPlanets;

            xmin = -(.5f * xSize);
            xmax = (.5f * xSize);
            zmin = -(.5f * zSize);
            zmax = (.5f * zSize);

            planets = Utility.InitialAssignments.CreatePlanets(numberOfPlanets, 25.0f, 
                new Microsoft.Xna.Framework.Vector3(25 * numberOfPlanets, 2 * numberOfPlanets, 
                30 * numberOfPlanets));

            Utility.InitialAssignments.AssignHomeworlds(planets, players);

        }

        public Player GetHumanPlayer()
        {
            foreach (Player p in players)
                if (p.IsHuman == true)
                    return p;
            return null;
        }

        public override void Update(Microsoft.Xna.Framework.GameTime time)
        {
            foreach (Planet p in planets)
                p.Update(time);
        }

        public override void Draw(GraphicsDevice drawDevice, Camera viewCamera)
        {
            foreach (Planet p in planets)
                p.Draw(drawDevice, viewCamera);
        }

        public Planet DoPick(float x, float y, Matrix view, Matrix projection)
        {
            foreach (Planet p in planets)
            {
                if (p.PointIsIn(x, y, view, projection) == true)
                    return p;
            }
            return null;
        }
    }
}
