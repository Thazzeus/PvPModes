using ProjectM;
using ProjectM.Network;
using PvPModes.Systems;
using PvPModes.Utils;
using System;
using Unity.Transforms;
using VampireCommandFramework;

namespace PvPModes.Commands
{ 
		public static class PlayerInfo
    {
		[Command("playerinfo", "pi", description: "Display the player information details.")]
		public static void getPlayerInfo(Context ctx)
        {
            if (ctx.Args.Length < 1) 
            {
                Output.MissingArguments(ctx);
                return;
            }

            if (!Helper.FindPlayer(ctx.Args[0], false, out var playerEntity, out var userEntity))
            {
				Output.SendSystemMessage(ctx, "Player not found."); 
                return;
            }

            var userData = ctx.EntityManager.GetComponentData<User>(userEntity);

            ulong SteamID = userData.PlatformId;
            string Name = userData.CharacterName.ToString();
            string CharacterEntity = playerEntity.Index.ToString() + ":" + playerEntity.Version.ToString();
            string UserEntity = userEntity.Index.ToString() + ":" + userEntity.Version.ToString();
            var ping = (int) ctx.EntityManager.GetComponentData<Latency>(playerEntity).Value;
            var position = ctx.EntityManager.GetComponentData<Translation>(playerEntity).Value;

            Database.PvPStats.TryGetValue(SteamID, out var pvpStats);
            Database.player_experience.TryGetValue(SteamID, out var exp);

            Output.SendSystemMessage(ctx, $"Name: {P_Color.White(Name)}");
            Output.SendSystemMessage(ctx, $"SteamID: {P_Color.White(SteamID.ToString())}");
            Output.SendSystemMessage(ctx, $"Latency: {P_Color.White(ping.ToString())}s");
            Output.SendSystemMessage(ctx, $"-- Position --");
            Output.SendSystemMessage(ctx, $"X: {P_Color.White(Math.Round(position.x,2).ToString())} " +
                $"Y: {P_Color.White(Math.Round(position.y,2).ToString())} " +
                $"Z: {P_Color.White(Math.Round(position.z,2).ToString())}");
            Output.SendSystemMessage(ctx, $"-- {P_Color.White("Entities")} --");
            Output.SendSystemMessage(ctx, $"Char Entity: {P_Color.White(CharacterEntity)}");
            Output.SendSystemMessage(ctx, $"User Entity: {P_Color.White(UserEntity)}");
            Output.SendSystemMessage(ctx, $"-- {P_Color.White("Experience")} --");
            Output.SendSystemMessage(ctx, $"-- {P_Color.White("PvP Stats")} --");

            if (PvPSystem.isHonorSystemEnabled)
            {
                Database.SiegeState.TryGetValue(SteamID, out var siegeState);
                P_Cache.HostilityState.TryGetValue(playerEntity, out var hostilityState);

                double tLeft = 0;
                if (siegeState.IsSiegeOn)
                {
                    TimeSpan TimeLeft = siegeState.SiegeEndTime - DateTime.Now;
                    tLeft = Math.Round(TimeLeft.TotalHours, 2);
                }

                string hostilityText = hostilityState.IsHostile ? "Aggresive" : "Passive";
                string siegeText = siegeState.IsSiegeOn ? "Sieging" : "Defensive";

                Output.SendSystemMessage(ctx, $"Reputation: {P_Color.White(pvpStats.Reputation.ToString())}");
                Output.SendSystemMessage(ctx, $"Hostility: {P_Color.White(hostilityText)}");
                Output.SendSystemMessage(ctx, $"Siege: {P_Color.White(siegeText)}");
                Output.SendSystemMessage(ctx, $"-- Time Left: {P_Color.White(tLeft.ToString())} hour(s)");
            }

            Output.SendSystemMessage(ctx, $"K/D: {P_Color.White(pvpStats.KD.ToString())} " +
                $"Kill: {P_Color.White(pvpStats.Kills.ToString())} " +
                $"Death: {P_Color.White(pvpStats.Deaths.ToString())}");
		}
	}
	
	
	
	public static class MyInfo
    {
		[Command("myinfo", "me", description: "Display your information details.")]
		public static void getMyInfo(Context ctx)
        {
            ulong SteamID = ctx.Event.User.PlatformId;
            string Name = ctx.Event.User.CharacterName.ToString();
            string CharacterEntity = ctx.Event.SenderCharacterEntity.Index.ToString() + ":" + ctx.Event.SenderCharacterEntity.Version.ToString();
            string UserEntity = ctx.Event.SenderUserEntity.Index.ToString() + ":" + ctx.Event.SenderUserEntity.Version.ToString();
            var ping = ctx.EntityManager.GetComponentData<Latency>(ctx.Event.SenderCharacterEntity).Value;
            var position = ctx.EntityManager.GetComponentData<Translation>(ctx.Event.SenderCharacterEntity).Value;

            Output.SendSystemMessage(ctx, $"Name: {P_Color.White(Name)}");
            Output.SendSystemMessage(ctx, $"SteamID: {P_Color.White(SteamID.ToString())}");
            Output.SendSystemMessage(ctx, $"Latency: {P_Color.White(ping.ToString())}s");
            Output.SendSystemMessage(ctx, $"-- Position --");
            Output.SendSystemMessage(ctx, $"X: {P_Color.White(Math.Round(position.x,2).ToString())} " +
                $"Y: {P_Color.White(Math.Round(position.y,2).ToString())} " +
                $"Z: {P_Color.White(Math.Round(position.z,2).ToString())}");
            Output.SendSystemMessage(ctx, $"-- Entities --");
            Output.SendSystemMessage(ctx, $"Char Entity: {P_Color.White(CharacterEntity)}");
            Output.SendSystemMessage(ctx, $"User Entity: {P_Color.White(UserEntity)}");
        }
    }
}

