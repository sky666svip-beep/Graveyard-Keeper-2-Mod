using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000019 RID: 25
	[HarmonyPatch]
	internal static class FishingRangeOverlayPatches
	{
		// Token: 0x0600015D RID: 349 RVA: 0x00017246 File Offset: 0x00015446
		[HarmonyPostfix]
		[HarmonyPatch(typeof(WGOInteractionHandlerBase), "OnInteractionTargetEnter")]
		private static void OnInteractionTargetEnter(WGOInteractionHandlerBase __instance, Wgo ___assignedWgo)
		{
			if (__instance is ReservoirInteractionHandler)
			{
				KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
				if (instance == null)
				{
					return;
				}
				instance.SetFishingReservoirInRange(___assignedWgo);
			}
		}

		// Token: 0x0600015E RID: 350 RVA: 0x00017260 File Offset: 0x00015460
		[HarmonyPostfix]
		[HarmonyPatch(typeof(WGOInteractionHandlerBase), "OnInteractionTargetExit")]
		private static void OnInteractionTargetExit(WGOInteractionHandlerBase __instance, Wgo ___assignedWgo)
		{
			if (__instance is ReservoirInteractionHandler)
			{
				KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
				if (instance == null)
				{
					return;
				}
				instance.ClearFishingReservoirInRange(___assignedWgo);
			}
		}
	}
}
