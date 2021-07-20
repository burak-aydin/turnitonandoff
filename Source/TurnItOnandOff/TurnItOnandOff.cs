using System;
using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using UnityEngine;
using HugsLib;
using HarmonyLib;
using System.Linq;
using System.Text;

using System.Reflection;


namespace TurnItOnandOff
{
    // Track the power users
    [HarmonyPatch(typeof(Building_WorkTable), "UsedThisTick", new Type[] { })]
    public static class Building_WorkTable_UsedThisTick_Patch
    {
        [HarmonyPrefix]
        public static void UsedThisTick(Building_WorkTable __instance)
        {
            // The Hook for tracking things used:            
            TurnItOnandOff.AddBuildingUsed(__instance);
        }
    }

    [HarmonyPatch(typeof(JobDriver_WatchBuilding), "WatchTickAction", new Type[] { })]
    public static class JobDriver_WatchBuilding_WatchTickAction_Patch
    {
        [HarmonyPrefix]
        public static void WatchTickAction(JobDriver_WatchBuilding __instance)
        {
            // The Hook for tracking things used:
            TurnItOnandOff.AddBuildingUsed(__instance.job.targetA.Thing as Building);
        }
    }

    public class TurnItOnandOff : ModBase
    {
        public override string ModIdentifier
        {
            get
            {
                return "TurnItOnandOff";
            }
        }

        // Track the number of buildings on the map
        // When this changes, rescan now instead of delayed
        // (This seems to be the best way of figuring out when a new building is placed)
        // For simplicity, cheese it and only care about the visible map
        int lastVisibleBuildings = 0;

        int ticksToRescan = 0; // Tick tracker for rescanning
        public override void Tick(int currentTick)
        {
            if (inUseTick != currentTick)
            {
                inUseTick = currentTick;
                                
                buildingsThatWereUsedLastTick.Clear();
                
                buildingsThatWereUsedLastTick.UnionWith(buildingsInUseThisTick);                

                buildingsInUseThisTick.Clear();
            }

            EvalVitalMon();            
            EvalResearchTablesandAnalyzers();
            EvalExternalReservable();
      

            foreach (Thing thing in buildingsToModifyPowerOn)
            {
          //      Verse.Log.Message("tick10");
                if (thing == null)
                {
                    Logger.Message("Tried to modify power level for thing which no longer exists");
                    continue;
                }
         //       Verse.Log.Message("tick11");
                var powerComp = thing.TryGetComp<CompPowerTrader>();
         //       Verse.Log.Message("tick12");

                if (powerComp != null)
                {
         //           Verse.Log.Message("tick13");
                    // Set the power requirement 
                    powerComp.PowerOutput = powerLevels[thing.def.defName][0];
         //           Verse.Log.Message("tick14");
                }
            }

            var visibleBuildings = Find.AnyPlayerHomeMap.listerBuildings.allBuildingsColonist.Count;
       //     Verse.Log.Message("tick15");
            if (visibleBuildings != lastVisibleBuildings)
            {
       //         Verse.Log.Message("tick16");
                lastVisibleBuildings = visibleBuildings;
                ticksToRescan = 0; // Rescan now
            }
            --ticksToRescan;
            if (ticksToRescan < 0)
            {
     //           Verse.Log.Message("tick19");
                ticksToRescan = 2000;
                // Destructively modifies the things to modify power on, do the state resetting first
     //           Verse.Log.Message("tick20");
                ScanForThings();
            }
     //       Verse.Log.Message("tick21");
            foreach (Building building in buildingsThatWereUsedLastTick)
            {
   //             Verse.Log.Message("tick2_2");
                // Skip modifying power on things we're not supposed to modify power on
                if (!buildingsToModifyPowerOn.Contains(building)) { continue; }     
                var powerComp = building.TryGetComp<CompPowerTrader>();    //            
                if (powerComp != null)
                {
    //                Verse.Log.Message("tick2_5");
                    // Set the power requirement to high if the building is in use
                    powerComp.PowerOutput = powerLevels[building.def.defName][1];
      //              Verse.Log.Message("tick2_6");
                }
            }
        }

        public static TurnItOnandOff instance;
        public static void Log(string log)
        {
            if (instance == null) return;
            instance.Logger.Message(log);
        }

        private HugsLib.Settings.SettingHandle<float> multiplier;
        private HugsLib.Settings.SettingHandle<int> minvalue;
        HashSet<String> whitelist = new HashSet<String>();


