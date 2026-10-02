using System;
using System.Collections.Generic;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x0200000B RID: 11
	[HarmonyPatch(typeof(UIBuildingWidgetData), "GetCurrentNeedItems")]
	internal static class FreeBuildingMenuCostPatch
	{
		// Token: 0x06000145 RID: 325 RVA: 0x00016C6E File Offset: 0x00014E6E
		[HarmonyPostfix]
		private static void RemoveDisplayedSelectionCost(ref List<NeedItemData> __result)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance != null && instance.FreeBuildingCostsEnabled)
			{
				__result = new List<NeedItemData>();
			}
		}
	}
}
