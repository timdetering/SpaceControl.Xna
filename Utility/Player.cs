using System;
using System.Collections.Generic;
using System.Text;
using SpaceControl.Entities;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceControl.Utility
{
    
    public class Player
    {
        //Uncontroled planets will be set to GIA.
        public const int GIA = 0;

        protected List<Planet> playerControledPlanets = new List<Planet>(1);
        protected Color displayColor;
        protected bool isHuman;
        public bool IsHuman
        {
            get { return isHuman; }
        }

        public Player(Planet homeworld, Color playerColor, bool isHuman)
        {
            playerControledPlanets.Add(homeworld);
            displayColor = playerColor;
            this.isHuman = isHuman;
        }

        public Player(Color playerColor, bool isHuman)
        {
            displayColor = playerColor;
            this.isHuman = isHuman;
        }

        /// <summary>
        /// Determines if a player has control over, or has a depolyment route to Planet p
        /// This limits how much information about a planet a player can see.  
        /// </summary>
        /// <param name="p">Planet in question</param>
        /// <returns>True if the player owns or has a depolyment route to p, otherwise false</returns>
        public bool CanSeePlanet(Planet p)
        {
            if (playerControledPlanets.Contains(p))
                return true;

            foreach (Planet myPlanet in playerControledPlanets)
            {
                if (myPlanet.CanSeePlanet(p) == true)
                    return true;
            }

            return false;
        }
        
        /// <summary>
        /// XNA Color assigned to the player, this is used to color code objects controled
        /// by this player for easy determination.
        /// </summary>
        public Color PlayerColor
        {
            get { return displayColor; }
        }

        /// <summary>
        /// Checks if this player controls a planet.  Required for AI searching.
        /// </summary>
        /// <param name="p">Planet in questions</param>
        /// <returns>True if p is in playerControledPlanets, otherwisefalse</returns>
        public bool ControlsPlanet(Planet p)
        {
            return playerControledPlanets.Contains(p);
        }

        public void AddPlanet(Planet p)
        {
            playerControledPlanets.Add(p);
            p.Owner = this;
        }

        public void RemovePlanet(Planet p)
        {
            playerControledPlanets.Remove(p);
        }

        public List<Planet> ControledPlanets
        {
            get { return playerControledPlanets; }
        }


        /// <summary>
        /// Returns the first planet in the list, this will most often be the 
        /// initial homeworld set by AssignHomeworlds, but if that planet is lost,
        /// it will be the earliest captured world still under our control.
        /// </summary>
        public Planet HomeWorld
        {
            get
            {
                if (playerControledPlanets != null)
                    return playerControledPlanets[0];
                return null;
            }
        }

        /// <summary>
        /// if there are no planets in playerControledPlanets, returns false, otherwise true.
        /// </summary>
        public bool IsAlive
        {
            get { return (playerControledPlanets.Count != 0); }
        }
    }
}
