using System;

namespace SpaceControl
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (SpaceControlMain game = new SpaceControlMain())
            {
                game.Run();
            }
        }
    }
}

