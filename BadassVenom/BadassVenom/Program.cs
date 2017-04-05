using System;
using System.Linq;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using System.Windows.Input;
using System.Collections.Generic;
using static AutoGhost.AutoGhost;

namespace BadassVenom
{
    class Program
    {
        private static uint[] PlagueWardDamage = { 15, 24, 33, 42 };
        private static Item Veil,
            Meka,
            Guardian,
            Arcane,
            LotusOrb,
            Medallion,
            SolarCrest,
            GlimmerCape,
            Pipe,
            CrimsonGuard,
            Stick,
            Wand,
            QuellingBlade,
            Eul,
            Tango,
            glimmer,
            ghost;
        private static IEnumerable<Hero> enemies;
        private static IEnumerable<Hero> allies;
        private const Key plagueKey = Key.G;
        private static bool autoPlague;
        private static Hero me;

        private static void Main()
        {
            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Game_OnDraw;
        }

        private static void Game_OnDraw(EventArgs args)
        {
            if (me == null)
            {
                return;
            }


            if (!Game.IsInGame || Game.IsPaused || Game.IsWatchingGame)
            {
                return;
            }
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (!Game.IsChatOpen)
            {
                if (Game.IsKeyDown(plagueKey))
                {
                    autoPlague = true;
                }
                else
                {
                    autoPlague = false;
                }

            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame)
                return;
            Utils.Sleep(125, "VenomancerWardControl");

            me = ObjectMgr.LocalHero;
            if (me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Venomancer)
                return;

            Medallion = me.FindItem("item_medallion_of_courage");
            SolarCrest = me.FindItem("item_solar_crest");
            GlimmerCape = me.FindItem("item_glimmer_cape");
            CrimsonGuard = me.FindItem("item_crimson_guard");
            Stick = me.FindItem("item_magic_stick");
            Wand = me.FindItem("item_magic_wand");
            Eul = me.FindItem("item_cyclone");
            Veil = me.FindItem("item_veil_of_discord");
            ghost = me.FindItem("item_ghost");
            glimmer = me.FindItem("item_glimmer_cape");
            if (Medallion == null)
            {
                Medallion = me.FindItem("item_solar_crest");
            }
            var plagueSpell = me.Spellbook.SpellE;
            var plagueWardLevel = plagueSpell.Level - 1;
            var gale = me.Spellbook.Spell1;

            enemies = ObjectManager.GetEntitiesFast<Hero>().Where(hero => hero.IsAlive && !hero.IsIllusion && hero.IsVisible && hero.Team == me.GetEnemyTeam()).ToList();
            var creeps = ObjectManager.GetEntitiesFast<Creep>().Where(creep => (creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane || creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege) && creep.IsAlive && creep.IsVisible && creep.IsSpawned).ToList();
            allies = ObjectManager.GetEntitiesFast<Hero>()
                                          .Where(
                                              x =>
                                                  x.Team == me.Team && !x.IsIllusion && x.IsAlive &&
                                                  me.Distance2D(x) <= 1500);
            var plaguewards = ObjectManager.GetEntitiesFast<Unit>().Where(unit => unit.ClassID == ClassID.CDOTA_BaseNPC_Venomancer_PlagueWard && unit.IsAlive && unit.IsVisible).ToList();

            foreach (var enemy in enemies)
            {
                if (enemy.Modifiers.FirstOrDefault(modifier => modifier.Name == "modifier_venomancer_poison_sting_ward") == null && enemy.Health > 0)
                {
                    foreach (var plagueward in plaguewards)
                    {
                        if (enemy.Distance2D(plagueward) < plagueward.AttackRange && Utils.SleepCheck(plagueward.Handle.ToString()))
                        {
                            plagueward.Attack(enemy);
                            Utils.Sleep(1000, plagueward.Handle.ToString());
                        }
                    }
                }
                if (enemy.HasModifier("modifier_venomancer_venomous_gale") && Veil.CanBeCasted() && Utils.SleepCheck("veil_venom") && me.IsVisible && enemy.Distance2D(me) < 900)
                {
                    Veil.UseAbility(enemy.Position);
                    Utils.Sleep(1000, "veil_venom");
                }
                if (Eul != null && Utils.SleepCheck("cyclone") && enemy.Distance2D(me) <= 600 && Eul.CanBeCasted() && !me.IsInvisible())
                {
                    var blink = enemy.FindItem("item_blink");
                    if (blink != null && blink.Cooldown > 8)
                    {
                        Eul.UseAbility(enemy);
                        Utils.Sleep(1000, "cyclone");
                    }
                    else if (enemy.IsChanneling())
                    {
                        Eul.UseAbility(enemy);
                        Utils.Sleep(1000, "cyclone");
                        if (!enemies.Any() || !plaguewards.Any() || !(plagueWardLevel > 0))
                            return;
                    }
                }
            }

            var wards =
                                        ObjectManager.GetEntitiesFast<Unit>()
                                            .Where(
                                                x =>
                                                    x.Team != me.Team && (x.ClassID == ClassID.CDOTA_NPC_Observer_Ward || x.ClassID == ClassID.CDOTA_NPC_Observer_Ward_TrueSight) &&
                                                    me.Distance2D(x) <= 1400)
                                            .ToList();

            if (wards.Any())
            {
                foreach (var ward in wards)
                {
                    foreach (var plagueward in plaguewards)
                    {
                        if (plagueward.Distance2D(ward) < plagueward.AttackRange && Utils.SleepCheck(plagueward.Handle.ToString()))
                        {
                            Console.WriteLine("attacking ward");
                            plagueward.Attack(ward);
                            Utils.Sleep(5000, plagueward.Handle.ToString());
                        }
                    }
                }

            }

            var traps =
                        ObjectManager.GetEntitiesFast<Unit>()
                            .Where(
                                x =>
                                    x.Team != me.Team && (x.ClassID == ClassID.CDOTA_NPC_TechiesMines) &&
                                    me.Distance2D(x) <= 1400)
                            .ToList();

            if (traps.Any())
            {
                foreach (var trap in traps)
                {
                    foreach (var plagueward in plaguewards)
                    {
                        if (plagueward.Distance2D(trap) < plagueward.AttackRange && Utils.SleepCheck(plagueward.Handle.ToString()))
                        {
                            plagueward.Attack(trap);
                            Utils.Sleep(1000, plagueward.Handle.ToString());
                        }
                    }
                }

            }
            if (plagueSpell.Level > 0)
            {
                foreach (var creep in creeps)
                {
                    if (creep.Team == me.GetEnemyTeam() && plagueWardLevel >= 0 && creep.Health > 0 && creep.Health < (PlagueWardDamage[plagueWardLevel] * (1 - creep.DamageResist) + 20))
                        foreach (var plagueward in plaguewards)
                        {
                            if (creep.Distance2D(plagueward) < plagueward.AttackRange && Utils.SleepCheck(plagueward.Handle.ToString()))
                            {
                                plagueward.Attack(creep);
                                Utils.Sleep(1000, plagueward.Handle.ToString());
                            }
                        }
                    else if (creep.Team == me.Team && creep.Health > (PlagueWardDamage[plagueWardLevel] * (1 - creep.DamageResist)) && creep.Health < (PlagueWardDamage[plagueWardLevel] * (1 - creep.DamageResist) + 88))
                        foreach (var plagueward in plaguewards)
                        {
                            if (creep.Distance2D(plagueward) < plagueward.AttackRange && Utils.SleepCheck(plagueward.Handle.ToString()))
                            {
                                plagueward.Attack(creep);
                                Utils.Sleep(1000, plagueward.Handle.ToString());
                            }
                        }
                }
            }

            foreach (var ally in allies)
            {
                if(ally.ClassID != me.ClassID)
                {
                    useGhost(glimmer, me, enemies, true, ally);
                }

                var isNuking = ally.Spellbook.Spells.Any(x => x.IsInAbilityPhase);
                if (isNuking && Veil != null && Veil.CanBeCasted() && Utils.SleepCheck("veil_venom"))
                {
                    foreach (var enemy in enemies)
                    {
                        if (IsFacing(ally, enemy) && enemy.Distance2D(me) < 900 && me.IsVisible)
                        {
                            Veil.UseAbility(enemy.Position);
                            Utils.Sleep(1000, "veil_venom");
                        }
                    }
                }
                if (isInDanger2(ally) && ally.Distance2D(me) <= 900 && !me.IsInvisible())
                {
                    foreach (var enemy in enemies)
                    {
                        if (enemy.Distance2D(me) <= 900 && IsFacing(enemy, ally))
                        {
                            if (enemy.Distance2D(me) <= 800 && gale.CanBeCasted() && Utils.SleepCheck("venom_gale"))
                            {
                                gale.UseAbility(enemy.Position);
                                Utils.Sleep(2000, "venom_gale");
                            }

                            if (canPlagueCast(plagueSpell))
                            {
                                plagueSpell.UseAbility(enemy.Position);
                                Utils.Sleep(2000, "auto_plague");
                            }

                        }

                    }
                    if (canPlagueCast(plagueSpell))
                    {

                        plagueSpell.UseAbility(ally.Position);
                        Utils.Sleep(2000, "auto_plague");
                    }
                }
            }

            AuxItems(me);
            var usedAux = useGhost(ghost, me, enemies);
            useGhost(glimmer, me, enemies, true, me);

            if (autoPlague)
            {


                Creep neutral = null;
                var enemy = me.ClosestToMouseTarget(200);
                if (enemy == null)
                {
                    uint currentHealth = 9999;
                    foreach (var pe in enemies)
                    {
                        if (pe.Health < currentHealth && me.Distance2D(pe) <= 475)
                        {
                            currentHealth = pe.Health;
                            enemy = pe;
                        }
                    }
                }
                if (enemy != null)
                {
                    Orbwalking.Orbwalk(enemy, 0, 0, false, false);
                    if (canPlagueCast(plagueSpell) && !me.IsInvisible())
                    {
                        plagueSpell.UseAbility(enemy.Position);
                        Utils.Sleep(2000, "auto_plague");
                    }
                }
                else
                {
                    neutral = ObjectManager.GetEntitiesFast<Creep>().Where(x => x.IsAlive && x.IsSpawned && x.IsVisible && x.Distance2D(me) <= 450).MinOrDefault(x => x.Health);
                    if (neutral != null)
                    {
                        Orbwalking.Orbwalk(neutral);
                        if (canPlagueCast(plagueSpell) && !me.IsInvisible())
                        {
                            plagueSpell.UseAbility(neutral.Position);
                            Utils.Sleep(2000, "auto_plague");
                        }
                    }
                    else if (creeps != null)
                    {
                        Orbwalking.Orbwalk(creeps.First());
                    }
                    else
                    {
                        Orbwalking.Orbwalk(null);

                    }
                }
                var position = Game.MousePosition;
                if (plagueSpell != null && plagueSpell.CanBeCasted() && Utils.SleepCheck("auto_plague") && !me.IsInvisible() && enemy != null && neutral != null)
                {
                    plagueSpell.UseAbility(Game.MousePosition);
                    Utils.Sleep(2000, "auto_plague");
                }
            }
        }

