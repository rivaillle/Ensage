using System;
using System.Linq;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using Ensage.Common.Objects;
using Ensage.Common.Objects.UtilityObjects;
using SharpDX;
using System.Windows.Input;
using System.Collections.Generic;
using static AutoGhost.AutoGhost;

namespace BristleJR
{
    internal class Program
    {
        private static Ability Quill, Goo;
        private static Hero _source, _target;
        private static Item abyssal, blink, solar, medallion, pipe, halberd, atos, dust, Stick, Wand, Crimson, Quelling, bladeMail;
        private static Ensage.Items.PowerTreads threads;
        private const Key triggerKey = Key.B;
        private const Key chaseKey = Key.G;
        private static bool trigger, chase;
        private static readonly uint[] Quilldmg = { 20, 40, 60, 80 };
        private static readonly Menu Menu = new Menu("Bristleback", "bristle", true);
        private static double threadsSwitchThreshold = 0.35;
        private static int maxGooStacks = 4;
        private static float scaleX;
        private static float scaleY;
        private static float HpBarSizeX;
        private static float HpBarSizeY;
        private static ParticleEffect rangeDisplay;

        static void Main(string[] args)
        {

            scaleX = ((float)Drawing.Width / 1366);
            scaleY = ((float)Drawing.Height / 768);
            HpBarSizeX = HUDInfo.GetHPBarSizeX();
            HpBarSizeY = HUDInfo.GetHpBarSizeY();
            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Game_OnDraw;
            Game.PrintMessage("Bristleback Sharp by <font color='#ff1111'>Jirico</font> Loaded !!");
            Player.OnExecuteOrder += Player_OnExecuteAction;
            var menu_utama = new Menu("Options", "opsi");
            menu_utama.AddItem(new MenuItem("Quill", "Quill").SetValue(new StringList(new[] { "Max", "Smart", "Disable", "Farm", "Smart and Farm" })));
            menu_utama.AddItem(new MenuItem("enable", "enable").SetValue(true));
            Menu.AddSubMenu(menu_utama);
            Menu.AddToMainMenu();
            Unit.OnModifierAdded += ModifierAdded;
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
            if (_source == null || _source.ClassId != ClassId.CDOTA_Unit_Hero_Bristleback)
            {
                return;
            }

            var _enemy = ObjectManager.GetEntitiesFast<Hero>().Where(hero => hero.IsAlive && !hero.IsIllusion && hero.IsVisible && hero.Team != _source.Team);
            var _creep = ObjectManager.GetEntitiesFast<Creep>().Where(x => (x.ClassId == ClassId.CDOTA_BaseNPC_Creep_Lane || x.ClassId == ClassId.CDOTA_BaseNPC_Creep_Siege) && x.IsAlive && x.IsSpawned && x.IsVisible).ToList();

            if (Quill == null)
            {
                Quill = _source.Spellbook.Spell2;
                Console.WriteLine(Quill.CastRange);
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

            medallion = _source.FindItem("item_medallion_of_courage");
            if (medallion == null)
            {
                medallion = _source.FindItem("item_solar_crest");
            }

            if (halberd == null)
            {
                halberd = _source.FindItem("item_heavens_halberd");
            }
            if (threads == null)
            {
                threads = (Ensage.Items.PowerTreads)_source.FindItem("item_power_treads");
            }

            Stick = _source.FindItem("item_magic_stick");
            Wand = _source.FindItem("item_magic_wand");

            if (Crimson == null)
            {
                Crimson = _source.FindItem("item_crimson_guard");
            }

            Quelling = _source.FindItem("item_quelling_blade");
            if (Quelling == null)
            {
                Quelling = _source.FindItem("item_iron_talon");
            }
            bladeMail = _source.FindItem("item_blade_mail");
            pipe = _source.FindItem("item_hood_of_defiance");
            if (pipe == null)
            {
                pipe = _source.FindItem("item_pipe");
            }
            if (_source.Level >= 15 && maxGooStacks == 4)
            {
                var talentsGoo = _source.Spellbook.Spells.First(x => x.Name == "special_bonus_unique_bristleback");
                if (talentsGoo.Level > 0)
                {
                    maxGooStacks = 8;
                }
            }

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
            useGhost(medallion, _source, _enemy, true, null);
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
            if (trigger && Utils.SleepCheck("trigger") && Menu.Item("enable").GetValue<bool>())
            {

                if ((Quelling != null || medallion != null) && (Quelling.CanBeCasted() || medallion.CanBeCasted()))
                {
                    var neutral = ObjectManager.GetEntitiesFast<Creep>().Where(x => x.IsAlive && x.IsSpawned && x.IsVisible && x.Distance2D(_source) <= 600).MaxOrDefault(x => x.Health);
                    if (neutral != null && neutral.Health > _source.DamageAverage && neutral.Health > neutral.MaximumHealth * 0.4)
                    {
                        if(Quelling.CanBeCasted() && Utils.SleepCheck("deward"))
                        {
                            Quelling.UseAbility(neutral);
                            Utils.Sleep(1000, "deward");
                        }
                        if (medallion.CanBeCasted() && Utils.SleepCheck("solar"))
                        {
                            medallion.UseAbility(neutral);
                            Utils.Sleep(4000, "solar");
                        }
                    }
                    _source.Attack(neutral);
                    Utils.Sleep(200, "trigger");

                }
                if (Utils.SleepCheck("quill") && Quill.CanBeCasted())
                {
                    useAbility(Quill);
                    Utils.Sleep(150 + Game.Ping, "quill");
                }

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
            if (Crimson != null || bladeMail != null || halberd != null || medallion != null || abyssal != null)
            {
                var allies = ObjectManager.GetEntitiesFast<Hero>().Where(hero => hero.IsAlive && !hero.IsIllusion && hero.ClassId != _source.ClassId && hero.Team == _source.Team);
                foreach (var enemy in _enemy)
                {
                    dealWithAntiMage(allies, enemy);
                    dealWithHuskar(allies, enemy);
                    dealWithSven(allies, enemy);
                    dealWithSlark(allies, enemy);
                    dealWithDrow(allies, enemy);
                    dealWithLuna(allies, enemy);
                    if (Crimson != null || medallion != null)
                    {
                        if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Juggernaut && (enemy.Spellbook.SpellR.IsInAbilityPhase || enemy.HasModifier("modifier_juggernaut_omnislash")))
                        {
                            if (Crimson != null && Utils.SleepCheck("crimson") && _source.Distance2D(enemy) < 400)
                            {
                                Crimson.UseAbility();
                                Utils.Sleep(4000 + Game.Ping, "crimson");
                            }
                            if (medallion != null && Utils.SleepCheck("solar") && _source.Distance2D(enemy) > 450)
                            {
                                foreach (var ally in allies)
                                {
                                    if (_source.Distance2D(ally) <= 1000 && ally.Distance2D(enemy) <= 400)
                                    {
                                        medallion.UseAbility(ally);
                                        Utils.Sleep(4000, "solar");
                                    }
                                }
                            }

                            /*
                        foreach(var modifier in enemy.Modifiers.ToList())
                        {
                            Console.WriteLine(modifier.Name);
                        }
                        */
                        }
                        else if ((enemy.ClassId == ClassId.CDOTA_Unit_Hero_Sven || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Legion_Commander) && _source.Distance2D(enemy) < 400 && Utils.SleepCheck("crimson"))
                        {
                            blink = enemy.FindItem("item_blink");
                            if (blink != null && blink.Cooldown > 8)
                            {
                                if (!((enemy.ClassId == ClassId.CDOTA_Unit_Hero_Sven && !enemy.HasModifier("modifier_sven_gods_strength")) || (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Legion_Commander && !(enemy.Spellbook.SpellR.CanBeCasted()))))
                                {
                                    Crimson.UseAbility();
                                    Utils.Sleep(4000 + Game.Ping, "crimson");
                                }

                            }
                        }
                    }
                    if (bladeMail != null && bladeMail.CanBeCasted())
                    {
                        if (_source.HasModifier("modifier_silver_edge_debuff"))
                        {
                            bladeMail.UseAbility();
                            Utils.Sleep(1000 + Game.Ping, "blademail");
                        }
                        if ((enemy.ClassId == ClassId.CDOTA_Unit_Hero_Axe && _source.Distance2D(enemy) < 350 && Utils.SleepCheck("blademail") && enemy.Spellbook.SpellQ.CanBeCasted()))
                        {
                            var blink = enemy.FindItem("item_blink");
                            if (blink != null && blink.Cooldown > 8)
                            {
                                bladeMail.UseAbility();
                                Utils.Sleep(1000 + Game.Ping, "blademail");
                            }

                        }
                        if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Sven && _source.Distance2D(enemy) <= 200 && Utils.SleepCheck("blademail") && enemy.HasModifier("modifier_sven_gods_strength") && IsFacing(enemy, _source))
                        {
                            bladeMail.UseAbility();
                            Utils.Sleep(1000 + Game.Ping, "blademail");

                        }
                        else if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_PhantomAssassin && _source.Distance2D(enemy) <= 200 && Utils.SleepCheck("blademail") && enemy.HasModifier("modifier_phantom_assassin_phantom_strike") && IsFacing(enemy, _source))
                        {
                            bladeMail.UseAbility();
                            Utils.Sleep(1000 + Game.Ping, "blademail");
                            if (medallion != null && medallion.CanBeCasted())
                            {
                                medallion.UseAbility(enemy);
                                Utils.Sleep(1000, "solar");
                            }

                        }
                        else if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Slark && _source.Distance2D(enemy) <= 200 && Utils.SleepCheck("blademail") && enemy.HasModifier("modifier_slark_shadow_dance_passive_regen") && IsFacing(enemy, _source))
                        {
                            bladeMail.UseAbility();
                            Utils.Sleep(1000 + Game.Ping, "blademail");

                        }
                    }
                    if (halberd != null && Utils.SleepCheck("halberd") && halberd.CanBeCasted())
                    {

                        if ((enemy.ClassId == ClassId.CDOTA_Unit_Hero_Legion_Commander) && _source.Distance2D(enemy) <= 600 && Utils.SleepCheck("heaven"))
                        {
                            if (_source.Distance2D(enemy) <= 200 && (enemy.Spellbook.SpellR.CanBeCasted()) && IsFacing(enemy, _source))
                            {
                                halberd.UseAbility(enemy);
                                Utils.Sleep(1000 + Game.Ping, "halberd");
                            }
                            else if (enemy.HasModifier("modifier_legion_commander_duel") && _source.CanCast())
                            {
                                halberd.UseAbility(enemy);
                                Utils.Sleep(5000 + Game.Ping, "halberd");
                            }
                        }
                        else if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Huskar && enemy.Distance2D(_source) <= 600)
                        {
                            var ult = enemy.Spellbook.SpellR;
                            if (ult != null && ult.Cooldown > 0 && enemy.IsAttacking())
                            {
                                halberd.UseAbility(enemy);
                                Utils.Sleep(5000, "halberd");
                            }
                        }
                    }
                    if (medallion != null && medallion.CanBeCasted())
                    {

                        if ((enemy.ClassId == ClassId.CDOTA_Unit_Hero_Legion_Commander) && _source.Distance2D(enemy) <= 1000 && Utils.SleepCheck("solar") && enemy.HasModifier("modifier_legion_commander_duel"))
                        {
                            foreach (var ally in allies)
                            {
                                if (ally.HasModifier("modifier_legion_commander_duel"))
                                {
                                    medallion.UseAbility(ally);
                                    Utils.Sleep(1000, "solar");
                                }
                            }

                        }
                        if ((enemy.ClassId == ClassId.CDOTA_Unit_Hero_Sven) && _source.Distance2D(enemy) <= 1000 && Utils.SleepCheck("solar") && enemy.HasModifier("modifier_sven_gods_strength"))
                        {
                            foreach (var ally in allies)
                            {
                                if (IsFacing(enemy, ally))
                                {
                                    medallion.UseAbility(ally);
                                    Utils.Sleep(1000, "solar");
                                }
                            }

                        }
                        if ((enemy.ClassId == ClassId.CDOTA_Unit_Hero_PhantomAssassin) && _source.Distance2D(enemy) <= 1000 && Utils.SleepCheck("solar"))
                        {
                            foreach (var ally in allies)
                            {
                                if (IsFacing(enemy, ally) && ally.Distance2D(enemy) <= 200)
                                {
                                    medallion.UseAbility(ally);
                                    Utils.Sleep(1000, "solar");
                                }
                            }

                        }
                        if ((enemy.ClassId == ClassId.CDOTA_Unit_Hero_Axe) && _source.Distance2D(enemy) <= 1000 && enemy.HasModifier("modifier_axe_berserkers_call_armor") && Utils.SleepCheck("solar"))
                        {
                            foreach (var ally in allies)
                            {
                                if (ally.Distance2D(enemy) <= 300)
                                {
                                    medallion.UseAbility(ally);
                                    Utils.Sleep(1000, "solar");
                                }
                            }

                        }

                    }
                    if (abyssal != null && enemy.ClassId != ClassId.CDOTA_Unit_Hero_Slark && _source.Distance2D(enemy) <= 200 && Utils.SleepCheck("abyssal") && abyssal.CanBeCasted() &&
                            (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Sven || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Sven || enemy.ClassId == ClassId.CDOTA_Unit_Hero_AntiMage || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Sniper
                            || enemy.ClassId == ClassId.CDOTA_Unit_Hero_TemplarAssassin || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Treant || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Shadow_Demon || enemy.ClassId == ClassId.CDOTA_Unit_Hero_Enigma || enemy.IsChanneling()))
                    {
                        abyssal.UseAbility(enemy);
                        Utils.Sleep(1000, "abyssal");
                    }
                }
            }

            if (chase)
            {
                var enemy = _source.ClosestToMouseTarget(200);
                Orbwalk(_source, enemy);
                if (enemy == null)
                {
                    uint currentHealth = 9999;
                    foreach (var pe in _enemy)
                    {
                        if (pe.Health < currentHealth && _source.Distance2D(pe) < 700)
                        {
                            currentHealth = pe.Health;
                            enemy = pe;
                        }
                    }
                }
                if (enemy != null)
                {

                    if (Goo != null && Goo.CanBeCasted() && Utils.SleepCheck("goo"))
                    {
                        /*foreach (var modifier in enemy.Modifiers.ToList())
                        {
                            Console.WriteLine(modifier.Name);
                        }*/
                        var gooModifier = enemy.FindModifier("modifier_bristleback_viscous_nasal_goo");
                        var gooStack = gooModifier?.StackCount ?? 0;
                        var gooTime = gooModifier?.RemainingTime ?? 0;
                        if (gooStack < maxGooStacks || gooTime < 1 && !_source.IsAttacking())
                        {
                            if (_source.HasModifier("modifier_item_ultimate_scepter") && _source.Distance2D(enemy) <= 750)
                            {
                                Goo.UseAbility();
                            }
                            else if (_source.Distance2D(enemy) <= Goo.CastRange)
                            {
                                Goo.UseAbility(enemy);
                            }
                            Utils.Sleep(500, "goo");
                        }

                    }
                    Orbwalking.Orbwalk(enemy, 0, 0, false, true);
                    if (medallion != null && Utils.SleepCheck("solar") && medallion.CanBeCasted() && !_source.IsAttacking() && enemy.ClassId != ClassId.CDOTA_Unit_Hero_Slark)
                    {
                        var isSafe = true;
                        foreach (var enemyUnit in _enemy)
                        {
                            blink = enemyUnit.FindItem("item_blink");
                            if (enemyUnit.ClassId != enemy.ClassId && _source.Distance2D(enemyUnit) < 800 && (IsFacing(enemyUnit, _source) || (blink != null && blink.Cooldown > 6)))
                            {
                                isSafe = false;
                            }
                        }

                        if (isSafe)
                        {
                            medallion.UseAbility(enemy);
                            Utils.Sleep(1000, "solar");
                        }
                    }


                }
                else
                {
                    Orbwalking.Orbwalk(null);
                    // _source.Move(Game.MousePosition);
                }
                if (Quill.CanBeCasted() && Utils.SleepCheck("quill"))
                {
                    useAbility(Quill);
                    Utils.Sleep(500 + Game.Ping, "quill");
                }


            }

            if (Quelling != null && Quelling.Cooldown == 0 && Utils.SleepCheck("deward"))
            {
                var wards =
                                        ObjectManager.GetEntitiesFast<Unit>()
                                            .Where(
                                                x =>
                                                    x.Team != _source.Team && (x.ClassId == ClassId.CDOTA_NPC_Observer_Ward || x.ClassId == ClassId.CDOTA_NPC_Observer_Ward_TrueSight) &&
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
            if (powerTreads != null && me.Health >= me.MaximumHealth * threadsSwitchThreshold)
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

        private static void Game_OnDraw(EventArgs args)
        {
            if (_source == null)
            {
                return;
            }
            if (rangeDisplay == null)
            {
                rangeDisplay = _source.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                rangeDisplay.SetControlPoint(1, new Vector3(255, 255, 255));
                rangeDisplay.SetControlPoint(2, new Vector3(700, 255, 0));
            }

            var hpbary = HUDInfo.GetHpBarSizeY();
            var hpvarx = HUDInfo.GetHPBarSizeX();
            var enemies = ObjectManager.GetEntities<Hero>().Where(hero => hero.IsAlive && !hero.IsIllusion && hero.IsVisible && hero.Team != _source.Team);
            foreach (var enemy in enemies)
            {
                var quillStack = enemy.FindModifier("modifier_bristleback_quill_spray")?.StackCount ?? 0;
                var duration = enemy.FindModifier("modifier_bristleback_quill_spray")?.RemainingTime ?? 0;
                if (quillStack > 0)
                {
                    var hpbarpositionX = HUDInfo.GetHPbarPosition(enemy).X;
                    var text = "Quill Stacks " + quillStack + " - " + duration.ToString("F1");
                    var textPos =
                   new Vector2(
                       (int)
                       (hpbarpositionX + 4
                        + (HpBarSizeX * ((float)enemy.Health * 1000 / enemy.MaximumHealth)) / 1000),
                       (int)(HUDInfo.GetHPbarPosition(enemy).Y - 2));
                    Drawing.DrawText(
                    text,
                    textPos,
                    new Vector2(18),
                    Color.White,
                    FontFlags.AntiAlias);
                }
            }

        }

        private static void ModifierAdded(Unit unit, ModifierChangedEventArgs args)
        {

        }
        private static void Player_OnExecuteAction(Player sender, ExecuteOrderEventArgs args)
        {
            var me = sender.Hero;
            if (me.IsInvisible())
            {
                return;
            }
            switch (args.OrderId)
            {
                case OrderId.TransferItem:
                case OrderId.MoveItem:
                case OrderId.DropItem:
                case OrderId.PickItem:
                case OrderId.BuyItem:
                case OrderId.AttackTarget:
                case OrderId.AttackLocation:
                case OrderId.AbilityTarget:
                case OrderId.AbilityLocation:
                case OrderId.Ability:
                    if (threads != null)
                    {
                        if (me.Health >= me.MaximumHealth * threadsSwitchThreshold)
                        {
                            setThreads(me, threads, Ensage.Attribute.Intelligence);
                        }
                    }
                    break;
                case OrderId.ToggleAbility:
                case OrderId.MoveLocation:
                case OrderId.MoveTarget:
                default:
                    break;
            }
        }

        private static void dealWithAntiMage(IEnumerable<Hero> allies, Hero enemy)
        {
            if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_AntiMage)
            {
                var manta = enemy.FindItem("item_manta");
                if (manta != null && manta.Cooldown > 20 && enemy.IsAttacking())
                {
                    foreach (var ally in allies)
                    {
                        if (IsFacing(enemy, ally))
                        {
                            if (halberd != null && halberd.CanBeCasted() && Utils.SleepCheck("halberd"))
                            {
                                halberd.UseAbility(enemy);
                                Utils.Sleep(5000, "halberd");
                            }
                            if (medallion != null && medallion.CanBeCasted() && Utils.SleepCheck("solar"))
                            {
                                medallion.UseAbility(ally);
                                Utils.Sleep(1000, "solar");
                            }
                            break;
                        }
                    }

                    if (IsFacing(enemy, _source))
                    {
                        if (Utils.SleepCheck("halberd"))
                        {
                            halberd.UseAbility(enemy);
                            Utils.Sleep(5000, "halberd");
                        }
                        if (bladeMail != null && bladeMail.CanBeCasted() && Utils.SleepCheck("blademail"))
                        {
                            bladeMail.UseAbility();
                            Utils.Sleep(5000, "blademail");
                        }
                    }
                }
            }
        }

        //Against Huskar go BladeMail, SolarCrest, Halberd and Pipe
        private static void dealWithHuskar(IEnumerable<Hero> allies, Hero enemy)
        {
            if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Huskar)
            {
                var ult = enemy.Spellbook.SpellR;
                if (ult.IsInAbilityPhase)
                {
                    foreach (var ally in allies)
                    {
                        if (IsFacing(enemy, ally))
                        {
                            if (halberd != null && halberd.CanBeCasted() && Utils.SleepCheck("halberd") && _source.Distance2D(enemy) <= 600)
                            {
                                halberd.UseAbility(enemy);
                                Utils.Sleep(5000, "halberd");
                            }
                            if (medallion != null && medallion.CanBeCasted() && Utils.SleepCheck("solar") && _source.Distance2D(ally) <= 1000)
                            {
                                medallion.UseAbility(ally);
                                Utils.Sleep(1000, "solar");
                            }
                            if (pipe != null && pipe.CanBeCasted() && Utils.SleepCheck("pipe") && _source.Distance2D(ally) <= 900)
                            {
                                pipe.UseAbility();
                                Utils.Sleep(5000, "pipe");
                            }
                            break;
                        }
                    }

                    if (IsFacing(enemy, _source))
                    {
                        if (bladeMail != null && bladeMail.CanBeCasted() && Utils.SleepCheck("blademail"))
                        {
                            bladeMail.UseAbility();
                            Utils.Sleep(5000, "blademail");
                        }
                        if (pipe != null && pipe.CanBeCasted() && Utils.SleepCheck("pipe"))
                        {
                            pipe.UseAbility();
                            Utils.Sleep(5000, "pipe");
                        }
                    }
                }
                else if (ult.Cooldown > 0 && enemy.IsAttacking() && IsFacing(enemy, _source) && _source.HasModifier("huskar_life_break"))
                {
                    if (Utils.SleepCheck("halberd"))
                    {
                        halberd.UseAbility(enemy);
                        Utils.Sleep(5000, "halberd");
                    }
                }
            }
        }

        //Against Huskar go BladeMail, SolarCrest, Halberd and Pipe
        private static void dealWithSven(IEnumerable<Hero> allies, Hero enemy)
        {
            if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Sven)
            {
                var ult = enemy.Spellbook.SpellR;
                if (enemy.HasModifier("modifier_sven_gods_strength"))
                {
                    var bkb = enemy.FindItem("item_black_king_bar");
                    foreach (var ally in allies)
                    {
                        if (IsFacing(enemy, ally) && enemy.Distance2D(ally) <= 200)
                        {
                            if (halberd != null && halberd.CanBeCasted() && Utils.SleepCheck("halberd") && _source.Distance2D(enemy) <= 600)
                            {
                                halberd.UseAbility(enemy);
                                Utils.Sleep(5000, "halberd");
                            }
                            if (medallion != null && medallion.CanBeCasted() && Utils.SleepCheck("solar") && _source.Distance2D(ally) <= 1000)
                            {
                                medallion.UseAbility(ally);
                                Utils.Sleep(1000, "solar");
                            }
                            break;
                        }
                    }

                    if (IsFacing(enemy, _source) && enemy.Distance2D(_source) <= 200)
                    {
                        if (bladeMail != null && bladeMail.CanBeCasted() && Utils.SleepCheck("blademail"))
                        {
                            bladeMail.UseAbility();
                            Utils.Sleep(5000, "blademail");
                        }
                        if (halberd != null && halberd.CanBeCasted() && Utils.SleepCheck("halberd"))
                        {
                            halberd.UseAbility(enemy);
                            Utils.Sleep(5000, "halberd");
                        }

                    }
                }
                else if (ult.Cooldown > 0 && enemy.IsAttacking() && IsFacing(enemy, _source) && _source.HasModifier("huskar_life_break"))
                {
                    if (Utils.SleepCheck("halberd"))
                    {
                        halberd.UseAbility(enemy);
                        Utils.Sleep(5000, "halberd");
                    }
                }
            }
        }

        //Against Slark go BladeMail, SolarCrest, Halberd
        private static void dealWithSlark(IEnumerable<Hero> allies, Hero enemy)
        {
            if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Slark)
            {
                var pact = enemy.Spellbook.SpellQ;
                if (true)
                {
                    foreach (var ally in allies)
                    {
                        if (IsFacing(enemy, ally) && enemy.Distance2D(ally) <= 200)
                        {
                            if (halberd != null && halberd.CanBeCasted() && Utils.SleepCheck("halberd") && _source.Distance2D(enemy) <= 600 && pact.Cooldown <= 7 && pact.Cooldown > 3)
                            {
                                halberd.UseAbility(enemy);
                                Utils.Sleep(5000, "halberd");
                            }
                            if (medallion != null && medallion.CanBeCasted() && Utils.SleepCheck("solar") && _source.Distance2D(ally) <= 1000)
                            {
                                medallion.UseAbility(ally);
                                Utils.Sleep(1000, "solar");
                            }
                            break;
                        }
                    }
                    if (enemy.Distance2D(_source) <= 200)
                    {
                        if (IsFacing(enemy, _source))
                        {
                            if (bladeMail != null && bladeMail.CanBeCasted() && Utils.SleepCheck("blademail"))
                            {
                                bladeMail.UseAbility();
                                Utils.Sleep(5000, "blademail");
                            }
                        }
                        if (halberd != null && halberd.CanBeCasted() && Utils.SleepCheck("halberd") && _source.Distance2D(enemy) <= 600 && pact.Cooldown <= 3.5 && pact.Cooldown > 0.5)
                        {
                            halberd.UseAbility(enemy);
                            Utils.Sleep(5000, "halberd");
                        }
                        if (abyssal != null && Utils.SleepCheck("abyssal") && abyssal.CanBeCasted() && (pact.Cooldown <= 3.5 || pact.CanBeCasted()))
                        {
                            abyssal.UseAbility(enemy);
                            Utils.Sleep(5000, "abyssal");
                        }
                    }
                    if (chase && medallion != null && medallion.CanBeCasted() && Utils.SleepCheck("solar") && (pact.Cooldown <= 3.5 && pact.Cooldown > 0.5))
                    {
                        medallion.UseAbility(enemy);
                    }
                }
            }
        }

        //Against Slark go BladeMail, SolarCrest, Halberd
        private static void dealWithDrow(IEnumerable<Hero> allies, Hero enemy)
        {
            if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_DrowRanger)
            {
                foreach (var ally in allies)
                {
                    if (IsFacing(enemy, ally) && enemy.Distance2D(ally) <= 1000)
                    {
                        if (halberd != null && halberd.CanBeCasted() && Utils.SleepCheck("halberd") && _source.Distance2D(enemy) <= 600)
                        {
                            halberd.UseAbility(enemy);
                            Utils.Sleep(5000, "halberd");
                        }
                        if (medallion != null && medallion.CanBeCasted() && Utils.SleepCheck("solar") && _source.Distance2D(ally) <= 1000)
                        {
                            medallion.UseAbility(ally);
                            Utils.Sleep(1000, "solar");
                        }
                        break;
                    }
                }

                if (IsFacing(enemy, _source) && enemy.Distance2D(_source) <= 1000)
                {
                    if (bladeMail != null && bladeMail.CanBeCasted() && Utils.SleepCheck("blademail"))
                    {
                        bladeMail.UseAbility();
                        Utils.Sleep(4500, "blademail");
                    }
                    if (halberd != null && halberd.CanBeCasted() && Utils.SleepCheck("halberd") && _source.Distance2D(enemy) <= 600 && Utils.SleepCheck("blademail"))
                    {
                        halberd.UseAbility(enemy);
                        Utils.Sleep(5000, "halberd");
                    }
                }

            }
        }

        //Against Slark go BladeMail, SolarCrest, Halberd
        private static void dealWithLuna(IEnumerable<Hero> allies, Hero enemy)
        {
            if (enemy.ClassId == ClassId.CDOTA_Unit_Hero_Luna)
            {
                var ult = enemy.Spellbook.SpellR;
                if (ult.IsInAbilityPhase && enemy.Distance2D(_source) <= 675)
                {
                    if (bladeMail != null && bladeMail.CanBeCasted() && Utils.SleepCheck("blademail"))
                    {
                        bladeMail.UseAbility();
                        Utils.Sleep(4500, "blademail");
                    }
                    if (pipe != null && pipe.CanBeCasted() && Utils.SleepCheck("pipe"))
                    {
                        pipe.UseAbility();
                        Utils.Sleep(5000, "pipe");
                    }
                }

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
        private static readonly Dictionary<uint, Orbwalker> OrbDict = new Dictionary<uint, Orbwalker>();
        private static void Orbwalk(Hero me, Unit target)
        {
            if (me == null)
            {
                return;
            }
            Orbwalker orb;
            if (!OrbDict.TryGetValue(me.Handle, out orb))
            {
                OrbDict.Add(me.Handle, new Orbwalker(me));
                return;
            }
            orb.OrbwalkOn(target, followTarget: true);
        }

    }
}