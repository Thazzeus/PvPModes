using HarmonyLib;
using ProjectM;
using PvPModes.Utils;

namespace PvPModes.Hooks
{
    [HarmonyPatch(typeof(TriggerPersistenceSaveSystem), nameof(TriggerPersistenceSaveSystem.TriggerSave))]
    public class TriggerPersistenceSaveSystem_Patch
    {
        public static void Prefix()
        {
            AutoSaveSystem.SaveDatabase();
        }
    }
}
