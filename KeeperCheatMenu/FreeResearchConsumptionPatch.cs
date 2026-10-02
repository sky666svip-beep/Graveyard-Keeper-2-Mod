using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000011 RID: 17
	[HarmonyPatch(typeof(CraftElementSurvey), "RemoveCraftRequirements")]
	internal static class FreeResearchConsumptionPatch
	{
		// Token: 0x0600014C RID: 332 RVA: 0x00016E0E File Offset: 0x0001500E
		[HarmonyPrefix]
		private static bool KeepResearchResources(CraftElementSurvey __instance)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || !instance.FreeResearchEnabled)
			{
				return true;
			}
			if (__instance != null)
			{
				__instance.IsAllRequirementsTaken = true;
			}
			return false;
		}
	}
}
