using System;
using System.Collections.Generic;
using System.Linq;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using SharpDX;
using Ensage.Common.Menu;

namespace ChallengeAccepted
{
    class ChallengeAccepted
    {
        private static readonly Menu Menu = new Menu("Challenge Accepted", "ChallengeAccepted", true, "npc_dota_hero_legion_commander", true);
        private static readonly Menu _item_config = new Menu("Duel Items", "Duel Items");
        private static readonly Menu _2_item_config = new Menu("Pop Linkens: ", "Pop Linkens: ");
        private static readonly Menu _3_item_config = new Menu("Mana/Health Itens: ", "Mana/Health Itens: ");
        private static readonly Menu _skill_config = new Menu("Skills", "Skills");
        private static Ability Duel, Heal, Odds;
        private static Item blink, armlet, blademail, bkb, abyssal, mjollnir, halberd, medallion, madness, urn, satanic, solar, dust, sentry, mango, arcane, buckler, crimson, lotusorb, cheese, magistick, magicwand, soulring, force, cyclone, vyse, atos, difusal;
        private static Hero me, target;
        private static readonly Dictionary<string, bool> duel_items = new Dictionary<string, bool>
            {
                {"item_blink",true},
                {"item_armlet",true},
                {"item_abyssal_blade",true},
                {"item_mjollnir",true},
            };
        private static readonly Dictionary<string, bool> duel_items2 = new Dictionary<string, bool>
            {
                {"item_medallion_of_courage",true},
                {"item_mask_of_madness",true},
                {"item_urn_of_shadows",true},
                {"item_solar_crest",true}
            };
        private static readonly Dictionary<string, bool> duel_items3 = new Dictionary<string, bool>
            {
                {"item_black_king_bar",true},
                {"item_blade_mail",true},
                {"item_satanic",true}
            };
        private static readonly Dictionary<string, bool> duel_items4 = new Dictionary<string, bool>
            {
                {"item_lotus_orb",true},
                {"item_magic_stick",true},
                {"item_magic_wand",true}
            };
        private static readonly Dictionary<string, bool> pop_linkens_itens = new Dictionary<string, bool>
            {
                {"item_sheepstick",true},
                {"item_abyssal_blade",true},
                {"item_diffusal_blade",true},
                {"item_rod_of_atos",true}
            };
        private static readonly Dictionary<string, bool> pop_linkens_itens2 = new Dictionary<string, bool>
            {
                {"item_heavens_halberd",true},
                {"item_force_staff",true},
                {"item_cyclone",true},
            };
        private static readonly Dictionary<string, bool> skills = new Dictionary<string, bool>
            {
                {"legion_commander_press_the_attack",true},
                {"legion_commander_overwhelming_odds",true}
            };
        private static readonly Dictionary<string, bool> healormana_items = new Dictionary<string, bool>
            {
                {"item_buckler",true},
                {"item_crimson_guard",true},
                {"item_cheese",true},
                {"item_soul_ring",true}
            };
        private static readonly Dictionary<string, bool> healormana_items2 = new Dictionary<string, bool>
            {
                {"item_dust",true},
                {"item_ward_sentry",true},
                {"item_enchanted_mango",true},
                {"item_arcane_boots",true}
            };
        static void Main(string[] args)
        {
            Menu.AddItem(new MenuItem("DUEL!", "DUEL!").SetValue(new KeyBind('D', KeyBindType.Press)));
            Menu.AddItem(new MenuItem("Black King Bar Toggle", "Black King Bar Toggle").SetValue(new KeyBind('F', KeyBindType.Press)));
            Menu.AddSubMenu(_item_config);
            Menu.AddSubMenu(_2_item_config);
            Menu.AddSubMenu(_3_item_config);
            Menu.AddSubMenu(_skill_config);
            _item_config.AddItem(new MenuItem("Duel Items", " ").SetValue(new AbilityToggler(duel_items)));
            _item_config.AddItem(new MenuItem("Duel Items2", " ").SetValue(new AbilityToggler(duel_items2)));
            _item_config.AddItem(new MenuItem("Duel Items3", " ").SetValue(new AbilityToggler(duel_items3)));
            _item_config.AddItem(new MenuItem("Duel Items4", " ").SetValue(new AbilityToggler(duel_items4)));
            _2_item_config.AddItem(new MenuItem("Pop Linkens: ", "Pop Linkens: ").SetValue(new AbilityToggler(pop_linkens_itens)));
            _2_item_config.AddItem(new MenuItem("Pop Linkens2: ", "Pop Linkens: ").SetValue(new AbilityToggler(pop_linkens_itens2)));
            _3_item_config.AddItem(new MenuItem("Mana/Health Itens: ", " ").SetValue(new AbilityToggler(healormana_items)));
            _3_item_config.AddItem(new MenuItem("Mana/Health Itens2: ", " ").SetValue(new AbilityToggler(healormana_items2)));
            _skill_config.AddItem(new MenuItem("Skills", "Skills").SetValue(new AbilityToggler(skills)));
            Menu.AddToMainMenu();
            PrintSuccess(">Challenge Accepted");
            //Game.OnUpdate += Working;
            Game.OnWndProc += Working;
            Drawing.OnDraw += markedfordeath;
        }
        public static void Working(EventArgs args)
        {
            if (!Game.IsInGame || Game.IsPaused || Game.IsWatchingGame)
                return;
            me = ObjectMgr.LocalHero;
            if (me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Legion_Commander)
                return;
            if (Game.IsKeyDown(Menu.Item("Black King Bar Toggle").GetValue<KeyBind>().Key) && !Game.IsChatOpen && Utils.SleepCheck("BKBTOGGLE"))
            {
                duel_items3["item_black_king_bar"] = !Menu.Item("Duel Items3").GetValue<AbilityToggler>().IsEnabled("item_black_king_bar");
                Utils.Sleep(750, "BKBTOGGLE");
            }
            if (Game.IsKeyDown(Menu.Item("DUEL!").GetValue<KeyBind>().Key) && !Game.IsChatOpen)
            {
                FindItems();
                target = me.ClosestToMouseTarget(1000);
                //if (Utils.SleepCheck("console"))
                //{
                //    Console.WriteLine("==================================================================");
                //    Console.WriteLine(target.Modifiers.LastOrDefault().Name);
                //    Console.WriteLine(me.Modifiers.LastOrDefault().Name);
                //    Console.WriteLine("==================================================================");
                //    Utils.Sleep(1200, "console");
                //}
                if (target != null && !target.IsInvul() && !IsPhysDamageImune(target) && (blink != null ? me.Distance2D(target) <= 1300 : me.Distance2D(target) <= 600))
                {
                    if (me.CanAttack() && me.CanCast())
                    {
                        if ((blink == null || blink.Cooldown > 0 || me.Distance2D(target) <= 400) && CanInvisCrit(me))
                            me.Attack(target);
                        else
                        {
                            manacheck();
                            if (target.IsLinkensProtected())
                            {
                                if (((cyclone.CanBeCasted() && Menu.Item("Pop Linkens2: ").GetValue<AbilityToggler>().IsEnabled(cyclone.Name)) || (force.CanBeCasted() && Menu.Item("Pop Linkens2: ").GetValue<AbilityToggler>().IsEnabled(force.Name)) || (halberd.CanBeCasted() && Menu.Item("Pop Linkens2: ").GetValue<AbilityToggler>().IsEnabled(halberd.Name)) || (vyse.CanBeCasted() && Menu.Item("Pop Linkens: ").GetValue<AbilityToggler>().IsEnabled(vyse.Name)) || (abyssal.CanBeCasted() && Menu.Item("Pop Linkens: ").GetValue<AbilityToggler>().IsEnabled(abyssal.Name)) || (atos.CanBeCasted() && Menu.Item("Pop Linkens: ").GetValue<AbilityToggler>().IsEnabled(atos.Name)) || (difusal.CanBeCasted() && Menu.Item("Pop Linkens: ").GetValue<AbilityToggler>().IsEnabled("item_diffusal_blade"))) && Utils.SleepCheck("Combo2"))
                                {
                                    if (blademail != null && blademail.Cooldown <= 0 && Menu.Item("Duel Items3").GetValue<AbilityToggler>().IsEnabled(blademail.Name) && me.Mana - blademail.ManaCost >= 75)
                                        blademail.UseAbility();
                                    if (satanic != null && satanic.Cooldown <= 0 && me.Health <= me.MaximumHealth * 0.5 && Menu.Item("Duel Items3").GetValue<AbilityToggler>().IsEnabled(satanic.Name))
                                        satanic.UseAbility();
                                    if (crimson != null && crimson.Cooldown <= 0 && Menu.Item("Mana/Health Itens: ").GetValue<AbilityToggler>().IsEnabled(crimson.Name))
                                        crimson.UseAbility();
                                    if (buckler != null && buckler.Cooldown <= 0 && Menu.Item("Mana/Health Itens: ").GetValue<AbilityToggler>().IsEnabled(buckler.Name) && me.Mana - buckler.ManaCost >= 75)
                                        buckler.UseAbility();
                                    if (lotusorb != null && lotusorb.Cooldown <= 0 && Menu.Item("Duel Items4").GetValue<AbilityToggler>().IsEnabled(lotusorb.Name) && me.Mana - lotusorb.ManaCost >= 75)
                                        lotusorb.UseAbility(me);
                                    if (mjollnir != null && mjollnir.Cooldown <= 0 && Menu.Item("Duel Items").GetValue<AbilityToggler>().IsEnabled(mjollnir.Name) && me.Mana - mjollnir.ManaCost >= 75)
                                        mjollnir.UseAbility(me);
                                    if (armlet != null && !armlet.IsToggled && Menu.Item("Duel Items").GetValue<AbilityToggler>().IsEnabled(armlet.Name) && Utils.SleepCheck("armlet"))
                                    {
                                        armlet.ToggleAbility();
                                        Utils.Sleep(300, "armlet");
                                    }
                                    if (madness != null && madness.Cooldown <= 0 && Menu.Item("Duel Items2").GetValue<AbilityToggler>().IsEnabled(madness.Name) && me.Mana - madness.ManaCost >= 75)
                                        madness.UseAbility();
                                    if (Heal != null && Heal.Level > 0 && Heal.Cooldown <= 0 && Menu.Item("Skills").GetValue<AbilityToggler>().IsEnabled(Heal.Name) && !me.IsMagicImmune() && me.Mana - Heal.ManaCost >= 75)
                                        Heal.UseAbility(me);
                                    if (bkb != null && bkb.Cooldown <= 0 && Menu.Item("Duel Items3").GetValue<AbilityToggler>().IsEnabled(bkb.Name) && (!Heal.CanBeCasted() || Heal == null || !Menu.Item("Skills").GetValue<AbilityToggler>().IsEnabled(Heal.Name)))
                                        bkb.UseAbility();
                                    if (blink != null && blink.Cooldown <= 0 && me.Distance2D(target) <= 1300 && me.Distance2D(target) >= 200 && Menu.Item("Duel Items").GetValue<AbilityToggler>().IsEnabled(blink.Name))
                                        blink.UseAbility(me.Distance2D(target.NetworkPosition) < 1200 ? target.NetworkPosition : new Vector3(me.NetworkPosition.X + 1150 * (float)Math.Cos(me.NetworkPosition.ToVector2().FindAngleBetween(target.NetworkPosition.ToVector2(), true)), me.NetworkPosition.Y + 1150 * (float)Math.Sin(me.NetworkPosition.ToVector2().FindAngleBetween(target.NetworkPosition.ToVector2(), true)), 100), false);
                                    if (urn != null && urn.CurrentCharges > 0 && Menu.Item("Duel Items2").GetValue<AbilityToggler>().IsEnabled(urn.Name))
                                        urn.UseAbility(target);
                                    if (solar != null && solar.CanBeCasted() && Menu.Item("Duel Items2").GetValue<AbilityToggler>().IsEnabled(solar.Name))
                                        solar.UseAbility(target);
                                    if (medallion != null && medallion.CanBeCasted() && Menu.Item("Duel Items2").GetValue<AbilityToggler>().IsEnabled(medallion.Name))
                                        medallion.UseAbility(target);
                                    if (cyclone != null && cyclone.CanBeCasted() && Utils.SleepCheck("CycloneRemoveLinkens") && Menu.Item("Pop Linkens2: ").GetValue<AbilityToggler>().IsEnabled(cyclone.Name) && me.Mana - cyclone.ManaCost >= 75)
                                    {
                                        cyclone.UseAbility(target);
                                        Utils.Sleep(100, "CycloneRemoveLinkens");
                                    }
                                    else if (atos != null && atos.CanBeCasted() && Utils.SleepCheck("atosRemoveLinkens") && Menu.Item("Pop Linkens: ").GetValue<AbilityToggler>().IsEnabled(atos.Name) && me.Mana - atos.ManaCost >= 75)
                                    {
                                        atos.UseAbility(target);
                                        Utils.Sleep(100, "atosRemoveLinkens");
                                    }
                                    else if (difusal != null && difusal.CanBeCasted() && Utils.SleepCheck("DifusalRemoveLinkens") && Menu.Item("Pop Linkens: ").GetValue<AbilityToggler>().IsEnabled("item_diffusal_blade"))
                                    {
                                        difusal.UseAbility(target);
                                        Utils.Sleep(600, "DifusalRemoveLinkens");
                                    }
                                    else if (force != null && force.CanBeCasted() && Utils.SleepCheck("ForceRemoveLinkens") && Menu.Item("Pop Linkens2: ").GetValue<AbilityToggler>().IsEnabled(force.Name) && me.Mana - force.ManaCost >= 75)
                                    {
                                        force.UseAbility(target);
                                        Utils.Sleep(100, "ForceRemoveLinkens");
                                    }
                                    else if (halberd != null && halberd.CanBeCasted() && Utils.SleepCheck("halberdLinkens") && Menu.Item("Pop Linkens2: ").GetValue<AbilityToggler>().IsEnabled(halberd.Name) && me.Mana - halberd.ManaCost >= 75)
                                    {
                                        halberd.UseAbility(target);
                                        Utils.Sleep(100, "halberdLinkens");
                                    }
                                    else if (vyse != null && vyse.CanBeCasted() && Utils.SleepCheck("vyseLinkens") && Menu.Item("Pop Linkens: ").GetValue<AbilityToggler>().IsEnabled(vyse.Name) && me.Mana - vyse.ManaCost >= 75)
                                    {
                                        vyse.UseAbility(target);
                                        Utils.Sleep(100, "vyseLinkens");
                                    }
                                    else if (abyssal != null && abyssal.CanBeCasted() && Utils.SleepCheck("abyssal") && Menu.Item("Pop Linkens: ").GetValue<AbilityToggler>().IsEnabled(abyssal.Name) && me.Mana - abyssal.ManaCost >= 75)
                                    {
                                        abyssal.UseAbility(target);
                                        Utils.Sleep(100, "abyssal");
                                    }
                                    Utils.Sleep(200, "Combo2");
                                }
                            }
                            else
                            {
                                if (UsedInvis(target))
                                {
                                    if (me.Distance2D(target) <= 500)
                                    {
                                        if (dust != null && dust.CanBeCasted() && Utils.SleepCheck("dust") && dust != null && Menu.Item("Mana/Health Itens2: ").GetValue<AbilityToggler>().IsEnabled(dust.Name))
                                        {
                                            dust.UseAbility();
                                            Utils.Sleep(100, "dust");
                                        }
                                        else if (sentry != null && sentry.CanBeCasted() && Utils.SleepCheck("sentry") && sentry != null && Menu.Item("Mana/Health Itens2: ").GetValue<AbilityToggler>().IsEnabled(sentry.Name))
                                        {
                                            sentry.UseAbility(me.Position);
                                            Utils.Sleep(100, "sentry");
                                        }
                                    }
                                }
                                uint elsecount = 1;
                                if (Utils.SleepCheck("combo"))
                                {
                                    if (blademail != null && blademail.Cooldown <= 0 && Menu.Item("Duel Items3").GetValue<AbilityToggler>().IsEnabled(blademail.Name) && me.Mana - blademail.ManaCost >= 75)
                                        blademail.UseAbility();
                                    else
                                        elsecount += 1;
                                    if (satanic != null && satanic.Cooldown <= 0 && me.Health <= me.MaximumHealth * 0.5 && Menu.Item("Duel Items3").GetValue<AbilityToggler>().IsEnabled(satanic.Name))
                                        satanic.UseAbility();
                                    else
                                        elsecount += 1;
                                    if (crimson != null && crimson.Cooldown <= 0 && Menu.Item("Mana/Health Itens: ").GetValue<AbilityToggler>().IsEnabled(crimson.Name))
                                        crimson.UseAbility();
                                    else
                                        elsecount += 1;
                                    if (buckler != null && buckler.Cooldown <= 0 && Menu.Item("Mana/Health Itens: ").GetValue<AbilityToggler>().IsEnabled(buckler.Name) && me.Mana - buckler.ManaCost >= 75)
                                        buckler.UseAbility();
                                    else
                                        elsecount += 1;
                                    if (lotusorb != null && lotusorb.Cooldown <= 0 && Menu.Item("Duel Items4").GetValue<AbilityToggler>().IsEnabled(lotusorb.Name) && me.Mana - lotusorb.ManaCost >= 75)
                                        lotusorb.UseAbility(me);
                                    else
                                        elsecount += 1;
                                    if (mjollnir != null && mjollnir.Cooldown <= 0 && Menu.Item("Duel Items").GetValue<AbilityToggler>().IsEnabled(mjollnir.Name) && me.Mana - mjollnir.ManaCost >= 75)
                                        mjollnir.UseAbility(me);
                                    else
                                        elsecount += 1;
                                    if (armlet != null && !armlet.IsToggled && Menu.Item("Duel Items").GetValue<AbilityToggler>().IsEnabled(armlet.Name) && Utils.SleepCheck("armlet"))
                                    {
                                        armlet.ToggleAbility();
                                        Utils.Sleep(300, "armlet");
                                    }
                                    else
                                        elsecount += 1;
                                    if (madness != null && madness.Cooldown <= 0 && Menu.Item("Duel Items2").GetValue<AbilityToggler>().IsEnabled(madness.Name) && me.Mana - madness.ManaCost >= 75)
                                        madness.UseAbility();
                                    else
                                        elsecount += 1;
                                    if (Heal != null && Heal.Level > 0 && Heal.Cooldown <= 0 && Menu.Item("Skills").GetValue<AbilityToggler>().IsEnabled(Heal.Name) && !me.IsMagicImmune() && me.Mana - Heal.ManaCost >= 75)
                                        Heal.UseAbility(me);
                                    else
                                        elsecount += 1;
                                    if (bkb != null && bkb.Cooldown <= 0 && Menu.Item("Duel Items3").GetValue<AbilityToggler>().IsEnabled(bkb.Name) && (!Heal.CanBeCasted() || Heal == null))
                                        bkb.UseAbility();
                                    else
                                        elsecount += 1;
                                    if (blink != null && blink.Cooldown <= 0 && me.Distance2D(target) <= 1300 && me.Distance2D(target) >= 200 && Menu.Item("Duel Items").GetValue<AbilityToggler>().IsEnabled(blink.Name))
                                        blink.UseAbility(target.Position);
                                    else
                                        elsecount += 1;
                                    if (abyssal != null && abyssal.Cooldown <= 0 && Menu.Item("Duel Items").GetValue<AbilityToggler>().IsEnabled(abyssal.Name) && me.Mana - abyssal.ManaCost >= 75)
                                        abyssal.UseAbility(target);
                                    else
                                        elsecount += 1;
                                    if (urn != null && urn.CanBeCasted() && urn.CurrentCharges > 0 && Menu.Item("Duel Items2").GetValue<AbilityToggler>().IsEnabled(urn.Name))
                                        urn.UseAbility(target);
                                    else
                                        elsecount += 1;
                                    if (solar != null && solar.CanBeCasted() && Menu.Item("Duel Items2").GetValue<AbilityToggler>().IsEnabled(solar.Name))
                                        solar.UseAbility(target);
                                    else
                                        elsecount += 1;
                                    if (medallion != null && medallion.CanBeCasted() && Menu.Item("Duel Items2").GetValue<AbilityToggler>().IsEnabled(medallion.Name))
                                        medallion.UseAbility(target);
                                    else
                                        elsecount += 1;
                                    if (Duel != null && Duel.Cooldown <= 0 && !target.IsLinkensProtected() && !target.Modifiers.Any(x => x.Name == "modifier_abaddon_borrowed_time") && Utils.SleepCheck("Duel") && elsecount == 16)
                                    {
                                        Duel.UseAbility(target);
                                        Utils.Sleep(100, "Duel");
                                    }
                                    else
                                        me.Attack(target, false);
                                    Utils.Sleep(150, "combo");
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (me.IsAlive && !me.IsChanneling() && !me.IsMoving && Utils.SleepCheck("moving"))
                    {
                        me.Move(Game.MousePosition, false);
                        Utils.Sleep(200, "moving");
                    }
                }
            }
        }
        static bool IsPhysDamageImune(Hero v)
        {
            if (me.Modifiers
                .Any(x =>
                (x.Name == "modifier_tinker_laser_blind" && !me.Inventory.Items.Any(y => y.Name == "item_monkey_king_bar"))
                || (x.Name == "modifier_troll_warlord_whirling_axes_blind" && !me.Inventory.Items.Any(y => y.Name == "item_monkey_king_bar"))
                || x.Name == "modifier_pugna_decrepify")
                || v.Modifiers.Any(x => x.Name == "modifier_omninight_guardian_angel"
                || x.Name == "modifier_pugna_decrepify"
                || (x.Name == "modifier_windrunner_windrun" && !me.Inventory.Items.Any(y => y.Name == "item_monkey_king_bar"))
                || x.Name == "modifier_winter_wyverny_cold_embrace"
                /*|| x.Name == "modifier_ghost_state" 
                || x.Name == "modifier_item_ethereal_blade_ethereal" 
                || x.Name == "modifier_item_ethereal_blade_ethereal"*/))
            {
                if (Heal.CanBeCasted() && me.Modifiers.Any(x => (x.Name == "modifier_tinker_laser_blind" && !me.Inventory.Items.Any(y => y.Name == "item_monkey_king_bar")) || (x.Name == "modifier_troll_warlord_whirling_axes_blind" && !me.Inventory.Items.Any(y => y.Name == "item_monkey_king_bar")) || x.Name == "modifier_pugna_decrepify"))
                {
                    if (me.CanCast())
                    {
                        Heal.UseAbility(me);
                        return false;
                    }
                    else
                        return false;
                }
                else if (difusal != null && difusal.CanBeCasted() && v.Modifiers.Any(x => x.Name == "modifier_omninight_guardian_angel"
                 || x.Name == "modifier_pugna_decrepify"))
                {
                    if (Utils.SleepCheck("difusalsleep"))
                    {
                        difusal.UseAbility(v);
                        Utils.Sleep(800, "difusalsleep");
                    }
                    return false;
                }
                else
                    return true;
            }
            else
                return false;
        }
        static bool UsedInvis(Hero v)
        {
            if (v.Modifiers.Any(
                    x =>
                   (x.Name == "modifier_bounty_hunter_wind_walk" ||
                    x.Name == "modifier_riki_permanent_invisibility" ||
                    x.Name == "modifier_mirana_moonlight_shadow" || x.Name == "modifier_treant_natures_guise" ||
                    x.Name == "modifier_weaver_shukuchi" ||
                    x.Name == "modifier_broodmother_spin_web_invisible_applier" ||
                    x.Name == "modifier_item_invisibility_edge_windwalk" || x.Name == "modifier_rune_invis" ||
                    x.Name == "modifier_clinkz_wind_walk" || x.Name == "modifier_item_shadow_amulet_fade" ||
                    x.Name == "modifier_item_silver_edge_windwalk" ||
                    x.Name == "modifier_item_edge_windwalk" ||
                    x.Name == "modifier_nyx_assassin_vendetta" ||
                    x.Name == "modifier_invisible" ||
                    x.Name == "modifier_invoker_ghost_walk_enemy")))
                return true;
            else
                return false;
        }
        static bool CanInvisCrit(Hero x)
        {
            if (x.Modifiers.Any(m => m.Name == "modifier_item_invisibility_edge_windwalk" || m.Name == "modifier_item_silver_edge_windwalk"))
                return true;
            else
                return false;
        }
        static bool IsLinkensProtected(Hero x)
        {
            if (x.Modifiers.Any(m => m.Name == "modifier_item_sphere_target") || x.FindItem("item_sphere") != null && x.FindItem("item_sphere").Cooldown <= 0)
                return true;
            else
                return false;
        }
        static void FindItems()
        {
            blink = me.FindItem("item_blink");
            armlet = me.FindItem("item_armlet");
            blademail = me.FindItem("item_blade_mail");
            bkb = me.FindItem("item_black_king_bar");
            abyssal = me.FindItem("item_abyssal_blade");
            mjollnir = me.FindItem("item_mjollnir");
            halberd = me.FindItem("item_heavens_halberd");
            medallion = me.FindItem("item_medallion_of_courage");
            madness = me.FindItem("item_mask_of_madness");
            urn = me.FindItem("item_urn_of_shadows");
            satanic = me.FindItem("item_satanic");
            solar = me.FindItem("item_solar_crest");
            dust = me.FindItem("item_dust");
            sentry = me.FindItem("item_ward_sentry");
            mango = me.FindItem("item_enchanted_mango");
            arcane = me.FindItem("item_arcane_boots");
            buckler = me.FindItem("item_buckler");
            crimson = me.FindItem("item_crimson_guard");
            lotusorb = me.FindItem("item_lotus_orb");
            cheese = me.FindItem("item_cheese");
            magistick = me.FindItem("item_magic_stick");
            magicwand = me.FindItem("item_magic_wand");
            soulring = me.FindItem("item_soul_ring");
            force = me.FindItem("item_force_staff");
            cyclone = me.FindItem("item_cyclone");
            vyse = me.FindItem("item_sheepstick");
            atos = me.FindItem("item_rod_of_atos");
            difusal = me.Inventory.Items.FirstOrDefault(item => item.Name.Contains("item_diffusal_blade"));
            Duel = me.Spellbook.SpellR;
            Heal = me.Spellbook.SpellW;
            Odds = me.Spellbook.SpellQ;
        }
        static void manacheck()
        {
            uint manacost = 0;
            if (me.IsAlive)
            {
                if (blademail != null && blademail.Cooldown <= 0 && Menu.Item("Duel Items3").GetValue<AbilityToggler>().IsEnabled(blademail.Name))
                    manacost += blademail.ManaCost;
                if (abyssal != null && abyssal.Cooldown <= 0 && Menu.Item("Duel Items").GetValue<AbilityToggler>().IsEnabled(abyssal.Name))
                    manacost += abyssal.ManaCost;
                if (mjollnir != null && mjollnir.Cooldown <= 0 && Menu.Item("Duel Items").GetValue<AbilityToggler>().IsEnabled(mjollnir.Name))
                    manacost += mjollnir.ManaCost;
                if (halberd != null && halberd.Cooldown <= 0 && Menu.Item("Pop Linkens: ").GetValue<AbilityToggler>().IsEnabled(halberd.Name))
                    manacost += halberd.ManaCost;
                if (madness != null && madness.Cooldown <= 0 && Menu.Item("Duel Items2").GetValue<AbilityToggler>().IsEnabled(madness.Name))
                    manacost += madness.ManaCost;
                if (lotusorb != null && lotusorb.Cooldown <= 0 && Menu.Item("Duel Items4").GetValue<AbilityToggler>().IsEnabled(lotusorb.Name))
                    manacost += lotusorb.ManaCost;
                if (buckler != null && buckler.Cooldown <= 0 && Menu.Item("Mana/Health Itens: ").GetValue<AbilityToggler>().IsEnabled(buckler.Name))
                    manacost += buckler.ManaCost;
                if (crimson != null && crimson.Cooldown <= 0 && Menu.Item("Mana/Health Itens: ").GetValue<AbilityToggler>().IsEnabled(crimson.Name))
                    manacost += crimson.ManaCost;
                if (force != null && force.Cooldown <= 0 && Menu.Item("Pop Linkens: ").GetValue<AbilityToggler>().IsEnabled(force.Name))
                    manacost += force.ManaCost;
                if (cyclone != null && cyclone.CanBeCasted() && Menu.Item("Pop Linkens: ").GetValue<AbilityToggler>().IsEnabled(cyclone.Name))
                    manacost += cyclone.ManaCost;
                if (vyse != null && vyse.Cooldown <= 0 && Menu.Item("Pop Linkens: ").GetValue<AbilityToggler>().IsEnabled(vyse.Name))
                    manacost += vyse.ManaCost;
                if (Heal.Cooldown <= 0 && Heal.Level > 0 && Menu.Item("Skills").GetValue<AbilityToggler>().IsEnabled(Heal.Name))
                    manacost += Heal.ManaCost;
                if (Duel.Cooldown <= 0 && Duel.Level > 0)
                    manacost += Heal.ManaCost;
            }
            if (manacost > me.Mana)
            {
                if (mango != null && mango.CanBeCasted() && Menu.Item("Mana/Health Itens2: ").GetValue<AbilityToggler>().IsEnabled(mango.Name) && Utils.SleepCheck("FastMango"))
                {
                    mango.UseAbility();
                    Utils.Sleep(Game.Ping, "FastMango");
                }
                if (arcane != null && arcane.CanBeCasted() && Menu.Item("Mana/Health Itens2: ").GetValue<AbilityToggler>().IsEnabled(arcane.Name) && Utils.SleepCheck("FastArcane"))
                {
                    arcane.UseAbility();
                    Utils.Sleep(Game.Ping, "FastArcane");
                }
                if (magicwand != null && magicwand.CanBeCasted() && Menu.Item("Duel Items4").GetValue<AbilityToggler>().IsEnabled(magicwand.Name) && Utils.SleepCheck("Fastmagicwand"))
                {
                    magicwand.UseAbility();
                    Utils.Sleep(Game.Ping, "Fastmagicwand");
                }
                if (magistick != null && magistick.CanBeCasted() && Menu.Item("Duel Items4").GetValue<AbilityToggler>().IsEnabled(magistick.Name) && Utils.SleepCheck("Fastmagistick"))
                {
                    magistick.UseAbility();
                    Utils.Sleep(Game.Ping, "Fastmagistick");
                }
                if (cheese != null && (cheese.CanBeCasted() && Menu.Item("Mana/Health Itens: ").GetValue<AbilityToggler>().IsEnabled(cheese.Name) && me.Health <= me.MaximumHealth * 0.5) || me.Health <= me.MaximumHealth * 0.30 && Utils.SleepCheck("FastCheese"))
                {
                    cheese.UseAbility();
                    Utils.Sleep(Game.Ping, "FastCheese");
                }
                if (soulring != null && soulring.CanBeCasted() && Menu.Item("Mana/Health Itens: ").GetValue<AbilityToggler>().IsEnabled(soulring.Name) && Utils.SleepCheck("FastSoulRing"))
                {
                    soulring.UseAbility();
                    Utils.Sleep(Game.Ping, "FastSoulRing");
                }
            }
        }
        static void markedfordeath(EventArgs args)
        {
            if (!Game.IsInGame || Game.IsWatchingGame)
                return;
            me = ObjectMgr.LocalHero;
            if (me == null)
                return;
            if (me.ClassID != ClassID.CDOTA_Unit_Hero_Legion_Commander)
                return;
            target = me.ClosestToMouseTarget(50000);
            if (target != null)
            {
                Vector2 target_health_bar = HeroPositionOnScreen(target);
                Drawing.DrawText("Marked for Death", target_health_bar, new Vector2(15, 200), me.Distance2D(target) < 1200 ? Color.Red : Color.Azure, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);
            }
            if (!Utils.SleepCheck("BKBTOGGLE"))
                Drawing.DrawText(Menu.Item("Duel Items3").GetValue<AbilityToggler>().IsEnabled("item_black_king_bar") == true ? "ON" : "OFF", new Vector2(HUDInfo.ScreenSizeX() / 2, HUDInfo.ScreenSizeY() / 2), new Vector2(30, 200), Menu.Item("Duel Items3").GetValue<AbilityToggler>().IsEnabled("item_black_king_bar") == true ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);

        }
        static Vector2 HeroPositionOnScreen(Hero x)
        {
            Vector2 PicPosition;
            PicPosition = new Vector2(HUDInfo.GetHPbarPosition(x).X - 1, HUDInfo.GetHPbarPosition(x).Y - 40);
            return PicPosition;
        }
        private static void PrintSuccess(string text, params object[] arguments)
        {
            PrintEncolored(text, ConsoleColor.Green, arguments);
        }
        private static void PrintEncolored(string text, ConsoleColor color, params object[] arguments)
        {
            var clr = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text, arguments);
            Console.ForegroundColor = clr;
        }

        private static void PrintModifiers(Unit unit)
        {
            var buffs = unit.Modifiers.ToList();

            if (buffs.Any())
            {
                foreach (var buff in buffs)
                {
                    Console.WriteLine(unit.Name + " has modifier: " + buff.Name);
                }
            }
            else
            {
                Console.WriteLine(unit.Name + " does not have any buff");
            }
        }
    }
}