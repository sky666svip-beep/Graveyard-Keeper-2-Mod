using System;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000025 RID: 37
	[HarmonyPatch(typeof(MultiInventory), MethodType.Constructor, new Type[]
	{
		typeof(PlayerData),
		typeof(bool)
	})]
	internal static class SharedStoragePlayerInventoryConstructorPatch
	{
		// Token: 0x06000175 RID: 373 RVA: 0x00017949 File Offset: 0x00015B49
		[HarmonyPostfix]
		private static void AddAllStorageContainers(MultiInventory __instance)
		{
			SharedStorageInventoryHelper.Extend(__instance);
		}
	}
}
