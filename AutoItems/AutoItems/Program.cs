using System;
using System.Linq;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using SharpDX;
using System.Windows.Input;
using System.Collections.Generic;
using static AutoGhost.AutoGhost;

namespace AutoItems
{
    class Program
    {
        private static Hero me;
        private static void OnLoad(object sender, EventArgs e)
        {
            Game.OnUpdate += Game_OnUpdate;
        }

        static void Main(string[] args)
        {
            Events.OnLoad += OnLoad;
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame || Game.IsWatchingGame)
            {
                return;
            }

            if (me == null)
            {
                me = ObjectManager.LocalHero;
            }
           
            var ghost = me.FindItem("item_ghost");

            var allies = ObjectManager.GetEntitiesFast<Hero>()
                           .Where(
                               x =>
                                   x.Team == me.Team && (x.ClassId != me.ClassId) && !x.IsIllusion && x.IsAlive &&
                                   me.Distance2D(x) <= 1050);

           var enemies =
                                    ObjectManager.GetEntitiesFast<Hero>()
                                        .Where(
                                            x =>
                                                x.Team != me.Team && !x.IsIllusion && x.IsAlive &&
                                                me.Distance2D(x) <= 1050)
                                        .ToList();
            var glimmer = me.FindItem("item_glimmer_cape");
            AuxItems(me, enemies, allies);
            useGhost(glimmer, me, enemies, true, me);

            useGhost(ghost, me, enemies);
            if(glimmer != null)
            {
                foreach (var ally in allies)
                {
                    if (ally.ClassId != me.ClassId && isInDanger2(ally))
                    {
                        useGhost(glimmer, me, enemies, true, ally);
                    }
                }
            }
        }

    }

}
