using HugsLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;


namespace TurnItOnandOff
{
    public class TurnItOnandOff : ModBase
    {
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
            Utils.Message("initialized", true);
            singleton = this;
        }
        public override void SettingsChanged()
        {
            ModSettings.ReadSettings(Settings);
            initPowerValues();
            Clear();
        }

        public override void DefsLoaded()
        {
            ModSettings.ReadSettings(Settings);
            ModDefs.ReadDefs();
            initPowerValues();
            Clear();
        }

        private void initPowerValues()
        {
            ModDefs.ClearBuildingPowerConfig();
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                // check if this uses power.
                // first, get the power properties.
                var powerProps = def.GetCompProperties<CompProperties_Power>();
                if (powerProps != null && typeof(CompPowerTrader).IsAssignableFrom(powerProps.compClass))
                {
                    //this thing uses power.
                    if ((ModSettings.WhitelistedDefs.Contains(def.defName) || typeof(Building_WorkTable).IsAssignableFrom(def.thingClass)) && !ModSettings.BlacklistedDefs.Contains(def.defName))
                    {
                        Utils.Message("powerUser:" + def.defName, false);

                        ModDefs.SetBuildingPowerConfig(def.defName,
                            -1 * ModSettings.IdlePowerUsage,
                            powerProps.PowerConsumption * ModSettings.ActivePowerMultiplier * -1);
                    }
                    else
                    {
                        Utils.Message("excluded powerUser:" + def.defName, false);
                    }
                }
            }
        }

        //THIS WILL RESET THE STATE OF THE MOD EVERYTIME A MAP LOADS.
        //NOT A PERFECT SOLUTION BUT SHOULD WORK GOOD ENOUGH
        //note from the future: I dont remember why i added this, i will just trust my previous judgement for now
        public override void MapLoaded(Verse.Map map)
        {
            Clear();
        }

        // DATA
        public List<MapDatabase> mapDatabase = new List<MapDatabase>();
        public HashSet<Building> buildingsInUseThisTick = new HashSet<Building>();
        public HashSet<Building> buildingsThatWereUsedLastTick = new HashSet<Building>();

        public void Clear()
        {
            mapDatabase.Clear();
            buildingsInUseThisTick.Clear();
            buildingsThatWereUsedLastTick.Clear();
        }
        //LOGIC
        public void setBuildingAsUsed(Building building)
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
                    mapDatabase[i].ticksToRescan = ModSettings.RescanPeriod;
                }

                if (mapDatabase[i].ticksToRescan <= 0)
                {
                    mapDatabase[i].ticksToRescan = ModSettings.RescanPeriod;
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
                        Utils.Message("Tried to modify power level for thing which no longer exists", false);
                        continue;
                    }
                    var powerComp = thing.TryGetComp<CompPowerTrader>();
                    if (powerComp != null)
                    {
                        Vector2 powerUsagesOfDef = ModDefs.GetBuildingPower(thing.def.defName);

                        // Set the power requirement
                        if (buildingsThatWereUsedLastTick.Contains(thing))
                        {
                            powerComp.PowerOutput = powerUsagesOfDef[1];
                        }
                        else
                        {
                            powerComp.PowerOutput = powerUsagesOfDef[0];
                        }
                    }
                }

            }
        }


        public void EvalExternalReservable(MapDatabase data)
        {
            foreach (var building in data.ReservableBuildings)
            {
                if (building != null && building.Map.reservationManager.IsReservedByAnyoneOf(building, building.Faction))
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
            foreach (var speaker in data.LoudSpeakers)
            {
                var speakerComp = speaker.TryGetComp<CompLoudspeaker>();
                // If the door allows passage and isn't blocked by an object
                if (speakerComp != default(CompLoudspeaker))
                {
                    if (speakerComp.Active) buildingsInUseThisTick.Add(speaker);
                }
            }
        }
        public void EvalLightBall(MapDatabase data)
        {
            foreach (var light in data.LightBalls)
            {
                var lightComp = light.TryGetComp<CompLightball>();
                // If the door allows passage and isn't blocked by an object
                if (lightComp != default(CompLightball))
                {
                    if (lightComp.parent.IsRitualTarget()) buildingsInUseThisTick.Add(light);
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
            }
        }

        //SCANNING LOGIC
        public void ScanForThings(Map map, MapDatabase data)
        {
            data.Clear();

            foreach (ThingDef def in ModDefs.GetBuildingDefs())
            {
                var matchingThings = map.listerBuildings.AllBuildingsColonistOfDef(def);
                // Merge in all matching things
                data.buildingsToModify.UnionWith(matchingThings);
            }

            // Register the medical beds in the watch list
            var mediBeds = map.listerBuildings.AllBuildingsColonistOfDef(ModDefs.MedicalBedDef);
            foreach (var mediBed in mediBeds)
            {
                var medicalBed = mediBed as Building_Bed;
                data.MedicalBeds.Add(medicalBed);
            }

            // Register Hightech research tables too
            var researchTables = map.listerBuildings.AllBuildingsColonistOfDef(ModDefs.HiTechResearchBenchDef);
            data.HiTechResearchBenches.UnionWith(researchTables);

            if (ModDefs.LoudSpeakerDef != null)
            {
                var loudspeakers = map.listerBuildings.AllBuildingsColonistOfDef(ModDefs.LoudSpeakerDef);
                data.LoudSpeakers.UnionWith(loudspeakers);
            }
            if (ModDefs.LightBallDef != null)
            {
                var lightballs = map.listerBuildings.AllBuildingsColonistOfDef(ModDefs.LightBallDef);
                data.LightBalls.UnionWith(lightballs);
            }
            if (ModDefs.AtomizerDef != null)
            {
                var atomizers = map.listerBuildings.AllBuildingsColonistOfDef(ModDefs.AtomizerDef);
                data.WastepackAtomizers.UnionWith(atomizers);
            }

            foreach (string def in ModSettings.ReservableDefs)
            {
                var buildings = map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named(def));
                foreach (var building in buildings)
                {
                    data.ReservableBuildings.Add(building);
                }

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
    }
}