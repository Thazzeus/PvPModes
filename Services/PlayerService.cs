using System.Collections.Generic;
using System.Linq;
using PvPModes.Models;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace PvPModes.Services;

internal class PlayerService
{
	Dictionary<FixedString64, PlayerData> NamePlayerP_Cache = new();
	Dictionary<ulong, PlayerData> SteamPlayerP_Cache = new();

	internal bool TryFindSteam(ulong steamId, out PlayerData playerData)
	{
		return SteamPlayerP_Cache.TryGetValue(steamId, out playerData);
	}

	internal bool TryFindName(FixedString64 name, out PlayerData playerData)
	{
		return NamePlayerP_Cache.TryGetValue(name, out playerData);
	}

	internal PlayerService()
	{
		NamePlayerP_Cache.Clear();
		SteamPlayerP_Cache.Clear();
		EntityQuery query = Core.EntityManager.CreateEntityQuery(new EntityQueryDesc()
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
			var userData = Core.EntityManager.GetComponentData<User>(entity);
			PlayerData playerData = new PlayerData(userData.CharacterName, userData.PlatformId, userData.IsConnected, entity, userData.LocalCharacter._Entity);

			NamePlayerP_Cache.TryAdd(userData.CharacterName.ToString().ToLower(), playerData);
			SteamPlayerP_Cache.TryAdd(userData.PlatformId, playerData);
		}


		var onlinePlayers = NamePlayerP_Cache.Values.Where(p => p.IsOnline).Select(p => $"\t{p.CharacterName}");
		Core.Log.LogWarning($"Player P_Cache Created with {NamePlayerP_Cache.Count} entries total, listing {onlinePlayers.Count()} online:");
		Core.Log.LogWarning(string.Join("\n", onlinePlayers));
	}

	internal void UpdatePlayerP_Cache(Entity userEntity, string oldName, string newName, bool forceOffline = false)
	{
		var userData = Core.EntityManager.GetComponentData<User>(userEntity);
		NamePlayerP_Cache.Remove(oldName.ToLower());

		if (forceOffline) userData.IsConnected = false;
		PlayerData playerData = new PlayerData(newName, userData.PlatformId, userData.IsConnected, userEntity, userData.LocalCharacter._Entity);

		NamePlayerP_Cache[newName.ToLower()] = playerData;
		SteamPlayerP_Cache[userData.PlatformId] = playerData;
	}

	internal bool RenamePlayer(Entity userEntity, Entity charEntity, FixedString64 newName)
	{
		var des = Core.Server.GetExistingSystem<DebugEventsSystem>();
		var networkId = Core.EntityManager.GetComponentData<NetworkId>(userEntity);
		var userData = Core.EntityManager.GetComponentData<User>(userEntity);
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
		UpdatePlayerP_Cache(userEntity, userData.CharacterName.ToString(), newName.ToString());
		return true;
	}
}
