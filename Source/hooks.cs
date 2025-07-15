using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace TurnItOnandOff
{

    // Track the tables
    [HarmonyPatch(typeof(Building_WorkTable), "UsedThisTick", new Type[] { })]
    public static class Building_WorkTable_UsedThisTick_Patch
    {
        [HarmonyPrefix]
        public static void UsedThisTick(Building_WorkTable __instance)
        {
            TurnItOnandOff.singleton.setBuildingAsUsed(__instance);
        }
    }


    // Track the watchbuilding jobs
    [HarmonyPatch(typeof(JobDriver_WatchBuilding), "WatchTickAction")]
    public static class JobDriver_WatchBuilding_WatchTickAction_Patch
    {
        [HarmonyPrefix]
        public static void WatchTickAction(JobDriver_WatchBuilding __instance)
        {
            TurnItOnandOff.singleton.setBuildingAsUsed(__instance.job.targetA.Thing as Building);
        }
    }

}
