using System;
using EloBuddy;

namespace HumanziedBaseUlt
{
    class Events
    {
        public class InvisibleEventArgs : EventArgs
        {
            public int StartTime { get; set; }
            public AIHeroClient sender { get; set; }
            /// <summary>
            /// per second without potions
            /// </summary>
            public float StdHealthRegen { get; set; }
        }

        public delegate void OnEnemyInvisibleH(InvisibleEventArgs args);
        public static event OnEnemyInvisibleH OnEnemyInvisible;
        protected virtual void FireOnEnemyInvisible(InvisibleEventArgs args)
        {
            if (OnEnemyInvisible != null) OnEnemyInvisible(args);
        }

        public delegate void OnEnemyVisibleH(AIHeroClient sender);
        public static event OnEnemyVisibleH OnEnemyVisible;
        protected virtual void FireOnEnemyVisible(AIHeroClient sender)
        {
            if (OnEnemyVisible != null) OnEnemyVisible(sender);
        }

        public class Messaging
        {
            private static string lastArgs;
            /// <summary>
            /// Prints ult delay in chat
            /// </summary>
            /// <param name="delay"></param>
            /// <param name="args">Champion name</param>
            public static void ProcessInfo(float delay, string args)
            {
                if (lastArgs == args)
                    return;
                Chat.Print(args + " ult delay: " + delay.ToString());
                lastArgs = args;
            } 
        }
    }
}
