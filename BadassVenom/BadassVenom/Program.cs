using System;
using System.Linq;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using System.Windows.Input;
using System.Collections.Generic;
using SharpDX;
using static AutoGhost.AutoGhost;
using NameValuePair = System.Collections.Generic.KeyValuePair<string, string>;

namespace BadassVenom
{
    internal class Program
    {
        private static uint[] PlagueWardDamage = { 15, 24, 33, 42 };
        private static Item Veil,
            Meka,
            Guardian,
            Arcane,
            LotusOrb,
            Medallion,
            SolarCrest,
            Pipe,
            CrimsonGuard,
            Stick,
            Wand,
            QuellingBlade,
            Eul,
            Tango,
            glimmer,
            ghost,
            invis;
        private static IEnumerable<Hero> enemies;
        private static IEnumerable<Hero> allies;
        private static IList<PlagueTarget> plagueTargets = new List<PlagueTarget>();
        private static PlagueTarget currentPlagueTarget;
        private const Key plagueKey = Key.E;
        private const Key chaseKey = Key.G;
        private static bool autoPlague;
        private static bool autoChase;
        private static bool hasJugger = false;
        private static uint plagueRange = 850;
        private static uint galeRange = 800;
        private static bool drawnedExtraRange = false;
        private static bool upgradedTalentRange = false;
        private static Hero me;
        private static ParticleEffect rangeDisplay;
        private static ParticleEffect daggerDisplay;
        private static void OnLoad(object sender, EventArgs e)
        {
            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Game_OnDraw;
        }

        static void Main(string[] args)
        {
            Events.OnLoad += OnLoad;
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

            if (rangeDisplay == null)
            {
                rangeDisplay = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                rangeDisplay.SetControlPoint(1, new Vector3(255, 255, 255));
                rangeDisplay.SetControlPoint(2, new Vector3(875, 255, 0));
            }
            if (daggerDisplay == null)
            {
                daggerDisplay = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                daggerDisplay.SetControlPoint(1, new Vector3(0, 255, 255));
                daggerDisplay.SetControlPoint(2, new Vector3(1200, 255, 0));
            }
            /*
            else if (upgradedTalentRange && !drawnedExtraRange)
            {
                rangeDisplay.SetControlPoint(2, new Vector3(875 + 150, 255, 0));
                rangeDisplay.Restart();
                drawnedExtraRange = true;

            }
            */

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

                if (Game.IsKeyDown(chaseKey))
                {
                    autoChase = false;
                }
                else
                {
                    autoChase = false;
                }

            }
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame)
                return;

            me = ObjectManager.LocalHero;
            if (me == null || me.ClassId != ClassId.CDOTA_Unit_Hero_Venomancer || !Utils.SleepCheck("rest_tick") || !me.IsVisible)
                return;

            Utils.Sleep(75, "rest_tick");
            Medallion = me.FindItem("item_medallion_of_courage");
            SolarCrest = me.FindItem("item_solar_crest");
            CrimsonGuard = me.FindItem("item_crimson_guard");
            Eul = me.FindItem("item_cyclone");
            Veil = me.FindItem("item_veil_of_discord");
            ghost = me.FindItem("item_ghost");
            glimmer = me.FindItem("item_glimmer_cape");
            invis = me.FindItem("item_invis_sword");
            if (Medallion == null)
            {
                Medallion = me.FindItem("item_solar_crest");
            }
            var plagueSpell = me.Spellbook.SpellE;
            var poisonSpell = me.Spellbook.SpellW;
            var plagueWardLevel = plagueSpell.Level - 1;
            var gale = me.Spellbook.Spell1;
            var nova  = me.Spellbook.SpellR;
            
            enemies = ObjectManager.GetEntitiesFast<Hero>().Where(hero => hero.IsAlive && !hero.IsIllusion && hero.IsVisible && hero.Team == me.GetEnemyTeam()).ToList();
            //var creeps = ObjectManager.GetEntitiesFast<Creep>().Where(creep => (creep.ClassId == ClassId.CDOTA_BaseNPC_Creep_Lane || creep.ClassId == ClassId.CDOTA_BaseNPC_Creep_Siege) && creep.IsAlive && creep.IsVisible && creep.IsSpawned).ToList();
            allies = ObjectManager.GetEntitiesFast<Hero>()
                                          .Where(
                                              x =>
                                                  x.Team == me.Team && !x.IsIllusion && x.IsAlive &&
                                                  me.Distance2D(x) <= 1500);
            var plaguewards = ObjectManager.GetEntitiesFast<Unit>().Where(unit => unit.ClassId == ClassId.CDOTA_BaseNPC_Venomancer_PlagueWard && unit.IsAlive && unit.IsVisible).ToList();

