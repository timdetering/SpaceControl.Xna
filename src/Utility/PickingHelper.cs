using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceControl.Entities;

namespace SpaceControl.Utility
{
    public static class PickingHelper
    {
        public static Ray GetRay(float xPosition, float yPosition, Matrix view, Matrix projection, Matrix world, Viewport viewPort)
        {
            Vector3 nearSource = new Vector3(xPosition, yPosition, 0);
            Vector3 farSource = new Vector3(xPosition, yPosition, 1);


            Vector3 nearPoint = viewPort.Unproject(nearSource, projection, view, world);
            Vector3 farPoint = viewPort.Unproject(farSource, projection, view, world);

            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();
            Ray ray = new Ray(nearPoint, direction);
            return ray;
        }

        
    }

    public static class InitialAssignments
    {
        public static void AssignHomeworlds(List<Planet> planetList, List<Player> playerList)
        {
            foreach (Planet p in planetList)
                p.Owner = playerList[Player.GIA];
            Random r = new Random();
            List<int> used = new List<int>(playerList.Count);
            for (int i = 1; i < playerList.Count; i++)
            {
                int homeworldIndex = new int();
                do
                {
                    homeworldIndex = r.Next(planetList.Count);
                } while (used.Contains(homeworldIndex) == true);
                used.Add(homeworldIndex);

                playerList[i].AddPlanet(planetList[homeworldIndex]);
                planetList[homeworldIndex].MakeHomeworld();
            }

        }

        /// <summary>
        /// returns a list of appropriately spaced planets
        /// </summary>
        /// <param name="planets">Number of planets to create</param>
        /// <param name="minSpacing">The minimum distance between planets</param>
        /// <param name="constraints"></param>
        /// <returns></returns>
        public static List<Planet> CreatePlanets(int planets, float minSpacing, Vector3 constraints)
        {
            List<Planet> planetList = new List<Planet>();
            Random r = new Random();
            while (planetList.Count < planets)
            {
                Vector3 pos = new Vector3(constraints.X * (float)(r.NextDouble() - 0.5),
                    constraints.Y * (float)(r.NextDouble() - 0.5),
                    constraints.Z * (float)(r.NextDouble() - 0.5));

                foreach (Planet p in planetList)
                {
                    Vector3 distance = pos - p.Position;
                    if (distance.Length() < minSpacing)
                        continue;
                }

                Planet newPlanet = new Planet(pos);
                planetList.Add(newPlanet);
            }

            return planetList;
        }

    }
}
