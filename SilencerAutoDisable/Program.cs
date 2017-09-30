using System;
using System.Collections.Generic;
using System.Linq;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using SharpDX;
using Ensage.Common.Menu;

namespace ChallengeAccepted
{
    class SilenceAccepted
    {
        private static readonly Menu Menu = new Menu("DisableAccepted", "DisableAccepted", true, "npc_dota_hero_silencer", true);
        private static Hero me, target;
        private static Ability globalSilence;

        static void Main(string[] args)
        {
            Menu.AddToMainMenu();
            PrintSuccess(">Silence Accepted");
            Game.OnWndProc += Working;
        }
        public static void Working(EventArgs args)
        {
            if (!Game.IsInGame || Game.IsPaused || Game.IsWatchingGame)
                return;
            me = ObjectMgr.LocalHero;
            if (me == null || me.ClassId != ClassId.CDOTA_Unit_Hero_Silencer)
                return;
            globalSilence = me.Spellbook.SpellR;
            if (me.IsAlive && me.CanCast() && Utils.SleepCheck("global_silence"))
            {
                foreach (var v in Ensage.Common.Objects.Heroes.GetByTeam(me.GetEnemyTeam()))
                {
                    if(v.ClassId == ClassId.CDOTA_Unit_Hero_Enigma && v.IsAlive && v.IsChanneling())
                    {
                        globalSilence.UseAbility();
                        Utils.Sleep(300, "global_silence");
                    }
                }
            }


        }
        private static void PrintSuccess(string text, params object[] arguments)
        {
            PrintEncolored(text, ConsoleColor.Green, arguments);
        }
        private static void PrintEncolored(string text, ConsoleColor color, params object[] arguments)
        {
            var clr = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text, arguments);
            Console.ForegroundColor = clr;
        }
    }       
}