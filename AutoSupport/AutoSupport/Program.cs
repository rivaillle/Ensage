﻿using System;
using System.Linq;
using System.Windows.Input;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using SharpDX;
using System.Collections.Generic;

namespace SupportSharp
{
    internal class Program
    {
        private const Key toggleKey = Key.T;
        private const Key offensiveKey = Key.D;
        private const Key saveSelfKey = Key.Y;
        private static Hero me;
        private static Ability misticAbility;
        private static double glimmerThreshold = 0.6;
        private static double shopThreshold = 0.45;
        private static double[] misticDamagePerLevel = new double[] { 100, 150, 200, 250 };
        private static Entity fountain;
        private static bool loaded;
        private static ParticleEffect rangeDisplay;
        private static Boolean rangeWithAether = false;
        private static ClassId medallionClassId = 0;
        private static ClassId enemyToHarass = 0;

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
            Eul,
            Tango;

        private static Hero needMana;
        private static Hero needMeka;
        private static Hero target;
        private static bool supportActive;
        private static bool includeSaveSelf;
        private static bool offensiveMode;
        private static bool shouldCastLotusOrb;
        private static bool shouldCastGlimmerCape;
        private static IEnumerable<Hero> enemies;
        private static IEnumerable<Hero> allies;

        private static void Main(string[] args)
        {
            Game.OnUpdate += Game_OnUpdate;
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
            Eul = null;
            misticAbility = null;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (loaded)
            {
                var mode = supportActive ? "ON" : "OFF";
                var behaviorMode = offensiveMode ? "Offensive" : "Defensive";              
                var includeSelfMode = includeSaveSelf ? "ON" : "OFF";
                Drawing.DrawText("Auto Support is: " + mode + ". Hotkey (Toggle): " + toggleKey,
                    new Vector2(Drawing.Width * 5 / 100, Drawing.Height * 4 / 100), new Vector2(24), (supportActive ? Color.LightBlue : Color.Red), FontFlags.DropShadow);
                Drawing.DrawText("Behavior is: " + behaviorMode + ". Hotkey (Toggle): " + offensiveKey,
                    new Vector2(Drawing.Width * 5 / 100, Drawing.Height * 10 / 100), new Vector2(24), (offensiveMode ? Color.Red : Color.LightBlue), FontFlags.DropShadow);

                if (rangeDisplay == null)
                {
                    rangeDisplay = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                    rangeDisplay.SetControlPoint(1, new Vector3(255, 255, 255));
                    rangeDisplay.SetControlPoint(2, new Vector3(550, 255, 0));
                }
                else if (me.HasModifier("modifier_item_aether_lens") && rangeWithAether == false)
                {
                    Console.WriteLine("aether");
                    rangeDisplay.SetControlPoint(2, new Vector3(550 + 220, 255, 0));
                    rangeDisplay.Restart();
                    rangeWithAether = true;

                }
            }
            
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!loaded)
            {
                me = ObjectManager.LocalHero;


                if (!Game.IsInGame || Game.IsWatchingGame || me == null || Game.IsChatOpen)
                {
                    return;
                }
                loaded = true;
            }

            if (me == null || !me.IsValid)
            {
                loaded = false;
                me = ObjectManager.LocalHero;
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
            Eul = me.FindItem("item_cyclone");
            if (QuellingBlade == null)
            {
                QuellingBlade = me.FindItem("item_iron_talon");
            }
            if (Medallion == null)
            {
                Medallion = me.FindItem("item_solar_crest");
            }
            needMana = null;
            needMeka = null;
            shouldCastLotusOrb = false;
            shouldCastGlimmerCape = false;
            bool dangerousSpell = false;
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
                if (Game.IsKeyDown(offensiveKey) && Utils.SleepCheck("togglingoption"))
                {
                    if (!offensiveMode)
                    {
                        offensiveMode = true;
                    }
                    else
                    {
                        offensiveMode = false;
                    }
                    Utils.Sleep(200, "togglingoption");
                }
            }

            if (supportActive && me.IsAlive)
            {
                if (me.HasModifier("modifier_silver_edge_debuff") && Utils.SleepCheck("ult") && me.Health < me.MaximumHealth * 0.75)
                {
                    me.Spellbook.SpellR.UseAbility();
                    Utils.Sleep(6000, "ult");
                }

                if (IsInDanger(me) && me.Health <= me.MaximumHealth * 0.4 )
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
                    extraRange = 220;
                }
                enemies =
                                    ObjectManager.GetEntitiesFast<Hero>()
                                        .Where(
                                            x =>
                                                x.Team != me.Team && !x.IsIllusion && x.IsAlive &&
                                                me.Distance2D(x) <= 1050)
                                        .ToList();

