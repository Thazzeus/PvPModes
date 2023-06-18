using HarmonyLib;
using Il2CppSystem;
using ProjectM;
using ProjectM.Auth;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Terrain;
using PvPModes.Systems;
using PvPModes.Utils;
using Stunlock.Network;
using System;
using System.Reflection;
using Unity.DebugDisplay;
using static Unity.Entities.Conversion.IncrementalConversionContext.RemoveFromHierarchy;


namespace PvPModes.Hooks
{
    /*
    [HarmonyPatch(typeof(LoadPersistenceSystemV2), nameof(LoadPersistenceSystemV2.SetLoadState))]
    public class PersistenceSystem_Patch
    {
        public static void Prefix(ServerStartupState.State loadState, LoadPersistenceSystemV2 __instance)
        {
            if (loadState == ServerStartupState.State.SuccessfulStartup)
            {
                Plugin.Initialize();
            }
        }
    }*/

    
    [HarmonyPatch(typeof(SettingsManager), nameof(SettingsManager.VerifyServerGameSettings))]
    public class ServerGameSetting_Patch
    {
        private static bool isInitialized = false;
		public static void Postfix()
        {
			//System.Console.WriteLine("Checking if isInit is T/F");
			//System.Console.WriteLine(isInitialized);
			if (isInitialized == false)
            {
				System.Console.WriteLine("Init is False..");
				Plugin.Initialize();
				System.Console.WriteLine("Init is now set to True..");
				isInitialized = true;
				System.Console.WriteLine("ServerGameSetting_Patch Complete..");
			}
        }
    }
    /*  ///COMMENTED OUT PRIOR --THAZZEUS
    [HarmonyPatch(typeof(HandleGameplayEventsSystem), nameof(HandleGameplayEventsSystem.OnUpdate))]
    public class InitializationPatch
    {
        [HarmonyPostfix]
        public static void PvPModes_Initialize_Method()
        {
            Plugin.Initialize();
            Plugin.harmony.Unpatch(typeof(HandleGameplayEventsSystem).GetMethod("OnUpdate"), typeof(InitializationPatch).GetMethod("PvPModes_Initialize_Method"));
        }
    }*/

    [HarmonyPatch(typeof(GameBootstrap), nameof(GameBootstrap.Start))]
    public static class GameBootstrap_Patch
    {
        public static void Postfix()
        {
			Plugin.Initialize();
			System.Console.WriteLine("GameBootstrat Complete..");
		}
    }

    [HarmonyPatch(typeof(GameBootstrap), nameof(GameBootstrap.OnApplicationQuit))]
    public static class GameBootstrapQuit_Patch
    {
        public static void Prefix()
        {
            AutoSaveSystem.SaveDatabase();
        }
    }

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
    public static class OnUserConnected_Patch
    {
        public static void Postfix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
        {
            try
            {
                var em = __instance.EntityManager;
                var userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
                var serverClient = __instance._ApprovedUsersLookup[userIndex];
                var userEntity = serverClient.UserEntity;
                var userData = __instance.EntityManager.GetComponentData<User>(userEntity);
                bool isNewVampire = userData.CharacterName.IsEmpty;

                if (!isNewVampire)
                {
                    if (PvPSystem.isHonorSystemEnabled)
                    {
                        if (PvPSystem.isHonorTitleEnabled) Helper.RenamePlayer(userEntity, userData.LocalCharacter._Entity, userData.CharacterName);

                        Database.PvPStats.TryGetValue(userData.PlatformId, out var pvpStats);
                        Database.SiegeState.TryGetValue(userData.PlatformId, out var siegeState);

                        if (pvpStats.Reputation <= -1000)
                        {
                            PvPSystem.HostileON(userData.PlatformId, userData.LocalCharacter._Entity, userEntity);
                        }
                        else
                        {
                            if (!siegeState.IsSiegeOn)
                            {
                                PvPSystem.HostileOFF(userData.PlatformId, userData.LocalCharacter._Entity);
                            }
                        }
                    }
                    else
                    {
                        var playerName = userData.CharacterName.ToString();
                        Helper.UpdatePlayerP_Cache(userEntity, playerName, playerName);
                    }
                }
            }
            catch { }
        }
    }
    /*
    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.BeginSetupServer))]
    public static class BeginSetupServer_Patch
    {
        private static void Prefix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId, ConnectionStatusChangeReason connectionStatusReason, string extraData)
        {
            Plugin.Initialize();
        }
    }*/
}
