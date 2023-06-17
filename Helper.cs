using ProjectM;
using ProjectM.Network;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PvPModes.Utils;
using PvPModes.Hooks;
using PvPModes.Systems;
using System.Text.RegularExpressions;
using ProjectM.Scripting;
using System.Collections.Generic;
using VampireCommandFramework;
using UnityEngine;
using Il2CppSystem.Net;
using System.IO;
using PvPModes.Data;
using static ProjectM.Roofs.RoofTestSceneBootstrapNew;


namespace PvPModes;

// This is an anti-pattern, move stuff away from Helper not into it
internal static partial class Helper
{
	private static Entity empty_entity = new Entity();
	private static System.Random rand = new System.Random();

	public static ServerGameSettings SGS = default;
	public static ServerGameManager SGM = default;
	public static UserActivityGridSystem UAGS = default;
	public static int groupRange = 50;

	public static Regex rxName = new Regex(@"(?<=\])[^\[].*");

	public static bool GetUserActivityGridSystem(out UserActivityGridSystem uags)
	{
		uags = Plugin.Server.GetExistingSystem<AiPrioritizationSystem>()?._UserActivityGridSystem;
		return true;
	}

	public static bool GetServerGameManager(out ServerGameManager sgm)
	{
		sgm = (ServerGameManager)Plugin.Server.GetExistingSystem<ServerScriptMapper>()?._ServerGameManager;
		return true;
	}

	public static bool GetServerGameSettings(out ServerGameSettings settings)
	{
		settings = Plugin.Server.GetExistingSystem<ServerGameSettingsSystem>()?._Settings;
		return true;
	}

	public static FixedString64 GetTrueName(string name)
	{
		MatchCollection match = rxName.Matches(name);
		if (match.Count > 0)
		{
			name = match[match.Count - 1].ToString();
		}
		return name;
	}

	public static void CreatePlayerP_Cache()
	{
		if (PvPSystem.isHonorSystemEnabled)
		{
			if (Database.PvPStats == null)
			{
				PvPSystem.LoadPvPStat();
			}
		}

		P_Cache.NamePlayerP_Cache.Clear();
		P_Cache.SteamPlayerP_Cache.Clear();
		EntityQuery query = Plugin.Server.EntityManager.CreateEntityQuery(new EntityQueryDesc()
		{
			All = new ComponentType[]
				{
						ComponentType.ReadOnly<User>()
				},
			Options = EntityQueryOptions.IncludeDisabled
		});
		var userEntities = query.ToEntityArray(Allocator.Temp);
		foreach (var entity in userEntities)
		{
			var userData = Plugin.Server.EntityManager.GetComponentData<User>(entity);
			PlayerData playerData = new PlayerData(userData.CharacterName, userData.PlatformId, userData.IsConnected, entity, userData.LocalCharacter._Entity);

			P_Cache.NamePlayerP_Cache.TryAdd(GetTrueName(userData.CharacterName.ToString().ToLower()), playerData);
			P_Cache.SteamPlayerP_Cache.TryAdd(userData.PlatformId, playerData);

			if (PvPSystem.isHonorSystemEnabled)
			{
				Database.PvPStats.TryGetValue(userData.PlatformId, out var pvpStats);
				Database.SiegeState.TryGetValue(userData.PlatformId, out var siegeData);

				bool isHostile = HasBuff(userData.LocalCharacter._Entity, PvPSystem.HostileBuff);
				if ((pvpStats.Reputation <= -1000 || siegeData.IsSiegeOn) && isHostile == false)
				{
					isHostile = true;
					if (PvPSystem.isEnableHostileGlow && !PvPSystem.isUseProximityGlow) ApplyBuff(entity, userData.LocalCharacter._Entity, PvPSystem.HostileBuff);
				}

				P_Cache.HostilityState[userData.LocalCharacter._Entity] = new StateData(userData.PlatformId, isHostile);

				if (siegeData.IsSiegeOn)
				{
					bool forceSiege = (siegeData.SiegeEndTime == DateTime.MinValue);
					PvPSystem.SiegeON(userData.PlatformId, userData.LocalCharacter._Entity, entity, forceSiege, false);
				}
			}
		}

		Plugin.Logger.LogWarning("Player P_Cache Created.");
	}

