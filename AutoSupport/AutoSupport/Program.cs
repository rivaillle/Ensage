using System;
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
        private static ParticleEffect rangeDisplay;
        private static Boolean rangeWithAether = false;

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
        private static bool shouldCastLotusOrb;
        private static bool shouldCastGlimmerCape;

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

                if (rangeDisplay == null)
                {
                    rangeDisplay = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                    rangeDisplay.SetControlPoint(1, new Vector3(255, 255, 255));
                    rangeDisplay.SetControlPoint(2, new Vector3(550, 255, 0));
                }
                else if (me.HasModifier("modifier_item_aether_lens") && rangeWithAether == false)
                {
                    Console.WriteLine("hohoho");
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
                            CastHeal(misticAbility, enemy);
                        }
                        if (enemy.IsLinkensProtected())
                        {
                            CastHeal(misticAbility, enemy);
                        }   

                    }
                }
                
                uint addedRange = 0;
                if (Support(me.ClassID))
                {
                    switch (me.ClassID)
                    {
                        case ClassID.CDOTA_Unit_Hero_Abaddon:
                            if (me.HasModifier("modifier_item_aether_lens"))
                            {
                                addedRange += 220;
                            }
                            Save(me, me.Spellbook.SpellW, 1000, me.Spellbook.SpellW.CastRange + addedRange);
                            Heal(me, me.Spellbook.SpellQ, new float[] { 100, 150, 200, 250 },
                                800 + addedRange);                           
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
           
        }

        private static void Save(Hero self, Ability saveSpell, float castTime = 800, uint castRange = 0)
        {
            if (saveSpell != null && saveSpell.CanBeCasted())
            {
                if (self.IsAlive && !self.IsChanneling() &&
                    (!self.IsInvisible()))
                {
                    var allies =
                        ObjectManager.GetEntitiesFast<Hero>()
                            .Where(
                                x =>
                                    x.Team == self.Team && IsInDanger2(x) && !x.IsIllusion && x.IsAlive)
                            .ToList();
                    
                    if (allies.Any())
                    {
                        foreach (var ally in allies)
                        {
                            if (Utils.SleepCheck("armor") && Utils.SleepCheck("saveduration") && self.Distance2D(ally) <= 1000 && Medallion != null && Medallion.CanBeCasted())
                            {
                                Medallion.UseAbility(ally);
                                Utils.Sleep(1000, "armor");
                            }

                            if (Utils.SleepCheck("saveduration") && self.Distance2D(ally) <= castRange)
                            {
                                saveSpell.UseAbility(ally);
                                Utils.Sleep(castTime + Game.Ping, "saveduration");
                            }
                            
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
                                    entity.IsAlive && entity.ClassID != me.ClassID).ToList();

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
            if (ally != null && ally.IsAlive && ally.ClassID != me.ClassID)
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

        private static bool Support(ClassID hero)
        {
            if (hero == ClassID.CDOTA_Unit_Hero_Abaddon)
            {
                return true;
            }
            return false;
        }

        private static bool IsFacing(Hero hero, Hero enemy)
        {

            float deltaY = hero.Position.Y - enemy.Position.Y;
            float deltaX = hero.Position.X - enemy.Position.X;
            float angle = (float)(Math.Atan2(deltaY, deltaX));

            float n1 = (float)Math.Sin(hero.RotationRad - angle);
            float n2 = (float)Math.Cos(hero.RotationRad - angle);

            return (Math.PI - Math.Abs(Math.Atan2(n1, n2))) < 0.2;
        }

        private static bool CanGoInvis(Hero ally) {
            if (ally.ClassID == ClassID.CDOTA_Unit_Hero_Clinkz || ally.ClassID == ClassID.CDOTA_Unit_Hero_Riki || ally.ClassID == ClassID.CDOTA_Unit_Hero_BountyHunter
                    || ally.HasModifier("modifier_item_invisibility_edge_windwalk") || ally.HasModifier("modifier_item_silver_edge_windwalk"))
            {
                return true;
            }
            return false;
        }
}
}