        private static void AuxItems(Hero self)
        {
            if (!self.IsInvisible() && self.CanCast())
            {

                if (Medallion != null && Utils.SleepCheck("solar") && Medallion.CanBeCasted())
                {
                    foreach (var enemy in enemies)
                    {
                        if (isCarry(enemy) && enemy.IsAttacking() || (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Juggernaut && enemy.Spellbook.Spells.Any(x => x.IsInAbilityPhase && x.AbilityType == AbilityType.Ultimate)))
                        {
                            foreach (var ally in allies)
                            {
                                if (IsFacing(enemy, ally))
                                {
                                    Medallion.UseAbility(ally);
                                    Utils.Sleep(1000, "solar");
                                }


                            }
                        }
                    }
                    if (Utils.SleepCheck("solar") && Medallion.CanBeCasted())
                    {
                        foreach (var ally in allies)
                        {
                            if (isCarry(ally) && ally.IsAttacking())
                            {
                                foreach (var enemy in enemies)
                                {
                                    if (IsFacing(ally, enemy))
                                    {
                                        Medallion.UseAbility(enemy);
                                        Utils.Sleep(1000, "solar");
                                    }
                                }
                            }
                        }
                    }
                }

            }


        }

        private static bool isNuker(Hero enemy)
        {
            if (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Morphling || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Necrolyte || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Invoker
                || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Batrider || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Juggernaut || enemy.ClassID == ClassID.CDOTA_Unit_Hero_StormSpirit)
            {
                return true;
            }
            return false;
        }

        private static bool canPlagueCast(Ability spell)
        {
            return spell != null && spell.CanBeCasted() && Utils.SleepCheck("auto_plague") && spell.Level > 0;
        }
    }
}
