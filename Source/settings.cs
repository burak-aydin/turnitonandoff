using System;
using System.Collections.Generic;
using HugsLib.Settings;
using HugsLib;
using Verse;


namespace TurnItOnandOff
{
    //HugsLib.Settings.ModSettingsPack
    public static class ModSettings
    {
        public static int RescanPeriod = 2000;
        public static bool VerboseLogging = false;
        public static float ActivePowerMultiplier = 1;
        public static float IdlePowerUsage = 1;
        public static List<string> WhitelistedDefs;
        public static List<string> BlacklistedDefs;
        public static List<string> ReservableDefs;


        public static void ReadSettings(ModSettingsPack settings)
        {
            WhitelistedDefs = new List<string>();
            BlacklistedDefs = new List<string>();
            ReservableDefs = new List<string>();
            RescanPeriod = settings.GetHandle<int>(
            "tioao_rescan_period",
            "Force refresh rate in ticks",
            "Force refresh rate in ticks, fixes desyncronized machines. Increasing this reduces performance impact, but things might occasionally misbehave",
            2000);
            VerboseLogging = settings.GetHandle<bool>(
            "tioao_verbose_logging",
            "Verbose logging",
            "More logs, if you want to share logs with the dev please enable",
            false);
            ActivePowerMultiplier = settings.GetHandle<float>(
            "tioao_active_power_multiplier",
            "Active power usage multiplier",
            "Active power usage multiplier, increase this to make buildings consume more power when active",
            1.0f);
            IdlePowerUsage = settings.GetHandle<int>(
            "tioao_idle_power_usage",
            "Idle power usage value",
            "Idle power usage value",
            1);
            var WhitelistedDefsHandler = settings.GetHandle<string>(
            "tioao_whitelisted_defs",
            "Whitelisted defs",
            "Whitelisted defs, seperated by new line. Workbenches are included by default",
            String.Join("\n", DefaultWhitelistedDefs) 
            );

            WhitelistedDefsHandler.CustomDrawer = rect =>
            {
                WhitelistedDefsHandler.Value = Widgets.TextArea(rect, WhitelistedDefsHandler.Value);
                return true;
            };
            WhitelistedDefsHandler.CustomDrawerHeight = 200;
            string WhitelistedDefsString = WhitelistedDefsHandler.Value;

            var BlacklistedDefsHandler = settings.GetHandle<string>(
            "tioao_blacklisted_defs",
            "Blacklisted defs",
            "Blacklisted defs, seperated by new line",
            String.Join("\n", DefaultBlacklistedDefs) 
            );

            BlacklistedDefsHandler.CustomDrawer = rect =>
            {
                BlacklistedDefsHandler.Value = Widgets.TextArea(rect, BlacklistedDefsHandler.Value);
                return true;
            };
            BlacklistedDefsHandler.CustomDrawerHeight = 200;
            string BlacklistedDefsString = BlacklistedDefsHandler.Value;


            var ReservableDefsHandler = settings.GetHandle<string>(
            "tioao_reservable_defs",
            "Reservable defs",
            "Reservable defs, seperated by new line",
            String.Join("\n", DefaultReservableDefs) 
            );

            ReservableDefsHandler.CustomDrawer = rect =>
            {
                ReservableDefsHandler.Value = Widgets.TextArea(rect, ReservableDefsHandler.Value);
                return true;
            };
            ReservableDefsHandler.CustomDrawerHeight = 200;
            string ReservableDefsString = ReservableDefsHandler.Value;

            foreach (var whitelistedItem in WhitelistedDefsString.Split('\n'))
            {
                if (whitelistedItem != "")
                {
                    ThingDef def = DefDatabase<ThingDef>.GetNamed(whitelistedItem, false);
                    if (def == default(ThingDef))
                    {
                        Utils.Warning(string.Format("whitelisted def {0} is not valid, ignoring", whitelistedItem));
                        continue;
                    }
                    WhitelistedDefs.Add(whitelistedItem);
                }
            }
            foreach (var blacklistedItem in BlacklistedDefsString.Split('\n'))
            {
                if (blacklistedItem != "")
                {
                    ThingDef def = DefDatabase<ThingDef>.GetNamed(blacklistedItem, false);
                    if (def == default(ThingDef))
                    {
                        Utils.Warning(string.Format("blacklisted def {0} is not valid, ignoring", blacklistedItem));
                        continue;
                    }
                    BlacklistedDefs.Add(blacklistedItem);
                }
            }
            foreach (var reservableItem in ReservableDefsString.Split('\n'))
            {
                if (reservableItem != "")
                {
                    ThingDef def = DefDatabase<ThingDef>.GetNamed(reservableItem, false);
                    if (def == default(ThingDef))
                    {
                        Utils.Warning(string.Format("reservable def {0} is not valid, ignoring", reservableItem));
                        continue;
                    }
                    ReservableDefs.Add(reservableItem);
                    WhitelistedDefs.Add(reservableItem);
                }
            }
        }
        private readonly static string[] DefaultWhitelistedDefs = {
            "TubeTelevision",
            "FlatscreenTelevision",
            "MegascreenTelevision",
            "HiTechResearchBench",
            "MultiAnalyzer",
            "VitalsMonitor",
            "LightBall",
            "Loudspeaker",
            "WastepackAtomizer",
        };

        private readonly static string[] DefaultBlacklistedDefs = {
            "MechGestator",
            "LargeMechGestator",
        };

        private readonly static string[] DefaultReservableDefs = {
            "LongRangeMineralScanner",
            "GroundPenetratingScanner",
            "DeepDrill",
        };
    }

}