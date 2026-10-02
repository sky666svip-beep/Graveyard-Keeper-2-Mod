using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace KeeperCheatMenu
{
	// Token: 0x02000038 RID: 56
	[HarmonyPatch]
	internal static class ZombieModifierPatches
	{
		// Token: 0x17000029 RID: 41
		// (get) Token: 0x06000198 RID: 408 RVA: 0x00018494 File Offset: 0x00016694
		private static KeeperCheatMenuPlugin Plugin
		{
			get
			{
				return KeeperCheatMenuPlugin.Instance;
			}
		}

		// Token: 0x06000199 RID: 409 RVA: 0x0001849B File Offset: 0x0001669B
		[HarmonyPrefix]
		[HarmonyPatch(typeof(MovementComponent), "Update")]
		private static void ScaleZombieMovement(IMovable ___assignedMovableObject, ref float deltaTime)
		{
			if (___assignedMovableObject is ZombieWgoData && ZombieModifierPatches.Plugin != null)
			{
				deltaTime *= Mathf.Clamp(ZombieModifierPatches.Plugin.ZombieMoveSpeedMultiplier, 0.1f, 100f);
			}
		}

		// Token: 0x0600019A RID: 410 RVA: 0x000184D0 File Offset: 0x000166D0
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ZombieCraftActivity), "Update")]
		private static void ScaleZombieCraftWork(ref float deltaTime)
		{
			if (ZombieModifierPatches.Plugin != null)
			{
				deltaTime *= Mathf.Clamp(ZombieModifierPatches.Plugin.ZombieWorkSpeedMultiplier, 0.1f, 100f);
			}
		}

		// Token: 0x0600019B RID: 411 RVA: 0x000184FD File Offset: 0x000166FD
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ZombieHPActivity), "Update")]
		private static void ScaleZombieHpWork(ref float deltaTime)
		{
			if (ZombieModifierPatches.Plugin != null)
			{
				deltaTime *= Mathf.Clamp(ZombieModifierPatches.Plugin.ZombieWorkSpeedMultiplier, 0.1f, 100f);
			}
		}

		// Token: 0x0600019C RID: 412 RVA: 0x0001852C File Offset: 0x0001672C
		[HarmonyPrefix]
		[HarmonyPatch(typeof(CraftComponent), "Update")]
		private static void ScaleZombieAutomaticWork(CraftComponent __instance, ref float deltaTime)
		{
			object obj;
			if (__instance == null)
			{
				obj = null;
			}
			else
			{
				ICraftable craftableObject = __instance.CraftableObject;
				obj = ((craftableObject != null) ? craftableObject.CraftableAttachedWorker : null);
			}
			if (obj is ZombieWgoData && ZombieModifierPatches.Plugin != null)
			{
				deltaTime *= Mathf.Clamp(ZombieModifierPatches.Plugin.ZombieWorkSpeedMultiplier, 0.1f, 100f);
			}
		}

		// Token: 0x0600019D RID: 413 RVA: 0x00018584 File Offset: 0x00016784
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ZombieWgoData), "DoTechPointsReward")]
		private static void ScaleZombieExperience(ref int r, ref int g, ref int b)
		{
			if (ZombieModifierPatches.Plugin == null)
			{
				return;
			}
			r = ZombieModifierPatches.ScaleReward(r, ZombieModifierPatches.Plugin.ZombieRedExperienceMultiplier);
			g = ZombieModifierPatches.ScaleReward(g, ZombieModifierPatches.Plugin.ZombieGreenExperienceMultiplier);
			b = ZombieModifierPatches.ScaleReward(b, ZombieModifierPatches.Plugin.ZombieBlueExperienceMultiplier);
		}

		// Token: 0x0600019E RID: 414 RVA: 0x000185D8 File Offset: 0x000167D8
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ZombieWgoData), "CrafterAddCraftDrop")]
		private static void TrackCrafterOutputBeforeAdd(ZombieWgoData __instance, Item drop, out ZombieModifierPatches.CrafterOutputAddState __state)
		{
			__state = default(ZombieModifierPatches.CrafterOutputAddState);
			KeeperCheatMenuPlugin plugin = ZombieModifierPatches.Plugin;
			if (plugin != null && plugin.ZombieCollectFinishedProductsEnabled)
			{
				bool flag;
				if (__instance == null)
				{
					flag = null != null;
				}
				else
				{
					Inventory workerInventory = __instance.WorkerInventory;
					flag = ((workerInventory != null) ? workerInventory.Data : null) != null;
				}
				if (flag && drop != null && !string.IsNullOrEmpty(drop.id))
				{
					__state.ItemId = drop.id;
					__state.RequestedCount = drop.Count;
					__state.CountBefore = __instance.WorkerInventory.Data.GetTotalCountInInventory(drop.id, null, false);
					return;
				}
			}
		}

		// Token: 0x0600019F RID: 415 RVA: 0x00018664 File Offset: 0x00016864
		[HarmonyPostfix]
		[HarmonyPatch(typeof(ZombieWgoData), "CrafterAddCraftDrop")]
		private static void TrackCrafterOutputAfterAdd(ZombieWgoData __instance, ZombieModifierPatches.CrafterOutputAddState __state)
		{
			KeeperCheatMenuPlugin plugin = ZombieModifierPatches.Plugin;
			if (plugin != null && plugin.ZombieCollectFinishedProductsEnabled)
			{
				bool flag;
				if (__instance == null)
				{
					flag = null != null;
				}
				else
				{
					Inventory workerInventory = __instance.WorkerInventory;
					flag = ((workerInventory != null) ? workerInventory.Data : null) != null;
				}
				if (flag && !string.IsNullOrEmpty(__state.ItemId) && __state.RequestedCount > 0)
				{
					if (__instance.WorkerInventory.Data.GetTotalCountInInventory(__state.ItemId, null, false) - __state.CountBefore < __state.RequestedCount)
					{
						ZombieModifierPatches.FailedCrafterOutputAdds.Add(__instance.UniqueId);
					}
					return;
				}
			}
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x000186F0 File Offset: 0x000168F0
		[HarmonyPostfix]
		[HarmonyPatch(typeof(CraftComponent), "PreFinishUpdate")]
		private static void CollectFinishedCrafterProducts(CraftComponent __instance)
		{
			try
			{
				KeeperCheatMenuPlugin plugin = ZombieModifierPatches.Plugin;
				if (plugin != null && plugin.ZombieCollectFinishedProductsEnabled && __instance != null && __instance.Status == (CraftComponentStatus)8)
				{
					ICraftable craftableObject = __instance.CraftableObject;
					ZombieWgoData zombieWgoData = ((craftableObject != null) ? craftableObject.CraftableAttachedWorker : null) as ZombieWgoData;
					if (zombieWgoData != null && zombieWgoData.ZombieType == (ZombieType)1)
					{
						WorldZoneData worldZoneData = zombieWgoData.WorldZoneData;
						Inventory workerInventory = zombieWgoData.WorkerInventory;
						if (worldZoneData != null && ((workerInventory != null) ? workerInventory.Data : null) != null)
						{
							if (!ZombieModifierPatches.FailedCrafterOutputAdds.Remove(zombieWgoData.UniqueId))
							{
								List<OrderBase> list = worldZoneData.FindOrdersByTarget(zombieWgoData.UniqueId, typeof(PickupOrder));
								if (list != null && list.Count != 0)
								{
									Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
									foreach (OrderBase orderBase in list)
									{
										Item item = ((orderBase != null) ? orderBase.Item : null);
										if (item == null || string.IsNullOrEmpty(item.id) || item.Count <= 0)
										{
											return;
										}
										int num;
										dictionary.TryGetValue(item.id, out num);
										dictionary[item.id] = num + item.Count;
									}
									foreach (KeyValuePair<string, int> keyValuePair in dictionary)
									{
										if (workerInventory.Data.GetTotalCountInInventory(keyValuePair.Key, null, false) < keyValuePair.Value)
										{
											return;
										}
									}
									foreach (OrderBase orderBase2 in list)
									{
										worldZoneData.RemoveOrder(orderBase2.UniqueId);
									}
									zombieWgoData.CrafterFinishAndContinueAfterBigItemDropped();
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Keeper Cheat Menu] Could not auto-collect a zombie craft output: " + ex.Message);
			}
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x00018944 File Offset: 0x00016B44
		private static int ScaleReward(int value, float multiplier)
		{
			if (value <= 0)
			{
				return value;
			}
			return Mathf.Max(1, Mathf.RoundToInt((float)value * Mathf.Clamp(multiplier, 0.1f, 100f)));
		}

		// Token: 0x0400012F RID: 303
		private static readonly HashSet<SGuid> FailedCrafterOutputAdds = new HashSet<SGuid>();

		// Token: 0x02000060 RID: 96
		private struct CrafterOutputAddState
		{
			// Token: 0x040001A6 RID: 422
			internal string ItemId;

			// Token: 0x040001A7 RID: 423
			internal int CountBefore;

			// Token: 0x040001A8 RID: 424
			internal int RequestedCount;
		}
	}
}
