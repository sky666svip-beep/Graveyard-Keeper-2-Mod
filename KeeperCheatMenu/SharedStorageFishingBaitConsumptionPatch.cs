using System;
using System.Collections.Generic;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000029 RID: 41
	[HarmonyPatch(typeof(Inventory), "RemoveItemById", new Type[]
	{
		typeof(string),
		typeof(int),
		typeof(Item),
		typeof(Item),
		typeof(bool)
	})]
	internal static class SharedStorageFishingBaitConsumptionPatch
	{
		// Token: 0x06000180 RID: 384 RVA: 0x00017F6C File Offset: 0x0001616C
		[HarmonyPrefix]
		private static bool ConsumeFromStorageWhenPlayerHasNone(Inventory __instance, string __0, int __1, ref List<Item> __result)
		{
			string selectedBaitId = SharedStorageFishingBaitContextPatch.SelectedBaitId;
			PlayerData playerData = MainGame.PlayerData;
			Inventory inventory = ((playerData != null) ? playerData.inventory : null);
			if (string.IsNullOrEmpty(selectedBaitId) || __instance != inventory || !string.Equals(__0, selectedBaitId, StringComparison.Ordinal) || __1 != 1)
			{
				return true;
			}
			if (inventory.Data.GetTotalCountInInventory(selectedBaitId, null, false) > 0)
			{
				return true;
			}
			if (!SharedStorageInventoryHelper.TryConsumeStoredItem(selectedBaitId))
			{
				return true;
			}
			__result = new List<Item>
			{
				new Item(selectedBaitId, 1)
			};
			return false;
		}
	}
}
