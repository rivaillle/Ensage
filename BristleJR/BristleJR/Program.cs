using System;
using System.Linq;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using SharpDX;
using System.Windows.Input;
using System.Collections.Generic;

namespace BristleJR
{
    internal class Program
    {
        private static Ability Quill, Goo;
        private static Hero _source, _target;
        private static Item abyssal, solar, medallion, halberd, atos, dust, Stick, Wand, Crimson, Quelling, bladeMail;
        private static Ensage.Items.PowerTreads threads;
        private const Key triggerKey = Key.B;
        private const Key chaseKey = Key.G;
        private static bool trigger, chase;
        private static readonly uint[] Quilldmg = { 20, 40, 60, 80 };
        private static readonly Menu Menu = new Menu("Bristleback", "bristle", true);
        private static double threadsSwitchThreshold = 0.35;
        static void Main(string[] args)
        {
            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Game.PrintMessage("Bristleback Sharp by <font color='#ff1111'>Spyware293</font> Loaded !!", MessageType.LogMessage);
            Player.OnExecuteOrder += Player_OnExecuteAction;
            var menu_utama = new Menu("Options", "opsi");
            menu_utama.AddItem(new MenuItem("Quill", "Quill").SetValue(new StringList(new[] { "Max", "Smart", "Disable", "Farm", "Smart and Farm" })));
            menu_utama.AddItem(new MenuItem("enable", "enable").SetValue(true));
            Menu.AddSubMenu(menu_utama);
            Menu.AddToMainMenu();
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (!Game.IsChatOpen)
            {
                if (Game.IsKeyDown(triggerKey))
                {
                    trigger = true;
                }
                else
                {
                    trigger = false;
                }

                if (Game.IsKeyDown(chaseKey))
                {
                    chase = true;
                }
                else
                {
                    chase = false;
                }
            }
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            _source = ObjectManager.LocalHero;

            if (!Game.IsInGame || Game.IsPaused || Game.IsWatchingGame)
            {
                return;
            }
            if (_source == null ||_source.ClassID != ClassID.CDOTA_Unit_Hero_Bristleback)
            {
                return;
            }
            var _enemy = ObjectManager.GetEntitiesFast<Hero>().Where(hero => hero.IsAlive && !hero.IsIllusion && hero.IsVisible && hero.Team != _source.Team);
            var _creep = ObjectManager.GetEntitiesFast<Creep>().Where(x => (x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege) && x.IsAlive && x.IsSpawned && x.IsVisible).ToList();
            
            if (Quill == null)
            {
                Quill = _source.Spellbook.Spell2;
            }
            if (Goo == null)
            {
                Goo = _source.Spellbook.Spell1;
            }
            if (abyssal == null)
            {
                abyssal = _source.FindItem("item_abyssal_blade");
            }
            if (dust == null)
            {
                dust = _source.FindItem("item_dust");
            }
            if (atos == null)
            {
                atos = _source.FindItem("item_rod_of_atos");
            }
            if (solar == null)
            {
                solar = _source.FindItem("item_solar_crest");
            }
            if (medallion == null)
            {
                medallion = _source.FindItem("item_medallion_of_courage");
            }
            if (halberd == null)
            {
                halberd = _source.FindItem("item_heavens_halberd");
            }
            if(threads == null)
            {
                threads = (Ensage.Items.PowerTreads)_source.FindItem("item_power_treads");
            }
           
            Stick = _source.FindItem("item_magic_stick");            
            Wand = _source.FindItem("item_magic_wand");
            
            if(Crimson == null)
            {
                Crimson = _source.FindItem("item_crimson_guard");
            }
            
            Quelling = _source.FindItem("item_quelling_blade");
            if(Quelling == null)
            {
                Quelling = _source.FindItem("item_iron_talon");
            }
            bladeMail = _source.FindItem("item_blade_mail");


            if (IsInDanger(_source, _enemy) && _source.Health <= _source.MaximumHealth * 0.35)
            {
                if (Stick != null && Utils.SleepCheck("Stick") && Stick.CurrentCharges > 0 && Stick.Cooldown > 0)
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

            var selectedIndex = Menu.Item("Quill").GetValue<StringList>().SelectedIndex;
            if (selectedIndex == 3 && Quill.CanBeCasted() && _source.CanCast() && Utils.SleepCheck("quill") && !_source.IsChanneling() && !_source.IsInvisible())
            {

                foreach (var x in _creep)
                {
                    if (x.Team == _source.GetEnemyTeam() && x.Health > 0 && x.Health < (Quilldmg[Quill.Level - 1] * (1 - x.DamageResist) + 20) && _source.Distance2D(x) < Quill.CastRange && Utils.SleepCheck("quill"))
                    {
                        useAbility(Quill);
                        Utils.Sleep(150 + Game.Ping, "quill");
                    }
                }
            }
            else if (selectedIndex == 0 && Quill.CanBeCasted() && _source.CanCast() && Utils.SleepCheck("quill") && !_source.IsChanneling() && !_source.IsInvisible())
            {
                /*
                useAbility(Quill);
                Utils.Sleep(150 + Game.Ping, "quill");*/
            }
            else if (selectedIndex == 4 && Quill.CanBeCasted() && _source.CanCast() && Utils.SleepCheck("quill") && !_source.IsChanneling() && !_source.IsInvisible())
            {
                foreach (var enemy in _enemy)
                {
                    if (Utils.SleepCheck("quill") && _source.Distance2D(enemy) < Quill.CastRange)
                    {
                        useAbility(Quill);                        
                        Utils.Sleep(150 + Game.Ping, "quill");
                    }
                }

                if (Utils.SleepCheck("quill"))
                {
                    foreach (var x in _creep)
                    {
                        if (x.Team == _source.GetEnemyTeam() && x.Health > 0 && x.Health < (Quilldmg[Quill.Level - 1] * (1 - x.DamageResist) + 20) && _source.Distance2D(x) < Quill.CastRange && Utils.SleepCheck("quill"))
                        {
                            useAbility(Quill);
                            Utils.Sleep(150 + Game.Ping, "quill");
                        }
                    }
                }

            }
            else if (selectedIndex == 1 && Quill.CanBeCasted() && _source.CanCast() && Utils.SleepCheck("quill") && !_source.IsChanneling() && !_source.IsInvisible())
            {
                foreach (var enemy in _enemy)
                {
                    if (Utils.SleepCheck("quill") && _source.Distance2D(enemy) < Quill.CastRange)
                    {
                        useAbility(Quill);
                        Utils.Sleep(150 + Game.Ping, "quill");
                    }
                }

            }
            if (trigger && Utils.SleepCheck("quill") && Quill.CanBeCasted() && Menu.Item("enable").GetValue<bool>())
            {
                useAbility(Quill);
                Utils.Sleep(150 + Game.Ping, "quill");

            }
            if (Utils.SleepCheck("threads") && threads != null && threads.ActiveAttribute != Ensage.Attribute.Strength)
            {
                setThreads(_source, threads, Ensage.Attribute.Strength);
            }
            /*
            foreach(var item in _source.Inventory.Items.ToList())
            {
                Console.WriteLine(item.Name);
            }
            */
            if (Crimson != null || bladeMail != null || halberd != null)
            {
                foreach (var enemy in _enemy)
                {
                    if (Crimson != null)
                    {
                        if (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Juggernaut && _source.Distance2D(enemy) < 400 && enemy.HasModifier("modifier_juggernaut_omnislash") && Utils.SleepCheck("crimson"))
                        {
                            Crimson.UseAbility();
                            Utils.Sleep(4000 + Game.Ping, "crimson");
                            /*
                        foreach(var modifier in enemy.Modifiers.ToList())
                        {
                            Console.WriteLine(modifier.Name);
                        }
                        */
                        }
                        else if ((enemy.ClassID == ClassID.CDOTA_Unit_Hero_Sven || enemy.ClassID == ClassID.CDOTA_Unit_Hero_Legion_Commander) && _source.Distance2D(enemy) < 400 && Utils.SleepCheck("crimson"))
                        {
                            var blink = enemy.FindItem("item_blink");
                            if (blink != null && blink.Cooldown > 8)
                            {
                                if (!((enemy.ClassID == ClassID.CDOTA_Unit_Hero_Sven && !enemy.HasModifier("modifier_sven_gods_strength")) || (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Legion_Commander && !(enemy.Spellbook.SpellR.CanBeCasted()))))
                                {
                                    Crimson.UseAbility();
                                    Utils.Sleep(4000 + Game.Ping, "crimson");
                                }

                            }
                        }
                    }
                    if (bladeMail != null)
                    {
                        if ((enemy.ClassID == ClassID.CDOTA_Unit_Hero_Axe && _source.Distance2D(enemy) < 350 && Utils.SleepCheck("blademail") && enemy.Spellbook.SpellQ.CanBeCasted()))
                        {
                            var blink = enemy.FindItem("item_blink");
                            if (blink != null && blink.Cooldown > 8)
                            {
                                bladeMail.UseAbility();
                                Utils.Sleep(1000 + Game.Ping, "blademail");
                            }
                                
                        }
                    }
                    if(halberd != null)
                    {
                        if ((enemy.ClassID == ClassID.CDOTA_Unit_Hero_Legion_Commander) && _source.Distance2D(enemy) <= 600 && Utils.SleepCheck("heaven"))
                        {
                            if (_source.Distance2D(enemy) <= 150 && (enemy.Spellbook.SpellR.CanBeCasted()) && IsFacing(enemy, _source))
                            {
                                halberd.UseAbility(enemy);
                            }
                            else if (blink != null && blink.Cooldown > 8)
                            {
                                if (!((enemy.ClassID == ClassID.CDOTA_Unit_Hero_Sven && !enemy.HasModifier("modifier_sven_gods_strength")) || (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Legion_Commander && !(enemy.Spellbook.SpellR.CanBeCasted()))))
                                {
                                    Crimson.UseAbility();
                                    Utils.Sleep(4000 + Game.Ping, "crimson");
                                }

                            }
                        }
                    }
                }
            }
            
            if (Quelling != null && Quelling.Cooldown == 0)
            {
                var wards =
                                        ObjectManager.GetEntitiesFast<Unit>()
                                            .Where(
                                                x =>
                                                    x.Team != _source.Team && (x.ClassID == ClassID.CDOTA_NPC_Observer_Ward || x.ClassID == ClassID.CDOTA_NPC_Observer_Ward_TrueSight) &&
                                                    _source.Distance2D(x) <= 475)
                                            .ToList();
                if (wards.Any() && Utils.SleepCheck("deward"))
                {
                    Quelling.UseAbility(wards[0]);
                    Utils.Sleep(1000, "deward");
                }
            }           
        }

        public static void useAbility(Ability ability)
        {
            if (ability.CanBeCasted())
            {
                setThreads(_source, threads, Ensage.Attribute.Intelligence);
                ability.UseAbility();
            }
        }

        public static void setThreads(Hero me, Ensage.Items.PowerTreads powerTreads, Ensage.Attribute attribute)
        {
            if(powerTreads != null && me.Health >= me.MaximumHealth * threadsSwitchThreshold)
            {
                 
                var currentAttribute = threads.ActiveAttribute;
                switch (attribute)
                {
                    case Ensage.Attribute.Agility:
                        if (currentAttribute == Ensage.Attribute.Strength)
                        {
                            powerTreads.UseAbility();
                            powerTreads.UseAbility();
                        }
                        else if (currentAttribute == Ensage.Attribute.Intelligence)
                        {
                            powerTreads.UseAbility();
                        }
                        break;
                    case Ensage.Attribute.Strength:
                        if (currentAttribute == Ensage.Attribute.Intelligence)
                        {
                            powerTreads.UseAbility();
                            powerTreads.UseAbility();
                        }
                        else if (currentAttribute == Ensage.Attribute.Agility)
                        {
                            powerTreads.UseAbility();
                        }
                        break;
                    case Ensage.Attribute.Intelligence:
                        if (currentAttribute == Ensage.Attribute.Agility)
                        {
                            powerTreads.UseAbility();
                            powerTreads.UseAbility();
                        }
                        else if (currentAttribute == Ensage.Attribute.Strength)
                        {
                            powerTreads.UseAbility();
                        }
                        break;
                }

                Utils.Sleep(150 + Game.Ping, "threads");
            }
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
        private static void Player_OnExecuteAction(Player sender, ExecuteOrderEventArgs args)
        {
            var me = sender.Hero;
            if (me.IsInvisible())
            {
                return;
            }
            switch (args.Order)
            {
                case Order.TransferItem:
                case Order.MoveItem:
                case Order.DropItem:
                case Order.PickItem:
                case Order.BuyItem:
                case Order.AttackTarget:
                case Order.AttackLocation:
                case Order.AbilityTarget:
                case Order.AbilityLocation:
                case Order.Ability:
                    if (threads != null)
                    {
                        if(me.Health >= me.MaximumHealth * threadsSwitchThreshold)
                        {
                            setThreads(me, threads, Ensage.Attribute.Intelligence);
                        }
                    }
                    break;
                case Order.ToggleAbility:
                case Order.MoveLocation:
                case Order.MoveTarget:
                default:
                    break;
            }
        }
        private static bool IsFacing(Hero hero, Hero enemy)
        {

            float deltaY = hero.Position.Y - enemy.Position.Y;
            float deltaX = hero.Position.X - enemy.Position.X;
            float angle = (float)(Math.Atan2(deltaY, deltaX));

            float n1 = (float)Math.Sin(hero.RotationRad - angle);
            float n2 = (float)Math.Cos(hero.RotationRad - angle);

            return (Math.PI - Math.Abs(Math.Atan2(n1, n2))) < 0.1;
        }
    }
}