using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000024 RID: 36
	[HarmonyPatch(typeof(MultiInventory), MethodType.Constructor, new Type[]
	{
		typeof(WorldZoneData),
		typeof(WgoData),
		typeof(bool)
	})]
	internal static class SharedStorageZoneInventoryConstructorPatch
	{
		// Token: 0x06000174 RID: 372 RVA: 0x00017941 File Offset: 0x00015B41
		[HarmonyPostfix]
		private static void AddAllStorageContainers(MultiInventory __instance)
		{
			SharedStorageInventoryHelper.Extend(__instance);
		}
	}
}
