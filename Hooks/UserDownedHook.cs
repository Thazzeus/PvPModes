using HarmonyLib;
using ProjectM;
using PvPModes.Systems;
using Unity.Collections;
using Unity.Entities;

namespace PvPModes.Hooks
{
	[HarmonyPatch(typeof(VampireDownedServerEventSystem), nameof(VampireDownedServerEventSystem.OnUpdate))]
	public class VampireDownedServerEventSystem_Patch
	{
		public static void Postfix(VampireDownedServerEventSystem __instance)
		{
			//if (__instance.__OnUpdate_LambdaJob0_entityQuery == null) return;

			EntityManager em = __instance.EntityManager;
			var EventsQuery = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);

			foreach (var entity in EventsQuery)
			{
				VampireDownedServerEventSystem.TryFindRootOwner(entity, 1, em, out var Victim);
				Entity Source = em.GetComponentData<VampireDownedBuff>(entity).Source;
				VampireDownedServerEventSystem.TryFindRootOwner(Source, 1, em, out var Killer);

				//-- Update PvP Stats & Check
				if (em.HasComponent<PlayerCharacter>(Killer) && em.HasComponent<PlayerCharacter>(Victim) && !Killer.Equals(Victim))
				{
					PvPSystem.Monitor(Killer, Victim);
					if (PvPSystem.isPunishEnabled) PvPSystem.PunishCheck(Killer, Victim);
				}
			}
		}
	}
}
