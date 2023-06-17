using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using PvPModes.Commands;
using PvPModes.Systems;
using PvPModes.Utils;
using Unity.Collections;
using Unity.Entities;

namespace PvPModes.Hooks {
[HarmonyPatch]
public class DeathEventListenerSystem_Patch
{
    [HarmonyPatch(typeof(DeathEventListenerSystem), "OnUpdate")]
    [HarmonyPostfix]
    public static void Postfix(DeathEventListenerSystem __instance)
    {
        //if (__instance._DeathEventQuery != null)
        {
            NativeArray<DeathEvent> deathEvents = __instance._DeathEventQuery.ToComponentDataArray<DeathEvent>(Allocator.Temp);
            foreach (DeathEvent ev in deathEvents)
            {
                //-- Player Creature Kill Tracking
                if (__instance.EntityManager.HasComponent<PlayerCharacter>(ev.Killer) && __instance.EntityManager.HasComponent<Movement>(ev.Died))
                {
                    if (PvPSystem.isHonorSystemEnabled) PvPSystem.MobKillMonitor(ev.Killer, ev.Died);

                }
                //-- ----------------------

                //-- Auto Respawn & HunterHunted System Begin
                if (__instance.EntityManager.HasComponent<PlayerCharacter>(ev.Died))
                {
                    PlayerCharacter player = __instance.EntityManager.GetComponentData<PlayerCharacter>(ev.Died);
                    Entity userEntity = player.UserEntity;
                    User user = __instance.EntityManager.GetComponentData<User>(userEntity);
                    ulong SteamID = user.PlatformId;

                    //-- Check for AutoRespawn
                    if (user.IsConnected)
                    {
                        bool isServerWide = Database.autoRespawn.ContainsKey(1);
                        bool doRespawn;
                        if (!isServerWide)
                        {
                            doRespawn = Database.autoRespawn.ContainsKey(SteamID);
                        }
                        else { doRespawn = true; }

                        if (doRespawn)
                        {
                            Utils.RespawnCharacter.Respawn(ev.Died, player, userEntity);
                        }
                    }
                    //-- ---------------------
                }
                //-- ----------------------------------------
            }
        }
    }
}
}
