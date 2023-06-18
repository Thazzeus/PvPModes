using PvPModes.Models;
using ProjectM;
using Unity.Entities;
using UnityEngine;
using VampireCommandFramework;
using static PvPModes.Utils.Helper;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PvPModes.Data;
using ProjectM.Network;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using static ProjectM.Roofs.RoofTestSceneBootstrapNew;


namespace PvPModes.Commands;

internal static class BloodCommands
{

	[Command("bloodpotion", "bp", description: "Creates a Potion with specified Blood Type, Quality and Value", adminOnly: true)]
	public static void GiveBloodPotionCommand(ChatCommandContext ctx, P_BloodType type = P_BloodType.Frailed, float quality = 100f)
	{
		quality = Mathf.Clamp(quality, 0, 100);

		Entity entity = Utils.Helper.AddItemToInventory(ctx.Event.SenderCharacterEntity, new PrefabGUID(828432508), 1);

		var blood = new StoredBlood()
		{
			BloodQuality = quality,
			BloodType = new PrefabGUID((int)type)
		};

		Core.EntityManager.SetComponentData(entity, blood);

		ctx.Reply($"Got Blood Potion Type <color=#ff0>{type}</color> with <color=#ff0>{quality}</color>% quality");
	}
}

