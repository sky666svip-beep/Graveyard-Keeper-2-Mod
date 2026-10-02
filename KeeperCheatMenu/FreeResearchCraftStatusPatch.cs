using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000010 RID: 16
	[HarmonyPatch(typeof(CraftElementBase), "CanStartCraft")]
	internal static class FreeResearchCraftStatusPatch
	{
		// Token: 0x0600014B RID: 331 RVA: 0x00016DEE File Offset: 0x00014FEE
		[HarmonyPostfix]
		private static void IgnoreResearchResourceStatus(CraftElementBase __instance, ref CraftStatus __result)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance != null && instance.FreeResearchEnabled && __instance is CraftElementSurvey)
			{
				__result = CraftStatus.OK;
			}
		}
	}
}
