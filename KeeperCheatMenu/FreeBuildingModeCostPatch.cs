using System;
using System.Collections.Generic;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x0200000C RID: 12
	[HarmonyPatch(typeof(BuildManager), "EnableBuildMode")]
	internal static class FreeBuildingModeCostPatch
	{
		// Token: 0x06000146 RID: 326 RVA: 0x00016C8A File Offset: 0x00014E8A
		[HarmonyPrefix]
		private static void RemovePlacementCost(ref List<NeedItemData> __2)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance != null && instance.FreeBuildingCostsEnabled)
			{
				__2 = new List<NeedItemData>();
			}
		}
	}
}
