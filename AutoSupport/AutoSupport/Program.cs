﻿using System;
using System.Linq;
using System.Windows.Input;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using SharpDX;

namespace SupportSharp
{
    internal class Program
    {
        private const Key toggleKey = Key.T;
        private const Key orbwalkKey = Key.Space;
        private const Key saveSelfKey = Key.Y;
        private static Hero me;
        private static Ability misticAbility;
        private static double[] misticDamagePerLevel = new double[] { 100, 150, 200, 250 };
        private static Entity fountain;
        private static bool loaded;

        private static Item Urn,
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
            Tango;

        private static Hero needMana;
        private static Hero needMeka;
        private static Hero target;
        private static bool supportActive;
        private static bool includeSaveSelf;
        private static bool shouldCastLotusOrb;
        private static bool shouldCastGlimmerCape;

        private static void Main(string[] args)
        {
            Game.OnUpdate += Game_OnUpdate;
            Orbwalking.Load();
            Drawing.OnDraw += Drawing_OnDraw;

            /*Items*/
            Urn = null;
            Meka = null;
            Guardian = null;
            Arcane = null;
            LotusOrb = null;
            Medallion = null;
            SolarCrest = null;
            GlimmerCape = null;
            Pipe = null;
            CrimsonGuard = null;
            QuellingBlade = null;
            Tango = null;
            loaded = false;
            supportActive = true;
            includeSaveSelf = false;
            shouldCastLotusOrb = false;
            shouldCastGlimmerCape = false;

            misticAbility = null;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (loaded)
            {
                var mode = supportActive ? "ON" : "OFF";
                var orbwalkMode = Game.IsKeyDown(orbwalkKey) ? "ON" : "OFF";
                var includeSelfMode = includeSaveSelf ? "ON" : "OFF";
                Drawing.DrawText("Auto Support is: " + mode + ". Hotkey (Toggle): " + toggleKey + "",
                    new Vector2(Drawing.Width * 5 / 100, Drawing.Height * 4 / 100), new Vector2(24), Color.LightBlue, FontFlags.DropShadow);
                
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!loaded)
            {
                me = ObjectMgr.LocalHero;


                if (!Game.IsInGame || Game.IsWatchingGame || me == null || Game.IsChatOpen)
                {
                    return;
                }
                loaded = true;
            }

            if (me == null || !me.IsValid)
            {
                loaded = false;
                me = ObjectMgr.LocalHero;
                supportActive = false;
                includeSaveSelf = false;
                shouldCastLotusOrb = false;
                shouldCastGlimmerCape = false;
                return;
            }

            if (Game.IsPaused)
            {
                return;
            }

            Urn = me.FindItem("item_urn_of_shadows");
            Meka = me.FindItem("item_mekansm");
            Guardian = me.FindItem("item_guardian_greaves");
            Arcane = null; // me.FindItem("item_arcane_boots");
            LotusOrb = me.FindItem("item_lotus_orb");
            Medallion = me.FindItem("item_medallion_of_courage");
            SolarCrest = me.FindItem("item_solar_crest");
            GlimmerCape = me.FindItem("item_glimmer_cape");
            Pipe = null;//me.FindItem("item_pipe");
            CrimsonGuard = me.FindItem("item_crimson_guard");
            misticAbility = me.Spellbook.SpellQ;
            Stick = me.FindItem("item_magic_stick");
            Wand = me.FindItem("item_magic_wand");
            QuellingBlade = me.FindItem("item_quelling_blade");
            needMana = null;
            needMeka = null;
            shouldCastLotusOrb = false;
            shouldCastGlimmerCape = false;

            if (!Game.IsChatOpen)
            {
                if (Game.IsKeyDown(toggleKey) && Utils.SleepCheck("togglingoption"))
                {
                    if (!supportActive)
                    {
                        supportActive = true;
                    }
                    else
                    {
                        supportActive = false;
                    }
                    Utils.Sleep(100 + Game.Ping, "togglingoption");
                }

                if (Game.IsKeyDown(saveSelfKey) && Utils.SleepCheck("togglingoption"))
                {
                    if (!includeSaveSelf)
                    {
                        includeSaveSelf = true;
                    }
                    else
                    {
                        includeSaveSelf = false;
                    }
                    Utils.Sleep(100 + Game.Ping, "togglingoption");
                }
            }

            if (supportActive && me.IsAlive)
            {
                if(IsInDanger(me) && me.Health <= me.MaximumHealth * 0.4 )
                {
                    if(Stick != null && Utils.SleepCheck("Stick") && Stick.CurrentCharges > 0 && Stick.Cooldown > 0)
                    {
                        Stick.UseAbility();
                        Utils.Sleep(100 + Game.Ping, "Stick");
                    }
                    if (Wand != null && Utils.SleepCheck("Wand") && Wand.CurrentCharges > 0)
                    {
                        Wand.UseAbility();
                        Utils.Sleep(100 + Game.Ping, "Wand");
                    }

                }
                double misticDamage = 0;
                if (misticAbility.Level > 0)
                {
                   misticDamage = misticDamagePerLevel[misticAbility.Level - 1];
                }
                var extraRange = 0;
                if (me.HasModifier("modifier_item_aether_lens"))
                {
                    extraRange = 200;
                }

                var myEnemyList =
                                    ObjectManager.GetEntitiesFast<Hero>()
                                        .Where(
                                            x =>
                                                x.Team != me.Team && !x.IsIllusion && x.IsAlive &&
                                                me.Distance2D(x) <= 800 + extraRange)
                                        .ToList();

                if (myEnemyList.Any())
                {
                    foreach (var enemy in myEnemyList)
                    {
                        
                        var enemyResistence = enemy.MagicDamageResist;
                        double totalDamage = misticDamage - misticDamage * enemyResistence;
                        if (me.HasModifier("modifier_item_aether_lens"))
                        {
                            totalDamage += totalDamage * 0.05;
                        }
                        totalDamage += totalDamage * (me.Intelligence % 16) / 100;
                        if (totalDamage >= (enemy.Health + enemy.HealthRegeneration * 1))
                        {
                            CastHeal(misticAbility, enemy, false);
                        }
                        //Console.WriteLine("total damage is:" + totalDamage);

                    }
                }

                var allies =
                    ObjectManager.GetEntitiesFast<Hero>()
                        .Where(
                            ally =>
                                ally.Team == me.Team && me.ClassID != ally.ClassID && ally.IsAlive && !ally.IsIllusion && me.Distance2D(ally) <= 1500)
                        .ToList();
                fountain =
                    ObjectManager.GetEntitiesFast<Entity>()
                        .First(entity => entity.ClassID == ClassID.CDOTA_Unit_Fountain && entity.Team == me.Team);


                if (allies.Any())
                {
                    foreach (var ally in allies)
                    {
                        if (!ally.IsIllusion() && ally.IsAlive && ally.Health > 0 && me.IsAlive && !me.IsChanneling() &&
                            me.Distance2D(fountain) > 1000 &&
                            !me.IsInvisible())
                        {
                            if ((ally.MaximumHealth - ally.Health) > (450 + ally.HealthRegeneration * 10) &&
                                me.Distance2D(ally) <= 1000 &&
                                (me.Mana >= 225 || Guardian != null))
                            {
                                if (needMeka == null || (needMeka != null && me.Distance2D(needMeka) <= 750))
                                {
                                    needMeka = ally;
                                }
                            }

                            var enemyTowers =
                                ObjectManager.GetEntities<Entity>()
                                    .Any(
                                        x =>
                                            x.ClassID == ClassID.CDOTA_BaseNPC_Tower && x.Team != me.Team &&
                                            x.IsAlive && ally.Distance2D(x) <= 750);

                            if (me.CanUseItems())
                            {
                                if (Urn != null && Urn.CanBeCasted() && Urn.CurrentCharges > 0 &&
                                    !ally.Modifiers.Any(x => x.Name == "modifier_item_urn_heal") && !enemyTowers)
                                {
                                    if (me.Distance2D(ally) <= 950 && !IsInDanger(ally) && Utils.SleepCheck("Urn") &&
                                        ally.Health <= (ally.MaximumHealth * 0.7))
                                    {
                                        Urn.UseAbility(ally);
                                        Utils.Sleep(100 + Game.Ping, "Urn");
                                    }
                                    if (ally.Modifiers.Any(x => x.Name == "modifier_wisp_tether") &&
                                        (ally.MaximumHealth - ally.Health) >= 600 && Utils.SleepCheck("Urn"))
                                    {
                                        Urn.UseAbility(me);
                                        Utils.Sleep(100 + Game.Ping, "Urn");
                                    }
                                }

                                if (Arcane != null && Arcane.Cooldown == 0)
                                {
                                    if ((ally.MaximumMana - ally.Mana) >= 135 && me.Distance2D(ally) < 2000 &&
                                        me.Mana >= 35)
                                    {
                                        if (needMana == null || (needMana != null && me.Distance2D(needMana) <= 600))
                                        {
                                            needMana = ally;
                                        }
                                    }
                                }

                                /*Pipe and Crimson Guard*/
                                if (((Pipe != null && Pipe.CanBeCasted()) ||
                                     (CrimsonGuard != null && CrimsonGuard.CanBeCasted())) && me.CanUseItems())
                                {
                                    var enemiesInRadius =
                                        ObjectManager.GetEntities<Hero>()
                                            .Where(
                                                x =>
                                                    x.Team != me.Team && x.IsAlive && me.Distance2D(x) <= 1500 &&
                                                    !x.IsIllusion).ToList();
                                    var alliesInRadius =
                                        ObjectManager.GetEntities<Hero>()
                                            .Where(
                                                x =>
                                                    x.Team == me.Team && x.IsAlive && me.Distance2D(x) <= 900 &&
                                                    !x.IsIllusion).ToList();

                                    if (enemiesInRadius.Any() && alliesInRadius.Any())
                                    {
                                        if (enemiesInRadius.Count >= 2 && alliesInRadius.Count >= 2)
                                        {
                                            if (Pipe != null && Pipe.CanBeCasted() && Utils.SleepCheck("Pipe"))
                                            {
                                                Pipe.UseAbility();
                                                Utils.Sleep(100 + Game.Ping, "Pipe");
                                            }

                                            if (CrimsonGuard != null && CrimsonGuard.CanBeCasted() &&
                                                Utils.SleepCheck("CrimsonGuard"))
                                            {
                                                CrimsonGuard.UseAbility();
                                                Utils.Sleep(100 + Game.Ping, "CrimsonGuard");
                                            }
                                        }
                                    }
                                }

                                var enemyList =
                                    ObjectManager.GetEntities<Hero>()
                                        .Where(
                                            x =>
                                                x.Team != me.Team && !x.IsIllusion && x.IsAlive && x.CanCast() &&
                                                ally.Distance2D(x) <= 1000)
                                        .ToList();

                                if (enemyList.Any())
                                {
                                    foreach (var enemy in enemyList)
                                    {
                                        var targettedSpell =
                                            enemy.Spellbook.Spells.Any(
                                                x =>
                                                    x.TargetTeamType == TargetTeamType.Enemy &&
                                                    x.AbilityState == AbilityState.Ready &&
                                                    ally.Distance2D(enemy) <= x.CastRange + 50 &&
                                                    x.AbilityBehavior == AbilityBehavior.UnitTarget);

                                        var targettedItem =
                                            enemy.Inventory.Items.Any(
                                                x =>
                                                    x.TargetTeamType == TargetTeamType.Enemy &&
                                                    x.AbilityState == AbilityState.Ready &&
                                                    x.AbilityBehavior == AbilityBehavior.UnitTarget &&
                                                    ally.Distance2D(enemy) <= x.CastRange + 50);

                                        var enemySkill =
                                            enemy.Spellbook.Spells.Any(
                                                x =>
                                                    x.DamageType == DamageType.Magical &&
                                                    x.TargetTeamType == TargetTeamType.Enemy &&
                                                    x.AbilityState == AbilityState.Ready &&
                                                    ally.Distance2D(enemy) <= x.CastRange + 50);

                                        if (enemySkill)
                                        {
                                            shouldCastGlimmerCape = true;
                                        }

                                        if (targettedSpell || targettedItem)
                                        {
                                            shouldCastLotusOrb = true;
                                        }
                                        
                                        if (enemy.IsLinkensProtected())
                                        {
                                            CastHeal(misticAbility, enemy, false);
                                        }
                                       
                                    }
                                }

                                if (LotusOrb != null && LotusOrb.Cooldown == 0 && Utils.SleepCheck("LotusOrb") &&
                                    me.Distance2D(ally) <= LotusOrb.CastRange + 50 &&
                                    (shouldCastLotusOrb || IsInDanger(ally)))
                                {
                                    LotusOrb.UseAbility(ally);
                                    Utils.Sleep(100 + Game.Ping, "LotusOrb");
                                }

                                if (Medallion != null && Medallion.Cooldown == 0 &&
                                    me.Distance2D(ally) <= Medallion.CastRange + 50 && Utils.SleepCheck("Medallion") &&
                                    ally != me && IsInDanger(ally))
                                {
                                    Medallion.UseAbility(ally);
                                    Utils.Sleep(100 + Game.Ping, "Medallion");
                                }

                                if (SolarCrest != null && SolarCrest.Cooldown == 0 &&
                                    me.Distance2D(ally) <= SolarCrest.CastRange + 50 && Utils.SleepCheck("SolarCrest") &&
                                    ally != me && IsInDanger(ally))
                                {
                                    SolarCrest.UseAbility(ally);
                                    Utils.Sleep(100 + Game.Ping, "SolarCrest");
                                }

                                if (GlimmerCape != null && GlimmerCape.Cooldown == 0 &&
                                    me.Distance2D(ally) <= GlimmerCape.CastRange + 50 && Utils.SleepCheck("GlimmerCape") &&
                                    (shouldCastGlimmerCape || IsInDanger(ally)))
                                {
                                    GlimmerCape.UseAbility(ally);
                                    Utils.Sleep(100 + Game.Ping, "GlimmerCape");
                                }
                            }
                        }
                    }
                }

                if (needMeka != null &&
                    ((Guardian != null && Guardian.CanBeCasted()) || (Meka != null && Meka.CanBeCasted())) &&
                    me.Distance2D(needMeka) <= 750)
                {
                    if (Meka != null)
                    {
                        Meka.UseAbility();
                    }
                    else
                    {
                        Guardian.UseAbility();
                    }
                }
                if (needMana != null && Arcane != null && Arcane.CanBeCasted() && me.Distance2D(needMana) <= 600)
                {
                    Arcane.UseAbility();
                }

                uint addedRange = 0;
                if (Support(me.ClassID))
                {
                    switch (me.ClassID)
                    {
                        case ClassID.CDOTA_Unit_Hero_Abaddon:
                            if (me.HasModifier("modifier_item_aether_lens"))
                            {
                                addedRange += 200;
                            }
                            Save(me, me.Spellbook.SpellW, 1000, me.Spellbook.SpellW.CastRange + addedRange);
                            Heal(me, me.Spellbook.SpellQ, new float[] { 100, 150, 200, 250 },
                                800 + addedRange,
                                1, false);
                            break;                        
                    }
                }
            }
            if (QuellingBlade != null && QuellingBlade.Cooldown == 0) {
                var wards =
                                        ObjectManager.GetEntitiesFast<Unit>()
                                            .Where(
                                                x =>
                                                    x.Team != me.Team && (x.ClassID == ClassID.CDOTA_NPC_Observer_Ward || x.ClassID == ClassID.CDOTA_NPC_Observer_Ward_TrueSight) &&
                                                    me.Distance2D(x) <= 475)
                                            .ToList();
                if (wards.Any() && Utils.SleepCheck("deward"))
                {
                    QuellingBlade.UseAbility(wards[0]);
                    Utils.Sleep(1000, "deward");
                }
            }
            if (Game.IsKeyDown(orbwalkKey))
            {
                if (target != null && (!target.IsValid || !target.IsVisible || !target.IsAlive || target.Health <= 0))
                {
                    target = null;
                }
                var canCancel = Orbwalking.CanCancelAnimation();
                if (canCancel)
                {
                    if (target != null && !target.IsVisible && !Orbwalking.AttackOnCooldown(target))
                    {
                        target = me.ClosestToMouseTarget();
                    }
                    else if (target == null || !Orbwalking.AttackOnCooldown(target))
                    {
                        var bestAa = me.BestAATarget();
                        if (bestAa != null)
                        {
                            target = me.BestAATarget();
                        }
                    }
                }

                Orbwalking.Orbwalk(target, Game.Ping, attackmodifiers: true);
            }
        }

