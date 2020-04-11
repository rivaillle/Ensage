using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Linq;
using Ensage;
using Ensage.SDK.Helpers;
using Ensage.SDK.Input;
using Ensage.SDK.Inventory;
using Ensage.SDK.Menu;
using Ensage.SDK.Abilities;
using Ensage.SDK.Service;
using Ensage.SDK.Service.Metadata;
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
    class PlagueControl
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
        private static bool hasShaman = false;
        private static bool hasClock = false;
        private static uint plagueRange = 850;
        private static uint galeRange = 800;
        private static bool drawnedExtraRange = false;
        private static bool upgradedTalentRange = false;
        private static Hero me;
        private static ParticleEffect rangeDisplay;
        private static ParticleEffect daggerDisplay;
        private static float towerHealthPercent = 0.35f;
        private static int plagueAttackRange = 600;
        private static int plagueDistanceRange = 850;
        public static void OnUpdate()
        {
            me = ObjectManager.LocalHero;
            if (me == null || me.ClassId != ClassId.CDOTA_Unit_Hero_Venomancer || !Utils.SleepCheck("rest_tick") || !me.IsVisible)
                return;

            Utils.Sleep(40, "rest_tick");
            Medallion = me.FindItem("item_medallion_of_courage");
            SolarCrest = me.FindItem("item_solar_crest");
            CrimsonGuard = me.FindItem("item_crimson_guard");
            Eul = me.FindItem("item_cyclone");
            Veil = me.FindItem("item_veil_of_discord");
            ghost = me.FindItem("item_ghost");
            glimmer = me.FindItem("item_glimmer_cape");
            invis = me.FindItem("item_invis_sword");
            var sleepAutoPlague = false;

            if (Medallion == null)
            {
                Medallion = me.FindItem("item_solar_crest");
            }
            var plagueSpell = me.Spellbook.SpellE;
            var poisonSpell = me.Spellbook.SpellW;
            var plagueWardLevel = plagueSpell.Level - 1;
            var gale = me.Spellbook.Spell1;
            var nova = me.Spellbook.SpellR;

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
                                                    x.Team != me.Team && x.IsAlive && (x.ClassId == ClassId.CDOTA_BaseNPC_Additive))
                                            .FirstOrDefault();
                if (healingWard != null)
                {
                    if (healingWard.Distance2D(me) < 2000)
                    {
                        sleepAutoPlague = true;
                    }
                    foreach (var plagueward in plaguewards)
                    {
                        if (plagueward.Distance2D(healingWard) < 600 && Utils.SleepCheck("healing") && Utils.SleepCheck(plagueward.Handle.ToString()))
                        {
                            plagueward.Attack(healingWard);
                            Utils.Sleep(1200, plagueward.Handle.ToString());
                            Utils.Sleep(1000, "healing");
                        }
                    }
                    if (healingWard.Distance2D(me) <= 850 && canPlagueCast(plagueSpell))
                    {
                        plagueSpell.UseAbility(healingWard.Position);
                        Utils.Sleep(2000, "auto_plague");

                    }
                    else if (healingWard.Distance2D(me) <= plagueAttackRange + plagueDistanceRange && canPlagueCast(plagueSpell))
                    {
                        plagueSpell.UseAbility(getPlagueMaximumDistance(healingWard));
                        Utils.Sleep(2000, "auto_plague");
                    }
                }
            }
            AuxItems(me, enemies, allies);
            if (hasShaman)
            {
                var shamanWards =
                                      ObjectManager.GetEntitiesFast<Unit>()
                                          .Where(
                                              x =>
                                                  x.Team != me.Team && x.IsAlive && (x.ClassId == ClassId.CDOTA_BaseNPC_ShadowShaman_SerpentWard))
                                          .ToList();
                if (shamanWards.Any())
                {
                    foreach (var ward in shamanWards.OrderBy(x => x.Health))
                    {
                        foreach (var plagueward in plaguewards)
                        {
                            if (plagueward.Distance2D(ward) < 600 && Utils.SleepCheck(plagueward.Handle.ToString()))
                            {
                                //Console.WriteLine(DateTime.Now + "----" + Utils.SleepCheck(plagueward.Handle.ToString()));
                                addPlagueTarget(plagueward, ward);
                                Utils.Sleep(800, plagueward.Handle.ToString());
                            }
                        }
                        if (ward.Distance2D(me) <= 850 && canPlagueCast(plagueSpell))
                        {
                            plagueSpell.UseAbility(ward.Position);
                            Utils.Sleep(2000, "auto_plague");

                        }else if(ward.Distance2D(me) <= 850 + 600 && canPlagueCast(plagueSpell))
                        {
                            plagueSpell.UseAbility(getPlagueMaximumDistance(ward));
                            Utils.Sleep(2000, "auto_plague");
                        }
                    }
                    sleepAutoPlague = true;

                }
            }
            if (hasClock)
            {
                var clockGears =
                                      ObjectManager.GetEntitiesFast<Unit>()
                                          .Where(
                                              x =>
                                                  x.IsAlive && x.IsVisible && (x.ClassId == ClassId.CDOTA_BaseNPC_Additive))
                                          .ToList();
                if (clockGears.Any())
                {
                    foreach (var ward in clockGears)
                    {
                        foreach (var plagueward in plaguewards)
                        {
                            if (plagueward.Distance2D(ward) < 600 && Utils.SleepCheck(plagueward.Handle.ToString()))
                            {
                                //Console.WriteLine(DateTime.Now + "----" + Utils.SleepCheck(plagueward.Handle.ToString()));
                                plagueward.Attack(ward);
                                Utils.Sleep(800, plagueward.Handle.ToString());
                            }
                        }
                        if (ward.Distance2D(me) <= 850 && canPlagueCast(plagueSpell))
                        {
                            plagueSpell.UseAbility(ward.Position);
                            Utils.Sleep(2000, "auto_plague");

                        }
                        else if (ward.Distance2D(me) <= plagueAttackRange + plagueDistanceRange && canPlagueCast(plagueSpell))
                        {
                            plagueSpell.UseAbility(getPlagueMaximumDistance(ward));
                            Utils.Sleep(2000, "auto_plague");
                        }
                    }
                    sleepAutoPlague = true;

                }
            }
           
            foreach (var enemy in enemies)
            {
               
                /*
                
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
                else if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_ShadowShaman)
                {
                    hasShaman = true;
                }
                else if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Rattletrap)
                {
                    hasClock = true;
                }
                if (enemy.Modifiers.FirstOrDefault(modifier => modifier.Name == "modifier_venomancer_poison_sting_ward") == null && enemy.Health > 0)
                {
                    foreach (var plagueward in plaguewards)
                    {
                        if (enemy.Distance2D(plagueward) < 600 && Utils.SleepCheck(plagueward.Handle.ToString()))
                        {
                            //Console.WriteLine("attacking because no modifier");
                            addPlagueTarget(plagueward, enemy);
                            Utils.Sleep(1000, plagueward.Handle.ToString());
                        }
                    }
                }
                var blink = enemy.FindItem("item_blink");
                if (blink != null && (blink.CanBeCasted() || blink.Cooldown < 1))
                {
                    foreach (var plagueward in plaguewards)
                    {
                        if (enemy.Distance2D(plagueward) < 600 && Utils.SleepCheck(plagueward.Handle.ToString()))
                        {
                            addPlagueTarget(plagueward, enemy);
                            Utils.Sleep(1000, plagueward.Handle.ToString());
                            //Utils.Sleep(2000, "sting_" + enemy.ClassId);
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
                                                    x.Team != me.Team && x.IsAlive &&(x.ClassId == ClassId.CDOTA_NPC_Observer_Ward || x.ClassId == ClassId.CDOTA_NPC_Observer_Ward_TrueSight))
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
                    if(ward.Distance2D(me) <= 850 && canPlagueCast(plagueSpell))
                    {
                        plagueSpell.UseAbility(ward.Position);
                        Utils.Sleep(2000, "auto_plague");

                    }
                }

            }
            
            var traps =
                        ObjectManager.GetEntitiesFast<Unit>()
                            .Where(
                                x =>
                                    x.Team != me.Team && x.IsAlive && (x.ClassId == ClassId.CDOTA_NPC_TechiesMines) &&
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
                            Utils.Sleep(2000, plagueward.Handle.ToString());
                        }
                    }
                    if (trap.Distance2D(me) <= 850 && canPlagueCast(plagueSpell))
                    {
                        plagueSpell.UseAbility(trap.Position);
                        Utils.Sleep(2000, "auto_plague");
                    }
                }

            }

            var enemyTowers = ObjectManager.GetEntitiesFast<Unit>()
                           .Where(
                               x => 
                                   x.Team != me.Team && x.IsVisible && x.IsAlive && (x.ClassId == ClassId.CDOTA_BaseNPC_Tower)
                                   ).ToList();

            foreach (var enemyTower in enemyTowers)
            {
                foreach (var plagueward in plaguewards)
                {
                    if (Utils.SleepCheck(plagueward.Handle.ToString()) && plagueward.Distance2D(enemyTower) < 600 && enemyTower.Health < 601)
                    {
                        plagueward.Attack(enemyTower);
                        Utils.Sleep(2000, plagueward.Handle.ToString());
                    }
                }
            }
            if (plagueSpell.Level > 0)
            {

                foreach (var plagueTarget in plagueTargets)
                {
                   // Console.WriteLine("hoho" + plagueTarget.Id + " com value " + plagueTarget.Value);
                }

                foreach (var plagueward in plaguewards)
                {
                    if (Utils.SleepCheck(plagueward.Handle.ToString()))
                    {
                        continue;
                    }
                    currentPlagueTarget = getPlagueTarget(plagueward);
                    //Console.WriteLine("getting plague target" + currentPlagueTarget);
                    if (currentPlagueTarget == null)
                    {
                        continue;

                    }
                    var targetEnemy = enemies.Where(x => x.ClassId == currentPlagueTarget.Value).FirstOrDefault();
                    //Console.WriteLine("target enemy is "+ targetEnemy);
                    if (targetEnemy != null && targetEnemy.Health > 120)
                    {
                        var blink = targetEnemy.FindItem("item_blink");
                        if (blink != null && blink.Cooldown < 2)
                        {
                            continue;
                        }
                        var newEnemy = targetEnemy;
                        var stingModifier = UnitExtensions.HasModifier(targetEnemy, "modifier_venomancer_poison_sting_ward");
                        if(stingModifier == false)
                        {
                            //Console.WriteLine(DateTime.Now  + " KKKKK NUKL for " + targetEnemy.Name);
                        }
                        // var stingTimer = stingModifier?.RemainingTime ?? 0;
                        var stingTimer = 15;
                       // Console.WriteLine("stinger time" + stingModifier + "for enemy " + newEnemy.Name );
                        if (stingModifier)
                        {
                            foreach (var enemy in enemies)
                            {
                                if (enemy.Distance2D(plagueward) < 600 && shouldFocus(enemy) && enemy.ClassId != targetEnemy.ClassId && (enemy.Health < newEnemy.Health || !shouldFocus(newEnemy)))
                                {
                                   // Console.WriteLine("no if");
                                    newEnemy = enemy;
                                }
                            }
                            if (newEnemy.ClassId != targetEnemy.ClassId)
                            {
                                Console.WriteLine("attacking because health is lower");
                                addPlagueTarget(plagueward, newEnemy);
                                Utils.Sleep(1000, plagueward.Handle.ToString());
                            }

                        }

                    }
                }
                if ((plagueSpell.Level > 2 || poisonSpell.Level > 3) && canPlagueCast(plagueSpell) && !sleepAutoPlague)
                {

                    var targetEnemy = enemies.Where(x => x.Distance2D(me) <= plagueRange + plagueAttackRange).MinOrDefault(x => x.Health);

                    if (targetEnemy != null)
                    {
                        var enemyTower = ObjectManager.GetEntitiesFast<Unit>()
                            .Where(
                                x =>
                                    x.Team != me.Team && (x.ClassId == ClassId.CDOTA_BaseNPC_Tower) &&
                                    targetEnemy.Distance2D(x) <= 600 && me.Distance2D(x) > 400).FirstOrDefault();
                        if (enemyTower == null)
                        {
                            if (targetEnemy.Distance2D(me) < plagueRange)
                            {
                                plagueSpell.UseAbility(targetEnemy.Position);
                                Utils.Sleep(2000, "auto_plague");
                            }
                            else if (targetEnemy.Distance2D(me) <= plagueAttackRange + plagueDistanceRange)
                            {
                                plagueSpell.UseAbility(getPlagueMaximumDistance(targetEnemy));
                                Utils.Sleep(2000, "auto_plague");
                            }
                        }
                    }
                    else if (autoPlague)
                    {
                        if (Game.MousePosition.Distance2D(me) <= 850)
                        {
                            plagueSpell.UseAbility(Game.MousePosition);
                            Utils.Sleep(2000, "auto_plague");
                        }

                    }
                    else if(plagueSpell.Level == 4 || poisonSpell.Level == 4)
                    {
                        var lowestHealthCreep = ObjectManager.GetEntitiesFast<Unit>().Where(x => x.Team == me.GetEnemyTeam() && !x.IsInvul() && x.IsAlive && x.IsVisible && x.Distance2D(me) < 850).MinOrDefault(x => x.Health);
                        if (lowestHealthCreep != null)
                        {
                            plagueSpell.UseAbility(lowestHealthCreep.Position);
                            Utils.Sleep(2000, "auto_plague");
                        }
                    }
                }

                foreach (var plagueward in plaguewards)
                {
                    if (!Utils.SleepCheck(plagueward.Handle.ToString()))
                    {
                        continue;
                    }
                    currentPlagueTarget = getPlagueTarget(plagueward);

                    if (currentPlagueTarget != null)
                    {
                        if (currentPlagueTarget.Value == ClassId.CDOTA_BaseNPC_ShadowShaman_SerpentWard)
                        {
                            continue;
                        }
                        //Console.WriteLine("removing plague target");
                        var targetEnemy = enemies.Where(x => x.ClassId == currentPlagueTarget.Value).FirstOrDefault();
                        if (targetEnemy != null && targetEnemy.Distance2D(plagueward) > 600)
                        {
                            plagueTargets.Remove(currentPlagueTarget);
                        }
                        else if (targetEnemy != null && shouldFocus(targetEnemy))
                        {
                            continue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    var lowestHealthEnemy = enemies.Where(x => x.Distance2D(plagueward) < 600 && shouldFocus(x)).MinOrDefault(x => x.Health);
                    if (lowestHealthEnemy != null)
                    {
                        //Console.WriteLine("attacking because health is lower 2");
                        addPlagueTarget(plagueward, lowestHealthEnemy);
                        Utils.Sleep(1000, plagueward.Handle.ToString());
                    }
                    else if (currentPlagueTarget == null)
                    {

                        if (Utils.SleepCheck(plagueward.Handle.ToString()))
                        {
                            var lowestHealthCreep = ObjectManager.GetEntitiesFast<Creep>().Where(x => x.IsAlive && x.Team != me.Team && x.IsSpawned && x.IsVisible && x.Distance2D(plagueward) < 600).MinOrDefault(x => x.Health);
                            if (lowestHealthCreep != null)
                            {
                                //Console.WriteLine("found lowest health creep " + lowestHealthCreep);
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
                if (ally.ClassId != me.ClassId)
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

        private static void addPlagueTarget(Unit plague, Unit target)
        {
            plague.Attack(target);
            PlagueTarget plagueTarget = getPlagueTarget(plague);
            if (plagueTarget != null && plagueTarget.Value == target.ClassId)
            {
                return;
            }
            if (plagueTarget != null)
            {
                plagueTargets.Remove(plagueTarget);
            }
            plagueTargets.Add(new PlagueTarget(plague.Handle, target.ClassId));
        }

        private static Vector3 getPlagueMaximumDistance(Unit target)
        {
            //get the positions of our transforms
            Vector3 pos1 = me.Position;
            Vector3 pos2 = target.Position;

            //get the direction between the two transforms -->
            Vector3 dir = (pos2 - pos1);
            dir.Normalize();
            Vector3 test = pos1 + 850 * dir;
            return test;            
        }

        private static bool shouldFocus(Hero enemy)
        {
            if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Shredder)
            {
                return false;
            }
            return true;
        }
    }


}