	public static void UpdatePlayerP_Cache(Entity userEntity, string oldName, string newName, bool forceOffline = false)
	{
		var userData = Plugin.Server.EntityManager.GetComponentData<User>(userEntity);
		P_Cache.NamePlayerP_Cache.Remove(GetTrueName(oldName.ToLower()));

		if (forceOffline) userData.IsConnected = false;
		PlayerData playerData = new PlayerData(newName, userData.PlatformId, userData.IsConnected, userEntity, userData.LocalCharacter._Entity);

		P_Cache.NamePlayerP_Cache[GetTrueName(newName.ToLower())] = playerData;
		P_Cache.SteamPlayerP_Cache[userData.PlatformId] = playerData;
	}

	public static bool RenamePlayer(Entity userEntity, Entity charEntity, FixedString64 newName)
	{
		//-- Max Char Length for FixedString64 is 61 bytes.
		//if (newName.utf8LengthInBytes > 61)
		//{
		//    return false;
		//}

		var userData = Plugin.Server.EntityManager.GetComponentData<User>(userEntity);
		if (PvPSystem.isHonorSystemEnabled && PvPSystem.isHonorTitleEnabled)
		{
			var vampire_name = GetTrueName(newName.ToString());

			if (Database.PvPStats.TryGetValue(userData.PlatformId, out var PvPData))
			{
				vampire_name = "[" + PvPSystem.GetHonorTitle(PvPData.Reputation).Title + "]" + vampire_name;
			}
			else
			{
				vampire_name = "[" + PvPSystem.GetHonorTitle(0).Title + "]" + vampire_name;
			}
			newName = vampire_name;
		}
		UpdatePlayerP_Cache(userEntity, userData.CharacterName.ToString(), newName.ToString());

		var des = Plugin.Server.GetExistingSystem<DebugEventsSystem>();
		var networkId = Plugin.Server.EntityManager.GetComponentData<NetworkId>(userEntity);
		var renameEvent = new RenameUserDebugEvent
		{
			NewName = newName,
			Target = networkId
		};
		var fromCharacter = new FromCharacter
		{
			User = userEntity,
			Character = charEntity
		};
		des.RenameUser(fromCharacter, renameEvent);
		return true;
	}

	public static bool ValidateName(string name, out CreateCharacterFailureReason invalidReason)
	{
		if (Regex.IsMatch(name, @"[^a-zA-Z0-9]"))
		{
			invalidReason = CreateCharacterFailureReason.InvalidName;
			return false;
		}

		//-- The game default max byte length is 20.
		//-- The max legth assignable is actually 61 bytes.
		FixedString64 charName = name;
		if (charName.utf8LengthInBytes > 20)
		{
			invalidReason = CreateCharacterFailureReason.InvalidName;
			return false;
		}

		if (P_Cache.NamePlayerP_Cache.TryGetValue(name.ToLower(), out _))
		{
			invalidReason = CreateCharacterFailureReason.NameTaken;
			return false;
		}

		invalidReason = CreateCharacterFailureReason.None;
		return true;
	}

	public static void ApplyBuff(Entity User, Entity Char, PrefabGUID GUID)
	{
		var des = Plugin.Server.GetExistingSystem<DebugEventsSystem>();
		var fromCharacter = new FromCharacter()
		{
			User = User,
			Character = Char
		};
		var buffEvent = new ApplyBuffDebugEvent()
		{
			BuffPrefabGUID = GUID
		}
		;
		Database.playerBuffs.Add(buffEvent);
		des.ApplyBuff(fromCharacter, buffEvent);

	}

	public static void RemoveBuff(Entity Char, PrefabGUID GUID)
	{
		if (BuffUtility.HasBuff(Plugin.Server.EntityManager, Char, GUID))
		{
			BuffUtility.TryGetBuff(Plugin.Server.EntityManager, Char, GUID, out var BuffEntity_);
			Plugin.Server.EntityManager.AddComponent<DestroyTag>(BuffEntity_);
			return;
		}
	}