        private void initPowerValues()
        {
            powerLevels.Clear();

            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                // check if this uses power.
                // first, get the power properties.

                var powerProps = def.GetCompProperties<CompProperties_Power>();
                if (powerProps != null && typeof(CompPowerTrader).IsAssignableFrom(powerProps.compClass))
                {
                    //this thing uses power. 
                    //Verse.Log.Message(def.defName);

                    if (powerLevels.ContainsKey(def.defName))
                    {
                        Verse.Log.Message("already contains def?");
                    }

                    if (whitelist.Contains(def.defName) || typeof(Building_WorkTable).IsAssignableFrom(def.thingClass))
                    {
                        RegisterDefThatUsesPower(def.defName,
                            -1 * minvalue,
                            powerProps.basePowerConsumption * multiplier * -1);
                    }

                }

            }

        }

        public override void DefsLoaded()
        {

            multiplier = Settings.GetHandle<float>(
            "tioao_multipl",
            "Active power usage multiplier",
            "Active power usage multiplier",
            1.0f);
            minvalue = Settings.GetHandle<int>(
            "tioao_minval",
            "Idle power usage value",
            "Idle power usage value",
            1);
                    

            whitelist.Add("DeepDrill");
            whitelist.Add("ElectricCrematorium");
            whitelist.Add("HiTechResearchBench");
            whitelist.Add("FabricationBench");
            whitelist.Add("BiofuelRefinery");
            whitelist.Add("ElectricSmelter");
            whitelist.Add("ElectricStove");
            whitelist.Add("TableMachining");
            whitelist.Add("ElectricSmithy");
            whitelist.Add("ElectricTailoringBench");
            whitelist.Add("VitalsMonitor");
            whitelist.Add("MultiAnalyzer");
            whitelist.Add("MegascreenTelevision");
            whitelist.Add("FlatscreenTelevision");
            whitelist.Add("TubeTelevision");
            whitelist.Add("GroundPenetratingScanner");
            whitelist.Add("LongRangeMineralScanner");
            
            
            buildingDefsReservable.Add(ThingDef.Named("LongRangeMineralScanner"));
            buildingDefsReservable.Add(ThingDef.Named("GroundPenetratingScanner"));
            buildingDefsReservable.Add(ThingDef.Named("DeepDrill"));

            medicalBedDef = ThingDef.Named("HospitalBed");
            HiTechResearchBenchDef = ThingDef.Named("HiTechResearchBench");
            AutodoorDef = ThingDef.Named("Autodoor");
            DeepDrillDef = ThingDef.Named("DeepDrill");

            initPowerValues();
        }


        public override void SettingsChanged()
        {
            initPowerValues();
        }

        public override void Initialize()
        {            
            instance = this;
            Logger.Message("Registered instance");
        }

        // Power levels pairs as Vector2's, X = Idling, Y = In Use
        static Dictionary<string, Vector2> powerLevels = new Dictionary<string, Vector2>();
        static void RegisterDefThatUsesPower(string defName, float idlePower, float activePower)
        {
            powerLevels.Add(defName, new Vector2(idlePower, activePower));            
        }

        
        #region tracking
        public static int inUseTick = 0;
        public static HashSet<Building> buildingsThatWereUsedLastTick = new HashSet<Building>();
        public static HashSet<Building> buildingsInUseThisTick = new HashSet<Building>();
        public static HashSet<Building> buildingsToModifyPowerOn = new HashSet<Building>();

        public static HashSet<ThingDef> buildingDefsReservable = new HashSet<ThingDef>();
        public static HashSet<Building> reservableBuildings = new HashSet<Building>();

        public static HashSet<Building_Bed> MedicalBeds = new HashSet<Building_Bed>();
        public static HashSet<Building> HiTechResearchBenches = new HashSet<Building>();

        public static HashSet<Building_Door> Autodoors = new HashSet<Building_Door>();
        public static HashSet<Building> DeepDrills = new HashSet<Building>();

        private static ThingDef medicalBedDef;
        private static ThingDef HiTechResearchBenchDef;
        private static ThingDef AutodoorDef;
        private static ThingDef DeepDrillDef;

        public static void AddBuildingUsed(Building building)
        {
            buildingsInUseThisTick.Add(building);
        }
        
