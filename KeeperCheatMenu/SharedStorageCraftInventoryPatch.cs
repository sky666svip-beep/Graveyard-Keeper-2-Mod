using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000023 RID: 35
	[HarmonyPatch(typeof(WgoData), "GetCraftableMultiInventory")]
	internal static class SharedStorageCraftInventoryPatch
	{
		// Token: 0x06000173 RID: 371 RVA: 0x00017938 File Offset: 0x00015B38
		[HarmonyPostfix]
		private static void AddAllStorageContainers(ref MultiInventory __result)
		{
			SharedStorageInventoryHelper.Extend(__result);
		}
	}
}
