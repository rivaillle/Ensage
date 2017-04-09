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
            if(dodge == true && destiny.ClassID != me.ClassID)
            {
                foreach (var enemy in enemies)
                {
                    if(IsFacing(destiny, enemy))
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
                    if (enemy.ClassID == ClassID.CDOTA_Unit_Hero_PhantomAssassin)
                    {
                        if (IsFacing(enemy, destiny) && (enemy.Spellbook.SpellW.IsInAbilityPhase || enemy.HasModifier("modifier_phantom_assassin_phantom_strike")))
                        {
                            useItem(ghost, target);
                            Utils.Sleep(1000, ghost.Name);
                            return true;
                        }
                    }
                    if (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Legion_Commander)
                    {
                        var duel = enemy.Spellbook.SpellR;
                        if (IsFacing(enemy, destiny) && (duel.IsInAbilityPhase || (enemy.Distance2D(me) <= 200 && duel.CanBeCasted())))
                        {
                            useItem(ghost, target);
                            Utils.Sleep(1000, ghost.Name);
                            return true;
                        }
                    }
                    else if (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Slark)
                    {
                        if (enemy.Distance2D(destiny) < 300 && destiny.HasModifier("modifier_slark_pounce_leash") && Utils.SleepCheck("pounce_dodged"))
                        {
                            useItem(ghost, target);
                            Utils.Sleep(1000, ghost.Name);
                            Utils.Sleep(5000, "pounce_dodged");
                            return true;
                        }
                    }
                    else if (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Juggernaut && enemy.Distance2D(destiny) < 400 && Utils.SleepCheck("omnislash_dodged_" + destiny.ClassID) && (enemy.Spellbook.SpellR.IsInAbilityPhase || enemy.HasModifier("modifier_juggernaut_omnislash")))
                    {
                        Console.WriteLine("dodge jugg with"+ ghost.Name);
                        useItem(ghost, target);
                        Utils.Sleep(1000, ghost.Name);
                        Utils.Sleep(5000, "omnislash_dodged_" + destiny.ClassID);
                        return true;
                    }
                    else if (dodge == true && (enemy.ClassID != ClassID.CDOTA_Unit_Hero_Juggernaut || Utils.SleepCheck("omnislash_dodged_" + destiny.ClassID)) && IsFacing(enemy, destiny) && enemy.Spellbook.Spells.Any(x => x.IsInAbilityPhase))
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
            if(target == null)
            {
                item.UseAbility();
            }else
            {
                item.UseAbility(target);
            }
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
                    "modifier_silencer_curse_of_the_silent", "modifier_silencer_last_word", "modifier_bane_fiends_grip",
                    "modifier_earth_spirit_magnetize", "modifier_jakiro_macropyre", "modifier_nerolyte_reapers_scythe",
                    "modifier_batrider_flaming_lasso", "modifier_sniper_assassinate", "modifier_pudge_dismember",
                    "modifier_enigma_black_hole_pull", "modifier_disruptor_static_storm", "modifier_sand_king_epicenter",
                    "modifier_bloodseeker_rupture", "modifier_dual_breath_burn", "modifier_jakiro_liquid_fire_burn",
                    "modifier_axe_battle_hunger", "modifier_viper_poison_attack",
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

            return (Math.PI - Math.Abs(Math.Atan2(n1, n2))) < 0.2;
        }

        private static bool CanGoInvis(Hero ally)
        {
            if (ally.ClassID == ClassID.CDOTA_Unit_Hero_Clinkz || ally.ClassID == ClassID.CDOTA_Unit_Hero_Treant || ally.ClassID == ClassID.CDOTA_Unit_Hero_Riki || ally.ClassID == ClassID.CDOTA_Unit_Hero_BountyHunter
                    || ally.HasModifier("modifier_item_invisibility_edge_windwalk") || ally.HasModifier("modifier_item_silver_edge_windwalk"))
            {
                return true;
            }
            return false;
        }

        public static bool isCarry(Hero enemy)
        {
            if (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Slark || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Sven || enemy.ClassID == ClassID.CDOTA_Unit_Hero_AntiMage || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Sniper
                            || enemy.ClassID == ClassID.CDOTA_Unit_Hero_TemplarAssassin || enemy.ClassID == ClassID.CDOTA_Unit_Hero_DragonKnight || enemy.ClassID == ClassID.CDOTA_Unit_Hero_DrowRanger || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Legion_Commander
                            || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Life_Stealer || enemy.ClassID == ClassID.CDOTA_Unit_Hero_MonkeyKing || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Ursa || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Weaver || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Windrunner
                            || enemy.ClassID == ClassID.CDOTA_Unit_Hero_SkeletonKing || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Riki || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Terrorblade || enemy.ClassID == ClassID.CDOTA_Unit_Hero_TrollWarlord || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Huskar || enemy.ClassID == ClassID.CDOTA_Unit_Hero_PhantomAssassin
                            || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Juggernaut || enemy.ClassID == ClassID.CDOTA_Unit_Hero_FacelessVoid)
            {
                return true;
            }
            return false;
        }
    }
}
