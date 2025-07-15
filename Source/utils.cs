using HugsLib;
using Verse;


namespace TurnItOnandOff
{

    public static class Utils
    {
        public static void Message(string log, bool important)
        {
            if (important || ModSettings.VerboseLogging)
            {
                Log.Message(log);
            }
        }
        public static void Error(string log)
        {
            Log.Error(log);
        }

         public static void Warning(string log)
        {
            Log.Warning(log);
        }
    }
   


}