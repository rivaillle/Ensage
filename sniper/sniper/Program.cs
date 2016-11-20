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
        private const Key saveSelfKey = Key.Y;
        private static Hero me;
        private static Ability assassination;
        private static Ability takeAim;
        private static double[] assassinationDamagePerLevel = new double[] { 320, 485, 650 };
        private static double[] extraAimPerLevel = new double[] { 100, 200, 300, 400 };
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
            Phase,
            Hurrikane,
            DragonLance;

        private static Hero needMana;
        private static Hero needMeka;
        private static Hero target;
        private static bool supportActive;
        private static bool includeSaveSelf;
        private static bool shouldCastLotusOrb;
        private static bool shouldCastGlimmerCape;
        private static Unit creepTarget;

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
            Phase = null;
            Hurrikane = null;
            DragonLance = null;
            loaded = false;
            supportActive = true;
            includeSaveSelf = false;
            shouldCastLotusOrb = false;
            shouldCastGlimmerCape = false;

            assassination = null;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (loaded)
            {
                var mode = supportActive ? "ON" : "OFF";
                var includeSelfMode = includeSaveSelf ? "ON" : "OFF";
                Drawing.DrawText("Sniper Sharp is: " + supportActive + ". Hotkey (Toggle): " + toggleKey + "",
                    new Vector2(Drawing.Width * 5 / 100, Drawing.Height * 4 / 100), Color.LightGreen, FontFlags.DropShadow);
                Drawing.DrawText(
                    "Include Saving yourself?: " + includeSelfMode + ". Hotkey (TOGGLE): " + saveSelfKey + "",
                    new Vector2(Drawing.Width * 5 / 100, Drawing.Height * 8 / 100), Color.LightGreen, FontFlags.DropShadow);
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
            assassination = me.Spellbook.SpellR;
            takeAim = me.Spellbook.SpellE;
            Stick = me.FindItem("item_magic_stick");
            Wand = me.FindItem("item_magic_wand");
            Phase = me.FindItem("item_phase_boots");
            DragonLance = me.FindItem("item_dragon_lance");
            Hurrikane = me.FindItem("item_hurricane_pike");
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

              
            }

            if (supportActive && me.IsAlive)
            
            {
                if (Phase != null && Phase.CanBeCasted() && !me.IsAttacking() &&
                    !me.IsChanneling() && me.NetworkActivity == NetworkActivity.Move && IsInDanger2(me) && Utils.SleepCheck("phaseboots"))
                {
                    Phase.UseAbility();
                    Utils.Sleep(500, "phaseboots");
                }

                double assassinationDamage = 0;
                if (assassination.Level > 0)
                {
                    assassinationDamage = assassinationDamagePerLevel[assassination.Level - 1];
                }
                double extraRange = 0;
                if (takeAim.Level > 0)
                {
                    extraRange = extraAimPerLevel[takeAim.Level - 1];
                }
                if(DragonLance != null || Hurrikane != null)
                {
                    extraRange += 140;
                }
                if (Utils.SleepCheck("debug"))
                {
                    foreach(var item in me.Inventory.Items)
                    {
                        Console.WriteLine(item.Name);
                        Utils.Sleep(1000, "debug");
                    }
                }
                var orbwalker = Orbwalking.new(me, false);
                double totalRange = me.AttackRange + extraRange;
                if (!me.IsAttacking() && !me.IsChanneling() && me.CanCast() && !Game.IsKeyDown(Key.Space) && !IsInDanger2(me) && Utils.SleepCheck("assassination"))
                {
                    var myEnemyList =
                                        ObjectMgr.GetEntities<Hero>()
                                            .Where(
                                                x =>
                                                    x.Team != me.Team && !x.IsIllusion && x.IsAlive &&
                                                    me.Distance2D(x) <= assassination.CastRange && me.Distance2D(x) > totalRange)
                                            .ToList();

                    if (myEnemyList.Any())
                    {
                        foreach (var enemy in myEnemyList)
                        {
                            var enemyResistence = enemy.MagicDamageResist;
                            double totalDamage = assassinationDamage - assassinationDamage * enemyResistence;

                            totalDamage += totalDamage * (me.Intelligence % 16) / 100;
                            Console.WriteLine(totalDamage);
                            if (totalDamage >= (enemy.Health + enemy.HealthRegeneration * 2))
                            {
                                Console.WriteLine(totalDamage);
                                assassination.UseAbility(enemy);
                                Utils.Sleep(1000, "assassination");
                            }
                            //Console.WriteLine("total damage is:" + totalDamage);

                        }
                    }
                }

            }
        }
   
     

        private static bool IsInDanger2(Hero ally)
        {
            if (ally != null && ally.IsAlive)
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
                        //Console.WriteLine(ally.Name + " has modifier: " + buff.Name);
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
            if ((hero == ClassID.CDOTA_Unit_Hero_Sniper))
            {
                return true;
            }
            return false;
        }
    }
}