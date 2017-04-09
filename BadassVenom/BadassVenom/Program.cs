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
        private static IList<PlagueTarget> plagueTargets = new List<PlagueTarget>();
        private static PlagueTarget currentPlagueTarget;
        private const Key plagueKey = Key.T;
        private const Key chaseKey = Key.G;
        private static bool autoPlague;
        private static bool autoChase;
        private static bool hasJugger = false;
        private static uint plagueRange = 850;
        private static uint galeRange = 850;
        private static bool drawnedExtraRange = false;
        private static bool upgradedTalentRange = false;
        private static Hero me;
        private static ParticleEffect rangeDisplay;

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
            else if (upgradedTalentRange && !drawnedExtraRange)
            {
                rangeDisplay.SetControlPoint(2, new Vector3(875 + 150, 255, 0));
                rangeDisplay.Restart();
                drawnedExtraRange = true;

            }

        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (!Game.IsChatOpen)
            {
                if (Game.IsKeyDown(plagueKey))
                {
                    autoPlague = false;
                }
                else
                {
                    autoPlague = false;
                }

                if (Game.IsKeyDown(chaseKey))
                {
                    autoChase = true;
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

            if (hasJugger)
            {
                var healingWard =
                                        ObjectManager.GetEntitiesFast<Unit>()
                                            .Where(
                                                x =>
                                                    x.Team != me.Team && (x.ClassID == ClassID.CDOTA_BaseNPC_Additive) &&
                                                    me.Distance2D(x) <= 1400)
                                            .FirstOrDefault();
                if (healingWard != null)
                {
                    foreach (var plagueward in plaguewards)
                    {
                        if (plagueward.Distance2D(healingWard) < plagueward.AttackRange && Utils.SleepCheck("healing") && Utils.SleepCheck(plagueward.Handle.ToString()))
                        {
                            plagueward.Attack(healingWard);
                            Utils.Sleep(1000, plagueward.Handle.ToString());
                            Utils.Sleep(5000, "healing");
                        }
                    }
                }
            }

            foreach (var enemy in enemies)
            {
                if(enemy.ClassID == ClassID.CDOTA_Unit_Hero_Juggernaut)
                {
                    hasJugger = true;
                }
                if (enemy.Modifiers.FirstOrDefault(modifier => modifier.Name == "modifier_venomancer_poison_sting_ward") == null && enemy.Health > 0)
                {
                    foreach (var plagueward in plaguewards)
                    {
                        if (enemy.Distance2D(plagueward) < plagueward.AttackRange && Utils.SleepCheck(plagueward.Handle.ToString()) && Utils.SleepCheck("sting_" + enemy.ClassID))
                        {
                            addPlagueTarget(plagueward, enemy);
                            Utils.Sleep(1000, plagueward.Handle.ToString());
                            Utils.Sleep(2000, "sting_"+enemy.ClassID);
                        }
                    }
                }
                if (enemy.HasModifier("modifier_venomancer_venomous_gale") && Veil.CanBeCasted() && Utils.SleepCheck("veil_venom") && me.IsVisible && enemy.Distance2D(me) < 900)
                {
                    Veil.UseAbility(enemy.Position);
                    Utils.Sleep(1000, "veil_venom");
                }
                if (enemy.Distance2D(me) <= 600 && !me.IsInvisible())
                {
                    var blink = enemy.FindItem("item_blink");
                    if (Eul != null && Utils.SleepCheck("cyclone") && Eul.CanBeCasted())
                    {
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
                    if (blink != null && gale.CanBeCasted() && Utils.SleepCheck("venom_gale") && blink.Cooldown > 8)
                    {
                        gale.UseAbility(enemy.Position);
                        Utils.Sleep(2000, "venom_gale");
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

                foreach (var plagueward in plaguewards)
                {
                    currentPlagueTarget = getPlagueTarget(plagueward);
                    if(currentPlagueTarget == null)
                    {
                        continue;
                    }
                    var targetEnemy = enemies.Where(x => x.ClassID == currentPlagueTarget.Value).FirstOrDefault();
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
                                if (enemy.Distance2D(plagueward) < plagueward.AttackRange && enemy.ClassID != targetEnemy.ClassID && enemy.Health < newEnemy.Health)
                                {
                                    newEnemy = enemy;
                                }
                            }
                            if (newEnemy.ClassID != targetEnemy.ClassID)
                            {
                                addPlagueTarget(plagueward, newEnemy);
                            }

                        }

                    }
                }

                if (plagueSpell.Level > 3 && canPlagueCast(plagueSpell))
                {
                    var targetEnemy = enemies.Where(x => x.Distance2D(me) <= plagueRange).MinOrDefault(x => x.Health);
                    
                    if(targetEnemy != null)
                    {
                        var enemyTower = ObjectManager.GetEntitiesFast<Unit>()
                            .Where(
                                x =>
                                    x.Team != me.Team && (x.ClassID == ClassID.CDOTA_BaseNPC_Tower) &&
                                    targetEnemy.Distance2D(x) <= 600 && me.Distance2D(x) > 400).FirstOrDefault();
                        if(enemyTower == null)
                        {
                            plagueSpell.UseAbility(targetEnemy.Position);
                            Utils.Sleep(2000, "auto_plague");
                        }
                    }
                }                

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

            AuxItems(me);
            var usedAux = useGhost(ghost, me, enemies);
            useGhost(glimmer, me, enemies, true, me);

            if (autoChase)
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
                    neutral = ObjectManager.GetEntitiesFast<Creep>().Where(x => x.IsAlive && x.IsSpawned && x.IsVisible && x.Distance2D(me) <= 800).MinOrDefault(x => x.Health);
                    if (neutral != null)
                    {
                        Orbwalking.Orbwalk(neutral);
                        if (canPlagueCast(plagueSpell) && !me.IsInvisible())
                        {
                            plagueSpell.UseAbility(neutral.Position);
                            Utils.Sleep(2000, "auto_plague");
                        }
                        foreach(var plague in plaguewards)
                        {
                            if(plague.NetworkActivity == NetworkActivity.Idle && plague.Distance2D(neutral) < plague.AttackRange)
                            {
                                plague.Attack(neutral);
                            }
                        }
                    }
                    else if (Utils.SleepCheck("move"))
                    {
                        me.Move(Game.MousePosition);
                        Utils.Sleep(100, "move");

                    }
                }
                var position = Game.MousePosition;
                if (plagueSpell != null && plagueSpell.CanBeCasted() && Utils.SleepCheck("auto_plague") && !me.IsInvisible() && enemy == null && neutral == null)
                {
                    plagueSpell.UseAbility(Game.MousePosition);
                    Utils.Sleep(2000, "auto_plague");
                }
            }
            if (autoPlague)
            {
                Console.WriteLine("here");
                if (canPlagueCast(plagueSpell))
                {
                    plagueSpell.UseAbility(Game.MousePosition);
                    Utils.Sleep(2000, "auto_plague");
                    me.Attack(Game.MousePosition);
                }
            }
            if (me.Level >= 15 && upgradedTalentRange == false)
            {
                var talentRange = me.Spellbook.Spells.First(x => x.Name == "special_bonus_cast_range_150");
                Console.WriteLine("found talent:" + talentRange.Level);
                if (talentRange.Level > 0)
                {
                    upgradedTalentRange = true;
                    plagueRange += 150;
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
            if(plagueTarget != null && plagueTarget.Value == target.ClassID)
            {
                return;
            }
            if (plagueTarget != null)
            {
                plagueTargets.Remove(plagueTarget);
            }
            plagueTargets.Add(new PlagueTarget(plague.Handle, target.ClassID));
        }
    }
}
