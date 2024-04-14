using HarmonyLib;
using HugsLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;


namespace TurnItOnandOff
{

    public class MapDatabase
    {
        public int visibleBuildingCount = 0;
        public HashSet<Building> reservableBuildings = new HashSet<Building>();
        public HashSet<Building> HiTechResearchBenches = new HashSet<Building>();
        public HashSet<Building> WastepackAtomizers = new HashSet<Building>();

        public HashSet<Building_Bed> MedicalBeds = new HashSet<Building_Bed>();
        public HashSet<Building_Door> Autodoors = new HashSet<Building_Door>();
        public HashSet<Building> loudSpeakers = new HashSet<Building>();
        public HashSet<Building> lightBalls = new HashSet<Building>();

        public int ticksToRescan = 0;

        public HashSet<Building> buildingsToModify = new HashSet<Building>();

        public void clear()
        {
            reservableBuildings.Clear();
            HiTechResearchBenches.Clear();
            MedicalBeds.Clear();
            Autodoors.Clear();
            buildingsToModify.Clear();
            ticksToRescan = 0;
        }
    }

    // Track the tables
    [HarmonyPatch(typeof(Building_WorkTable), "UsedThisTick", new Type[] { })]
    public static class Building_WorkTable_UsedThisTick_Patch
    {
        [HarmonyPrefix]
        public static void UsedThisTick(Building_WorkTable __instance)
        {
            TurnItOnandOff.singleton.useBuilding(__instance);
        }
    }



    // Track the watchbuilding jobs
    [HarmonyPatch(typeof(JobDriver_WatchBuilding), "WatchTickAction", new Type[] { })]
    public static class JobDriver_WatchBuilding_WatchTickAction_Patch
    {
        [HarmonyPrefix]
        public static void WatchTickAction(JobDriver_WatchBuilding __instance)
        {
            TurnItOnandOff.singleton.useBuilding(__instance.job.targetA.Thing as Building);
        }
    }


    public class TurnItOnandOff : ModBase
    {
        private int rescanPeriod = 2000;

        public static TurnItOnandOff singleton;
        public override string ModIdentifier
        {
            get
            {
                return "TurnItOnandOff";
            }
        }

        public override void Initialize()
        {
            Logger.Message("Initialized");
            singleton = this;
        }
        public override void SettingsChanged()
        {
            initPowerValues();
            rescanPeriod = Settings.GetHandle<int>(
            "tioao_period",
            "Force refresh rate in ticks",
            "Force refresh rate in ticks, fixes desyncronized machines. Increasing this reduces performance impact, but things might occasionally misbehave",
            2000);
        }


        // INITIALIZE
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
            whitelist.Add("Loudspeaker");
            whitelist.Add("LightBall");
            whitelist.Add("WastepackAtomizer");



            reservableDefs.Add(ThingDef.Named("LongRangeMineralScanner"));
            reservableDefs.Add(ThingDef.Named("GroundPenetratingScanner"));
            reservableDefs.Add(ThingDef.Named("DeepDrill"));

            LoudSpeakerDef = DefDatabase<ThingDef>.GetNamed("Loudspeaker", false);
            LoudSpeakerDef = LoudSpeakerDef == default(ThingDef) ? null : LoudSpeakerDef;
            LightBallDef = LoudSpeakerDef == null ? null : ThingDef.Named("LightBall");

            AtomizerDef = DefDatabase<ThingDef>.GetNamed("WastepackAtomizer", false);
            AtomizerDef = AtomizerDef == default(ThingDef) ? null : AtomizerDef;

            medicalBedDef = ThingDef.Named("HospitalBed");
            HiTechResearchBenchDef = ThingDef.Named("HiTechResearchBench");
            AutodoorDef = ThingDef.Named("Autodoor");

