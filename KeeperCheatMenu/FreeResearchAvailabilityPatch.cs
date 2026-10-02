using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x0200000F RID: 15
	[HarmonyPatch(typeof(UIResourceBasedCraftWindowData), "CanStartCraft")]
	internal static class FreeResearchAvailabilityPatch
	{
		// Token: 0x0600014A RID: 330 RVA: 0x00016DC8 File Offset: 0x00014FC8
		[HarmonyPostfix]
		private static void AllowResearch(UIResourceBasedCraftWindowData __instance, ref bool __result)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance != null && instance.FreeResearchEnabled && ((__instance != null) ? __instance.SelectedItem : null) != null)
			{
				__result = true;
			}
		}
	}
}
