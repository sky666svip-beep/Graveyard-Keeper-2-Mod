using System;
using System.Reflection;
using HarmonyLib;

namespace KeeperCheatMenu
{
	// Token: 0x02000028 RID: 40
	[HarmonyPatch(typeof(UIFishingWindow), "OnSubmitBait")]
	internal static class SharedStorageFishingBaitContextPatch
	{
		// Token: 0x17000027 RID: 39
		// (get) Token: 0x0600017C RID: 380 RVA: 0x00017E74 File Offset: 0x00016074
		internal static string SelectedBaitId
		{
			get
			{
				return SharedStorageFishingBaitContextPatch._selectedBaitId;
			}
		}

		// Token: 0x0600017D RID: 381 RVA: 0x00017E7C File Offset: 0x0001607C
		[HarmonyPrefix]
		private static bool BeginSharedBaitRemoval(UIFishingWindow __instance, ref bool __result, out bool __state)
		{
			__state = false;
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || !instance.SharedStorageEnabled)
			{
				return true;
			}
			string text;
			if (!(__instance == null))
			{
				MethodInfo getCurrentBaitIdMethod = SharedStorageFishingBaitContextPatch.GetCurrentBaitIdMethod;
				text = ((getCurrentBaitIdMethod != null) ? getCurrentBaitIdMethod.Invoke(__instance, null) : null) as string;
			}
			else
			{
				text = null;
			}
			string text2 = text;
			if (string.IsNullOrEmpty(text2) || text2 == "no_bait")
			{
				return true;
			}
			PlayerData playerData = MainGame.PlayerData;
			Inventory inventory = ((playerData != null) ? playerData.inventory : null);
			int? num;
			if (inventory == null)
			{
				num = null;
			}
			else
			{
				Item data = inventory.Data;
				num = ((data != null) ? new int?(data.GetTotalCountInInventory(text2, null, false)) : null);
			}
			int? num2 = num;
			if (num2.GetValueOrDefault() + SharedStorageInventoryHelper.GetStoredItemCount(text2) <= 0)
			{
				__result = false;
				return false;
			}
			SharedStorageFishingBaitContextPatch._selectedBaitId = text2;
			__state = true;
			return true;
		}

		// Token: 0x0600017E RID: 382 RVA: 0x00017F42 File Offset: 0x00016142
		[HarmonyPostfix]
		private static void EndSharedBaitRemoval(bool __state)
		{
			if (__state)
			{
				SharedStorageFishingBaitContextPatch._selectedBaitId = null;
			}
		}

		// Token: 0x04000120 RID: 288
		[ThreadStatic]
		private static string _selectedBaitId;

		// Token: 0x04000121 RID: 289
		private static readonly MethodInfo GetCurrentBaitIdMethod = AccessTools.Method(typeof(UIFishingWindow), "GetCurrentBaitId", null, null);
	}
}
