using ProjectM;
using ProjectM.Network;
using PvPModes.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using P_Cache = PvPModes.Utils.P_Cache;

namespace PvPModes.Systems
{
    public static class ProximityLoop
    {
        public static float maxDistance = 15.0f;

        private static EntityManager em = Plugin.Server.EntityManager;
        private static HashSet<Entity> SkipList = new();
        private static Dictionary<Entity, ulong> HostileList = new();
        private static HashSet<Entity> HostileOutRange = new();

        private static bool LoopInProgress = false;

        private static EntityQuery query = Plugin.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc()
        {
            All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<PlayerCharacter>(),
                        ComponentType.ReadOnly<IsConnected>()
                    },
            Options = EntityQueryOptions.IncludeDisabled
        });

        public static void UpdateP_Cache()
        {
            P_Cache.PlayerLocations.Clear();
            var EntityArray = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in EntityArray)
            {
                P_Cache.PlayerLocations[entity] = em.GetComponentData<LocalToWorld>(entity);
            }
        }

        public static void HostileProximityGlow()
        {
            if (LoopInProgress) return;
            LoopInProgress = true;

            SkipList.Clear();
            HostileList.Clear();
            HostileOutRange.Clear();

            foreach (var entity in P_Cache.HostilityState)
            {
                if (!entity.Value.IsHostile) continue;
                if (SkipList.Contains(entity.Key)) continue;

                P_Cache.SteamPlayerP_Cache.TryGetValue(entity.Value.SteamID, out var playerData);
                if (playerData.IsOnline == false)
                {
                    SkipList.Add(entity.Key);
                    HostileOutRange.Add(entity.Key);
                    continue;
                }

                if (ClosePlayers(entity.Key, out var TBSkip))
                {
                    SkipList.Add(entity.Key);
                    HostileList[entity.Key] = entity.Value.SteamID;

                    foreach (var close_entity in TBSkip)
                    {
                        SkipList.Add(close_entity);
                        if (P_Cache.HostilityState[close_entity].IsHostile) HostileList[close_entity] = P_Cache.HostilityState[close_entity].SteamID;
                    }
                }
                else
                {
                    SkipList.Add(entity.Key);
                    HostileOutRange.Add(entity.Key);
                }
            }

            foreach (var entity in HostileList)
            {
                bool hasHostileBuff = Helper.HasBuff(entity.Key, PvPSystem.HostileBuff);
                bool isRatForm = Helper.HasBuff(entity.Key, Database.Buff.RatForm);
                if (hasHostileBuff)
                {
                    if (isRatForm) Helper.RemoveBuff(entity.Key, PvPSystem.HostileBuff);
                }
                else
                {
                    if (!isRatForm) Helper.ApplyBuff(P_Cache.SteamPlayerP_Cache[entity.Value].UserEntity, entity.Key, PvPSystem.HostileBuff);
                }
            }

            foreach(var entity in HostileOutRange)
            {
                Helper.RemoveBuff(entity, PvPSystem.HostileBuff);
            }

            LoopInProgress = false;
        }

        private static bool ClosePlayers(Entity characterEntity, out List<Entity> ClosePlayers)
        {
            ClosePlayers = new();

            if (P_Cache.PlayerLocations.TryGetValue(characterEntity, out var charPosition))
            {
                foreach (var item in P_Cache.HostilityState)
                {
                    if (item.Key.Equals(characterEntity)) continue;
                    if (SkipList.Contains(item.Key)) continue;

                    P_Cache.SteamPlayerP_Cache.TryGetValue(item.Value.SteamID, out var playerData);
                    if (playerData.IsOnline == false)
                    {
                        SkipList.Add(item.Key);
                        continue;
                    }

                    if (P_Cache.PlayerLocations.TryGetValue(item.Key, out var targetPosition))
                    {
                        var distance = math.distance(charPosition.Position.xz, targetPosition.Position.xz);

                        if (distance < maxDistance)
                        {
                            ClosePlayers.Add(item.Key);
                        }
                    }
                    else
                    {
                        SkipList.Add(item.Key);
                        continue;
                    }
                }
                if (ClosePlayers.Count > 0) return true;
            }
            else
            {
                SkipList.Add(characterEntity);
                return false;
            }
            
            return false;
        }
    }
}
