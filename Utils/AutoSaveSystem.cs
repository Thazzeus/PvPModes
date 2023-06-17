using PvPModes.Commands;
using PvPModes.Systems;
using System;
using UnityEngine.Rendering.HighDefinition;

namespace PvPModes.Utils
{
    public static class AutoSaveSystem
    {
        //-- AutoSave is now directly hooked into the Server game save activity.
        public const string mainSaveFolder = "BepInEx/config/PvPModes/Saves/";
        public const string backupSaveFolder = "BepInEx/config/PvPModes/Saves/Backup/";
        public static int saveCount = 0;
        private static int backupFrequency = 5;
        public static void SaveDatabase(){
            saveCount++;
            string saveFolder = mainSaveFolder;
            if(saveCount % backupFrequency == 0) {
                saveFolder = backupSaveFolder;
            }
            PermissionSystem.SaveUserPermission(); //-- Nothing new to save.
            //GodMode.SaveGodMode(saveFolder);
            
            //SunImmunity.SaveImmunity();
            //Waypoint.SaveWaypoints();
            //NoCooldown.SaveCooldown(saveFolder);
            //Speed.SaveSpeed();
            //AutoRespawn.SaveAutoRespawn();
            //Kit.SaveKits();   //-- Nothing to save here for now.
            //PowerUp.SavePowerUp(saveFolder);

            //-- System Related
            //ExperienceSystem.SaveEXPData(saveFolder);
            PvPSystem.SavePvPStat();
            //WeaponMasterSystem.SaveWeaponMastery(saveFolder);
            //Bloodlines.saveBloodlines(saveFolder);
            //BanSystem.SaveBanList();
            //WorldDynamicsSystem.SaveFactionStats(saveFolder);
            //WorldDynamicsSystem.SaveIgnoredMobs(saveFolder);

            Plugin.Logger.LogInfo(DateTime.Now+"All database saved to JSON file.");
        }

        public static void LoadDatabase()
        {
            //-- Commands Related
            //PermissionSystem.LoadPermissions();
            //NoCooldown.LoadNoCooldown();
            //GodMode.LoadGodMode();
            //PowerUp.LoadPowerUp();

            /*
            SunImmunity.LoadSunImmunity();
            Waypoint.LoadWaypoints();
            Speed.LoadSpeed();
            AutoRespawn.LoadAutoRespawn();
            Kit.LoadKits();*/

            //-- System Related
            PvPSystem.LoadPvPStat();
            //ExperienceSystem.LoadEXPData();
            //WeaponMasterSystem.LoadWeaponMastery();
            //Bloodlines.loadBloodlines();
            //BanSystem.LoadBanList();
            //WorldDynamicsSystem.LoadFactionStats();
            //WorldDynamicsSystem.LoadIgnoredMobs();

            Plugin.Logger.LogInfo("All database is now loaded.");
        }
    }
}