        private static void Save(Hero self, Ability saveSpell, float castTime = 800, uint castRange = 0,
            int targettingType = 1)
        {
            if (saveSpell != null && saveSpell.CanBeCasted())
            {
                if (self.IsAlive && !self.IsChanneling() &&
                    (!self.IsInvisible() || !me.Modifiers.Any(x => x.Name == "modifier_treant_natures_guise")))
                {
                    var alliesExcludeMe =
                        ObjectMgr.GetEntities<Hero>()
                            .Where(
                                x =>
                                    x.Team == self.Team && self.Distance2D(x) <= castRange && IsInDanger2(x) && !x.IsIllusion && x.IsAlive)
                            .ToList();
                    var alliesIncludeMe = alliesExcludeMe;
                    /*
                    var alliesIncludeMe =
                        ObjectMgr.GetEntities<Hero>()
                            .Where(
                                x =>
                                    x.Team == self.Team && self.Distance2D(x) <= castRange && IsInDanger(x) && x.IsAlive &&
                                    !x.IsIllusion && x.IsAlive)
                            .ToList();
                    */
                    if (false)
                    {
                        if (alliesIncludeMe.Any())
                        {
                            foreach (var ally in alliesIncludeMe)
                            {
                                if (ally.Health <= (ally.MaximumHealth * 0.3))
                                {
                                    if (targettingType == 1)
                                    {
                                        if (Utils.SleepCheck("saveduration"))
                                        {
                                            saveSpell.UseAbility(ally);
                                            Utils.Sleep(castTime + Game.Ping, "saveduration");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (alliesExcludeMe.Any())
                        {
                            foreach (var ally in alliesExcludeMe)
                            {
                                //Console.WriteLine("ally has health to be saved");
                                if (targettingType == 1)
                                {
                                    if (Utils.SleepCheck("saveduration"))
                                    {
                                        saveSpell.UseAbility(ally);
                                        Utils.Sleep(castTime + Game.Ping, "saveduration");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void Heal(Hero self, Ability healSpell, float[] amount, uint range, int targettingType,
            bool targetSelf = true)
        {
            if (healSpell != null && healSpell.CanBeCasted() && !self.IsChanneling())
            {
                if (self.IsAlive && !self.IsChanneling() &&
                    (!self.IsInvisible() || !me.Modifiers.Any(x => x.Name == "modifier_treant_natures_guise")) &&
                    self.Distance2D(fountain) > 2000)
                {
                    var heroes = targetSelf
                        ? ObjectMgr.GetEntities<Hero>()
                            .Where(
                                entity =>
                                    entity.Team == self.Team && self.Distance2D(entity) <= range && !entity.IsIllusion &&
                                    entity.IsAlive)
                            .ToList()
                        : ObjectMgr.GetEntities<Hero>()
                            .Where(
                                entity =>
                                    entity.Team == self.Team && self.Distance2D(entity) <= range && !entity.IsIllusion &&
                                    entity.IsAlive && !Equals(entity, me)).ToList();

                    if (heroes.Any())
                    {
                        foreach (var ally in heroes)
                        {
                            if (ally.Health <= (ally.MaximumHealth * 0.7) && healSpell.CanBeCasted() &&
                                self.Distance2D(fountain) > 2000 && IsInDanger(ally) &&
                                ally.Health + amount[healSpell.Level - 1] <= ally.MaximumHealth && (me.ClassID != ClassID.CDOTA_Unit_Hero_WitchDoctor || !me.Spellbook.SpellW.IsToggled))
                            {
                                if (targettingType == 1)
                                    CastHeal(healSpell, ally);
                                else if (targettingType == 2)
                                    CastHeal(healSpell);
                                else if (targettingType == 3 && Utils.SleepCheck("ToggleHeal"))
                                {
                                    /*if (healSpell.CanBeCasted() && Utils.SleepCheck("ToggleHeal"))
                                    {
                                        if (!healSpell.IsToggled)
                                        {
                                            CastHeal(healSpell);
                                            Utils.Sleep(1000 + Game.Ping, "ToggleHeal");
                                        }
                                    }*/

                                    CastHeal(healSpell, null, true);
                                    Utils.Sleep(1000 + Game.Ping, "ToggleHeal");
                                }
                            }
                            else if (targettingType == 3 && ally.Health > (ally.MaximumHealth * 0.7) && healSpell.IsToggled &&
                                     Utils.SleepCheck("ToggleHeal"))
                            {
                                healSpell.ToggleAbility();
                                Utils.Sleep(1000 + Game.Ping, "ToggleHeal");
                            }
                        }
                    }
                }
            }
        }

        private static void CastHeal(Ability healSpell, Hero destination = null, bool toggle = false)
        {
            /*if (destination != null)
            {
                if (healSpell.CanBeCasted() && Utils.SleepCheck("Casting Heal"))
                {
                    healSpell.UseAbility(destination);
                    Utils.Sleep(100 + Game.Ping, "Casting Heal");
                }
            }
            else
            {
                if (Utils.SleepCheck("Casting Heal"))
                {
                    healSpell.UseAbility();
                    Utils.Sleep(healSpell.ChannelTime + Game.Ping, "Casting Heal");
                }
            }*/

            if (healSpell != null && healSpell.CanBeCasted() && me.CanCast())
            {
                if (toggle)
                {
                    if (!healSpell.IsToggled)
                    {
                        healSpell.ToggleAbility();
                    }
                }
                else
                {
                    if (destination == null)
                    {
                    }
                    else
                    {
                        if (Utils.SleepCheck("HealSpell"))
                        {
                            healSpell.UseAbility(destination);
                            Utils.Sleep(1000 + Game.Ping, "HealSpell");
                        }
                        if (healSpell.Name == "wisp_tether" && !me.Spellbook.SpellW.IsToggled &&
                            me.Spellbook.SpellW.Cooldown == 0)
                        {
                            me.Spellbook.Spell4.ToggleAbility();
                        }
                    }
                }
            }
        }

        private static bool IsInDanger(Hero ally)
        {
            if (ally != null && ally.IsAlive)
            {
              
                var percHealth = (ally.Health <= (ally.MaximumHealth * 0.7));
                var enemies =
                    ObjectMgr.GetEntities<Hero>()
                        .Where(
                            entity =>
                                entity.Team != ally.Team && entity.IsAlive && entity.IsVisible && !entity.IsIllusion)
                        .ToList();
                if (enemies.Any() && percHealth)
                {
                    foreach (var enemy in enemies)
                    {
                        if (ally.Distance2D(enemy) < enemy.AttackRange + 50)
                        {
                            return true;
                        }
                        if (enemy.Spellbook.Spells.Any(abilities => ally.Distance2D(enemy) < abilities.CastRange + 50))
                        {
                            return true;
                        }
                    }
                }

                var buffs = new[]
                {
                    "modifier_item_urn_damage", "modifier_doom_bringer_doom", "modifier_axe_battle_hunger",
                    "modifier_queenofpain_shadow_strike", "modifier_phoenix_fire_spirit_burn",
                    "modifier_venomancer_poison_nova", "modifier_venomancer_venomous_gale",
                    "modifier_silencer_curse_of_the_silent", "modifier_silencer_last_word", "modifier_bane_fiends_grip",
                    "modifier_earth_spirit_magnetize", "modifier_jakiro_macropyre", "modifier_nerolyte_reapers_scythe",
                    "modifier_batrider_flaming_lasso", "modifier_sniper_assassinate", "modifier_pudge_dismember",
                    "modifier_enigma_black_hole_pull", "modifier_disruptor_static_storm", "modifier_sand_king_epicenter",
                    "modifier_bloodseeker_rupture", "modifier_dual_breath_burn", "modifier_jakiro_liquid_fire_burn",
                    "modifier_axe_battle_hunger", "modifier_viper_corrosive_skin", "modifier_viper_poison_attack",
                    "modifier_viper_viper_strike", "modifier_bounty_hunter_track"
                };
                foreach (var buff in buffs)
                {
                    if (ally.HasModifier(buff))
                    {
                        //Console.WriteLine("has modifier returning true");
                        return true;
                    }

                }
                var buffs2 = ally.Modifiers.ToList();

                if (buffs2.Any())
                {
                    
                    foreach (var buff in buffs2)
                    {
                        //Console.WriteLine(ally.Name + " has modifier: " + buff.Name);
                    }
                    
                }
                else
                {
                    //Console.WriteLine(ally.Name + " does not have any buff");
                }
                if (ally.IsStunned() || ally.IsSilenced())
                {
                    //Console.WriteLine("stun detected!");
                    return true;
                }
                if ((ally.IsStunned() ||
                     (ally.IsSilenced() &&
                      ((ally.FindItem("item_manta_style") == null || ally.FindItem("item_manta_style").Cooldown > 0) ||
                       (ally.FindItem("item_black_king_bar") == null ||
                        ally.FindItem("item_black_king_bar").Cooldown > 0))) ||
                     ally.IsHexed() ||
                     ally.IsRooted()) && !ally.IsInvul()
                    )
                {
                    //Console.WriteLine("stun detected!");
                    return true;
                }

                return false;
            }
            return false;
        }

        private static bool IsInDanger2(Hero ally)
        {
            if (ally != null && ally.IsAlive && ally.ClassID != me.ClassID)
            {
                /*
                var projectiles = ObjectMgr.Projectiles.Where(x => Equals(x.Target, ally)).ToList();

                if (projectiles.Any())
                {
                    foreach (var projectile in projectiles)
                    {
                        if (projectile.Source != null && Equals(projectile.Target, ally))
                        {
                            return true;
                        }
                    }
                }
                */
                var percHealth = (ally.Health <= (ally.MaximumHealth * 0.3));
                var enemies =
                    ObjectMgr.GetEntities<Hero>()
                        .Where(
                            entity =>
                                entity.Team != ally.Team && entity.IsAlive && entity.IsVisible && !entity.IsIllusion)
                        .ToList();
                if (enemies.Any() && percHealth)
                {
                    foreach (var enemy in enemies)
                    {
                        if (ally.Distance2D(enemy) < enemy.AttackRange + 50)
                        {
                            return true;
                        }
                        if (enemy.Spellbook.Spells.Any(abilities => ally.Distance2D(enemy) < abilities.CastRange + 50))
                        {
                            return true;
                        }
                    }
                }

                var buffs = new[]
                {
                    "modifier_item_urn_damage", "modifier_doom_bringer_doom", "modifier_axe_battle_hunger",
                    "modifier_queenofpain_shadow_strike", "modifier_phoenix_fire_spirit_burn",
                    "modifier_venomancer_poison_nova", "modifier_venomancer_venomous_gale",
                    "modifier_silencer_curse_of_the_silent", "modifier_silencer_last_word", "modifier_bane_fiends_grip",
                    "modifier_earth_spirit_magnetize", "modifier_jakiro_macropyre", "modifier_nerolyte_reapers_scythe",
                    "modifier_batrider_flaming_lasso", "modifier_sniper_assassinate", "modifier_pudge_dismember",
                    "modifier_enigma_black_hole_pull", "modifier_disruptor_static_storm", "modifier_sand_king_epicenter",
                    "modifier_bloodseeker_rupture", "modifier_dual_breath_burn", "modifier_jakiro_liquid_fire_burn",
                    "modifier_axe_battle_hunger", "modifier_viper_corrosive_skin", "modifier_viper_poison_attack",
                    "modifier_viper_viper_strike", "modifier_bounty_hunter_track", "modifier_life_stealer_open_wounds",
                    "modifier_phantom_assassin_stiflingdagger", "modifier_phantom_lancer_spirit_lance"
                };
                foreach (var buff in buffs)
                {
                    if (ally.HasModifier(buff))
                    {
                        //Console.WriteLine("has modifier returning true");
                        return true;
                    }

                }

                var buffs2 = ally.Modifiers.ToList();

                if (false && buffs2.Any())
                {

                    foreach (var buff in buffs2)
                    {
                        Console.WriteLine(ally.Name + " has modifier: " + buff.Name);
                    }

                }
                else
                {
                    //Console.WriteLine(ally.Name + " does not have any buff");
                }

                if (ally.IsStunned() || ally.IsSilenced())
                {
                    return true;
                }
                if ((ally.IsStunned() ||
                     (ally.IsSilenced() &&
                      ((ally.FindItem("item_manta_style") == null || ally.FindItem("item_manta_style").Cooldown > 0) ||
                       (ally.FindItem("item_black_king_bar") == null ||
                        ally.FindItem("item_black_king_bar").Cooldown > 0))) ||
                     ally.IsHexed() ||
                     ally.IsRooted()) && !ally.IsInvul()
                    )
                {
                    //Console.WriteLine("stun detected!");
                    return true;
                }

                return false;
            }
            return false;
        }

        private static bool Support(ClassID hero)
        {
            if ((hero == ClassID.CDOTA_Unit_Hero_Oracle || hero == ClassID.CDOTA_Unit_Hero_Winter_Wyvern ||
                 hero == ClassID.CDOTA_Unit_Hero_KeeperOfTheLight || hero == ClassID.CDOTA_Unit_Hero_Dazzle ||
                 hero == ClassID.CDOTA_Unit_Hero_Chen || hero == ClassID.CDOTA_Unit_Hero_Enchantress ||
                 hero == ClassID.CDOTA_Unit_Hero_Legion_Commander || hero == ClassID.CDOTA_Unit_Hero_Abaddon ||
                 hero == ClassID.CDOTA_Unit_Hero_Omniknight || hero == ClassID.CDOTA_Unit_Hero_Treant ||
                 hero == ClassID.CDOTA_Unit_Hero_Wisp || hero == ClassID.CDOTA_Unit_Hero_Centaur ||
                 hero == ClassID.CDOTA_Unit_Hero_Undying || hero == ClassID.CDOTA_Unit_Hero_WitchDoctor ||
                 hero == ClassID.CDOTA_Unit_Hero_Necrolyte || hero == ClassID.CDOTA_Unit_Hero_Warlock ||
                 hero == ClassID.CDOTA_Unit_Hero_Rubick || hero == ClassID.CDOTA_Unit_Hero_Huskar ||
                 hero == ClassID.CDOTA_Unit_Hero_Shadow_Demon) && Utils.SleepCheck("checkIfSupport"))
            {
                return true;
            }
            return false;
        }
    }
}