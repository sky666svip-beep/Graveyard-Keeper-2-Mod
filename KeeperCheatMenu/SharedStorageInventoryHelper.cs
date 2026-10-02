using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KeeperCheatMenu
{
	// Token: 0x02000026 RID: 38
	internal static class SharedStorageInventoryHelper
	{
		// Token: 0x06000176 RID: 374 RVA: 0x00017954 File Offset: 0x00015B54
		internal static void Extend(MultiInventory multiInventory)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || !instance.SharedStorageEnabled || multiInventory == null)
			{
				return;
			}
			try
			{
				FieldInfo inventoryListField = SharedStorageInventoryHelper.InventoryListField;
				List<Inventory> list = ((inventoryListField != null) ? inventoryListField.GetValue(multiInventory) : null) as List<Inventory>;
				if (list != null)
				{
					SharedStorageInventoryHelper.RefreshStorageCache();
					HashSet<Inventory> hashSet = new HashSet<Inventory>(list);
					bool flag = false;
					foreach (Inventory inventory in SharedStorageInventoryHelper.CachedStorageInventories)
					{
						if (hashSet.Add(inventory))
						{
							list.Add(inventory);
							flag = true;
						}
					}
					if (flag)
					{
						MethodInfo sortMethod = SharedStorageInventoryHelper.SortMethod;
						if (sortMethod != null)
						{
							sortMethod.Invoke(multiInventory, null);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Keeper Cheat Menu] Could not extend shared crafting storage: " + ex.Message);
			}
		}

		// Token: 0x06000177 RID: 375 RVA: 0x00017A3C File Offset: 0x00015C3C
		internal static int GetStoredItemCount(string itemId)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || !instance.SharedStorageEnabled || string.IsNullOrEmpty(itemId))
			{
				return 0;
			}
			int num2;
			try
			{
				SharedStorageInventoryHelper.RefreshStorageCache();
				int num = 0;
				foreach (Inventory inventory in SharedStorageInventoryHelper.CachedStorageInventories)
				{
					if (((inventory != null) ? inventory.Data : null) != null)
					{
						num += inventory.Data.GetTotalCountInInventory(itemId, null, false);
					}
				}
				num2 = num;
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Keeper Cheat Menu] Could not count shared-storage bait: " + ex.Message);
				num2 = 0;
			}
			return num2;
		}

		// Token: 0x06000178 RID: 376 RVA: 0x00017AFC File Offset: 0x00015CFC
		internal static bool TryConsumeStoredItem(string itemId)
		{
			KeeperCheatMenuPlugin instance = KeeperCheatMenuPlugin.Instance;
			if (instance == null || !instance.SharedStorageEnabled || string.IsNullOrEmpty(itemId))
			{
				return false;
			}
			try
			{
				SharedStorageInventoryHelper.RefreshStorageCache();
				foreach (Inventory inventory in SharedStorageInventoryHelper.CachedStorageInventories)
				{
					if (((inventory != null) ? inventory.Data : null) != null && inventory.Data.GetTotalCountInInventory(itemId, null, false) > 0)
					{
						List<Item> list = inventory.RemoveItemById(itemId, 1, null, null, false);
						if (list != null)
						{
							if (list.Sum<Item>(delegate(Item item)
							{
								if (item == null)
								{
									return 0;
								}
								return item.Count;
							}) > 0)
							{
								return true;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Keeper Cheat Menu] Could not consume shared-storage bait: " + ex.Message);
			}
			return false;
		}

		// Token: 0x06000179 RID: 377 RVA: 0x00017BF4 File Offset: 0x00015DF4
		private static void RefreshStorageCache()
		{
			WorldData worldData = MainGame.WorldData;
			if (worldData == null)
			{
				return;
			}
			if (worldData == SharedStorageInventoryHelper._cachedWorld && Time.unscaledTime < SharedStorageInventoryHelper._nextCacheRefresh)
			{
				return;
			}
			SharedStorageInventoryHelper._cachedWorld = worldData;
			SharedStorageInventoryHelper._nextCacheRefresh = Time.unscaledTime + 5f;
			SharedStorageInventoryHelper.CachedStorageInventories.Clear();
			FieldInfo gameSceneListField = SharedStorageInventoryHelper.GameSceneListField;
			List<GameSceneData> list = ((gameSceneListField != null) ? gameSceneListField.GetValue(worldData) : null) as List<GameSceneData>;
			if (list == null)
			{
				return;
			}
			HashSet<Inventory> hashSet = new HashSet<Inventory>();
			foreach (GameSceneData gameSceneData in list)
			{
				FieldInfo sceneWgoListField = SharedStorageInventoryHelper.SceneWgoListField;
				List<WgoData> list2 = ((sceneWgoListField != null) ? sceneWgoListField.GetValue(gameSceneData) : null) as List<WgoData>;
				if (list2 != null)
				{
					foreach (WgoData wgoData in list2)
					{
						WGODef wgodef = ((wgoData != null) ? wgoData.Definition : null);
						Inventory inventory = ((wgoData != null) ? wgoData.Inventory : null);
						if (wgodef != null && inventory != null && wgodef.inventorySize > 0 && wgodef.OpenInMultiInventory && hashSet.Add(inventory))
						{
							SharedStorageInventoryHelper.CachedStorageInventories.Add(inventory);
						}
					}
				}
			}
		}

		// Token: 0x04000119 RID: 281
		private static readonly FieldInfo InventoryListField = AccessTools.Field(typeof(MultiInventory), "inventoryList");

		// Token: 0x0400011A RID: 282
		private static readonly FieldInfo GameSceneListField = AccessTools.Field(typeof(WorldData), "gameSceneDataList");

		// Token: 0x0400011B RID: 283
		private static readonly FieldInfo SceneWgoListField = AccessTools.Field(typeof(GameSceneData), "wgoDataList");

		// Token: 0x0400011C RID: 284
		private static readonly MethodInfo SortMethod = AccessTools.Method(typeof(MultiInventory), "Sort", null, null);

		// Token: 0x0400011D RID: 285
		private static readonly List<Inventory> CachedStorageInventories = new List<Inventory>();

		// Token: 0x0400011E RID: 286
		private static WorldData _cachedWorld;

		// Token: 0x0400011F RID: 287
		private static float _nextCacheRefresh;
	}
}