            if (hasJugger)
            {
                var healingWard =
                                        ObjectManager.GetEntitiesFast<Unit>()
                                            .Where(
                                                x =>
                                                    x.Team != me.Team && (x.ClassId == ClassId.CDOTA_BaseNPC_Additive))
                                            .FirstOrDefault();
                if (healingWard != null)
                {
                    foreach (var plagueward in plaguewards)
                    {
                        if (plagueward.Distance2D(healingWard) < 600 && Utils.SleepCheck("healing") && Utils.SleepCheck(plagueward.Handle.ToString()))
                        {
                            plagueward.Attack(healingWard);
                            Utils.Sleep(1200, plagueward.Handle.ToString());
                            Utils.Sleep(3000, "healing");
                        }
                    }
                }
            }
            AuxItems(me, enemies, allies);
            foreach (var enemy in enemies)
            {
                if (!Utils.SleepCheck("sting_" + enemy.ClassId))
                {
                    continue;
                }
                /*
                Console.WriteLine("OXE");
                var modifiers = enemy.Modifiers.ToList();
                foreach(var modifier in modifiers)
                {
                    Console.WriteLine(modifier.Name);
                }
                */
                if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Juggernaut)
                {
                    hasJugger = true;
                }
                if (enemy.Modifiers.FirstOrDefault(modifier => modifier.Name == "modifier_venomancer_poison_sting_ward") == null && enemy.Health > 0)
                {
                    foreach (var plagueward in plaguewards)
                    {
                        if (enemy.Distance2D(plagueward) < 600 && Utils.SleepCheck(plagueward.Handle.ToString()))
                        {
                            addPlagueTarget(plagueward, enemy);
                            Utils.Sleep(1000, plagueward.Handle.ToString());
                            Utils.Sleep(2000, "sting_"+enemy.ClassId);
                        }
                    }
                }
                var blink = enemy.FindItem("item_blink");
                if(blink != null && (blink.CanBeCasted() || blink.Cooldown < 1))
                {
                    foreach (var plagueward in plaguewards)
                    {
                        if (enemy.Distance2D(plagueward) < 600 && Utils.SleepCheck(plagueward.Handle.ToString()))
                        {
                            addPlagueTarget(plagueward, enemy);
                            Utils.Sleep(1000, plagueward.Handle.ToString());
                            Utils.Sleep(2000, "sting_" + enemy.ClassId);
                        }
                    }
                }

