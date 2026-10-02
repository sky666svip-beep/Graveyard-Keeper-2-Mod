using System;
using System.Collections.Generic;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x0200000E RID: 14
	[HarmonyPatch(typeof(UIResourceBasedCraftWindowData), "CreateCraftElement")]
	internal static class FreeResearchRequirementsPatch
	{
		// Token: 0x06000149 RID: 329 RVA: 0x00016DA0 File Offset: 0x00014FA0
		[HarmonyPostfix]
		private static void RemoveResearchCosts(CraftElementSurvey __result)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance != null && instance.FreeResearchEnabled && __result != null)
			{
				List<NeedItemData> requirements = __result.Requirements;
				if (requirements == null)
				{
					return;
				}
				requirements.Clear();
			}
		}
	}
}