            SettingsChanged();
        }

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
                    if (whitelist.Contains(def.defName) || typeof(Building_WorkTable).IsAssignableFrom(def.thingClass))
                    {
                        Log.Message("PowerUser:" + def.defName);

                        RegisterDefThatUsesPower(def.defName,
                            -1 * minvalue,
                            powerProps.PowerConsumption * multiplier * -1);
                    }
                    else
                    {
                        Log.Message("Excluded PowerUser:" + def.defName);
                    }

                }

            }

        }

        void RegisterDefThatUsesPower(string defName, float idlePower, float activePower)
        {
            powerLevels.Add(defName, new Vector2(idlePower, activePower));
        }

        //THIS WILL RESET THE STATE OF THE MOD EVERYTIME A MAP LOADS.
        //NOT A PERFECT SOLUTION BUT SHOULD WORK GOOD ENOUGH
        public override void MapLoaded(Verse.Map map)
        {
            mapDatabase.Clear();
            buildingsInUseThisTick.Clear();
            buildingsThatWereUsedLastTick.Clear();
        }

        // DATA

        HashSet<String> whitelist = new HashSet<String>();

        private HugsLib.Settings.SettingHandle<float> multiplier;
        private HugsLib.Settings.SettingHandle<int> minvalue;


        public HashSet<ThingDef> reservableDefs = new HashSet<ThingDef>();
        private ThingDef medicalBedDef;
        private ThingDef HiTechResearchBenchDef;
        private ThingDef AutodoorDef;
        private ThingDef LoudSpeakerDef;
        private ThingDef LightBallDef;
        private ThingDef AtomizerDef;



        public List<MapDatabase> mapDatabase = new List<MapDatabase>();
        Dictionary<string, Vector2> powerLevels = new Dictionary<string, Vector2>();


        public HashSet<Building> buildingsInUseThisTick = new HashSet<Building>();
        public HashSet<Building> buildingsThatWereUsedLastTick = new HashSet<Building>();




        //LOGIC

        public void useBuilding(Building building)
        {
            buildingsInUseThisTick.Add(building);
        }

        public override void Tick(int currentTick)
        {
            buildingsThatWereUsedLastTick = new HashSet<Building>(buildingsInUseThisTick);
            buildingsInUseThisTick.Clear();

            //// SCANNING BUILDINGS LOGIC

            // CHECK IF NUMBER OF MAPS CORRECT, IF NOT RECREATE NUMBERBUILDINGS LIST
            if (mapDatabase.Count != Find.Maps.Count)
            {
                mapDatabase = new List<MapDatabase>(Find.Maps.Count);
                for (int i = 0; i < Find.Maps.Count; i++)
                {
                    mapDatabase.Add(new MapDatabase());
                }
            }

            //CHECK IF VISIBLE BUILDINGS NUMBER IS SAME, IF NOT RESCAN
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];

                if (mapDatabase[i].visibleBuildingCount != map.listerBuildings.allBuildingsColonist.Count)
                {
                    mapDatabase[i].visibleBuildingCount = map.listerBuildings.allBuildingsColonist.Count;
                    ScanForThings(map, mapDatabase[i]);
                    mapDatabase[i].ticksToRescan = rescanPeriod;
                }

                if (mapDatabase[i].ticksToRescan <= 0)
                {
                    mapDatabase[i].ticksToRescan = rescanPeriod;
                    ScanForThings(map, mapDatabase[i]);
                }
                mapDatabase[i].ticksToRescan--;
            }


            //// MODIFY POWER LOGIC

            foreach (MapDatabase data in mapDatabase)
            {
                EvalVitalMon(data);
                EvalResearchTablesandAnalyzers(data);
                EvalExternalReservable(data);
                EvalSpeakers(data);
                EvalLightBall(data);
                EvalWastepackAtomizer(data);
                foreach (Building thing in data.buildingsToModify)
                {
                    if (thing == null)
                    {
                        Logger.Message("Tried to modify power level for thing which no longer exists");
                        continue;
                    }
                    var powerComp = thing.TryGetComp<CompPowerTrader>();
                    if (powerComp != null)
                    {

                        // Set the power requirement
                        if (buildingsThatWereUsedLastTick.Contains(thing))
                        {
                            powerComp.PowerOutput = powerLevels[thing.def.defName][1];
                        }
                        else
                        {
                            powerComp.PowerOutput = powerLevels[thing.def.defName][0];
                        }
                    }
                }

            }
        }


        public void EvalExternalReservable(MapDatabase data)
        {
            foreach (var building in data.reservableBuildings)
            {
                if (building.Map.reservationManager.IsReservedByAnyoneOf(building, building.Faction))
                {
                    buildingsInUseThisTick.Add(building);
                }
            }
        }

        // Evaluate medical beds for medical beds in use, to register that the vitals monitors should be in high power mode
        public void EvalVitalMon(MapDatabase data)
        {
            foreach (var mediBed in data.MedicalBeds)
            {
                bool occupied = false;

                foreach (var occupant in mediBed.CurOccupants)
                {
                    if (occupant != null && occupant != default(Pawn))
                    {
                        occupied = true;
                        break;
                    }
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
        public void EvalResearchTablesandAnalyzers(MapDatabase data)
        {
            foreach (var researchTable in data.HiTechResearchBenches)
            {
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
        public void EvalAutodoors(MapDatabase data)
        {
            foreach (var autodoor in data.Autodoors)
            {
                // If the door allows passage and isn't blocked by an object
                var inUse = autodoor.Open && (!autodoor.BlockedOpenMomentary);
                if (inUse) buildingsInUseThisTick.Add(autodoor);
            }
        }

        public void EvalSpeakers(MapDatabase data)
        {
            foreach (var speaker in data.loudSpeakers)
            {
                var speakerComp = speaker.TryGetComp<CompLoudspeaker>();
                // If the door allows passage and isn't blocked by an object
                if (speakerComp != default(CompLoudspeaker))
                {
                    if (speakerComp.Active) buildingsInUseThisTick.Add(speaker);
                }
                else
                {
                    Logger.Warning("LoudSpeaker comp error");
                }
            }
        }
        public void EvalLightBall(MapDatabase data)
        {
            foreach (var light in data.lightBalls)
            {
                var lightComp = light.TryGetComp<CompLightball>();
                // If the door allows passage and isn't blocked by an object
                if (lightComp != default(CompLightball))
                {
                    if (lightComp.parent.IsRitualTarget()) buildingsInUseThisTick.Add(light);
                }
                else
                {
                    Logger.Warning("LightBall comp error");
                }
            }
        }

        public void EvalWastepackAtomizer(MapDatabase data)
        {
            foreach (var atomizer in data.WastepackAtomizers)
            {
                // Determine if we are reserved:
                var atomizerComp = atomizer.TryGetComp<CompAtomizer>();

                if (atomizerComp != default(CompAtomizer))
                {
                    if (atomizerComp.FillPercent != 0)
                    {
                        buildingsInUseThisTick.Add(atomizer);
                    }
                }
                else
                {
                    Logger.Warning("Atomizer comp error");
                }
            }
        }

        //SCANNING LOGIC
        public HashSet<ThingDef> thingDefsToLookFor;
        public void ScanForThings(Map map, MapDatabase data)
        {
            // Build the set of def names to look for if we don't have it
            if (thingDefsToLookFor == null)
            {
                var defNames = powerLevels.Keys;
                thingDefsToLookFor = new HashSet<ThingDef>(defNames.Count);
                foreach (var defName in defNames)
                {
                    thingDefsToLookFor.Add(ThingDef.Named(defName));
                }
            }

            data.clear();
            ScanExternalReservable(map, data); // Handle the scanning of external reservable objects

            foreach (ThingDef def in thingDefsToLookFor)
            {
                var matchingThings = map.listerBuildings.AllBuildingsColonistOfDef(def);
                // Merge in all matching things
                data.buildingsToModify.UnionWith(matchingThings);
            }

            // Register the medical beds in the watch list
            var mediBeds = map.listerBuildings.AllBuildingsColonistOfDef(medicalBedDef);
            foreach (var mediBed in mediBeds)
            {
                var medicalBed = mediBed as Building_Bed;
                data.MedicalBeds.Add(medicalBed);
            }

            // Register Hightech research tables too
            var researchTables = map.listerBuildings.AllBuildingsColonistOfDef(HiTechResearchBenchDef);
            data.HiTechResearchBenches.UnionWith(researchTables);

            if (LoudSpeakerDef != null)
            {
                var loudspeakers = map.listerBuildings.AllBuildingsColonistOfDef(LoudSpeakerDef);
                data.loudSpeakers.UnionWith(loudspeakers);
            }
            if (LightBallDef != null)
            {
                var lightballs = map.listerBuildings.AllBuildingsColonistOfDef(LightBallDef);
                data.lightBalls.UnionWith(lightballs);
            }
            if (AtomizerDef != null)
            {
                var atomizers = map.listerBuildings.AllBuildingsColonistOfDef(AtomizerDef);
                data.WastepackAtomizers.UnionWith(atomizers);
            }

            /*
            var doors = map.listerBuildings.AllBuildingsColonistOfDef(AutodoorDef);
            foreach (var door in doors)
            {
                var autodoor = door as Building_Door;
                Autodoors.Add(autodoor);
            }
            */


        }

        public void ScanExternalReservable(Map map, MapDatabase data)
        {
            foreach (ThingDef def in reservableDefs)
            {
                var buildings = map.listerBuildings.AllBuildingsColonistOfDef(def);
                foreach (var building in buildings)
                {
                    data.reservableBuildings.Add(building);
                }

            }
        }

    }
}