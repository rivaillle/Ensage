using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using SharpDX;
using Ensage.Common.Menu;
using System.Windows.Input;

namespace LionDisable
{
    internal class Program
    {
        private static Ability Impale, Hex, Drain, Ult;
        private static Hero _source, _target;
        private static Item abyssal, blink, solar, medallion, pipe, halberd, atos, dust, Stick, Wand, Crimson, Quelling, bladeMail;
        private static Ensage.Items.PowerTreads threads;
        private const Key triggerKey = Key.B;
        private const Key chaseKey = Key.G;
        private static bool trigger, chase;
        private static readonly uint[] Quilldmg = { 20, 40, 60, 80 };
        private static readonly Menu Menu = new Menu("Lion", "lion", true);
        private static double[] ultDamagePerLevel = new double[] { 600, 725, 850};
        private static double threadsSwitchThreshold = 0.35;
        private static bool hasAether = false;
        private static int disableRange = 600;
        private static int extraRange = 0;
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
            Game.PrintMessage("Lion Sharp by <font color='#ff1111'>Jirico</font> Loaded !!", MessageType.LogMessage);
            var menu_utama = new Menu("Options", "opsi");
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
            if (_source == null || _source.ClassID != ClassID.CDOTA_Unit_Hero_Lion)
            {
                return;
            }

