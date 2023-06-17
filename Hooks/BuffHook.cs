using HarmonyLib;
using Unity.Entities;
using Unity.Collections;
using ProjectM.Network;
using ProjectM;
using PvPModes.Utils;
using PvPModes.Systems;
using System.Collections.Generic;
using ProjectM.Scripting;

namespace PvPModes.Hooks
{
	[HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
	public class ModifyUnitStatBuffSystem_Spawn_Patch
	{
		#region GodMode & Other Buff
		private static ModifyUnitStatBuff_DOTS Cooldown = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.CooldownModifier,
			Value = 0,
			ModificationType = ModificationType.Set,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS SunCharge = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.SunChargeTime,
			Value = 50000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS Hazard = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.ImmuneToHazards,
			Value = 1,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS SunResist = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.SunResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS Speed = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.MovementSpeed,
			Value = 15,
			ModificationType = ModificationType.Set,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS PResist = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.PhysicalResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS FResist = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.FireResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS HResist = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.HolyResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS SResist = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.SilverResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS GResist = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.GarlicResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS SPResist = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.SpellResistance,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS PPower = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.PhysicalPower,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS RPower = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.ResourcePower,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS SPPower = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.SpellPower,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS PHRegen = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.PassiveHealthRegen,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS HRecovery = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.HealthRecovery,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS MaxHP = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.MaxHealth,
			Value = 10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS MaxYield = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.ResourceYield,
			Value = 10,
			ModificationType = ModificationType.Multiply,
			Id = ModificationId.NewId(0)
		};

		private static ModifyUnitStatBuff_DOTS DurabilityLoss = new ModifyUnitStatBuff_DOTS()
		{
			StatType = UnitStatType.ReducedResourceDurabilityLoss,
			Value = -10000,
			ModificationType = ModificationType.Add,
			Id = ModificationId.NewId(0)
		};
		#endregion

		public static bool buffLogging = false;

		private static void Prefix(ModifyUnitStatBuffSystem_Spawn __instance)
		{
			if (buffLogging) Plugin.Logger.LogInfo(System.DateTime.Now + ": Entered Buff System, attempting Old Style");
			oldStyleBuffHook(__instance);
			//if (buffLogging) Plugin.Logger.LogInfo(System.DateTime.Now + ": Old Style Done, attemping New Style, just cause");
			//rebuiltBuffHook(__instance);
		}

		public static void oldStyleBuffApplicaiton(Entity entity, EntityManager entityManager)
		{

			if (buffLogging) Plugin.Logger.LogInfo(System.DateTime.Now + ": Applying PvPModes Buffs");
			Entity Owner = entityManager.GetComponentData<EntityOwner>(entity).Owner;
			if (buffLogging) Plugin.Logger.LogInfo(System.DateTime.Now + ": Owner found, hash: " + Owner.GetHashCode());
			if (!entityManager.HasComponent<PlayerCharacter>(Owner)) return;

			PlayerCharacter playerCharacter = entityManager.GetComponentData<PlayerCharacter>(Owner);
			Entity User = playerCharacter.UserEntity/*._Entity*/;
			User Data = entityManager.GetComponentData<User>(User);

			var Buffer = entityManager.GetBuffer<ModifyUnitStatBuff_DOTS>(entity);
			if (buffLogging) Plugin.Logger.LogInfo(System.DateTime.Now + ": Buffer acquired, length: " + Buffer.Length);

			//Buffer.Clear();
			if (buffLogging) Plugin.Logger.LogInfo(System.DateTime.Now + ": Buffer cleared, to confirm length: " + Buffer.Length);
			if (buffLogging) Plugin.Logger.LogInfo(System.DateTime.Now + ": Now doing PowerUp Command");
			if (Database.PowerUpList.TryGetValue(Data.PlatformId, out var powerUpData))
			{
				if (powerUpData.Equals(null))
				{
					powerUpData = new PowerUpData();
				}
				if (powerUpData.MaxHP.Equals(null))
				{
					powerUpData.MaxHP = 0;
				}
				if (powerUpData.PATK.Equals(null))
				{
					powerUpData.PATK = 0;
				}
				if (powerUpData.SATK.Equals(null))
				{
					powerUpData.SATK = 0;
				}
				if (powerUpData.PDEF.Equals(null))
				{
					powerUpData.PDEF = 0;
				}
				if (powerUpData.SDEF.Equals(null))
				{
					powerUpData.SDEF = 0;
				}
				Buffer.Add(new ModifyUnitStatBuff_DOTS()
				{
					StatType = UnitStatType.MaxHealth,
					Value = powerUpData.MaxHP,
					ModificationType = ModificationType.Add,
					Id = ModificationId.NewId(0)
				});

				Buffer.Add(new ModifyUnitStatBuff_DOTS()
				{
					StatType = UnitStatType.PhysicalPower,
					Value = powerUpData.PATK,
					ModificationType = ModificationType.Add,
					Id = ModificationId.NewId(0)
				});

				Buffer.Add(new ModifyUnitStatBuff_DOTS()
				{
					StatType = UnitStatType.SpellPower,
					Value = powerUpData.SATK,
					ModificationType = ModificationType.Add,
					Id = ModificationId.NewId(0)
				});

				Buffer.Add(new ModifyUnitStatBuff_DOTS()
				{
					StatType = UnitStatType.PhysicalResistance,
					Value = powerUpData.PDEF,
					ModificationType = ModificationType.Add,
					Id = ModificationId.NewId(0)
				});

				Buffer.Add(new ModifyUnitStatBuff_DOTS()
				{
					StatType = UnitStatType.SpellResistance,
					Value = powerUpData.SDEF,
					ModificationType = ModificationType.Add,
					Id = ModificationId.NewId(0)
				});
			}



			if (buffLogging) Plugin.Logger.LogInfo(System.DateTime.Now + ": Now doing NoCD Command");
			if (Database.nocooldownlist.ContainsKey(Data.PlatformId))
			{
				Buffer.Add(Cooldown);
			}
			/*
            if (buffLogging) Plugin.Logger.LogInfo(System.DateTime.Now + ": Now doing Sun Immunity Command");
            if (Database.sunimmunity.ContainsKey(Data.PlatformId))
            {
                Buffer.Add(SunCharge);
                Buffer.Add(Hazard);
                Buffer.Add(SunResist);
            }

            if (buffLogging) Plugin.Logger.LogInfo(System.DateTime.Now + ": Now doing Speeding Command");
            if (Database.speeding.ContainsKey(Data.PlatformId))
            {
                Buffer.Add(Speed);
            }

            if (buffLogging) Plugin.Logger.LogInfo(System.DateTime.Now + ": Now doing GodMode Command");
            if (Database.godmode.ContainsKey(Data.PlatformId))
            {
                Buffer.Add(PResist);
                Buffer.Add(FResist);
                Buffer.Add(HResist);
                Buffer.Add(SResist);
                Buffer.Add(SunResist);
                Buffer.Add(GResist);
                Buffer.Add(SPResist);
                Buffer.Add(PPower);
                Buffer.Add(RPower);
                Buffer.Add(SPPower);
                Buffer.Add(MaxYield);
                Buffer.Add(MaxHP);
                Buffer.Add(Hazard);
                Buffer.Add(SunCharge);
                Buffer.Add(DurabilityLoss);
            }
            */
			if (buffLogging) Plugin.Logger.LogInfo(System.DateTime.Now + ": Done Adding, Buffer length: " + Buffer.Length);

		}

		public static void oldStyleBuffHook(ModifyUnitStatBuffSystem_Spawn __instance)
		{
			//if (__instance.__OnUpdate_LambdaJob0_entityQuery == null) return;

			EntityManager entityManager = __instance.EntityManager;
			NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);

			if (buffLogging) Plugin.Logger.LogInfo(System.DateTime.Now + ": Entities Length of " + entities.Length);
			foreach (var entity in entities)
			{
				PrefabGUID GUID = entityManager.GetComponentData<PrefabGUID>(entity);
				if (buffLogging) Plugin.Logger.LogInfo(System.DateTime.Now + ": GUID of " + GUID.GuidHash + ": Compared to moose blood of " + Database.Buff.Buff_VBlood_Perk_Moose.GuidHash);
				if (GUID.Equals(Database.Buff.Buff_VBlood_Perk_Moose) || GUID.GuidHash == -1465458722)
				{
					oldStyleBuffApplicaiton(entity, entityManager);
				}
			}
		}
		public static void rebuiltBuffHook(ModifyUnitStatBuffSystem_Spawn __instance)
		{
			EntityManager em = __instance.EntityManager;
			bool hasSGM = Helper.GetServerGameManager(out ServerGameManager sgm);
			if (!hasSGM)
			{
				Plugin.Logger.LogInfo("No Server Game Manager, Something is WRONG.");
				return;

			}

			EntityQuery query = Plugin.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc()
			{
				All = new ComponentType[]
						{
							ComponentType.ReadOnly<PlayerCharacter>(),
							ComponentType.ReadOnly<IsConnected>()
						},
				Options = EntityQueryOptions.IncludeDisabled
			});
			NativeArray<Entity> pcArray = query.ToEntityArray(Allocator.Temp);
			if (buffLogging) Plugin.Logger.LogInfo("got connected Players, array of length " + pcArray.Length);
			foreach (var entity in pcArray)
			{
				em.TryGetComponentData<PlayerCharacter>(entity, out PlayerCharacter pc);
				em.TryGetComponentData<User>(entity, out User userEntity);
				ulong SteamID = userEntity.PlatformId;
				bool hasBuffs = P_Cache.buffData.TryGetValue(SteamID, out List<BuffData> bdl);
				if (!hasBuffs) { continue; }

				var Buffer = em.GetBuffer<ModifyUnitStatBuff_DOTS>(entity);
				//em.TryGetComponentData<BuffBuffer>(entity, out BuffBuffer buffer2);

				em.TryGetBuffer<ModifyUnitStats>(entity, out var stats);

				if (buffLogging) Plugin.Logger.LogInfo("got entities modifyunitystatbuffDOTS buffer of length " + Buffer.Length);
				if (buffLogging) Plugin.Logger.LogInfo("got entities modifyunitystatbuff buffer of length " + stats.Length);

				foreach (BuffData bd in bdl)
				{
					if (bd.isApplied) { continue; }
					ModifyUnitStatBuff_DOTS buff = new ModifyUnitStatBuff_DOTS
					{
						StatType = (UnitStatType)bd.targetStat,
						Value = (float)bd.value,
						ModificationType = (ModificationType)bd.modificationType,
						Id = ModificationId.NewId(bd.ID)
					};
					applyBuff(em, buff, sgm, entity);
					//baseStats.PhysicalPower.ApplyModification(sgm, entity, entity, buff.ModificationType, buff.Value);
				}


			}
		}

		public static bool applyBuff(EntityManager em, ModifyUnitStatBuff_DOTS buff, ServerGameManager sgm, Entity e)
		{
			ModifiableFloat stat = new ModifiableFloat();
			ModifiableInt statInt = new ModifiableInt();
			bool targetIsInt = false;
			bool applied = false;
			UnitStatType tar = buff.StatType;

			if (Helper.baseStatsSet.Contains((int)tar))
			{
				em.TryGetComponentData<UnitStats>(e, out var baseStats);
				if (tar == UnitStatType.PhysicalPower)
				{
					stat = baseStats.PhysicalPower;
					//baseStats.PhysicalPower.ApplyModification(sgm, e, e, buff.ModificationType, buff.Value);
				}
				else if (tar == UnitStatType.ResourcePower)
				{
					stat = baseStats.ResourcePower;
				}
				else if (tar == UnitStatType.SiegePower)
				{
					stat = baseStats.SiegePower;
					//baseStats.SiegePower.ApplyModification(sgm, e, e, buff.ModificationType, buff.Value);
				}
				else if (tar == UnitStatType.AttackSpeed || tar == UnitStatType.PrimaryAttackSpeed)
				{
					stat = baseStats.AttackSpeed;
				}
				else if (tar == UnitStatType.FireResistance)
				{
					statInt = baseStats.FireResistance; targetIsInt = true;
				}
				else if (tar == UnitStatType.GarlicResistance)
				{
					statInt = baseStats.GarlicResistance; targetIsInt = true;
				}
				else if (tar == UnitStatType.SilverResistance)
				{
					statInt = baseStats.SilverResistance; targetIsInt = true;
				}
				else if (tar == UnitStatType.HolyResistance)
				{
					statInt = baseStats.HolyResistance; targetIsInt = true;
				}
				else if (tar == UnitStatType.SunResistance)
				{
					statInt = baseStats.SunResistance; targetIsInt = true;
				}
				else if (tar == UnitStatType.SpellResistance)
				{
					stat = baseStats.SpellResistance;
				}
				else if (tar == UnitStatType.PhysicalResistance)
				{
					stat = baseStats.PhysicalResistance;
				}
				else if (tar == UnitStatType.PhysicalCriticalStrikeChance)
				{
					stat = baseStats.PhysicalCriticalStrikeChance;
				}
				else if (tar == UnitStatType.PhysicalCriticalStrikeDamage)
				{
					stat = baseStats.PhysicalCriticalStrikeDamage;
				}
				else if (tar == UnitStatType.SpellCriticalStrikeChance)
				{
					stat = baseStats.SpellCriticalStrikeChance;
				}
				else if (tar == UnitStatType.SpellCriticalStrikeDamage)
				{
					stat = baseStats.SpellCriticalStrikeDamage;
				}
				else if (tar == UnitStatType.PassiveHealthRegen)
				{
					stat = baseStats.PassiveHealthRegen;
				}
				else if (tar == UnitStatType.PvPResilience)
				{
					statInt = baseStats.PvPResilience; targetIsInt = true;
				}
				else if (tar == UnitStatType.ResourceYield)
				{
					stat = baseStats.ResourceYieldModifier;
				}
				else if (tar == UnitStatType.PvPResilience)
				{
					statInt = baseStats.PvPResilience; targetIsInt = true;
				}
				else if (tar == UnitStatType.ReducedResourceDurabilityLoss)
				{
					stat = baseStats.ReducedResourceDurabilityLoss;
				}
			}
			else if (tar == UnitStatType.MaxHealth)
			{
				em.TryGetComponentData<Health>(e, out Health health);
				health.MaxHealth.ApplyModification(sgm, e, e, buff.ModificationType, buff.Value);
			}
			else if (tar == UnitStatType.MovementSpeed)
			{
				em.TryGetComponentData<Movement>(e, out Movement speed);
				speed.Speed.ApplyModification(sgm, e, e, buff.ModificationType, buff.Value);
			}
			try
			{
				if (targetIsInt)
				{
					statInt.ApplyModification(sgm, e, e, buff.ModificationType, (int)buff.Value);
				}
				else
				{
					stat.ApplyModification(sgm, e, e, buff.ModificationType, buff.Value);
				}
				applied = true;
			}
			catch
			{
				if (buffLogging) Plugin.Logger.LogInfo("Failed to apply buff to statID: " + tar);
			}
			return applied;
		}
	}

	[HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
	public class BuffSystem_Spawn_Server_Patch
	{
		private static void Prefix(BuffSystem_Spawn_Server __instance)
		{

			if (PvPSystem.isPunishEnabled || SiegeSystem.isSiegeBuff || PermissionSystem.isVIPSystem || PvPSystem.isHonorSystemEnabled)
			{
				NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
				foreach (var entity in entities)
				{
					PrefabGUID GUID = __instance.EntityManager.GetComponentData<PrefabGUID>(entity);
					//if (WeaponMasterSystem.isMasteryEnabled) WeaponMasterSystem.BuffReceiver(entities[i], GUID);
					if (PvPSystem.isHonorSystemEnabled) PvPSystem.HonorBuffReceiver(entity, GUID);
					if (PermissionSystem.isVIPSystem) PermissionSystem.BuffReceiver(entity, GUID);
					if (PvPSystem.isPunishEnabled) PvPSystem.BuffReceiver(entity, GUID);
					if (SiegeSystem.isSiegeBuff) SiegeSystem.BuffReceiver(entity, GUID);
				}
			}
		}

		[HarmonyPatch(typeof(ModifyBloodDrainSystem_Spawn), nameof(ModifyBloodDrainSystem_Spawn.OnUpdate))]
		public class ModifyBloodDrainSystem_Spawn_Patch
		{
			private static void Prefix(ModifyBloodDrainSystem_Spawn __instance)
			{

				if (PermissionSystem.isVIPSystem || PvPSystem.isHonorSystemEnabled)
				{
					NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
					foreach (var entity in entities)
					{
						PrefabGUID GUID = __instance.EntityManager.GetComponentData<PrefabGUID>(entity);
						//if (WeaponMasterSystem.isMasteryEnabled) WeaponMasterSystem.BuffReceiver(entities[i], GUID);
						if (PermissionSystem.isVIPSystem) PermissionSystem.BuffReceiver(entity, GUID);
						if (PvPSystem.isHonorSystemEnabled) PvPSystem.HonorBuffReceiver(entity, GUID);
					}
				}
			}
		}

		[HarmonyPatch(typeof(Destroy_TravelBuffSystem), nameof(Destroy_TravelBuffSystem.OnUpdate))]
		public class Destroy_TravelBuffSystem_Patch
		{
			private static void Postfix(Destroy_TravelBuffSystem __instance)
			{
				var entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
				foreach (var entity in entities)
				{
					PrefabGUID GUID = __instance.EntityManager.GetComponentData<PrefabGUID>(entity);
					//-- Most likely it's a new player!
					if (GUID.Equals(Database.Buff.AB_Interact_TombCoffinSpawn_Travel))
					{
						var Owner = __instance.EntityManager.GetComponentData<EntityOwner>(entity).Owner;
						if (!__instance.EntityManager.HasComponent<PlayerCharacter>(Owner)) return;

						var userEntity = __instance.EntityManager.GetComponentData<PlayerCharacter>(Owner).UserEntity;
						var playerName = __instance.EntityManager.GetComponentData<User>(userEntity).CharacterName.ToString();

						if (PvPSystem.isHonorSystemEnabled) PvPSystem.NewPlayerReceiver(userEntity, Owner, playerName);
						else Helper.UpdatePlayerP_Cache(userEntity, playerName, playerName);
					}
				}
			}
		}
	}
}
