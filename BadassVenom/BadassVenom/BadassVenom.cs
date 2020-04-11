using System;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Linq;
using Ensage;
using Ensage.SDK.Helpers;
using Ensage.SDK.Input;
using Ensage.SDK.Inventory;
using Ensage.SDK.Menu;
using Ensage.SDK.Abilities;
using Ensage.SDK.Service;
using Ensage.SDK.Service.Metadata;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using System.Windows.Input;
using System.Collections.Generic;
using SharpDX;


namespace BadassVenom
{
    [ExportPlugin(
        name: "BadassVenom",
        mode: StartupMode.Auto,
        author: "Rivaillle",
        version: "1.0.0.1",
        units: HeroId.npc_dota_hero_venomancer)]
    internal class BadassVenom : Plugin
    {
       
        private AbilityFactory AbilityFactory { get; }
        public IServiceContext Context { get; }

        [ImportingConstructor]
        public BadassVenom([Import] IServiceContext context)
        {
            Context = context;
            AbilityFactory = context.AbilityFactory;
        }

        protected override void OnActivate()
        {
            UpdateManager.Subscribe(PlagueControl.OnUpdate, 25);
        }

        protected override void OnDeactivate()
        {
            UpdateManager.Unsubscribe(PlagueControl.OnUpdate);
        }
        /*
        private static void OnLoad(object sender, EventArgs e)
        {
            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Game_OnDraw;
        }
        */

        /*
    static void Main(string[] args)
    {
        Events.OnLoad += OnLoad;
    }
    */
        private static void Game_OnDraw(EventArgs args)
        {
            /*
            if (me == null)
            {
                return;
            }

            if (!Game.IsInGame || Game.IsPaused || Game.IsWatchingGame)
            {
                return;
            }

            if (rangeDisplay == null)
            {
                rangeDisplay = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                rangeDisplay.SetControlPoint(1, new Vector3(255, 255, 255));
                rangeDisplay.SetControlPoint(2, new Vector3(875, 255, 0));
            }
            if (daggerDisplay == null)
            {
                daggerDisplay = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                daggerDisplay.SetControlPoint(1, new Vector3(0, 255, 255));
                daggerDisplay.SetControlPoint(2, new Vector3(1200, 255, 0));
            }
            /*
            else if (upgradedTalentRange && !drawnedExtraRange)
            {
                rangeDisplay.SetControlPoint(2, new Vector3(875 + 150, 255, 0));
                rangeDisplay.Restart();
                drawnedExtraRange = true;

            }
            */

        }
        /*
        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (!Game.IsChatOpen)
            {
                if (Game.IsKeyDown(plagueKey))
                {
                    autoPlague = true;
                }
                else
                {
                    autoPlague = false;
                }

                if (Game.IsKeyDown(chaseKey))
                {
                    autoChase = false;
                }
                else
                {
                    autoChase = false;
                }

            }
        }
        
      }*/

    }
}
