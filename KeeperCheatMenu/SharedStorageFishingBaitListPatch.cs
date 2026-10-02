using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000027 RID: 39
	[HarmonyPatch(typeof(UIFishingWindowData), "UpdateAvailableFishingAndBaits")]
	internal static class SharedStorageFishingBaitListPatch
	{
		// Token: 0x0600017B RID: 379 RVA: 0x00017DC8 File Offset: 0x00015FC8
		[HarmonyPostfix]
		private static void IncludeStoredBait(UIFishingWindowData __instance)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || !instance.SharedStorageEnabled || ((__instance != null) ? __instance.BaitItems : null) == null)
			{
				return;
			}
			foreach (Item item in __instance.BaitItems)
			{
				if (item != null && !string.IsNullOrEmpty(item.id) && !(item.id == "no_bait"))
				{
					item.Count += SharedStorageInventoryHelper.GetStoredItemCount(item.id);
				}
			}
		}
	}
}