                if ((enemy.HasModifier("modifier_venomancer_venomous_gale") || enemy.HasModifier("modifier_venomancer_poison_nova")) && Veil.CanBeCasted() && Utils.SleepCheck("veil_venom") && me.IsVisible && enemy.Distance2D(me) < 900)
                {
                    Veil.UseAbility(enemy.Position);
                    Utils.Sleep(1000, "veil_venom");
                }
                if (enemy.Distance2D(me) <= 700 && !me.IsInvisible())
                {

                    if (blink != null && blink.Cooldown > 8 && (Eul == null || (Eul != null && Utils.SleepCheck("cyclone") && Eul.Cooldown < 20)) && (gale.CanBeCasted() || nova.CanBeCasted()))
                    {
                        if (gale.CanBeCasted() && Utils.SleepCheck("venom_gale"))
                        {
                            gale.UseAbility(enemy.Position);
                            Utils.Sleep(2000, "venom_gale");
                        }
                        if (Eul == null && nova.CanBeCasted() && Utils.SleepCheck("venom_nova"))
                        {
                            nova.UseAbility();
                            Utils.Sleep(2000, "venom_nova");
                        }
                        if (invis != null && invis.CanBeCasted() && Utils.SleepCheck(invis.Name))
                        {
                            invis.UseAbility();
                            Utils.Sleep(2000, invis.Name);
                        }
                        if (glimmer != null && glimmer.CanBeCasted() && Utils.SleepCheck(glimmer.Name))
                        {
                            glimmer.UseAbility(me);
                            Utils.Sleep(2000, glimmer.Name);
                        }
                    }
                }
            }

            var wards =
                                        ObjectManager.GetEntitiesFast<Unit>()
                                            .Where(
                                                x =>
                                                    x.Team != me.Team && (x.ClassId == ClassId.CDOTA_NPC_Observer_Ward || x.ClassId == ClassId.CDOTA_NPC_Observer_Ward_TrueSight))
                                            .ToList();
            if (wards.Any())
            {
                foreach (var ward in wards)
                {
                    foreach (var plagueward in plaguewards)
                    {
                        if (plagueward.Distance2D(ward) < 600 && Utils.SleepCheck(plagueward.Handle.ToString()))
                        {
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
                                    x.Team != me.Team && (x.ClassId == ClassId.CDOTA_NPC_TechiesMines) &&
                                    me.Distance2D(x) <= 1400)
                            .ToList();

            if (traps.Any())
            {
                foreach (var trap in traps)
                {
                    foreach (var plagueward in plaguewards)
                    {
                        if (plagueward.Distance2D(trap) < 600 && Utils.SleepCheck(plagueward.Handle.ToString()))
                        {
                            plagueward.Attack(trap);
                            Utils.Sleep(1000, plagueward.Handle.ToString());
                        }
                    }
                }

            }
            if (plagueSpell.Level > 0)
            {

                foreach (var plagueward in plaguewards)
                {
                    if (Utils.SleepCheck(plagueward.Handle.ToString()))
                    {
                        continue;
                    }
                    currentPlagueTarget = getPlagueTarget(plagueward);
                    if(currentPlagueTarget == null)
                    {
                        continue;

                    }
                    var targetEnemy = enemies.Where(x => x.ClassId == currentPlagueTarget.Value).FirstOrDefault();
                    if (targetEnemy != null && targetEnemy.Health > 120)
                    {
                        var blink = targetEnemy.FindItem("item_blink");
                        if (blink != null && blink.Cooldown < 2)
                        {
                            continue;
                        }
                        var newEnemy = targetEnemy;
                        var stingModifier = targetEnemy.FindModifier("modifier_venomancer_poison_sting_ward");
                        var stingTimer = stingModifier?.RemainingTime ?? 0;
                        if (stingTimer > 4)
                        {
                            foreach (var enemy in enemies)
                            {
                                if (enemy.Distance2D(plagueward) < 600 && shouldFocus(enemy) && enemy.ClassId != targetEnemy.ClassId && (enemy.Health < newEnemy.Health || !shouldFocus(newEnemy)))
                                {
                                    newEnemy = enemy;
                                }
                            }
                            if (newEnemy.ClassId != targetEnemy.ClassId)
                            {
                                addPlagueTarget(plagueward, newEnemy);
                                Utils.Sleep(1000, plagueward.Handle.ToString());
                            }

                        }

                    }                    
                }

                if ((plagueSpell.Level > 2 || poisonSpell.Level > 2) && canPlagueCast(plagueSpell))
                {
                    var targetEnemy = enemies.Where(x => x.Distance2D(me) <= plagueRange).MinOrDefault(x => x.Health);
                    
                    if(targetEnemy != null)
                    {
                        var enemyTower = ObjectManager.GetEntitiesFast<Unit>()
                            .Where(
                                x =>
                                    x.Team != me.Team && (x.ClassId == ClassId.CDOTA_BaseNPC_Tower) &&
                                    targetEnemy.Distance2D(x) <= 600 && me.Distance2D(x) > 400).FirstOrDefault();
                        if(enemyTower == null)
                        {
                            plagueSpell.UseAbility(targetEnemy.Position);
                            Utils.Sleep(2000, "auto_plague");
                        }
                    }else if (autoPlague)
                    {
                        if(Game.MousePosition.Distance2D(me) <= 850)
                        {
                            plagueSpell.UseAbility(Game.MousePosition);
                            Utils.Sleep(2000, "auto_plague");
                        }

                    }
                }

                foreach (var plagueward in plaguewards)
                {
                    if (!Utils.SleepCheck(plagueward.Handle.ToString())){
                        continue;
                    }
                    currentPlagueTarget = getPlagueTarget(plagueward);
                    if(currentPlagueTarget != null)
                    {
                        var targetEnemy = enemies.Where(x => x.ClassId == currentPlagueTarget.Value).FirstOrDefault();
                        if (targetEnemy != null && targetEnemy.Distance2D(plagueward) > 600)
                        {
                            plagueTargets.Remove(currentPlagueTarget);
                        }else if(targetEnemy != null && shouldFocus(targetEnemy))
                        {
                            continue;
                        }else
                        {
                            continue;
                        }
                    }
                    var lowestHealthEnemy = enemies.Where(x => x.Distance2D(plagueward) < 600 && shouldFocus(x)).MinOrDefault(x => x.Health);
                    if (lowestHealthEnemy != null)
                    {
                        addPlagueTarget(plagueward, lowestHealthEnemy);
                        Utils.Sleep(1000, plagueward.Handle.ToString());
                    }else if(currentPlagueTarget == null)
                    {

                        if (Utils.SleepCheck(plagueward.Handle.ToString()))
                        {
                            var lowestHealthCreep = ObjectManager.GetEntitiesFast<Creep>().Where(x => x.IsAlive && x.Team != me.Team && x.IsSpawned && x.IsVisible && x.Distance2D(plagueward) < 600).MinOrDefault(x => x.Health);
                            if (lowestHealthCreep != null)
                            {
                                plagueward.Attack(lowestHealthCreep);
                                Utils.Sleep(1000, plagueward.Handle.ToString());
                            }
                        }
                        /*
                        foreach (var creep in creeps)
                        {
                            if (creep.Team == me.GetEnemyTeam() && plagueWardLevel >= 0 && creep.Health > 0 && creep.Health < (PlagueWardDamage[plagueWardLevel] * (1 - creep.DamageResist) + 20))
                            {
                                if (creep.Distance2D(plagueward) < plagueward.AttackRange)
                                {
                                    plagueward.Attack(creep);
                                    Utils.Sleep(1000, plagueward.Handle.ToString());
                                }
                            }
                            else if (creep.Team == me.Team && creep.Health > (PlagueWardDamage[plagueWardLevel] * (1 - creep.DamageResist)) && creep.Health < (PlagueWardDamage[plagueWardLevel] * (1 - creep.DamageResist) + 88))
                            {
                                if (creep.Distance2D(plagueward) < plagueward.AttackRange && Utils.SleepCheck(plagueward.Handle.ToString()))
                                {
                                    plagueward.Attack(creep);
                                    Utils.Sleep(1000, plagueward.Handle.ToString());
                                }
                            }
                        }*/
                    }
                    
                }
                    
            }

            foreach (var ally in allies)
            {
                if(ally.ClassId != me.ClassId)
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
                        if (enemy.Distance2D(me) <= 950 && IsFacing(enemy, ally))
                        {
                            if (enemy.Distance2D(me) <= galeRange && gale.CanBeCasted() && Utils.SleepCheck("venom_gale"))
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

            useWand(me, enemies);
            ShopItems(me, false, enemies);
            var usedAux = useGhost(ghost, me, enemies);
            useGhost(glimmer, me, enemies, true, me);
            //useGhost(invis, me, enemies, true, null);
            if (autoChase)
            {

                var enemy = me.ClosestToMouseTarget(400);
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
                    
                }
                else
                {
                    me.Move(Game.MousePosition);
                    Utils.Sleep(100, "move");

                }
            }
            if (autoPlague)
            {
                if (false && canPlagueCast(plagueSpell))
                {
                    plagueSpell.UseAbility(Game.MousePosition);
                    Utils.Sleep(2000, "auto_plague");
                    me.Attack(Game.MousePosition);
                }
            }
            if (me.Level >= 15 && upgradedTalentRange == false)
            {
                var talentRange = me.Spellbook.Spells.First(x => x.Name == "special_bonus_cast_range_150");
                if (talentRange.Level > 0)
                {
                    upgradedTalentRange = true;
                    plagueRange += 150;
                }
            }
        }

        private static bool canPlagueCast(Ability spell)
        {
            return spell != null && spell.CanBeCasted() && Utils.SleepCheck("auto_plague") && spell.Level > 0 && !me.IsInvisible();
        }

        private static PlagueTarget getPlagueTarget(Unit plague)
        {
            PlagueTarget plagueTarget = plagueTargets.Where(x => x.Id == (plague.Handle)).FirstOrDefault();
            return plagueTarget;
        }

        private static void addPlagueTarget(Unit plague, Hero target)
        {
            plague.Attack(target);
            PlagueTarget plagueTarget = getPlagueTarget(plague);
            if(plagueTarget != null && plagueTarget.Value == target.ClassId)
            {
                return;
            }
            if (plagueTarget != null)
            {
                plagueTargets.Remove(plagueTarget);
            }
            plagueTargets.Add(new PlagueTarget(plague.Handle, target.ClassId));
        }

        private static bool shouldFocus(Hero enemy)
        {
            if(enemy.ClassId == ClassId.CDOTA_Unit_Hero_Shredder)
            {
                return false;
            }
            return true;
        }
    }
}