        public static void ScanExternalReservable()
        {
            reservableBuildings.Clear();
            foreach (ThingDef def in buildingDefsReservable)
            {
                foreach (var map in Find.Maps)
                {
                    if (map == null) continue;
                    var buildings = map.listerBuildings.AllBuildingsColonistOfDef(def);
                    foreach (var building in buildings)
                    {
                        if (building == null) continue;
                        reservableBuildings.Add(building);
                    }
                }
            }
        }

        public static void EvalExternalReservable()
        {
            foreach (var building in reservableBuildings)
            {                
                if (building?.Map == null) continue;

                if (building.Map.reservationManager.IsReservedByAnyoneOf(building, building.Faction))
                {
                    buildingsInUseThisTick.Add(building);
                }
            }
        }

        // Evaluate medical beds for medical beds in use, to register that the vitals monitors should be in high power mode
        public static void EvalVitalMon()
        {
            foreach (var mediBed in MedicalBeds)
            {
                if (mediBed?.Map == null) continue;

                bool occupied = false;
                foreach (var occupant in mediBed.CurOccupants)
                {
                    occupied = true;
                }

                if (!occupied) continue;
                var facilityAffector = mediBed.GetComp<CompAffectedByFacilities>();
                foreach (var facility in facilityAffector.LinkedFacilitiesListForReading)
                {
                    buildingsInUseThisTick.Add(facility as Building);
                }
            }
        }
                
        // How to tell if a research table is in use?
        // I can't figure it out. Instead let's base it on being reserved for use
        public static void EvalResearchTablesandAnalyzers()
        {
            foreach (var researchTable in HiTechResearchBenches)
            {
                if (researchTable?.Map == null) continue;

                // Determine if we are reserved:
                var inUse = researchTable.Map.reservationManager.IsReservedByAnyoneOf(researchTable, researchTable.Faction);

                if (!inUse) continue;

                buildingsInUseThisTick.Add(researchTable);
                var facilityAffector = researchTable.GetComp<CompAffectedByFacilities>();
                foreach (var facility in facilityAffector.LinkedFacilitiesListForReading)
                {
                    buildingsInUseThisTick.Add(facility as Building);
                }
            }
        }

        public static void EvalAutodoors()
        {
            foreach (var autodoor in Autodoors)
            {
                if (autodoor == null) continue;
                if (autodoor.Map == null) continue;

                // If the door allows passage and isn't blocked by an object
                var inUse = autodoor.Open && (!autodoor.BlockedOpenMomentary);
                if (inUse) buildingsInUseThisTick.Add(autodoor);
            }
        }

        public static HashSet<ThingDef> thingDefsToLookFor;
        public static void ScanForThings()
        {            
            // Build the set of def names to look for if we don't have it
            if (thingDefsToLookFor == null)
            {
                thingDefsToLookFor = new HashSet<ThingDef>();
                var defNames = powerLevels.Keys;
                foreach (var defName in defNames)
                {
                    thingDefsToLookFor.Add(ThingDef.Named(defName));
                }
            }
            
            ScanExternalReservable(); // Handle the scanning of external reservable objects

            buildingsToModifyPowerOn.Clear();
            MedicalBeds.Clear();
            HiTechResearchBenches.Clear();
          //  Autodoors.Clear();
           // DeepDrills.Clear();
            
            var maps = Find.Maps;
            foreach (Map map in maps)
            {
                foreach (ThingDef def in thingDefsToLookFor)
                {                    
                    var matchingThings = map.listerBuildings.AllBuildingsColonistOfDef(def);
                    // Merge in all matching things
                    buildingsToModifyPowerOn.UnionWith(matchingThings);                    
                }
                                
                // Register the medical beds in the watch list
                var mediBeds = map.listerBuildings.AllBuildingsColonistOfDef(medicalBedDef);
                foreach (var mediBed in mediBeds)
                {
                    var medicalBed = mediBed as Building_Bed;
                    MedicalBeds.Add(medicalBed);
                }

                // Register Hightech research tables too
                var researchTables = map.listerBuildings.AllBuildingsColonistOfDef(HiTechResearchBenchDef);
                HiTechResearchBenches.UnionWith(researchTables);

                /*
                var doors = map.listerBuildings.AllBuildingsColonistOfDef(AutodoorDef);
                foreach (var door in doors)
                {
                    var autodoor = door as Building_Door;
                    Autodoors.Add(autodoor);
                }

                var deepDrills = map.listerBuildings.AllBuildingsColonistOfDef(DeepDrillDef);
                DeepDrills.UnionWith(deepDrills);
                */
            }
        }
        #endregion
    }
}