            var _enemy = ObjectManager.GetEntitiesFast<Hero>().Where(hero => hero.IsAlive && !hero.IsIllusion && hero.IsVisible && hero.Team != _source.Team);
            var _creep = ObjectManager.GetEntitiesFast<Creep>().Where(x => (x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege) && x.IsAlive && x.IsSpawned && x.IsVisible).ToList();
            if (Impale == null)
            {
                Impale = _source.Spellbook.Spell1;
            }
            if (Hex == null)
            {
                Hex = _source.Spellbook.Spell2;
            }
            if (Drain == null)
            {
                Drain = _source.Spellbook.Spell3;
            }
            if (Ult == null)
            {
                Ult = _source.Spellbook.SpellR;
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
            pipe = _source.FindItem("item_hood_of_defiance");
            if (pipe == null)
            {
                pipe = _source.FindItem("item_pipe");
            }
            if (_source.HasModifier("modifier_item_aether_lens") && !hasAether)
            {
                disableRange += 220;
                hasAether = true;
                extraRange = 220;
            }

            if (IsInDanger(_source, _enemy) && _source.Health <= _source.MaximumHealth * 0.5)
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

            if (Ult.Level > 0 && Ult.CanBeCasted() && Utils.SleepCheck("ult"))
            {
                var ultDamage = ultDamagePerLevel[Ult.Level - 1];
                var ultRange = 925 + extraRange;
                var enemies =
                                   ObjectManager.GetEntitiesFast<Hero>()
                                       .Where(
                                           x =>
                                               x.Team != _source.Team && !x.IsIllusion && x.IsAlive &&
                                               _source.Distance2D(x) <= ultRange && !x.IsLinkensProtected())
                                       .ToList();
                foreach (var enemy in enemies)
                {
                    var enemyResistence = enemy.MagicDamageResist;
                    double totalDamage = ultDamage - ultDamage * enemyResistence;
                    if (_source.HasModifier("modifier_item_aether_lens"))
                    {
                        totalDamage += totalDamage * 0.05;
                    }
                    totalDamage += totalDamage * (_source.Intelligence % 16) / 100;
                    if (totalDamage >= (enemy.Health + enemy.HealthRegeneration * 1))
                    {
                        Ult.UseAbility(enemy);
                        Utils.Sleep(200 + Game.Ping, "cast");
                        Utils.Sleep(1000, "ult");
                    }
                }
            }
            
            if (chase)
            {
                var enemy = _source.ClosestToMouseTarget(300);
                if (enemy == null)
                {
                    uint currentHealth = 9999;
                    foreach (var pe in _enemy)
                    {
                        if (pe.Health < currentHealth && _source.Distance2D(pe) <= disableRange)
                        {
                            currentHealth = pe.Health;
                            enemy = pe;
                        }
                    }
                }
                if (enemy != null)
                {
                    var hexModifier = enemy.FindModifier("modifier_lion_voodoo");
                    var hexTime = hexModifier?.RemainingTime ?? 0;
                    var impaleModifier = enemy.FindModifier("modifier_lion_impale");
                    var impaleTimer = impaleModifier?.RemainingTime ?? 0;
                    if (Hex != null && Hex.CanBeCasted() && Utils.SleepCheck("hex") && impaleTimer < 0.1 && Utils.SleepCheck("cast"))
                    {
                        /*foreach (var modifier in enemy.Modifiers.ToList())
                        {
                            Console.WriteLine(modifier.Name);
                        }*/
                        if (enemy.IsStunned() || enemy.IsHexed())
                        {

                        }
                        else
                        {
                            Hex.UseAbility(enemy);
                            Utils.Sleep(200 + Game.Ping, "cast");
                            Utils.Sleep(1000, "hex");

                        }
                    }
                    else if(Impale != null && Impale.CanBeCasted() && !Hex.CanBeCasted() && Utils.SleepCheck("imp") && Utils.SleepCheck("cast"))
                    {
                        
                       // Console.WriteLine(hexTime);
                       // Console.WriteLine(hexModifier);
                        //Console.WriteLine(hexTime < 0.5);
                        if (hexTime < 0.5)
                        {
                            Console.WriteLine("casting impale!!");
                            Impale.UseAbility(enemy);
                            Utils.Sleep(1000, "imp");
                            Utils.Sleep(200 + Game.Ping, "cast");
                        }
                    }                    
                    if(!Hex.CanBeCasted() || !Impale.CanBeCasted() && Utils.SleepCheck("cast"))
                    {
                        if((impaleTimer > 1.5 || hexTime > 2) && enemy.Mana > 100 && Drain.CanBeCasted() && Utils.SleepCheck("drain"))
                        {
                            Console.WriteLine("casting drain!!");
                            //Drain.UseAbility(enemy);
                            Orbwalking.Orbwalk(enemy, 0, 0, false, false);
                            Utils.Sleep(1000, "drain");
                            Utils.Sleep(200 + Game.Ping, "cast");
                        }
                        else if(!_source.IsChanneling() && Utils.SleepCheck("drain"))
                        {
                            Orbwalking.Orbwalk(enemy, 0, 0, false, false);
                        }
                    }
                    else
                    {
                        Orbwalking.Orbwalk(null);
                    }

                }
                else
                {
                    Orbwalking.Orbwalk(null);
                }
               


            }

            foreach(var enemy in _enemy)
            {
                if(enemy.Distance2D(_source) <= disableRange)
                {
                    if (enemy.IsChanneling())
                    {
                        useDisable(enemy);
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
            if (ability.CanBeCasted() )
            {
                ability.UseAbility();
            }
        }

        public static void useDisable(Hero enemy)
        {
            if(enemy.IsStunned() || enemy.IsHexed() || enemy.IsMagicImmune())
            {
                return;
            }
            if (Hex != null && Hex.CanBeCasted() && Utils.SleepCheck("hex") && Utils.SleepCheck("cast"))
            {                
               
                Hex.UseAbility(enemy);
                Utils.Sleep(200 + Game.Ping, "cast");
                Utils.Sleep(1000, "hex");

                
            }
            else if (Impale != null && Impale.CanBeCasted() && !Hex.CanBeCasted() && Utils.SleepCheck("imp") && Utils.SleepCheck("cast"))
            {
                Impale.UseAbility(enemy);
                Utils.Sleep(1000, "imp");
                Utils.Sleep(200 + Game.Ping, "cast");
                
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
                rangeDisplay.SetControlPoint(2, new Vector3(1200, 255, 0));
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

        private static void dealWithAntiMage(IEnumerable<Hero> allies, Hero enemy)
        {
            if (enemy.ClassID == ClassID.CDOTA_Unit_Hero_AntiMage)
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
            if (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Huskar)
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
            if (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Sven)
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
            if (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Slark)
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
            if (enemy.ClassID == ClassID.CDOTA_Unit_Hero_DrowRanger)
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
            if (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Luna)
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
    }
}