	public static string GetNameFromSteamID(ulong SteamID)
	{
		//var UserEntities = Plugin.Server.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<User>()).ToEntityArray(Allocator.Temp);
		//foreach (var Entity in UserEntities)
		//{
		//    var EntityData = Plugin.Server.EntityManager.GetComponentData<User>(Entity);
		//    if (EntityData.PlatformId == SteamID) return EntityData.CharacterName.ToString();
		//}
		//return null;
		if (P_Cache.SteamPlayerP_Cache.TryGetValue(SteamID, out var data))
		{
			return data.CharacterName.ToString();
		}
		else
		{
			return null;
		}
	}

	public static PrefabGUID GetGUIDFromName(string name)
	{
		var gameDataSystem = Plugin.Server.GetExistingSystem<GameDataSystem>();
		var managed = gameDataSystem.ManagedDataRegistry;

		foreach (var entry in gameDataSystem.ItemHashLookupMap)
		{
			try
			{
				var item = managed.GetOrDefault<ManagedItemData>(entry.Key);
				if (item.PrefabName.StartsWith("Item_VBloodSource") || item.PrefabName.StartsWith("GM_Unit_Creature_Base") || item.PrefabName == "Item_Cloak_ShadowPriest") continue;
				if (item.Name.ToString().ToLower().Equals(name.ToLower()))
				{
					return entry.Key;
				}
				if (item.PrefabName.ToLower().Equals(name.ToLower())) { return entry.Key; }
				//if (item.PrefabName.Substring(item.PrefabName.IndexOf("_"+1)).ToLower().Equals(name.ToLower())) { return entry.Key; }
			}
			catch { }
		}

		return new PrefabGUID(0);
	}

	public static void KickPlayer(Entity userEntity)
	{
		EntityManager em = Plugin.Server.EntityManager;
		var userData = em.GetComponentData<User>(userEntity);
		int index = userData.Index;
		NetworkId id = em.GetComponentData<NetworkId>(userEntity);

		var entity = em.CreateEntity(
			ComponentType.ReadOnly<NetworkEventType>(),
			ComponentType.ReadOnly<SendEventToUser>(),
			ComponentType.ReadOnly<KickEvent>()
		);

		var KickEvent = new KickEvent()
		{
			PlatformId = userData.PlatformId
		};

		em.SetComponentData<SendEventToUser>(entity, new()
		{
			UserIndex = index
		});
		em.SetComponentData<NetworkEventType>(entity, new()
		{
			EventId = NetworkEvents.EventId_KickEvent,
			IsAdminEvent = false,
			IsDebugEvent = false
		});

		em.SetComponentData(entity, KickEvent);
	}

	public static void AddItemToInventory(ChatCommandContext ctx, PrefabGUID guid, int amount)
	{
		/*
		unsafe
		{
			var gameData = Plugin.Server.GetExistingSystem<GameDataSystem>();
			var bytes = stackalloc byte[Marshal.SizeOf<FakeNull>()];
			var bytePtr = new IntPtr(bytes);
			Marshal.StructureToPtr<FakeNull>(new()
			{
				value = 7,
				has_value = true
			}, bytePtr, false);
			var boxedBytePtr = IntPtr.Subtract(bytePtr, 0x10);
			var hack = new Il2CppSystem.Nullable<int>(boxedBytePtr);
			var sets = new AddItemSettings();
			sets.DropRemainder = true;
			sets.EquipIfPossible = true;
			var hasAdded = InventoryUtilitiesServer.TryAddItem(sets,ctx.Event.SenderCharacterEntity, guid ,amount);
		}*/

		var gameData = Plugin.Server.GetExistingSystem<GameDataSystem>();
		var itemSettings = AddItemSettings.Create(Plugin.Server.EntityManager, gameData.ItemHashLookupMap);
		var inventoryResponse = InventoryUtilitiesServer.TryAddItem(itemSettings, ctx.Event.SenderCharacterEntity, guid, amount);
		//return inventoryResponse.NewEntity;
	}

