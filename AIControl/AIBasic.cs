using System;
using System.Collections.Generic;
using System.Text;
using SpaceControl.Utility;
using SpaceControl.Entities;
using System.Collections;

namespace SpaceControl.AIControl
{
    internal abstract class Goal : IDisposable
    {
        public enum TaskType : int { BuildDeffense = 0, Attack = 1 }
        public TaskType task;
        public int targetValue;
        public Planet target;
        public Planet source;

        public Goal(Planet source, Planet target, TaskType task, int targetValue)
        {
            this.source = source;
            this.target = target;
            this.targetValue = targetValue;
            this.task = task;
        }

        public abstract bool GoalMet();

        public virtual void Dispose()
        {
            return;
        }
    }

    internal class DeffenseGoal : Goal
    {
        protected int depolymentRouteID;

        public DeffenseGoal(Planet source, Planet desitnation, int defenseFleetGoal)
            : base(source, desitnation, TaskType.BuildDeffense, defenseFleetGoal)
        {
            if (source == desitnation)
            {
                source.DispatchRates[0] = 1.0f;
                source.NormalizeRoutes();
                depolymentRouteID = -1;
            }
            else
            {
                source.CreateDeploymentRoute(desitnation);
                depolymentRouteID = source.GetRouteByDestination(desitnation);
            }
                
        }

        public override bool GoalMet()
        {
            return (source.DefenseFleets >= targetValue);
        }

        public override void Dispose()
        {
            if (depolymentRouteID != -1)
                source.RemoveRoute(depolymentRouteID + 1);
        }

    }

    internal class AttackGoal : Goal
    {
        protected int deploymentRouteID;

        public AttackGoal(Planet source, Planet target)
            : base(source, target, TaskType.Attack, 0)
        {
            for (int i = 0; i < source.DispatchRates.Count; i++)
                source.DispatchRates[i] = 0;

            source.CreateDeploymentRoute(target);
            deploymentRouteID = source.GetRouteByDestination(target);



        }

        public override bool GoalMet()
        {
            return (source.Owner == target.Owner);
        }

        public override void Dispose()
        {
            source.RemoveRoute(deploymentRouteID +1);
        }
    }


    public class AIBasic
    {
        Player controledPlayer;
        Universe gameUniverse;
        List<Planet> enemyPlanets = new List<Planet>();
        Hashtable currentGoals = new Hashtable();

        public AIBasic(Player player, Universe universe)
        {
            controledPlayer = player;
            gameUniverse = universe;
            foreach (Planet p in universe.Planets)
            {
                if (p.Owner == player)
                {
                    currentGoals.Add(p, new List<Goal>());
                }
            }
        }

        public void Run()
        {
            
            while (controledPlayer.IsAlive == true)
            {
                List<Planet> ourPlanets = new List<Planet>(currentGoals.Keys.Count);

                //Get a current list of our planets, 
                lock (gameUniverse.Planets)
                {
                    for (int i = 0; i < gameUniverse.Planets.Count; i++ )
                    {
                        if (gameUniverse.Planets[i].Owner == controledPlayer)
                            ourPlanets.Add(gameUniverse.Planets[i]);
                        else
                            enemyPlanets.Add(gameUniverse.Planets[i]);
                    }
                }

                //compare it to the class list of controled planets, 
                //add any newly taken planets to our list, and initial a set of goals for them.
                foreach (Planet p in ourPlanets)
                {
                    if (currentGoals.ContainsKey(p) == false)
                    {
                        currentGoals.Add(p, new List<Goal>());
                    }
                }

                //loop through each of our planets, check the status of their goals.  Removing
                //completed goals as required.
                foreach (List<Goal> g in currentGoals.Values)
                {
                    for (int i = g.Count-1; i >= 0; i--)
                    {
                        if (g[i].GoalMet() == true)
                        {
                            g[i].Dispose();
                            g.RemoveAt(i);
                        }
                    }
                }

                //find any planets with available production and assign new goals for them.
                foreach (Planet p in currentGoals.Keys)
                {
                    if (p.DispatchRates[0] > 0 && ((List<Goal>)(currentGoals[p])).Count == 0)
                        AssignGoal(p);
                }

                enemyPlanets.Clear();
                //sleep for a while
            }
        }

        private void AssignGoal(Planet p)
        {
            if(p.DefenseFleets < p.Production * .5f)
            {
                ((List<Goal>)(currentGoals[p])).Add(new DeffenseGoal(p, p, (int)(p.Production * .6f)));
                return;
            }

            Planet bestFit = null;
            foreach(Planet enemy in enemyPlanets)
            {
                if(enemy.Production < p.Production)
                {
                    if (bestFit != null)
                    {
                        if (bestFit.Production / bestFit.DefenseFleets < enemy.Production / enemy.DefenseFleets)
                            bestFit = enemy;
                    }
                    else
                        bestFit = enemy;
                }
            }

            if (bestFit != null)
                ((List<Goal>)(currentGoals[p])).Add(new AttackGoal(p, bestFit));

            //we didn't need to defend the planet, and couldn't find a suitable planet to attack 
            //on our own.

        }
    }
}