                allies = ObjectManager.GetEntitiesFast<Hero>()
                               .Where(
                                   x =>
                                       x.Team == me.Team && (x.ClassId != me.ClassId) && !x.IsIllusion && x.IsAlive &&
                                       me.Distance2D(x) <= 1050);

                if (enemies.Any())
                {
                    foreach (var enemy in enemies)
                    {
                        if(me.Distance2D(enemy) <= 800 + extraRange)
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
                                CastHeal(misticAbility, enemy);
                            }
                            if (enemy.IsLinkensProtected())
                            {
                                CastHeal(misticAbility, enemy);
                            }                            
                            if (Eul != null && Utils.SleepCheck("cyclone") && Eul.CanBeCasted())
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
                                }else if (checkSlarkConditions(enemy) && enemy.HasModifier("modifier_slark_pounce"))
                                {
                                    Console.WriteLine("POUNCE DETECTED");
                                    var allies =
                                    ObjectManager.GetEntitiesFast<Hero>()
                                        .Where(
                                            x =>
                                                x.Team == me.Team && !x.IsIllusion && x.IsAlive && enemy.Distance2D(x) > 300 && enemy.Distance2D(x) < 900)
                                        .ToList();
                                    if (allies.Any())
                                    {
                                        foreach (var ally in allies)
                                        {
                                            if (IsFacing(enemy, ally))
                                            {
                                                Eul.UseAbility(enemy);
                                                Utils.Sleep(1000, "cyclone");
                                            }
                                        }
                                    }
                                    
                                    
                                }
                            }
                        }
                    }
                }
                
                uint addedRange = 0;
                if (Support(me.ClassId))
                {
                    switch (me.ClassId)
                    {
                        case ClassId.CDOTA_Unit_Hero_Abaddon:
                            var healSpell = me.Spellbook.SpellQ;
                            if (me.HasModifier("modifier_item_aether_lens"))
                            {
                                addedRange += 220;
                            }
                            Save(me, me.Spellbook.SpellW, 580, me.Spellbook.SpellW.CastRange + addedRange);
                            if (!offensiveMode)
                            {
                                Heal(me, healSpell, new float[] { 100, 150, 200, 250 },
                                800 + addedRange);
                                AuxItems(me);
                                ShopItems(me);
                            }
                                                     
                            if(offensiveMode)
                            {
                                var closestEnemy = me.ClosestToMouseTarget(700);
                                if(closestEnemy != null && Utils.SleepCheck("solar") && Utils.SleepCheck("saveduration") && me.Distance2D(closestEnemy) <= 1000 && Medallion != null && Medallion.CanBeCasted())
                                {
                                    Medallion.UseAbility(closestEnemy);
                                    Utils.Sleep(1000, "solar");
                                }
                                if (closestEnemy != null && healSpell.CanBeCasted() && me.Distance2D(closestEnemy) <= 800 + extraRange)
                                {
                                    CastHeal(healSpell, closestEnemy);

                                }
                            }
                            break;
                    }
                }
            }
            if (QuellingBlade != null && QuellingBlade.Cooldown == 0) {
                var wards =
                                        ObjectManager.GetEntitiesFast<Unit>()
                                            .Where(
                                                x =>
                                                    x.Team != me.Team && (x.ClassId == ClassId.CDOTA_NPC_Observer_Ward || x.ClassId == ClassId.CDOTA_NPC_Observer_Ward_TrueSight) &&
                                                    me.Distance2D(x) <= 475)
                                            .ToList();
                if (wards.Any() && Utils.SleepCheck("deward"))
                {
                    QuellingBlade.UseAbility(wards[0]);
                    Utils.Sleep(1000, "deward");
                }
            }
           
        }

        private static void Save(Hero self, Ability saveSpell, float castTime = 800, uint castRange = 0)
        {
            
                    long auxCastRange = 0;
            if(self.IsAlive && !self.IsChanneling() && !self.IsInvisible())
            {
                if (GlimmerCape != null && Utils.SleepCheck("glimmer") && GlimmerCape.CanBeCasted())
                {
                    auxCastRange = 1000;
                }
                else if (saveSpell != null && saveSpell.CanBeCasted() && Utils.SleepCheck("saveduration"))
                {
                    auxCastRange = castRange;
                }
                else
                {
                    return;
                }
                    var allies =
                        ObjectManager.GetEntitiesFast<Hero>()
                            .Where(
                                x =>
                                    x.Team == self.Team && self.ClassId != x.ClassId && !x.IsIllusion && x.IsAlive && self.Distance2D(x) <= auxCastRange)
                            .ToList();
                    var isInDanger = false;
                    if (allies.Any())
                    {
                        foreach (var ally in allies)
                        {
                            isInDanger = IsInDanger2(ally);                          
                            if (saveSpell.CanBeCasted() && Utils.SleepCheck("saveduration") && self.Distance2D(ally) <= castRange && isInDanger)
                            {
                                saveSpell.UseAbility(ally);
                                Utils.Sleep(castTime + Game.Ping, "saveduration");
                            }

                            else if (GlimmerCape != null && Utils.SleepCheck("glimmer") && Utils.SleepCheck("saveduration_" + ally.Name) && GlimmerCape.CanBeCasted() && !ally.IsMagicImmune() && !ally.IsAttacking() && ally.Health <= ally.MaximumHealth * glimmerThreshold)
                            {
                                foreach (var enemy in enemies)
                                {
                                     if(IsFacing(ally, enemy))
                                     {
                                        return;
                                     }
                                }
                                GlimmerCape.UseAbility(ally);
                                Utils.Sleep(1000, "glimmer");
                            }

                        }
                    }
                    
                }
            
        }

        private static void Heal(Hero self, Ability healSpell, float[] amount, uint range)
        {
            if (healSpell != null && healSpell.CanBeCasted() && !self.IsChanneling())
            {
                if (self.IsAlive && !self.IsChanneling() &&
                    (!self.IsInvisible()))
                {
                    var heroes = ObjectManager.GetEntitiesFast<Hero>()
                            .Where(
                                entity =>
                                    entity.Team == self.Team && self.Distance2D(entity) <= range && !entity.IsIllusion &&
                                    entity.IsAlive && entity.ClassId != me.ClassId).ToList();

                    if (heroes.Any())
                    {
                        foreach (var ally in heroes)
                        {
                            if ((ally.Health <= (ally.MaximumHealth * 0.7) || (ally.Health + amount[healSpell.Level - 1] <= ally.MaximumHealth && (me.Mana > (me.MaximumMana * 0.9) || me.Mana > 500)))  && healSpell.CanBeCasted() &&
                                IsInDanger(ally) && me.Health > 500)
                            {
                                  CastHeal(healSpell, ally);
                                
                            }                            
                        }
                        //checkExtraBuffs(heroes)
                    }
                }
            }
        }

        private static void CastHeal(Ability healSpell, Hero destination = null)
        {
            if (healSpell != null && healSpell.CanBeCasted() && me.CanCast())
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
                }                
            }
        }

        private static bool IsInDanger(Hero ally)
        {
            if (ally != null && ally.IsAlive)
            {
              
                var enemies =
                    ObjectManager.GetEntitiesFast<Hero>()
                        .Where(
                            entity =>
                                entity.Team != ally.Team && entity.IsAlive && entity.IsVisible && !entity.IsIllusion)
                        .ToList();
                if (enemies.Any())
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
                    "modifier_axe_battle_hunger", "modifier_viper_poison_attack",
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
                if (ally.IsStunned() || 
                     ally.IsSilenced()||
                     ally.IsHexed() ||
                     ally.IsRooted()
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
            if (ally != null && ally.IsAlive && ally.ClassId != me.ClassId)
            {               
                var percHealth = (ally.Health <= (ally.MaximumHealth * 0.3));
                var enemies =
                    ObjectManager.GetEntitiesFast<Hero>()
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
                    "modifier_axe_battle_hunger", "modifier_viper_poison_attack", "modifier_axe_berserkers_call",
                    "modifier_viper_viper_strike", "modifier_bounty_hunter_track", "modifier_life_stealer_open_wounds",
                    "modifier_phantom_assassin_stiflingdagger", "modifier_ember_spirit_searing_chains", "modifier_phantom_lancer_spirit_lance", "modifier_treant_leech_seed"
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
                foreach (var item in ally.Inventory.Items)
                {
                   // Console.WriteLine(item.Name);
                }
               
                if(ally.HasModifier("modifier_item_dustofappearance") && CanGoInvis(ally))
                {
                    return true;
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

        private static void ShopItems(Hero me)
        {
            var ult = me.Spellbook.SpellR;
            var reliableGold =  me.Player.ReliableGold;
            var unReliableGold = me.Player.UnreliableGold;
            long gold = reliableGold + unReliableGold;
            uint cost = 0;
            bool shouldSaveBuyback = ShouldSaveForBuyback(me, 27);
            if (shouldSaveBuyback)
            {
                return;
            }
            if (!ult.CanBeCasted() && Utils.SleepCheck("shop") && IsInDanger(me) && me.Health < me.MaximumHealth * shopThreshold)
            {
                var itemsToBuy = Player.QuickBuyItems.OrderByDescending(x => Ability.GetAbilityDataById(x).Cost);
                foreach (var itemToBuy in itemsToBuy)
                {
                    cost = Ability.GetAbilityDataById(itemToBuy).Cost;
                    if(gold >= cost)
                    {
                        Player.BuyItem(me, itemToBuy);
                        gold = gold - cost;
                        Utils.Sleep(500, "shop");
                    }
                }
                cost = Ability.GetAbilityDataById(AbilityId.item_ward_observer).Cost;
                var wardsCount = GetWardsCount(me, AbilityId.item_ward_observer);
                if (gold >= cost && wardsCount < 2)
                {
                    while(gold >= cost && wardsCount < 2)
                    {
                        Player.BuyItem(me, AbilityId.item_ward_observer);
                        gold = gold - cost;
                        wardsCount += 1;
                        Utils.Sleep(500, "shop");
                    }
                    
                }
                cost = Ability.GetAbilityDataById(AbilityId.item_ward_sentry).Cost;
                var sentriesCount = GetWardsCount(me, AbilityId.item_ward_sentry);
                if (gold >= cost && sentriesCount < 1)
                {
                    while (gold >= cost && sentriesCount < 2)
                    {
                        Player.BuyItem(me, AbilityId.item_ward_sentry);
                        gold = gold - cost;
                        sentriesCount += 1;
                        Utils.Sleep(500, "shop");
                    }
                    gold = gold - cost;
                    Utils.Sleep(500, "shop");
                }

                cost = Ability.GetAbilityDataById(AbilityId.item_tpscroll).Cost;
                var tpCount = GetItemCount(me, AbilityId.item_tpscroll);
                if (gold >= cost && tpCount < 2)
                {
                    while (gold >= cost && tpCount < 2)
                    {
                        Player.BuyItem(me, AbilityId.item_tpscroll);
                        gold = gold - cost;
                        tpCount += 1;
                        Utils.Sleep(500, "shop");
                    }
                    gold = gold - cost;
                    Utils.Sleep(500, "shop");
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
                        if (isCarry(enemy) && enemy.IsAttacking() || (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Juggernaut && enemy.Spellbook.Spells.Any(x => x.IsInAbilityPhase && x.AbilityType == AbilityType.Ultimate)))
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

                if (GlimmerCape != null && Utils.SleepCheck("glimmer") && GlimmerCape.CanBeCasted())
                {
                    foreach (var enemy in enemies)
                    {
                        if (isNuker(enemy) && enemy.Spellbook.Spells.Any(x => x.IsInAbilityPhase))
                        {
                            if (IsFacing(enemy, self) && enemy.Distance2D(self) <= 1000 && !self.IsAttacking())
                            {
                                GlimmerCape.UseAbility(self);
                                Utils.Sleep(1000, "glimmer");
                            }
                            else if (allies.Any())
                            {
                                foreach (var ally in allies)
                                {
                                    if (IsFacing(enemy, ally) && !ally.IsMagicImmune() && enemy.Distance2D(ally) <= 1000 && !ally.IsAttacking())
                                    {
                                        GlimmerCape.UseAbility(ally);
                                        Utils.Sleep(1000, "glimmer");
                                    }
                                }
                            }

                        }
                    }


                }

            }


        }

        private static bool Support(ClassId hero)
        {
            if (hero == ClassId.CDOTA_Unit_Hero_Abaddon)
            {
                return true;
            }
            return false;
        }

        private static bool IsFacing(Hero hero, Hero enemy, double maxAngle = 0.1)
        {

            float deltaY = hero.Position.Y - enemy.Position.Y;
            float deltaX = hero.Position.X - enemy.Position.X;
            float angle = (float)(Math.Atan2(deltaY, deltaX));

            float n1 = (float)Math.Sin(hero.RotationRad - angle);
            float n2 = (float)Math.Cos(hero.RotationRad - angle);
            return (Math.PI - Math.Abs(Math.Atan2(n1, n2))) < 0.1;
        }

        private static bool CanGoInvis(Hero ally) {
            if (ally.ClassId == ClassId.CDOTA_Unit_Hero_Clinkz || ally.ClassId == ClassId.CDOTA_Unit_Hero_Treant || ally.ClassId == ClassId.CDOTA_Unit_Hero_Riki || ally.ClassId == ClassId.CDOTA_Unit_Hero_BountyHunter
                    || ally.HasModifier("modifier_item_invisibility_edge_windwalk") || ally.HasModifier("modifier_item_silver_edge_windwalk"))
            {
                return true;
            }
            return false;
        }

        private static bool isCarry(Hero enemy)
        {
            if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Slark || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Sven || enemy.ClassId == ClassId.CDOTA_Unit_Hero_AntiMage || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Sniper
                            || enemy.ClassId == ClassId.CDOTA_Unit_Hero_TemplarAssassin || enemy.ClassId == ClassId.CDOTA_Unit_Hero_DragonKnight || enemy.ClassId == ClassId.CDOTA_Unit_Hero_DrowRanger || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Legion_Commander
                            || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Life_Stealer || enemy.ClassId == ClassId.CDOTA_Unit_Hero_MonkeyKing || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Ursa || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Weaver || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Windrunner
                            || enemy.ClassId == ClassId.CDOTA_Unit_Hero_SkeletonKing || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Riki || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Terrorblade || enemy.ClassId == ClassId.CDOTA_Unit_Hero_TrollWarlord || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Huskar || enemy.ClassId == ClassId.CDOTA_Unit_Hero_PhantomAssassin
                            || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Luna || enemy.ClassId == ClassId.CDOTA_Unit_Hero_EmberSpirit || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Clinkz || enemy.ClassId == ClassId.CDOTA_Unit_Hero_LoneDruid || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Juggernaut || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Gyrocopter || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Obsidian_Destroyer || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Shadow_Demon || enemy.ClassId == ClassId.CDOTA_Unit_Hero_FacelessVoid)
            {
                return true;
            }
            return false;
        }
        private static bool isNuker(Hero enemy)
        {
            if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Morphling || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Zuus || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Necrolyte || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Invoker
                || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Batrider || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Juggernaut || enemy.ClassId == ClassId.CDOTA_Unit_Hero_StormSpirit)
            {
                return true;
            }
            return false;
        }

        //Against Slark go BladeMail, SolarCrest, Halberd
        private static bool checkSlarkConditions(Hero enemy)
        {
            if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Slark)
            {

                var pact = enemy.Spellbook.SpellQ;
                
                if (pact.Cooldown <= 7 && pact.Cooldown > 3 || pact.CanBeCasted())
                {
                    return true;
                }else
                {
                    return false;
                }

            }else
            {
                return false;
            }
        }

        private static uint GetItemCount(Hero me, AbilityId id)
        {
            return (me.Inventory.Items.FirstOrDefault(x => x.GetAbilityId().Equals(id))?.CurrentCharges ?? 0)
                   + (me.Inventory.Stash.FirstOrDefault(x => x.GetAbilityId().Equals(id))?.CurrentCharges ?? 0)
                   + (me.Inventory.Backpack.FirstOrDefault(x => x.GetAbilityId().Equals(id))?.CurrentCharges ?? 0);
        }

        private static uint GetWardsCount(Hero me, AbilityId id)
        {
            var inventoryDispenser = me.Inventory.Items.FirstOrDefault(x => x.Id == AbilityId.item_ward_dispenser);
            var stashDispenser = me.Inventory.Stash.FirstOrDefault(x => x.Id == AbilityId.item_ward_dispenser);
            var backpackDispenser = me.Inventory.Backpack.FirstOrDefault(x => x.Id == AbilityId.item_ward_dispenser);

            return GetItemCount(me, id)
                   + (id == AbilityId.item_ward_observer
                          ? (inventoryDispenser?.CurrentCharges ?? 0) + (stashDispenser?.CurrentCharges ?? 0)
                            + (backpackDispenser?.CurrentCharges ?? 0)
                          : (inventoryDispenser?.SecondaryCharges ?? 0) + (stashDispenser?.SecondaryCharges ?? 0)
                            + (backpackDispenser?.SecondaryCharges ?? 0));
        }

        protected static int BuybackCost(Hero me) { return (int)(100 + Math.Pow(me.Level, 2) * 1.5 + Game.GameTime / 60 * 15); }
        protected static bool ShouldSaveForBuyback(Hero me, float time)
        {
            return time > 0 && Game.GameTime / 60 > time
                   && me.Player.BuybackCooldownTime < 3.8 * me.Level + 5 + me.RespawnTimePenalty;
        }
    }
}