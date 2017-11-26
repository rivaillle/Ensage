using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;

namespace AutoGhost
{
    class AutoGhost
    {
        private static double shopThreshold = 0.45;
        private static Unit fountain;
        public static bool useGhost(Item ghost, Hero me, IEnumerable<Hero> enemies, bool dodge = false, Hero target = null)
        {
            Hero destiny = null;
            if (target == null) {
                destiny = me;
            }
            else
            {
                destiny = target;
            }
            var isFacing = false;
            if (dodge == true && destiny.ClassId != me.ClassId)
            {
                foreach (var enemy in enemies)
                {
                    if (IsFacing(destiny, enemy))
                    {
                        isFacing = true;
                        return false;
                    }
                }
                if (isFacing)
                {
                    return false;
                }
            }
            if (ghost != null && ghost.CanBeCasted() && Utils.SleepCheck(ghost.Name))
            {
                foreach (var enemy in enemies)
                {
                    if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_PhantomAssassin)
                    {
                        if (IsFacing(enemy, destiny) && (enemy.Spellbook.SpellW.IsInAbilityPhase || enemy.HasModifier("modifier_phantom_assassin_phantom_strike")))
                        {
                            useItem(ghost, target);
                            Utils.Sleep(1000, ghost.Name);
                            return true;
                        }
                    }
                    if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Legion_Commander)
                    {
                        var duel = enemy.Spellbook.SpellR;
                        if (IsFacing(enemy, destiny) && (duel.IsInAbilityPhase || (enemy.Distance2D(me) <= 200 && duel.CanBeCasted())))
                        {
                            useItem(ghost, target);
                            Utils.Sleep(1000, ghost.Name);
                            return true;
                        }
                    }
                    else if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Slark)
                    {
                        if (enemy.Distance2D(destiny) < 300 && destiny.HasModifier("modifier_slark_pounce_leash") && Utils.SleepCheck("pounce_dodged"))
                        {
                            useItem(ghost, target);
                            Utils.Sleep(1000, ghost.Name);
                            Utils.Sleep(5000, "pounce_dodged");
                            return true;
                        }
                    }
                    else if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Juggernaut && enemy.Distance2D(destiny) < 400 && Utils.SleepCheck("omnislash_dodged_" + destiny.ClassId) && (enemy.Spellbook.SpellR.IsInAbilityPhase || enemy.HasModifier("modifier_juggernaut_omnislash")))
                    {
                        useItem(ghost, target);
                        Utils.Sleep(1000, ghost.Name);
                        Utils.Sleep(5000, "omnislash_dodged_" + destiny.ClassId);
                        return true;
                    }
                    else if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_AntiMage && enemy.Distance2D(destiny) < 200 && IsFacing(enemy, destiny))
                    {
                        var manta = enemy.FindItem("item_manta");
                        if (manta != null && !manta.CanBeCasted() && manta.Cooldown > 25)
                        {
                            useItem(ghost, target);
                        } else if (manta == null)
                        {
                            useItem(ghost, target);
                        }
                    }
                    else if (dodge == true && (enemy.ClassId != ClassId.CDOTA_Unit_Hero_Juggernaut || Utils.SleepCheck("omnislash_dodged_" + destiny.ClassId)) && IsFacing(enemy, destiny) && enemy.Spellbook.Spells.Any(x => x.IsInAbilityPhase && x.IsNuke()) && enemy.Distance2D(destiny) < 650)
                    {
                        useItem(ghost, target);
                        Utils.Sleep(1000, ghost.Name);
                        return true;


                    }
                    else if (isCarry(enemy) && enemy.Distance2D(destiny) < enemy.AttackRange && IsFacing(enemy, destiny) && (enemy.IsAttacking() || destiny.Health < destiny.MaximumHealth * 20 / 100) && (destiny.Health < destiny.MaximumHealth * 40 / 100 || isCarryMad(enemy)))
                    {
                        useItem(ghost, target);
                        Utils.Sleep(1000, ghost.Name);
                        return true;
                    }
                    else if (isNuker(enemy) && IsFacing(enemy, destiny) && (enemy.Spellbook.Spells.Any(x => x.IsInAbilityPhase && (x.IsNuke() || x.IsDisable())) || (enemy.IsAttacking() && hasLowHeahth(destiny))) && IsFacing(enemy, destiny) && enemy.Distance2D(destiny) <= 1000)
                    {
                        useItem(ghost, target);
                        Utils.Sleep(1000, ghost.Name);
                        return true;
                    }
                }
            }
            return false;
        }

        private static void useItem(Item item, Hero target)
        {
            if (target == null)
            {
                item.UseAbility();
            } else
            {
                item.UseAbility(target);
            }
        }

        public static void useWand(Hero me, IEnumerable<Hero> enemies)
        {
            Item stick = me.FindItem("item_magic_stick");
            if(stick == null)
            {
                stick = me.FindItem("item_magic_wand");
            }
            if (stick != null && me.Health <= me.MaximumHealth * 0.4 && IsInDanger(me, enemies))
            {
                if (stick != null && Utils.SleepCheck("Stick") && stick.CurrentCharges > 0 && stick.CanBeCasted())
                {
                    stick.UseAbility();
                    Utils.Sleep(100 + Game.Ping, "Stick");
                }

            }

        }

        public static void ShopItems(Hero me, bool ultSoft, IEnumerable<Hero> enemies)
        {
            return;
            var ult = me.Spellbook.SpellR;
            var reliableGold = me.Player.ReliableGold;
            var unReliableGold = me.Player.UnreliableGold;
            long gold = reliableGold + unReliableGold;
            uint cost = 0;
            bool shouldSaveBuyback = ShouldSaveForBuyback(me, 27);
            if (shouldSaveBuyback)
            {
                return;
            }
            if ((ultSoft && !ult.CanBeCasted() || !ultSoft) && Utils.SleepCheck("shop") && IsInDanger(me, enemies) && me.Health < me.MaximumHealth * shopThreshold)
            {
                var itemsToBuy = Player.QuickBuyItems.OrderByDescending(x => Ability.GetAbilityDataById(x).Cost);
                foreach (var itemToBuy in itemsToBuy)
                {
                    cost = Ability.GetAbilityDataById(itemToBuy).Cost;
                    if (gold >= cost)
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
                    while (gold >= cost && wardsCount < 2)
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

        protected static bool ShouldSaveForBuyback(Hero me, float time)
        {
            return time > 0 && Game.GameTime / 60 > time
                   && me.Player.BuybackCooldownTime < 3.8 * me.Level + 5 + me.RespawnTimePenalty;
        }

        private static bool IsInDanger(Hero ally, IEnumerable<Hero> enemies)
        {
            if (ally != null && ally.IsAlive)
            {
                               
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
                /*
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
                */
                if (ally.IsStunned() || ally.IsSilenced())
                {
                    //Console.WriteLine("stun detected!");
                    return true;
                }
                if (ally.IsStunned() ||
                     ally.IsSilenced() ||
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

        public static bool isInDanger2(Hero ally)
        {
            if (ally != null && ally.IsAlive)
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
                    "modifier_silencer_last_word", "modifier_bane_fiends_grip",
                    "modifier_earth_spirit_magnetize", "modifier_jakiro_macropyre", "modifier_nerolyte_reapers_scythe",
                    "modifier_batrider_flaming_lasso", "modifier_sniper_assassinate", "modifier_pudge_dismember",
                    "modifier_enigma_black_hole_pull", "modifier_disruptor_static_storm", "modifier_sand_king_epicenter",
                    "modifier_bloodseeker_rupture", "modifier_dual_breath_burn", "modifier_jakiro_liquid_fire_burn",
                    "modifier_axe_battle_hunger", "modifier_viper_poison_attack",
                    "modifier_viper_viper_strike", "modifier_life_stealer_open_wounds",
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

                if (ally.HasModifier("modifier_item_dustofappearance") && CanGoInvis(ally))
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

        public static bool IsFacing(Hero hero, Hero enemy)
        {

            float deltaY = hero.Position.Y - enemy.Position.Y;
            float deltaX = hero.Position.X - enemy.Position.X;
            float angle = (float)(Math.Atan2(deltaY, deltaX));

            float n1 = (float)Math.Sin(hero.RotationRad - angle);
            float n2 = (float)Math.Cos(hero.RotationRad - angle);

            return (Math.PI - Math.Abs(Math.Atan2(n1, n2))) < 0.1;
        }

        private static bool CanGoInvis(Hero ally)
        {
            if (ally.ClassId == ClassId.CDOTA_Unit_Hero_Clinkz || ally.ClassId == ClassId.CDOTA_Unit_Hero_Treant || ally.ClassId == ClassId.CDOTA_Unit_Hero_Riki || ally.ClassId == ClassId.CDOTA_Unit_Hero_BountyHunter
                    || ally.HasModifier("modifier_item_invisibility_edge_windwalk") || ally.HasModifier("modifier_item_silver_edge_windwalk"))
            {
                return true;
            }
            return false;
        }

        public static void AuxItems(Hero me, IEnumerable<Hero> enemies, IEnumerable<Hero> allies)
        {
            var Medallion = me.FindItem("item_medallion_of_courage");
            if (Medallion == null)
            {
                Medallion = me.FindItem("item_solar_crest");
            }
            var Eul = me.FindItem("item_cyclone");
            var myBlink = me.FindItem("item_blink");
            var staff = me.FindItem("item_force_staff");
            Item manta = null;
            var diffusal = me.FindItem("item_diffusal_blade");
            if (!me.IsInvisible() && me.CanCast())
            {
                if (Medallion != null || Eul != null || myBlink != null || staff != null || diffusal != null)
                {
                    foreach (var enemy in enemies)
                    {
                        if (Medallion != null && Utils.SleepCheck("solar") && Medallion.CanBeCasted())
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
                        var blink = enemy.FindItem("item_blink");
                        if (Eul != null && enemy.Distance2D(me) <= 700 && !me.IsInvisible())
                        {
                            if (Eul != null && Utils.SleepCheck("cyclone") && Eul.CanBeCasted())
                            {
                                if (blink != null && blink.Cooldown > 8)
                                {
                                    Eul.UseAbility(enemy);
                                    Utils.Sleep(1000, "cyclone");
                                }
                                else if (enemy.IsChanneling() && me.ClassId != ClassId.CDOTA_Unit_Hero_Zuus)
                                {
                                    Eul.UseAbility(enemy);
                                    Utils.Sleep(1000, "cyclone");
                                }
                                else if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_AntiMage)
                                {
                                    manta = enemy.FindItem("item_manta");
                                    if (manta != null)
                                    {
                                        var abyssal = enemy.FindItem("item_abyssal_blade");
                                        if ((!manta.CanBeCasted() && manta.Cooldown > 25) || (abyssal != null && !abyssal.CanBeCasted()) || (!manta.CanBeCasted() && enemy.Spellbook.SpellW.Cooldown > 3))
                                        {
                                            Eul.UseAbility(enemy);
                                            break;                                           
                                        }
                                    }
                                }
                                else if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_FacelessVoid)
                                {
                                    manta = enemy.FindItem("item_manta");
                                    if (manta != null)
                                    {
                                        if ((!manta.CanBeCasted() && manta.Cooldown > 25))
                                        {
                                            if (IsFacing(enemy, me) && enemy.Distance2D(me) < 200)
                                            {
                                                Eul.UseAbility(enemy);
                                                break;
                                            }
                                            foreach (var ally in allies)
                                            {
                                                if (IsFacing(enemy, ally) && enemy.Distance2D(ally) < 200)
                                                {
                                                    Eul.UseAbility(enemy);
                                                    Utils.Sleep(1000, "cyclone");
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    
                                    if(enemy.Spellbook.Spells.Any(x => x.IsInAbilityPhase && x.AbilityType == AbilityType.Ultimate))
                                    {
                                        Eul.UseAbility(enemy);
                                        Utils.Sleep(1000, "cyclone");
                                        break;
                                    }
                                    if(enemy.Spellbook.SpellQ.Cooldown > 4 && enemy.Spellbook.SpellR.CanBeCasted())
                                    {
                                        Eul.UseAbility(enemy);
                                        Utils.Sleep(1000, "cyclone");
                                        break;
                                    }
                                    if (enemy.HasModifier("modifier_faceless_void_chronosphere_speed"))
                                    {
                                        Eul.UseAbility(enemy);
                                        Utils.Sleep(1000, "cyclone");
                                        break;
                                    }

                                }
                            }

                        }
                        if (myBlink != null && Utils.SleepCheck("myBlink") && myBlink.CanBeCasted() && !me.IsInvisible())
                        {
                            if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_AntiMage)
                            {
                                if (enemy.Distance2D(me) < 300)
                                {
                                    var blinkUnit = getFountain(me);
                                    myBlink.UseAbility(blinkUnit.NetworkPosition);
                                    Utils.Sleep(1000, "myBlink");
                                }
                            }
                        }
                        if (staff != null && Utils.SleepCheck("staff") && staff.CanBeCasted() && !me.IsInvisible())
                        {
                            if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_AntiMage)
                            {
                                if (enemy.Distance2D(me) < 750)
                                {
                                    if(manta == null)
                                    {
                                        manta = enemy.FindItem("item_manta");
                                    }
                                    if(manta != null && !manta.CanBeCasted() && manta.Cooldown > 25)
                                    {
                                        staff.UseAbility(enemy);
                                    }
                                    Utils.Sleep(1000, "staff");
                                }
                            }
                            else if(blink != null && enemy.Distance2D(me) < 400 && blink.Cooldown > 9)
                            {
                                staff.UseAbility(enemy);
                                Utils.Sleep(1000, "staff");
                            }
                        }
                    }
                    if (Medallion != null && Utils.SleepCheck("solar") && Medallion.CanBeCasted())
                    {
                        foreach (var ally in allies)
                        {
                            if (isCarry(ally) && ally.IsAttacking())
                            {
                                foreach (var enemy in enemies)
                                {
                                    if (IsFacing(ally, enemy) && Utils.SleepCheck("solar") && Medallion.CanBeCasted())
                                    {
                                        Medallion.UseAbility(enemy);
                                        Utils.Sleep(1000, "solar");
                                    }
                                }
                            }
                        }
                        if (isCarry(me) && me.IsAttacking())
                        {
                            foreach (var enemy in enemies)
                            {
                                if (IsFacing(me, enemy) && enemy.Distance2D(me) < 900 && Utils.SleepCheck("solar") && Medallion.CanBeCasted())
                                {
                                    Medallion.UseAbility(enemy);
                                    Utils.Sleep(1000, "solar");
                                }
                            }
                        }
                    }
                    if (diffusal != null && Utils.SleepCheck("diffusal") && diffusal.CanBeCasted())
                    {
                        foreach (var ally in allies)
                        {
                            if (isCarry(ally) && ally.IsAttacking())
                            {
                                foreach (var enemy in enemies)
                                {
                                    if (IsFacing(ally, enemy) && enemy.Distance2D(me) < 600 && Utils.SleepCheck("solar") && diffusal.CanBeCasted())
                                    {
                                        diffusal.UseAbility(enemy);
                                        Utils.Sleep(1000, "diffusal");
                                    }
                                }
                            }
                        }
                        if (isCarry(me) && (me.IsAttacking() || me.HasModifier("modifier_pangolier_swashbuckle")))
                        {
                            foreach (var enemy in enemies)
                            {
                                if (IsFacing(me, enemy) && enemy.Distance2D(me) < 600 && Utils.SleepCheck("diffusal") && diffusal.CanBeCasted())
                                {
                                    diffusal.UseAbility(enemy);
                                    Utils.Sleep(1000, "diffusal");
                                }
                            }
                        }
                    }
                }
            }
            
        }

        public static bool isCarry(Hero enemy)
        {
            if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Lycan || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Slark || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Sven || enemy.ClassId == ClassId.CDOTA_Unit_Hero_AntiMage || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Sniper || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Enchantress
                            || enemy.ClassId == ClassId.CDOTA_Unit_Hero_TemplarAssassin || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Clinkz || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Razor || enemy.ClassId == ClassId.CDOTA_Unit_Hero_DragonKnight || enemy.ClassId == ClassId.CDOTA_Unit_Hero_ChaosKnight || enemy.ClassId == ClassId.CDOTA_Unit_Hero_PhantomLancer|| enemy.ClassId == ClassId.CDOTA_Unit_Hero_DrowRanger || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Legion_Commander
                            || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Life_Stealer || enemy.ClassId == ClassId.CDOTA_Unit_Hero_MonkeyKing || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Ursa || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Weaver || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Windrunner
                            || enemy.ClassId == ClassId.CDOTA_Unit_Hero_SkeletonKing || enemy.ClassId == ClassId.CDOTA_Unit_Hero_EmberSpirit || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Riki || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Terrorblade || enemy.ClassId == ClassId.CDOTA_Unit_Hero_TrollWarlord || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Huskar || enemy.ClassId == ClassId.CDOTA_Unit_Hero_PhantomAssassin
                            || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Obsidian_Destroyer || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Luna || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Pangolier || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Bristleback || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Bloodseeker || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Tiny || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Furion || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Spectre || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Juggernaut || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Alchemist || enemy.ClassId == ClassId.CDOTA_Unit_Hero_FacelessVoid)
            {
                return true;
            }
            return false;
        }

        public static bool isCarryMad(Hero enemy)
        {
            if(enemy.ClassId == ClassId.CDOTA_Unit_Hero_TrollWarlord && enemy.HasModifier("modifier_troll_warlord_battle_trance"))
            {
                return true;
            }
            return false;
        }

        public static bool isNuker(Hero enemy)
        {
            if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Morphling || enemy.ClassId == ClassId.CDOTA_Unit_Hero_QueenOfPain || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Necrolyte || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Invoker
                || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Batrider || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Juggernaut || enemy.ClassId == ClassId.CDOTA_Unit_Hero_StormSpirit || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Tinker)
            {
                return true;
            }
            return false;
        }
        public static bool hasLowHeahth(Hero hero) {
            if(hero.Health < hero.MaximumHealth * 40 / 100)
            {
                return true;
            }
            return false;
        }

        public static Unit getFountain(Hero me)
        {
            if(fountain == null)
            {
                fountain = ObjectManager.GetEntitiesFast<Unit>()
                          .FirstOrDefault(
                              x => x.Team == me.Team
                                   && x.ClassId == ClassId.CDOTA_Unit_Fountain);
            }
            return fountain;
           

        }
    }
}
