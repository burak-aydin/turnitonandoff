using Verse;
using RimWorld;
using System.Collections.Generic;

namespace TurnItOnandOff
{

    public class MapDatabase
    {
        public int visibleBuildingCount = 0;
        public HashSet<Building> ReservableBuildings = new HashSet<Building>();
        public HashSet<Building> HiTechResearchBenches = new HashSet<Building>();
        public HashSet<Building> WastepackAtomizers = new HashSet<Building>();

        public HashSet<Building_Bed> MedicalBeds = new HashSet<Building_Bed>();
        public HashSet<Building_Door> Autodoors = new HashSet<Building_Door>();
        public HashSet<Building> LoudSpeakers = new HashSet<Building>();
        public HashSet<Building> LightBalls = new HashSet<Building>();

        public int ticksToRescan = 0;

        public HashSet<Building> buildingsToModify = new HashSet<Building>();

        public void Clear()
        {
            ReservableBuildings.Clear();
            HiTechResearchBenches.Clear();
            MedicalBeds.Clear();
            Autodoors.Clear();
            buildingsToModify.Clear();
            ticksToRescan = 0;
        }
    }
}