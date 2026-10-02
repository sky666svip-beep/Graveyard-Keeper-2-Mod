using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000013 RID: 19
	[HarmonyPatch(typeof(Wgo), "ReinitBalanceRelatedStuff")]
	internal static class FishingPondInitialStockPatch
	{
		// Token: 0x0600014F RID: 335 RVA: 0x00016FC0 File Offset: 0x000151C0
		[HarmonyPostfix]
		private static void RefillMultipliedStock(Wgo __instance)
		{
			FishingPondStockHelper.RefillToConfiguredCapacity(__instance, false);
		}
	}
}
