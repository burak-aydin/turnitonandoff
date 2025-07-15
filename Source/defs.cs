using Verse;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace TurnItOnandOff
{
    static public class ModDefs
    {
        //vanilla defs
        public static ThingDef MedicalBedDef;
        public static ThingDef HiTechResearchBenchDef;
        public static ThingDef AutodoorDef;

        //dlc defs
        public static ThingDef LoudSpeakerDef;
        public static ThingDef LightBallDef;
        public static ThingDef AtomizerDef;

        public static void ReadDefs()
        {
            MedicalBedDef = ThingDef.Named("HospitalBed");
            HiTechResearchBenchDef = ThingDef.Named("HiTechResearchBench");
            AutodoorDef = ThingDef.Named("Autodoor");

            //dlc defs
            LoudSpeakerDef = DefDatabase<ThingDef>.GetNamed("Loudspeaker", false);
            LoudSpeakerDef = LoudSpeakerDef == default(ThingDef) ? null : LoudSpeakerDef;

            LightBallDef = DefDatabase<ThingDef>.GetNamed("LightBall", false);
            LightBallDef = LightBallDef == default(ThingDef) ? null : LightBallDef;

            AtomizerDef = DefDatabase<ThingDef>.GetNamed("WastepackAtomizer", false);
            AtomizerDef = AtomizerDef == default(ThingDef) ? null : AtomizerDef;
        }

        private static Dictionary<string, Vector2> BuildingPowerMap = new Dictionary<string, Vector2>();

        public static void SetBuildingPowerConfig(string defName, float idlePower, float activePower)
        {
            BuildingPowerMap.Add(defName, new Vector2(idlePower, activePower));
        }

        public static void ClearBuildingPowerConfig()
        {
            BuildingPowerMap.Clear();
            BuildingDefs?.Clear();
        }

        public static Vector2 GetBuildingPower(string defName)
        {
            if (BuildingPowerMap.ContainsKey(defName))
            {
                return BuildingPowerMap[defName];
            }
            Utils.Error(string.Format("trying to get building power for def: {0}", defName));
            return new Vector2();
        }

        private static HashSet<ThingDef> BuildingDefs;
        public static HashSet<ThingDef> GetBuildingDefs()
        {
            if (BuildingDefs == null)
            {
                var defNames = BuildingPowerMap.Keys;
                BuildingDefs = new HashSet<ThingDef>(defNames.Count);
                foreach (var defName in defNames)
                {
                    BuildingDefs.Add(ThingDef.Named(defName));
                }
            }

            return BuildingDefs;
        }


    }
}