	public static BloodType GetBloodTypeFromName(string name)
	{
		BloodType type = BloodType.Frailed;
		if (Enum.IsDefined(typeof(BloodType), CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name)))
			Enum.TryParse(name, true, out type);
		return type;
	}

	public static PrefabGUID GetSourceTypeFromName(string name)
	{
		PrefabGUID type;
		name = name.ToLower();
		if (name.Equals("brute")) type = new PrefabGUID(-1464869978);
		else if (name.Equals("warrior")) type = new PrefabGUID(-1128238456);
		else if (name.Equals("rogue")) type = new PrefabGUID(-1030822544);
		else if (name.Equals("scholar")) type = new PrefabGUID(-700632469);
		else if (name.Equals("creature")) type = new PrefabGUID(1897056612);
		else if (name.Equals("worker")) type = new PrefabGUID(-1342764880);
		else if (name.Equals("mutant")) type = new PrefabGUID(-2017994753);
		else type = new PrefabGUID();
		return type;
	}

	public static bool FindPlayer(string name, bool mustOnline, out Entity playerEntity, out Entity userEntity)
	{
		EntityManager entityManager = Plugin.Server.EntityManager;

		//-- Way of the P_Cache
		if (P_Cache.NamePlayerP_Cache.TryGetValue(name.ToLower(), out var data))
		{
			playerEntity = data.CharEntity;
			userEntity = data.UserEntity;
			if (mustOnline)
			{
				var userComponent = entityManager.GetComponentData<User>(userEntity);
				if (!userComponent.IsConnected)
				{
					return false;
				}
			}
			return true;
		}
		else
		{
			playerEntity = empty_entity;
			userEntity = empty_entity;
			return false;
		}

		//-- Way of the Query
		//foreach (var UsersEntity in entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>()).ToEntityArray(Allocator.Temp))
		//{
		//    var target_component = entityManager.GetComponentData<User>(UsersEntity);
		//    if (mustOnline)
		//    {
		//        if (!target_component.IsConnected) continue;
		//    }


		//    string CharName = target_component.CharacterName.ToString();
		//    if (CharName.Equals(name))
		//    {
		//        userEntity = UsersEntity;
		//        playerEntity = target_component.LocalCharacter._Entity;
		//        return true;
		//    }
		//}
		//playerEntity = empty_entity;
		//userEntity = empty_entity;
		//return false;
	}
	public static bool FindPlayer(ulong steamid, bool mustOnline, out Entity playerEntity, out Entity userEntity)
	{
		EntityManager entityManager = Plugin.Server.EntityManager;

		//-- Way of the P_Cache
		if (P_Cache.SteamPlayerP_Cache.TryGetValue(steamid, out var data))
		{
			playerEntity = data.CharEntity;
			userEntity = data.UserEntity;
			if (mustOnline)
			{
				var userComponent = entityManager.GetComponentData<User>(userEntity);
				if (!userComponent.IsConnected)
				{
					return false;
				}
			}
			return true;
		}
		else
		{
			playerEntity = empty_entity;
			userEntity = empty_entity;
			return false;
		}
	}

	public static bool IsPlayerInCombat(Entity player)
	{
		return BuffUtility.HasBuff(Plugin.Server.EntityManager, player, Database.Buff.InCombat) || BuffUtility.HasBuff(Plugin.Server.EntityManager, player, Database.Buff.InCombat_PvP);
	}

	public static bool HasBuff(Entity player, PrefabGUID BuffGUID)
	{
		return BuffUtility.HasBuff(Plugin.Server.EntityManager, player, BuffGUID);
	}

	public static void SetPvPShield(Entity character, bool value)
	{
		var em = Plugin.Server.EntityManager;
		var cUnitStats = em.GetComponentData<UnitStats>(character);
		var cBuffer = em.GetBuffer<BoolModificationBuffer>(character);
		cUnitStats.PvPProtected.SetBaseValue(value, cBuffer);
		em.SetComponentData(character, cUnitStats);
	}

	public static bool SpawnNPCIdentify(out float identifier, string name, float3 position, float minRange = 1, float maxRange = 2, float duration = -1)
	{
		
		/// DEFAULT FALSE RETURN UNTIL FIX --THAZZEUS

		identifier = 0f;
		float default_duration = 5.0f;
		float duration_final;
		var isFound = Database.database_units.TryGetValue(name, out var unit);
		if (!isFound) return false;

		float UniqueID = (float)rand.NextDouble();
		if (UniqueID == 0.0) UniqueID += 0.00001f;
		else if (UniqueID == 1.0f) UniqueID -= 0.00001f;
		duration_final = default_duration + UniqueID;

		while (P_Cache.spawnNPC_Listen.ContainsKey(duration))
		{
			UniqueID = (float)rand.NextDouble();
			if (UniqueID == 0.0) UniqueID += 0.00001f;
			else if (UniqueID == 1.0f) UniqueID -= 0.00001f;
			duration_final = default_duration + UniqueID;
		}

		/// UnitSpawnerReactSystem_Patch.listen = true;  <---ISSUE IS HERE THAZZEUS
		identifier = duration_final;
		var Data = new SpawnNPCListen(duration, default, default, default, false);
		P_Cache.spawnNPC_Listen.Add(duration_final, Data);

		Plugin.Server.GetExistingSystem<UnitSpawnerUpdateSystem>().SpawnUnit(empty_entity, unit, position, 1, minRange, maxRange, duration_final);
		return false; /// <--- CHANGE TO TRUE WHEN FIXED --THAZZEUS
		
	}

	public static bool SpawnAtPosition(Entity user, string name, int count, float3 position, float minRange = 1, float maxRange = 2, float duration = -1)
	{
		var isFound = Database.database_units.TryGetValue(name, out var unit);
		if (!isFound) return false;

		//var translation = Plugin.Server.EntityManager.GetComponentData<Translation>(user);
		//var f3pos = new float3(position.x, translation.Value.y, position.y);
		Plugin.Server.GetExistingSystem<UnitSpawnerUpdateSystem>().SpawnUnit(empty_entity, unit, position, count, minRange, maxRange, duration);
		return true;
	}

	public static bool SpawnAtPosition(Entity user, int GUID, int count, float3 position, float minRange = 1, float maxRange = 2, float duration = -1)
	{
		var unit = new PrefabGUID(GUID);

		//var translation = Plugin.Server.EntityManager.GetComponentData<Translation>(user);
		//var f3pos = new float3(position.x, translation.Value.y, position.y);
		try
		{
			Plugin.Server.GetExistingSystem<UnitSpawnerUpdateSystem>().SpawnUnit(empty_entity, unit, position, count, minRange, maxRange, duration);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public static string GetPrefabName(PrefabGUID hashCode)
	{
		var s = Plugin.Server.GetExistingSystem<PrefabCollectionSystem>();
		string name = "Nonexistent";
		if (hashCode.GuidHash == 0)
		{
			return name;
		}
		try
		{
			name = s.PrefabLookupMap[hashCode].ToString();
		}
		catch
		{
			name = "NoPrefabName";
		}
		return name;
	}

	/*
	public static void TeleportTo(Context ctx, float3 position)
	{
		var entity = ctx.EntityManager.CreateEntity(
				ComponentType.ReadWrite<FromCharacter>(),
				ComponentType.ReadWrite<PlayerTeleportDebugEvent>()
			);

		ctx.EntityManager.SetComponentData<FromCharacter>(entity, new()
		{
			User = ctx.Event.SenderUserEntity,
			Character = ctx.Event.SenderCharacterEntity
		});

		ctx.EntityManager.SetComponentData<PlayerTeleportDebugEvent>(entity, new()
		{
			Position = new float3(position.x, position.y, position.z),
			Target = PlayerTeleportDebugEvent.TeleportTarget.Self
		});
	}*/
	/*
	struct FakeNull
	{
		public int value;
		public bool has_value;
	}*/

	public enum BloodType
	{
		Frailed = -899826404,
		Creature = -77658840,
		Warrior = -1094467405,
		Rogue = 793735874,
		Brute = 581377887,
		Scholar = -586506765,
		Worker = -540707191,
		Mutant = -2017994753
	}
	public enum BloodCategory
	{
		Frailed = 0,
		Creature = 1,
		Warrior = 2,
		Rogue = 3,
		Brute = 4,
		Scholar = 5,
		Worker = 6
	}


	// For stats that reduce as a multiplier of 1 - their value, so that a value of 0.5 halves the thing, and 0.75 quarters it.
	// I do this so that we can compute linear increases to a formula of X/(X+Y) where Y is the amount for +100% effectivness and X is the stat value
	public static HashSet<int> inverseMultiplierStats = new HashSet<int> {
			{(int)UnitStatType.CooldownModifier },
			{(int)UnitStatType.PhysicalResistance },
			{(int)UnitStatType.SpellResistance },
			{(int)UnitStatType.ResistVsBeasts },
			{(int)UnitStatType.ResistVsCastleObjects },
			{(int)UnitStatType.ResistVsDemons },
			{(int)UnitStatType.ResistVsHumans },
			{(int)UnitStatType.ResistVsMechanical },
			{(int)UnitStatType.ResistVsPlayerVampires },
			{(int)UnitStatType.ResistVsUndeads },
			{(int)UnitStatType.ReducedResourceDurabilityLoss }
		};

	//This should be a dictionary lookup for the stats to what mod type they should use, and i should put the name strings in here, i might do it later.
	public static HashSet<int> multiplierStats = new HashSet<int> {
			{(int)UnitStatType.CooldownModifier },
			{(int)UnitStatType.PhysicalResistance },
			{(int)UnitStatType.SpellResistance },
			{(int)UnitStatType.ResistVsBeasts },
			{(int)UnitStatType.ResistVsCastleObjects },
			{(int)UnitStatType.ResistVsDemons },
			{(int)UnitStatType.ResistVsHumans },
			{(int)UnitStatType.ResistVsMechanical },
			{(int)UnitStatType.ResistVsPlayerVampires },
			{(int)UnitStatType.ResistVsUndeads },
			{(int)UnitStatType.ReducedResourceDurabilityLoss },
			{(int)UnitStatType.ResourceYield }

		};

	public static HashSet<int> baseStatsSet = new HashSet<int> {
			{(int)UnitStatType.PhysicalPower },
			{(int)UnitStatType.ResourcePower },
			{(int)UnitStatType.SiegePower },
			{(int)UnitStatType.AttackSpeed },
			{(int)UnitStatType.FireResistance },
			{(int)UnitStatType.GarlicResistance },
			{(int)UnitStatType.SilverResistance },
			{(int)UnitStatType.HolyResistance },
			{(int)UnitStatType.SunResistance },
			{(int)UnitStatType.SpellResistance },
			{(int)UnitStatType.PhysicalResistance },
			{(int)UnitStatType.SpellCriticalStrikeDamage },
			{(int)UnitStatType.SpellCriticalStrikeChance },
			{(int)UnitStatType.PhysicalCriticalStrikeDamage },
			{(int)UnitStatType.PhysicalCriticalStrikeChance },
			{(int)UnitStatType.PassiveHealthRegen },
			{(int)UnitStatType.ResourceYield },
			{(int)UnitStatType.PvPResilience },
			{(int)UnitStatType.ReducedResourceDurabilityLoss }

		};

	public static void confirmFile(string address)
	{
		if (!File.Exists(address))
		{
			FileStream stream = File.Create(address);
			stream.Dispose();
		}
	}
	public static String statTypeToString(UnitStatType type)
	{
		String name = "Unknown";
		if (type == UnitStatType.PhysicalPower)
			name = "Physical Power";
		if (type == UnitStatType.ResourcePower)
			name = "Resource Power";
		if (type == UnitStatType.SiegePower)
			name = "Siege Power";
		if (type == UnitStatType.ResourceYield)
			name = "Resource Yield";
		if (type == UnitStatType.MaxHealth)
			name = "Max Health";
		if (type == UnitStatType.MovementSpeed)
			name = "MovementSpeed";
		if (type == UnitStatType.CooldownModifier)
			name = "CooldownModifier";
		if (type == UnitStatType.PhysicalResistance)
			name = "PhysicalResistance";
		if (type == UnitStatType.FireResistance)
			name = "FireResistance";
		if (type == UnitStatType.HolyResistance)
			name = "HolyResistance";
		if (type == UnitStatType.SilverResistance)
			name = "SilverResistance";
		if (type == UnitStatType.SunChargeTime)
			name = "SunChargeTime";
		if (type == UnitStatType.EnergyGain)
			name = "EnergyGain";
		if (type == UnitStatType.MaxEnergy)
			name = "MaxEnergy";
		if (type == UnitStatType.SunResistance)
			name = "SunResistance";
		if (type == UnitStatType.GarlicResistance)
			name = "GarlicResistance";
		if (type == UnitStatType.Vision)
			name = "Vision";
		if (type == UnitStatType.SpellResistance)
			name = "SpellResistance";
		if (type == UnitStatType.Radial_SpellResistance)
			name = "Radial_SpellResistance";
		if (type == UnitStatType.SpellPower)
			name = "SpellPower";
		if (type == UnitStatType.PassiveHealthRegen)
			name = "PassiveHealthRegen";
		if (type == UnitStatType.PhysicalLifeLeech)
			name = "PhysicalLifeLeech";
		if (type == UnitStatType.SpellLifeLeech)
			name = "SpellLifeLeech";
		if (type == UnitStatType.PhysicalCriticalStrikeChance)
			name = "PhysicalCriticalStrikeChance";
		if (type == UnitStatType.PhysicalCriticalStrikeDamage)
			name = "PhysicalCriticalStrikeDamage";
		if (type == UnitStatType.SpellCriticalStrikeChance)
			name = "SpellCriticalStrikeChance";
		if (type == UnitStatType.SpellCriticalStrikeDamage)
			name = "SpellCriticalStrikeDamage";
		if (type == UnitStatType.AttackSpeed)
			name = "AttackSpeed";
		if (type == UnitStatType.DamageVsUndeads)
			name = "DamageVsUndead";
		if (type == UnitStatType.DamageVsHumans)
			name = "DamageVsHumans";
		if (type == UnitStatType.DamageVsDemons)
			name = "DamageVsDemons";
		if (type == UnitStatType.DamageVsMechanical)
			name = "DamageVsMechanical";
		if (type == UnitStatType.DamageVsBeasts)
			name = "DamageVsBeasts";
		if (type == UnitStatType.DamageVsCastleObjects)
			name = "DamageVsCastleObjects";
		if (type == UnitStatType.DamageVsPlayerVampires)
			name = "DamageVsPlayerVampires";
		if (type == UnitStatType.ResistVsUndeads)
			name = "ResistVsUndead";
		if (type == UnitStatType.ResistVsHumans)
			name = "ResistVsHumans";
		if (type == UnitStatType.ResistVsDemons)
			name = "ResistVsDemons";
		if (type == UnitStatType.ResistVsMechanical)
			name = "ResistVsMechanical";
		if (type == UnitStatType.ResistVsBeasts)
			name = "ResistVsBeasts";
		if (type == UnitStatType.ResistVsCastleObjects)
			name = "ResistVsCastleObjects";
		if (type == UnitStatType.ResistVsPlayerVampires)
			name = "ResistVsPlayerVampires";
		if (type == UnitStatType.DamageVsWood)
			name = "DamageVsWood";
		if (type == UnitStatType.DamageVsMineral)
			name = "DamageVsMineral";
		if (type == UnitStatType.DamageVsVegetation)
			name = "DamageVsVegetation";
		if (type == UnitStatType.DamageVsLightArmor)
			name = "DamageVsLightArmor";
		if (type == UnitStatType.DamageVsHeavyArmor)
			name = "DamageVsHeavyArmor";
		if (type == UnitStatType.DamageVsMagic)
			name = "DamageVsMagic";
		if (type == UnitStatType.ReducedResourceDurabilityLoss)
			name = "ReducedResourceDurabilityLoss";
		if (type == UnitStatType.PrimaryAttackSpeed)
			name = "PrimaryAttackSpeed";
		if (type == UnitStatType.ImmuneToHazards)
			name = "ImmuneToHazards";
		if (type == UnitStatType.PrimaryLifeLeech)
			name = "PrimaryLifeLeech";
		if (type == UnitStatType.HealthRecovery)
			name = "HealthRecovery";
		return name;
	}


	public static PrefabGUID GetPrefabGUID(Entity entity)
	{
		var entityManager = Core.EntityManager;
		PrefabGUID guid;
		try
		{
			guid = entityManager.GetComponentData<PrefabGUID>(entity);
		}
		catch
		{
			guid.GuidHash = 0;
		}
		return guid;
	}


	public static Entity AddItemToInventory(Entity recipient, PrefabGUID guid, int amount)
	{
		try
		{
			var gameData = Core.Server.GetExistingSystem<GameDataSystem>();
			var itemSettings = AddItemSettings.Create(Core.EntityManager, gameData.ItemHashLookupMap);
			var inventoryResponse = InventoryUtilitiesServer.TryAddItem(itemSettings, recipient, guid, amount);

			return inventoryResponse.NewEntity;
		}
		catch (Exception e)
		{
			Core.LogException(e);
		}
		return new Entity();
	}
}
